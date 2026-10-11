using Alter.Lib;
using Xunit;

namespace Alter.Lib.Tests;
    public class JQueryDependencyRemoverTests
    {
        [Fact]
        public void RemoveJQueryDependencies_NullInput_ReturnsNull()
        {
            // Arrange
            string input = null;

            // Act
            string result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

            // Assert
            Assert.Null(result);
        }

       

        [Fact]
        public void RemoveJQueryDependencies_EmptyString_ReturnsEmptyString()
        {
            // Arrange
            string input = "";

            // Act
            string result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

            // Assert
            Assert.Empty(result);
        }

        [Theory]
        [InlineData("import $ from 'jquery';")]
        [InlineData("import $ from \"jquery\";")]
        [InlineData("import $ from '$';")]
        public void RemoveJQueryDependencies_RemovesImportStatements(string importStatement)
        {
            // Arrange
            string input = $"{importStatement}\nconsole.log('hello');";

            // Act
            string result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

            // Assert
            Assert.DoesNotContain("$", result);
            Assert.Contains("console.log", result);
        }

        [Theory]
        [InlineData(@"require('jquery');")]
        [InlineData(@"require('$');")]
        [InlineData(@"require('jquery')")]
        public void RemoveJQueryDependencies_RemovesRequireStatements(string requireStatement)
        {
            // Arrange
            string input = $"{requireStatement}\nconsole.log('hello');";

            // Act
            string result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

            // Assert
            Assert.DoesNotContain("require", result);
            Assert.Contains("console.log", result);
        }

        [Fact]
        public void RemoveJQueryDependencies_RemovesCdnScriptTags()
        {
            // Arrange
            string input = @"<script src='https://code.jquery.com/jquery-3.6.0.min.js'></script>
console.log('hello');";

            // Act
            string result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

            // Assert
            Assert.DoesNotContain("<script", result);
            Assert.Contains("console.log", result);
        }

        [Theory]
        [InlineData(
            "$(document).ready(function() { console.log('ready'); });",
            "DOMContentLoaded"
        )]
        public void ConvertJQueryToES6_ConvertsDocumentReady(string input, string expected)
        {
            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains(expected, result);
            Assert.DoesNotContain("$.document", result);
        }

        [Theory]
        [InlineData(
            "$('#myId')",
            "document.getElementById('myId')"
        )]
        [InlineData(
            "$('#button1')",
            "document.getElementById('button1')"
        )]
        public void ConvertJQueryToES6_ConvertsIdSelectors(string input, string expected)
        {
            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains(expected, result);
        }

        [Theory]
        [InlineData(
            "$('.myClass')",
            "document.querySelector('.myClass')"
        )]
        [InlineData(
            "$('.container')",
            "document.querySelector('.container')"
        )]
        public void ConvertJQueryToES6_ConvertsClassSelectors(string input, string expected)
        {
            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains(expected, result);
        }

        [Theory]
        [InlineData(
            "$('div')",
            "document.querySelector('div')"
        )]
        [InlineData(
            "$('span')",
            "document.querySelector('span')"
        )]
        public void ConvertJQueryToES6_ConvertsTagSelectors(string input, string expected)
        {
            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains(expected, result);
        }

        [Fact]
        public void ConvertJQueryToES6_ConvertsAttributeSelectors()
        {
            // Arrange
            string input = "$(\"[data-id='123']\")";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains("querySelector", result);
            Assert.DoesNotContain("$(", result);
        }

        [Fact]
        public void ConvertJQueryToES6_ConvertsClickHandlers()
        {
            // Arrange
            string input = "$('#btn').click(function() { alert('clicked'); });";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains("addEventListener", result);
            Assert.DoesNotContain(".click(", result);
        }

        [Fact]
        public void ConvertJQueryToES6_ConvertsSubmitHandlers()
        {
            // Arrange
            string input = "$('#form').submit(function() { return false; });";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains("addEventListener", result);
            Assert.DoesNotContain(".submit(", result);
        }

        [Fact]
        public void ConvertJQueryToES6_ConvertsChangeHandlers()
        {
            // Arrange
            string input = "$('#input').change(function() { console.log('changed'); });";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains("addEventListener", result);
            Assert.DoesNotContain(".change(", result);
        }

        [Fact]
        public void ConvertJQueryToES6_ConvertsOnHandlers()
        {
            // Arrange
            string input = "$('#elem').on('click', function() {});";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains("addEventListener", result);
            Assert.DoesNotContain(".on('click'", result);
        }

        [Fact]
        public void ConvertJQueryToES6_ConvertsHoverHandlers()
        {
            // Arrange
            string input = "$('#elem').hover(function() { enter(); }, function() { leave(); });";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains("mouseenter", result);
            Assert.Contains("mouseleave", result);
            Assert.DoesNotContain(".hover(", result);
        }

        [Fact]
        public void ConvertJQueryToES6_ConvertsOffHandlers()
        {
            // Arrange
            string input = "$('#elem').off('click');";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains("removeEventListener", result);
            Assert.DoesNotContain(".off(", result);
        }

        [Fact]
        public void ConvertJQueryToES6_ConvertsCssSetters()
        {
            // Arrange
            string input = "$('#elem').css('color', 'red');";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains(".style.color = 'red'", result);
            Assert.DoesNotContain(".css(", result);
        }

        [Fact]
        public void ConvertJQueryToES6_ConvertsCssCamelCaseProperties()
        {
            // Arrange
            string input = "$('#elem').css('background-color', 'blue');";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains(".style.backgroundColor = 'blue'", result);
        }

        [Fact]
        public void ConvertJQueryToES6_ConvertsCssObject()
        {
            // Arrange
            string input = "$('#elem').css({ 'color': 'red', 'font-size': '14px' });";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains(".style.color", result);
            Assert.DoesNotContain(".css(", result);
        }

        [Fact]
        public void ConvertJQueryToES6_ConvertsAddClass()
        {
            // Arrange
            string input = "$('#elem').addClass('active');";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains("classList.add", result);
            Assert.DoesNotContain(".addClass(", result);
        }

        [Fact]
        public void ConvertJQueryToES6_ConvertsRemoveClass()
        {
            // Arrange
            string input = "$('#elem').removeClass('hidden');";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains("classList.remove", result);
            Assert.DoesNotContain(".removeClass(", result);
        }

        [Fact]
        public void ConvertJQueryToES6_ConvertsToggleClass()
        {
            // Arrange
            string input = "$('#elem').toggleClass('selected');";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains("classList.toggle", result);
            Assert.DoesNotContain(".toggleClass(", result);
        }

        [Fact]
        public void ConvertJQueryToES6_ConvertsTextSetter()
        {
            // Arrange
            string input = "$('#elem').text('Hello');";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains("textContent =", result);
            Assert.DoesNotContain(".text(", result);
        }

        [Fact]
        public void ConvertJQueryToES6_ConvertsHtmlSetter()
        {
            // Arrange
            string input = "$('#elem').html('<p>Hello</p>');";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains("innerHTML =", result);
            Assert.DoesNotContain(".html(", result);
        }

        [Fact]
        public void ConvertJQueryToES6_ConvertsAppend()
        {
            // Arrange
            string input = "$('#elem').append('<span>extra</span>');";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains("insertAdjacentHTML", result);
            Assert.DoesNotContain(".append(", result);
        }

        [Fact]
        public void ConvertJQueryToES6_ConvertsValGetter()
        {
            // Arrange
            string input = "$('#input').val();";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains(".value", result);
            Assert.DoesNotContain(".val(", result);
        }

        [Fact]
        public void ConvertJQueryToES6_ConvertsValSetter()
        {
            // Arrange
            string input = "$('#input').val('test');";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains(".value =", result);
            Assert.DoesNotContain(".val(", result);
        }

        [Fact]
        public void ConvertJQueryToES6_ConvertsShow()
        {
            // Arrange
            string input = "$('#elem').show();";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains(".style.display = ''", result);
            Assert.DoesNotContain(".show(", result);
        }

        [Fact]
        public void ConvertJQueryToES6_ConvertsHide()
        {
            // Arrange
            string input = "$('#elem').hide();";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains(".style.display = 'none'", result);
            Assert.DoesNotContain(".hide(", result);
        }

        [Fact]
        public void ConvertJQueryToES6_ConvertsFadeIn()
        {
            // Arrange
            string input = "$('#elem').fadeIn(200);";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains(".style.display = ''", result);
            Assert.DoesNotContain(".fadeIn(", result);
        }

        [Fact]
        public void ConvertJQueryToES6_ConvertsFadeOut()
        {
            // Arrange
            string input = "$('#elem').fadeOut(300);";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains(".style.display = 'none'", result);
            Assert.DoesNotContain(".fadeOut(", result);
        }

        [Fact]
        public void ConvertJQueryToES6_ConvertsEmpty()
        {
            // Arrange
            string input = "$('#elem').empty();";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains(".innerHTML = ''", result);
            Assert.DoesNotContain(".empty(", result);
        }

        [Fact]
        public void ConvertJQueryToES6_ConvertsRemove()
        {
            // Arrange
            string input = "$('#elem').remove();";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert — .remove() is a native DOM API, so it stays;
            // only verify the selector was converted
            Assert.Contains("document.getElementById('elem')", result);
            Assert.Contains(".remove()", result);
        }
        [Fact]
        public void ConvertJQueryToES6_AjaxGet_ConvertsToFetch()
        {
            // Arrange
            string input = "$.get('/api/data', function(data) { console.log(data); });";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains("fetch", result);
            Assert.Contains("'/api/data'", result);
            Assert.DoesNotContain("$.get", result);
        }

        [Fact]
        public void ConvertJQueryToES6_AjaxPost_ConvertsToFetch()
        {
            // Arrange
            string input = "$.post('/api/submit', { name: 'test' }, function(response) { console.log(response); });";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains("fetch", result);
            Assert.Contains("method: 'POST'", result);
            Assert.DoesNotContain("$.post", result);
        }

        [Fact]
        public void ConvertJQueryToES6_AjaxJson_GetConvertsToFetch()
        {
            // Arrange
            string input = "$.getJSON('/api/users', function(users) { renderUsers(users); });";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains("fetch", result);
            Assert.Contains("'/api/users'", result);
            Assert.DoesNotContain("$.getJSON", result);
        }

        [Fact]
        public void ConvertJQueryToES6_ComplexAjax_ConvertsToFetchWithHandlers()
        {
            // Arrange
            string input = @"$.ajax({
    url: '/api/items',
    type: 'PUT',
    data: { id: 123 },
    success: function(result) { updateItem(result); },
    error: function(xhr, status, error) { handleError(error); }
});";

            // Act
            string result = JQueryDependencyRemover.ConvertJQueryToES6(input);

            // Assert
            Assert.Contains("fetch", result);
            Assert.Contains("'/api/items'", result);
            Assert.Contains("method: 'PUT'", result);
            Assert.Contains("handleError", result);
            Assert.DoesNotContain("$.ajax", result);
        }

        [Fact]
        public void ConvertJQueryToES6_IntegrationTest_RealWorldCode()
        {
            // Arrange
            string input = @"import $ from 'jquery';

$(document).ready(function() {
    $('#submitBtn').click(function(e) {
        e.preventDefault();
        var formData = $('#myForm').serialize();
        
        $.post('/api/save', formData, function(response) {
            if (response.success) {
                $('#message').addClass('success').text('Saved!');
                $('#loading').hide();
            }
        });
    });
    
    $('.close-btn').on('click', function() {
        $(this).parent().remove();
    });
});";

            // Act
            string result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

            // Assert
            Assert.DoesNotContain("import $", result);
            Assert.DoesNotContain("$.ajax", result);
            Assert.DoesNotContain("$.post", result);
            Assert.DoesNotContain("$.click", result);
            Assert.Contains("addEventListener", result);
            Assert.Contains("fetch", result);
            Assert.Contains("getElementById", result);
            Assert.Contains("querySelector", result);
            Assert.Contains("DOMContentLoaded", result);
        }

        [Fact]
        public async Task RemoveJQueryDependenciesAsync_ProcessesCorrectly()
        {
            // Arrange
            string input = "import $ from 'jquery'; $('#test').click(function() {});";

            // Act
            string result = await JQueryDependencyRemover.RemoveJQueryDependenciesAsync(input);

            // Assert
            Assert.DoesNotContain("import $", result);
            Assert.Contains("addEventListener", result);
        }

        [Fact]
        public void RemoveJQueryDependencies_MultipleBlankLines_CleansUp()
        {
            // Arrange
            string input = @"

console.log('test');


";

            // Act
            string result = JQueryDependencyRemover.RemoveJQueryDependencies(input);

            // Assert
            Assert.DoesNotContain("\n\n\n\n", result);
            Assert.Equal(result.Trim(), result);
        }
    }
