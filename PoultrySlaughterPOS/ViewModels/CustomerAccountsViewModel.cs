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
            _logger.LogInformation("CustomerAccountsViewModel initialized with enhanced debt settlement capabilities");
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
                    OnPropertyChanged(nameof(CanProcessPayment));
                    OnPropertyChanged(nameof(HasSelectedCustomerWithDebt));
                    OnPropertyChanged(nameof(SelectedCustomerDebtAmount));
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

        // ENHANCED: Debt Settlement Properties

        /// <summary>
        /// Total collections processed today
        /// </summary>
        public decimal TotalCollectionsToday
        {
            get => _totalCollectionsToday;
            set => SetProperty(ref _totalCollectionsToday, value);
        }

        /// <summary>
        /// Number of payments processed today
        /// </summary>
        public int PaymentsProcessedToday
        {
            get => _paymentsProcessedToday;
            set => SetProperty(ref _paymentsProcessedToday, value);
        }

        /// <summary>
        /// Average payment amount across all customers
        /// </summary>
        public decimal AveragePaymentAmount
        {
            get => _averagePaymentAmount;
            set => SetProperty(ref _averagePaymentAmount, value);
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
            set => SetProperty(ref _totalCollectionsThisMonth, value);
        }

        /// <summary>
        /// Collection efficiency rate (collections vs total debt)
        /// </summary>
        public decimal CollectionEfficiencyRate
        {
            get => _collectionEfficiencyRate;
            set => SetProperty(ref _collectionEfficiencyRate, value);
        }

        /// <summary>
        /// Indicates whether a payment can be processed for selected customer
        /// </summary>
        public bool CanProcessPayment => SelectedCustomer != null && SelectedCustomer.TotalDebt > 0;

        /// <summary>
        /// Indicates whether selected customer has outstanding debt
        /// </summary>
        public bool HasSelectedCustomerWithDebt => SelectedCustomer?.TotalDebt > 0;

        /// <summary>
        /// Selected customer's debt amount for display
        /// </summary>
        public decimal SelectedCustomerDebtAmount => SelectedCustomer?.TotalDebt ?? 0;

        #endregion

        #region Commands

        // ENHANCED: Debt Settlement Commands

        [RelayCommand]
        private async Task ProcessPaymentAsync()
        {
            if (SelectedCustomer == null) return;

            await ExecuteWithErrorHandlingAsync(async () =>
            {
                _logger.LogInformation("Opening payment dialog for customer: {CustomerName} (Debt: {Debt})",
                    SelectedCustomer.CustomerName, SelectedCustomer.TotalDebt);

                var currentWindow = GetCurrentWindow();
                var processedPayment = await PaymentDialog.ShowPaymentDialogAsync(
                    _serviceProvider, currentWindow, SelectedCustomer);

                if (processedPayment != null)
                {
                    await RefreshAfterPaymentAsync(processedPayment);
                    UpdateStatus($"تم تسجيل دفعة بمبلغ {processedPayment.Amount:N2} USD للزبون '{SelectedCustomer.CustomerName}'",
                        "CheckCircle", "#28A745");
                }
            }, "معالجة دفعة");
        }

        [RelayCommand]
        private async Task QuickPaymentAsync(object? parameter)
        {
            if (SelectedCustomer == null) return;

            await ExecuteWithErrorHandlingAsync(async () =>
            {
                decimal paymentAmount = 0;

                // Determine payment amount based on parameter
                if (parameter is string percentageStr && decimal.TryParse(percentageStr, out var percentage))
                {
                    paymentAmount = Math.Round(SelectedCustomer.TotalDebt * percentage, 2);
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
                    paymentAmount = SelectedCustomer.TotalDebt;
                }

                if (paymentAmount <= 0 || paymentAmount > SelectedCustomer.TotalDebt * 2)
                {
                    UpdateStatus("مبلغ الدفعة غير صحيح", "ExclamationTriangle", "#DC3545");
                    return;
                }

                // Confirm quick payment
                var result = MessageBox.Show(
                    $"هل تريد تسجيل دفعة سريعة بمبلغ {paymentAmount:N2} USD للزبون '{SelectedCustomer.CustomerName}'؟",
                    "تأكيد الدفعة السريعة",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Create payment directly
                    var payment = new Payment
                    {
                        CustomerId = SelectedCustomer.CustomerId,
                        Amount = paymentAmount,
                        PaymentMethod = "CASH",
                        PaymentDate = DateTime.Now,
                        Notes = "دفعة سريعة",
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    };

                    var processedPayment = await _unitOfWork.Payments.CreatePaymentWithTransactionAsync(payment);
                    await _unitOfWork.SaveChangesAsync("QUICK_PAYMENT");

                    await RefreshAfterPaymentAsync(processedPayment);
                    UpdateStatus($"تم تسجيل دفعة سريعة بمبلغ {paymentAmount:N2} USD", "CheckCircle", "#28A745");
                }
            }, "دفعة سريعة");
        }

        [RelayCommand]
        private async Task ViewPaymentHistoryAsync()
        {
            if (SelectedCustomer == null) return;

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

        [RelayCommand]
        private async Task RefreshCollectionStatisticsAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                await CalculateCollectionStatisticsAsync();
                UpdateStatus("تم تحديث إحصائيات التحصيل", "ChartBar", "#007BFF");
            }, "تحديث إحصائيات التحصيل");
        }

        [RelayCommand]
        private async Task SettleAllDebtsAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                var customersWithDebt = Customers.Where(c => c.TotalDebt > 0).ToList();

                if (!customersWithDebt.Any())
                {
                    UpdateStatus("لا يوجد زبائن مديونين", "Info", "#17A2B8");
                    return;
                }

                var result = MessageBox.Show(
                    $"هل تريد تسوية جميع ديون الزبائن ({customersWithDebt.Count} زبون)؟\n\nإجمالي المبلغ: {customersWithDebt.Sum(c => c.TotalDebt):N2} USD\n\nتحذير: هذا الإجراء لا يمكن التراجع عنه!",
                    "تأكيد تسوية جميع الديون",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    int settledCount = 0;
                    decimal totalSettled = 0;
                    var failedCustomers = new List<string>();

                    foreach (var customer in customersWithDebt)
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

                            await _unitOfWork.Payments.CreatePaymentWithTransactionAsync(payment);
                            settledCount++;
                            totalSettled += payment.Amount;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to settle debt for customer {CustomerId}", customer.CustomerId);
                            failedCustomers.Add(customer.CustomerName);
                        }
                    }

                    await _unitOfWork.SaveChangesAsync("BULK_DEBT_SETTLEMENT");
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
            }, "تسوية جميع الديون");
        }

        [RelayCommand]
        private async Task ProcessBulkPaymentsAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                var customersWithDebt = Customers.Where(c => c.TotalDebt > 0).Take(10).ToList();

                if (!customersWithDebt.Any())
                {
                    UpdateStatus("لا يوجد زبائن مديونين", "Info", "#17A2B8");
                    return;
                }

                // Process partial payments for top 10 debtors
                var result = MessageBox.Show(
                    $"هل تريد معالجة دفعات جزئية لأعلى {customersWithDebt.Count} زبون مديون؟\n\nسيتم دفع 25% من دين كل زبون.",
                    "تأكيد الدفعات الجماعية",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    int processedCount = 0;
                    decimal totalProcessed = 0;

                    foreach (var customer in customersWithDebt)
                    {
                        try
                        {
                            var paymentAmount = Math.Round(customer.TotalDebt * 0.25m, 2);
                            if (paymentAmount > 0)
                            {
                                var payment = new Payment
                                {
                                    CustomerId = customer.CustomerId,
                                    Amount = paymentAmount,
                                    PaymentMethod = "CASH",
                                    PaymentDate = DateTime.Now,
                                    Notes = "دفعة جماعية - 25% من الدين",
                                    CreatedDate = DateTime.Now,
                                    UpdatedDate = DateTime.Now
                                };

                                await _unitOfWork.Payments.CreatePaymentWithTransactionAsync(payment);
                                processedCount++;
                                totalProcessed += paymentAmount;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to process bulk payment for customer {CustomerId}", customer.CustomerId);
                        }
                    }

                    await _unitOfWork.SaveChangesAsync("BULK_PAYMENTS");
                    await RefreshCustomersAsync();
                    await CalculateCollectionStatisticsAsync();

                    UpdateStatus($"تم معالجة {processedCount} دفعة جماعية بإجمالي {totalProcessed:N2} USD", "CheckCircle", "#28A745");
                }
            }, "الدفعات الجماعية");
        }

        // Standard Customer Management Commands

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

        // Pagination Commands
        [RelayCommand]
        private async Task GoToPreviousPageAsync()
        {
            if (CanGoToPreviousPage)
            {
                CurrentPage--;
                await Task.Delay(10); // Brief delay for UI responsiveness
            }
        }

        [RelayCommand]
        private async Task GoToNextPageAsync()
        {
            if (CanGoToNextPage)
            {
                CurrentPage++;
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
            if (CurrentPage != TotalPages)
            {
                CurrentPage = TotalPages;
                await Task.Delay(10); // Brief delay for UI responsiveness
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
                await CalculateCollectionStatisticsAsync();

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
        /// Calculates collection and payment statistics
        /// </summary>
        private async Task CalculateCollectionStatisticsAsync()
        {
            try
            {
                var today = DateTime.Today;
                var startOfDay = today;
                var endOfDay = today.AddDays(1);

                // Get today's payment summary
                var (totalAmount, paymentCount) = await _unitOfWork.Payments.GetPaymentsSummaryAsync(startOfDay, endOfDay);
                TotalCollectionsToday = totalAmount;
                PaymentsProcessedToday = paymentCount;

                // Calculate this month's collections
                var startOfMonth = new DateTime(today.Year, today.Month, 1);
                var (monthlyTotal, monthlyCount) = await _unitOfWork.Payments.GetPaymentsSummaryAsync(startOfMonth, endOfDay);
                TotalCollectionsThisMonth = monthlyTotal;

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

                _logger.LogDebug("Collection statistics calculated - Today: {TodayAmount}, Count: {TodayCount}, Avg: {AvgAmount}, Efficiency: {Efficiency:P}",
                    TotalCollectionsToday, PaymentsProcessedToday, AveragePaymentAmount, CollectionEfficiencyRate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating collection statistics");
            }
        }

        /// <summary>
        /// Refreshes data after payment processing
        /// </summary>
        private async Task RefreshAfterPaymentAsync(Payment processedPayment)
        {
            try
            {
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
                    var updatedCustomer = Customers.FirstOrDefault(c => c.CustomerId == SelectedCustomer.CustomerId);
                    if (updatedCustomer != null)
                    {
                        SelectedCustomer = updatedCustomer;
                    }
                }

                _logger.LogDebug("Data refreshed after payment processing. PaymentId: {PaymentId}",
                    processedPayment.PaymentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing data after payment processing");
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