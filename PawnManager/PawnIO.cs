using Microsoft.Win32;
using System.Windows;
using System.Xml.Linq;

namespace PawnManager
{
    /// <summary>
    /// The three Pawns that follow the player
    /// </summary>
    public enum SavSlot
    {
        MainPawn,
        Pawn1,
        Pawn2
    }
    
    /// <summary>
    /// Contains the logic of saving and loading Pawns to and from various sources
    /// </summary>
    public static class PawnIO
    {
        public const string PawnFilter = "Pawn file|*.xml|All Files|*.*";
        public const string SavFilter = "DDDA save file|*.xml;*.sav|All Files|*.*";
        
        /// <summary>
        /// Load a Pawn from a Pawn file
        /// </summary>
        /// <param name="pawnFilePath">The path to the Pawn file</param>
        /// <returns>The loaded Pawn</returns>
        public static Pawn LoadPawn()
        {
            Pawn ret = null;

            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = PawnFilter;
            openDialog.Title = "Open Pawn file";

            bool? dialogResult = openDialog.ShowDialog();
            if (dialogResult == true)
            {
                ret = new Pawn();
                ret.EditClass = XElement.Load(openDialog.FileName);
            }

            return ret;
        }

        /// <summary>
        /// Save a Pawn to a Pawn file
        /// </summary>
        /// <param name="pawn">The Pawn to save</param>
        public static void SavePawn(Pawn pawn)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = PawnFilter;
            saveDialog.Title = "Save Pawn file";

            bool? dialogResult = saveDialog.ShowDialog();
            if (dialogResult == true)
            {
                pawn.EditClass.SetAttributeValue("version", 1);
                System.IO.File.WriteAllText(saveDialog.FileName, pawn.EditClass.ToString());
            }
        }

        /// <summary>
        /// Load a Pawn from the .sav file
        /// </summary>
        /// <param name="savSlot">The Pawn to load</param>
        /// <param name="savRoot">The .sav file</param>
        /// <returns>The loaded Pawn, or null if no Pawn was loaded</returns>
        public static Pawn LoadPawnSav(SavSlot savSlot, XElement savRoot)
        {
            XElement savPawn = SavGetPawnEdit(savRoot, savSlot);
            return new Pawn() { EditClass = savPawn };
        }
        
        /// <summary>
        /// Save a Pawn to the .sav file
        /// </summary>
        /// <param name="pawn">The Pawn to save</param>
        /// <param name="savSlot">The Pawn to save to</param>
        /// <param name="savRoot">The loaded .sav file</param>
        public static void SavePawnSav(Pawn pawn, SavSlot savSlot, ref XElement savRoot)
        {
            XElement savPawn = SavGetPawnEdit(savRoot, savSlot);
            savPawn.ReplaceWith(pawn.EditClass);
        }
        
        /// <summary>
        /// Check if a file is a valid Pawn file
        /// </summary>
        /// <param name="savPath">The file to validate</param>
        /// <returns>True if the file is a valid Pawn file</returns>
        public static bool ValidatePawnFile(string pawnPath)
        {
            try
            {
                XElement pawnRoot = XElement.Load(pawnPath);
                if (pawnRoot.Attribute("mEdit") == null)
                {
                    return false;
                }
            }
            catch (System.Exception)
            {
                return false;
            }

            return true;
        }
        
        public static string GetPawnName(XElement pawnEdit)
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

        private static XElement SavGetPawnEdit(XElement savRoot, SavSlot savSlot)
        {
            XElement pawnArray = savRoot.GetChildByName("mPlayerDataManual");
            pawnArray = pawnArray.GetChildByName("mPlCmcEditAndParam");
            pawnArray = pawnArray.GetChildByName("mCmc");

            int index = (int)savSlot;
            int currentIndex = 0;
            XElement pawnClass = null;
            foreach (XElement child in pawnArray.Elements())
            {
                if (currentIndex++ == index)
                {
                    pawnClass = child;
                    break;
                }
            }

            pawnClass = pawnClass.GetChildByName("mEdit");
            return pawnClass;
        }
    }
}
