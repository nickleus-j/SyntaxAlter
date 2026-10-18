using Alter.Lib;
namespace Alter.Lib.Syntax;

public class XmlToJsonAlterer: ISyntaxAlterer
{
    public string AlterSyntax(string code)
    {
        return code.ConvertXmlToJson();
    }
}