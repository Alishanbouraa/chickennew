using System.Globalization;
using System.Windows.Data;

namespace PoultrySlaughterPOS.Converters
{
    /// <summary>
    /// Enterprise-grade value converters for WPF data binding in the Poultry Slaughter POS system.
    /// Implements IValueConverter and IMultiValueConverter interfaces with comprehensive error handling
    /// and localization support for Arabic and English formatting.
    /// </summary>

    /// <summary>
    /// Converter for calculating weight per cage from total weight and cage count
    /// </summary>
    public class WeightPerCageConverter : IMultiValueConverter
    {
        /// <summary>
        /// Converts total weight and cage count to weight per cage
        /// </summary>
        /// <param name="values">Array containing [TotalWeight, CagesCount]</param>
        /// <param name="targetType">Target type (typically decimal)</param>
        /// <param name="parameter">Optional parameter for formatting</param>
        /// <param name="culture">Culture info for localization</param>
        /// <returns>Calculated weight per cage or 0 if invalid input</returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values == null || values.Length < 2)
                    return 0m;

                // Extract and validate total weight
                if (!decimal.TryParse(values[0]?.ToString(), out decimal totalWeight) || totalWeight <= 0)
                    return 0m;

                // Extract and validate cage count
                if (!int.TryParse(values[1]?.ToString(), out int cagesCount) || cagesCount <= 0)
                    return 0m;

                // Calculate weight per cage with precision
                decimal weightPerCage = totalWeight / cagesCount;

                // Apply formatting if parameter is provided
                if (parameter is string format && !string.IsNullOrEmpty(format))
                {
                    return weightPerCage.ToString(format, culture);
                }

