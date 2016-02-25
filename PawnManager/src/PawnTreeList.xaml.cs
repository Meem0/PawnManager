using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace PawnManager
{
    /// <summary>
    /// Interaction logic for PawnTreeList.xaml
    /// </summary>
    public partial class PawnTreeList : UserControl
    {
        public PawnTreeList()
        {
            InitializeComponent();
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
}
