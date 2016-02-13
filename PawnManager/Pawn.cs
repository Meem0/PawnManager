using System.Xml.Linq;

namespace PawnManager
{
    public class Pawn : IPawn
    {
        public string Name { get; private set; }

        private XElement editClass;
        public XElement EditClass
        {
            get { return editClass; }
            set
            {
                editClass = value;
                UpdateName();
            }
        }
        
        private void UpdateName()
        {
            if (editClass == null)
            {
                Name = "";
            }
            else
            {
                Name = GetPawnName(EditClass);
            }
        }

        private static string GetPawnName(XElement pawnEdit)
        {
            char[] nameArray = new char[25];
            XElement nameArrayElement = pawnEdit.Element("array");
            int index = 0;
            foreach (XElement letter in nameArrayElement.Elements())
            {
                nameArray[index] = (char)int.Parse(letter.Attribute("value").Value);
                if (nameArray[index] == '\0')
                    break;
                ++index;
            }
            return new string(nameArray, 0, index);
        }
    }
}
