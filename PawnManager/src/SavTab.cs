using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.Xml.Linq;

namespace PawnManager
{
    public class SavTab : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private string savPath = "";
        public string SavPath
        {
            get { return savPath; }
            set
            {
                savPath = value;
                NotifyPropertyChanged();
                IsSavPathValidFile = File.Exists(savPath);
            }
        }

        private bool isSavPathValidFile;
        public bool IsSavPathValidFile
        {
            get { return isSavPathValidFile; }
            private set
            {
                isSavPathValidFile = value;
                NotifyPropertyChanged();
            }
        }
        
        public SavSlot SavSourcePawn { get; set; } = SavSlot.MainPawn;

        /// <summary>
        /// Loads the .sav file specified by SavPath, using DDsavelib if it is packed,
        /// and returns the Pawn in the slot specified by SavSourcePawn.
        /// Throws an exception if anything fails.
        /// </summary>
        /// <returns>The loaded Pawn</returns>
        public IPawn Import()
        {
            bool? isPacked;
            string savText = LoadSav(out isPacked);
            XElement savRoot = XElement.Parse(savText);

            return PawnIO.LoadPawnSav(SavSourcePawn, savRoot);
        }

        /// <summary>
        /// Loads the .sav file specified by SavPath, using DDsavelib if it is packed,
        /// replaces the Pawn in the slot specified by SavSourcePawn with the given Pawn,
        /// then writes the modified .sav back, repacking it using DDsavelib if it was originally packed.
        /// Throws an exception if anything fails.
        /// </summary>
        /// <param name="exportPawn">The Pawn to export to the .sav file</param>
        public void Export(IPawn exportPawn)
        {
            bool? isPacked;
            string savText = LoadSav(out isPacked);
            XElement savRoot = XElement.Parse(savText, LoadOptions.PreserveWhitespace);

            PawnIO.SavePawnSav(exportPawn, SavSourcePawn, ref savRoot);

            if (isPacked == true)
            {
                string savTextEdited = savRoot.ToString(SaveOptions.DisableFormatting);
                SavTool.RepackSav(SavPath, savTextEdited);
            }
            else if (isPacked == false)
            {
                savRoot.Save(SavPath, SaveOptions.DisableFormatting);
            }
        }
        
        private string LoadSav(out bool? isPacked)
        {
            if (!File.Exists(SavPath))
            {
                throw new Exception(string.Format("File {0} does not exist", SavPath));
            }

            isPacked = null;

            if (SavTool.ValidateSav(SavPath))
            {
                isPacked = true;
                return SavTool.UnpackSav(SavPath);
            }
            else
            {
                string unpackedText = File.ReadAllText(SavPath);
                isPacked = false;
                return unpackedText;
            }
        }
        
        private const string DDDAID = "367500";
        /// <summary>
        /// Get the path to DDDA.sav that the game uses.
        /// Throws an exception if it can't be found.
        /// </summary>
        /// <returns>The path to DDDA.sav</returns>
        public string GetDefaultSavPath()
        {
            // from http://forums.steampowered.com/forums/showthread.php?t=2208578
            
            Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.CurrentUser;
            if (regKey == null)
            {
                throw new Exception("Could not get CurrentUser RegKey");
            }

            regKey = regKey.OpenSubKey(@"Software\Valve\Steam");
            if (regKey == null)
            {
                throw new Exception("Could not open Steam RegKey");
            }
            
            object regKeyValue = regKey.GetValue("SteamPath");
            if (regKeyValue == null)
            {
                throw new Exception("Could not get SteamPath RegKey value");
            }

            string searchRootPath = regKeyValue.ToString() + "/userdata/";
            if (!Directory.Exists(searchRootPath))
            {
                throw new Exception(string.Format("Could not find directory {0}", searchRootPath));
            }

            string savDir = null;
            foreach (string dir in Directory.EnumerateDirectories(searchRootPath))
            {
                string trySavDir = string.Format("{0}/{1}", dir, DDDAID);
                if (Directory.Exists(trySavDir))
                {
                    savDir = trySavDir;
                    break;
                }
            }

            if (savDir == null)
            {
                throw new Exception(string.Format("Could not find directory {0}", DDDAID));
            }

            string savPath = savDir + "/remote/DDDA.sav";
            if (!File.Exists(savPath))
            {
                throw new Exception(string.Format("File {0} does not exist", savPath));
            }

            // capitalize the damn drive letter
            if (savPath.Length > 0)
            {
                var sb = new System.Text.StringBuilder(savPath);
                sb[0] = char.ToUpper(sb[0]);
                sb.Replace('/', '\\');
                savPath = sb.ToString();
            }

            return savPath;
        }
    }
}
