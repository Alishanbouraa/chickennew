using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Models;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PoultrySlaughterPOS.Controls
{
    /// <summary>
    /// Enterprise-grade Customer Details Control providing comprehensive customer information display
    /// with real-time data updates, transaction summaries, and integrated action management.
    /// Optimized for business intelligence and customer relationship management.
    /// </summary>
    public partial class CustomerDetailsControl : UserControl, INotifyPropertyChanged
    {
        #region Dependency Properties

        /// <summary>
        /// Customer to display details for
        /// </summary>
        public static readonly DependencyProperty CustomerProperty =
            DependencyProperty.Register(
                nameof(Customer),
                typeof(Customer),
                typeof(CustomerDetailsControl),
                new PropertyMetadata(null, OnCustomerChanged));

        /// <summary>
        /// Loading state indicator
        /// </summary>
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(
                nameof(IsLoading),
                typeof(bool),
                typeof(CustomerDetailsControl),
                new PropertyMetadata(false));

        #endregion

        #region Public Properties

        /// <summary>
        /// Customer to display details for
        /// </summary>
        public Customer? Customer
        {
            get => (Customer?)GetValue(CustomerProperty);
            set => SetValue(CustomerProperty, value);
        }

        /// <summary>
        /// Loading state indicator
        /// </summary>
        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised when customer edit is requested
        /// </summary>
        public event EventHandler<CustomerActionEventArgs>? CustomerEditRequested;

        /// <summary>
        /// Raised when add payment is requested
        /// </summary>
        public event EventHandler<CustomerActionEventArgs>? AddPaymentRequested;

        /// <summary>
        /// Raised when balance recalculation is requested
        /// </summary>
        public event EventHandler<CustomerActionEventArgs>? RecalculateBalanceRequested;

        /// <summary>
        /// Raised when transaction history view is requested
        /// </summary>
        public event EventHandler<CustomerActionEventArgs>? ViewTransactionsRequested;

        /// <summary>
        /// PropertyChanged event for INotifyPropertyChanged
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region Private Fields

        private readonly ILogger<CustomerDetailsControl> _logger;
        private CustomerSummaryData? _summaryData;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes CustomerDetailsControl with comprehensive event handling and UI setup
        /// </summary>
        public CustomerDetailsControl()
        {
            try
            {
                InitializeComponent();

                // Initialize logger if available - FIXED: Use App.Services instead of App.Current.Services
                _logger = App.Services?.GetService<ILogger<CustomerDetailsControl>>()
                          ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<CustomerDetailsControl>.Instance;

                ConfigureEventHandlers();

                _logger.LogDebug("CustomerDetailsControl initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing CustomerDetailsControl: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the customer transaction summary data
        /// </summary>
        /// <param name="summaryData">Customer summary data including transaction counts and amounts</param>
        public void UpdateSummaryData(CustomerSummaryData summaryData)
        {
            try
            {
                _summaryData = summaryData ?? throw new ArgumentNullException(nameof(summaryData));

                Dispatcher.Invoke(() =>
                {
                    // Update transaction summary
                    TotalInvoicesText.Text = summaryData.TotalInvoices.ToString();
                    TotalPaymentsText.Text = summaryData.TotalPayments.ToString();

                    // Update last transaction info
                    LastInvoiceText.Text = summaryData.LastInvoiceDate?.ToString("yyyy/MM/dd") ?? "لا توجد";
                    LastPaymentText.Text = summaryData.LastPaymentDate?.ToString("yyyy/MM/dd") ?? "لا توجد";

                    // Update account age
                    if (Customer != null)
                    {
                        var accountAge = (DateTime.Now - Customer.CreatedDate).Days;
                        AccountAgeText.Text = $"{accountAge} يوم";
                    }
                });

                _logger.LogDebug("Customer summary data updated for customer: {CustomerName}",
                    Customer?.CustomerName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating customer summary data");
            }
        }

        /// <summary>
        /// Refreshes the customer details display
        /// </summary>
        public async Task RefreshDetailsAsync()
        {
            try
            {
                if (Customer == null) return;

                IsLoading = true;

                // Trigger data refresh - this would typically involve calling back to the parent ViewModel
                // For now, we'll just update the display with current customer data
                await Task.Delay(100); // Simulate async operation

                UpdateDisplayFromCustomer();

                _logger.LogDebug("Customer details refreshed for: {CustomerName}", Customer.CustomerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing customer details");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Clears the customer details display
        /// </summary>
        public void ClearDetails()
        {
            try
            {
                Customer = null;
                _summaryData = null;

                // Reset summary displays
                Dispatcher.Invoke(() =>
                {
                    TotalInvoicesText.Text = "0";
                    TotalPaymentsText.Text = "0";
                    LastInvoiceText.Text = "لا توجد";
                    LastPaymentText.Text = "لا توجد";
                    AccountAgeText.Text = "-- يوم";
                });

                _logger.LogDebug("Customer details cleared");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error clearing customer details");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles edit customer button click
        /// </summary>
        private void EditCustomerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Customer != null)
                {
                    CustomerEditRequested?.Invoke(this, new CustomerActionEventArgs(Customer));
                    _logger.LogDebug("Customer edit requested for: {CustomerName}", Customer.CustomerName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling edit customer button click");
            }
        }

        /// <summary>
        /// Handles add payment button click
        /// </summary>
        private void AddPaymentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Customer != null)
                {
                    AddPaymentRequested?.Invoke(this, new CustomerActionEventArgs(Customer));
                    _logger.LogDebug("Add payment requested for: {CustomerName}", Customer.CustomerName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling add payment button click");
            }
        }

        /// <summary>
        /// Handles recalculate balance button click
        /// </summary>
        private void RecalculateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Customer != null)
                {
                    RecalculateBalanceRequested?.Invoke(this, new CustomerActionEventArgs(Customer));
                    _logger.LogDebug("Balance recalculation requested for: {CustomerName}", Customer.CustomerName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling recalculate button click");
            }
        }

        /// <summary>
        /// Handles view transactions button click
        /// </summary>
        private void ViewTransactionsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Customer != null)
                {
                    ViewTransactionsRequested?.Invoke(this, new CustomerActionEventArgs(Customer));
                    _logger.LogDebug("View transactions requested for: {CustomerName}", Customer.CustomerName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling view transactions button click");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Configures event handlers for UI elements
        /// </summary>
        private void ConfigureEventHandlers()
        {
            try
            {
                // Button event handlers
                EditCustomerButton.Click += EditCustomerButton_Click;
                AddPaymentButton.Click += AddPaymentButton_Click;
                RecalculateButton.Click += RecalculateButton_Click;
                ViewTransactionsButton.Click += ViewTransactionsButton_Click;

                _logger.LogDebug("Event handlers configured successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring event handlers");
                throw;
            }
        }

        /// <summary>
        /// Updates the display when customer changes
        /// </summary>
        private void UpdateDisplayFromCustomer()
        {
            try
            {
                if (Customer == null) return;

                // Update account age
                var accountAge = (DateTime.Now - Customer.CreatedDate).Days;
                AccountAgeText.Text = $"{accountAge} يوم";

                // Reset summary data if no specific data is available
                if (_summaryData == null)
                {
                    TotalInvoicesText.Text = "جاري التحميل...";
                    TotalPaymentsText.Text = "جاري التحميل...";
                    LastInvoiceText.Text = "جاري التحميل...";
                    LastPaymentText.Text = "جاري التحميل...";
                }

                _logger.LogDebug("Display updated for customer: {CustomerName}", Customer.CustomerName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating display from customer data");
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
            if (d is CustomerDetailsControl control)
            {
                control.UpdateDisplayFromCustomer();
                control._logger.LogDebug("Customer changed to: {CustomerName}",
                    (e.NewValue as Customer)?.CustomerName ?? "None");
            }
        }

        #endregion
    }

    #region Supporting Data Classes

    /// <summary>
    /// Customer summary data for display in details control
    /// </summary>
    public class CustomerSummaryData
    {
        public int TotalInvoices { get; set; }
        public int TotalPayments { get; set; }
        public decimal TotalInvoiceAmount { get; set; }
        public decimal TotalPaymentAmount { get; set; }
        public DateTime? LastInvoiceDate { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public decimal AverageInvoiceAmount { get; set; }
        public decimal AveragePaymentAmount { get; set; }
    }

    #endregion
}