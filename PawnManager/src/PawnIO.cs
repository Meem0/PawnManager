using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.Win32;

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
        public const string PawnFilter = "Pawn file|*.pawn|All Files|*.*";
        public const string SavFilter = "DDDA save file|*.xml;*.sav|All Files|*.*";

        private static PawnParser pawnParser;
        
        /// <summary>
        /// Initializes the object using the given config.
        /// Throws an exception if parsing the config fails.
        /// </summary>
        /// <param name="config">
        /// Contains the instructions for parsing the .sav file,
        /// as well as information on how to display Pawn parameters to the user,
        /// such as labels, slider ranges, and drop-down options.
        /// </param>
        public static void SetConfig(XElement config)
        {
            pawnParser = new PawnParser();
            pawnParser.SetConfig(config);
        }

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
                try
                {
                    using (FileStream stream = File.OpenRead(openDialog.FileName))
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
                        ret = (Pawn)formatter.Deserialize(stream);
                    }
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
        public static void SavePawn(Pawn pawn)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = PawnFilter;
            saveDialog.Title = "Save Pawn file";
            saveDialog.FileName = pawn.Name;

            bool? dialogResult = saveDialog.ShowDialog();
            if (dialogResult == true)
            {
                using (FileStream stream = File.OpenWrite(saveDialog.FileName))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, pawn);
                }
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
            XElement pawnSavXElement = SavGetPawn(savRoot, savSlot);
            Pawn ret = pawnParser.LoadPawnFromSav(pawnSavXElement);
            return ret;
        }

        /// <summary>
        /// Save a Pawn to the .sav file
        /// </summary>
        /// <param name="pawn">The Pawn to save</param>
        /// <param name="savSlot">The Pawn to save to</param>
        /// <param name="savRoot">The loaded .sav file</param>
        public static void SavePawnSav(Pawn pawn, SavSlot savSlot, XElement savRoot)
        {
            XElement savPawnXElement = SavGetPawn(savRoot, savSlot);
            pawnParser.ExportPawnToSav(pawn, savPawnXElement);
        }

        private static XElement SavGetPawn(XElement savRoot, SavSlot savSlot)
        {
            XElement pawnClass = null;

            try
            {
                XElement pawnArray = savRoot.GetChildByName("mPlayerDataManual");
                pawnArray = pawnArray.GetChildByName("mPlCmcEditAndParam");
                pawnArray = pawnArray.GetChildByName("mCmc");

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
            }
            catch (Exception ex)
            {
                throw new Exception("Invalid save file.", ex);
            }

            return pawnClass;
        }
    }
}
