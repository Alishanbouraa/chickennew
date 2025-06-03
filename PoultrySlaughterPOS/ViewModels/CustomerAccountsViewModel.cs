using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Models;
using PoultrySlaughterPOS.Services;
using PoultrySlaughterPOS.Services.Repositories;
using PoultrySlaughterPOS.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace PoultrySlaughterPOS.ViewModels
{
    /// <summary>
    /// Enterprise-grade Customer Accounts ViewModel implementing comprehensive customer management,
    /// financial tracking, debt analysis, and business intelligence for poultry slaughter operations.
    /// Optimized for high-performance operations with advanced search, filtering, and reporting capabilities.
    /// </summary>
    public partial class CustomerAccountsViewModel : ObservableObject
    {
        #region Private Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CustomerAccountsViewModel> _logger;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly IServiceProvider _serviceProvider;

        // Collections for UI binding
        private ObservableCollection<Customer> _customers = new();
        private ObservableCollection<Invoice> _customerInvoices = new();
        private ObservableCollection<Payment> _customerPayments = new();
        private ObservableCollection<string> _validationErrors = new();

        // Current selections and state
        private Customer? _selectedCustomer;
        private Invoice? _selectedInvoice;
        private Payment? _selectedPayment;

        // Search and filtering
        private string _searchText = string.Empty;
        private bool _showActiveOnly = true;
        private bool _showDebtOnly = false;
        private decimal _minimumDebtFilter = 0;

        // Date range filtering
        private DateTime _startDate = DateTime.Today.AddMonths(-3);
        private DateTime _endDate = DateTime.Today;

        // UI State
        private bool _isLoading = false;
        private bool _hasValidationErrors = false;
        private string _statusMessage = "جاهز لإدارة حسابات الزبائن";
        private string _statusIcon = "Users";
        private string _statusColor = "#28A745";

        // Statistics and analytics
        private int _totalCustomersCount = 0;
        private int _activeCustomersCount = 0;
        private decimal _totalDebtAmount = 0;
        private int _customersWithDebtCount = 0;
        private decimal _averageDebtPerCustomer = 0;

        // Pagination
        private int _currentPage = 1;
        private int _pageSize = 50;
        private int _totalPages = 1;
        private int _totalRecords = 0;

        // View states
        private bool _isCustomerDetailsVisible = false;
        private bool _isTransactionHistoryVisible = false;
        private bool _isPaymentHistoryVisible = false;
        private bool _isDebtAnalysisVisible = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes CustomerAccountsViewModel with comprehensive dependency injection
        /// </summary>
        public CustomerAccountsViewModel(
            IUnitOfWork unitOfWork,
            ILogger<CustomerAccountsViewModel> logger,
            IErrorHandlingService errorHandlingService,
            IServiceProvider serviceProvider)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            InitializeCollectionViews();
            _logger.LogInformation("CustomerAccountsViewModel initialized with enterprise-grade customer management");
        }

        #endregion

        #region Observable Properties

        /// <summary>
        /// Collection of customers with advanced filtering and search capabilities
        /// </summary>
        public ObservableCollection<Customer> Customers
        {
            get => _customers;
            set => SetProperty(ref _customers, value);
        }

        /// <summary>
        /// Collection of invoices for selected customer
        /// </summary>
        public ObservableCollection<Invoice> CustomerInvoices
        {
            get => _customerInvoices;
            set => SetProperty(ref _customerInvoices, value);
        }

        /// <summary>
        /// Collection of payments for selected customer
        /// </summary>
        public ObservableCollection<Payment> CustomerPayments
        {
            get => _customerPayments;
            set => SetProperty(ref _customerPayments, value);
        }

        /// <summary>
        /// Currently selected customer for detailed operations
        /// </summary>
        public Customer? SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                if (SetProperty(ref _selectedCustomer, value))
                {
                    OnCustomerSelectionChanged();
                }
            }
        }

        /// <summary>
        /// Currently selected invoice for transaction details
        /// </summary>
        public Invoice? SelectedInvoice
        {
            get => _selectedInvoice;
            set => SetProperty(ref _selectedInvoice, value);
        }

        /// <summary>
        /// Currently selected payment for payment details
        /// </summary>
        public Payment? SelectedPayment
        {
            get => _selectedPayment;
            set => SetProperty(ref _selectedPayment, value);
        }

        /// <summary>
        /// Search text for real-time customer filtering
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = ApplyFiltersAsync();
                }
            }
        }

        /// <summary>
        /// Filter to show only active customers
        /// </summary>
        public bool ShowActiveOnly
        {
            get => _showActiveOnly;
            set
            {
                if (SetProperty(ref _showActiveOnly, value))
                {
                    _ = ApplyFiltersAsync();
                }
            }
        }

        /// <summary>
        /// Filter to show only customers with debt
        /// </summary>
        public bool ShowDebtOnly
        {
            get => _showDebtOnly;
            set
            {
                if (SetProperty(ref _showDebtOnly, value))
                {
                    _ = ApplyFiltersAsync();
                }
            }
        }

        /// <summary>
        /// Minimum debt amount filter
        /// </summary>
        public decimal MinimumDebtFilter
        {
            get => _minimumDebtFilter;
            set
            {
                if (SetProperty(ref _minimumDebtFilter, value))
                {
                    _ = ApplyFiltersAsync();
                }
            }
        }

        /// <summary>
        /// Start date for transaction filtering
        /// </summary>
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                if (SetProperty(ref _startDate, value))
                {
                    _ = LoadCustomerTransactionsAsync();
                }
            }
        }

        /// <summary>
        /// End date for transaction filtering
        /// </summary>
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (SetProperty(ref _endDate, value))
                {
                    _ = LoadCustomerTransactionsAsync();
                }
            }
        }

        /// <summary>
        /// Loading state indicator
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Validation errors indicator
        /// </summary>
        public bool HasValidationErrors
        {
            get => _hasValidationErrors;
            set => SetProperty(ref _hasValidationErrors, value);
        }

        /// <summary>
        /// Collection of validation error messages
        /// </summary>
        public ObservableCollection<string> ValidationErrors
        {
            get => _validationErrors;
            set => SetProperty(ref _validationErrors, value);
        }

        /// <summary>
        /// Status message for user feedback
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Status icon for UI feedback
        /// </summary>
        public string StatusIcon
        {
            get => _statusIcon;
            set => SetProperty(ref _statusIcon, value);
        }

        /// <summary>
        /// Status color for UI feedback
        /// </summary>
        public string StatusColor
        {
            get => _statusColor;
            set => SetProperty(ref _statusColor, value);
        }

        // Statistics Properties
        public int TotalCustomersCount
        {
            get => _totalCustomersCount;
            set => SetProperty(ref _totalCustomersCount, value);
        }

        public int ActiveCustomersCount
        {
            get => _activeCustomersCount;
            set => SetProperty(ref _activeCustomersCount, value);
        }

        public decimal TotalDebtAmount
        {
            get => _totalDebtAmount;
            set => SetProperty(ref _totalDebtAmount, value);
        }

        public int CustomersWithDebtCount
        {
            get => _customersWithDebtCount;
            set => SetProperty(ref _customersWithDebtCount, value);
        }

        public decimal AverageDebtPerCustomer
        {
            get => _averageDebtPerCustomer;
            set => SetProperty(ref _averageDebtPerCustomer, value);
        }

        // Pagination Properties
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (SetProperty(ref _currentPage, value))
                {
                    _ = LoadCustomersPagedAsync();
                }
            }
        }

        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (SetProperty(ref _pageSize, value))
                {
                    CurrentPage = 1;
                    _ = LoadCustomersPagedAsync();
                }
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, value);
        }

        public int TotalRecords
        {
            get => _totalRecords;
            set => SetProperty(ref _totalRecords, value);
        }

        // View State Properties
        public bool IsCustomerDetailsVisible
        {
            get => _isCustomerDetailsVisible;
            set => SetProperty(ref _isCustomerDetailsVisible, value);
        }

        public bool IsTransactionHistoryVisible
        {
            get => _isTransactionHistoryVisible;
            set => SetProperty(ref _isTransactionHistoryVisible, value);
        }

        public bool IsPaymentHistoryVisible
        {
            get => _isPaymentHistoryVisible;
            set => SetProperty(ref _isPaymentHistoryVisible, value);
        }

        public bool IsDebtAnalysisVisible
        {
            get => _isDebtAnalysisVisible;
            set => SetProperty(ref _isDebtAnalysisVisible, value);
        }

        /// <summary>
        /// Indicates whether pagination controls should be visible
        /// </summary>
        public bool IsPaginationVisible => TotalPages > 1;

        /// <summary>
        /// Indicates whether previous page navigation is available
        /// </summary>
        public bool CanGoToPreviousPage => CurrentPage > 1;

        /// <summary>
        /// Indicates whether next page navigation is available
        /// </summary>
        public bool CanGoToNextPage => CurrentPage < TotalPages;

        #endregion

        #region Commands

        [RelayCommand]
        private async Task AddNewCustomerAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                _logger.LogInformation("Opening add new customer dialog");

                var currentWindow = GetCurrentWindow();
                var createdCustomer = await AddCustomerDialog.ShowNewCustomerDialogAsync(_serviceProvider, currentWindow);

                if (createdCustomer != null)
                {
                    await RefreshCustomersAsync();
                    SelectedCustomer = createdCustomer;
                    UpdateStatus($"تم إضافة الزبون '{createdCustomer.CustomerName}' بنجاح", "CheckCircle", "#28A745");
                }
            }, "إضافة زبون جديد");
        }

        [RelayCommand]
        private async Task EditCustomerAsync()
        {
            if (SelectedCustomer == null) return;

            await ExecuteWithErrorHandlingAsync(async () =>
            {
                _logger.LogInformation("Opening edit customer dialog for customer: {CustomerId}", SelectedCustomer.CustomerId);

                var currentWindow = GetCurrentWindow();
                var editedCustomer = await AddCustomerDialog.ShowEditCustomerDialogAsync(_serviceProvider, currentWindow, SelectedCustomer);

                if (editedCustomer != null)
                {
                    await RefreshCustomersAsync();
                    SelectedCustomer = editedCustomer;
                    UpdateStatus($"تم تحديث بيانات الزبون '{editedCustomer.CustomerName}' بنجاح", "CheckCircle", "#28A745");
                }
            }, "تعديل بيانات الزبون");
        }

        [RelayCommand]
        private async Task DeleteCustomerAsync()
        {
            if (SelectedCustomer == null) return;

            await ExecuteWithErrorHandlingAsync(async () =>
            {
                var result = MessageBox.Show(
                    $"هل أنت متأكد من حذف الزبون '{SelectedCustomer.CustomerName}'؟\n\nهذا الإجراء لا يمكن التراجع عنه.",
                    "تأكيد الحذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    var canDelete = await _unitOfWork.Customers.CanDeleteCustomerAsync(SelectedCustomer.CustomerId);
                    if (!canDelete)
                    {
                        UpdateStatus("لا يمكن حذف الزبون لوجود معاملات مرتبطة به", "ExclamationTriangle", "#DC3545");
                        return;
                    }

                    await _unitOfWork.Customers.DeleteAsync(SelectedCustomer.CustomerId);
                    await _unitOfWork.SaveChangesAsync("CUSTOMER_MANAGEMENT");

                    await RefreshCustomersAsync();
                    SelectedCustomer = null;
                    UpdateStatus("تم حذف الزبون بنجاح", "CheckCircle", "#28A745");
                }
            }, "حذف الزبون");
        }

        [RelayCommand]
        private async Task RefreshDataAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                await RefreshCustomersAsync();
                await CalculateStatisticsAsync();
                UpdateStatus("تم تحديث البيانات بنجاح", "Refresh", "#007BFF");
            }, "تحديث البيانات");
        }

        [RelayCommand]
        private async Task ClearFiltersAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                SearchText = string.Empty;
                ShowActiveOnly = true;
                ShowDebtOnly = false;
                MinimumDebtFilter = 0;
                StartDate = DateTime.Today.AddMonths(-3);
                EndDate = DateTime.Today;

                await RefreshCustomersAsync();
                UpdateStatus("تم مسح المرشحات", "Filter", "#6C757D");
            }, "مسح المرشحات");
        }

        [RelayCommand]
        private void ShowCustomerDetails()
        {
            IsCustomerDetailsVisible = true;
            IsTransactionHistoryVisible = false;
            IsPaymentHistoryVisible = false;
            IsDebtAnalysisVisible = false;
        }

        [RelayCommand]
        private void ShowTransactionHistory()
        {
            IsTransactionHistoryVisible = true;
            IsCustomerDetailsVisible = false;
            IsPaymentHistoryVisible = false;
            IsDebtAnalysisVisible = false;
            _ = LoadCustomerTransactionsAsync();
        }

        [RelayCommand]
        private void ShowPaymentHistory()
        {
            IsPaymentHistoryVisible = true;
            IsCustomerDetailsVisible = false;
            IsTransactionHistoryVisible = false;
            IsDebtAnalysisVisible = false;
            _ = LoadCustomerPaymentsAsync();
        }

        [RelayCommand]
        private void ShowDebtAnalysis()
        {
            IsDebtAnalysisVisible = true;
            IsCustomerDetailsVisible = false;
            IsTransactionHistoryVisible = false;
            IsPaymentHistoryVisible = false;
        }

        [RelayCommand]
        private async Task RecalculateCustomerBalanceAsync()
        {
            if (SelectedCustomer == null) return;

            await ExecuteWithErrorHandlingAsync(async () =>
            {
                var wasRecalculated = await _unitOfWork.Customers.RecalculateCustomerBalanceAsync(SelectedCustomer.CustomerId);

                if (wasRecalculated)
                {
                    await RefreshCustomersAsync();
                    await LoadCustomerTransactionsAsync();
                    UpdateStatus("تم إعادة حساب الرصيد بنجاح", "Calculator", "#28A745");
                }
                else
                {
                    UpdateStatus("الرصيد صحيح ولا يحتاج إعادة حساب", "CheckCircle", "#17A2B8");
                }
            }, "إعادة حساب الرصيد");
        }

        [RelayCommand]
        private async Task GoToPreviousPageAsync()
        {
            if (CanGoToPreviousPage)
            {
                CurrentPage--;
            }
        }

        [RelayCommand]
        private async Task GoToNextPageAsync()
        {
            if (CanGoToNextPage)
            {
                CurrentPage++;
            }
        }

        [RelayCommand]
        private async Task GoToFirstPageAsync()
        {
            if (CurrentPage != 1)
            {
                CurrentPage = 1;
            }
        }

        [RelayCommand]
        private async Task GoToLastPageAsync()
        {
            if (CurrentPage != TotalPages)
            {
                CurrentPage = TotalPages;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the customer accounts view with comprehensive data loading
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                UpdateStatus("جاري تحميل بيانات الزبائن...", "Spinner", "#007BFF");

                await LoadCustomersPagedAsync();
                await CalculateStatisticsAsync();

                // Show customer details by default
                ShowCustomerDetails();

                UpdateStatus($"تم تحميل {TotalRecords} زبون بنجاح", "CheckCircle", "#28A745");
                _logger.LogInformation("CustomerAccountsViewModel initialized successfully with {CustomerCount} customers", TotalRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during CustomerAccountsViewModel initialization");
                await _errorHandlingService.HandleExceptionAsync(ex, "CustomerAccountsViewModel.InitializeAsync");
                UpdateStatus("خطأ في تحميل بيانات الزبائن", "ExclamationTriangle", "#DC3545");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Cleanup method for resource disposal
        /// </summary>
        public void Cleanup()
        {
            try
            {
                Customers.Clear();
                CustomerInvoices.Clear();
                CustomerPayments.Clear();
                ValidationErrors.Clear();

                _logger.LogInformation("CustomerAccountsViewModel cleanup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during CustomerAccountsViewModel cleanup");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes collection views for advanced filtering and sorting
        /// </summary>
        private void InitializeCollectionViews()
        {
            Customers = new ObservableCollection<Customer>();
            CustomerInvoices = new ObservableCollection<Invoice>();
            CustomerPayments = new ObservableCollection<Payment>();
            ValidationErrors = new ObservableCollection<string>();
        }

        /// <summary>
        /// Loads customers with pagination support
        /// </summary>
        private async Task LoadCustomersPagedAsync()
        {
            try
            {
                var (customers, totalCount) = await _unitOfWork.Customers.GetCustomersPagedAsync(
                    CurrentPage,
                    PageSize,
                    SearchText,
                    ShowActiveOnly ? true : null);

                Customers.Clear();
                foreach (var customer in customers)
                {
                    Customers.Add(customer);
                }

                TotalRecords = totalCount;
                TotalPages = (int)Math.Ceiling((double)totalCount / PageSize);

                OnPropertyChanged(nameof(IsPaginationVisible));
                OnPropertyChanged(nameof(CanGoToPreviousPage));
                OnPropertyChanged(nameof(CanGoToNextPage));

                _logger.LogDebug("Loaded {CustomerCount} customers for page {Page}", customers.Count(), CurrentPage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customers paged");
                throw;
            }
        }

        /// <summary>
        /// Applies search and filter criteria
        /// </summary>
        private async Task ApplyFiltersAsync()
        {
            try
            {
                CurrentPage = 1; // Reset to first page when filtering
                await LoadCustomersPagedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying filters");
            }
        }

        /// <summary>
        /// Refreshes customer list and statistics
        /// </summary>
        private async Task RefreshCustomersAsync()
        {
            try
            {
                await LoadCustomersPagedAsync();
                await CalculateStatisticsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing customers");
                throw;
            }
        }

        /// <summary>
        /// Calculates comprehensive customer statistics
        /// </summary>
        private async Task CalculateStatisticsAsync()
        {
            try
            {
                TotalCustomersCount = await _unitOfWork.Customers.CountAsync();
                ActiveCustomersCount = await _unitOfWork.Customers.GetActiveCustomerCountAsync();

                var (totalDebt, customersWithDebtCount) = await _unitOfWork.Customers.GetDebtSummaryAsync();
                TotalDebtAmount = totalDebt;
                CustomersWithDebtCount = customersWithDebtCount;

                AverageDebtPerCustomer = CustomersWithDebtCount > 0 ? TotalDebtAmount / CustomersWithDebtCount : 0;

                _logger.LogDebug("Customer statistics calculated - Total: {Total}, Active: {Active}, Debt: {Debt}",
                    TotalCustomersCount, ActiveCustomersCount, TotalDebtAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating customer statistics");
            }
        }

        /// <summary>
        /// Handles customer selection changes and loads related data
        /// </summary>
        private async void OnCustomerSelectionChanged()
        {
            try
            {
                if (SelectedCustomer != null)
                {
                    _logger.LogDebug("Customer selected: {CustomerName} (ID: {CustomerId})",
                        SelectedCustomer.CustomerName, SelectedCustomer.CustomerId);

                    // Load customer transactions if transaction history is visible
                    if (IsTransactionHistoryVisible)
                    {
                        await LoadCustomerTransactionsAsync();
                    }

                    // Load customer payments if payment history is visible
                    if (IsPaymentHistoryVisible)
                    {
                        await LoadCustomerPaymentsAsync();
                    }
                }
                else
                {
                    CustomerInvoices.Clear();
                    CustomerPayments.Clear();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling customer selection change");
            }
        }

        /// <summary>
        /// Loads transaction history for selected customer
        /// </summary>
        private async Task LoadCustomerTransactionsAsync()
        {
            try
            {
                if (SelectedCustomer == null) return;

                var invoices = await _unitOfWork.Customers.GetCustomerInvoicesAsync(
                    SelectedCustomer.CustomerId, StartDate, EndDate);

                CustomerInvoices.Clear();
                foreach (var invoice in invoices)
                {
                    CustomerInvoices.Add(invoice);
                }

                _logger.LogDebug("Loaded {InvoiceCount} invoices for customer {CustomerId}",
                    invoices.Count(), SelectedCustomer.CustomerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer transactions for customer {CustomerId}",
                    SelectedCustomer?.CustomerId);
            }
        }

        /// <summary>
        /// Loads payment history for selected customer
        /// </summary>
        private async Task LoadCustomerPaymentsAsync()
        {
            try
            {
                if (SelectedCustomer == null) return;

                var payments = await _unitOfWork.Customers.GetCustomerPaymentsAsync(
                    SelectedCustomer.CustomerId, StartDate, EndDate);

                CustomerPayments.Clear();
                foreach (var payment in payments)
                {
                    CustomerPayments.Add(payment);
                }

                _logger.LogDebug("Loaded {PaymentCount} payments for customer {CustomerId}",
                    payments.Count(), SelectedCustomer.CustomerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer payments for customer {CustomerId}",
                    SelectedCustomer?.CustomerId);
            }
        }

        /// <summary>
        /// Gets the current window for dialog positioning
        /// </summary>
        private Window? GetCurrentWindow()
        {
            try
            {
                return Application.Current.Windows
                    .Cast<Window>()
                    .FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting current window");
                return Application.Current.MainWindow;
            }
        }

        /// <summary>
        /// Executes an operation with comprehensive error handling
        /// </summary>
        private async Task ExecuteWithErrorHandlingAsync(Func<Task> operation, string operationName)
        {
            try
            {
                IsLoading = true;
                await operation();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing operation: {OperationName}", operationName);
                var (success, userMessage) = await _errorHandlingService.HandleExceptionAsync(ex, operationName);
                UpdateStatus(userMessage, "ExclamationTriangle", "#DC3545");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Updates the status display for user feedback
        /// </summary>
        private void UpdateStatus(string message, string icon, string color)
        {
            StatusMessage = message;
            StatusIcon = icon;
            StatusColor = color;
            _logger.LogDebug("Status updated: {Message}", message);
        }

        #endregion
    }
}