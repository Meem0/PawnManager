using System;
using System.Globalization;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml.Linq;
using Microsoft.Win32;
using System.Net;

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

            InitializeConfig();
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
                        XElement newConfig = PendingUpdatedConfig;
                        PendingUpdatedConfig = null;
                        SetConfig(newConfig);
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

        private void SetConfig(XElement config)
        {
            int? version = GetConfigVersion(config);
            configFileVersion = version.HasValue ? version.Value : 0;

            PawnTemplateCategory template = PawnIO.ParseConfig(config);

            pawnModel = new PawnModel(template);
            PawnEditTreeTab.TreeList.Model = pawnModel;
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
            catch { }
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
                savDir = savDir.Substring(0, substrLength);
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
                if (PendingUpdatedConfig != null)
                {
                    XElement newConfig = PendingUpdatedConfig;
                    PendingUpdatedConfig = null;

                    SetConfig(newConfig);
                }
            }
        }
        
        private void butOptionsInfo_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Then help out!  No programming knowledge required!\n\n" +
                "The config.xml file contains the instructions that tell PawnManager what fields to show here, " +
                "and what what constraints to put on the fields, such as the range of values for a slider or " +
                "the list of options for a drop-down.",
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

    public class TestNode
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }

    public class TestInt
    {
        public TestInt(int num)
        {
            Num = num;
        }
        public int Num { get; set; }
    }

    public class TestString
    {
        public TestString(string str)
        {
            Str = str;
        }
        public string Str { get; set; }
    }

    public class TestChar
    {
        public TestChar(char ch)
        {
            Ch = ch;
        }
        public int Ch { get; set; }
    }

}
