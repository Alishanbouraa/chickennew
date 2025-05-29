using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PoultrySlaughterPOS.Converters
{
    /// <summary>
    /// Comprehensive collection of value converters for the Poultry Slaughter POS system.
    /// Implements enterprise-grade data transformation for MVVM binding scenarios
    /// with proper error handling and cultural formatting support.
    /// </summary>

    /// <summary>
    /// Multi-value converter for calculating weight per cage from total weight and cages count
    /// </summary>
    public class WeightPerCageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values?.Length != 2) return "0.00";

                if (values[0] is decimal totalWeight && values[1] is int cagesCount && cagesCount > 0)
                {
                    decimal weightPerCage = totalWeight / cagesCount;
                    return weightPerCage.ToString("F2", culture);
                }

                return "0.00";
            }
            catch (Exception)
            {
                return "0.00";
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("WeightPerCageConverter does not support ConvertBack");
        }
    }

    /// <summary>
    /// Converter for null to visibility conversion with configurable behavior
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool isInverted = parameter?.ToString()?.ToLowerInvariant() == "invert";
                bool isNull = value == null || (value is string str && string.IsNullOrWhiteSpace(str));

                if (isInverted)
                {
                    return isNull ? Visibility.Visible : Visibility.Collapsed;
                }

                return isNull ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("NullToVisibilityConverter does not support ConvertBack");
        }
    }

    /// <summary>
    /// Standard boolean to visibility converter for UI state management
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is bool boolValue)
                {
                    return boolValue ? Visibility.Visible : Visibility.Collapsed;
                }
                return Visibility.Collapsed;
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is Visibility visibility)
                {
                    return visibility == Visibility.Visible;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Inverse boolean to visibility converter for complementary UI states
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is bool boolValue)
                {
                    return boolValue ? Visibility.Collapsed : Visibility.Visible;
                }
                return Visibility.Visible;
            }
            catch (Exception)
            {
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is Visibility visibility)
                {
                    return visibility == Visibility.Collapsed;
                }
                return true;
            }
            catch (Exception)
            {
                return true;
            }
        }
    }

    /// <summary>
    /// Boolean value inverter for complementary logic operations
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is bool boolValue)
                {
                    return !boolValue;
                }
                return true;
            }
            catch (Exception)
            {
                return true;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is bool boolValue)
                {
                    return !boolValue;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Enterprise-grade currency converter with multi-format support and cultural formatting
    /// </summary>
    public class CurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null) return "$0.00";

                decimal amount = 0;

                if (value is decimal decimalValue)
                    amount = decimalValue;
                else if (value is double doubleValue)
                    amount = (decimal)doubleValue;
                else if (value is float floatValue)
                    amount = (decimal)floatValue;
                else if (decimal.TryParse(value.ToString(), out decimal parsedValue))
                    amount = parsedValue;
                else
                    return "$0.00";

                return amount.ToString("C2", culture ?? CultureInfo.CurrentCulture);
            }
            catch (Exception)
            {
                return "$0.00";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
                {
                    // Remove currency symbols and formatting
                    string cleanValue = stringValue.Replace("$", "").Replace(",", "").Trim();

                    if (decimal.TryParse(cleanValue, NumberStyles.Currency, culture ?? CultureInfo.CurrentCulture, out decimal result))
                    {
                        return result;
                    }
                }
                return 0m;
            }
            catch (Exception)
            {
                return 0m;
            }
        }
    }

    /// <summary>
    /// Weight converter with Arabic unit display and precision formatting
    /// </summary>
    public class WeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null) return "0.00 كغم";

                decimal weight = 0;

                if (value is decimal decimalValue)
                    weight = decimalValue;
                else if (value is double doubleValue)
                    weight = (decimal)doubleValue;
                else if (value is float floatValue)
                    weight = (decimal)floatValue;
                else if (decimal.TryParse(value.ToString(), out decimal parsedValue))
                    weight = parsedValue;
                else
                    return "0.00 كغم";

                return $"{weight:F2} كغم";
            }
            catch (Exception)
            {
                return "0.00 كغم";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
                {
                    string cleanValue = stringValue.Replace("كغم", "").Trim();

                    if (decimal.TryParse(cleanValue, out decimal result))
                    {
                        return result;
                    }
                }
                return 0m;
            }
            catch (Exception)
            {
                return 0m;
            }
        }
    }

    /// <summary>
    /// Percentage converter with precision control and localized formatting
    /// </summary>
    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null) return "0.0%";

                decimal percentage = 0;

                if (value is decimal decimalValue)
                    percentage = decimalValue;
                else if (value is double doubleValue)
                    percentage = (decimal)doubleValue;
                else if (value is float floatValue)
                    percentage = (decimal)floatValue;
                else if (decimal.TryParse(value.ToString(), out decimal parsedValue))
                    percentage = parsedValue;
                else
                    return "0.0%";

                return $"{percentage:F1}%";
            }
            catch (Exception)
            {
                return "0.0%";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is string stringValue && !string.IsNullOrWhiteSpace(stringValue))
                {
                    string cleanValue = stringValue.Replace("%", "").Trim();

                    if (decimal.TryParse(cleanValue, out decimal result))
                    {
                        return result;
                    }
                }
                return 0m;
            }
            catch (Exception)
            {
                return 0m;
            }
        }
    }

    /// <summary>
    /// Debt amount to color converter for financial status visualization
    /// </summary>
    public class DebtColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null) return "#6C757D";

                decimal debt = 0;

                if (value is decimal decimalValue)
                    debt = decimalValue;
                else if (value is double doubleValue)
                    debt = (decimal)doubleValue;
                else if (value is float floatValue)
                    debt = (decimal)floatValue;
                else if (decimal.TryParse(value.ToString(), out decimal parsedValue))
                    debt = parsedValue;

                return debt switch
                {
                    > 0 => "#DC3545",     // Red for outstanding debt
                    < 0 => "#28A745",     // Green for credit balance
                    _ => "#6C757D"        // Gray for zero balance
                };
            }
            catch (Exception)
            {
                return "#6C757D";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("DebtColorConverter does not support ConvertBack");
        }
    }

    /// <summary>
    /// Efficiency percentage to color converter for performance indicators
    /// </summary>
    public class EfficiencyToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null) return "#6C757D";

                double efficiency = 0;

                if (value is double doubleValue)
                    efficiency = doubleValue;
                else if (value is decimal decimalValue)
                    efficiency = (double)decimalValue;
                else if (value is float floatValue)
                    efficiency = floatValue;
                else if (double.TryParse(value.ToString(), out double parsedValue))
                    efficiency = parsedValue;

                return efficiency switch
                {
                    >= 85 => "#28A745", // Green - Excellent
                    >= 70 => "#FFC107", // Yellow - Good
                    >= 50 => "#FD7E14", // Orange - Fair
                    _ => "#DC3545"      // Red - Poor
                };
            }
            catch (Exception)
            {
                return "#6C757D"; // Gray default
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("EfficiencyToColorConverter does not support ConvertBack");
        }
    }

    /// <summary>
    /// Status to color mapping converter for operational state visualization
    /// </summary>
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is string status && !string.IsNullOrWhiteSpace(status))
                {
                    return status.ToUpperInvariant() switch
                    {
                        "LOADED" => "#007BFF",      // Blue - Loaded
                        "IN_TRANSIT" => "#FFC107",  // Yellow - In Transit
                        "COMPLETED" => "#28A745",   // Green - Completed
                        "CANCELLED" => "#DC3545",   // Red - Cancelled
                        "PENDING" => "#6C757D",     // Gray - Pending
                        _ => "#6C757D"              // Default Gray
                    };
                }
                return "#6C757D";
            }
            catch (Exception)
            {
                return "#6C757D";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("StatusToColorConverter does not support ConvertBack");
        }
    }

    /// <summary>
    /// Truck availability status converter with Arabic localization
    /// </summary>
    public class TruckAvailabilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is bool isAvailable)
                {
                    return isAvailable ? "متاحة" : "غير متاحة";
                }
                return "غير معروف";
            }
            catch (Exception)
            {
                return "غير معروف";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is string status)
                {
                    return status == "متاحة";
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Count to visibility converter for conditional UI element display
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is int count)
                {
                    return count > 0 ? Visibility.Visible : Visibility.Collapsed;
                }

                if (value is decimal countDecimal)
                {
                    return countDecimal > 0 ? Visibility.Visible : Visibility.Collapsed;
                }

                return Visibility.Collapsed;
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("CountToVisibilityConverter does not support ConvertBack");
        }
    }
}