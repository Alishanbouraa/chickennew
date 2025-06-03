using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PoultrySlaughterPOS.Converters
{
    /// <summary>
    /// Comprehensive collection of value converters for the Poultry Slaughter POS system.
    /// Implements enterprise-grade data transformation for MVVM binding scenarios with proper 
    /// error handling, cultural formatting support, and complete customer management integration.
    /// 
    /// ENHANCED: Complete converter collection including customer management, financial analysis,
    /// and advanced UI binding converters with professional error handling patterns.
    /// </summary>

    #region Core System Converters

    /// <summary>
    /// Multi-value converter for calculating weight per cage from total weight and cages count
    /// with enhanced precision and error handling
    /// </summary>
    public class WeightPerCageConverter : IMultiValueConverter
    {
        public static readonly WeightPerCageConverter Instance = new();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values?.Length != 2) return "0.00";

                if (values[0] is decimal totalWeight && values[1] is int cagesCount && cagesCount > 0)
                {
                    decimal weightPerCage = totalWeight / cagesCount;
                    return weightPerCage.ToString("F2", culture ?? CultureInfo.CurrentCulture);
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
            throw new NotImplementedException("WeightPerCageConverter does not support ConvertBack operation");
        }
    }

    /// <summary>
    /// Converter for null to visibility conversion with configurable behavior and enhanced logic
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public static readonly NullToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool isInverted = parameter?.ToString()?.ToLowerInvariant() == "invert";
                bool isNull = value == null ||
                             (value is string str && string.IsNullOrWhiteSpace(str)) ||
                             (value is int intVal && intVal == 0) ||
                             (value is decimal decVal && decVal == 0);

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
            throw new NotImplementedException("NullToVisibilityConverter does not support ConvertBack operation");
        }
    }

    /// <summary>
    /// Standard boolean to visibility converter for UI state management with enhanced reliability
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public static readonly BooleanToVisibilityConverter Instance = new();

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
        public static readonly InverseBooleanToVisibilityConverter Instance = new();

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
    /// Boolean value inverter for complementary logic operations with type safety
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public static readonly InverseBooleanConverter Instance = new();

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

    #endregion

    #region Financial and Currency Converters

    /// <summary>
    /// Enterprise-grade currency converter with multi-format support, cultural formatting,
    /// and enhanced precision handling for financial applications
    /// </summary>
    public class CurrencyConverter : IValueConverter
    {
        public static readonly CurrencyConverter Instance = new();

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
                else if (value is int intValue)
                    amount = intValue;
                else if (decimal.TryParse(value.ToString(), out decimal parsedValue))
                    amount = parsedValue;
                else
                    return "$0.00";

                // Use parameter for currency symbol override
                string currencySymbol = parameter?.ToString() ?? "$";
                string format = $"{currencySymbol}{{0:N2}}";

                return string.Format(culture ?? CultureInfo.CurrentCulture, format.Replace("{0:N2}", "{0:N2}"), amount);
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
                    string cleanValue = stringValue
                        .Replace("$", "")
                        .Replace("USD", "")
                        .Replace("€", "")
                        .Replace("£", "")
                        .Replace(",", "")
                        .Trim();

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
    /// Weight converter with Arabic unit display and enhanced precision formatting
    /// </summary>
    public class WeightConverter : IValueConverter
    {
        public static readonly WeightConverter Instance = new();

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
                else if (value is int intValue)
                    weight = intValue;
                else if (decimal.TryParse(value.ToString(), out decimal parsedValue))
                    weight = parsedValue;
                else
                    return "0.00 كغم";

                // Use parameter for unit override
                string unit = parameter?.ToString() ?? "كغم";

                return $"{weight:F2} {unit}";
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
                    string cleanValue = stringValue
                        .Replace("كغم", "")
                        .Replace("kg", "")
                        .Replace("lbs", "")
                        .Trim();

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
        public static readonly PercentageConverter Instance = new();

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
                else if (value is int intValue)
                    percentage = intValue;
                else if (decimal.TryParse(value.ToString(), out decimal parsedValue))
                    percentage = parsedValue;
                else
                    return "0.0%";

                // Use parameter for decimal places override
                int decimalPlaces = 1;
                if (parameter != null && int.TryParse(parameter.ToString(), out int paramValue))
                {
                    decimalPlaces = Math.Max(0, Math.Min(4, paramValue));
                }

                string format = $"F{decimalPlaces}";
                return $"{percentage.ToString(format, culture ?? CultureInfo.CurrentCulture)}%";
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
    /// Debt amount to color converter for financial status visualization with enhanced color logic
    /// </summary>
    public class DebtColorConverter : IValueConverter
    {
        public static readonly DebtColorConverter Instance = new();

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
                else if (value is int intValue)
                    debt = intValue;
                else if (decimal.TryParse(value.ToString(), out decimal parsedValue))
                    debt = parsedValue;

                return debt switch
                {
                    > 5000 => "#B91C1C",    // Dark red for very high debt
                    > 1000 => "#DC2626",    // Red for high debt
                    > 500 => "#EF4444",     // Medium red for moderate debt
                    > 0 => "#F97316",       // Orange for low debt
                    < 0 => "#059669",       // Green for credit balance
                    _ => "#6B7280"          // Gray for zero balance
                };
            }
            catch (Exception)
            {
                return "#6B7280";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("DebtColorConverter does not support ConvertBack operation");
        }
    }

    #endregion

    #region Customer Management Converters

    /// <summary>
    /// Converter for customer status indicator colors based on active status and debt amount
    /// with sophisticated business logic for risk assessment
    /// </summary>
    public class StatusIndicatorConverter : IMultiValueConverter
    {
        public static readonly StatusIndicatorConverter Instance = new();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values?.Length != 2) return Colors.Gray;

                var isActive = values[0] is bool active && active;
                var debt = values[1] is decimal debtAmount ? debtAmount : 0m;

                // Enhanced business logic for status determination
                if (!isActive)
                    return Colors.Red; // Inactive customer

                if (debt > 10000)
                    return Colors.DarkRed; // Critical debt level

                if (debt > 5000)
                    return Colors.OrangeRed; // High debt level

                if (debt > 1000)
                    return Colors.Orange; // Moderate debt level

                if (debt > 0)
                    return Colors.Gold; // Low debt level

                return Colors.Green; // Good standing or credit balance
            }
            catch (Exception)
            {
                return Colors.Gray;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("StatusIndicatorConverter does not support ConvertBack operation");
        }
    }

    /// <summary>
    /// Converter for status badges - converts boolean active status to appropriate brush color
    /// </summary>
    public class StatusToBrushConverter : IValueConverter
    {
        public static readonly StatusToBrushConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is bool isActive)
                {
                    return isActive ? Colors.Green : Colors.Red;
                }
                return Colors.Gray;
            }
            catch (Exception)
            {
                return Colors.Gray;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("StatusToBrushConverter does not support ConvertBack operation");
        }
    }

    /// <summary>
    /// Converter for account age calculation with enhanced localization
    /// </summary>
    public class AccountAgeConverter : IValueConverter
    {
        public static readonly AccountAgeConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is DateTime createdDate)
                {
                    var age = (DateTime.Now - createdDate).Days;

                    return age switch
                    {
                        < 30 => $"{age} يوم",
                        < 365 => $"{age / 30} شهر",
                        _ => $"{age / 365} سنة"
                    };
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
            throw new NotImplementedException("AccountAgeConverter does not support ConvertBack operation");
        }
    }

    /// <summary>
    /// Converter for formatting large numbers with K/M suffixes and Arabic support
    /// </summary>
    public class NumberFormatConverter : IValueConverter
    {
        public static readonly NumberFormatConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is decimal number)
                {
                    if (number >= 1000000)
                        return $"{number / 1000000:F1}M";
                    if (number >= 1000)
                        return $"{number / 1000:F1}K";
                    return number.ToString("F0", culture ?? CultureInfo.CurrentCulture);
                }
                return "0";
            }
            catch (Exception)
            {
                return "0";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("NumberFormatConverter does not support ConvertBack operation");
        }
    }

    /// <summary>
    /// Converter for debt urgency levels with sophisticated business rules
    /// </summary>
    public class DebtUrgencyConverter : IMultiValueConverter
    {
        public static readonly DebtUrgencyConverter Instance = new();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values?.Length != 2) return "عادي";

                var debt = values[0] is decimal debtAmount ? debtAmount : 0m;
                var createdDate = values[1] is DateTime created ? created : DateTime.Now;

                var accountAge = (DateTime.Now - createdDate).Days;

                if (debt <= 0) return "جيد";
                if (debt > 10000 || accountAge > 730) return "حرج";
                if (debt > 5000 || accountAge > 365) return "عالي";
                if (debt > 1000 || accountAge > 180) return "متوسط";
                if (debt > 100) return "منخفض";

                return "عادي";
            }
            catch (Exception)
            {
                return "عادي";
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("DebtUrgencyConverter does not support ConvertBack operation");
        }
    }

    /// <summary>
    /// Converter for customer priority ranking based on transaction volume and loyalty
    /// </summary>
    public class CustomerPriorityConverter : IMultiValueConverter
    {
        public static readonly CustomerPriorityConverter Instance = new();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values?.Length != 3) return "عادي";

                var totalDebt = values[0] is decimal debt ? Math.Abs(debt) : 0m;
                var accountAge = values[1] is DateTime created ? (DateTime.Now - created).Days : 0;
                var isActive = values[2] is bool active && active;

                if (!isActive) return "غير نشط";

                // Enhanced priority calculation with weighted factors
                var score = 0;

                // Debt amount factor (indicates transaction volume)
                if (totalDebt > 20000) score += 5;
                else if (totalDebt > 10000) score += 4;
                else if (totalDebt > 5000) score += 3;
                else if (totalDebt > 1000) score += 2;
                else if (totalDebt > 100) score += 1;

                // Account age factor (indicates loyalty)
                if (accountAge > 1095) score += 3; // 3+ years
                else if (accountAge > 730) score += 2; // 2+ years
                else if (accountAge > 365) score += 1; // 1+ year

                return score switch
                {
                    >= 7 => "VIP",
                    >= 5 => "مميز",
                    >= 3 => "مهم",
                    >= 1 => "عادي",
                    _ => "جديد"
                };
            }
            catch (Exception)
            {
                return "عادي";
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("CustomerPriorityConverter does not support ConvertBack operation");
        }
    }

    /// <summary>
    /// Converter for financial health indicators with advanced color coding
    /// </summary>
    public class FinancialHealthConverter : IValueConverter
    {
        public static readonly FinancialHealthConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is decimal debt)
                {
                    if (debt <= 0) return new SolidColorBrush(Colors.Green);
                    if (debt <= 500) return new SolidColorBrush(Colors.YellowGreen);
                    if (debt <= 1000) return new SolidColorBrush(Colors.Orange);
                    if (debt <= 5000) return new SolidColorBrush(Colors.OrangeRed);
                    return new SolidColorBrush(Colors.Red);
                }
                return new SolidColorBrush(Colors.Gray);
            }
            catch (Exception)
            {
                return new SolidColorBrush(Colors.Gray);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("FinancialHealthConverter does not support ConvertBack operation");
        }
    }

    /// <summary>
    /// Converter for relative date formatting with comprehensive Arabic localization
    /// </summary>
    public class RelativeDateConverter : IValueConverter
    {
        public static readonly RelativeDateConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is DateTime date)
                {
                    var timeSpan = DateTime.Now - date;
                    var days = (int)timeSpan.TotalDays;
                    var hours = (int)timeSpan.TotalHours;
                    var minutes = (int)timeSpan.TotalMinutes;

                    return days switch
                    {
                        0 when hours == 0 && minutes < 60 => minutes switch
                        {
                            0 => "الآن",
                            1 => "منذ دقيقة",
                            < 60 => $"منذ {minutes} دقيقة"
                        },
                        0 when hours < 24 => hours switch
                        {
                            1 => "منذ ساعة",
                            < 24 => $"منذ {hours} ساعة"
                        },
                        0 => "اليوم",
                        1 => "أمس",
                        < 7 => $"منذ {days} أيام",
                        < 30 => $"منذ {days / 7} أسبوع",
                        < 365 => $"منذ {days / 30} شهر",
                        _ => $"منذ {days / 365} سنة"
                    };
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
            throw new NotImplementedException("RelativeDateConverter does not support ConvertBack operation");
        }
    }

    /// <summary>
    /// Converter for transaction frequency indicators with enhanced business logic
    /// </summary>
    public class TransactionFrequencyConverter : IValueConverter
    {
        public static readonly TransactionFrequencyConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is int transactionCount)
                {
                    return transactionCount switch
                    {
                        0 => "لا توجد معاملات",
                        1 => "معاملة واحدة",
                        < 5 => "معاملات قليلة",
                        < 20 => "معاملات متوسطة",
                        < 50 => "معاملات كثيرة",
                        < 100 => "عميل نشط",
                        _ => "عميل نشط جداً"
                    };
                }
                return "غير محدد";
            }
            catch (Exception)
            {
                return "غير محدد";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("TransactionFrequencyConverter does not support ConvertBack operation");
        }
    }

    #endregion

    #region System Status and Operational Converters

    /// <summary>
    /// Efficiency percentage to color converter for performance indicators
    /// </summary>
    public class EfficiencyToColorConverter : IValueConverter
    {
        public static readonly EfficiencyToColorConverter Instance = new();

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
                else if (value is int intValue)
                    efficiency = intValue;
                else if (double.TryParse(value.ToString(), out double parsedValue))
                    efficiency = parsedValue;

                return efficiency switch
                {
                    >= 95 => "#059669", // Dark green - Excellent
                    >= 85 => "#10B981", // Green - Very good
                    >= 75 => "#34D399", // Light green - Good
                    >= 60 => "#FCD34D", // Yellow - Fair
                    >= 40 => "#FB923C", // Orange - Poor
                    _ => "#EF4444"      // Red - Very poor
                };
            }
            catch (Exception)
            {
                return "#6C757D"; // Gray default
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("EfficiencyToColorConverter does not support ConvertBack operation");
        }
    }

    /// <summary>
    /// Status to color mapping converter for operational state visualization
    /// </summary>
    public class StatusToColorConverter : IValueConverter
    {
        public static readonly StatusToColorConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is string status && !string.IsNullOrWhiteSpace(status))
                {
                    return status.ToUpperInvariant() switch
                    {
                        "LOADED" or "محملة" => "#3B82F6",      // Blue - Loaded
                        "IN_TRANSIT" or "في الطريق" => "#F59E0B",  // Orange - In Transit
                        "COMPLETED" or "مكتملة" => "#10B981",   // Green - Completed
                        "CANCELLED" or "ملغية" => "#EF4444",   // Red - Cancelled
                        "PENDING" or "في الانتظار" => "#6B7280",     // Gray - Pending
                        "PROCESSING" or "قيد المعالجة" => "#8B5CF6", // Purple - Processing
                        _ => "#6B7280"              // Default Gray
                    };
                }
                return "#6B7280";
            }
            catch (Exception)
            {
                return "#6B7280";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("StatusToColorConverter does not support ConvertBack operation");
        }
    }

    /// <summary>
    /// Truck availability status converter with Arabic localization
    /// </summary>
    public class TruckAvailabilityConverter : IValueConverter
    {
        public static readonly TruckAvailabilityConverter Instance = new();

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
        public static readonly CountToVisibilityConverter Instance = new();

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

                if (value is double countDouble)
                {
                    return countDouble > 0 ? Visibility.Visible : Visibility.Collapsed;
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
            throw new NotImplementedException("CountToVisibilityConverter does not support ConvertBack operation");
        }
    }

    #endregion

    #region Payment and Transaction Converters

    /// <summary>
    /// Payment method to brush converter for visual differentiation
    /// </summary>
    public class PaymentMethodToBrushConverter : IValueConverter
    {
        public static readonly PaymentMethodToBrushConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is string paymentMethod && !string.IsNullOrWhiteSpace(paymentMethod))
                {
                    var color = paymentMethod.ToUpperInvariant() switch
                    {
                        "CASH" or "نقد" => "#10B981",        // Green
                        "CHECK" or "شيك" => "#3B82F6",       // Blue
                        "BANK_TRANSFER" or "تحويل بنكي" => "#8B5CF6", // Purple
                        "CREDIT_CARD" or "بطاقة ائتمان" => "#F59E0B",  // Orange
                        "DEBIT_CARD" or "بطاقة دفع" => "#EF4444",    // Red
                        _ => "#6B7280"                      // Gray default
                    };
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
                }
                return new SolidColorBrush(Colors.Gray);
            }
            catch (Exception)
            {
                return new SolidColorBrush(Colors.Gray);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("PaymentMethodToBrushConverter does not support ConvertBack operation");
        }
    }

    /// <summary>
    /// Transaction type to brush converter for visual categorization
    /// </summary>
    public class TransactionTypeToBrushConverter : IValueConverter
    {
        public static readonly TransactionTypeToBrushConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is string transactionType && !string.IsNullOrWhiteSpace(transactionType))
                {
                    var color = transactionType.ToUpperInvariant() switch
                    {
                        "INVOICE" or "فاتورة" => "#EF4444",     // Red for debit
                        "PAYMENT" or "دفعة" => "#10B981",      // Green for credit
                        "ADJUSTMENT" or "تعديل" => "#F59E0B",  // Orange for adjustment
                        "REFUND" or "استرداد" => "#8B5CF6",    // Purple for refund
                        _ => "#6B7280"                        // Gray default
                    };
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
                }
                return new SolidColorBrush(Colors.Gray);
            }
            catch (Exception)
            {
                return new SolidColorBrush(Colors.Gray);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("TransactionTypeToBrushConverter does not support ConvertBack operation");
        }
    }

    #endregion

    #region UI Progress and Animation Converters

    /// <summary>
    /// Percentage to width converter for progress bars and visual indicators
    /// </summary>
    public class PercentageToWidthConverter : IValueConverter
    {
        public static readonly PercentageToWidthConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is double percentage)
                {
                    // Clamp percentage between 0 and 100
                    percentage = Math.Max(0, Math.Min(100, percentage));

                    // Use parameter for max width or default to 100
                    double maxWidth = 100;
                    if (parameter != null && double.TryParse(parameter.ToString(), out double paramWidth))
                    {
                        maxWidth = paramWidth;
                    }

                    return (percentage / 100.0) * maxWidth;
                }
                return 0.0;
            }
            catch (Exception)
            {
                return 0.0;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is double width && parameter != null && double.TryParse(parameter.ToString(), out double maxWidth))
                {
                    return maxWidth > 0 ? (width / maxWidth) * 100.0 : 0.0;
                }
                return 0.0;
            }
            catch (Exception)
            {
                return 0.0;
            }
        }
    }

    #endregion
}