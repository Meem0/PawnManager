using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.IO;

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
            set { ValidateAndSetSav(value, true); }
        }

        /// <summary>
        /// Determines if a file is a valid .sav file, and whether it is packed or unpacked.
        /// Sets SavPath if the input is valid, or if setIfInvalid is true.
        /// </summary>
        /// <param name="savPath">The path to the .sav file</param>
        /// <param name="setIfInvalid">If true, sets SavPath even if the file is invalid</param>
        /// <returns>True if SavPath was updated</returns>
        public bool ValidateAndSetSav(string savPath, bool setIfInvalid)
        {
            savPath = savPath.Replace("\"", "");

            bool validPacked = false;
            bool validUnpacked = false;

            if (PawnIO.ValidateSav(savPath))
            {
                validUnpacked = true;
            }
            else if (SavTool.ValidateSav(savPath) == null)
            {
                validPacked = true;
            }
            
            if (setIfInvalid || validPacked || validUnpacked)
            {
                this.savPath = savPath;

                bool validBefore = IsValidSav;
                
                IsValidPackedSav = validPacked;
                IsValidUnpackedSav = validUnpacked;

                NotifyPropertyChanged("SavPath");

                if (validBefore != IsValidSav)
                {
                    NotifyPropertyChanged("IsValidSav");
                }
                
                return IsValidSav;
            }
            else return false;
        }

        private bool isValidPackedSav;
        public bool IsValidPackedSav
        {
            get { return isValidPackedSav; }
            private set
            {
                if (value != isValidPackedSav)
                {
                    isValidPackedSav = value;
                    NotifyPropertyChanged();
                }
            }
        }

        private bool isValidUnpackedSav;
        public bool IsValidUnpackedSav
        {
            get { return isValidUnpackedSav; }
            private set
            {
                if (value != isValidUnpackedSav)
                {
                    isValidUnpackedSav = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool IsValidSav { get { return IsValidPackedSav || IsValidUnpackedSav; } }
        
        public SavSlot SavSourcePawn { get; set; } = SavSlot.MainPawn;

        public Pawn Import()
        {
            string extractedSavPath = SavPath;
            if (IsValidPackedSav)
            {
                if (!SavTool.UnpackSav(SavPath))
                {
                    return null;
                }
                extractedSavPath += ".xml";
            }

            if (!PawnIO.DoesSavContainPawn(extractedSavPath, SavSourcePawn))
            {
                MessageBox.Show(
                    "The selected Pawn is not present in the save file.",
                    "Error importing Pawn",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return null;
            }

            return PawnIO.LoadPawnSav(SavSourcePawn, extractedSavPath);
        }

        public void Export(Pawn exportPawn)
        {
            if (exportPawn != null)
            {
                string extractedSavPath = SavPath;
                if (IsValidPackedSav)
                {
                    if (!SavTool.UnpackSav(SavPath))
                    {
                        return;
                    }
                    extractedSavPath += ".xml";
                }

                if (!PawnIO.DoesSavContainPawn(extractedSavPath, SavSourcePawn))
                {
                    MessageBox.Show(
                        "Exporting to a Pawn slot you don't have yet will crash your game.  I'll see if it's possible to fix this later.",
                        "Error exporting Pawn",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                PawnIO.SavePawnSav(exportPawn, SavSourcePawn, extractedSavPath);
                if (IsValidPackedSav)
                {
                    SavTool.RepackSav(extractedSavPath);
                }
            }
        }

        public void Unpack()
        {
            SavTool.UnpackSav(SavPath);
        }

        public void Repack()
        {
            SavTool.RepackSav(SavPath);
        }

        private const string DDDAID = "367500";
        private bool triedAlready = false;
        private string cachedSavPath = null;
        public string GetDefaultSavPath()
        {
            if (cachedSavPath != null)
                return cachedSavPath;
            if (triedAlready)
                return null;

            // from http://forums.steampowered.com/forums/showthread.php?t=2208578
            
            Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.CurrentUser;

            if (regKey == null)
            {
                triedAlready = true;
                return null;
            }

            regKey = regKey.OpenSubKey(@"Software\Valve\Steam");
            
            if (regKey == null)
            {
                triedAlready = true;
                return null;
            }
            
            object regKeyValue = regKey.GetValue("SteamPath");
            if (regKeyValue == null)
            {
                triedAlready = true;
                return null;
            }

            string searchRootPath = regKeyValue.ToString() + "/userdata/";

            if (!Directory.Exists(searchRootPath))
            {
                triedAlready = true;
                return null;
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
                triedAlready = true;
                return null;
            }

            cachedSavPath = savDir + "/remote/DDDA.sav";
            return cachedSavPath;
        }
    }
}
