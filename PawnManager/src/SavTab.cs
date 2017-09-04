using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.Xml.Linq;
using System.Xml;
using System.Text;

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
                value = value.Replace("\"", "");
                savPath = value;
                NotifyPropertyChanged();
                try
                {
                    IsSavPathValidFile = File.Exists(savPath);
                }
                catch { }
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
        public PawnData Import()
        {
            bool? isPacked;
            XElement savRoot = LoadSav(out isPacked);

            return PawnIO.LoadPawnSav(SavSourcePawn, savRoot);
        }

        private string EncodeXml(XElement xml)
        {
            string ret = "";
            using (MemoryStream memoryStream = new MemoryStream())
            {
                UTF8Encoding encoding = new UTF8Encoding(false);
                using (StreamWriter streamWriter = new StreamWriter(memoryStream, encoding))
                {
                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.NewLineChars = "\n";
                    using (XmlWriter xmlWriter = XmlWriter.Create(streamWriter, settings))
                    {
                        xmlWriter.WriteStartDocument();
                        xmlWriter.WriteWhitespace("\n");
                        xml.WriteTo(xmlWriter);
                        xmlWriter.WriteWhitespace("\n");
                    }

                    byte[] buffer = new byte[memoryStream.Length];
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    memoryStream.Read(buffer, 0, (int)memoryStream.Length);

                    string replaceStr = encoding.GetString(buffer, 0, (int)memoryStream.Length);
                    ret = replaceStr.Replace(" />", "/>");
                }
            }
            return ret;
        }

        /// <summary>
        /// Loads the .sav file specified by SavPath, using DDsavelib if it is packed,
        /// replaces the Pawn in the slot specified by SavSourcePawn with the given Pawn,
        /// then writes the modified .sav back, repacking it using DDsavelib if it was originally packed.
        /// Throws an exception if anything fails.
        /// </summary>
        /// <param name="exportPawn">The Pawn to export to the .sav file</param>
        public void Export(PawnData exportPawn)
        {
            bool? isPacked;
            XElement savRoot = LoadSav(out isPacked);

            PawnIO.SavePawnSav(exportPawn, SavSourcePawn, savRoot);

            string encoded = EncodeXml(savRoot);

            if (isPacked == true)
            {
                SavTool.RepackSav(SavPath, encoded);
            }
            else if (isPacked == false)
            {
                File.WriteAllText(SavPath, encoded, new UTF8Encoding(false));
            }
        }
        
        private XElement LoadSav(out bool? isPacked)
        {
            if (!File.Exists(SavPath))
            {
                throw new Exception(string.Format("File {0} does not exist", SavPath));
            }

            isPacked = null;

            if (SavTool.ValidateSav(SavPath))
            {
                isPacked = true;
                string unpackedText = SavTool.UnpackSav(SavPath);
                return XElement.Parse(unpackedText, LoadOptions.PreserveWhitespace);
            }
            else
            {
                isPacked = false;
                return XElement.Load(SavPath, LoadOptions.PreserveWhitespace);
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
