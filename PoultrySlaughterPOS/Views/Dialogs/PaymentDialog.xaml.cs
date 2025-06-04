// PoultrySlaughterPOS/Views/Dialogs/PaymentDialog.xaml.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Models;
using PoultrySlaughterPOS.Services;
using PoultrySlaughterPOS.Services.Repositories;
using PoultrySlaughterPOS.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace PoultrySlaughterPOS.Views.Dialogs
{
    /// <summary>
    /// Enterprise-grade Payment Dialog for comprehensive debt settlement operations.
    /// Implements MVVM pattern with minimal code-behind for optimal maintainability
    /// and testability. Supports keyboard navigation and accessibility features.
    /// </summary>
    public partial class PaymentDialog : Window
    {
        #region Private Fields

        private readonly ILogger<PaymentDialog> _logger;
        private PaymentDialogViewModel? _viewModel;
        private bool _isDialogInitialized = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes PaymentDialog with comprehensive event handling and UI setup
        /// </summary>
        public PaymentDialog()
        {
            try
            {
                InitializeComponent();

                // Initialize logger if available through service provider
                _logger = App.Services?.GetService<ILogger<PaymentDialog>>()
                          ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PaymentDialog>.Instance;

                ConfigureDialog();
                ConfigureEventHandlers();

                _logger.LogDebug("PaymentDialog initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing PaymentDialog: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Constructor with ViewModel injection for dependency injection support
        /// </summary>
        /// <param name="viewModel">Pre-configured PaymentDialogViewModel instance</param>
        public PaymentDialog(PaymentDialogViewModel viewModel) : this()
        {
            SetViewModel(viewModel);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the ViewModel and initializes the dialog with customer data
        /// </summary>
        /// <param name="viewModel">PaymentDialogViewModel instance</param>
        public void SetViewModel(PaymentDialogViewModel viewModel)
        {
            try
            {
                _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

                // Set DataContext on UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DataContext = _viewModel;
                });

                _logger.LogInformation("ViewModel set successfully for PaymentDialog");

                // Initialize the dialog asynchronously
                _ = InitializeDialogAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting ViewModel for PaymentDialog");
                throw;
            }
        }

        /// <summary>
        /// Static factory method to create and show payment dialog with proper DI
        /// </summary>
        /// <param name="serviceProvider">Service provider for dependency injection</param>
        /// <param name="owner">Owner window for modal display</param>
        /// <param name="customer">Customer for payment processing</param>
        /// <returns>Created payment if successful, null if cancelled</returns>
        public static async Task<Payment?> ShowPaymentDialogAsync(
            IServiceProvider serviceProvider,
            Window? owner,
            Customer customer)
        {
            try
            {
                if (serviceProvider == null)
                    throw new ArgumentNullException(nameof(serviceProvider));

                if (customer == null)
                    throw new ArgumentNullException(nameof(customer));

                // Create ViewModel with DI
                var unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
                var logger = serviceProvider.GetService<ILogger<PaymentDialogViewModel>>()
                            ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PaymentDialogViewModel>.Instance;
                var errorHandlingService = serviceProvider.GetRequiredService<IErrorHandlingService>();

                var viewModel = new PaymentDialogViewModel(customer, unitOfWork, logger, errorHandlingService);

                // Create and configure dialog
                var dialog = new PaymentDialog(viewModel)
                {
                    Owner = owner,
                    WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
                };

                // Refresh customer debt information before showing
                await viewModel.RefreshCustomerDebtAsync();

                // Show dialog and return result
                var result = dialog.ShowDialog();
                return result == true ? viewModel.CreatedPayment : null;
            }
            catch (Exception ex)
            {
                var logger = serviceProvider.GetService<ILogger<PaymentDialog>>()
                            ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PaymentDialog>.Instance;

                logger.LogError(ex, "Error creating and showing PaymentDialog for customer {CustomerName}",
                    customer?.CustomerName);

                MessageBox.Show(
                    "حدث خطأ أثناء فتح نافذة تسديد الدين. يرجى المحاولة مرة أخرى.",
                    "خطأ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return null;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the Loaded event to initialize the dialog when it becomes visible
        /// </summary>
        private async void PaymentDialog_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isDialogInitialized)
                {
                    await InitializeDialogAsync();
                }

                // Focus on payment amount input using the corrected name
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    PaymentAmountInput.IsEnabled = true;
                    PaymentAmountInput.IsReadOnly = false;
                    PaymentAmountInput.Focus();
                    PaymentAmountInput.SelectAll();
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during PaymentDialog Loaded event");
            }
        }

        /// <summary>
        /// Handles keyboard shortcuts for enhanced user productivity
        /// </summary>
        private void PaymentDialog_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (_viewModel == null) return;

                switch (e.Key)
                {
                    case Key.Enter:
                        // Save payment on Enter (if valid)
                        if (_viewModel.CanSavePayment && _viewModel.SavePaymentCommand.CanExecute(null))
                        {
                            _viewModel.SavePaymentCommand.Execute(null);
                            e.Handled = true;
                        }
                        break;

                    case Key.Escape:
                        // Cancel on Escape
                        if (_viewModel.CancelCommand.CanExecute(null))
                        {
                            _viewModel.CancelCommand.Execute(null);
                            e.Handled = true;
                        }
                        break;

                    case Key.F1:
                        // Set 25% of debt
                        _viewModel.SetPercentageCommand.Execute("0.25");
                        e.Handled = true;
                        break;

                    case Key.F2:
                        // Set 50% of debt
                        _viewModel.SetPercentageCommand.Execute("0.50");
                        e.Handled = true;
                        break;

                    case Key.F3:
                        // Set 75% of debt
                        _viewModel.SetPercentageCommand.Execute("0.75");
                        e.Handled = true;
                        break;

                    case Key.F4:
                        // Set full amount
                        _viewModel.SetFullAmountCommand.Execute(null);
                        e.Handled = true;
                        break;

                    default:
                        // Handle Ctrl key combinations
                        if (Keyboard.Modifiers == ModifierKeys.Control)
                        {
                            switch (e.Key)
                            {
                                case Key.S:
                                    // Save payment (Ctrl+S)
                                    if (_viewModel.CanSavePayment && _viewModel.SavePaymentCommand.CanExecute(null))
                                    {
                                        _viewModel.SavePaymentCommand.Execute(null);
                                        e.Handled = true;
                                    }
                                    break;
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling keyboard shortcut: {Key}", e.Key);
            }
        }

        /// <summary>
        /// Handles window closing to ensure proper cleanup
        /// </summary>
        private void PaymentDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // Allow closing if processing is not in progress
                if (_viewModel?.IsProcessing == true)
                {
                    var result = MessageBox.Show(
                        "جاري معالجة الدفعة. هل تريد إلغاء العملية؟",
                        "تأكيد الإلغاء",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }
                }

                _logger.LogDebug("PaymentDialog closing");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during PaymentDialog closing");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Configures dialog properties and behavior
        /// </summary>
        private void ConfigureDialog()
        {
            try
            {
                // Set dialog properties
                WindowStyle = WindowStyle.SingleBorderWindow;
                ResizeMode = ResizeMode.NoResize;
                ShowInTaskbar = false;
                Topmost = false;

                // Set minimum and maximum sizes
                MinWidth = 480;
                MinHeight = 550;
                MaxWidth = 600;
                MaxHeight = 700;

                _logger.LogDebug("Dialog properties configured successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error configuring dialog properties");
            }
        }

        /// <summary>
        /// Configures event handlers for UI elements
        /// </summary>
        private void ConfigureEventHandlers()
        {
            try
            {
                // Subscribe to dialog events
                Loaded += PaymentDialog_Loaded;
                KeyDown += PaymentDialog_KeyDown;
                Closing += PaymentDialog_Closing;

                // Enable keyboard focus for the dialog
                Focusable = true;

                _logger.LogDebug("Event handlers configured successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring event handlers");
                throw;
            }
        }

        /// <summary>
        /// Enhanced dialog initialization with proper error handling
        /// </summary>
        private async Task InitializeDialogAsync()
        {
            try
            {
                if (_viewModel != null && !_isDialogInitialized)
                {
                    // Ensure we're on the UI thread
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        // Refresh customer debt information
                        await _viewModel.RefreshCustomerDebtAsync();

                        _isDialogInitialized = true;

                        // Ensure the payment amount input is focusable and enabled
                        if (PaymentAmountInput != null)
                        {
                            PaymentAmountInput.IsEnabled = true;
                            PaymentAmountInput.IsReadOnly = false;
                            PaymentAmountInput.Focus();
                            PaymentAmountInput.SelectAll();
                        }
                    });

                    _logger.LogDebug("PaymentDialog initialized successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during PaymentDialog initialization");

                // Show user-friendly error message
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(
                        "حدث خطأ أثناء تحميل بيانات الزبون. يرجى المحاولة مرة أخرى.",
                        "خطأ في التحميل",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });

                // Close dialog on initialization failure
                Close();
            }
        }

        #endregion
    }
}