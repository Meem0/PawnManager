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
    /// The available rom Pawn files that correspond to level scaling
    /// </summary>
    public enum RomSlot
    {
        Rom0,  Rom1,  Rom2,  Rom3,  Rom4,  Rom5,  Rom6,  Rom7,  Rom8,  Rom9,  Rom10,
        Rom11, Rom12, Rom13, Rom14, Rom15, Rom16, Rom17, Rom18, Rom19, Rom20, Rom21,
        Rom22, Rom23, Rom24, Rom25, Rom26, Rom27, Rom28, Rom29, Rom30, Rom31, Rom32
    }
    
    public static class PawnIO
    {
        #region global consts

        public const string PawnFilter = "Pawn file|*.xml|All Files|*.*";
        public const string SavFilter = "DDDA save file|*.xml;*.sav|All Files|*.*";

        #endregion

        #region interface

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
        /// <param name="savFile">The path to the .sav file</param>
        /// <returns>The loaded Pawn, or null if no Pawn was loaded</returns>
        public static Pawn LoadPawnSav(SavSlot savSlot, string savFile)
        {
            XElement savRoot = XElement.Load(savFile);
            XElement savPawn = SavGetPawnEdit(savRoot, savSlot);

            return new Pawn() { EditClass = savPawn };
        }

        /// <summary>
        /// Save a Pawn to the .sav file
        /// </summary>
        /// <param name="pawn">The Pawn to save</param>
        /// <param name="savSlot">The Pawn to save to</param>
        /// <param name="savFile">The path to the .sav file</param>
        public static void SavePawnSav(Pawn pawn, SavSlot savSlot, string savFile)
        {
            XElement savRoot = XElement.Load(savFile);
            XElement savPawn = SavGetPawnEdit(savRoot, savSlot);
            
            savPawn.ReplaceWith(pawn.EditClass);
            savRoot.Save(savFile);
        }

        /// <summary>
        /// Load a Pawn from romNoraPawnData.arc
        /// </summary>
        /// <param name="romSlot">The rom Pawn file to use</param>
        /// <param name="index">The index in the rom Pawn file</param>
        /// <returns>The loaded Pawn</returns>
        public static Pawn LoadPawnRom(RomSlot romSlot, uint index)
        {
            throw new System.NotImplementedException("romPawn not supported");
        }

        /// <summary>
        /// Save a Pawn to romNoraPawnData.arc
        /// </summary>
        /// <param name="pawn">The Pawn to save</param>
        /// <param name="romSlot">The rom Pawn file to use</param>
        /// <param name="index">The index in the rom Pawn file</param>
        public static void SavePawnRom(Pawn pawn, RomSlot romSlot, uint index)
        {
            throw new System.NotImplementedException("romPawn not supported");
        }

        /// <summary>
        /// Check if a file contains a valid unpacked .sav
        /// </summary>
        /// <param name="savPath">The file to validate</param>
        /// <returns>True if the file contains a valid unpacked .sav</returns>
        public static bool ValidateSav(string savPath)
        {
            try
            {
                XElement savRoot = XElement.Load(savPath);
                SavGetPawnClass(savRoot, SavSlot.MainPawn);
            }
            catch (System.Exception)
            {
                return false;
            }

            return true;
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

        public static bool DoesSavContainPawn(string savPath, SavSlot savSlot)
        {
            System.Console.WriteLine("DoesSavContainPawn {0}", savSlot.ToString());
            XElement savRoot = XElement.Load(savPath);
            XElement pawnEdit = SavGetPawnEdit(savRoot, savSlot);
            return GetPawnName(pawnEdit).Length > 0;
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

        #endregion

        #region implementation

        private static XElement SavGetPawnEdit(XElement savRoot, SavSlot savSlot)
        {
            XElement pawnClass = SavGetPawnClass(savRoot, savSlot);
            pawnClass = GetChildWithNameAttribute(pawnClass, "mEdit");
            return pawnClass;
        }

        private static XElement SavGetPawnClass(XElement savRoot, SavSlot savSlot)
        {
            XElement pawnClass = null;
            XElement pawnArray = GetChildWithNameAttribute(savRoot, "mPlayerDataManual");
            pawnArray = GetChildWithNameAttribute(pawnArray, "mPlCmcEditAndParam");
            pawnArray = GetChildWithNameAttribute(pawnArray, "mCmc");
                
            int index = (int)savSlot;
            int currentIndex = 0;
            foreach (XElement child in pawnArray.Elements())
            {
                if (currentIndex++ == index)
                {
                    pawnClass = child;
                    break;
                }
            }

            return pawnClass;
        }

        private static XElement GetChildWithNameAttribute(XElement parent, string nameAttribute)
        {
            foreach (XElement child in parent.Elements())
            {
                if ((string)child.Attribute("name") == nameAttribute)
                    return child;
            }

            throw new System.Xml.XmlException(string.Format(
                "Element named \"{0}\" does not have a child named \"{1}\"",
                nameAttribute,
                (string)parent.Attribute("name")));
        }

        #endregion
    }
}
