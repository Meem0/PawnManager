using System;
using System.Xml;
using System.Xml.Linq;

namespace PawnManager
{
    public static class Extensions
    {
        public static XElement GetChildByName(this XElement parent, string nameAttribute)
        {
            foreach (XElement child in parent.Elements())
            {
                if (child.GetNameAttribute() == nameAttribute)
                    return child;
            }

            return null;
        }

        /// <summary>
        /// Returns the contents of the given XElement's 'name' attribute,
        /// or null if it doesn't have one
        /// </summary>
        /// <returns>The XElement's 'name' attribute</returns>
        public static string GetNameAttribute(this XElement xElement)
        {
            XAttribute nameAttribute = xElement.Attribute("name");
            if (nameAttribute == null)
            {
                return null;
            }
            else
            {
                return nameAttribute.Value;
            }
        }

        /// <summary>
        /// Get the 'value' attribute of the given XElement
        /// </summary>
        /// <param name="xElement">The element with the 'value' attribute</param>
        /// <returns>The 'value' attribute</returns>
        public static XAttribute GetValueAttribute(this XElement xElement)
        {
            return xElement.Attribute("value");
        }

        /// <summary>
        /// Reads the 'value' attribute of the given XElement,
        /// and returns its int representation.
        /// Throws an exception if any of that fails.
        /// </summary>
        /// <param name="xElement">The element with the 'value' attribute</param>
        /// <returns>The int value of the 'value' attribute</returns>
        public static Int64 GetParsedValueAttribute(this XElement xElement)
        {
            try
            {
                string valueAttribute = xElement.GetValueAttribute().Value;
                Int64 result;
                if (Int64.TryParse(valueAttribute, out result))
                {
                    return result;
                }
                return (Int64)float.Parse(valueAttribute);
            }
            catch (Exception ex)
            {
                string extraInfo = null;
                string name = xElement.GetNameAttribute();
                if (name != null)
                {
                    extraInfo = string.Format("with name={0}", name);
                }
                else
                {
                    extraInfo = xElement.Name.ToString();
                }
                throw new XmlException(string.Format(
                    "Element {0} does not have a valid value attribute",
                    extraInfo),
                    ex);
            }
        }

        public static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }

        public static int ToInt(this object value, int defaultValue = 0)
        {
            int ret = defaultValue;
            try
            {
                ret = Convert.ToInt32(value);
            }
            catch { }
            return ret;
        }

        public static Int64 ToInt64(this object value, Int64 defaultValue = 0)
        {
            Int64 ret = defaultValue;
            try
            {
                ret = Convert.ToInt64(value);
            }
            catch { }
            return ret;
        }
    }
}
