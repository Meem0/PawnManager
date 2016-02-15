using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace PawnManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        public class PawnData : INotifyPropertyChanged
        {
            public string Key { get; set; } = "";

            private object value;

            public event PropertyChangedEventHandler PropertyChanged;

            public object Value
            {
                get { return value; }
                set
                {
                    this.value = value;
                    if (PropertyChanged != null)
                        PropertyChanged(this, new PropertyChangedEventArgs("Value"));
                }
            }
        }

        public List<PawnData> TestItems { get; set; }
            = new List<PawnData>
        {
            new PawnData { Key = "key1", Value = "walnut" },
            new PawnData { Key = "key2", Value = 2 },
            new PawnData { Key = "key3", Value = "pineapple" },
            new PawnData { Key = "key4", Value = "grape" },
            new PawnData { Key = "key5", Value = 8 },
            new PawnData { Key = "key6", Value = "orange" },
            new PawnData { Key = "key7", Value = "coconut" } 
        };
        

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public SavTab SavTab { get; private set; }

        private IPawn loadedPawn = null;
        public IPawn LoadedPawn
        {
            get { return loadedPawn; }
            set
            {
                if (value != loadedPawn)
                {
                    loadedPawn = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged("IsPawnLoaded");
                }
            }
        }

        public bool IsPawnLoaded { get { return LoadedPawn != null; } }
        
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            SavTab = new SavTab();
        }

        private void butLoad_Click(object sender, RoutedEventArgs e)
        {
            IPawn result = null;
            try
            {
                result = PawnIO.LoadPawn();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Error loading Pawn",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            if (result != null)
            {
                LoadedPawn = result;
            }
        }

        private void butSave_Click(object sender, RoutedEventArgs e)
        {
            if (LoadedPawn != null)
            {
                try
                {
                    PawnIO.SavePawn(LoadedPawn);
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

        private void butSavImport_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            IPawn result = null;
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
                LoadedPawn = result;
            }
            Cursor = Cursors.Arrow;
        }

        private void butSavExport_Click(object sender, RoutedEventArgs e)
        {
            Cursor = Cursors.Wait;
            try
            {
                SavTab.Export(LoadedPawn);
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
            Microsoft.Win32.OpenFileDialog openDialog = new Microsoft.Win32.OpenFileDialog();
            openDialog.Filter = PawnIO.SavFilter;
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

    public class IntToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToInt32((double)value);
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

    public class PawnReadTemplateSelector : System.Windows.Controls.DataTemplateSelector
    {
        public DataTemplate SliderTemplate { get; set; }
        public DataTemplate StringTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            MainWindow.PawnData pawnData = item as MainWindow.PawnData;

            if (pawnData != null)
            {
                if (pawnData.Value is int)
                    return SliderTemplate;
                else
                    return StringTemplate;
            }

            return null;
        }
    }

    public class PawnEditTemplateSelector : System.Windows.Controls.DataTemplateSelector
    {
        public DataTemplate SliderTemplate { get; set; }
        public DataTemplate StringTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            MainWindow.PawnData pawnData = item as MainWindow.PawnData;

            if (pawnData != null)
            {
                if (pawnData.Value is int)
                    return SliderTemplate;
                else
                    return StringTemplate;
            }

            return null;
        }
    }
}
