using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PoultrySlaughterPOS.Converters
{
    /// <summary>
    /// Value converter for calculating weight per cage from total weight and cages count
    /// </summary>
    public class WeightPerCageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2)
                return "0.00";

            // Extract total weight and cages count
            if (values[0] is decimal totalWeight && values[1] is int cagesCount)
            {
                if (cagesCount > 0)
                {
                    decimal weightPerCage = totalWeight / cagesCount;
                    return weightPerCage.ToString("F2", culture);
                }
            }

            return "0.00";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("WeightPerCageConverter does not support ConvertBack");
        }
    }

    /// <summary>
    /// Converter for null to visibility conversion
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("NullToVisibilityConverter does not support ConvertBack");
        }
    }

    /// <summary>
    /// Converter for boolean to visibility conversion
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }

    /// <summary>
    /// Converter for inverting boolean values
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }

    /// <summary>
    /// Converter for formatting currency values
    /// </summary>
    public class CurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
            {
                return decimalValue.ToString("C2", culture);
            }
            else if (value is double doubleValue)
            {
                return doubleValue.ToString("C2", culture);
            }
            else if (value is float floatValue)
            {
                return floatValue.ToString("C2", culture);
            }
            return "0.00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                if (decimal.TryParse(stringValue.Replace("$", "").Replace(",", ""), out decimal result))
                {
                    return result;
                }
            }
            return 0m;
        }
    }

    /// <summary>
    /// Converter for efficiency percentage to color
    /// </summary>
    public class EfficiencyToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double efficiency)
            {
                return efficiency switch
                {
                    >= 85 => "#28A745", // Green
                    >= 70 => "#FFC107", // Yellow
                    >= 50 => "#FD7E14", // Orange
                    _ => "#DC3545"      // Red
                };
            }
            return "#6C757D"; // Gray default
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("EfficiencyToColorConverter does not support ConvertBack");
        }
    }

    /// <summary>
    /// Converter for status to color mapping
    /// </summary>
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToUpper() switch
                {
                    "LOADED" => "#007BFF",      // Blue
                    "IN_TRANSIT" => "#FFC107",  // Yellow
                    "COMPLETED" => "#28A745",   // Green
                    "CANCELLED" => "#DC3545",   // Red
                    "PENDING" => "#6C757D",     // Gray
                    _ => "#6C757D"              // Default Gray
                };
            }
            return "#6C757D";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("StatusToColorConverter does not support ConvertBack");
        }
    }

    /// <summary>
    /// Converter for truck availability status
    /// </summary>
    public class TruckAvailabilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isAvailable)
            {
                return isAvailable ? "متاحة" : "غير متاحة";
            }
            return "غير معروف";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status == "متاحة";
            }
            return false;
        }
    }

    /// <summary>
    /// Converter for count to visibility (show if count > 0)
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("CountToVisibilityConverter does not support ConvertBack");
        }
    }
}