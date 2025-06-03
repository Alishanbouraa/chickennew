using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Models;
using PoultrySlaughterPOS.ViewModels;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace PoultrySlaughterPOS.Views.Dialogs
{
    /// <summary>
    /// Enterprise-grade customer creation dialog implementing comprehensive MVVM patterns,
    /// dependency injection integration, and advanced user experience optimizations.
    /// Provides seamless integration with the POS workflow for real-time customer management.
    /// </summary>
    public partial class AddCustomerDialog : Window
    {
        #region Private Fields

        private readonly AddCustomerDialogViewModel _viewModel;
        private readonly ILogger<AddCustomerDialog> _logger;
        private bool _isClosingProgrammatically = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for dependency injection with comprehensive ViewModel integration
        /// </summary>
        /// <param name="viewModel">Customer dialog ViewModel injected via DI container</param>
        /// <param name="logger">Logger instance for diagnostic and error tracking</param>
        public AddCustomerDialog(AddCustomerDialogViewModel viewModel, ILogger<AddCustomerDialog> logger)
        {
            InitializeComponent();

            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Establish MVVM data binding
            DataContext = _viewModel;

            // Configure dialog properties for optimal user experience
            ConfigureDialogProperties();

            // Wire up event handlers for advanced dialog management
            WireUpEventHandlers();

            _logger.LogInformation("AddCustomerDialog initialized with enterprise-grade MVVM architecture");
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Returns the created/updated customer if dialog completed successfully, null otherwise
        /// </summary>
        public Customer? CreatedCustomer => _viewModel.CreatedCustomer;

        /// <summary>
        /// Indicates whether the dialog was completed successfully (true) or cancelled (false)
        /// </summary>
        public bool WasSuccessful => _viewModel.DialogResult == true;

        /// <summary>
        /// Access to the underlying ViewModel for advanced scenarios
        /// </summary>
        public AddCustomerDialogViewModel ViewModel => _viewModel;

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Factory method for creating and showing new customer dialog
        /// FIXED: Complete implementation using ViewModel pattern
        /// </summary>
        /// <param name="serviceProvider">Service provider for dependency resolution</param>
        /// <param name="owner">Parent window for modal positioning</param>
        /// <returns>Created customer or null if cancelled</returns>
        public static async Task<Customer?> ShowNewCustomerDialogAsync(IServiceProvider serviceProvider, Window? owner = null)
        {
            try
            {
                var logger = serviceProvider.GetService<ILogger<AddCustomerDialog>>();
                logger?.LogInformation("Creating new customer dialog via factory method");

                // Resolve dialog from DI container
                var dialog = serviceProvider.GetRequiredService<AddCustomerDialog>();

                // Configure for new customer mode
                dialog._viewModel.ConfigureForNewCustomer();
                dialog.Title = dialog._viewModel.DialogTitle;

                // Show dialog and return result
                return await dialog.ShowDialogInternalAsync(owner);
            }
            catch (Exception ex)
            {
                var logger = serviceProvider.GetService<ILogger<AddCustomerDialog>>();
                logger?.LogError(ex, "Error in ShowNewCustomerDialogAsync factory method");

                MessageBox.Show($"خطأ في إنشاء نافذة إضافة الزبون:\n{ex.Message}",
                               "خطأ في النظام",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
                return null;
            }
        }

        /// <summary>
        /// Shows the edit customer dialog with pre-populated customer data
        /// FIXED: Complete implementation using ViewModel pattern instead of direct control access
        /// </summary>
        /// <param name="serviceProvider">Service provider for dependency injection</param>
        /// <param name="owner">Owner window for modal positioning</param>
        /// <param name="customer">Customer to edit</param>
        /// <returns>Updated customer if saved, null if cancelled</returns>
        public static async Task<Customer?> ShowEditCustomerDialogAsync(
            IServiceProvider serviceProvider,
            Window? owner,
            Customer customer)
        {
            try
            {
                if (customer == null)
                    throw new ArgumentNullException(nameof(customer));

                var logger = serviceProvider.GetService<ILogger<AddCustomerDialog>>();
                logger?.LogInformation("Creating edit customer dialog for customer {CustomerId}", customer.CustomerId);

                // Resolve dialog from DI container
                var dialog = serviceProvider.GetRequiredService<AddCustomerDialog>();

                // FIXED: Use ViewModel to load customer data instead of direct control access
                dialog._viewModel.LoadCustomerForEdit(customer);
                dialog.Title = dialog._viewModel.DialogTitle;

                // Show dialog and return result
                return await dialog.ShowDialogInternalAsync(owner);
            }
            catch (Exception ex)
            {
                var logger = serviceProvider.GetService<ILogger<AddCustomerDialog>>();
                logger?.LogError(ex, "Error showing edit customer dialog for customer {CustomerId}", customer?.CustomerId);

                MessageBox.Show(
                    "حدث خطأ أثناء فتح نافذة تعديل الزبون. يرجى المحاولة مرة أخرى.",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return null;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the dialog modally and returns the created/updated customer if successful
        /// </summary>
        /// <param name="owner">Parent window for modal positioning</param>
        /// <returns>Created/updated customer or null if cancelled</returns>
        public async Task<Customer?> ShowDialogAsync(Window? owner = null)
        {
            return await ShowDialogInternalAsync(owner);
        }

        /// <summary>
        /// Resets the dialog to initial state for reuse scenarios
        /// </summary>
        public async Task ResetDialogStateAsync()
        {
            try
            {
                _viewModel.ResetDialog();
                _logger.LogDebug("AddCustomerDialog state reset successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error resetting dialog state");
            }
        }

        /// <summary>
        /// Programmatically closes the dialog with success result
        /// </summary>
        public void CloseWithSuccess()
        {
            try
            {
                _isClosingProgrammatically = true;
                DialogResult = true;
                _logger.LogDebug("AddCustomerDialog closed programmatically with success");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing dialog with success");
            }
        }

        /// <summary>
        /// Programmatically closes the dialog with cancellation result
        /// </summary>
        public void CloseWithCancellation()
        {
            try
            {
                _isClosingProgrammatically = true;
                DialogResult = false;
                _logger.LogDebug("AddCustomerDialog closed programmatically with cancellation");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing dialog with cancellation");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Internal method to show dialog with proper configuration
        /// </summary>
        private async Task<Customer?> ShowDialogInternalAsync(Window? owner)
        {
            try
            {
                _logger.LogInformation("Displaying AddCustomerDialog modally");

                // Set owner for proper modal behavior
                if (owner != null)
                {
                    Owner = owner;
                    WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else
                {
                    WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }

                // Set initial focus for optimal user experience
                Loaded += (s, e) => SetInitialFocus();

                // Show dialog modally
                var dialogResult = ShowDialog();

                _logger.LogInformation("AddCustomerDialog closed with result: {DialogResult}, Customer created: {CustomerCreated}",
                    dialogResult, CreatedCustomer != null);

                return dialogResult == true ? CreatedCustomer : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error displaying AddCustomerDialog");
                MessageBox.Show($"خطأ في عرض نافذة الزبون:\n{ex.Message}",
                               "خطأ",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
                return null;
            }
        }

        /// <summary>
        /// Configures dialog-specific properties for optimal user experience
        /// </summary>
        private void ConfigureDialogProperties()
        {
            try
            {
                // Prevent resizing for consistent experience
                ResizeMode = ResizeMode.NoResize;

                // Set appropriate minimum size
                MinWidth = 480;
                MinHeight = 520;

                // Configure for modal behavior
                ShowInTaskbar = false;

                _logger.LogDebug("Dialog properties configured successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error configuring dialog properties");
            }
        }

        /// <summary>
        /// Wires up comprehensive event handlers for advanced dialog management
        /// </summary>
        private void WireUpEventHandlers()
        {
            try
            {
                // Window event handlers
                Loaded += AddCustomerDialog_Loaded;
                Closing += AddCustomerDialog_Closing;
                KeyDown += AddCustomerDialog_KeyDown;

                // ViewModel event handlers
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;

                _logger.LogDebug("Event handlers wired up successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error wiring up event handlers");
            }
        }

        /// <summary>
        /// Sets initial focus to the first input field for optimal user experience
        /// </summary>
        private void SetInitialFocus()
        {
            try
            {
                // Try to find CustomerNameTextBox and set focus
                if (FindName("CustomerNameTextBox") is FrameworkElement customerNameTextBox)
                {
                    customerNameTextBox.Focus();
                    if (customerNameTextBox is System.Windows.Controls.TextBox textBox)
                    {
                        textBox.SelectAll();
                    }
                    _logger.LogDebug("Initial focus set to customer name field");
                }
                else
                {
                    // Fallback to first focusable element
                    MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
                    _logger.LogDebug("Initial focus set to first focusable element");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error setting initial focus");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles window loaded event with initialization and focus management
        /// </summary>
        private void AddCustomerDialog_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                SetInitialFocus();
                _logger.LogDebug("AddCustomerDialog loaded and initial focus set");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in AddCustomerDialog_Loaded");
            }
        }

        /// <summary>
        /// Handles window closing event with validation and cleanup
        /// </summary>
        private void AddCustomerDialog_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                // If closing programmatically, allow without validation
                if (_isClosingProgrammatically)
                {
                    return;
                }

                // Check if user has unsaved changes
                if (HasUnsavedChanges() && !_viewModel.IsSaving)
                {
                    var result = MessageBox.Show(
                        "هل تريد إغلاق النافذة بدون حفظ التغييرات؟",
                        "تأكيد الإغلاق",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question,
                        MessageBoxResult.No);

                    if (result == MessageBoxResult.No)
                    {
                        e.Cancel = true;
                        _logger.LogDebug("Dialog closing cancelled by user to preserve unsaved changes");
                        return;
                    }
                }

                // Set appropriate dialog result
                if (DialogResult == null)
                {
                    DialogResult = false;
                }

                _logger.LogInformation("AddCustomerDialog closing with DialogResult: {DialogResult}", DialogResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddCustomerDialog_Closing");
            }
        }

        /// <summary>
        /// Handles ViewModel property changes for dialog state management
        /// </summary>
        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            try
            {
                switch (e.PropertyName)
                {
                    case nameof(AddCustomerDialogViewModel.DialogResult):
                        HandleDialogResultChanged();
                        break;

                    case nameof(AddCustomerDialogViewModel.IsSaving):
                        HandleSavingStateChanged();
                        break;

                    case nameof(AddCustomerDialogViewModel.HasValidationErrors):
                        HandleValidationStateChanged();
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling ViewModel property change: {PropertyName}", e.PropertyName);
            }
        }

        /// <summary>
        /// Handles keyboard shortcuts for improved user experience
        /// </summary>
        private void AddCustomerDialog_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                switch (e.Key)
                {
                    case Key.Escape:
                        if (!_viewModel.IsSaving)
                        {
                            _viewModel.CancelDialogCommand.Execute(null);
                        }
                        e.Handled = true;
                        break;

                    case Key.F5:
                        _viewModel.ClearFieldsCommand.Execute(null);
                        e.Handled = true;
                        break;

                    case Key.Enter when (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control:
                        if (_viewModel.CanSaveCustomer)
                        {
                            _viewModel.SaveCustomerCommand.Execute(null);
                        }
                        e.Handled = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling keyboard shortcut: {Key}", e.Key);
            }
        }

        /// <summary>
        /// Handles ViewModel DialogResult changes for proper dialog closure
        /// </summary>
        private void HandleDialogResultChanged()
        {
            try
            {
                var vmDialogResult = _viewModel.DialogResult;

                if (vmDialogResult.HasValue)
                {
                    _isClosingProgrammatically = true;
                    DialogResult = vmDialogResult.Value;

                    _logger.LogDebug("Dialog result set from ViewModel: {DialogResult}", vmDialogResult.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling dialog result change");
            }
        }

        /// <summary>
        /// Handles ViewModel saving state changes for UI feedback
        /// </summary>
        private void HandleSavingStateChanged()
        {
            try
            {
                if (_viewModel.IsSaving)
                {
                    Cursor = Cursors.Wait;
                }
                else
                {
                    Cursor = Cursors.Arrow;
                }

                _logger.LogDebug("Saving state changed: {IsSaving}", _viewModel.IsSaving);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling saving state change");
            }
        }

        /// <summary>
        /// Handles validation state changes for user feedback
        /// </summary>
        private void HandleValidationStateChanged()
        {
            try
            {
                _logger.LogDebug("Validation state changed: {HasErrors}", _viewModel.HasValidationErrors);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling validation state change");
            }
        }

        /// <summary>
        /// Determines if the user has unsaved changes that would be lost on close
        /// </summary>
        /// <returns>True if there are unsaved changes</returns>
        private bool HasUnsavedChanges()
        {
            try
            {
                return !string.IsNullOrWhiteSpace(_viewModel.CustomerName) ||
                       !string.IsNullOrWhiteSpace(_viewModel.PhoneNumber) ||
                       !string.IsNullOrWhiteSpace(_viewModel.Address);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking for unsaved changes");
                return false;
            }
        }

        #endregion
    }
}