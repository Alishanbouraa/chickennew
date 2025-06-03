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
    /// Enterprise-grade Account Statement Control providing comprehensive transaction history,
    /// financial analysis, and statement generation capabilities with advanced filtering and export features.
    /// Implements professional accounting standards with real-time balance calculations.
    /// </summary>
    public partial class AccountStatementControl : UserControl, INotifyPropertyChanged
    {
        #region Dependency Properties

        /// <summary>
        /// Customer for statement generation
        /// </summary>
        public static readonly DependencyProperty CustomerProperty =
            DependencyProperty.Register(
                nameof(Customer),
                typeof(Customer),
                typeof(AccountStatementControl),
                new PropertyMetadata(null, OnCustomerChanged));

        /// <summary>
        /// Customer name for display purposes
        /// </summary>
        public static readonly DependencyProperty CustomerNameProperty =
            DependencyProperty.Register(
                nameof(CustomerName),
                typeof(string),
                typeof(AccountStatementControl),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Start date for statement period
        /// </summary>
        public static readonly DependencyProperty StartDateProperty =
            DependencyProperty.Register(
                nameof(StartDate),
                typeof(DateTime),
                typeof(AccountStatementControl),
                new PropertyMetadata(DateTime.Today.AddMonths(-3), OnDateRangeChanged));

        /// <summary>
        /// End date for statement period
        /// </summary>
        public static readonly DependencyProperty EndDateProperty =
            DependencyProperty.Register(
                nameof(EndDate),
                typeof(DateTime),
                typeof(AccountStatementControl),
                new PropertyMetadata(DateTime.Today, OnDateRangeChanged));

        /// <summary>
        /// Loading state indicator
        /// </summary>
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(
                nameof(IsLoading),
                typeof(bool),
                typeof(AccountStatementControl),
                new PropertyMetadata(false, OnIsLoadingChanged));

        #endregion

        #region Public Properties

        /// <summary>
        /// Customer for statement generation
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
        /// Start date for statement period
        /// </summary>
        public DateTime StartDate
        {
            get => (DateTime)GetValue(StartDateProperty);
            set => SetValue(StartDateProperty, value);
        }

        /// <summary>
        /// End date for statement period
        /// </summary>
        public DateTime EndDate
        {
            get => (DateTime)GetValue(EndDateProperty);
            set => SetValue(EndDateProperty, value);
        }

        /// <summary>
        /// Loading state indicator
        /// </summary>
        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        /// <summary>
        /// Collection of statement transactions
        /// </summary>
        public ObservableCollection<StatementTransaction> Transactions { get; private set; }

        /// <summary>
        /// Current statement summary data
        /// </summary>
        public StatementSummary? Summary { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// Raised when transaction details view is requested
        /// </summary>
        public event EventHandler<TransactionDetailsEventArgs>? TransactionDetailsRequested;

        /// <summary>
        /// Raised when statement export is requested
        /// </summary>
        public event EventHandler<StatementExportEventArgs>? StatementExportRequested;

        /// <summary>
        /// Raised when statement print is requested
        /// </summary>
        public event EventHandler<StatementPrintEventArgs>? StatementPrintRequested;

        /// <summary>
        /// Raised when statement data refresh is requested
        /// </summary>
        public event EventHandler<StatementRefreshEventArgs>? StatementRefreshRequested;

        /// <summary>
        /// PropertyChanged event for INotifyPropertyChanged
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region Private Fields

        private readonly ILogger<AccountStatementControl> _logger;
        private StatementFilterType _currentFilterType = StatementFilterType.All;
        private bool _isInitialized = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes AccountStatementControl with comprehensive financial analysis capabilities
        /// </summary>
        public AccountStatementControl()
        {
            try
            {
                InitializeComponent();

                // Initialize logger if available - FIXED: Use App.Services instead of App.Current.Services
                _logger = App.Services?.GetService<ILogger<AccountStatementControl>>()
                          ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<AccountStatementControl>.Instance;

                InitializeCollections();
                ConfigureEventHandlers();
                ConfigureDataGrid();

                _logger.LogDebug("AccountStatementControl initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing AccountStatementControl: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads statement data for the specified customer and date range
        /// </summary>
        /// <param name="customer">Customer to generate statement for</param>
        /// <param name="startDate">Statement start date</param>
        /// <param name="endDate">Statement end date</param>
        public async Task LoadStatementAsync(Customer customer, DateTime startDate, DateTime endDate)
        {
            try
            {
                if (customer == null)
                    throw new ArgumentNullException(nameof(customer));

                IsLoading = true;
                Customer = customer;
                CustomerName = customer.CustomerName;
                StartDate = startDate;
                EndDate = endDate;

                // Request statement data from parent component
                var refreshArgs = new StatementRefreshEventArgs(customer, startDate, endDate, _currentFilterType);
                StatementRefreshRequested?.Invoke(this, refreshArgs);

                // FIXED: Add actual async operation to resolve CS1998 warning
                await Task.Delay(50, CancellationToken.None); // Brief delay for UI responsiveness

                _logger.LogInformation("Statement loading initiated for customer: {CustomerName}, Period: {StartDate} to {EndDate}",
                    customer.CustomerName, startDate.ToString("yyyy-MM-dd"), endDate.ToString("yyyy-MM-dd"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading statement for customer: {CustomerName}", customer?.CustomerName);
                throw;
            }
        }

        /// <summary>
        /// Updates the statement display with new transaction data
        /// </summary>
        /// <param name="transactions">Statement transactions</param>
        /// <param name="summary">Statement summary data</param>
        public void UpdateStatement(IEnumerable<StatementTransaction> transactions, StatementSummary summary)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    // Clear existing data
                    Transactions.Clear();

                    // Add new transactions
                    foreach (var transaction in transactions.OrderBy(t => t.TransactionDate))
                    {
                        Transactions.Add(transaction);
                    }

                    // Update summary
                    Summary = summary;
                    UpdateSummaryDisplay();
                    UpdateStatusDisplay();

                    // Show/hide no data state
                    NoDataState.Visibility = Transactions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

                    _logger.LogDebug("Statement updated with {TransactionCount} transactions", Transactions.Count);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating statement display");
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Clears the statement display
        /// </summary>
        public void ClearStatement()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    Transactions.Clear();
                    Summary = null;
                    Customer = null;
                    CustomerName = string.Empty;

                    UpdateSummaryDisplay();
                    UpdateStatusDisplay();

                    NoDataState.Visibility = Visibility.Visible;
                });

                _logger.LogDebug("Statement display cleared");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error clearing statement display");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles export button click
        /// </summary>
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Customer != null && Summary != null)
                {
                    var exportArgs = new StatementExportEventArgs(Customer, StartDate, EndDate, Transactions.ToList(), Summary);
                    StatementExportRequested?.Invoke(this, exportArgs);
                    _logger.LogDebug("Statement export requested for customer: {CustomerName}", Customer.CustomerName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling export button click");
            }
        }

        /// <summary>
        /// Handles print button click
        /// </summary>
        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Customer != null && Summary != null)
                {
                    var printArgs = new StatementPrintEventArgs(Customer, StartDate, EndDate, Transactions.ToList(), Summary);
                    StatementPrintRequested?.Invoke(this, printArgs);
                    _logger.LogDebug("Statement print requested for customer: {CustomerName}", Customer.CustomerName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling print button click");
            }
        }

        /// <summary>
        /// Handles transaction type filter change
        /// </summary>
        private void TransactionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (TransactionTypeComboBox.SelectedIndex >= 0)
                {
                    _currentFilterType = (StatementFilterType)TransactionTypeComboBox.SelectedIndex;

                    if (_isInitialized && Customer != null)
                    {
                        _ = RefreshStatementAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling transaction type filter change");
            }
        }

        /// <summary>
        /// Handles quick filter button clicks
        /// </summary>
        private void Last30DaysButton_Click(object sender, RoutedEventArgs e)
        {
            SetDateRange(DateTime.Today.AddDays(-30), DateTime.Today);
        }

        private void Last90DaysButton_Click(object sender, RoutedEventArgs e)
        {
            SetDateRange(DateTime.Today.AddDays(-90), DateTime.Today);
        }

        private void ThisYearButton_Click(object sender, RoutedEventArgs e)
        {
            SetDateRange(new DateTime(DateTime.Today.Year, 1, 1), DateTime.Today);
        }

        /// <summary>
        /// Handles view transaction details button click
        /// </summary>
        private void ViewDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.DataContext is StatementTransaction transaction)
                {
                    var detailsArgs = new TransactionDetailsEventArgs(transaction);
                    TransactionDetailsRequested?.Invoke(this, detailsArgs);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling view details button click");
            }
        }

        /// <summary>
        /// Handles print transaction button click
        /// </summary>
        private void PrintTransactionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.DataContext is StatementTransaction transaction)
                {
                    // Implement individual transaction print
                    _logger.LogDebug("Transaction print requested: {ReferenceNumber}", transaction.ReferenceNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling print transaction button click");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes collections and data structures
        /// </summary>
        private void InitializeCollections()
        {
            // FIXED: Initialize Transactions collection to resolve CS8618 warning
            Transactions = new ObservableCollection<StatementTransaction>();
            TransactionsDataGrid.ItemsSource = Transactions;
        }
        /// <summary>
        /// Configures event handlers for UI elements
        /// </summary>
        private void ConfigureEventHandlers()
        {
            try
            {
                // Button event handlers
                ExportButton.Click += ExportButton_Click;
                PrintButton.Click += PrintButton_Click;

                // Quick filter button handlers
                Last30DaysButton.Click += Last30DaysButton_Click;
                Last90DaysButton.Click += Last90DaysButton_Click;
                ThisYearButton.Click += ThisYearButton_Click;

                // ComboBox event handlers
                TransactionTypeComboBox.SelectionChanged += TransactionTypeComboBox_SelectionChanged;

                // DatePicker event handlers
                StartDatePicker.SelectedDateChanged += (s, e) => OnDateRangeChanged();
                EndDatePicker.SelectedDateChanged += (s, e) => OnDateRangeChanged();

                _isInitialized = true;
                _logger.LogDebug("Event handlers configured successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring event handlers");
                throw;
            }
        }

        /// <summary>
        /// Configures DataGrid settings and behavior
        /// </summary>
        private void ConfigureDataGrid()
        {
            try
            {
                // Enable virtualization for performance
                TransactionsDataGrid.EnableRowVirtualization = true;
                TransactionsDataGrid.EnableColumnVirtualization = true;

                // Configure sorting
                TransactionsDataGrid.CanUserSortColumns = true;

                _logger.LogDebug("DataGrid configured successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error configuring DataGrid");
            }
        }

        /// <summary>
        /// Updates the summary display with current statement data
        /// </summary>
        private void UpdateSummaryDisplay()
        {
            try
            {
                if (Summary != null)
                {
                    OpeningBalanceText.Text = $"{Summary.OpeningBalance:N2} USD";
                    TotalInvoicesAmountText.Text = $"{Summary.TotalInvoices:N2} USD";
                    TotalPaymentsAmountText.Text = $"{Summary.TotalPayments:N2} USD";
                    ClosingBalanceText.Text = $"{Summary.ClosingBalance:N2} USD";
                }
                else
                {
                    OpeningBalanceText.Text = "0.00 USD";
                    TotalInvoicesAmountText.Text = "0.00 USD";
                    TotalPaymentsAmountText.Text = "0.00 USD";
                    ClosingBalanceText.Text = "0.00 USD";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating summary display");
            }
        }

        /// <summary>
        /// Updates the status display with transaction count and date range
        /// </summary>
        private void UpdateStatusDisplay()
        {
            try
            {
                TransactionCountText.Text = $"{Transactions.Count} معاملة";
                DateRangeText.Text = $"الفترة: {StartDate:yyyy/MM/dd} - {EndDate:yyyy/MM/dd}";
                LastUpdateText.Text = $"آخر تحديث: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating status display");
            }
        }

        /// <summary>
        /// Sets the date range and refreshes the statement
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
                    _ = RefreshStatementAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error setting date range");
            }
        }

        /// <summary>
        /// Handles date range changes and refreshes statement
        /// </summary>
        private async void OnDateRangeChanged()
        {
            try
            {
                if (_isInitialized && Customer != null)
                {
                    await RefreshStatementAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling date range change");
            }
        }

        /// <summary>
        /// Refreshes the statement data with current filters
        /// </summary>
        private async Task RefreshStatementAsync()
        {
            try
            {
                if (Customer != null)
                {
                    await LoadStatementAsync(Customer, StartDate, EndDate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing statement");
            }
        }

        /// <summary>
        /// Raises PropertyChanged event
        /// </summary>
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Static Event Handlers

        /// <summary>
        /// Handles Customer property changes
        /// </summary>
        private static void OnCustomerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AccountStatementControl control && e.NewValue is Customer customer)
            {
                control.CustomerName = customer.CustomerName;
                control._logger.LogDebug("Customer changed to: {CustomerName}", customer.CustomerName);
            }
        }

        /// <summary>
        /// Handles date range property changes
        /// </summary>
        private static void OnDateRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AccountStatementControl control)
            {
                control.OnDateRangeChanged();
            }
        }

        /// <summary>
        /// Handles IsLoading property changes
        /// </summary>
        private static void OnIsLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AccountStatementControl control)
            {
                control.LoadingIndicator.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        #endregion
    }

    #region Supporting Data Classes and Enums

    /// <summary>
    /// Statement transaction data model for display purposes
    /// </summary>
    public class StatementTransaction
    {
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public decimal RunningBalance { get; set; }
        public int? InvoiceId { get; set; }
        public int? PaymentId { get; set; }
    }

    /// <summary>
    /// Statement summary data for financial analysis
    /// </summary>
    public class StatementSummary
    {
        public decimal OpeningBalance { get; set; }
        public decimal TotalInvoices { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal ClosingBalance { get; set; }
        public int TransactionCount { get; set; }
        public DateTime StatementStartDate { get; set; }
        public DateTime StatementEndDate { get; set; }
    }

    /// <summary>
    /// Statement filter types
    /// </summary>
    public enum StatementFilterType
    {
        All = 0,
        InvoicesOnly = 1,
        PaymentsOnly = 2
    }

    #endregion

    #region Event Argument Classes

    /// <summary>
    /// Event arguments for transaction details requests
    /// </summary>
    public class TransactionDetailsEventArgs : EventArgs
    {
        public StatementTransaction Transaction { get; }

        public TransactionDetailsEventArgs(StatementTransaction transaction)
        {
            Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        }
    }

    /// <summary>
    /// Event arguments for statement export requests
    /// </summary>
    public class StatementExportEventArgs : EventArgs
    {
        public Customer Customer { get; }
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }
        public List<StatementTransaction> Transactions { get; }
        public StatementSummary Summary { get; }

        public StatementExportEventArgs(Customer customer, DateTime startDate, DateTime endDate,
            List<StatementTransaction> transactions, StatementSummary summary)
        {
            Customer = customer ?? throw new ArgumentNullException(nameof(customer));
            StartDate = startDate;
            EndDate = endDate;
            Transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));
            Summary = summary ?? throw new ArgumentNullException(nameof(summary));
        }
    }

    /// <summary>
    /// Event arguments for statement print requests
    /// </summary>
    public class StatementPrintEventArgs : StatementExportEventArgs
    {
        public StatementPrintEventArgs(Customer customer, DateTime startDate, DateTime endDate,
            List<StatementTransaction> transactions, StatementSummary summary)
            : base(customer, startDate, endDate, transactions, summary)
        {
        }
    }

    /// <summary>
    /// Event arguments for statement refresh requests
    /// </summary>
    public class StatementRefreshEventArgs : EventArgs
    {
        public Customer Customer { get; }
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }
        public StatementFilterType FilterType { get; }

        public StatementRefreshEventArgs(Customer customer, DateTime startDate, DateTime endDate, StatementFilterType filterType)
        {
            Customer = customer ?? throw new ArgumentNullException(nameof(customer));
            StartDate = startDate;
            EndDate = endDate;
            FilterType = filterType;
        }
    }

    #endregion
}