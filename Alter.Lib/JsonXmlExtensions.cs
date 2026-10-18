using System;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Alter.Lib;

public static class JsonXmlExtensions
{
    /// <summary>
    /// Converts a System.Text.Json JsonElement directly into an XElement.
    /// </summary>
    public static XElement ToXElement(this JsonElement element, string rootName = "Root")
    {
        return ConvertJsonToXml(element, rootName);
    }

    /// <summary>
    /// Converts an XElement into a JSON string using System.Text.Json.
    /// </summary>
    public static string ToJsonString(this XElement xmlElement, JsonSerializerOptions options = null)
    {
        using var stream = new System.IO.MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = options?.WriteIndented ?? false }))
        {
            ConvertXmlToJson(xmlElement, writer);
        }
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static XElement ConvertJsonToXml(JsonElement element, string name)
    {
        XElement xml = new XElement(name);

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    // Handle attribute markers (matching conventional converters like Newtonsoft)
                    if (property.Name.StartsWith("@"))
                    {
                        xml.SetAttributeValue(property.Name.TrimStart('@'), property.Value.ToString());
                    }
                    // Handle text blocks within objects
                    else if (property.Name == "#text")
                    {
                        xml.Value = property.Value.ToString();
                    }
                    else if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        // Arrays repeat the property key name for each element in XML
                        foreach (var arrayItem in property.Value.EnumerateArray())
                        {
                            xml.Add(ConvertJsonToXml(arrayItem, property.Name));
                        }
                    }
                    else
                    {
                        xml.Add(ConvertJsonToXml(property.Value, property.Name));
                    }
                }
                break;

            case JsonValueKind.Array:
                // Raw top-level arrays use a default repeated item element name
                foreach (var item in element.EnumerateArray())
                {
                    xml.Add(ConvertJsonToXml(item, "Item"));
                }
                break;

            default:
                xml.Value = element.ToString();
                break;
        }

        return xml;
    }
    public static string ConvertXmlToJson(this string xmlString)
    {
        // Parse to XDocument (preferred over XmlDocument for modern .NET LINQ parsing)
        XElement root = XElement.Parse(xmlString);
    
        // Parse elements to a dictionary structure
        var dictionary = ParseXmlElement(root);

        // Serialize natively using System.Text.Json
        return JsonSerializer.Serialize(dictionary, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
    }

    private static Dictionary<string, object> ParseXmlElement(XElement element)
    {
        var dict = new Dictionary<string, object>();

        // Process Attributes (prefixed with '@' to follow standard XML-JSON conventions)
        foreach (var attribute in element.Attributes())
        {
            dict[$"@{attribute.Name.LocalName}"] = attribute.Value;
        }

        // Process Child Elements
        if (element.HasElements)
        {
            var elementGroups = element.Elements().GroupBy(e => e.Name.LocalName);
            foreach (var group in elementGroups)
            {
                // If there's more than one element with the same name, make it a JSON list
                if (group.Count() > 1)
                {
                    dict[group.Key] = group.Select(ParseXmlElement).ToList();
                }
                else
                {
                    var child = group.First();
                    dict[group.Key] = child.HasElements || child.Attributes().Any() 
                        ? ParseXmlElement(child) 
                        : child.Value;
                }
            }
        }
        else if (!string.IsNullOrEmpty(element.Value))
        {
            dict["#text"] = element.Value;
        }

        return dict;
    }
    private static void ConvertXmlToJson(XElement xml, Utf8JsonWriter writer)
    {
        writer.WriteStartObject();

        // Write Attributes as properties prefixed with '@'
        foreach (var attribute in xml.Attributes())
        {
            writer.WriteString($"@{attribute.Name.LocalName}", attribute.Value);
        }

        // Group sibling elements to check for repeated nodes (which form JSON arrays)
        var elementGroups = xml.Elements().GroupBy(e => e.Name.LocalName);

        foreach (var group in elementGroups)
        {
            if (group.Count() > 1)
            {
                // Convert matching sibling elements into a JSON Array
                writer.WriteStartArray(group.Key);
                foreach (var element in group)
                {
                    if (!element.HasElements && !element.HasAttributes)
                    {
                        writer.WriteStringValue(element.Value);
                    }
                    else
                    {
                        ConvertXmlToJson(element, writer);
                    }
                }
                writer.WriteEndArray();
            }
            else
            {
                // Single element conversion
                var element = group.First();
                if (!element.HasElements && !element.HasAttributes)
                {
                    writer.WriteString(element.Name.LocalName, element.Value);
                }
                else
                {
                    writer.WritePropertyName(element.Name.LocalName);
                    ConvertXmlToJson(element, writer);
                }
            }
        }

        // Write inner text node if elements/attributes exist beside a text value
        if (!xml.HasElements && !string.IsNullOrEmpty(xml.Value) && xml.HasAttributes)
        {
            writer.WriteString("#text", xml.Value);
        }

        writer.WriteEndObject();
    }
}
