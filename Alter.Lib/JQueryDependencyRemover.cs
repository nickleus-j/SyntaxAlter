using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public static class JQueryDependencyRemover
{
    /// <summary>
    /// Removes jQuery dependencies and converts common jQuery patterns to vanilla ES6+ JavaScript
    /// </summary>
    /// <param name="javascriptCode">The JavaScript code string to process</param>
    /// <returns>JavaScript code without jQuery dependencies (ES6+ compatible)</returns>
    public static string RemoveJQueryDependencies(string javascriptCode)
    {
        if (string.IsNullOrWhiteSpace(javascriptCode))
            return javascriptCode;

        string result = javascriptCode;

        // 1. Remove jQuery import/require statements
        result = Regex.Replace(result, @"import\s+\$\s+from\s+['""](?:jquery|\$)['""];?", "");
        result = Regex.Replace(result, @"require\s*\(\s*['""](?:jquery|\$)['""]?\s*\);?", "");
        result = Regex.Replace(result, @"require\s*\(\s*['""]jquery['""]\s*\)\s*;?", "");
        // 2. Remove CDN script tags referencing jQuery
        result = Regex.Replace(result, @"<script[^>]*src=['""][^'"">]*jquery[^'"">]*['""][^>]*>\s*</script>", "");

        // 3. Convert common jQuery patterns to vanilla JS
        result = ConvertJQueryToVanilla(result);

        // 4. Clean up multiple consecutive blank lines created by removals
        result = Regex.Replace(result, @"\n\s*\n\s*\n+", "\n\n");

        // 5. Trim leading/trailing whitespace
        result = result.Trim();

        return result;
    }

    private static string ConvertJQueryToVanilla(string code)
    {
        // $(document).ready(fn) -> document.addEventListener('DOMContentLoaded', fn)
        code = Regex.Replace(code, @"\$\(document\)\.ready\s*\(\s*(function\s*\([^)]*\)\s*\{[^}]*\})", 
            m => $"document.addEventListener('DOMContentLoaded', {m.Groups[1].Value})");
        
        code = Regex.Replace(code, @"\$\(document\)\.ready\s*\(\s*(.*?)\s*\);?", 
            "document.addEventListener('DOMContentLoaded', $1);");

        // Convert jQuery to vanilla JS patterns
        code = ProcessSelectorConversions(code);

        return code;
    }

    private static string ProcessSelectorConversions(string code)
    {
        var replacements = new List<(string pattern, string replacement)>
        {
            // $.ajax -> fetch
            (@"(\$\.)?ajax\s*\(", "fetch("),
            
            // .on('event', fn) -> addEventListener
            (@"\.on\s*\(\s*['""]([^'""]+)['""]", ".addEventListener('$1'"),
            
            // .off('event') -> removeEventListener  
            (@"\.off\s*\(\s*['""]([^'""]+)['""]", ".removeEventListener('$1'"),
            
            // .hide() -> style.display = 'none'
            (@"\.hide\s*\(\)", ".style.display = 'none'"),
            
            // .show() -> style.removeProperty or ''
            (@"\.show\s*\(\)", ".style.removeProperty('display')"),
            
            // .css('prop', val) -> style.prop = val
            (@"\.css\s*\(\s*['""]([^'""]+)['""]\s*,\s*['""]([^'""]+)['""]", ".style.$1 = '$2'"),
            
            // .attr('href') -> .getAttribute('href')
            (@"\.attr\s*\(\s*['""]([^'""]+)['""]", ".getAttribute('$1')"),
            
            // .val() -> .value
            (@"\.val\s*\(\)", ".value"),
            
            // .text() -> .textContent
            (@"\.text\s*\(\)", ".textContent"),
            
            // .html() -> .innerHTML
            (@"\.html\s*\(\)", ".innerHTML"),
            
            // .addClass() -> classList.add
            (@"\.addClass\s*\(\s*['""]([^'""]+)['""]", ".classList.add('$1')"),
            
            // .removeClass() -> classList.remove
            (@"\.removeClass\s*\(\s*['""]([^'""]+)['""]", ".classList.remove('$1')"),
            
            // .toggleClass() -> classList.toggle
            (@"\.toggleClass\s*\(\s*['""]([^'""]+)['""]", ".classList.toggle('$1')"),
            
            // .fadeIn() -> animation (complex, simplified)
            (@"\.fadeIn\s*\(\s*[0-9]*\s*\)", ""),
            
            // .fadeOut() 
            (@"\.fadeOut\s*\(\s*[0-9]*\s*\)", ""),
        };

        foreach (var (pattern, replacement) in replacements)
        {
            code = Regex.Replace(code, pattern, replacement);
        }

        return code;
    }

    /// <summary>
    /// Async version for use in async contexts
    /// </summary>
    public static async Task<string> RemoveJQueryDependenciesAsync(string javascriptCode)
    {
        return await Task.Run(() => RemoveJQueryDependencies(javascriptCode));
    }
}
