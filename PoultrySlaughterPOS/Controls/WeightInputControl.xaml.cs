using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PoultrySlaughterPOS.Controls
{
    /// <summary>
    /// Advanced weight input control with real-time calculation, validation, and efficiency metrics.
    /// Provides comprehensive weight and cage count input with automatic validation and visual feedback.
    /// Implements enterprise-grade validation patterns with shared event architecture.
    /// </summary>
    public partial class WeightInputControl : UserControl
    {
        #region Dependency Properties

        /// <summary>
        /// Total weight dependency property
        /// </summary>
        public static readonly DependencyProperty TotalWeightProperty =
            DependencyProperty.Register(
                nameof(TotalWeight),
                typeof(decimal),
                typeof(WeightInputControl),
                new FrameworkPropertyMetadata(0m, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnWeightValueChanged));

        /// <summary>
        /// Cages count dependency property
        /// </summary>
        public static readonly DependencyProperty CagesCountProperty =
            DependencyProperty.Register(
                nameof(CagesCount),
                typeof(int),
                typeof(WeightInputControl),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnWeightValueChanged));

        /// <summary>
        /// Weight per cage dependency property (calculated)
        /// </summary>
        public static readonly DependencyProperty WeightPerCageProperty =
            DependencyProperty.Register(
                nameof(WeightPerCage),
                typeof(decimal),
                typeof(WeightInputControl),
                new PropertyMetadata(0m));

        /// <summary>
        /// Efficiency rating dependency property (calculated)
        /// </summary>
        public static readonly DependencyProperty EfficiencyRatingProperty =
            DependencyProperty.Register(
                nameof(EfficiencyRating),
                typeof(double),
                typeof(WeightInputControl),
                new PropertyMetadata(0.0, OnEfficiencyRatingChanged));

        /// <summary>
        /// Efficiency color dependency property
        /// </summary>
        public static readonly DependencyProperty EfficiencyColorProperty =
            DependencyProperty.Register(
                nameof(EfficiencyColor),
                typeof(Brush),
                typeof(WeightInputControl),
                new PropertyMetadata(Brushes.Gray));

        /// <summary>
        /// Weight per cage color dependency property
        /// </summary>
        public static readonly DependencyProperty WeightPerCageColorProperty =
            DependencyProperty.Register(
                nameof(WeightPerCageColor),
                typeof(Brush),
                typeof(WeightInputControl),
                new PropertyMetadata(Brushes.Black));

        /// <summary>
        /// Efficiency bar width dependency property
        /// </summary>
        public static readonly DependencyProperty EfficiencyBarWidthProperty =
            DependencyProperty.Register(
                nameof(EfficiencyBarWidth),
                typeof(double),
                typeof(WeightInputControl),
                new PropertyMetadata(0.0));

        /// <summary>
        /// Has calculated values dependency property
        /// </summary>
        public static readonly DependencyProperty HasCalculatedValuesProperty =
            DependencyProperty.Register(
                nameof(HasCalculatedValues),
                typeof(bool),
                typeof(WeightInputControl),
                new PropertyMetadata(false));

        /// <summary>
        /// Validation messages dependency property
        /// </summary>
        public static readonly DependencyProperty ValidationMessagesProperty =
            DependencyProperty.Register(
                nameof(ValidationMessages),
                typeof(ObservableCollection<string>),
                typeof(WeightInputControl),
                new PropertyMetadata(null));

        /// <summary>
        /// Minimum weight per cage for validation
        /// </summary>
        public static readonly DependencyProperty MinWeightPerCageProperty =
            DependencyProperty.Register(
                nameof(MinWeightPerCage),
                typeof(decimal),
                typeof(WeightInputControl),
                new PropertyMetadata(5m, OnWeightValueChanged));

        /// <summary>
        /// Maximum weight per cage for validation
        /// </summary>
        public static readonly DependencyProperty MaxWeightPerCageProperty =
            DependencyProperty.Register(
                nameof(MaxWeightPerCage),
                typeof(decimal),
                typeof(WeightInputControl),
                new PropertyMetadata(100m, OnWeightValueChanged));

        /// <summary>
        /// Optimal weight per cage for efficiency calculation
        /// </summary>
        public static readonly DependencyProperty OptimalWeightPerCageProperty =
            DependencyProperty.Register(
                nameof(OptimalWeightPerCage),
                typeof(decimal),
                typeof(WeightInputControl),
                new PropertyMetadata(25m, OnWeightValueChanged));

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the total weight
        /// </summary>
        public decimal TotalWeight
        {
            get => (decimal)GetValue(TotalWeightProperty);
            set => SetValue(TotalWeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the cages count
        /// </summary>
        public int CagesCount
        {
            get => (int)GetValue(CagesCountProperty);
            set => SetValue(CagesCountProperty, value);
        }

        /// <summary>
        /// Gets the calculated weight per cage
        /// </summary>
        public decimal WeightPerCage
        {
            get => (decimal)GetValue(WeightPerCageProperty);
            private set => SetValue(WeightPerCageProperty, value);
        }

        /// <summary>
        /// Gets the calculated efficiency rating
        /// </summary>
        public double EfficiencyRating
        {
            get => (double)GetValue(EfficiencyRatingProperty);
            private set => SetValue(EfficiencyRatingProperty, value);
        }

        /// <summary>
        /// Gets the efficiency color brush
        /// </summary>
        public Brush EfficiencyColor
        {
            get => (Brush)GetValue(EfficiencyColorProperty);
            private set => SetValue(EfficiencyColorProperty, value);
        }

        /// <summary>
        /// Gets the weight per cage color brush
        /// </summary>
        public Brush WeightPerCageColor
        {
            get => (Brush)GetValue(WeightPerCageColorProperty);
            private set => SetValue(WeightPerCageColorProperty, value);
        }

        /// <summary>
        /// Gets the efficiency bar width
        /// </summary>
        public double EfficiencyBarWidth
        {
            get => (double)GetValue(EfficiencyBarWidthProperty);
            private set => SetValue(EfficiencyBarWidthProperty, value);
        }

        /// <summary>
        /// Gets whether calculated values should be displayed
        /// </summary>
        public bool HasCalculatedValues
        {
            get => (bool)GetValue(HasCalculatedValuesProperty);
            private set => SetValue(HasCalculatedValuesProperty, value);
        }

        /// <summary>
        /// Gets the validation messages collection
        /// </summary>
        public ObservableCollection<string> ValidationMessages
        {
            get => (ObservableCollection<string>)GetValue(ValidationMessagesProperty);
            private set => SetValue(ValidationMessagesProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum weight per cage for validation
        /// </summary>
        public decimal MinWeightPerCage
        {
            get => (decimal)GetValue(MinWeightPerCageProperty);
            set => SetValue(MinWeightPerCageProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum weight per cage for validation
        /// </summary>
        public decimal MaxWeightPerCage
        {
            get => (decimal)GetValue(MaxWeightPerCageProperty);
            set => SetValue(MaxWeightPerCageProperty, value);
        }

        /// <summary>
        /// Gets or sets the optimal weight per cage for efficiency calculation
        /// </summary>
        public decimal OptimalWeightPerCage
        {
            get => (decimal)GetValue(OptimalWeightPerCageProperty);
            set => SetValue(OptimalWeightPerCageProperty, value);
        }

        #endregion

        #region Events

        /// <summary>
        /// Event raised when weight values change
        /// </summary>
        public event EventHandler<WeightValuesChangedEventArgs>? WeightValuesChanged;

        /// <summary>
        /// Event raised when validation state changes - uses shared ValidationStateChangedEventArgs
        /// </summary>
        public event EventHandler<ValidationStateChangedEventArgs>? ValidationStateChanged;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the WeightInputControl
        /// </summary>
        public WeightInputControl()
        {
            InitializeComponent();

            // Initialize validation messages collection
            ValidationMessages = new ObservableCollection<string>();

            // Set initial state
            HasCalculatedValues = false;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles total weight text box text changed event
        /// </summary>
        private void TotalWeightTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateAndCalculate();
        }

        /// <summary>
        /// Handles cages count text box text changed event
        /// </summary>
        private void CagesCountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateAndCalculate();
        }

        /// <summary>
        /// Handles quick weight button clicks
        /// </summary>
        private void QuickWeight_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string weightStr)
            {
                if (decimal.TryParse(weightStr, out decimal weight))
                {
                    TotalWeight = weight;
                    TotalWeightTextBox.Text = weight.ToString(CultureInfo.CurrentCulture);
                }
            }
        }

        /// <summary>
        /// Handles clear button click
        /// </summary>
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            ClearValues();
        }

        #endregion

        #region Dependency Property Change Handlers

        /// <summary>
        /// Handles changes to weight-related properties
        /// </summary>
        private static void OnWeightValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WeightInputControl control)
            {
                control.ValidateAndCalculate();
            }
        }

        /// <summary>
        /// Handles changes to efficiency rating
        /// </summary>
        private static void OnEfficiencyRatingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WeightInputControl control)
            {
                control.UpdateEfficiencyBarWidth();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validates input and calculates derived values
        /// </summary>
        private void ValidateAndCalculate()
        {
            // Clear previous validation messages
            ValidationMessages.Clear();

            // Validate total weight
            ValidateTotalWeight();

            // Validate cages count
            ValidateCagesCount();

            // Calculate derived values
            CalculateDerivedValues();

            // Update display state
            UpdateDisplayState();

            // Raise events
            RaiseEvents();
        }

        /// <summary>
        /// Validates the total weight input
        /// </summary>
        private void ValidateTotalWeight()
        {
            if (TotalWeight <= 0)
            {
                ValidationMessages.Add("يجب إدخال وزن إجمالي أكبر من صفر");
            }
            else if (TotalWeight > 10000)
            {
                ValidationMessages.Add("الوزن الإجمالي كبير جداً (الحد الأقصى 10000 كيلو)");
            }
        }

        /// <summary>
        /// Validates the cages count input
        /// </summary>
        private void ValidateCagesCount()
        {
            if (CagesCount <= 0)
            {
                ValidationMessages.Add("يجب إدخال عدد أقفاص أكبر من صفر");
            }
            else if (CagesCount > 1000)
            {
                ValidationMessages.Add("عدد الأقفاص كبير جداً (الحد الأقصى 1000 قفص)");
            }
        }

        /// <summary>
        /// Calculates derived values (weight per cage, efficiency, etc.)
        /// </summary>
        private void CalculateDerivedValues()
        {
            if (TotalWeight > 0 && CagesCount > 0)
            {
                // Calculate weight per cage
                WeightPerCage = TotalWeight / CagesCount;

                // Validate weight per cage range
                if (WeightPerCage < MinWeightPerCage)
                {
                    ValidationMessages.Add($"متوسط وزن القفص ({WeightPerCage:F2} كيلو) أقل من الحد الأدنى ({MinWeightPerCage} كيلو)");
                }
                else if (WeightPerCage > MaxWeightPerCage)
                {
                    ValidationMessages.Add($"متوسط وزن القفص ({WeightPerCage:F2} كيلو) أكبر من الحد الأقصى ({MaxWeightPerCage} كيلو)");
                }

                // Calculate efficiency rating
                CalculateEfficiencyRating();

                // Update colors based on values
                UpdateColors();

                HasCalculatedValues = true;
            }
            else
            {
                WeightPerCage = 0;
                EfficiencyRating = 0;
                HasCalculatedValues = false;
            }
        }

        /// <summary>
        /// Calculates efficiency rating based on optimal weight per cage
        /// </summary>
        private void CalculateEfficiencyRating()
        {
            if (WeightPerCage > 0)
            {
                // Calculate efficiency based on deviation from optimal weight
                var deviation = Math.Abs((double)(WeightPerCage - OptimalWeightPerCage));
                var maxDeviation = Math.Max((double)(OptimalWeightPerCage - MinWeightPerCage),
                                          (double)(MaxWeightPerCage - OptimalWeightPerCage));

                var efficiency = Math.Max(0, 100 - (deviation / maxDeviation * 100));
                EfficiencyRating = Math.Min(100, efficiency);
            }
            else
            {
                EfficiencyRating = 0;
            }
        }

        /// <summary>
        /// Updates colors based on calculated values
        /// </summary>
        private void UpdateColors()
        {
            // Update efficiency color
            EfficiencyColor = EfficiencyRating switch
            {
                >= 85 => Brushes.Green,
                >= 70 => Brushes.LimeGreen,
                >= 50 => Brushes.Orange,
                >= 30 => Brushes.OrangeRed,
                _ => Brushes.Red
            };

            // Update weight per cage color
            if (WeightPerCage >= MinWeightPerCage && WeightPerCage <= MaxWeightPerCage)
            {
                WeightPerCageColor = Brushes.Green;
            }
            else
            {
                WeightPerCageColor = Brushes.Red;
            }
        }

        /// <summary>
        /// Updates the efficiency bar width
        /// </summary>
        private void UpdateEfficiencyBarWidth()
        {
            // Calculate bar width based on efficiency rating (max width of 200 pixels)
            EfficiencyBarWidth = (EfficiencyRating / 100.0) * 200.0;
        }

        /// <summary>
        /// Updates the display state of the control
        /// </summary>
        private void UpdateDisplayState()
        {
            // Update calculated values visibility
            HasCalculatedValues = TotalWeight > 0 && CagesCount > 0;
        }

        /// <summary>
        /// Raises relevant events using shared event architecture
        /// </summary>
        private void RaiseEvents()
        {
            // Raise weight values changed event
            WeightValuesChanged?.Invoke(this, new WeightValuesChangedEventArgs(TotalWeight, CagesCount, WeightPerCage));

            // Raise validation state changed event using shared ValidationStateChangedEventArgs
            var hasErrors = ValidationMessages.Any();
            var validationMessages = ValidationMessages.ToList();
            var validationArgs = new ValidationStateChangedEventArgs(
                !hasErrors,                    // isValid
                hasErrors,                     // hasErrors
                hasErrors ? ValidationMessages.Count : 0,  // errorCount
                0,                            // warningCount
                validationMessages);          // allMessages

            ValidationStateChanged?.Invoke(this, validationArgs);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Clears all input values
        /// </summary>
        public void ClearValues()
        {
            TotalWeight = 0;
            CagesCount = 0;
            TotalWeightTextBox.Text = string.Empty;
            CagesCountTextBox.Text = string.Empty;
            ValidationMessages.Clear();
            HasCalculatedValues = false;
        }

        /// <summary>
        /// Validates the current input and returns the result
        /// </summary>
        /// <returns>True if input is valid, false otherwise</returns>
        public bool ValidateInput()
        {
            ValidateAndCalculate();
            return !ValidationMessages.Any();
        }

        /// <summary>
        /// Sets the weight values programmatically
        /// </summary>
        /// <param name="totalWeight">Total weight to set</param>
        /// <param name="cagesCount">Cages count to set</param>
        public void SetValues(decimal totalWeight, int cagesCount)
        {
            TotalWeight = totalWeight;
            CagesCount = cagesCount;

            TotalWeightTextBox.Text = totalWeight.ToString(CultureInfo.CurrentCulture);
            CagesCountTextBox.Text = cagesCount.ToString(CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Focuses the total weight input
        /// </summary>
        public void FocusTotalWeight()
        {
            TotalWeightTextBox.Focus();
            TotalWeightTextBox.SelectAll();
        }

        /// <summary>
        /// Focuses the cages count input
        /// </summary>
        public void FocusCagesCount()
        {
            CagesCountTextBox.Focus();
            CagesCountTextBox.SelectAll();
        }

        /// <summary>
        /// Gets comprehensive validation summary including efficiency metrics
        /// </summary>
        /// <returns>Detailed validation and performance summary</returns>
        public string GetValidationSummary()
        {
            var summary = new System.Text.StringBuilder();

            summary.AppendLine($"حالة التحقق: {(ValidationMessages.Any() ? "غير صالح" : "صالح")}");
            summary.AppendLine($"الأخطاء: {ValidationMessages.Count}");
            summary.AppendLine($"كفاءة التحميل: {EfficiencyRating:F1}%");
            summary.AppendLine($"متوسط وزن القفص: {WeightPerCage:F2} كيلو");

            if (ValidationMessages.Any())
            {
                summary.AppendLine("\nتفاصيل الأخطاء:");
                foreach (var message in ValidationMessages)
                {
                    summary.AppendLine($"• {message}");
                }
            }

            return summary.ToString();
        }

        #endregion
    }

    /// <summary>
    /// Event arguments for weight values changed event - specific to WeightInputControl
    /// </summary>
    public class WeightValuesChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the total weight
        /// </summary>
        public decimal TotalWeight { get; }

        /// <summary>
        /// Gets the cages count
        /// </summary>
        public int CagesCount { get; }

        /// <summary>
        /// Gets the calculated weight per cage
        /// </summary>
        public decimal WeightPerCage { get; }

        /// <summary>
        /// Gets the calculated efficiency rating
        /// </summary>
        public double EfficiencyRating { get; }

        /// <summary>
        /// Initializes a new instance of WeightValuesChangedEventArgs
        /// </summary>
        /// <param name="totalWeight">Total weight value</param>
        /// <param name="cagesCount">Number of cages</param>
        /// <param name="weightPerCage">Calculated weight per cage</param>
        public WeightValuesChangedEventArgs(decimal totalWeight, int cagesCount, decimal weightPerCage)
        {
            TotalWeight = totalWeight;
            CagesCount = cagesCount;
            WeightPerCage = weightPerCage;

            // Calculate efficiency rating using optimal weight range (20-30 kg per cage)
            if (weightPerCage > 0)
            {
                const double optimalWeight = 25.0;
                const double toleranceRange = 5.0;
                var difference = Math.Abs((double)weightPerCage - optimalWeight);
                EfficiencyRating = Math.Max(0, 100 - (difference / toleranceRange * 20));
            }
            else
            {
                EfficiencyRating = 0;
            }
        }

        /// <summary>
        /// Gets a formatted summary of the weight calculation
        /// </summary>
        /// <returns>Formatted weight summary string</returns>
        public string GetFormattedSummary()
        {
            return $"الوزن الإجمالي: {TotalWeight:F2} كيلو | " +
                   $"عدد الأقفاص: {CagesCount} | " +
                   $"متوسط وزن القفص: {WeightPerCage:F2} كيلو | " +
                   $"كفاءة التحميل: {EfficiencyRating:F1}%";
        }
    }
}