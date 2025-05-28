using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PoultrySlaughterPOS.Controls
{
    /// <summary>
    /// Enterprise-grade validation summary control providing comprehensive validation state management
    /// with real-time feedback, detailed error reporting, and user-friendly validation messaging.
    /// Implements advanced dependency property patterns for seamless MVVM integration.
    /// </summary>
    public partial class ValidationSummaryControl : UserControl
    {
        #region Dependency Properties

        /// <summary>
        /// Dependency property for validation errors collection
        /// </summary>
        public static readonly DependencyProperty ValidationErrorsProperty =
            DependencyProperty.Register(
                nameof(ValidationErrors),
                typeof(ObservableCollection<string>),
                typeof(ValidationSummaryControl),
                new PropertyMetadata(null, OnValidationStateChanged));

        /// <summary>
        /// Dependency property for validation warnings collection
        /// </summary>
        public static readonly DependencyProperty ValidationWarningsProperty =
            DependencyProperty.Register(
                nameof(ValidationWarnings),
                typeof(ObservableCollection<string>),
                typeof(ValidationSummaryControl),
                new PropertyMetadata(null, OnValidationStateChanged));

        /// <summary>
        /// Dependency property for validation info collection
        /// </summary>
        public static readonly DependencyProperty ValidationInfoProperty =
            DependencyProperty.Register(
                nameof(ValidationInfo),
                typeof(ObservableCollection<string>),
                typeof(ValidationSummaryControl),
                new PropertyMetadata(null, OnValidationStateChanged));

        /// <summary>
        /// Dependency property for overall validation state
        /// </summary>
        public static readonly DependencyProperty IsValidProperty =
            DependencyProperty.Register(
                nameof(IsValid),
                typeof(bool),
                typeof(ValidationSummaryControl),
                new PropertyMetadata(true, OnValidationStateChanged));

        /// <summary>
        /// Dependency property for error presence indicator
        /// </summary>
        public static readonly DependencyProperty HasErrorsProperty =
            DependencyProperty.Register(
                nameof(HasErrors),
                typeof(bool),
                typeof(ValidationSummaryControl),
                new PropertyMetadata(false, OnValidationStateChanged));

        /// <summary>
        /// Dependency property for error count
        /// </summary>
        public static readonly DependencyProperty ErrorCountProperty =
            DependencyProperty.Register(
                nameof(ErrorCount),
                typeof(int),
                typeof(ValidationSummaryControl),
                new PropertyMetadata(0));

        /// <summary>
        /// Dependency property for warning count
        /// </summary>
        public static readonly DependencyProperty WarningCountProperty =
            DependencyProperty.Register(
                nameof(WarningCount),
                typeof(int),
                typeof(ValidationSummaryControl),
                new PropertyMetadata(0));

        /// <summary>
        /// Dependency property for last validation time
        /// </summary>
        public static readonly DependencyProperty LastValidationTimeProperty =
            DependencyProperty.Register(
                nameof(LastValidationTime),
                typeof(DateTime),
                typeof(ValidationSummaryControl),
                new PropertyMetadata(DateTime.Now));

        /// <summary>
        /// Dependency property for total rules checked count
        /// </summary>
        public static readonly DependencyProperty TotalRulesCheckedProperty =
            DependencyProperty.Register(
                nameof(TotalRulesChecked),
                typeof(int),
                typeof(ValidationSummaryControl),
                new PropertyMetadata(0));

        /// <summary>
        /// Dependency property for system status
        /// </summary>
        public static readonly DependencyProperty SystemStatusProperty =
            DependencyProperty.Register(
                nameof(SystemStatus),
                typeof(string),
                typeof(ValidationSummaryControl),
                new PropertyMetadata("جاهز"));

        /// <summary>
        /// Dependency property for auto-refresh functionality
        /// </summary>
        public static readonly DependencyProperty AutoRefreshEnabledProperty =
            DependencyProperty.Register(
                nameof(AutoRefreshEnabled),
                typeof(bool),
                typeof(ValidationSummaryControl),
                new PropertyMetadata(true));

        /// <summary>
        /// Dependency property for validation context information
        /// </summary>
        public static readonly DependencyProperty ValidationContextProperty =
            DependencyProperty.Register(
                nameof(ValidationContext),
                typeof(string),
                typeof(ValidationSummaryControl),
                new PropertyMetadata("عام"));

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the collection of validation errors
        /// </summary>
        public ObservableCollection<string> ValidationErrors
        {
            get => (ObservableCollection<string>)GetValue(ValidationErrorsProperty);
            set => SetValue(ValidationErrorsProperty, value);
        }

        /// <summary>
        /// Gets or sets the collection of validation warnings
        /// </summary>
        public ObservableCollection<string> ValidationWarnings
        {
            get => (ObservableCollection<string>)GetValue(ValidationWarningsProperty);
            set => SetValue(ValidationWarningsProperty, value);
        }

        /// <summary>
        /// Gets or sets the collection of validation information messages
        /// </summary>
        public ObservableCollection<string> ValidationInfo
        {
            get => (ObservableCollection<string>)GetValue(ValidationInfoProperty);
            set => SetValue(ValidationInfoProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the overall validation state is valid
        /// </summary>
        public bool IsValid
        {
            get => (bool)GetValue(IsValidProperty);
            set => SetValue(IsValidProperty, value);
        }

        /// <summary>
        /// Gets or sets whether there are validation errors
        /// </summary>
        public bool HasErrors
        {
            get => (bool)GetValue(HasErrorsProperty);
            set => SetValue(HasErrorsProperty, value);
        }

        /// <summary>
        /// Gets or sets the count of validation errors
        /// </summary>
        public int ErrorCount
        {
            get => (int)GetValue(ErrorCountProperty);
            set => SetValue(ErrorCountProperty, value);
        }

        /// <summary>
        /// Gets or sets the count of validation warnings
        /// </summary>
        public int WarningCount
        {
            get => (int)GetValue(WarningCountProperty);
            set => SetValue(WarningCountProperty, value);
        }

        /// <summary>
        /// Gets or sets the timestamp of the last validation check
        /// </summary>
        public DateTime LastValidationTime
        {
            get => (DateTime)GetValue(LastValidationTimeProperty);
            set => SetValue(LastValidationTimeProperty, value);
        }

        /// <summary>
        /// Gets or sets the total number of validation rules checked
        /// </summary>
        public int TotalRulesChecked
        {
            get => (int)GetValue(TotalRulesCheckedProperty);
            set => SetValue(TotalRulesCheckedProperty, value);
        }

        /// <summary>
        /// Gets or sets the current system status
        /// </summary>
        public string SystemStatus
        {
            get => (string)GetValue(SystemStatusProperty);
            set => SetValue(SystemStatusProperty, value);
        }

        /// <summary>
        /// Gets or sets whether auto-refresh is enabled
        /// </summary>
        public bool AutoRefreshEnabled
        {
            get => (bool)GetValue(AutoRefreshEnabledProperty);
            set => SetValue(AutoRefreshEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets the validation context description
        /// </summary>
        public string ValidationContext
        {
            get => (string)GetValue(ValidationContextProperty);
            set => SetValue(ValidationContextProperty, value);
        }

        #endregion

        #region Events

        /// <summary>
        /// Event raised when validation state changes
        /// </summary>
        public event EventHandler<ValidationStateChangedEventArgs>? ValidationStateChanged;

        /// <summary>
        /// Event raised when validation refresh is requested
        /// </summary>
        public event EventHandler? ValidationRefreshRequested;

        /// <summary>
        /// Event raised when validation clear is requested
        /// </summary>
        public event EventHandler? ValidationClearRequested;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the ValidationSummaryControl
        /// </summary>
        public ValidationSummaryControl()
        {
            InitializeComponent();

            // Initialize collections if not already set
            if (ValidationErrors == null)
                ValidationErrors = new ObservableCollection<string>();

            if (ValidationWarnings == null)
                ValidationWarnings = new ObservableCollection<string>();

            if (ValidationInfo == null)
                ValidationInfo = new ObservableCollection<string>();

            // Set initial state
            UpdateValidationState();
            UpdateUIState();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles refresh button click event
        /// </summary>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshValidation();
        }

        /// <summary>
        /// Handles clear button click event
        /// </summary>
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearValidation();
        }

        #endregion

        #region Dependency Property Change Handlers

        /// <summary>
        /// Handles changes to validation-related properties
        /// </summary>
        private static void OnValidationStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ValidationSummaryControl control)
            {
                control.OnValidationStateChanged();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles validation state changes and updates UI accordingly
        /// </summary>
        private void OnValidationStateChanged()
        {
            UpdateValidationState();
            UpdateUIState();
            RaiseValidationStateChangedEvent();
        }

        /// <summary>
        /// Updates the internal validation state based on current collections
        /// </summary>
        private void UpdateValidationState()
        {
            // Update counts
            ErrorCount = ValidationErrors?.Count ?? 0;
            WarningCount = ValidationWarnings?.Count ?? 0;

            // Update error state
            HasErrors = ErrorCount > 0;

            // Update overall validity
            IsValid = !HasErrors;

            // Update last validation time
            LastValidationTime = DateTime.Now;

            // Calculate total rules checked (estimated based on collections)
            TotalRulesChecked = CalculateTotalRulesChecked();

            // Update system status
            UpdateSystemStatus();
        }

        /// <summary>
        /// Updates the UI visual state based on current validation results
        /// </summary>
        private void UpdateUIState()
        {
            if (IsValid)
            {
                UpdateUIForValidState();
            }
            else
            {
                UpdateUIForInvalidState();
            }
        }

        /// <summary>
        /// Updates UI elements for valid state
        /// </summary>
        private void UpdateUIForValidState()
        {
            // Update header icon and color
            HeaderIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.CheckCircle;
            HeaderIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#28A745")!);

            // Update status badge
            StatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#28A745")!);
            StatusText.Text = "صالح";

            // Update header text color
            HeaderText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#155724")!);
        }

        /// <summary>
        /// Updates UI elements for invalid state
        /// </summary>
        private void UpdateUIForInvalidState()
        {
            // Update header icon and color
            HeaderIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.ExclamationTriangle;
            HeaderIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545")!);

            // Update status badge
            StatusBadge.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC3545")!);
            StatusText.Text = "غير صالح";

            // Update header text color
            HeaderText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#721C24")!);
        }

        /// <summary>
        /// Calculates the estimated total number of validation rules checked
        /// </summary>
        /// <returns>Total rules checked count</returns>
        private int CalculateTotalRulesChecked()
        {
            // Estimate based on the presence of different validation types
            int totalRules = 0;

            // Base validation rules (always checked)
            totalRules += 5; // Basic input validation rules

            // Add rules based on validation collections
            if (ValidationErrors?.Count > 0)
                totalRules += ValidationErrors.Count;

            if (ValidationWarnings?.Count > 0)
                totalRules += ValidationWarnings.Count * 2; // Warnings typically involve multiple checks

            if (ValidationInfo?.Count > 0)
                totalRules += ValidationInfo.Count;

            return Math.Max(totalRules, 5); // Minimum of 5 rules
        }

        /// <summary>
        /// Updates the system status based on current validation state
        /// </summary>
        private void UpdateSystemStatus()
        {
            if (IsValid)
            {
                SystemStatus = "جاهز";
            }
            else if (HasErrors)
            {
                SystemStatus = $"يوجد {ErrorCount} خطأ";
            }
            else
            {
                SystemStatus = "قيد المراجعة";
            }
        }

        /// <summary>
        /// Raises the validation state changed event with comprehensive validation information
        /// </summary>
        private void RaiseValidationStateChangedEvent()
        {
            var allMessages = GetAllValidationMessages();
            var args = new ValidationStateChangedEventArgs(
                IsValid,
                HasErrors,
                ErrorCount,
                WarningCount,
                allMessages);

            ValidationStateChanged?.Invoke(this, args);
        }

        /// <summary>
        /// Gets all validation messages combined into a single collection
        /// </summary>
        /// <returns>Combined validation messages</returns>
        private List<string> GetAllValidationMessages()
        {
            var allMessages = new List<string>();

            if (ValidationErrors != null)
                allMessages.AddRange(ValidationErrors);

            if (ValidationWarnings != null)
                allMessages.AddRange(ValidationWarnings);

            if (ValidationInfo != null)
                allMessages.AddRange(ValidationInfo);

            return allMessages;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a validation error message
        /// </summary>
        /// <param name="errorMessage">Error message to add</param>
        public void AddError(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
                return;

            ValidationErrors ??= new ObservableCollection<string>();

            if (!ValidationErrors.Contains(errorMessage))
            {
                ValidationErrors.Add(errorMessage);
                UpdateValidationState();
                UpdateUIState();
            }
        }

        /// <summary>
        /// Adds a validation warning message
        /// </summary>
        /// <param name="warningMessage">Warning message to add</param>
        public void AddWarning(string warningMessage)
        {
            if (string.IsNullOrWhiteSpace(warningMessage))
                return;

            ValidationWarnings ??= new ObservableCollection<string>();

            if (!ValidationWarnings.Contains(warningMessage))
            {
                ValidationWarnings.Add(warningMessage);
                UpdateValidationState();
                UpdateUIState();
            }
        }

        /// <summary>
        /// Adds a validation info message
        /// </summary>
        /// <param name="infoMessage">Info message to add</param>
        public void AddInfo(string infoMessage)
        {
            if (string.IsNullOrWhiteSpace(infoMessage))
                return;

            ValidationInfo ??= new ObservableCollection<string>();

            if (!ValidationInfo.Contains(infoMessage))
            {
                ValidationInfo.Add(infoMessage);
                UpdateValidationState();
                UpdateUIState();
            }
        }

        /// <summary>
        /// Clears all validation messages
        /// </summary>
        public void ClearValidation()
        {
            ValidationErrors?.Clear();
            ValidationWarnings?.Clear();
            ValidationInfo?.Clear();

            UpdateValidationState();
            UpdateUIState();

            ValidationClearRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Clears only error messages
        /// </summary>
        public void ClearErrors()
        {
            ValidationErrors?.Clear();
            UpdateValidationState();
            UpdateUIState();
        }

        /// <summary>
        /// Clears only warning messages
        /// </summary>
        public void ClearWarnings()
        {
            ValidationWarnings?.Clear();
            UpdateValidationState();
            UpdateUIState();
        }

        /// <summary>
        /// Refreshes the validation state and triggers refresh event
        /// </summary>
        public void RefreshValidation()
        {
            LastValidationTime = DateTime.Now;
            UpdateValidationState();
            UpdateUIState();

            ValidationRefreshRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Sets multiple validation errors at once
        /// </summary>
        /// <param name="errors">Collection of error messages</param>
        public void SetErrors(IEnumerable<string> errors)
        {
            ValidationErrors?.Clear();
            ValidationErrors ??= new ObservableCollection<string>();

            foreach (var error in errors.Where(e => !string.IsNullOrWhiteSpace(e)))
            {
                ValidationErrors.Add(error);
            }

            UpdateValidationState();
            UpdateUIState();
        }

        /// <summary>
        /// Sets multiple validation warnings at once
        /// </summary>
        /// <param name="warnings">Collection of warning messages</param>
        public void SetWarnings(IEnumerable<string> warnings)
        {
            ValidationWarnings?.Clear();
            ValidationWarnings ??= new ObservableCollection<string>();

            foreach (var warning in warnings.Where(w => !string.IsNullOrWhiteSpace(w)))
            {
                ValidationWarnings.Add(warning);
            }

            UpdateValidationState();
            UpdateUIState();
        }

        /// <summary>
        /// Gets the current validation summary as a formatted string
        /// </summary>
        /// <returns>Formatted validation summary</returns>
        public string GetValidationSummary()
        {
            var summary = new System.Text.StringBuilder();

            summary.AppendLine($"حالة التحقق: {(IsValid ? "صالح" : "غير صالح")}");
            summary.AppendLine($"الأخطاء: {ErrorCount}");
            summary.AppendLine($"التحذيرات: {WarningCount}");
            summary.AppendLine($"آخر فحص: {LastValidationTime:HH:mm:ss}");

            if (ValidationErrors?.Count > 0)
            {
                summary.AppendLine("\nالأخطاء:");
                foreach (var error in ValidationErrors)
                {
                    summary.AppendLine($"- {error}");
                }
            }

            if (ValidationWarnings?.Count > 0)
            {
                summary.AppendLine("\nالتحذيرات:");
                foreach (var warning in ValidationWarnings)
                {
                    summary.AppendLine($"- {warning}");
                }
            }

            return summary.ToString();
        }

        /// <summary>
        /// Validates the control state and updates all validation properties
        /// </summary>
        /// <returns>True if validation passes, false otherwise</returns>
        public bool ValidateAndRefresh()
        {
            RefreshValidation();
            return IsValid;
        }

        #endregion
    }

    /// <summary>
    /// Enhanced event arguments for validation state changes with comprehensive state information
    /// specific to ValidationSummaryControl requirements
    /// </summary>
    public class ValidationStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets whether the overall validation state is valid
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets whether there are validation errors
        /// </summary>
        public bool HasErrors { get; }

        /// <summary>
        /// Gets the count of validation errors
        /// </summary>
        public int ErrorCount { get; }

        /// <summary>
        /// Gets the count of validation warnings
        /// </summary>
        public int WarningCount { get; }

        /// <summary>
        /// Gets all validation messages
        /// </summary>
        public IReadOnlyList<string> AllMessages { get; }

        /// <summary>
        /// Gets the timestamp when validation state changed
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets the primary validation message for display
        /// </summary>
        public string PrimaryMessage { get; }

        /// <summary>
        /// Initializes a new instance of ValidationStateChangedEventArgs with comprehensive validation information
        /// </summary>
        /// <param name="isValid">Overall validation state</param>
        /// <param name="hasErrors">Whether errors exist</param>
        /// <param name="errorCount">Count of errors</param>
        /// <param name="warningCount">Count of warnings</param>
        /// <param name="allMessages">All validation messages</param>
        public ValidationStateChangedEventArgs(
            bool isValid,
            bool hasErrors,
            int errorCount,
            int warningCount,
            List<string> allMessages)
        {
            IsValid = isValid;
            HasErrors = hasErrors;
            ErrorCount = errorCount;
            WarningCount = warningCount;
            AllMessages = allMessages.AsReadOnly();
            Timestamp = DateTime.Now;
            PrimaryMessage = hasErrors && allMessages.Count > 0
                ? allMessages[0]
                : isValid ? "Validation successful" : "No validation messages";
        }

        /// <summary>
        /// Simplified constructor for basic validation state changes
        /// </summary>
        /// <param name="hasError">Whether there is a validation error</param>
        /// <param name="message">The validation message</param>
        public ValidationStateChangedEventArgs(bool hasError, string message)
            : this(!hasError, hasError, hasError ? 1 : 0, 0, new List<string> { message })
        {
        }

        /// <summary>
        /// Gets a formatted summary of all validation messages
        /// </summary>
        /// <returns>Formatted validation summary</returns>
        public string GetFormattedSummary()
        {
            if (!HasErrors && IsValid)
                return "All validation checks passed successfully.";

            var summary = new System.Text.StringBuilder();
            summary.AppendLine($"Validation Summary (Errors: {ErrorCount}, Warnings: {WarningCount}):");

            foreach (var message in AllMessages)
            {
                summary.AppendLine($"• {message}");
            }

            return summary.ToString();
        }
    }
}