using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PoultrySlaughterPOS.Views
{
    /// <summary>
    /// Enterprise-grade Point of Sale view implementation following MVVM architecture.
    /// Provides invoice creation interface with comprehensive weight calculations,
    /// customer management, and real-time financial computations for poultry sales operations.
    /// </summary>
    public partial class POSView : UserControl
    {
        #region Private Fields

        private readonly ILogger<POSView> _logger;
        private POSViewModel _viewModel;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the POS view with dependency injection support and MVVM binding
        /// </summary>
        /// <param name="viewModel">POS ViewModel injected via dependency injection</param>
        /// <param name="logger">Logger instance for diagnostic and error tracking</param>
        public POSView(POSViewModel viewModel, ILogger<POSView> logger)
        {
            InitializeComponent();

            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Set DataContext for MVVM binding
            DataContext = _viewModel;

            // Wire up event handlers for enhanced user experience
            Loaded += POSView_Loaded;
            Unloaded += POSView_Unloaded;

            _logger.LogInformation("POS View initialized successfully with MVVM architecture");
        }

        /// <summary>
        /// Property to access the ViewModel externally (for MainWindow integration)
        /// </summary>
        public POSViewModel ViewModel => _viewModel;

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles view loaded event with initialization of POS data and focus management
        /// </summary>
        private async void POSView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogDebug("POS View loaded, initializing data and UI state");

                // Initialize ViewModel data asynchronously
                await _viewModel.InitializeAsync();

                // Set initial focus to customer selection for improved UX
                CustomerComboBox.Focus();

                // Configure numeric input validation for weight and price fields
                ConfigureNumericInputValidation();

                _logger.LogInformation("POS View loaded successfully with data initialization completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during POS View load initialization");

                MessageBox.Show($"خطأ في تحميل صفحة نقطة البيع:\n{ex.Message}",
                               "خطأ في التحميل",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Handles view unloaded event with proper cleanup and resource disposal
        /// </summary>
        private void POSView_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogDebug("POS View unloading, performing cleanup operations");

                // Perform cleanup operations through ViewModel
                _viewModel?.Cleanup();

                _logger.LogInformation("POS View unloaded successfully with cleanup completed");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during POS View unload cleanup");
            }
        }

        #endregion

        #region Input Validation and User Experience Enhancement

        /// <summary>
        /// Configures numeric input validation for weight and price fields
        /// to ensure data integrity and prevent invalid input
        /// </summary>
        private void ConfigureNumericInputValidation()
        {
            try
            {
                // Configure numeric validation for weight fields
                GrossWeightTextBox.PreviewTextInput += NumericTextBox_PreviewTextInput;
                GrossWeightTextBox.PreviewKeyDown += NumericTextBox_PreviewKeyDown;
                GrossWeightTextBox.LostFocus += WeightTextBox_LostFocus;

                CageWeightTextBox.PreviewTextInput += NumericTextBox_PreviewTextInput;
                CageWeightTextBox.PreviewKeyDown += NumericTextBox_PreviewKeyDown;
                CageWeightTextBox.LostFocus += WeightTextBox_LostFocus;

                UnitPriceTextBox.PreviewTextInput += NumericTextBox_PreviewTextInput;
                UnitPriceTextBox.PreviewKeyDown += NumericTextBox_PreviewKeyDown;
                UnitPriceTextBox.LostFocus += PriceTextBox_LostFocus;

                DiscountTextBox.PreviewTextInput += NumericTextBox_PreviewTextInput;
                DiscountTextBox.PreviewKeyDown += NumericTextBox_PreviewKeyDown;
                DiscountTextBox.LostFocus += DiscountTextBox_LostFocus;

                // Configure integer validation for cage count
                CageCountTextBox.PreviewTextInput += IntegerTextBox_PreviewTextInput;
                CageCountTextBox.PreviewKeyDown += NumericTextBox_PreviewKeyDown;

                _logger.LogDebug("Numeric input validation configured for all POS input fields");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring numeric input validation");
            }
        }

        /// <summary>
        /// Validates numeric input for decimal fields (weights, prices)
        /// </summary>
        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                var textBox = sender as TextBox;
                var proposedText = textBox.Text + e.Text;

                // Allow decimal numbers with up to 2 decimal places
                if (!IsValidDecimalInput(proposedText))
                {
                    e.Handled = true;
                    _logger.LogDebug("Invalid numeric input blocked: {Input}", proposedText);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in numeric input validation");
                e.Handled = true;
            }
        }

        /// <summary>
        /// Validates integer input for count fields
        /// </summary>
        private void IntegerTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                var textBox = sender as TextBox;
                var proposedText = textBox.Text + e.Text;

                // Allow only positive integers
                if (!IsValidIntegerInput(proposedText))
                {
                    e.Handled = true;
                    _logger.LogDebug("Invalid integer input blocked: {Input}", proposedText);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in integer input validation");
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles special key inputs for numeric fields (backspace, delete, etc.)
        /// </summary>
        private void NumericTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Allow navigation and editing keys
            if (e.Key == Key.Back || e.Key == Key.Delete ||
                e.Key == Key.Tab || e.Key == Key.Enter ||
                e.Key == Key.Left || e.Key == Key.Right ||
                e.Key == Key.Home || e.Key == Key.End)
            {
                return;
            }

            // Allow Ctrl+A, Ctrl+C, Ctrl+V, Ctrl+X
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.Key == Key.A || e.Key == Key.C || e.Key == Key.V || e.Key == Key.X)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Handles weight field focus lost events for automatic calculations
        /// </summary>
        private void WeightTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                var textBox = sender as TextBox;
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = "0.00";
                }

                // Trigger ViewModel calculation update
                _viewModel?.RecalculateInvoiceTotals();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in weight field focus lost handler");
            }
        }

        /// <summary>
        /// Handles price field focus lost events with validation
        /// </summary>
        private void PriceTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                var textBox = sender as TextBox;
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = "0.00";
                }

                // Trigger ViewModel calculation update
                _viewModel?.RecalculateInvoiceTotals();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in price field focus lost handler");
            }
        }

        /// <summary>
        /// Handles discount field focus lost events with percentage validation
        /// </summary>
        private void DiscountTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                var textBox = sender as TextBox;
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = "0.00";
                }
                else if (decimal.TryParse(textBox.Text, out decimal discount) && discount > 100)
                {
                    textBox.Text = "100.00";
                    MessageBox.Show("نسبة الخصم لا يمكن أن تتجاوز 100%", "تنبيه",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                // Trigger ViewModel calculation update
                _viewModel?.RecalculateInvoiceTotals();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in discount field focus lost handler");
            }
        }

        #endregion

        #region Input Validation Helper Methods

        /// <summary>
        /// Validates decimal input format and range
        /// </summary>
        /// <param name="input">Input string to validate</param>
        /// <returns>True if input is valid decimal format</returns>
        private bool IsValidDecimalInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return true;

            // Check for valid decimal format
            if (!decimal.TryParse(input, out decimal result))
                return false;

            // Check for reasonable range (0 to 999999.99)
            if (result < 0 || result > 999999.99m)
                return false;

            // Check decimal places (max 2)
            var parts = input.Split('.');
            if (parts.Length > 1 && parts[1].Length > 2)
                return false;

            return true;
        }

        /// <summary>
        /// Validates integer input format and range
        /// </summary>
        /// <param name="input">Input string to validate</param>
        /// <returns>True if input is valid integer format</returns>
        private bool IsValidIntegerInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return true;

            // Check for valid integer format
            if (!int.TryParse(input, out int result))
                return false;

            // Check for reasonable range (0 to 9999)
            if (result < 0 || result > 9999)
                return false;

            return true;
        }

        #endregion

        #region Public Methods for External Integration

        /// <summary>
        /// Programmatically focuses on the customer selection field
        /// </summary>
        public void FocusCustomerSelection()
        {
            CustomerComboBox.Focus();
        }

        /// <summary>
        /// Programmatically focuses on the weight input field
        /// </summary>
        public void FocusWeightInput()
        {
            GrossWeightTextBox.Focus();
        }

        /// <summary>
        /// Validates all input fields and returns validation result
        /// </summary>
        /// <returns>True if all inputs are valid</returns>
        public bool ValidateAllInputs()
        {
            return _viewModel?.ValidateCurrentInvoice() ?? false;
        }

        /// <summary>
        /// Clears all input fields and resets the form
        /// </summary>
        public void ClearAllInputs()
        {
            _viewModel?.ResetCurrentInvoice();
            CustomerComboBox.Focus();
        }

        #endregion
    }
}