                return Math.Round(weightPerCage, 2);
            }
            catch (Exception)
            {
                // Return 0 for any calculation errors
                return 0m;
            }
        }

        /// <summary>
        /// Not implemented for this converter (one-way binding only)
        /// </summary>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("WeightPerCageConverter is designed for one-way binding only");
        }
    }

    /// <summary>
    /// Converter for formatting decimal weights with Arabic/English number formatting
    /// </summary>
    public class WeightFormatConverter : IValueConverter
    {
        /// <summary>
        /// Converts decimal weight to formatted string with units
        /// </summary>
        /// <param name="value">Decimal weight value</param>
        /// <param name="targetType">Target type (string)</param>
        /// <param name="parameter">Format parameter: "kg", "unit", or custom format</param>
        /// <param name="culture">Culture info for number formatting</param>
        /// <returns>Formatted weight string with appropriate units</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null)
                    return "0.00";

                if (!decimal.TryParse(value.ToString(), out decimal weight))
                    return "0.00";

                string unit = " كيلو";
                string format = "F2";

                // Parse parameter for custom formatting
                if (parameter is string param)
                {
                    switch (param.ToLower())
                    {
                        case "kg":
                            unit = " كيلو";
                            break;
                        case "unit":
                            unit = " وحدة";
                            break;
                        case "none":
                            unit = "";
                            break;
                        default:
                            if (param.Contains(":"))
                            {
                                var parts = param.Split(':');
                                if (parts.Length >= 1) format = parts[0];
                                if (parts.Length >= 2) unit = $" {parts[1]}";
                            }
                            break;
                    }
                }

                return $"{weight.ToString(format, culture)}{unit}";
            }
            catch (Exception)
            {
                return "0.00 كيلو";
            }
        }

        /// <summary>
        /// Converts formatted weight string back to decimal (for two-way binding)
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null)
                    return 0m;

                string input = value.ToString()?.Trim() ?? "";

                // Remove common unit suffixes
                string[] unitsToRemove = { "كيلو", "وحدة", "kg", "unit" };
                foreach (string unit in unitsToRemove)
                {
                    input = input.Replace(unit, "").Trim();
                }

                if (decimal.TryParse(input, NumberStyles.Number, culture, out decimal result))
                {
                    return Math.Max(0, result); // Ensure non-negative values
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
    /// Converter for truck status to display-friendly Arabic text with color coding
    /// </summary>
    public class TruckStatusConverter : IValueConverter
    {
        /// <summary>
        /// Converts truck status enum/string to Arabic display text
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null)
                    return "غير محدد";

                string status = value.ToString()?.ToUpper() ?? "";

                return status switch
                {
                    "LOADED" => "محملة",
                    "IN_TRANSIT" => "في الطريق",
                    "COMPLETED" => "مكتملة",
                    "AVAILABLE" => "متاحة",
                    "MAINTENANCE" => "صيانة",
                    "OUT_OF_SERVICE" => "خارج الخدمة",
                    _ => status // Return original if no mapping found
                };
            }
            catch (Exception)
            {
                return "غير محدد";
            }
        }

        /// <summary>
        /// Converts Arabic status back to English (for two-way binding)
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null)
                    return "UNKNOWN";

                string arabicStatus = value.ToString()?.Trim() ?? "";

                return arabicStatus switch
                {
                    "محملة" => "LOADED",
                    "في الطريق" => "IN_TRANSIT",
                    "مكتملة" => "COMPLETED",
                    "متاحة" => "AVAILABLE",
                    "صيانة" => "MAINTENANCE",
                    "خارج الخدمة" => "OUT_OF_SERVICE",
                    _ => arabicStatus.ToUpper()
                };
            }
            catch (Exception)
            {
                return "UNKNOWN";
            }
        }
    }

    /// <summary>
    /// Converter for efficiency percentage to color brush for visual indicators
    /// </summary>
    public class EfficiencyToColorConverter : IValueConverter
    {
        /// <summary>
        /// Converts efficiency percentage to appropriate color brush
        /// </summary>
        /// <param name="value">Efficiency percentage (0-100)</param>
        /// <param name="targetType">Target brush type</param>
        /// <param name="parameter">Optional color scheme parameter</param>
        /// <param name="culture">Culture info</param>
        /// <returns>Color brush based on efficiency level</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (!double.TryParse(value?.ToString(), out double efficiency))
                    return System.Windows.Media.Brushes.Gray;

                // Color scheme based on efficiency percentage
                return efficiency switch
                {
                    >= 85 => System.Windows.Media.Brushes.Green,        // Excellent
                    >= 70 => System.Windows.Media.Brushes.LimeGreen,    // Good
                    >= 50 => System.Windows.Media.Brushes.Orange,       // Fair
                    >= 30 => System.Windows.Media.Brushes.OrangeRed,    // Poor
                    _ => System.Windows.Media.Brushes.Red               // Critical
                };
            }
            catch (Exception)
            {
                return System.Windows.Media.Brushes.Gray;
            }
        }

        /// <summary>
        /// Not implemented for color conversion (one-way binding only)
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("EfficiencyToColorConverter is designed for one-way binding only");
        }
    }

    /// <summary>
    /// Converter for date/time formatting with Arabic calendar support
    /// </summary>
    public class ArabicDateTimeConverter : IValueConverter
    {
        /// <summary>
        /// Formats DateTime to Arabic-friendly display format
        /// </summary>
        /// <param name="value">DateTime value</param>
        /// <param name="targetType">Target string type</param>
        /// <param name="parameter">Format parameter: "date", "time", "datetime", or custom format</param>
        /// <param name="culture">Culture info for localization</param>
        /// <returns>Formatted date/time string</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is not DateTime dateTime)
                    return "";

                string format = parameter?.ToString()?.ToLower() ?? "datetime";

                return format switch
                {
                    "date" => dateTime.ToString("yyyy/MM/dd", culture),
                    "time" => dateTime.ToString("HH:mm", culture),
                    "datetime" => dateTime.ToString("yyyy/MM/dd HH:mm", culture),
                    "full" => dateTime.ToString("dddd، dd MMMM yyyy", new CultureInfo("ar-SA")),
                    "short" => dateTime.ToString("dd/MM", culture),
                    _ => dateTime.ToString(format, culture)
                };
            }
            catch (Exception)
            {
                return value?.ToString() ?? "";
            }
        }

        /// <summary>
        /// Parses formatted date string back to DateTime
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null)
                    return DateTime.Now;

                string input = value.ToString()?.Trim() ?? "";

                if (DateTime.TryParse(input, culture, DateTimeStyles.None, out DateTime result))
                {
                    return result;
                }

                return DateTime.Now;
            }
            catch (Exception)
            {
                return DateTime.Now;
            }
        }
    }

    /// <summary>
    /// Converter for boolean to Arabic Yes/No text
    /// </summary>
    public class BooleanToArabicTextConverter : IValueConverter
    {
        /// <summary>
        /// Converts boolean to Arabic Yes/No text
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is bool boolValue)
                {
                    return boolValue ? "نعم" : "لا";
                }

                return "غير محدد";
            }
            catch (Exception)
            {
                return "غير محدد";
            }
        }

        /// <summary>
        /// Converts Arabic Yes/No text back to boolean
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                string text = value?.ToString()?.Trim() ?? "";

                return text switch
                {
                    "نعم" => true,
                    "لا" => false,
                    _ => false
                };
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}