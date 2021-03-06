﻿using System;
using System.Windows.Data;

namespace AutoMerge.Converters
{
    /// <summary> 
    /// Escape the label mnemonics such as underscore
    /// </summary> 
    public class EscapeMnemonicConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string label = (value is string) ? (string)value : String.Empty;
            return label.Replace("_", "__");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return String.Empty;
        }
    }
}
