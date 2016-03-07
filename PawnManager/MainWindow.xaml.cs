using System;
using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml.Linq;
using Microsoft.Win32;

namespace PawnManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public const string PawnFilter = "Pawn file|*.xml|All Files|*.*";
        public const string SavFilter = "DDDA save file|*.xml;*.sav|All Files|*.*";
        private const string ConfigURL = "https://raw.githubusercontent.com/Meem0/PawnManager/master/PawnManager/config.xml";
        private const string ConfigFileName = "config.xml";

        public SavTab SavTab { get; private set; }

        private XElement pendingUpdatedConfig;
        public XElement PendingUpdatedConfig
        {
            get { return pendingUpdatedConfig; }
            set
            {
                pendingUpdatedConfig = value;
                NotifyPropertyChanged("IsConfigUpdateAvailable");
            }
        }
        public bool IsConfigUpdateAvailable { get { return PendingUpdatedConfig != null; } }
        private int configFileVersion = -1;

        private PawnModel pawnModel = null;
        public PawnModel PawnModel
        {
            get { return pawnModel; }
        }
        private void SetLoadedPawn(PawnData pawnData)
        {
            PawnEditTreeTab.TreeList.Model = null;
            pawnModel.LoadedPawn = pawnData;
            PawnEditTreeTab.TreeList.Model = PawnModel;
        }
        
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            
            SavTab = new SavTab();
            
            try
            {
                InitializeConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(
                    "{0}\n\nPawnManager requires the config file.  Sorry, but I have to close now.",
                    ex.Message),
                    "Error reading config",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
        }

        private void InitializeConfig()
        {
            XElement config = null;
            try
            {
                config = XElement.Load(ConfigFileName);
            }
            catch { }

            if (config != null)
            {
                SetConfig(config);
                (new Thread(CheckForNewConfig)).Start();
            }
            else
            {
                bool success = false;
                MessageBoxResult result =
                    MessageBox.Show(
                        "Could not open config.xml.  Would you like to try to get the latest one from online?",
                        "Error reading config",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Error);

                if (result == MessageBoxResult.Yes)
                {
                    CheckForNewConfig();
                    if (PendingUpdatedConfig != null)
                    {
                        ApplyUpdatedConfig();
                        success = true;
                    }
                }

                if (!success)
                {
                    Application.Current.Shutdown();
                }
            }
        }

        private int? GetConfigVersion(XElement config)
        {
            int? ret = null;
            try
            {
                ret = int.Parse(config.Attribute("version").Value);
            }
            catch { }
            return ret;
        }

        private void ApplyUpdatedConfig()
        {
            if (PendingUpdatedConfig == null)
            {
                return;
            }
            
            XElement newConfig = PendingUpdatedConfig;
            // set PendingUpdatedConfig to null as soon as possible,
            // because there may be race conditions around calling this function twice
            // before PendingUpdatedConfig was set to null...
            PendingUpdatedConfig = null;

            SetConfig(newConfig);
            newConfig.Save(ConfigFileName);
        }

        private void SetConfig(XElement config)
        {
            int? version = GetConfigVersion(config);
            configFileVersion = version.HasValue ? version.Value : 0;

            PawnTemplateCategory template = PawnIO.ParseConfig(config);

            pawnModel = new PawnModel(template);
            PawnEditTreeTab.TreeList.Model = pawnModel;

            NotifyPropertyChanged("PawnModel");
        }

        private void CheckForNewConfig()
        {
            try
            {
                WebClient client = new WebClient();
                string result = client.DownloadString(ConfigURL);

                XElement updateConfig = XElement.Parse(result);
                int? version = GetConfigVersion(updateConfig);
                if (version.HasValue && version.Value > configFileVersion)
                {
                    PendingUpdatedConfig = updateConfig;
                }
            }
            catch (WebException ex)
            {
                throw new WebException(string.Format(
                    "An Internet error occurred:\n{0}",
                    ex.Message),
                    ex);
            }
            catch (Exception ex)
            {
                throw new System.Xml.XmlException("Invalid config file.", ex);
            }
        }
        
        private void butLoad_Click(object sender, RoutedEventArgs e)
        {
            PawnData result = null;
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = PawnFilter;
            openDialog.Title = "Open Pawn file";

            bool? dialogResult = openDialog.ShowDialog();
            if (dialogResult == true)
            {
                try
                {
                    result = PawnIO.LoadPawn(openDialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        ex.Message,
                        "Error loading Pawn",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            if (result != null)
            {
                SetLoadedPawn(result);
            }
        }

        private void butSave_Click(object sender, RoutedEventArgs e)
        {
            if (PawnModel.LoadedPawn != null)
            {
                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.Filter = PawnFilter;
                saveDialog.Title = "Save Pawn file";
                saveDialog.FileName = PawnModel.Name;

                bool? dialogResult = saveDialog.ShowDialog();
                if (dialogResult == true)
                {
                    try
                    {
                        PawnIO.SavePawn(PawnModel.LoadedPawn, saveDialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            ex.Message,
                            "Error loading Pawn",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
        }

        private void butSavImport_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            PawnData result = null;
            try
            {
                result = SavTab.Import();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Error importing from .sav",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            if (result != null)
            {
                SetLoadedPawn(result);
            }
            Cursor = Cursors.Arrow;
        }

        private void butSavExport_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            try
            {
                SavTab.Export(PawnModel.LoadedPawn);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Error exporting to .sav",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            Cursor = Cursors.Arrow;
        }

        private void butSavBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = SavFilter;
            openDialog.Title = "Browse for save file";

            bool? result = openDialog.ShowDialog();
            if (result == true)
            {
                SavTab.SavPath = openDialog.FileName;
            }
        }

        private void butSavLoadDefault_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SavTab.SavPath = SavTab.GetDefaultSavPath();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Error loading default .sav",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void butSavOpenDir_Click(object sender, RoutedEventArgs e)
        {
            string savDir = SavTab.SavPath;
            int substrLength = Math.Max(savDir.LastIndexOf('\\'), savDir.LastIndexOf('/'));
            if (substrLength != -1)
            {
                savDir = savDir.Substring(0, substrLength);
            }
            if (System.IO.Directory.Exists(savDir))
            {
                savDir = savDir.Replace('/', '\\');
                System.Diagnostics.Process.Start("explorer.exe", savDir);
            }
        }

        private void butUpdateConfig_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "This will unload the current Pawn.  Make sure any changes are saved.  Are you sure you want to update?",
                "Update config file?",
                MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                ApplyUpdatedConfig();
            }
        }
        
        private void butOptionsInfo_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Then help out!  No programming knowledge required!\n\n" +
                "The config.xml file determines what you can edit in the Edit tab.  " +
                "It tells PawnManager to display \"eye type\" as a slider with 40 possible values, " +
                "and that \"vocation\" should be a drop-down with the options \"Fighter,\" \"Strider,\" etc.\n\n" +
                "But it's too much work for Meem0 to research the game's ID number for every single skill and inclination!  " +
                "So check out the PawnManager Nexus page to learn what you can do to help!",
                "I want more options!",
                MessageBoxButton.OK);
        }
    }


    public class RadioButtonCheckedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(true) ? parameter : Binding.DoNothing;
        }
    }

    public class BooleanAndConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (object value in values)
            {
                if ((value is bool) && (bool)value == false)
                {
                    return false;
                }
            }
            return true;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("BooleanAndConverter is a OneWay converter.");
        }
    }
}
