using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alter.Lib;
using Alter.Lib.Syntax;

namespace SyntaxAlter.components;

public partial class JsonXmlView : ContentView
{
    public JsonXmlView()
    {
        InitializeComponent();
    }

    private void OnConvertToJsonClicked(object? sender, EventArgs e)
    {
        try
        {
            ISyntaxAlterer alterer = new XmlToJsonAlterer();
            JsonToXml.Text = alterer.AlterSyntax(JsonToXml.Text);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Parse Error: {ex.Message}");
            JsonToXml.Text = $"Error: {ex.Message}";
        }
    }

    private void OnConvertToXmlClicked(object? sender, EventArgs e)
    {
        
    }
}