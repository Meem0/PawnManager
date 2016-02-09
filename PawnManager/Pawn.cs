using System.Xml.Linq;

namespace PawnManager
{
    public class Pawn
    {
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
        
        public string Name { get; private set; }

        private void UpdateName()
        {
            if (editClass == null)
            {
                Name = "";
            }
            else
            {
                Name = PawnIO.GetPawnName(EditClass);
            }
        }
    }
}
