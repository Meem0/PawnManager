using System;
using Microsoft.Win32;
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
    /// Contains all the logic that involves reading and writing Pawns to and from files
    /// and requires knowledge of the internals of Pawns.
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
        public static IPawn LoadPawn()
        {
            Pawn ret = null;

            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = PawnFilter;
            openDialog.Title = "Open Pawn file";

            bool? dialogResult = openDialog.ShowDialog();
            if (dialogResult == true)
            {
                ret = new Pawn();
                try
                {
                    ret.EditClass = XElement.Load(openDialog.FileName);
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        string.Format("{0} is not a valid Pawn file.", openDialog.FileName),
                        ex);
                }
            }

            return ret;
        }

        /// <summary>
        /// Save a Pawn to a Pawn file
        /// </summary>
        /// <param name="pawn">The Pawn to save</param>
        public static void SavePawn(IPawn pawn)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = PawnFilter;
            saveDialog.Title = "Save Pawn file";

            bool? dialogResult = saveDialog.ShowDialog();
            if (dialogResult == true)
            {
                Pawn pawnImp = (Pawn)pawn;

                pawnImp.EditClass.SetAttributeValue("version", 1);
                System.IO.File.WriteAllText(saveDialog.FileName, pawnImp.EditClass.ToString());
            }
        }

        /// <summary>
        /// Load a Pawn from the .sav file
        /// </summary>
        /// <param name="savSlot">The Pawn to load</param>
        /// <param name="savRoot">The .sav file</param>
        /// <returns>The loaded Pawn, or null if no Pawn was loaded</returns>
        public static IPawn LoadPawnSav(SavSlot savSlot, XElement savRoot)
        {
            XElement savPawn = SavGetPawnEdit(savRoot, savSlot);
            Pawn ret = new Pawn() { EditClass = savPawn };
            if (ret.Name.Length == 0)
            {
                throw new Exception("The .sav file does not contain a Pawn in that slot.");
            }
            return ret;
        }
        
        /// <summary>
        /// Save a Pawn to the .sav file
        /// </summary>
        /// <param name="pawn">The Pawn to save</param>
        /// <param name="savSlot">The Pawn to save to</param>
        /// <param name="savRoot">The loaded .sav file</param>
        public static void SavePawnSav(IPawn pawn, SavSlot savSlot, ref XElement savRoot)
        {
            XElement savPawn = SavGetPawnEdit(savRoot, savSlot);
            savPawn.ReplaceWith(((Pawn)pawn).EditClass);
        }

        private static XElement SavGetPawnEdit(XElement savRoot, SavSlot savSlot)
        {
            XElement pawnArray = null;

            try
            {
                pawnArray = savRoot.GetChildByName("mPlayerDataManual");
                pawnArray = pawnArray.GetChildByName("mPlCmcEditAndParam");
                pawnArray = pawnArray.GetChildByName("mCmc");
            }
            catch (Exception ex)
            {
                throw new Exception("Invalid save file.", ex);
            }

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
