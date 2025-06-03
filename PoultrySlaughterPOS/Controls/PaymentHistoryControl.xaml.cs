using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PoultrySlaughterPOS.Controls
{
    /// <summary>
    /// Enterprise-grade Payment History Control providing comprehensive payment tracking,
    /// financial analysis, and payment management capabilities with advanced filtering and reporting features.
    /// Implements professional financial management standards with real-time payment processing integration.
    /// </summary>
    public partial class PaymentHistoryControl : UserControl, INotifyPropertyChanged
    {
        #region Dependency Properties

        /// <summary>
        /// Customer for payment history tracking
        /// </summary>
        public static readonly DependencyProperty CustomerProperty =
            DependencyProperty.Register(
                nameof(Customer),
                typeof(Customer),
                typeof(PaymentHistoryControl),
                new PropertyMetadata(null, OnCustomerChanged));

        /// <summary>
        /// Customer name for display purposes
        /// </summary>
        public static readonly DependencyProperty CustomerNameProperty =
            DependencyProperty.Register(
                nameof(CustomerName),
                typeof(string),
                typeof(PaymentHistoryControl),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Start date for payment history period
        /// </summary>
        public static readonly DependencyProperty StartDateProperty =
            DependencyProperty.Register(
                nameof(StartDate),
                typeof(DateTime),
                typeof(PaymentHistoryControl),
                new PropertyMetadata(DateTime.Today.AddMonths(-6), OnDateRangeChanged));

        /// <summary>
        /// End date for payment history period
        /// </summary>
        public static readonly DependencyProperty EndDateProperty =
            DependencyProperty.Register(
                nameof(EndDate),
                typeof(DateTime),
                typeof(PaymentHistoryControl),
                new PropertyMetadata(DateTime.Today, OnDateRangeChanged));

        /// <summary>
        /// Loading state indicator for async operations
        /// </summary>
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(
                nameof(IsLoading),
                typeof(bool),
                typeof(PaymentHistoryControl),
                new PropertyMetadata(false, OnIsLoadingChanged));

        #endregion

        #region Public Properties

        /// <summary>
        /// Customer for payment history tracking
        /// </summary>
        public Customer? Customer
        {
            get => (Customer?)GetValue(CustomerProperty);
            set => SetValue(CustomerProperty, value);
        }

        /// <summary>
        /// Customer name for display purposes
        /// </summary>
        public string CustomerName
        {
            get => (string)GetValue(CustomerNameProperty);
            set => SetValue(CustomerNameProperty, value);
        }

        /// <summary>
        /// Start date for payment history period
        /// </summary>
        public DateTime StartDate
        {
            get => (DateTime)GetValue(StartDateProperty);
            set => SetValue(StartDateProperty, value);
        }

        /// <summary>
        /// End date for payment history period
        /// </summary>
        public DateTime EndDate
        {
            get => (DateTime)GetValue(EndDateProperty);
            set => SetValue(EndDateProperty, value);
        }

        /// <summary>
        /// Loading state indicator for async operations
        /// </summary>
        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        /// <summary>
        /// Collection of payment records for display
        /// </summary>
        public ObservableCollection<PaymentDisplayRecord> Payments { get; private set; }

        /// <summary>
        /// Current payment summary analytics
        /// </summary>
        public PaymentSummaryAnalytics? Summary { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// Raised when payment addition is requested
        /// </summary>
        public event EventHandler<PaymentActionEventArgs>? AddPaymentRequested;

        /// <summary>
        /// Raised when payment editing is requested
        /// </summary>
        public event EventHandler<PaymentActionEventArgs>? EditPaymentRequested;

        /// <summary>
        /// Raised when payment deletion is requested
        /// </summary>
        public event EventHandler<PaymentActionEventArgs>? DeletePaymentRequested;

        /// <summary>
        /// Raised when payment details view is requested
        /// </summary>
        public event EventHandler<PaymentActionEventArgs>? ViewPaymentDetailsRequested;

        /// <summary>
        /// Raised when payment receipt print is requested
        /// </summary>
        public event EventHandler<PaymentActionEventArgs>? PrintReceiptRequested;

        /// <summary>
        /// Raised when payment history export is requested
        /// </summary>
        public event EventHandler<PaymentExportEventArgs>? PaymentExportRequested;

        /// <summary>
        /// Raised when payment data refresh is requested
        /// </summary>
        public event EventHandler<PaymentRefreshEventArgs>? PaymentRefreshRequested;

        /// <summary>
        /// PropertyChanged event for INotifyPropertyChanged implementation
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region Private Fields

        private readonly ILogger<PaymentHistoryControl> _logger;
        private PaymentMethodFilter _currentPaymentMethodFilter = PaymentMethodFilter.All;
        private bool _isInitialized = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes PaymentHistoryControl with comprehensive payment management capabilities
        /// </summary>
        public PaymentHistoryControl()
        {
            try
            {
                InitializeComponent();

                // Initialize logger through dependency injection if available - FIXED: Use App.Services instead of App.Current.Services
                _logger = App.Services?.GetService<ILogger<PaymentHistoryControl>>()
                          ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PaymentHistoryControl>.Instance;

                InitializeCollections();
                ConfigureEventHandlers();
                ConfigureDataGrid();

                _logger.LogDebug("PaymentHistoryControl initialized successfully with enterprise payment management capabilities");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Critical error initializing PaymentHistoryControl: {ex.Message}");
                throw;
            }
        }


        #endregion

        #region Public Methods

        /// <summary>
        /// Loads comprehensive payment history for the specified customer and date range
        /// </summary>
        /// <param name="customer">Customer to load payment history for</param>
        /// <param name="startDate">Payment history start date</param>
        /// <param name="endDate">Payment history end date</param>
        public async Task LoadPaymentHistoryAsync(Customer customer, DateTime startDate, DateTime endDate)
        {
            try
            {
                if (customer == null)
                    throw new ArgumentNullException(nameof(customer), "Customer cannot be null for payment history loading");

                IsLoading = true;
                Customer = customer;
                CustomerName = customer.CustomerName;
                StartDate = startDate;
                EndDate = endDate;

                // Request payment data from parent component through event-driven architecture
                var refreshArgs = new PaymentRefreshEventArgs(customer, startDate, endDate, _currentPaymentMethodFilter);
                PaymentRefreshRequested?.Invoke(this, refreshArgs);

                // FIXED: Add actual async operation to resolve CS1998 warning
                await Task.Delay(50, CancellationToken.None); // Brief delay for UI responsiveness

                _logger.LogInformation("Payment history loading initiated for customer: {CustomerName}, Period: {StartDate} to {EndDate}",
                    customer.CustomerName, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payment history for customer: {CustomerName}", customer?.CustomerName);
                throw;
            }
        }


        /// <summary>
        /// Updates the payment history display with new payment data and analytics
        /// </summary>
        /// <param name="payments">Payment records collection</param>
        /// <param name="summary">Payment summary analytics</param>
        public void UpdatePaymentHistory(IEnumerable<PaymentDisplayRecord> payments, PaymentSummaryAnalytics summary)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    // Clear existing payment data
                    Payments.Clear();

                    // Add new payment records sorted by date (latest first)
                    foreach (var payment in payments.OrderByDescending(p => p.PaymentDate))
                    {
                        Payments.Add(payment);
                    }

                    // Update analytics summary
                    Summary = summary;
                    UpdateSummaryDisplay();
                    UpdateStatusDisplay();

                    // Manage no data state visibility
                    NoPaymentsState.Visibility = Payments.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

                    _logger.LogDebug("Payment history updated with {PaymentCount} records for customer: {CustomerName}",
                        Payments.Count, CustomerName);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating payment history display");
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Clears the payment history display and resets all data
        /// </summary>
        public void ClearPaymentHistory()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    Payments.Clear();
                    Summary = null;
                    Customer = null;
                    CustomerName = string.Empty;

                    UpdateSummaryDisplay();
                    UpdateStatusDisplay();

                    NoPaymentsState.Visibility = Visibility.Visible;
                });

                _logger.LogDebug("Payment history display cleared successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error clearing payment history display");
            }
        }

        /// <summary>
        /// Refreshes payment data with current filter settings
        /// </summary>
        public async Task RefreshPaymentDataAsync()
        {
            try
            {
                if (Customer != null)
                {
                    await LoadPaymentHistoryAsync(Customer, StartDate, EndDate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing payment data");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles add payment button click with comprehensive validation
        /// </summary>
        private void AddPaymentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Customer != null)
                {
                    var actionArgs = new PaymentActionEventArgs(Customer, null);
                    AddPaymentRequested?.Invoke(this, actionArgs);
                    _logger.LogDebug("Add payment requested for customer: {CustomerName}", Customer.CustomerName);
                }
                else
                {
                    _logger.LogWarning("Add payment attempted without selected customer");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling add payment button click");
            }
        }

        /// <summary>
        /// Handles export payments button click
        /// </summary>
        private void ExportPaymentsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Customer != null && Summary != null)
                {
                    var exportArgs = new PaymentExportEventArgs(Customer, StartDate, EndDate, Payments.ToList(), Summary);
                    PaymentExportRequested?.Invoke(this, exportArgs);
                    _logger.LogDebug("Payment export requested for customer: {CustomerName}", Customer.CustomerName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling export payments button click");
            }
        }

        /// <summary>
        /// Handles payment method filter changes
        /// </summary>
        private void PaymentMethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (PaymentMethodComboBox.SelectedIndex >= 0)
                {
                    _currentPaymentMethodFilter = (PaymentMethodFilter)PaymentMethodComboBox.SelectedIndex;

                    if (_isInitialized && Customer != null)
                    {
                        _ = RefreshPaymentDataAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling payment method filter change");
            }
        }

        /// <summary>
        /// Handles quick date filter button clicks
        /// </summary>
        private void Last30DaysButton_Click(object sender, RoutedEventArgs e)
        {
            SetDateRange(DateTime.Today.AddDays(-30), DateTime.Today);
        }

        private void Last6MonthsButton_Click(object sender, RoutedEventArgs e)
        {
            SetDateRange(DateTime.Today.AddMonths(-6), DateTime.Today);
        }

        private void ThisYearButton_Click(object sender, RoutedEventArgs e)
        {
            SetDateRange(new DateTime(DateTime.Today.Year, 1, 1), DateTime.Today);
        }

        /// <summary>
        /// Handles view payment details button click
        /// </summary>
        private void ViewPaymentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.DataContext is PaymentDisplayRecord payment && Customer != null)
                {
                    var actionArgs = new PaymentActionEventArgs(Customer, payment);
                    ViewPaymentDetailsRequested?.Invoke(this, actionArgs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling view payment button click");
            }
        }

        /// <summary>
        /// Handles print receipt button click
        /// </summary>
        private void PrintReceiptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.DataContext is PaymentDisplayRecord payment && Customer != null)
                {
                    var actionArgs = new PaymentActionEventArgs(Customer, payment);
                    PrintReceiptRequested?.Invoke(this, actionArgs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling print receipt button click");
            }
        }

        /// <summary>
        /// Handles edit payment button click
        /// </summary>
        private void EditPaymentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.DataContext is PaymentDisplayRecord payment && Customer != null)
                {
                    var actionArgs = new PaymentActionEventArgs(Customer, payment);
                    EditPaymentRequested?.Invoke(this, actionArgs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling edit payment button click");
            }
        }

        /// <summary>
        /// Handles add first payment button click from no data state
        /// </summary>
        private void AddFirstPaymentButton_Click(object sender, RoutedEventArgs e)
        {
            AddPaymentButton_Click(sender, e);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes collections and data structures for payment management
        /// </summary>
        private void InitializeCollections()
        {
            // FIXED: Initialize Payments collection to resolve CS8618 warning
            Payments = new ObservableCollection<PaymentDisplayRecord>();
            PaymentsDataGrid.ItemsSource = Payments;
        }

        /// <summary>
        /// Configures comprehensive event handlers for all UI elements
        /// </summary>
        private void ConfigureEventHandlers()
        {
            try
            {
                // Primary action button handlers
                AddPaymentButton.Click += AddPaymentButton_Click;
                ExportPaymentsButton.Click += ExportPaymentsButton_Click;
                AddFirstPaymentButton.Click += AddFirstPaymentButton_Click;

                // Quick filter button handlers
                Last30DaysButton.Click += Last30DaysButton_Click;
                Last6MonthsButton.Click += Last6MonthsButton_Click;
                ThisYearButton.Click += ThisYearButton_Click;

                // Filter control handlers
                PaymentMethodComboBox.SelectionChanged += PaymentMethodComboBox_SelectionChanged;

                // Date picker change handlers
                StartDatePicker.SelectedDateChanged += (s, e) => OnDateRangeChanged();
                EndDatePicker.SelectedDateChanged += (s, e) => OnDateRangeChanged();

                _isInitialized = true;
                _logger.LogDebug("Event handlers configured successfully for PaymentHistoryControl");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring event handlers for PaymentHistoryControl");
                throw;
            }
        }

        /// <summary>
        /// Configures DataGrid settings for optimal performance and user experience
        /// </summary>
        private void ConfigureDataGrid()
        {
            try
            {
                // Enable virtualization for performance with large payment datasets
                PaymentsDataGrid.EnableRowVirtualization = true;
                PaymentsDataGrid.EnableColumnVirtualization = true;

                // Configure sorting capabilities
                PaymentsDataGrid.CanUserSortColumns = true;

                // Set default sort by payment date (descending)
                var dateColumn = PaymentsDataGrid.Columns.FirstOrDefault();
                if (dateColumn != null)
                {
                    dateColumn.SortDirection = System.ComponentModel.ListSortDirection.Descending;
                }

                _logger.LogDebug("DataGrid configured successfully with performance optimizations");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error configuring DataGrid for payment history");
            }
        }

        /// <summary>
        /// Updates the summary display with current payment analytics
        /// </summary>
        private void UpdateSummaryDisplay()
        {
            try
            {
                if (Summary != null)
                {
                    TotalPaymentsText.Text = $"{Summary.TotalAmount:N2} USD";
                    AveragePaymentText.Text = $"{Summary.AverageAmount:N2} USD";
                    PaymentCountText.Text = Summary.PaymentCount.ToString();
                    LastPaymentDateText.Text = Summary.LastPaymentDate?.ToString("yyyy/MM/dd") ?? "لا توجد";
                }
                else
                {
                    TotalPaymentsText.Text = "0.00 USD";
                    AveragePaymentText.Text = "0.00 USD";
                    PaymentCountText.Text = "0";
                    LastPaymentDateText.Text = "لا توجد";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating payment summary display");
            }
        }

        /// <summary>
        /// Updates the status display with payment count and date range information
        /// </summary>
        private void UpdateStatusDisplay()
        {
            try
            {
                var totalAmount = Summary?.TotalAmount ?? 0;
                StatusSummaryText.Text = $"{Payments.Count} دفعة - {totalAmount:N2} USD";
                DateRangeText.Text = $"الفترة: {StartDate:yyyy/MM/dd} - {EndDate:yyyy/MM/dd}";
                LastUpdateText.Text = $"آخر تحديث: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating payment status display");
            }
        }

        /// <summary>
        /// Sets the date range and triggers data refresh
        /// </summary>
        private void SetDateRange(DateTime startDate, DateTime endDate)
        {
            try
            {
                StartDate = startDate;
                EndDate = endDate;
                StartDatePicker.SelectedDate = startDate;
                EndDatePicker.SelectedDate = endDate;

                if (_isInitialized && Customer != null)
                {
                    _ = RefreshPaymentDataAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error setting date range for payment history");
            }
        }

        /// <summary>
        /// Handles date range changes and triggers data refresh
        /// </summary>
        private async void OnDateRangeChanged()
        {
            try
            {
                if (_isInitialized && Customer != null)
                {
                    await RefreshPaymentDataAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling date range change");
            }
        }

        /// <summary>
        /// Raises PropertyChanged event for data binding updates
        /// </summary>
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Static Event Handlers

        /// <summary>
        /// Handles Customer property changes with validation and display updates
        /// </summary>
        private static void OnCustomerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PaymentHistoryControl control && e.NewValue is Customer customer)
            {
                control.CustomerName = customer.CustomerName;
                control._logger.LogDebug("Customer changed to: {CustomerName} for payment history", customer.CustomerName);
            }
        }

        /// <summary>
        /// Handles date range property changes
        /// </summary>
        private static void OnDateRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PaymentHistoryControl control)
            {
                control.OnDateRangeChanged();
            }
        }

        /// <summary>
        /// Handles IsLoading property changes for loading indicator management
        /// </summary>
        private static void OnIsLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PaymentHistoryControl control)
            {
                control.LoadingIndicator.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        #endregion
    }

    #region Supporting Data Classes and Enums

    /// <summary>
    /// Payment display record optimized for UI presentation with enhanced analytics
    /// </summary>
    public class PaymentDisplayRecord
    {
        public int PaymentId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? InvoiceNumber { get; set; }
        public string? Notes { get; set; }
        public int CustomerId { get; set; }
        public int? InvoiceId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Comprehensive payment summary analytics for business intelligence
    /// </summary>
    public class PaymentSummaryAnalytics
    {
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
        public int PaymentCount { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public DateTime? FirstPaymentDate { get; set; }
        public Dictionary<string, decimal> PaymentMethodBreakdown { get; set; } = new();
        public Dictionary<string, int> MonthlyPaymentCounts { get; set; } = new();
        public decimal MaxPayment { get; set; }
        public decimal MinPayment { get; set; }
    }

    /// <summary>
    /// Payment method filter enumeration for advanced filtering capabilities
    /// </summary>
    public enum PaymentMethodFilter
    {
        All = 0,
        Cash = 1,
        Check = 2,
        BankTransfer = 3,
        CreditCard = 4
    }

    #endregion

    #region Event Argument Classes

    /// <summary>
    /// Event arguments for payment action requests with comprehensive context
    /// </summary>
    public class PaymentActionEventArgs : EventArgs
    {
        public Customer Customer { get; }
        public PaymentDisplayRecord? Payment { get; }

        public PaymentActionEventArgs(Customer customer, PaymentDisplayRecord? payment)
        {
            Customer = customer ?? throw new ArgumentNullException(nameof(customer));
            Payment = payment;
        }
    }

    /// <summary>
    /// Event arguments for payment export requests with full dataset context
    /// </summary>
    public class PaymentExportEventArgs : EventArgs
    {
        public Customer Customer { get; }
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }
        public List<PaymentDisplayRecord> Payments { get; }
        public PaymentSummaryAnalytics Summary { get; }

        public PaymentExportEventArgs(Customer customer, DateTime startDate, DateTime endDate,
            List<PaymentDisplayRecord> payments, PaymentSummaryAnalytics summary)
        {
            Customer = customer ?? throw new ArgumentNullException(nameof(customer));
            StartDate = startDate;
            EndDate = endDate;
            Payments = payments ?? throw new ArgumentNullException(nameof(payments));
            Summary = summary ?? throw new ArgumentNullException(nameof(summary));
        }
    }

    /// <summary>
    /// Event arguments for payment refresh requests with filter context
    /// </summary>
    public class PaymentRefreshEventArgs : EventArgs
    {
        public Customer Customer { get; }
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }
        public PaymentMethodFilter MethodFilter { get; }

        public PaymentRefreshEventArgs(Customer customer, DateTime startDate, DateTime endDate, PaymentMethodFilter methodFilter)
        {
            Customer = customer ?? throw new ArgumentNullException(nameof(customer));
            StartDate = startDate;
            EndDate = endDate;
            MethodFilter = methodFilter;
        }
    }

    #endregion
}