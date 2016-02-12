using System;
using System.Xml.Linq;

namespace PawnManager
{
    public static class XmlHelper
    {
        public static XElement GetChildByName(this XElement parent, string nameAttribute)
        {
            foreach (XElement child in parent.Elements())
            {
                if ((string)child.Attribute("name") == nameAttribute)
                    return child;
            }

            return null;
        }
    }
}
