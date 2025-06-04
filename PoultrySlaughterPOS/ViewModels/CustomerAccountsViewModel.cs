// PoultrySlaughterPOS/ViewModels/CustomerAccountsViewModel.cs
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
    /// ENHANCED: Complete debt settlement integration with advanced payment processing capabilities,
    /// real-time collection statistics, and bulk settlement operations.
    /// 
    /// ARCHITECTURE: Defensive programming with comprehensive null safety, error handling,
    /// and robust state management for mission-critical financial operations.
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
        private bool _isCustomerDetailsVisible = true;
        private bool _isTransactionHistoryVisible = false;
        private bool _isPaymentHistoryVisible = false;
        private bool _isDebtAnalysisVisible = false;

        // ENHANCED: Debt settlement specific properties
        private decimal _totalCollectionsToday = 0;
        private int _paymentsProcessedToday = 0;
        private decimal _averagePaymentAmount = 0;
        private bool _isQuickPaymentMode = false;
        private decimal _totalCollectionsThisMonth = 0;
        private decimal _collectionEfficiencyRate = 0;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes CustomerAccountsViewModel with comprehensive dependency injection
        /// and enhanced null safety validation
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
            ValidateDependencies();

            _logger.LogInformation("CustomerAccountsViewModel initialized with enhanced debt settlement capabilities and comprehensive null safety");
        }

        #endregion

        #region Observable Properties

        /// <summary>
        /// Collection of customers with advanced filtering and search capabilities
        /// </summary>
        public ObservableCollection<Customer> Customers
        {
            get => _customers;
            set => SetProperty(ref _customers, value ?? new ObservableCollection<Customer>());
        }

        /// <summary>
        /// Collection of invoices for selected customer
        /// </summary>
        public ObservableCollection<Invoice> CustomerInvoices
        {
            get => _customerInvoices;
            set => SetProperty(ref _customerInvoices, value ?? new ObservableCollection<Invoice>());
        }

        /// <summary>
        /// Collection of payments for selected customer
        /// </summary>
        public ObservableCollection<Payment> CustomerPayments
        {
            get => _customerPayments;
            set => SetProperty(ref _customerPayments, value ?? new ObservableCollection<Payment>());
        }

        /// <summary>
        /// Currently selected customer for detailed operations with enhanced null safety
        /// </summary>
        public Customer? SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                if (SetProperty(ref _selectedCustomer, value))
                {
                    OnCustomerSelectionChanged();
                    OnPropertyChanged(nameof(CanProcessPayment));
                    OnPropertyChanged(nameof(HasSelectedCustomerWithDebt));
                    OnPropertyChanged(nameof(SelectedCustomerDebtAmount));
                    OnPropertyChanged(nameof(SelectedCustomerDisplayName));
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
                if (SetProperty(ref _searchText, value ?? string.Empty))
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
            set => SetProperty(ref _validationErrors, value ?? new ObservableCollection<string>());
        }

        /// <summary>
        /// Status message for user feedback
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value ?? string.Empty);
        }

        /// <summary>
        /// Status icon for UI feedback
        /// </summary>
        public string StatusIcon
        {
            get => _statusIcon;
            set => SetProperty(ref _statusIcon, value ?? "Users");
        }

        /// <summary>
        /// Status color for UI feedback
        /// </summary>
        public string StatusColor
        {
            get => _statusColor;
            set => SetProperty(ref _statusColor, value ?? "#28A745");
        }

        // Statistics Properties with null safety
        public int TotalCustomersCount
        {
            get => _totalCustomersCount;
            set => SetProperty(ref _totalCustomersCount, Math.Max(0, value));
        }

        public int ActiveCustomersCount
        {
            get => _activeCustomersCount;
            set => SetProperty(ref _activeCustomersCount, Math.Max(0, value));
        }

        public decimal TotalDebtAmount
        {
            get => _totalDebtAmount;
            set => SetProperty(ref _totalDebtAmount, Math.Max(0, value));
        }

        public int CustomersWithDebtCount
        {
            get => _customersWithDebtCount;
            set => SetProperty(ref _customersWithDebtCount, Math.Max(0, value));
        }

        public decimal AverageDebtPerCustomer
        {
            get => _averageDebtPerCustomer;
            set => SetProperty(ref _averageDebtPerCustomer, Math.Max(0, value));
        }

        // Pagination Properties with validation
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                var newValue = Math.Max(1, value);
                if (SetProperty(ref _currentPage, newValue))
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
                var newValue = Math.Max(10, Math.Min(1000, value)); // Limit page size
                if (SetProperty(ref _pageSize, newValue))
                {
                    CurrentPage = 1;
                    _ = LoadCustomersPagedAsync();
                }
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            set => SetProperty(ref _totalPages, Math.Max(1, value));
        }

        public int TotalRecords
        {
            get => _totalRecords;
            set => SetProperty(ref _totalRecords, Math.Max(0, value));
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

        // ENHANCED: Debt Settlement Properties with validation

        /// <summary>
        /// Total collections processed today
        /// </summary>
        public decimal TotalCollectionsToday
        {
            get => _totalCollectionsToday;
            set => SetProperty(ref _totalCollectionsToday, Math.Max(0, value));
        }

        /// <summary>
        /// Number of payments processed today
        /// </summary>
        public int PaymentsProcessedToday
        {
            get => _paymentsProcessedToday;
            set => SetProperty(ref _paymentsProcessedToday, Math.Max(0, value));
        }

        /// <summary>
        /// Average payment amount across all customers
        /// </summary>
        public decimal AveragePaymentAmount
        {
            get => _averagePaymentAmount;
            set => SetProperty(ref _averagePaymentAmount, Math.Max(0, value));
        }

        /// <summary>
        /// Quick payment mode for rapid debt settlement processing
        /// </summary>
        public bool IsQuickPaymentMode
        {
            get => _isQuickPaymentMode;
            set => SetProperty(ref _isQuickPaymentMode, value);
        }

        /// <summary>
        /// Total collections for current month
        /// </summary>
        public decimal TotalCollectionsThisMonth
        {
            get => _totalCollectionsThisMonth;
            set => SetProperty(ref _totalCollectionsThisMonth, Math.Max(0, value));
        }

        /// <summary>
        /// Collection efficiency rate (collections vs total debt)
        /// </summary>
        public decimal CollectionEfficiencyRate
        {
            get => _collectionEfficiencyRate;
            set => SetProperty(ref _collectionEfficiencyRate, Math.Max(0, Math.Min(1, value)));
        }

        /// <summary>
        /// Indicates whether a payment can be processed for selected customer with comprehensive validation
        /// </summary>
        public bool CanProcessPayment => SelectedCustomer != null &&
                                        SelectedCustomer.TotalDebt > 0 &&
                                        !IsLoading &&
                                        _serviceProvider != null;

        /// <summary>
        /// Indicates whether selected customer has outstanding debt
        /// </summary>
        public bool HasSelectedCustomerWithDebt => SelectedCustomer?.TotalDebt > 0;

        /// <summary>
        /// Selected customer's debt amount for display
        /// </summary>
        public decimal SelectedCustomerDebtAmount => SelectedCustomer?.TotalDebt ?? 0;

        /// <summary>
        /// Selected customer's display name with null safety
        /// </summary>
        public string SelectedCustomerDisplayName => SelectedCustomer?.CustomerName ?? "لم يتم اختيار زبون";

        #endregion

        #region Commands

        // ENHANCED: Debt Settlement Commands with comprehensive null safety

        /// <summary>
        /// Process payment command with enhanced error handling and null safety
        /// </summary>
        [RelayCommand]
        private async Task ProcessPaymentAsync()
        {
            // Comprehensive pre-validation
            if (!ValidatePaymentProcessingPreconditions())
            {
                return;
            }

            // Capture customer reference to prevent race conditions
            var customerForPayment = SelectedCustomer;
            if (customerForPayment == null)
            {
                UpdateStatus("لم يتم اختيار زبون للدفع", "ExclamationTriangle", "#DC3545");
                return;
            }

            await ExecuteWithErrorHandlingAsync(async () =>
            {
                _logger.LogInformation("Initiating payment processing for customer: {CustomerName} (ID: {CustomerId}, Debt: {Debt:C})",
                    customerForPayment.CustomerName, customerForPayment.CustomerId, customerForPayment.TotalDebt);

                // Validate service provider
                if (_serviceProvider == null)
                {
                    throw new InvalidOperationException("Service provider is not available for payment dialog creation");
                }

                // Get current window with null safety
                var currentWindow = GetCurrentWindowSafely();

                _logger.LogDebug("Obtained current window reference: {WindowType}",
                    currentWindow?.GetType().Name ?? "null");

                // Call payment dialog with comprehensive error handling
                var processedPayment = await PaymentDialog.ShowPaymentDialogAsync(
                    _serviceProvider, currentWindow, customerForPayment);

                if (processedPayment != null)
                {
                    _logger.LogInformation("Payment processed successfully: PaymentId={PaymentId}, Amount={Amount:C}, CustomerId={CustomerId}",
                        processedPayment.PaymentId, processedPayment.Amount, customerForPayment.CustomerId);

                    await RefreshAfterPaymentAsync(processedPayment);
                    UpdateStatus($"تم تسجيل دفعة بمبلغ {processedPayment.Amount:N2} USD للزبون '{customerForPayment.CustomerName}'",
                        "CheckCircle", "#28A745");
                }
                else
                {
                    _logger.LogInformation("Payment dialog was cancelled by user for customer {CustomerId}",
                        customerForPayment.CustomerId);
                    UpdateStatus("تم إلغاء عملية الدفع", "Info", "#17A2B8");
                }

            }, "معالجة الدفعة");
        }

        /// <summary>
        /// Quick payment command with enhanced validation and error handling
        /// </summary>
        [RelayCommand]
        private async Task QuickPaymentAsync(object? parameter)
        {
            // Comprehensive pre-validation
            if (!ValidatePaymentProcessingPreconditions())
            {
                return;
            }

            var customerForPayment = SelectedCustomer;
            if (customerForPayment == null)
            {
                UpdateStatus("لم يتم اختيار زبون للدفع السريع", "ExclamationTriangle", "#DC3545");
                return;
            }

            await ExecuteWithErrorHandlingAsync(async () =>
            {
                decimal paymentAmount = CalculateQuickPaymentAmount(parameter, customerForPayment.TotalDebt);

                if (paymentAmount <= 0 || paymentAmount > customerForPayment.TotalDebt * 2)
                {
                    UpdateStatus("مبلغ الدفعة السريعة غير صحيح", "ExclamationTriangle", "#DC3545");
                    return;
                }

                // Confirm quick payment
                var result = MessageBox.Show(
                    $"هل تريد تسجيل دفعة سريعة بمبلغ {paymentAmount:N2} USD للزبون '{customerForPayment.CustomerName}'؟",
                    "تأكيد الدفعة السريعة",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Create payment directly with comprehensive validation
                    var payment = new Payment
                    {
                        CustomerId = customerForPayment.CustomerId,
                        Amount = paymentAmount,
                        PaymentMethod = "CASH",
                        PaymentDate = DateTime.Now,
                        Notes = "دفعة سريعة",
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    };

                    // Validate unit of work before usage
                    if (_unitOfWork?.Payments == null)
                    {
                        throw new InvalidOperationException("Payment repository is not available");
                    }

                    var processedPayment = await _unitOfWork.Payments.CreatePaymentWithTransactionAsync(payment);
                    await _unitOfWork.SaveChangesAsync("QUICK_PAYMENT");

                    await RefreshAfterPaymentAsync(processedPayment);
                    UpdateStatus($"تم تسجيل دفعة سريعة بمبلغ {paymentAmount:N2} USD", "CheckCircle", "#28A745");
                }
            }, "دفعة سريعة");
        }

        /// <summary>
        /// View payment history command with null safety
        /// </summary>
        [RelayCommand]
        private async Task ViewPaymentHistoryAsync()
        {
            if (SelectedCustomer == null)
            {
                UpdateStatus("يرجى اختيار زبون لعرض تاريخ الدفعات", "Info", "#17A2B8");
                return;
            }

            await ExecuteWithErrorHandlingAsync(async () =>
            {
                IsPaymentHistoryVisible = true;
                IsCustomerDetailsVisible = false;
                IsTransactionHistoryVisible = false;
                IsDebtAnalysisVisible = false;

                await LoadCustomerPaymentsAsync();
                UpdateStatus($"تم تحميل تاريخ الدفعات للزبون '{SelectedCustomer.CustomerName}'", "History", "#007BFF");
            }, "عرض تاريخ الدفعات");
        }

        /// <summary>
        /// Refresh collection statistics command
        /// </summary>
        [RelayCommand]
        private async Task RefreshCollectionStatisticsAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                await CalculateCollectionStatisticsAsync();
                UpdateStatus("تم تحديث إحصائيات التحصيل", "ChartBar", "#007BFF");
            }, "تحديث إحصائيات التحصيل");
        }

        /// <summary>
        /// Settle all debts command with comprehensive validation
        /// </summary>
        [RelayCommand]
        private async Task SettleAllDebtsAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                var customersWithDebt = Customers?.Where(c => c != null && c.TotalDebt > 0).ToList() ?? new List<Customer>();

                if (!customersWithDebt.Any())
                {
                    UpdateStatus("لا يوجد زبائن مديونين", "Info", "#17A2B8");
                    return;
                }

                var totalDebt = customersWithDebt.Sum(c => c.TotalDebt);
                var result = MessageBox.Show(
                    $"هل تريد تسوية جميع ديون الزبائن ({customersWithDebt.Count} زبون)؟\n\nإجمالي المبلغ: {totalDebt:N2} USD\n\nتحذير: هذا الإجراء لا يمكن التراجع عنه!",
                    "تأكيد تسوية جميع الديون",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    await ProcessBulkDebtSettlement(customersWithDebt);
                }
            }, "تسوية جميع الديون");
        }

        /// <summary>
        /// Process bulk payments command
        /// </summary>
        [RelayCommand]
        private async Task ProcessBulkPaymentsAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                var customersWithDebt = Customers?.Where(c => c != null && c.TotalDebt > 0).Take(10).ToList() ?? new List<Customer>();

                if (!customersWithDebt.Any())
                {
                    UpdateStatus("لا يوجد زبائن مديونين", "Info", "#17A2B8");
                    return;
                }

                // Process partial payments for top 10 debtors
                var totalDebt = customersWithDebt.Sum(c => c.TotalDebt);
                var result = MessageBox.Show(
                    $"هل تريد معالجة دفعات جزئية لأعلى {customersWithDebt.Count} زبون مديون؟\n\nسيتم دفع 25% من دين كل زبون.\nإجمالي المبلغ المتوقع: {totalDebt * 0.25m:N2} USD",
                    "تأكيد الدفعات الجماعية",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await ProcessBulkPartialPayments(customersWithDebt, 0.25m);
                }
            }, "الدفعات الجماعية");
        }

        // Standard Customer Management Commands with enhanced null safety

        [RelayCommand]
        private async Task AddNewCustomerAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                _logger.LogInformation("Opening add new customer dialog");

                if (_serviceProvider == null)
                {
                    throw new InvalidOperationException("Service provider is not available for customer dialog creation");
                }

                var currentWindow = GetCurrentWindowSafely();
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
            if (SelectedCustomer == null)
            {
                UpdateStatus("يرجى اختيار زبون للتعديل", "Info", "#17A2B8");
                return;
            }

            var customerToEdit = SelectedCustomer;

            await ExecuteWithErrorHandlingAsync(async () =>
            {
                _logger.LogInformation("Opening edit customer dialog for customer: {CustomerId}", customerToEdit.CustomerId);

                if (_serviceProvider == null)
                {
                    throw new InvalidOperationException("Service provider is not available for customer dialog creation");
                }

                var currentWindow = GetCurrentWindowSafely();
                var editedCustomer = await AddCustomerDialog.ShowEditCustomerDialogAsync(_serviceProvider, currentWindow, customerToEdit);

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
            if (SelectedCustomer == null)
            {
                UpdateStatus("يرجى اختيار زبون للحذف", "Info", "#17A2B8");
                return;
            }

            var customerToDelete = SelectedCustomer;

            await ExecuteWithErrorHandlingAsync(async () =>
            {
                var result = MessageBox.Show(
                    $"هل أنت متأكد من حذف الزبون '{customerToDelete.CustomerName}'؟\n\nهذا الإجراء لا يمكن التراجع عنه.",
                    "تأكيد الحذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    if (_unitOfWork?.Customers == null)
                    {
                        throw new InvalidOperationException("Customer repository is not available");
                    }

                    var canDelete = await _unitOfWork.Customers.CanDeleteCustomerAsync(customerToDelete.CustomerId);
                    if (!canDelete)
                    {
                        UpdateStatus("لا يمكن حذف الزبون لوجود معاملات مرتبطة به", "ExclamationTriangle", "#DC3545");
                        return;
                    }

                    await _unitOfWork.Customers.DeleteAsync(customerToDelete.CustomerId);
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
                await CalculateCollectionStatisticsAsync();
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
            if (SelectedCustomer == null)
            {
                UpdateStatus("يرجى اختيار زبون لعرض تاريخ المعاملات", "Info", "#17A2B8");
                return;
            }

            IsTransactionHistoryVisible = true;
            IsCustomerDetailsVisible = false;
            IsPaymentHistoryVisible = false;
            IsDebtAnalysisVisible = false;
            _ = LoadCustomerTransactionsAsync();
        }

        [RelayCommand]
        private void ShowPaymentHistory()
        {
            if (SelectedCustomer == null)
            {
                UpdateStatus("يرجى اختيار زبون لعرض تاريخ الدفعات", "Info", "#17A2B8");
                return;
            }

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
            if (SelectedCustomer == null)
            {
                UpdateStatus("يرجى اختيار زبون لإعادة حساب الرصيد", "Info", "#17A2B8");
                return;
            }

            var customerForRecalculation = SelectedCustomer;

            await ExecuteWithErrorHandlingAsync(async () =>
            {
                if (_unitOfWork?.Customers == null)
                {
                    throw new InvalidOperationException("Customer repository is not available");
                }

                var wasRecalculated = await _unitOfWork.Customers.RecalculateCustomerBalanceAsync(customerForRecalculation.CustomerId);

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

        // Pagination Commands with validation
        [RelayCommand]
        private async Task GoToPreviousPageAsync()
        {
            if (CanGoToPreviousPage)
            {
                CurrentPage = Math.Max(1, CurrentPage - 1);
                await Task.Delay(10); // Brief delay for UI responsiveness
            }
        }

        [RelayCommand]
        private async Task GoToNextPageAsync()
        {
            if (CanGoToNextPage)
            {
                CurrentPage = Math.Min(TotalPages, CurrentPage + 1);
                await Task.Delay(10); // Brief delay for UI responsiveness
            }
        }

        [RelayCommand]
        private async Task GoToFirstPageAsync()
        {
            if (CurrentPage != 1)
            {
                CurrentPage = 1;
                await Task.Delay(10); // Brief delay for UI responsiveness
            }
        }

        [RelayCommand]
        private async Task GoToLastPageAsync()
        {
            if (CurrentPage != TotalPages && TotalPages > 0)
            {
                CurrentPage = TotalPages;
                await Task.Delay(10); // Brief delay for UI responsiveness
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the customer accounts view with comprehensive data loading and null safety
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                UpdateStatus("جاري تحميل بيانات الزبائن...", "Spinner", "#007BFF");

                // Validate dependencies before initialization
                ValidateDependencies();

                await LoadCustomersPagedAsync();
                await CalculateStatisticsAsync();
                await CalculateCollectionStatisticsAsync();

                // Show customer details by default
                ShowCustomerDetails();

                UpdateStatus($"تم تحميل {TotalRecords} زبون بنجاح", "CheckCircle", "#28A745");
                _logger.LogInformation("CustomerAccountsViewModel initialized successfully with {CustomerCount} customers", TotalRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during CustomerAccountsViewModel initialization");
                var (_, userMessage) = await _errorHandlingService.HandleExceptionAsync(ex, "CustomerAccountsViewModel.InitializeAsync");
                UpdateStatus(userMessage, "ExclamationTriangle", "#DC3545");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Cleanup method for resource disposal with null safety
        /// </summary>
        public void Cleanup()
        {
            try
            {
                Customers?.Clear();
                CustomerInvoices?.Clear();
                CustomerPayments?.Clear();
                ValidationErrors?.Clear();

                _logger.LogInformation("CustomerAccountsViewModel cleanup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during CustomerAccountsViewModel cleanup");
            }
        }

        #endregion

        #region Private Methods - Validation and Safety

        /// <summary>
        /// Validates all dependencies are properly injected and available
        /// </summary>
        private void ValidateDependencies()
        {
            if (_unitOfWork == null)
                throw new InvalidOperationException("Unit of work dependency is not available");

            if (_logger == null)
                throw new InvalidOperationException("Logger dependency is not available");

            if (_errorHandlingService == null)
                throw new InvalidOperationException("Error handling service dependency is not available");

            if (_serviceProvider == null)
                throw new InvalidOperationException("Service provider dependency is not available");

            _logger.LogDebug("All dependencies validated successfully");
        }

        /// <summary>
        /// Validates preconditions for payment processing with comprehensive checks
        /// </summary>
        private bool ValidatePaymentProcessingPreconditions()
        {
            try
            {
                // Check if customer is selected
                if (SelectedCustomer == null)
                {
                    UpdateStatus("يرجى اختيار زبون لمعالجة الدفعة", "ExclamationTriangle", "#DC3545");
                    _logger.LogWarning("Payment processing attempted without selected customer");
                    return false;
                }

                // Check if customer has debt
                if (SelectedCustomer.TotalDebt <= 0)
                {
                    UpdateStatus($"الزبون '{SelectedCustomer.CustomerName}' ليس لديه ديون للتسديد", "Info", "#17A2B8");
                    _logger.LogInformation("Payment processing attempted for customer without debt: {CustomerId}", SelectedCustomer.CustomerId);
                    return false;
                }

                // Check if system is not currently loading
                if (IsLoading)
                {
                    UpdateStatus("النظام قيد التحميل، يرجى الانتظار", "Spinner", "#007BFF");
                    _logger.LogWarning("Payment processing attempted while system is loading");
                    return false;
                }

                // Check if service provider is available
                if (_serviceProvider == null)
                {
                    UpdateStatus("خدمة معالجة الدفعات غير متاحة", "ExclamationTriangle", "#DC3545");
                    _logger.LogError("Payment processing attempted without service provider");
                    return false;
                }

                // Check if unit of work is available
                if (_unitOfWork?.Payments == null)
                {
                    UpdateStatus("خدمة قاعدة البيانات غير متاحة", "ExclamationTriangle", "#DC3545");
                    _logger.LogError("Payment processing attempted without unit of work or payment repository");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during payment processing precondition validation");
                UpdateStatus("خطأ في التحقق من شروط معالجة الدفعة", "ExclamationTriangle", "#DC3545");
                return false;
            }
        }

        /// <summary>
        /// Gets the current window safely with proper null handling
        /// </summary>
        private Window? GetCurrentWindowSafely()
        {
            try
            {
                if (Application.Current?.Windows == null)
                {
                    _logger.LogWarning("Application.Current.Windows is null");
                    return null;
                }

                var activeWindow = Application.Current.Windows
                    .Cast<Window>()
                    .FirstOrDefault(w => w != null && w.IsActive);

                if (activeWindow != null)
                {
                    _logger.LogDebug("Found active window: {WindowType}", activeWindow.GetType().Name);
                    return activeWindow;
                }

                var mainWindow = Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    _logger.LogDebug("Using main window: {WindowType}", mainWindow.GetType().Name);
                    return mainWindow;
                }

                _logger.LogWarning("No suitable window found for dialog positioning");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting current window safely");
                return null;
            }
        }

        /// <summary>
        /// Calculates quick payment amount with comprehensive validation
        /// </summary>
        private decimal CalculateQuickPaymentAmount(object? parameter, decimal totalDebt)
        {
            try
            {
                decimal paymentAmount = 0;

                if (parameter is string percentageStr && decimal.TryParse(percentageStr, out var percentage))
                {
                    paymentAmount = Math.Round(totalDebt * percentage, 2);
                }
                else if (parameter is decimal amount)
                {
                    paymentAmount = amount;
                }
                else if (parameter is string amountStr && decimal.TryParse(amountStr, out var parsedAmount))
                {
                    paymentAmount = parsedAmount;
                }
                else
                {
                    // Default to full amount
                    paymentAmount = totalDebt;
                }

                // Ensure payment amount is positive and reasonable
                paymentAmount = Math.Max(0, paymentAmount);

                _logger.LogDebug("Calculated quick payment amount: {Amount:C} from parameter: {Parameter}",
                    paymentAmount, parameter);

                return paymentAmount;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating quick payment amount, using full debt amount");
                return totalDebt;
            }
        }

        #endregion

        #region Private Methods - Data Operations

        /// <summary>
        /// Initializes collection views for advanced filtering and sorting with null safety
        /// </summary>
        private void InitializeCollectionViews()
        {
            Customers = new ObservableCollection<Customer>();
            CustomerInvoices = new ObservableCollection<Invoice>();
            CustomerPayments = new ObservableCollection<Payment>();
            ValidationErrors = new ObservableCollection<string>();

            _logger.LogDebug("Collection views initialized successfully");
        }

        /// <summary>
        /// Loads customers with pagination support and comprehensive error handling
        /// </summary>
        private async Task LoadCustomersPagedAsync()
        {
            try
            {
                if (_unitOfWork?.Customers == null)
                {
                    throw new InvalidOperationException("Customer repository is not available");
                }

                var (customers, totalCount) = await _unitOfWork.Customers.GetCustomersPagedAsync(
                    CurrentPage,
                    PageSize,
                    SearchText,
                    ShowActiveOnly ? true : null);

                if (customers == null)
                {
                    _logger.LogWarning("Received null customer collection from repository");
                    customers = new List<Customer>();
                }

                Customers.Clear();
                foreach (var customer in customers.Where(c => c != null))
                {
                    Customers.Add(customer);
                }

                TotalRecords = Math.Max(0, totalCount);
                TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalRecords / PageSize));

                OnPropertyChanged(nameof(IsPaginationVisible));
                OnPropertyChanged(nameof(CanGoToPreviousPage));
                OnPropertyChanged(nameof(CanGoToNextPage));

                _logger.LogDebug("Loaded {CustomerCount} customers for page {Page} of {TotalPages}",
                    Customers.Count, CurrentPage, TotalPages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customers paged");
                throw;
            }
        }

        /// <summary>
        /// Applies search and filter criteria with null safety
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
                UpdateStatus("خطأ في تطبيق المرشحات", "ExclamationTriangle", "#DC3545");
            }
        }

        /// <summary>
        /// Refreshes customer list and statistics with comprehensive error handling
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
        /// Calculates comprehensive customer statistics with null safety
        /// </summary>
        private async Task CalculateStatisticsAsync()
        {
            try
            {
                if (_unitOfWork?.Customers == null)
                {
                    _logger.LogWarning("Cannot calculate statistics - customer repository not available");
                    return;
                }

                TotalCustomersCount = await _unitOfWork.Customers.CountAsync();
                ActiveCustomersCount = await _unitOfWork.Customers.GetActiveCustomerCountAsync();

                var (totalDebt, customersWithDebtCount) = await _unitOfWork.Customers.GetDebtSummaryAsync();
                TotalDebtAmount = Math.Max(0, totalDebt);
                CustomersWithDebtCount = Math.Max(0, customersWithDebtCount);

                AverageDebtPerCustomer = CustomersWithDebtCount > 0 ? TotalDebtAmount / CustomersWithDebtCount : 0;

                _logger.LogDebug("Customer statistics calculated - Total: {Total}, Active: {Active}, Debt: {Debt:C}, AvgDebt: {AvgDebt:C}",
                    TotalCustomersCount, ActiveCustomersCount, TotalDebtAmount, AverageDebtPerCustomer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating customer statistics");
                // Don't throw, just log the error to avoid breaking the UI
            }
        }

        /// <summary>
        /// Calculates collection and payment statistics with comprehensive validation
        /// </summary>
        private async Task CalculateCollectionStatisticsAsync()
        {
            try
            {
                if (_unitOfWork?.Payments == null)
                {
                    _logger.LogWarning("Cannot calculate collection statistics - payment repository not available");
                    return;
                }

                var today = DateTime.Today;
                var startOfDay = today;
                var endOfDay = today.AddDays(1);

                // Get today's payment summary
                var (totalAmount, paymentCount) = await _unitOfWork.Payments.GetPaymentsSummaryAsync(startOfDay, endOfDay);
                TotalCollectionsToday = Math.Max(0, totalAmount);
                PaymentsProcessedToday = Math.Max(0, paymentCount);

                // Calculate this month's collections
                var startOfMonth = new DateTime(today.Year, today.Month, 1);
                var (monthlyTotal, monthlyCount) = await _unitOfWork.Payments.GetPaymentsSummaryAsync(startOfMonth, endOfDay);
                TotalCollectionsThisMonth = Math.Max(0, monthlyTotal);

                // Calculate average payment amount (last 30 days)
                var thirtyDaysAgo = DateTime.Today.AddDays(-30);
                var (monthlyAvgTotal, monthlyAvgCount) = await _unitOfWork.Payments.GetPaymentsSummaryAsync(thirtyDaysAgo, endOfDay);
                AveragePaymentAmount = monthlyAvgCount > 0 ? monthlyAvgTotal / monthlyAvgCount : 0;

                // Calculate collection efficiency rate
                if (TotalDebtAmount > 0)
                {
                    CollectionEfficiencyRate = Math.Min(1.0m, TotalCollectionsThisMonth / TotalDebtAmount);
                }
                else
                {
                    CollectionEfficiencyRate = 0;
                }

                _logger.LogDebug("Collection statistics calculated - Today: {TodayAmount:C}, Count: {TodayCount}, MonthlyTotal: {MonthlyTotal:C}, Avg: {AvgAmount:C}, Efficiency: {Efficiency:P}",
                    TotalCollectionsToday, PaymentsProcessedToday, TotalCollectionsThisMonth, AveragePaymentAmount, CollectionEfficiencyRate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating collection statistics");
                // Don't throw, just log the error to avoid breaking the UI
            }
        }

        /// <summary>
        /// Refreshes data after payment processing with comprehensive error handling
        /// </summary>
        private async Task RefreshAfterPaymentAsync(Payment processedPayment)
        {
            try
            {
                if (processedPayment == null)
                {
                    _logger.LogWarning("Attempted to refresh after null payment");
                    return;
                }

                // Refresh customer data
                await RefreshCustomersAsync();

                // Refresh customer transactions if visible
                if (SelectedCustomer != null)
                {
                    if (IsPaymentHistoryVisible)
                    {
                        await LoadCustomerPaymentsAsync();
                    }

                    if (IsTransactionHistoryVisible)
                    {
                        await LoadCustomerTransactionsAsync();
                    }
                }

                // Update collection statistics
                await CalculateCollectionStatisticsAsync();

                // Update customer selection to reflect new balance
                if (SelectedCustomer != null)
                {
                    var updatedCustomer = Customers?.FirstOrDefault(c => c != null && c.CustomerId == SelectedCustomer.CustomerId);
                    if (updatedCustomer != null)
                    {
                        SelectedCustomer = updatedCustomer;
                    }
                }

                _logger.LogDebug("Data refreshed successfully after payment processing. PaymentId: {PaymentId}",
                    processedPayment.PaymentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing data after payment processing");
                UpdateStatus("تم معالجة الدفعة ولكن حدث خطأ في تحديث البيانات", "ExclamationTriangle", "#FFC107");
            }
        }

        /// <summary>
        /// Handles customer selection changes and loads related data with null safety
        /// </summary>
        private async void OnCustomerSelectionChanged()
        {
            try
            {
                if (SelectedCustomer != null)
                {
                    _logger.LogDebug("Customer selected: {CustomerName} (ID: {CustomerId}, Debt: {Debt:C})",
                        SelectedCustomer.CustomerName, SelectedCustomer.CustomerId, SelectedCustomer.TotalDebt);

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
                    CustomerInvoices?.Clear();
                    CustomerPayments?.Clear();
                    _logger.LogDebug("Customer selection cleared");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling customer selection change");
                UpdateStatus("خطأ في تحميل بيانات الزبون المحدد", "ExclamationTriangle", "#DC3545");
            }
        }

        /// <summary>
        /// Loads transaction history for selected customer with comprehensive validation
        /// </summary>
        private async Task LoadCustomerTransactionsAsync()
        {
            try
            {
                if (SelectedCustomer == null)
                {
                    _logger.LogDebug("Cannot load transactions - no customer selected");
                    return;
                }

                if (_unitOfWork?.Customers == null)
                {
                    _logger.LogWarning("Cannot load customer transactions - customer repository not available");
                    return;
                }

                var invoices = await _unitOfWork.Customers.GetCustomerInvoicesAsync(
                    SelectedCustomer.CustomerId, StartDate, EndDate);

                CustomerInvoices.Clear();
                if (invoices != null)
                {
                    foreach (var invoice in invoices.Where(i => i != null))
                    {
                        CustomerInvoices.Add(invoice);
                    }
                }

                _logger.LogDebug("Loaded {InvoiceCount} invoices for customer {CustomerId}",
                    CustomerInvoices.Count, SelectedCustomer.CustomerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer transactions for customer {CustomerId}",
                    SelectedCustomer?.CustomerId);
                UpdateStatus("خطأ في تحميل معاملات الزبون", "ExclamationTriangle", "#DC3545");
            }
        }

        /// <summary>
        /// Loads payment history for selected customer with comprehensive validation
        /// </summary>
        private async Task LoadCustomerPaymentsAsync()
        {
            try
            {
                if (SelectedCustomer == null)
                {
                    _logger.LogDebug("Cannot load payments - no customer selected");
                    return;
                }

                if (_unitOfWork?.Customers == null)
                {
                    _logger.LogWarning("Cannot load customer payments - customer repository not available");
                    return;
                }

                var payments = await _unitOfWork.Customers.GetCustomerPaymentsAsync(
                    SelectedCustomer.CustomerId, StartDate, EndDate);

                CustomerPayments.Clear();
                if (payments != null)
                {
                    foreach (var payment in payments.Where(p => p != null))
                    {
                        CustomerPayments.Add(payment);
                    }
                }

                _logger.LogDebug("Loaded {PaymentCount} payments for customer {CustomerId}",
                    CustomerPayments.Count, SelectedCustomer.CustomerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customer payments for customer {CustomerId}",
                    SelectedCustomer?.CustomerId);
                UpdateStatus("خطأ في تحميل دفعات الزبون", "ExclamationTriangle", "#DC3545");
            }
        }

        #endregion

        #region Private Methods - Bulk Operations

        /// <summary>
        /// Processes bulk debt settlement with comprehensive error handling
        /// </summary>
        private async Task ProcessBulkDebtSettlement(List<Customer> customers)
        {
            if (customers == null || !customers.Any())
            {
                UpdateStatus("لا يوجد زبائن للتسوية", "Info", "#17A2B8");
                return;
            }

            int settledCount = 0;
            decimal totalSettled = 0;
            var failedCustomers = new List<string>();

            foreach (var customer in customers.Where(c => c != null))
            {
                try
                {
                    var payment = new Payment
                    {
                        CustomerId = customer.CustomerId,
                        Amount = customer.TotalDebt,
                        PaymentMethod = "CASH",
                        PaymentDate = DateTime.Now,
                        Notes = "تسوية شاملة للديون",
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    };

                    if (_unitOfWork?.Payments != null)
                    {
                        await _unitOfWork.Payments.CreatePaymentWithTransactionAsync(payment);
                        settledCount++;
                        totalSettled += payment.Amount;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to settle debt for customer {CustomerId}", customer.CustomerId);
                    failedCustomers.Add(customer.CustomerName ?? $"Customer {customer.CustomerId}");
                }
            }

            try
            {
                if (_unitOfWork != null)
                {
                    await _unitOfWork.SaveChangesAsync("BULK_DEBT_SETTLEMENT");
                }

                await RefreshCustomersAsync();
                await CalculateCollectionStatisticsAsync();

                var statusMessage = $"تم تسوية ديون {settledCount} زبون بإجمالي {totalSettled:N2} USD";
                if (failedCustomers.Any())
                {
                    statusMessage += $" (فشل في تسوية {failedCustomers.Count} زبون)";
                }

                UpdateStatus(statusMessage, "CheckCircle", "#28A745");

                if (failedCustomers.Any())
                {
                    MessageBox.Show(
                        $"تم تسوية معظم الديون بنجاح.\n\nفشل في تسوية ديون الزبائن التاليين:\n{string.Join("\n", failedCustomers)}",
                        "تقرير التسوية",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk debt settlement finalization");
                UpdateStatus("حدث خطأ أثناء حفظ التسويات", "ExclamationTriangle", "#DC3545");
            }
        }

        /// <summary>
        /// Processes bulk partial payments with comprehensive error handling
        /// </summary>
        private async Task ProcessBulkPartialPayments(List<Customer> customers, decimal paymentPercentage)
        {
            if (customers == null || !customers.Any())
            {
                UpdateStatus("لا يوجد زبائن للدفعات الجماعية", "Info", "#17A2B8");
                return;
            }

            int processedCount = 0;
            decimal totalProcessed = 0;
            var failedCustomers = new List<string>();

            foreach (var customer in customers.Where(c => c != null))
            {
                try
                {
                    var paymentAmount = Math.Round(customer.TotalDebt * paymentPercentage, 2);
                    if (paymentAmount > 0)
                    {
                        var payment = new Payment
                        {
                            CustomerId = customer.CustomerId,
                            Amount = paymentAmount,
                            PaymentMethod = "CASH",
                            PaymentDate = DateTime.Now,
                            Notes = $"دفعة جماعية - {paymentPercentage:P0} من الدين",
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        };

                        if (_unitOfWork?.Payments != null)
                        {
                            await _unitOfWork.Payments.CreatePaymentWithTransactionAsync(payment);
                            processedCount++;
                            totalProcessed += paymentAmount;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to process bulk payment for customer {CustomerId}", customer.CustomerId);
                    failedCustomers.Add(customer.CustomerName ?? $"Customer {customer.CustomerId}");
                }
            }

            try
            {
                if (_unitOfWork != null)
                {
                    await _unitOfWork.SaveChangesAsync("BULK_PAYMENTS");
                }

                await RefreshCustomersAsync();
                await CalculateCollectionStatisticsAsync();

                var statusMessage = $"تم معالجة {processedCount} دفعة جماعية بإجمالي {totalProcessed:N2} USD";
                if (failedCustomers.Any())
                {
                    statusMessage += $" (فشل في معالجة {failedCustomers.Count} دفعة)";
                }

                UpdateStatus(statusMessage, "CheckCircle", "#28A745");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk payments finalization");
                UpdateStatus("حدث خطأ أثناء حفظ الدفعات الجماعية", "ExclamationTriangle", "#DC3545");
            }
        }

        #endregion

        #region Private Methods - Error Handling and UI

        /// <summary>
        /// Executes an operation with comprehensive error handling and user feedback
        /// </summary>
        private async Task ExecuteWithErrorHandlingAsync(Func<Task> operation, string operationName)
        {
            if (operation == null)
            {
                _logger.LogWarning("Attempted to execute null operation: {OperationName}", operationName);
                return;
            }

            try
            {
                IsLoading = true;
                await operation();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing operation: {OperationName}", operationName);

                if (_errorHandlingService != null)
                {
                    var (success, userMessage) = await _errorHandlingService.HandleExceptionAsync(ex, operationName);
                    UpdateStatus(userMessage, "ExclamationTriangle", "#DC3545");
                }
                else
                {
                    UpdateStatus($"خطأ في {operationName}", "ExclamationTriangle", "#DC3545");
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Updates the status display for user feedback with null safety
        /// </summary>
        private void UpdateStatus(string message, string icon, string color)
        {
            StatusMessage = message ?? "حالة غير معروفة";
            StatusIcon = icon ?? "Info";
            StatusColor = color ?? "#6C757D";

            _logger.LogDebug("Status updated: {Message}", StatusMessage);
        }

        #endregion
    }
}