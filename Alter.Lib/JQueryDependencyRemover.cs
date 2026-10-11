using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Alter.Lib;
    /// <summary>
    /// Removes jQuery dependencies and converts common jQuery patterns to vanilla ES6+ JavaScript
    /// Output is JSHint-compliant without requiring helper wrappers
    /// </summary>
    public static class JQueryDependencyRemover
    {
        /// <summary>
        /// Removes jQuery dependencies and converts common jQuery patterns to vanilla ES6+ JavaScript
        /// </summary>
        /// <param name="javascriptCode">The JavaScript code string to process</param>
        /// <returns>JSHint-compliant JavaScript code without jQuery dependencies</returns>
        public static string RemoveJQueryDependencies(string javascriptCode)
        {
            if (string.IsNullOrWhiteSpace(javascriptCode))
                return javascriptCode;

            string result = javascriptCode;

            // 1. Remove jQuery import/require statements
            result = Regex.Replace(result, @"import\s+\$\s+from\s+['""](?:jquery|\$)['""];?", "", RegexOptions.Multiline);
            result = Regex.Replace(result, @"require\s*\(\s*['""](?:jquery|\$)['""]?\s*\);?", "", RegexOptions.Multiline);
            result = Regex.Replace(result, @"require\s*\(\s*['""]jquery['""]\s*\)\s*;?", "", RegexOptions.Multiline);

            // 2. Remove CDN script tags referencing jQuery
            result = Regex.Replace(result, @"<script[^>]*src=['""][^'"">]*jquery[^'"">]*['""][^>]*>\s*</script>", "", RegexOptions.Multiline);

            // 3. Convert common jQuery patterns to vanilla JS
            result = ConvertJQueryToES6(result);

            // 4. Clean up multiple consecutive blank lines created by removals
            result = Regex.Replace(result, @"\n\s*\n\s*\n+", "\n\n", RegexOptions.Multiline);

            // 5. Trim leading/trailing whitespace
            result = result.Trim();

            return result;
        }

        /// <summary>
        /// Async version for use in async contexts
        /// </summary>
        public static async Task<string> RemoveJQueryDependenciesAsync(string javascriptCode)
        {
            return await Task.Run(() => RemoveJQueryDependencies(javascriptCode));
        }

        public static string ConvertJQueryToES6(string jQueryCode)
{
    string es6Code = jQueryCode;

    // Step 1: Convert $(document).ready(function() { ... }) to DOMContentLoaded.
    // The original closing "});" balances the addEventListener call, so NO IIFE wrapper needed.
    es6Code = Regex.Replace(es6Code,
        @"\$\(document\)\.ready\s*\(\s*function\s*\(\s*([^)]*)\s*\)\s*\{",
        match => $"document.addEventListener('DOMContentLoaded', function({match.Groups[1].Value}) {{",
        RegexOptions.Multiline);

    // Step 2: Convert jQuery AJAX calls
    es6Code = ConvertAjaxCalls(es6Code);

    // Step 3: Convert jQuery selectors to proper DOM APIs
    es6Code = ConvertSelectors(es6Code);

    // Step 4: Convert jQuery event handlers
    es6Code = ConvertEventHandlers(es6Code);

    // Step 5: Convert jQuery style methods
    es6Code = ConvertStyleMethods(es6Code);

    // Step 6: Convert jQuery utility methods
    es6Code = ConvertUtilityMethods(es6Code);

    return es6Code;
}

private static string ConvertEventHandlers(string code)
{
    var events = new[] { "click", "change", "submit", "focus", "blur",
        "mouseover", "mouseout", "keyup", "keydown", "load", "scroll", "resize", "dblclick", "input" };

    foreach (var evt in events)
    {
        // .click(function(e) { ... }) -> .addEventListener('click', function(e) {
        // FIXED: capture handler parameters instead of requiring empty ()
        code = Regex.Replace(code,
            $@"\.{evt}\s*\(\s*function\s*\(\s*([^)]*)\s*\)\s*\{{",
            match => $".addEventListener('{evt}', function({match.Groups[1].Value}) {{",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        // .on('click', function(e) { ... }) -> .addEventListener('click', function(e) {
        code = Regex.Replace(code,
            $@"\.on\s*\(\s*['""]{evt}['""]\s*,\s*function\s*\(\s*([^)]*)\s*\)\s*\{{",
            match => $".addEventListener('{evt}', function({match.Groups[1].Value}) {{",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);
    }

    // .off('event') -> .removeEventListener('event', ...)
    code = Regex.Replace(code,
        @"\.off\s*\(\s*['""]([^'""]+)['""]\s*\)",
        match => $".removeEventListener('{match.Groups[1].Value}'",
        RegexOptions.Multiline);

    // .hover(inFn, outFn) -> mouseenter/mouseleave
    code = Regex.Replace(code,
        @"\.hover\s*\(\s*function\s*\(\s*\)\s*\{\s*([^}]*)\s*\}\s*,\s*function\s*\(\s*\)\s*\{\s*([^}]*)\s*\}\s*\)",
        match =>
        {
            string hoverIn = match.Groups[1].Value.Trim();
            string hoverOut = match.Groups[2].Value.Trim();
            return $".addEventListener('mouseenter', function() {{{hoverIn}}}).addEventListener('mouseleave', function() {{{hoverOut}}})";
        },
        RegexOptions.Multiline | RegexOptions.Singleline);

    return code;
}

private static string ConvertAjaxCalls(string code)
{
    // $.ajax({...}) -> fetch
    // FIXED: lazy (.*?) spans nested braces like "data: { id: 123 }"
    // and stops at the first "}" followed by ")" (i.e., the call's closing);
    // required Singleline so "." crosses newlines.
    code = Regex.Replace(code,
        @"\$\.ajax\s*\(\s*\{(.*?)\}\s*\)",
        match => ConvertAjaxObject(match.Groups[1].Value),
        RegexOptions.Singleline);

    // $.get('url', function(data) { ... })
    code = Regex.Replace(code,
        @"\$\.get\s*\(\s*'([^']+)'\s*,\s*function\s*\(\s*([^)]*)\)\s*\{(.*?)\}\s*\)",
        match =>
        {
            string url = match.Groups[1].Value;
            string param = string.IsNullOrWhiteSpace(match.Groups[2].Value.Trim()) ? "data" : match.Groups[2].Value.Trim();
            string body = match.Groups[3].Value;
            return $"fetch('{url}').then(response => response.ok ? response.json() : Promise.reject(new Error('HTTP error'))).then(function({param}) {{{body}}})";
        },
        RegexOptions.Singleline);

    // $.post('url', data, function(data) { ... })
    // FIXED: lazy (.*?) body so nested if/else blocks inside the callback match correctly
    code = Regex.Replace(code,
        @"\$\.post\s*\(\s*'([^']+)'\s*,\s*(\{.*?\}|[^,]+?)\s*,\s*function\s*\(\s*([^)]*)\)\s*\{(.*?)\}\s*\)",
        match =>
        {
            string url = match.Groups[1].Value;
            string postData = match.Groups[2].Value.Trim();
            string param = string.IsNullOrWhiteSpace(match.Groups[3].Value.Trim()) ? "data" : match.Groups[3].Value.Trim();
            string body = match.Groups[4].Value;

            return $"fetch('{url}', {{ method: 'POST', headers: {{'Content-Type': 'application/json'}}, body: JSON.stringify({postData}) }}).then(response => response.ok ? response.json() : Promise.reject(new Error('HTTP error'))).then(function({param}) {{{body}}})";
        },
        RegexOptions.Singleline);

    // $.getJSON('url', function(data) { ... })
    code = Regex.Replace(code,
        @"\$\.getJSON\s*\(\s*'([^']+)'\s*,\s*function\s*\(\s*([^)]*)\)\s*\{(.*?)\}\s*\)",
        match =>
        {
            string url = match.Groups[1].Value;
            string param = string.IsNullOrWhiteSpace(match.Groups[2].Value.Trim()) ? "data" : match.Groups[2].Value.Trim();
            string body = match.Groups[3].Value;
            return $"fetch('{url}').then(response => response.ok ? response.json() : Promise.reject(new Error('HTTP error'))).then(function({param}) {{{body}}})";
        },
        RegexOptions.Singleline);

    return code;
}

        private static string ConvertSelectors(string code)
        {
            // Convert $('#id') to document.getElementById()
            code = Regex.Replace(code,
                @"\$\(['""]#([^'""]+)['""]\)",
                match => $"document.getElementById('{match.Groups[1].Value}')",
                RegexOptions.Multiline);

            // Convert $('.class') to document.querySelector(). Note: only returns first element
            // For multiple elements, the caller needs to handle iteration
            code = Regex.Replace(code,
                @"\$\(['""]\.([^'""]+)['""]\)",
                match => $"document.querySelector('.{match.Groups[1].Value}')",
                RegexOptions.Multiline);

            // Convert $('tag') to document.querySelector
            code = Regex.Replace(code,
                @"\$\(['""]([a-z][a-z0-9]*)['""]\)",
                match => $"document.querySelector('{match.Groups[1].Value}')",
                RegexOptions.IgnoreCase | RegexOptions.Multiline);

            // Convert $('[attribute="value"]') to document.querySelector
            code = Regex.Replace(code,
                @"\$\(['""]\[([^\]]+)\]['""]\)",
                match => $"document.querySelector('[{match.Groups[1].Value}]')",
                RegexOptions.Multiline);

            return code;
        }

        

        private static string ConvertStyleMethods(string code)
        {
            // Convert .css('property', 'value') to .style.property = 'value'
            code = Regex.Replace(code,
                @"\.css\s*\(\s*['""]([^'""]+)['""]\s*,\s*['""]([^'""]*)['""]\s*\)",
                match =>
                {
                    string prop = ConvertCssProperty(match.Groups[1].Value);
                    string value = match.Groups[2].Value;
                    return $".style.{prop} = '{value}'";
                },
                RegexOptions.Multiline);

            // Convert .css({ 'prop': 'value', ... }) to multiple .style assignments
            code = Regex.Replace(code,
                @"\.css\s*\(\s*\{([^}]+)\}\s*\)",
                match =>
                {
                    string properties = match.Groups[1].Value;
                    var styles = new List<string>();
                    var propMatches = Regex.Matches(properties, @"['""]?([^'""]+)['""]?:\s*['""]([^'""]*)['""]");

                    foreach (Match propMatch in propMatches)
                    {
                        string prop = ConvertCssProperty(propMatch.Groups[1].Value.Trim());
                        string value = propMatch.Groups[2].Value;
                        styles.Add($".style.{prop} = '{value}'");
                    }

                    return string.Join("; ", styles);
                },
                RegexOptions.Multiline | RegexOptions.Singleline);

            // Get CSS property value: .css('property') -> getComputedStyle().getPropertyValue()
            code = Regex.Replace(code,
                @"\.css\s*\(\s*['""]([^'""]+)['""]\s*\)",
                match =>
                {
                    string prop = match.Groups[1].Value;
                    return $"window.getComputedStyle(this).getPropertyValue('{prop}')";
                },
                RegexOptions.Multiline);

            return code;
        }

        private static string ConvertUtilityMethods(string code)
        {
            // Class manipulation
            code = Regex.Replace(code,
                @"\.addClass\s*\(\s*['""]([^'""]+)['""]\s*\)",
                match => $".classList.add('{match.Groups[1].Value}')",
                RegexOptions.Multiline);

            code = Regex.Replace(code,
                @"\.removeClass\s*\(\s*['""]([^'""]+)['""]\s*\)",
                match => $".classList.remove('{match.Groups[1].Value}')",
                RegexOptions.Multiline);

            code = Regex.Replace(code,
                @"\.toggleClass\s*\(\s*['""]([^'""]+)['""]\s*\)",
                match => $".classList.toggle('{match.Groups[1].Value}')",
                RegexOptions.Multiline);

            // Text and HTML
            code = Regex.Replace(code,
                @"\.text\s*\(\s*['""]([^'""]*)['""]\s*\)",
                match => $".textContent = '{match.Groups[1].Value}'",
                RegexOptions.Multiline);

            code = Regex.Replace(code,
                @"\.html\s*\(\s*['""]([^'""]*)['""]\s*\)",
                match => $".innerHTML = '{match.Groups[1].Value}'",
                RegexOptions.Multiline);

            code = Regex.Replace(code,
                @"\.append\s*\(\s*['""]([^'""]*)['""]\s*\)",
                match => $".insertAdjacentHTML('beforeend', '{match.Groups[1].Value}')",
                RegexOptions.Multiline);

            // Get/set value
            code = Regex.Replace(code,
                @"\.val\s*\(\s*\)",
                ".value",
                RegexOptions.Multiline);

            code = Regex.Replace(code,
                @"\.val\s*\(\s*['""]([^'""]*)['""]\s*\)",
                match => $".value = '{match.Groups[1].Value}'",
                RegexOptions.Multiline);

            // Show/hide/display
            code = Regex.Replace(code,
                @"\.show\s*\(\s*\)",
                ".style.display = ''",
                RegexOptions.Multiline);

            code = Regex.Replace(code,
                @"\.hide\s*\(\s*\)",
                ".style.display = 'none'",
                RegexOptions.Multiline);

            code = Regex.Replace(code,
                @"\.fadeIn\s*\([^)]*\)",
                ".style.display = ''",
                RegexOptions.Multiline);

            code = Regex.Replace(code,
                @"\.fadeOut\s*\([^)]*\)",
                ".style.display = 'none'",
                RegexOptions.Multiline);

            // Element manipulation
            code = Regex.Replace(code,
                @"\.empty\s*\(\s*\)",
                ".innerHTML = ''",
                RegexOptions.Multiline);

            code = Regex.Replace(code,
                @"\.remove\s*\(\s*\)",
                ".remove()",
                RegexOptions.Multiline);

            code = Regex.Replace(code,
                @"\.detach\s*\(\s*\)",
                ".remove()",
                RegexOptions.Multiline);

            // Attributes
            code = Regex.Replace(code,
                @"\.attr\s*\(\s*['""]([^'""]+)['""]\s*,\s*['""]([^'""]*)['""]\s*\)",
                match => $".setAttribute('{match.Groups[1].Value}', '{match.Groups[2].Value}')",
                RegexOptions.Multiline);

            code = Regex.Replace(code,
                @"\.removeAttr\s*\(\s*['""]([^'""]+)['""]\s*\)",
                match => $".removeAttribute('{match.Groups[1].Value}')",
                RegexOptions.Multiline);

            // Width/height
            code = Regex.Replace(code,
                @"\.width\s*\(\s*(\d+)\s*\)",
                match => $".style.width = '{match.Groups[1].Value}px'",
                RegexOptions.Multiline);

            code = Regex.Replace(code,
                @"\.height\s*\(\s*(\d+)\s*\)",
                match => $".style.height = '{match.Groups[1].Value}px'",
                RegexOptions.Multiline);

            return code;
        }

        

        private static string ConvertAjaxObject(string ajaxParams)
        {
            // Extract url
            var urlMatch = Regex.Match(ajaxParams, @"url:\s*['""]([^'""]+)['""]");
            string url = urlMatch.Success ? urlMatch.Groups[1].Value : "''";

            // Extract method (type)
            var methodMatch = Regex.Match(ajaxParams, @"type:\s*['""]([^'""]+)['""]", RegexOptions.IgnoreCase);
            string method = methodMatch.Success ? methodMatch.Groups[1].Value.ToUpper() : "GET";

            // Extract data
            var dataMatch = Regex.Match(ajaxParams, @"data:\s*(\{[^}]*\}|[^,}]+)");
            string data = dataMatch.Success ? dataMatch.Groups[1].Value.Trim() : "";

            // Extract dataType
            var dataTypeMatch = Regex.Match(ajaxParams, @"dataType:\s*['""]([^'""]+)['""]", RegexOptions.IgnoreCase);
            string dataType = dataTypeMatch.Success ? dataTypeMatch.Groups[1].Value.ToLower() : "json";

            // Extract success callback
            var successMatch = Regex.Match(ajaxParams, @"success:\s*function\s*\(\s*([^)]*)\)\s*\{\s*((?:[^{}]|{[^}]*})*)\s*\}", RegexOptions.Singleline);
            string successParam = successMatch.Success ? successMatch.Groups[1].Value.Trim() : "data";
            string successBody = successMatch.Success ? successMatch.Groups[2].Value : "";

            // Extract error callback
            var errorMatch = Regex.Match(ajaxParams, @"error:\s*function\s*\(\s*([^)]*)\s*,\s*([^)]*)\s*\)\s*\{\s*((?:[^{}]|{[^}]*})*)\s*\}", RegexOptions.Singleline);
            string errorBody = errorMatch.Success ? errorMatch.Groups[3].Value : "";

            // Build fetch call
            string fetchCall = $"fetch('{url}'";

            if (method != "GET" || !string.IsNullOrEmpty(data))
            {
                fetchCall += ", { ";
                if (method != "GET")
                {
                    fetchCall += $"method: '{method}'";
                }

                if (!string.IsNullOrEmpty(data))
                {
                    if (method != "GET") fetchCall += ", ";
                    fetchCall += $"headers: {{'Content-Type': 'application/json'}}, body: JSON.stringify({data})";
                }

                fetchCall += " }";
            }

            fetchCall += ")";

            // Add response handling
            string responseHandler = dataType == "json" ? "response.ok ? response.json() : Promise.reject(new Error('HTTP error'))" : "response.text()";
            fetchCall += $".then(function(response) {{{responseHandler}}})";

            // Add success handler
            if (!string.IsNullOrEmpty(successBody))
            {
                fetchCall += $".then(function({successParam}) {{{successBody}}})";
            }
            else
            {
                fetchCall += $".then(function({successParam}) {{}})";
            }

            // Add error handler
            if (!string.IsNullOrEmpty(errorBody))
            {
                fetchCall += $".catch(function(error) {{{errorBody}}})";
            }
            else
            {
                fetchCall += ".catch(function(error) { console.error('Error:', error); });";
            }

            return fetchCall;
        }

        private static string ConvertCssProperty(string cssProperty)
        {
            var parts = cssProperty.Split('-');
            string camelCase = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                camelCase += char.ToUpper(parts[i][0]) + parts[i].Substring(1);
            }

            return camelCase;
        }
    }