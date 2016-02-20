using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PawnManager.src
{
    public static class PawnParser
    {
        private abstract class SavElement
        {
            public string Name { get; set; }
        }

        private class SavClassElement : SavElement
        {
            public List<SavElement> Children { get; set; }
        }

        private class SavDataElement : SavElement
        {
            public string Key { get; set; }
        }

        private static List<SavElement> savParseTree = null;

        public static void SetConfig(XElement config)
        {
            XElement savTreeXml = config.Element("sav");
            if (savTreeXml == null)
            {
                throw new System.Xml.XmlException("Config file is missing sav parse tree.");
            }

            savParseTree = new List<SavElement>(ParseChildSavElements(savTreeXml));
        }

        private static IEnumerable<SavElement> ParseChildSavElements(XElement root)
        {
            foreach (XElement child in root.Elements())
            {
                if (child.Name == "class")
                {
                    yield return ParseSavClassElement(child);
                }
                else if (child.Name == "data")
                {
                    yield return ParseSavDataElement(child);
                }
                else
                {
                    throw new System.Xml.XmlException(
                        string.Format("Invalid element in sav parse tree: {0}", child.Name));
                }
            }
        }

        private static SavClassElement ParseSavClassElement(XElement xElement)
        {
            return null;
        }

        private static SavDataElement ParseSavDataElement(XElement xElement)
        {
            return null;
        }

        private static string GetSavElementName(XElement xElement)
        {
            return null;
        }
    }
}
