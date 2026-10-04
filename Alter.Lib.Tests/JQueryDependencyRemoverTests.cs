using System;
using System.Threading.Tasks;
using Xunit;
namespace Alter.Lib.Tests;

public class JQueryDependencyRemoverTests
{
    [Fact]
    public void RemoveJQueryDependencies_WithNullInput_ReturnsNull()
    {
        var result = JQueryDependencyRemover.RemoveJQueryDependencies(null);
        Assert.Null(result);
    }

    [Fact]
    public void RemoveJQueryDependencies_WithEmptyString_ReturnsEmptyString()
    {
        var result = JQueryDependencyRemover.RemoveJQueryDependencies("");
        Assert.Equal("", result);
    }

    [Fact]
    public void RemoveJQueryDependencies_WithWhitespaceOnly_ReturnsEmptyString()
    {
        string original = "   \n\n  ";
        var result = JQueryDependencyRemover.RemoveJQueryDependencies(original);
        Assert.Equal(original, result);
    }

    [Fact]
    public void RemoveJQueryDependencies_RemovesESModuleImport()
    {
        var input = "import $ from 'jquery';\nconsole.log('test');";
        var result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

        Assert.DoesNotContain("import", result);
        Assert.Contains("console.log('test');", result);
    }

    [Fact]
    public void RemoveJQueryDependencies_RemovesESModuleImportWithDoubleQuotes()
    {
        var input = "import $ from \"jquery\";\nvar x = 1;";
        var result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

        Assert.DoesNotContain("import", result);
        Assert.Contains("var x = 1;", result);
    }

    [Fact]
    public void RemoveJQueryDependencies_RemovesCommonJSRequire()
    {
        var input = "const $ = require('jquery');\nlet data = [];";
        var result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

        Assert.DoesNotContain("require('jquery')", result);
        Assert.Contains("let data = [];", result);
    }

    [Fact]
    public void RemoveJQueryDependencies_RemovesVarRequire()
    {
        var input = "var $ = require('jquery');\nfunction test() {}";
        var result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

        Assert.DoesNotContain("require('jquery')", result);
        Assert.Contains("function test() {}", result);
    }

    [Fact]
    public void RemoveJQueryDependencies_RemovesCDNScriptTag()
    {
        var input = "<script src=\"https://code.jquery.com/jquery-3.6.0.min.js\"></script>\n<h1>Test</h1>";
        var result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

        Assert.DoesNotContain("<script", result);
        Assert.Contains("<h1>Test</h1>", result);
    }

    [Fact]
    public void RemoveJQueryDependencies_RemovesCDNWithSingleQuotes()
    {
        var input = "<script src='https://code.jquery.com/jquery.js'></script>";
        var result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

        Assert.DoesNotContain("<script", result);
    }

    [Fact]
    public void RemoveJQueryDependencies_ConvertsDOMReadyFunction()
    {
        var input = "$(document).ready(function() { console.log('ready'); });";
        var result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

        Assert.Contains("document.addEventListener('DOMContentLoaded'", result);
    }

    [Fact]
    public void RemoveJQueryDependencies_ConvertsAjaxToFetch()
    {
        var input = "$.ajax({ url: '/api/data' });";
        var result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

        Assert.Contains("fetch(", result);
        Assert.DoesNotContain("$.ajax", result);
    }

    [Fact]
    public void RemoveJQueryDependencies_ConvertsOnEventListener()
    {
        var input = "$(document).on('click', handler);";
        var result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

        Assert.Contains(".addEventListener('click'", result);
    }

    [Fact]
    public void RemoveJQueryDependencies_ConvertsOffEventListener()
    {
        var input = "$(document).off('click');";
        var result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

        Assert.Contains(".removeEventListener('click'", result);
    }

    [Fact]
    public void RemoveJQueryDependencies_ConvertsHideToDisplay()
    {
        var input = "$('#element').hide();";
        var result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

        Assert.Contains(".style.display = 'none'", result);
        Assert.DoesNotContain(".hide()", result);
    }

    [Fact]
    public void RemoveJQueryDependencies_ConvertsShowToRemoveProperty()
    {
        var input = "$('#element').show();";
        var result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

        Assert.Contains(".style.removeProperty('display')", result);
    }

    [Fact]
    public void RemoveJQueryDependencies_ConvertsCssMethod()
    {
        var input = "$('.box').css('background', 'red');";
        var result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

        Assert.Contains(".style.background = 'red'", result);
    }

    [Fact]
    public void RemoveJQueryDependencies_ConvertsAttrMethod()
    {
        var input = "$('a').attr('href');";
        var result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

        Assert.Contains(".getAttribute('href')", result);
    }

    [Fact]
    public void RemoveJQueryDependencies_ConvertsValMethod()
    {
        var input = "$('input').val();";
        var result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

        Assert.Contains(".value", result);
        Assert.DoesNotContain(".val()", result);
    }

    [Fact]
    public void RemoveJQueryDependencies_ConvertsTextMethod()
    {
        var input = "$('p').text();";
        var result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

        Assert.Contains(".textContent", result);
    }

    [Fact]
    public void RemoveJQueryDependencies_ConvertsHtmlMethod()
    {
        var input = "$('div').html();";
        var result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

        Assert.Contains(".innerHTML", result);
    }

    [Fact]
    public void RemoveJQueryDependencies_ConvertsAddClass()
    {
        var input = "$('div').addClass('active');";
        var result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

        Assert.Contains(".classList.add('active')", result);
    }

    [Fact]
    public void RemoveJQueryDependencies_ConvertsRemoveClass()
    {
        var input = "$('.item').removeClass('hidden');";
        var result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

        Assert.Contains(".classList.remove('hidden')", result);
    }

    [Fact]
    public void RemoveJQueryDependencies_ConvertsToggleClass()
    {
        var input = "$('#menu').toggleClass('open');";
        var result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

        Assert.Contains(".classList.toggle('open')", result);
    }

    [Fact]
    public void RemoveJQueryDependencies_ConvertsFadeIn()
    {
        var input = "$('#box').fadeIn(300);";
        var result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

        Assert.DoesNotContain(".fadeIn(", result);
    }

    [Fact]
    public void RemoveJQueryDependencies_ConvertsFadeOut()
    {
        var input = "$('#box').fadeOut(300);";
        var result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

        Assert.DoesNotContain(".fadeOut(", result);
    }

    [Fact]
    public void RemoveJQueryDependencies_HandlesComplexRealWorldCode()
    {
        var input = @"
import $ from 'jquery';

$(document).ready(function() {
    $('#submit-btn').on('click', function() {
        const value = $('#input-field').val();
        $.ajax({
            url: '/api/submit',
            data: { value: value }
        });
    });
});";

        var result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

        Assert.DoesNotContain("import", result);
        Assert.DoesNotContain("$.ajax", result);
        Assert.DoesNotContain(".val()", result);
        Assert.Contains("document.addEventListener('DOMContentLoaded'", result);
        Assert.Contains("fetch(", result);
        Assert.Contains(".value", result);
        Assert.Contains(".addEventListener('click'", result);
    }
}