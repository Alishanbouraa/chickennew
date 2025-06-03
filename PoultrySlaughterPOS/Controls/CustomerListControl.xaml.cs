using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PoultrySlaughterPOS.Controls
{
    /// <summary>
    /// Enterprise-grade Customer List Control providing advanced customer management interface
    /// with real-time filtering, sorting, and context-sensitive actions.
    /// Optimized for high-performance operations with keyboard shortcuts and accessibility support.
    /// </summary>
    public partial class CustomerListControl : UserControl, INotifyPropertyChanged
    {
        #region Dependency Properties

        /// <summary>
        /// Collection of customers to display in the grid
        /// </summary>
        public static readonly DependencyProperty CustomersProperty =
            DependencyProperty.Register(
                nameof(Customers),
                typeof(ObservableCollection<Customer>),
                typeof(CustomerListControl),
                new PropertyMetadata(null, OnCustomersChanged));

        /// <summary>
        /// Currently selected customer
        /// </summary>
        public static readonly DependencyProperty SelectedCustomerProperty =
            DependencyProperty.Register(
                nameof(SelectedCustomer),
                typeof(Customer),
                typeof(CustomerListControl),
                new PropertyMetadata(null, OnSelectedCustomerChanged));

        /// <summary>
        /// Loading state indicator
        /// </summary>
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(
                nameof(IsLoading),
                typeof(bool),
                typeof(CustomerListControl),
                new PropertyMetadata(false, OnIsLoadingChanged));

        /// <summary>
        /// Filter text for real-time search
        /// </summary>
        public static readonly DependencyProperty FilterTextProperty =
            DependencyProperty.Register(
                nameof(FilterText),
                typeof(string),
                typeof(CustomerListControl),
                new PropertyMetadata(string.Empty, OnFilterTextChanged));

        #endregion

        #region Public Properties

        /// <summary>
        /// Collection of customers to display in the grid
        /// </summary>
        public ObservableCollection<Customer> Customers
        {
            get => (ObservableCollection<Customer>)GetValue(CustomersProperty);
            set => SetValue(CustomersProperty, value);
        }

        /// <summary>
        /// Currently selected customer
        /// </summary>
        public Customer? SelectedCustomer
        {
            get => (Customer?)GetValue(SelectedCustomerProperty);
            set => SetValue(SelectedCustomerProperty, value);
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
        /// Filter text for real-time search
        /// </summary>
        public string FilterText
        {
            get => (string)GetValue(FilterTextProperty);
            set => SetValue(FilterTextProperty, value);
        }

        #endregion

        #region Events

        /// <summary>
        /// Raised when a customer is selected
        /// </summary>
        public event EventHandler<CustomerSelectedEventArgs>? CustomerSelected;

        /// <summary>
        /// Raised when a customer edit is requested
        /// </summary>
        public event EventHandler<CustomerActionEventArgs>? CustomerEditRequested;

        /// <summary>
        /// Raised when a customer delete is requested
        /// </summary>
        public event EventHandler<CustomerActionEventArgs>? CustomerDeleteRequested;

        /// <summary>
        /// Raised when customer details view is requested
        /// </summary>
        public event EventHandler<CustomerActionEventArgs>? CustomerDetailsRequested;

        /// <summary>
        /// Raised when data refresh is requested
        /// </summary>
        public event EventHandler? RefreshRequested;

        /// <summary>
        /// Raised when data export is requested
        /// </summary>
        public event EventHandler? ExportRequested;

        /// <summary>
        /// PropertyChanged event for INotifyPropertyChanged
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region Private Fields

        private readonly ILogger<CustomerListControl> _logger;
        private bool _isInitialized = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes CustomerListControl with comprehensive event handling and UI setup
        /// </summary>
        public CustomerListControl()
        {
            try
            {
                InitializeComponent();

                // Initialize logger if available - FIXED: Use App.Services instead of App.Current.Services
                _logger = App.Services?.GetService<ILogger<CustomerListControl>>()
                          ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<CustomerListControl>.Instance;

                ConfigureEventHandlers();
                ConfigureDataGrid();

                _logger.LogDebug("CustomerListControl initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing CustomerListControl: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the customer statistics display
        /// </summary>
        /// <param name="totalDebt">Total debt amount across all customers</param>
        /// <param name="customerCount">Total number of customers</param>
        public void UpdateStatistics(decimal totalDebt, int customerCount)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    TotalDebtSummary.Text = $"{totalDebt:N2} USD";
                    CustomerCountBadge.Text = $"{customerCount} زبون";
                    LastUpdateText.Text = $"آخر تحديث: {DateTime.Now:HH:mm:ss}";
                });

                _logger.LogDebug("Customer statistics updated - Total Debt: {TotalDebt}, Count: {Count}",
                    totalDebt, customerCount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating customer statistics");
            }
        }

        /// <summary>
        /// Sets the filter summary text
        /// </summary>
        /// <param name="filterDescription">Description of current filters</param>
        public void SetFilterSummary(string filterDescription)
        {
            try
            {
                FilterSummaryText.Text = filterDescription;
                _logger.LogDebug("Filter summary updated: {FilterDescription}", filterDescription);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error setting filter summary");
            }
        }

        /// <summary>
        /// Scrolls to and selects a specific customer
        /// </summary>
        /// <param name="customer">Customer to select and scroll to</param>
        public void ScrollToCustomer(Customer customer)
        {
            try
            {
                if (customer == null) return;

                CustomersDataGrid.SelectedItem = customer;
                CustomersDataGrid.ScrollIntoView(customer);
                CustomersDataGrid.Focus();

                _logger.LogDebug("Scrolled to customer: {CustomerName}", customer.CustomerName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error scrolling to customer: {CustomerName}", customer?.CustomerName);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles DataGrid selection changes
        /// </summary>
        private void CustomersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectedCustomer = CustomersDataGrid.SelectedItem as Customer;
                SelectedCustomer = selectedCustomer;

                // Update selection info
                if (selectedCustomer != null)
                {
                    SelectionInfoText.Text = $"محدد: {selectedCustomer.CustomerName}";
                    CustomerSelected?.Invoke(this, new CustomerSelectedEventArgs(selectedCustomer));
                }
                else
                {
                    SelectionInfoText.Text = "لم يتم اختيار زبون";
                }

                _logger.LogDebug("Customer selection changed: {CustomerName}",
                    selectedCustomer?.CustomerName ?? "None");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling customer selection change");
            }
        }

        /// <summary>
        /// Handles DataGrid double-click for quick edit
        /// </summary>
        private void CustomersDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (SelectedCustomer != null)
                {
                    CustomerEditRequested?.Invoke(this, new CustomerActionEventArgs(SelectedCustomer));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling DataGrid double-click");
            }
        }

        /// <summary>
        /// Handles refresh button click
        /// </summary>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RefreshRequested?.Invoke(this, EventArgs.Empty);
                _logger.LogDebug("Refresh requested by user");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling refresh button click");
            }
        }

        /// <summary>
        /// Handles export button click
        /// </summary>
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ExportRequested?.Invoke(this, EventArgs.Empty);
                _logger.LogDebug("Export requested by user");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling export button click");
            }
        }

        /// <summary>
        /// Handles edit customer button click
        /// </summary>
        private void EditCustomerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.DataContext is Customer customer)
                {
                    CustomerEditRequested?.Invoke(this, new CustomerActionEventArgs(customer));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling edit customer button click");
            }
        }

        /// <summary>
        /// Handles view details button click
        /// </summary>
        private void ViewDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.DataContext is Customer customer)
                {
                    CustomerDetailsRequested?.Invoke(this, new CustomerActionEventArgs(customer));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling view details button click");
            }
        }

        /// <summary>
        /// Handles delete customer button click
        /// </summary>
        private void DeleteCustomerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.DataContext is Customer customer)
                {
                    var result = MessageBox.Show(
                        $"هل أنت متأكد من حذف الزبون '{customer.CustomerName}'؟",
                        "تأكيد الحذف",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        CustomerDeleteRequested?.Invoke(this, new CustomerActionEventArgs(customer));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling delete customer button click");
            }
        }

        /// <summary>
        /// Handles keyboard shortcuts
        /// </summary>
        private void CustomerListControl_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                switch (e.Key)
                {
                    case Key.F5:
                        RefreshRequested?.Invoke(this, EventArgs.Empty);
                        e.Handled = true;
                        break;

                    case Key.Enter:
                        if (SelectedCustomer != null)
                        {
                            CustomerDetailsRequested?.Invoke(this, new CustomerActionEventArgs(SelectedCustomer));
                            e.Handled = true;
                        }
                        break;

                    case Key.Delete:
                        if (SelectedCustomer != null)
                        {
                            CustomerDeleteRequested?.Invoke(this, new CustomerActionEventArgs(SelectedCustomer));
                            e.Handled = true;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling keyboard shortcut: {Key}", e.Key);
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
                // DataGrid events
                CustomersDataGrid.SelectionChanged += CustomersDataGrid_SelectionChanged;
                CustomersDataGrid.MouseDoubleClick += CustomersDataGrid_MouseDoubleClick;

                // Button events
                RefreshButton.Click += RefreshButton_Click;
                ExportButton.Click += ExportButton_Click;

                // Keyboard events
                KeyDown += CustomerListControl_KeyDown;

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
                CustomersDataGrid.EnableRowVirtualization = true;
                CustomersDataGrid.EnableColumnVirtualization = true;

                // Configure sorting
                CustomersDataGrid.CanUserSortColumns = true;

                _logger.LogDebug("DataGrid configured successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error configuring DataGrid");
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
        /// Handles Customers property changes
        /// </summary>
        private static void OnCustomersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CustomerListControl control)
            {
                control.CustomersDataGrid.ItemsSource = e.NewValue as ObservableCollection<Customer>;
                control._logger.LogDebug("Customers collection updated");
            }
        }

        /// <summary>
        /// Handles SelectedCustomer property changes
        /// </summary>
        private static void OnSelectedCustomerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CustomerListControl control)
            {
                control.CustomersDataGrid.SelectedItem = e.NewValue as Customer;
            }
        }

        /// <summary>
        /// Handles IsLoading property changes
        /// </summary>
        private static void OnIsLoadingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CustomerListControl control)
            {
                control.LoadingOverlay.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Handles FilterText property changes
        /// </summary>
        private static void OnFilterTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Filter text changes can be handled by parent ViewModel
            // This is here for potential future local filtering implementation
        }

        #endregion
    }

    #region Event Argument Classes

    /// <summary>
    /// Event arguments for customer selection events
    /// </summary>
    public class CustomerSelectedEventArgs : EventArgs
    {
        public Customer SelectedCustomer { get; }

        public CustomerSelectedEventArgs(Customer selectedCustomer)
        {
            SelectedCustomer = selectedCustomer ?? throw new ArgumentNullException(nameof(selectedCustomer));
        }
    }

    /// <summary>
    /// Event arguments for customer action events
    /// </summary>
    public class CustomerActionEventArgs : EventArgs
    {
        public Customer Customer { get; }

        public CustomerActionEventArgs(Customer customer)
        {
            Customer = customer ?? throw new ArgumentNullException(nameof(customer));
        }
    }

    #endregion
}