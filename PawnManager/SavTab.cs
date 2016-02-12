using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
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

        public string SavPath { get; set; } = "";
        
        public SavSlot SavSourcePawn { get; set; } = SavSlot.MainPawn;

        public Pawn Import()
        {
            bool? isPacked;
            XElement savRoot = LoadSav(out isPacked);

            return savRoot == null ? null : PawnIO.LoadPawnSav(SavSourcePawn, savRoot);
        }

        public void Export(Pawn exportPawn)
        {
            bool? isPacked;
            XElement savRoot = LoadSav(out isPacked);

            if (savRoot == null)
            {
                return;
            }

            PawnIO.SavePawnSav(exportPawn, SavSourcePawn, ref savRoot);

            if (isPacked == true)
            {
                // repack
            }
            else if (isPacked == false)
            {
                savRoot.Save(SavPath, SaveOptions.DisableFormatting);
            }
        }
        
        private XElement LoadSav(out bool? isPacked)
        {
            isPacked = null;

            if (SavTool.Validate(SavPath))
            {
                isPacked = true;
                return SavTool.Unpack(SavPath);
            }
            else
            {
                XElement ret = null;

                try
                {
                    ret = XElement.Load(SavPath, LoadOptions.PreserveWhitespace);
                    isPacked = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        string.Format("Error while unpacking .sav:\n{0}", ex.Message),
                        "Error unpacking .sav",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }

                return ret;
            }
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
