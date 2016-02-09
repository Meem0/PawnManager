using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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

        public SavTab SavTab { get; private set; }

        private Pawn loadedPawn = null;
        public Pawn LoadedPawn
        {
            get { return loadedPawn; }
            set
            {
                if (value != loadedPawn)
                {
                    loadedPawn = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged("IsPawnLoaded");
                    NotifyPropertyChanged("IsPawnLoadedAndSavValid");
                }
            }
        }

        public bool IsPawnLoaded { get { return LoadedPawn != null; } }

        public bool IsPawnLoadedAndSavValid { get { return IsPawnLoaded && SavTab.IsValidSav; } }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            SavTab = new SavTab();
            SavTab.PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                if (e.PropertyName == "IsValidSav")
                {
                    NotifyPropertyChanged("IsPawnLoadedAndSavValid");
                }
            };
        }

        private void butLoad_Click(object sender, RoutedEventArgs e)
        {
            Pawn result = PawnIO.LoadPawn();
            if (result != null)
            {
                LoadedPawn = result;
            }
        }

        private void butSave_Click(object sender, RoutedEventArgs e)
        {
            if (LoadedPawn != null)
            {
                PawnIO.SavePawn(LoadedPawn);
            }
        }

        private void butSavImport_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            Pawn result = SavTab.Import();
            if (result != null)
            {
                LoadedPawn = result;
            }
            Cursor = Cursors.Arrow;
        }

        private void butSavExport_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            SavTab.Export(LoadedPawn);
            Cursor = Cursors.Arrow;
        }

        private void butSavBrowse_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openDialog = new Microsoft.Win32.OpenFileDialog();
            openDialog.Filter = PawnIO.SavFilter;
            openDialog.Title = "Browse for save file";

            bool? result = openDialog.ShowDialog();
            if (result == true)
            {
                if (!SavTab.ValidateAndSetSav(openDialog.FileName, false))
                {
                    MessageBox.Show(
                        "Selected file is not a valid packed or unpacked DDDA save",
                        ".sav error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }
        
        private void butSavUnpack_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            SavTab.Unpack();
            Cursor = Cursors.Arrow;
        }

        private void butSavRepack_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            SavTab.Repack();
            Cursor = Cursors.Arrow;
        }

        private void butSavLoadDefault_Click(object sender, RoutedEventArgs e)
        {
            string savPath = SavTab.GetDefaultSavPath();
            if (savPath == null)
            {
                MessageBox.Show(
                    "Unable to find your save file.\nIf it's where it should be, please let Meem0 know, along with the actual path to your save file.",
                    "Error loading default .sav",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            else
            {
                if (!SavTab.ValidateAndSetSav(savPath, false))
                {
                    MessageBox.Show(
                        string.Format("Unable to open your .sav file: {0}", savPath),
                        "Error loading default .sav",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
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
                savDir = savDir.Replace('/','\\');
                System.Diagnostics.Process.Start("explorer.exe", savDir);
            }
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
}
