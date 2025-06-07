// PoultrySlaughterPOS/ViewModels/POSViewModel.cs
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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Printing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Documents;

namespace PoultrySlaughterPOS.ViewModels
{
    /// <summary>
    /// Enhanced Point of Sale ViewModel with comprehensive invoice search and edit capabilities.
    /// Supports both new invoice creation and existing invoice modification with seamless data flow.
    /// </summary>
    public partial class POSViewModel : ObservableObject
    {
        #region Private Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<POSViewModel> _logger;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly IServiceProvider _serviceProvider;
        private bool _isLoading = false;
        private bool _hasValidationErrors = false;

        // Collections for UI binding
        private ObservableCollection<Customer> _customers = new();
        private ObservableCollection<Truck> _trucks = new();
        private ObservableCollection<InvoiceItem> _invoiceItems = new();
        private ObservableCollection<string> _validationErrors = new();

        // Current selections and state
        private Customer? _selectedCustomer;
        private Truck? _selectedTruck;
        private Invoice _currentInvoice = new();
        private DateTime _currentDateTime = DateTime.Now;

        // Calculated totals
        private decimal _totalNetWeight = 0;
        private decimal _totalAmount = 0;
        private decimal _totalDiscount = 0;
        private decimal _finalTotal = 0;

        // Status and UI state
        private string _statusMessage = "جاهز لإنشاء فاتورة جديدة";
        private string _statusIcon = "CheckCircle";
        private string _statusColor = "#28A745";

        // NEW: Invoice Search Properties
        private string _invoiceSearchTerm = string.Empty;
        private ObservableCollection<InvoiceSearchResult> _invoiceSearchResults = new();
        private InvoiceSearchResult? _selectedInvoiceSearchResult;
        private bool _isInvoiceSearchVisible = false;
        private bool _isEditMode = false;
        private int? _editingInvoiceId = null;

        #endregion

        #region Payment Transaction Properties

        /// <summary>
        /// Total amount due for current transaction (readonly calculated)
        /// </summary>
        public decimal AmountDue => FinalTotal;

        private decimal _paymentReceived = 0;
        /// <summary>
        /// Payment amount received from customer during transaction
        /// </summary>
        public decimal PaymentReceived
        {
            get => _paymentReceived;
            set
            {
                if (SetProperty(ref _paymentReceived, Math.Max(0, value)))
                {
                    CalculateRemainingBalance();
                    OnPropertyChanged(nameof(PaymentReceivedDisplay));
                    OnPropertyChanged(nameof(RemainingBalance));
                    OnPropertyChanged(nameof(RemainingBalanceDisplay));
                    OnPropertyChanged(nameof(IsFullyPaid));
                    OnPropertyChanged(nameof(IsPartialPayment));
                    OnPropertyChanged(nameof(IsOverpayment));
                    NotifyValidationStateChanged();
                }
            }
        }

        /// <summary>
        /// Formatted payment received display
        /// </summary>
        public string PaymentReceivedDisplay
        {
            get => PaymentReceived.ToString("F2");
            set
            {
                if (decimal.TryParse(value, out decimal amount))
                {
                    PaymentReceived = amount;
                }
            }
        }

        private decimal _remainingBalance = 0;
        /// <summary>
        /// Remaining balance after payment (will be added to customer debt if positive)
        /// </summary>
        public decimal RemainingBalance
        {
            get => _remainingBalance;
            private set => SetProperty(ref _remainingBalance, value);
        }

        /// <summary>
        /// Formatted remaining balance display
        /// </summary>
        public string RemainingBalanceDisplay => $"{RemainingBalance:F2}";

        /// <summary>
        /// Amount due display formatted for UI
        /// </summary>
        public string AmountDueDisplay => $"{AmountDue:F2}";

        /// <summary>
        /// Indicates if transaction is fully paid
        /// </summary>
        public bool IsFullyPaid => PaymentReceived >= AmountDue && AmountDue > 0;

        /// <summary>
        /// Indicates if transaction has partial payment
        /// </summary>
        public bool IsPartialPayment => PaymentReceived > 0 && PaymentReceived < AmountDue;

        /// <summary>
        /// Indicates if payment exceeds amount due
        /// </summary>
        public bool IsOverpayment => PaymentReceived > AmountDue && AmountDue > 0;

        private string _paymentMethod = "CASH";
        /// <summary>
        /// Payment method for current transaction
        /// </summary>
        public string PaymentMethod
        {
            get => _paymentMethod;
            set => SetProperty(ref _paymentMethod, value ?? "CASH");
        }

        /// <summary>
        /// Available payment methods
        /// </summary>
        public List<string> AvailablePaymentMethods { get; } = new()
        {
            "CASH", "CHECK", "BANK_TRANSFER", "CREDIT_CARD", "DEBIT_CARD"
        };

        private string _paymentNotes = string.Empty;
        /// <summary>
        /// Optional payment notes
        /// </summary>
        public string PaymentNotes
        {
            get => _paymentNotes;
            set => SetProperty(ref _paymentNotes, value ?? string.Empty);
        }

        #endregion

        #region Invoice Search Properties

        /// <summary>
        /// Search term for finding existing invoices
        /// </summary>
        public string InvoiceSearchTerm
        {
            get => _invoiceSearchTerm;
            set
            {
                if (SetProperty(ref _invoiceSearchTerm, value ?? string.Empty))
                {
                    _ = PerformInvoiceSearchAsync();
                }
            }
        }

        /// <summary>
        /// Collection of search results for invoice lookup
        /// </summary>
        public ObservableCollection<InvoiceSearchResult> InvoiceSearchResults
        {
            get => _invoiceSearchResults;
            set => SetProperty(ref _invoiceSearchResults, value);
        }

        /// <summary>
        /// Currently selected invoice from search results
        /// </summary>
        public InvoiceSearchResult? SelectedInvoiceSearchResult
        {
            get => _selectedInvoiceSearchResult;
            set
            {
                // Clear previous selection
                if (_selectedInvoiceSearchResult != null)
                {
                    _selectedInvoiceSearchResult.IsSelected = false;
                }

                if (SetProperty(ref _selectedInvoiceSearchResult, value))
                {
                    // Set new selection
                    if (_selectedInvoiceSearchResult != null)
                    {
                        _selectedInvoiceSearchResult.IsSelected = true;
                    }

                    // Notify command availability changes
                    LoadSelectedInvoiceCommand.NotifyCanExecuteChanged();
                    OnPropertyChanged(nameof(HasSelectedInvoiceResult));
                }
            }
        }
        /// <summary>
        /// Indicates if there's a selected search result
        /// </summary>
        public bool HasSelectedInvoiceResult => SelectedInvoiceSearchResult != null;

        /// <summary>
        /// NEW: Command to select a search result item
        /// </summary>
        [RelayCommand]
        private async Task SelectInvoiceSearchResultAsync(InvoiceSearchResult searchResult)
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                if (searchResult == null) return;

                SelectedInvoiceSearchResult = searchResult;

                _logger.LogDebug("Selected invoice search result: {InvoiceNumber}", searchResult.InvoiceNumber);

                await Task.CompletedTask;
            }, "تحديد نتيجة البحث");
        }

        /// <summary>
        /// Controls visibility of invoice search section
        /// </summary>
        public bool IsInvoiceSearchVisible
        {
            get => _isInvoiceSearchVisible;
            set => SetProperty(ref _isInvoiceSearchVisible, value);
        }

        /// <summary>
        /// Indicates if currently editing an existing invoice
        /// </summary>
        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                if (SetProperty(ref _isEditMode, value))
                {
                    OnPropertyChanged(nameof(CurrentModeDisplay));
                    OnPropertyChanged(nameof(SaveButtonText));
                    NotifyValidationStateChanged();
                }
            }
        }

        /// <summary>
        /// Display text showing current operation mode
        /// </summary>
        public string CurrentModeDisplay => IsEditMode
            ? $"تعديل الفاتورة رقم: {CurrentInvoice?.InvoiceNumber ?? "غير محدد"}"
            : "إنشاء فاتورة جديدة";

        /// <summary>
        /// Dynamic save button text based on mode
        /// </summary>
        public string SaveButtonText => IsEditMode ? "تحديث الفاتورة" : "حفظ الفاتورة";

        #endregion

        #region Observable Properties

        /// <summary>
        /// Collection of active customers for selection
        /// </summary>
        public ObservableCollection<Customer> Customers
        {
            get => _customers;
            set => SetProperty(ref _customers, value);
        }

        /// <summary>
        /// Collection of available trucks for invoice assignment
        /// </summary>
        public ObservableCollection<Truck> AvailableTrucks
        {
            get => _trucks;
            set => SetProperty(ref _trucks, value);
        }

        /// <summary>
        /// Collection of invoice items for bulk processing
        /// </summary>
        public ObservableCollection<InvoiceItem> InvoiceItems
        {
            get => _invoiceItems;
            set
            {
                if (SetProperty(ref _invoiceItems, value))
                {
                    // Subscribe to collection changes for real-time calculations
                    if (_invoiceItems != null)
                    {
                        _invoiceItems.CollectionChanged += InvoiceItems_CollectionChanged;

                        // Subscribe to property changes of existing items
                        foreach (var item in _invoiceItems)
                        {
                            item.PropertyChanged += InvoiceItem_PropertyChanged;
                        }
                    }

                    RecalculateTotals();
                }
            }
        }

        /// <summary>
        /// Currently selected customer for invoice
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
        /// Currently selected truck for invoice
        /// </summary>
        public Truck? SelectedTruck
        {
            get => _selectedTruck;
            set
            {
                if (SetProperty(ref _selectedTruck, value))
                {
                    OnTruckSelectionChanged();
                }
            }
        }

        /// <summary>
        /// Current invoice being processed
        /// </summary>
        public Invoice CurrentInvoice
        {
            get => _currentInvoice;
            set => SetProperty(ref _currentInvoice, value);
        }

        /// <summary>
        /// Current date and time for invoice
        /// </summary>
        public DateTime CurrentDateTime
        {
            get => _currentDateTime;
            set => SetProperty(ref _currentDateTime, value);
        }

        /// <summary>
        /// Total net weight across all invoice items
        /// </summary>
        public decimal TotalNetWeight
        {
            get => _totalNetWeight;
            set => SetProperty(ref _totalNetWeight, value);
        }

        /// <summary>
        /// Total amount before discount across all items
        /// </summary>
        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        /// <summary>
        /// Total discount amount across all items
        /// </summary>
        public decimal TotalDiscount
        {
            get => _totalDiscount;
            set => SetProperty(ref _totalDiscount, value);
        }

        /// <summary>
        /// Final total after all discounts
        /// </summary>
        public decimal FinalTotal
        {
            get => _finalTotal;
            set => SetProperty(ref _finalTotal, value);
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
        /// Status message for UI feedback
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

        /// <summary>
        /// Indicates whether the current invoice can be saved
        /// </summary>
        public bool CanSaveInvoice => ValidateCurrentInvoice(false);

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes POSViewModel with comprehensive dependency injection for bulk invoice processing
        /// </summary>
        public POSViewModel(
         IUnitOfWork unitOfWork,
         ILogger<POSViewModel> logger,
         IErrorHandlingService errorHandlingService,
         IServiceProvider serviceProvider)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            InitializeCommands();
            InitializeCurrentInvoiceWithTempNumber();
            InitializeInvoiceItems();

            _logger.LogInformation("POSViewModel initialized with invoice search and edit capabilities");
        }
        #endregion

        #region Commands

        [RelayCommand]
        private async Task AddInvoiceItemAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                _logger.LogInformation("Adding new invoice item to collection");

                var newItem = new InvoiceItem
                {
                    InvoiceDate = DateTime.Today,
                    GrossWeight = 0,
                    CagesCount = 0,
                    CageWeight = 0,
                    UnitPrice = 0,
                    DiscountPercentage = 0
                };

                // Subscribe to property changes for real-time calculations
                newItem.PropertyChanged += InvoiceItem_PropertyChanged;

                InvoiceItems.Add(newItem);

                UpdateStatus("تم إضافة صف جديد بنجاح", "Plus", "#27AE60");
                _logger.LogDebug("New invoice item added. Total items: {Count}", InvoiceItems.Count);

                await Task.CompletedTask;
            }, "إضافة صف جديد");
        }

        [RelayCommand]
        private async Task RemoveInvoiceItemAsync(InvoiceItem item)
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                if (InvoiceItems.Count <= 1)
                {
                    UpdateStatus("لا يمكن حذف آخر صف في الفاتورة", "ExclamationTriangle", "#E74C3C");
                    return;
                }

                _logger.LogInformation("Removing invoice item from collection");

                // Unsubscribe from property changes
                if (item != null)
                {
                    item.PropertyChanged -= InvoiceItem_PropertyChanged;
                    InvoiceItems.Remove(item);
                }

                UpdateStatus("تم حذف الصف بنجاح", "Trash", "#E74C3C");
                _logger.LogDebug("Invoice item removed. Remaining items: {Count}", InvoiceItems.Count);

                await Task.CompletedTask;
            }, "حذف صف");
        }

        [RelayCommand(CanExecute = nameof(CanExecuteSaveAndPrintInvoice))]
        private async Task SaveAndPrintInvoiceAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                _logger.LogInformation("Executing SaveAndPrintInvoice with payment processing");

                var transactionResult = await ProcessTransactionWithPaymentAsync();
                if (transactionResult?.Success == true)
                {
                    await PrintBulkInvoiceAsync(transactionResult.Invoice!);

                    if (IsEditMode)
                    {
                        UpdateStatus($"تم تحديث وطباعة الفاتورة بنجاح", "CheckCircle", "#28A745");
                    }
                    else
                    {
                        await ResetForNewInvoiceAsync();
                        UpdateStatus($"تم حفظ وطباعة الفاتورة مع معالجة الدفعة بنجاح", "CheckCircle", "#28A745");
                    }
                }
            }, "حفظ وطباعة الفاتورة مع الدفعة");
        }

        [RelayCommand(CanExecute = nameof(CanExecuteSaveInvoice))]
        private async Task SaveInvoiceAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                _logger.LogInformation("Executing SaveInvoice with payment processing");

                var transactionResult = await ProcessTransactionWithPaymentAsync();
                if (transactionResult?.Success == true)
                {
                    if (IsEditMode)
                    {
                        UpdateStatus("تم تحديث الفاتورة بنجاح", "CheckCircle", "#28A745");
                    }
                    else
                    {
                        await ResetForNewInvoiceAsync();
                        UpdateStatus(transactionResult.Message, "CheckCircle", "#28A745");
                    }
                }
            }, "حفظ الفاتورة مع الدفعة");
        }

        [RelayCommand]
        private async Task PrintPreviousInvoiceAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                _logger.LogInformation("Executing PrintPreviousInvoice command");
                UpdateStatus("طباعة الفواتير السابقة قيد التطوير", "Info", "#FFC107");
                await Task.Delay(100);
            }, "طباعة فاتورة سابقة");
        }

        [RelayCommand]
        private async Task NewInvoiceAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                _logger.LogInformation("Executing NewInvoice command");
                await ResetForNewInvoiceAsync();
            }, "فاتورة جديدة");
        }

        [RelayCommand]
        private async Task AddNewCustomerAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                _logger.LogInformation("Executing AddNewCustomer command");

                var currentWindow = GetCurrentWindow();
                var createdCustomer = await AddCustomerDialog.ShowNewCustomerDialogAsync(_serviceProvider, currentWindow);

                if (createdCustomer != null)
                {
                    Customers.Insert(0, createdCustomer);
                    SelectedCustomer = createdCustomer;
                    UpdateStatus($"تم إضافة الزبون '{createdCustomer.CustomerName}' بنجاح", "CheckCircle", "#28A745");
                }
            }, "إضافة زبون جديد");
        }

        // NEW: Invoice Search Commands

        [RelayCommand]
        private async Task ToggleInvoiceSearchAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                IsInvoiceSearchVisible = !IsInvoiceSearchVisible;

                if (IsInvoiceSearchVisible)
                {
                    InvoiceSearchTerm = string.Empty;
                    InvoiceSearchResults.Clear();
                    SelectedInvoiceSearchResult = null;
                    UpdateStatus("وضع البحث عن الفواتير مفعل", "Search", "#3B82F6");
                }
                else
                {
                    UpdateStatus("تم إغلاق وضع البحث", "CheckCircle", "#28A745");
                }

                await Task.CompletedTask;
            }, "تبديل وضع البحث");
        }

        [RelayCommand(CanExecute = nameof(CanLoadSelectedInvoice))]
        private async Task LoadSelectedInvoiceAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                if (SelectedInvoiceSearchResult == null) return;

                _logger.LogInformation("Loading invoice {InvoiceNumber} for editing",
                    SelectedInvoiceSearchResult.InvoiceNumber);

                await LoadExistingInvoiceAsync(SelectedInvoiceSearchResult.InvoiceId);

                // Close search panel after successful load
                IsInvoiceSearchVisible = false;
                InvoiceSearchTerm = string.Empty;
                InvoiceSearchResults.Clear();
                SelectedInvoiceSearchResult = null;

                UpdateStatus($"تم تحميل الفاتورة رقم {CurrentInvoice.InvoiceNumber} للتعديل", "CheckCircle", "#28A745");
            }, "تحميل فاتورة للتعديل");
        }

        [RelayCommand]
        private async Task ClearInvoiceSearchAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                InvoiceSearchTerm = string.Empty;
                InvoiceSearchResults.Clear();
                SelectedInvoiceSearchResult = null;
                UpdateStatus("تم مسح نتائج البحث", "Info", "#6B7280");
                await Task.CompletedTask;
            }, "مسح البحث");
        }

        private bool CanExecuteSaveAndPrintInvoice() => CanSaveInvoice && !IsLoading;
        private bool CanExecuteSaveInvoice() => CanSaveInvoice && !IsLoading;
        private bool CanLoadSelectedInvoice() => SelectedInvoiceSearchResult != null && !IsLoading;

        #endregion

        #region Invoice Search Implementation

        /// <summary>
        /// Performs real-time invoice search with debouncing
        /// </summary>
        private async Task PerformInvoiceSearchAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(InvoiceSearchTerm) || InvoiceSearchTerm.Length < 3)
                {
                    InvoiceSearchResults.Clear();
                    SelectedInvoiceSearchResult = null;
                    return;
                }

                _logger.LogDebug("Performing invoice search for term: {SearchTerm}", InvoiceSearchTerm);

                // Add small delay for debouncing
                await Task.Delay(300);

                // Ensure search term hasn't changed during delay
                var currentTerm = InvoiceSearchTerm;
                if (string.IsNullOrWhiteSpace(currentTerm) || currentTerm.Length < 3)
                {
                    return;
                }

                var searchResults = await _unitOfWork.Invoices.SearchInvoicesAsync(
                    currentTerm,
                    DateTime.Today.AddMonths(-6), // Search last 6 months
                    DateTime.Today.AddDays(1)
                );

                // Convert to enhanced search result DTOs with selection support
                var results = searchResults.Select((invoice, index) => new InvoiceSearchResult
                {
                    InvoiceId = invoice.InvoiceId,
                    InvoiceNumber = invoice.InvoiceNumber,
                    InvoiceDate = invoice.InvoiceDate,
                    CustomerName = invoice.Customer?.CustomerName ?? "غير محدد",
                    TruckNumber = invoice.Truck?.TruckNumber ?? "غير محدد",
                    FinalAmount = invoice.FinalAmount,
                    NetWeight = invoice.NetWeight,
                    IsSelected = false // Initialize all as unselected
                }).OrderByDescending(r => r.InvoiceDate).Take(20);

                // Clear and populate results
                InvoiceSearchResults.Clear();
                SelectedInvoiceSearchResult = null;

                foreach (var result in results)
                {
                    InvoiceSearchResults.Add(result);
                }

                // Auto-select first result if only one found
                if (InvoiceSearchResults.Count == 1)
                {
                    SelectedInvoiceSearchResult = InvoiceSearchResults[0];
                }

                _logger.LogDebug("Found {Count} invoices matching search term", InvoiceSearchResults.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing invoice search");
                UpdateStatus("خطأ في البحث عن الفواتير", "ExclamationTriangle", "#DC3545");
            }
        }

        /// <summary>
        /// Loads an existing invoice for editing
        /// </summary>
        private async Task LoadExistingInvoiceAsync(int invoiceId)
        {
            try
            {
                _logger.LogInformation("Loading existing invoice {InvoiceId} for editing", invoiceId);

                var invoice = await _unitOfWork.Invoices.GetInvoiceWithDetailsAsync(invoiceId);
                if (invoice == null)
                {
                    throw new InvalidOperationException($"Invoice with ID {invoiceId} not found");
                }

                // Set edit mode
                IsEditMode = true;
                _editingInvoiceId = invoiceId;

                // Populate basic invoice data
                CurrentInvoice = new Invoice
                {
                    InvoiceId = invoice.InvoiceId,
                    InvoiceNumber = invoice.InvoiceNumber,
                    InvoiceDate = invoice.InvoiceDate,
                    CustomerId = invoice.CustomerId,
                    TruckId = invoice.TruckId,
                    GrossWeight = invoice.GrossWeight,
                    CagesWeight = invoice.CagesWeight,
                    CagesCount = invoice.CagesCount,
                    NetWeight = invoice.NetWeight,
                    UnitPrice = invoice.UnitPrice,
                    TotalAmount = invoice.TotalAmount,
                    DiscountPercentage = invoice.DiscountPercentage,
                    FinalAmount = invoice.FinalAmount,
                    PreviousBalance = invoice.PreviousBalance,
                    CurrentBalance = invoice.CurrentBalance,
                    CreatedDate = invoice.CreatedDate,
                    UpdatedDate = DateTime.Now
                };

                // Set customer and truck selections
                SelectedCustomer = Customers.FirstOrDefault(c => c.CustomerId == invoice.CustomerId);
                SelectedTruck = AvailableTrucks.FirstOrDefault(t => t.TruckId == invoice.TruckId);

                // Reconstruct invoice items from aggregate data
                await ReconstructInvoiceItemsAsync(invoice);

                // Update UI properties
                OnPropertyChanged(nameof(CurrentModeDisplay));
                OnPropertyChanged(nameof(SaveButtonText));
                NotifyValidationStateChanged();

                _logger.LogInformation("Successfully loaded invoice {InvoiceNumber} for editing", invoice.InvoiceNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading existing invoice {InvoiceId}", invoiceId);
                throw;
            }
        }

        /// <summary>
        /// Reconstructs invoice items from aggregated invoice data
        /// This creates a reasonable representation for editing
        /// </summary>
        private async Task ReconstructInvoiceItemsAsync(Invoice invoice)
        {
            try
            {
                // Clear existing items
                foreach (var item in InvoiceItems.ToList())
                {
                    item.PropertyChanged -= InvoiceItem_PropertyChanged;
                }
                InvoiceItems.Clear();

                // Create a single representative item from the aggregate data
                // In a real system, you might store individual items separately
                var reconstructedItem = new InvoiceItem
                {
                    InvoiceDate = invoice.InvoiceDate,
                    GrossWeight = invoice.GrossWeight,
                    CagesCount = invoice.CagesCount,
                    CageWeight = invoice.CagesCount > 0 ? invoice.CagesWeight / invoice.CagesCount : 0,
                    UnitPrice = invoice.UnitPrice,
                    DiscountPercentage = invoice.DiscountPercentage
                };

                // Calculate derived values
                reconstructedItem.RecalculateAllWithValidation();

                // Subscribe to changes
                reconstructedItem.PropertyChanged += InvoiceItem_PropertyChanged;

                // Add to collection
                InvoiceItems.Add(reconstructedItem);

                // Recalculate totals
                RecalculateTotals();

                _logger.LogDebug("Reconstructed {Count} invoice items from aggregate data", InvoiceItems.Count);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reconstructing invoice items");
                throw;
            }
        }

        #endregion

        #region Payment Calculation Methods

        /// <summary>
        /// Calculates remaining balance after payment application
        /// </summary>
        private void CalculateRemainingBalance()
        {
            try
            {
                RemainingBalance = Math.Max(0, AmountDue - PaymentReceived);

                _logger.LogDebug("Payment calculation - AmountDue: {AmountDue:C}, PaymentReceived: {PaymentReceived:C}, RemainingBalance: {RemainingBalance:C}",
                    AmountDue, PaymentReceived, RemainingBalance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating remaining balance");
                RemainingBalance = AmountDue;
            }
        }

        /// <summary>
        /// Resets payment fields for new transaction
        /// </summary>
        private void ResetPaymentFields()
        {
            PaymentReceived = 0;
            PaymentMethod = "CASH";
            PaymentNotes = string.Empty;
            RemainingBalance = 0;

            _logger.LogDebug("Payment fields reset for new transaction");
        }

        /// <summary>
        /// Sets payment to full amount due
        /// </summary>
        [RelayCommand]
        private void SetFullPayment()
        {
            try
            {
                PaymentReceived = AmountDue;
                UpdateStatus("تم تعيين المبلغ كاملاً", "CheckCircle", "#28A745");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting full payment amount");
            }
        }

        /// <summary>
        /// Sets payment to percentage of amount due
        /// </summary>
        [RelayCommand]
        private void SetPercentagePayment(object parameter)
        {
            try
            {
                if (parameter is string percentageStr && decimal.TryParse(percentageStr, out var percentage))
                {
                    PaymentReceived = Math.Round(AmountDue * percentage, 2);
                    UpdateStatus($"تم تعيين {percentage:P0} من المبلغ المستحق", "CheckCircle", "#28A745");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting percentage payment");
            }
        }

        #endregion

        #region Enhanced Transaction Processing

        /// <summary>
        /// Core transaction processing method with support for both new and update operations
        /// </summary>
        private async Task<TransactionResult?> ProcessTransactionWithPaymentAsync()
        {
            try
            {
                if (!ValidateCurrentInvoice())
                {
                    return null;
                }

                UpdateStatus(IsEditMode ? "جاري تحديث الفاتورة..." : "جاري معالجة المعاملة والدفعة...",
                    "Spinner", "#007BFF");

                // Populate invoice from items
                RecalculateTotals();
                PopulateInvoiceFromItems();

                // Handle edit vs create
                TransactionResult result;
                if (IsEditMode && _editingInvoiceId.HasValue)
                {
                    result = await UpdateExistingInvoiceAsync();
                }
                else
                {
                    result = await CreateNewInvoiceAsync();
                }

                if (result.Success)
                {
                    var statusMessage = IsEditMode
                        ? $"تم تحديث الفاتورة {result.Invoice?.InvoiceNumber} بنجاح"
                        : $"تم إنشاء الفاتورة {result.Invoice?.InvoiceNumber} بنجاح";

                    // Display result to user
                    MessageBox.Show(
                        $"{statusMessage}\n\n" +
                        $"المبلغ المستحق: {result.AmountDue:F2} USD\n" +
                        $"المبلغ المدفوع: {result.PaymentReceived:F2} USD\n" +
                        (result.RemainingBalance > 0
                            ? $"الرصيد المتبقي: {result.RemainingBalance:F2} USD"
                            : "تم تسديد المبلغ بالكامل"),
                        IsEditMode ? "تأكيد التحديث" : "تأكيد المعاملة",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    UpdateStatus($"خطأ في {(IsEditMode ? "تحديث" : "معالجة")} المعاملة: {result.Error}",
                        "ExclamationTriangle", "#DC3545");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing transaction with payment");
                UpdateStatus($"خطأ في {(IsEditMode ? "تحديث" : "معالجة")} المعاملة والدفعة",
                    "ExclamationTriangle", "#DC3545");
                throw;
            }
        }

        /// <summary>
        /// Creates a new invoice transaction
        /// </summary>
        private async Task<TransactionResult> CreateNewInvoiceAsync()
        {
            var transactionService = _serviceProvider.GetService<ITransactionProcessingService>();
            if (transactionService == null)
            {
                throw new InvalidOperationException("Transaction processing service not available");
            }

            var transactionRequest = new TransactionRequest
            {
                Invoice = CurrentInvoice,
                PaymentAmount = PaymentReceived,
                PaymentMethod = PaymentMethod,
                PaymentNotes = PaymentNotes
            };

            return await transactionService.ProcessTransactionWithPaymentAsync(transactionRequest);
        }

        /// <summary>
        /// Updates an existing invoice
        /// </summary>
        private async Task<TransactionResult> UpdateExistingInvoiceAsync()
        {
            try
            {
                // Update the existing invoice
                CurrentInvoice.UpdatedDate = DateTime.Now;

                // For updates, we need to handle the invoice update separately
                // This is simplified - in a production system you'd want more sophisticated update logic

                var updatedInvoice = await _unitOfWork.Invoices.UpdateAsync(CurrentInvoice);
                await _unitOfWork.SaveChangesAsync("INVOICE_UPDATE");

                // Create a mock transaction result for updates
                var result = new TransactionResult
                {
                    Success = true,
                    Invoice = updatedInvoice,
                    AmountDue = CurrentInvoice.FinalAmount,
                    PaymentReceived = PaymentReceived,
                    RemainingBalance = Math.Max(0, CurrentInvoice.FinalAmount - PaymentReceived),
                    Message = $"تم تحديث الفاتورة رقم {CurrentInvoice.InvoiceNumber} بنجاح"
                };

                _logger.LogInformation("Successfully updated invoice {InvoiceNumber}",
                    CurrentInvoice.InvoiceNumber);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating existing invoice");
                return new TransactionResult
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تحديث الفاتورة",
                    Error = ex.Message
                };
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles collection changes in invoice items for real-time updates
        /// </summary>
        private void InvoiceItems_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            try
            {
                // Subscribe to new items
                if (e.NewItems != null)
                {
                    foreach (InvoiceItem newItem in e.NewItems)
                    {
                        newItem.PropertyChanged += InvoiceItem_PropertyChanged;
                    }
                }

                // Unsubscribe from removed items
                if (e.OldItems != null)
                {
                    foreach (InvoiceItem oldItem in e.OldItems)
                    {
                        oldItem.PropertyChanged -= InvoiceItem_PropertyChanged;
                    }
                }

                RecalculateTotals();
                NotifyValidationStateChanged();

                _logger.LogDebug("Invoice items collection changed. Current count: {Count}", InvoiceItems?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling invoice items collection change");
            }
        }

        /// <summary>
        /// Handles property changes in individual invoice items for real-time calculations
        /// </summary>
        private void InvoiceItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            try
            {
                if (sender is InvoiceItem item)
                {
                    // Recalculate item totals when relevant properties change
                    var triggerCalculation = e.PropertyName switch
                    {
                        nameof(InvoiceItem.GrossWeight) or
                        nameof(InvoiceItem.CagesCount) or
                        nameof(InvoiceItem.CageWeight) or
                        nameof(InvoiceItem.UnitPrice) or
                        nameof(InvoiceItem.DiscountPercentage) => true,
                        _ => false
                    };

                    if (triggerCalculation)
                    {
                        CalculateInvoiceItem(item);
                        RecalculateTotals();
                        NotifyValidationStateChanged();
                    }

                    _logger.LogDebug("Invoice item property changed: {PropertyName} for item with gross weight {GrossWeight}",
                        e.PropertyName, item.GrossWeight);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling invoice item property change: {PropertyName}", e.PropertyName);
            }
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Enhanced initialization with proper invoice number generation
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                UpdateStatus("جاري تحميل البيانات...", "Spinner", "#007BFF");

                // Load reference data first
                await LoadCustomersAsync();
                await LoadTrucksAsync();

                // Generate real invoice number for new invoices only
                if (!IsEditMode)
                {
                    await GenerateAndSetRealInvoiceNumberAsync();
                }

                UpdateStatus("جاهز لإنشاء فاتورة جديدة", "CheckCircle", "#28A745");
                _logger.LogInformation("POSViewModel initialized successfully with search and edit capabilities");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during POSViewModel initialization");
                await _errorHandlingService.HandleExceptionAsync(ex, "POSViewModel.InitializeAsync");
                UpdateStatus("خطأ في تحميل البيانات", "ExclamationTriangle", "#DC3545");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Generates real invoice number and updates CurrentInvoice property
        /// </summary>
        private async Task GenerateAndSetRealInvoiceNumberAsync()
        {
            try
            {
                var realInvoiceNumber = await _unitOfWork.Invoices.GenerateInvoiceNumberAsync();
                CurrentInvoice.InvoiceNumber = realInvoiceNumber;
                OnPropertyChanged(nameof(CurrentInvoice));

                _logger.LogInformation("Real invoice number generated and set: {InvoiceNumber}", realInvoiceNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating real invoice number");

                // Fallback to enhanced temporary number
                var fallbackNumber = $"INV-{DateTime.Now:yyyyMMddHHmmss}";
                CurrentInvoice.InvoiceNumber = fallbackNumber;
                OnPropertyChanged(nameof(CurrentInvoice));
            }
        }

        /// <summary>
        /// Populates CurrentInvoice with aggregated data from InvoiceItems for repository processing
        /// </summary>
        private void PopulateInvoiceFromItems()
        {
            try
            {
                if (InvoiceItems == null || InvoiceItems.Count == 0)
                {
                    _logger.LogWarning("No invoice items found for invoice population");
                    return;
                }

                // Calculate aggregated values for single Invoice record
                var totalGrossWeight = InvoiceItems.Sum(item => item.GrossWeight);
                var totalCagesWeight = InvoiceItems.Sum(item => item.CagesWeight);
                var totalCagesCount = InvoiceItems.Sum(item => item.CagesCount);
                var weightedAveragePrice = CalculateWeightedAverageUnitPrice();
                var averageDiscountPercentage = CalculateAverageDiscountPercentage();

                // Populate CurrentInvoice with aggregated data
                CurrentInvoice.GrossWeight = totalGrossWeight;
                CurrentInvoice.CagesWeight = totalCagesWeight;
                CurrentInvoice.CagesCount = totalCagesCount;
                CurrentInvoice.UnitPrice = weightedAveragePrice;
                CurrentInvoice.DiscountPercentage = averageDiscountPercentage;

                // These will be recalculated by repository, but set them anyway
                CurrentInvoice.NetWeight = TotalNetWeight;
                CurrentInvoice.TotalAmount = TotalAmount;
                CurrentInvoice.FinalAmount = FinalTotal;

                _logger.LogDebug("Invoice populated with aggregated data - GrossWeight: {GrossWeight}, UnitPrice: {UnitPrice}",
                    totalGrossWeight, weightedAveragePrice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating invoice from items");
            }
        }

        /// <summary>
        /// Calculates weighted average unit price across all invoice items
        /// </summary>
        private decimal CalculateWeightedAverageUnitPrice()
        {
            var totalWeight = InvoiceItems.Where(item => item.NetWeight > 0).Sum(item => item.NetWeight);
            if (totalWeight == 0) return 0;

            return InvoiceItems
                .Where(item => item.NetWeight > 0)
                .Sum(item => item.UnitPrice * item.NetWeight) / totalWeight;
        }

        /// <summary>
        /// Calculates average discount percentage weighted by amount
        /// </summary>
        private decimal CalculateAverageDiscountPercentage()
        {
            var totalAmount = InvoiceItems.Where(item => item.TotalAmount > 0).Sum(item => item.TotalAmount);
            if (totalAmount == 0) return 0;

            return InvoiceItems
                .Where(item => item.TotalAmount > 0)
                .Sum(item => item.DiscountPercentage * item.TotalAmount) / totalAmount;
        }

        /// <summary>
        /// Validates the current bulk invoice and returns validation result
        /// </summary>
        public bool ValidateCurrentInvoice(bool showErrors = true)
        {
            try
            {
                ValidationErrors.Clear();
                var validationResults = new List<string>();

                _logger.LogDebug("Starting invoice validation - Customer: {Customer}, Truck: {Truck}, Items: {ItemCount}",
                    SelectedCustomer?.CustomerName ?? "None",
                    SelectedTruck?.TruckNumber ?? "None",
                    InvoiceItems?.Count ?? 0);

                // Customer validation
                if (SelectedCustomer == null)
                {
                    validationResults.Add("يجب اختيار زبون للفاتورة");
                    _logger.LogDebug("Validation failed: No customer selected");
                }

                // Truck validation  
                if (SelectedTruck == null)
                {
                    validationResults.Add("يجب اختيار شاحنة للفاتورة");
                    _logger.LogDebug("Validation failed: No truck selected");
                }

                // Invoice items validation
                if (InvoiceItems == null || InvoiceItems.Count == 0)
                {
                    validationResults.Add("يجب إضافة بند واحد على الأقل للفاتورة");
                    _logger.LogDebug("Validation failed: No invoice items");
                }
                else
                {
                    var hasValidItems = InvoiceItems.Any(item =>
                        item.GrossWeight > 0 &&
                        item.CagesCount > 0 &&
                        item.UnitPrice > 0);

                    _logger.LogDebug("Invoice items validation - Valid items found: {HasValid}", hasValidItems);

                    if (!hasValidItems)
                    {
                        validationResults.Add("يجب إدخال بيانات صحيحة في بنود الفاتورة");
                        _logger.LogDebug("Validation failed: No valid items with required data");

                        // Log individual item states for debugging
                        for (int i = 0; i < InvoiceItems.Count; i++)
                        {
                            var item = InvoiceItems[i];
                            _logger.LogDebug("Item {Index}: GrossWeight={GrossWeight}, CagesCount={CagesCount}, UnitPrice={UnitPrice}",
                                i + 1, item.GrossWeight, item.CagesCount, item.UnitPrice);
                        }
                    }

                    // Validate individual items
                    for (int i = 0; i < InvoiceItems.Count; i++)
                    {
                        var item = InvoiceItems[i];
                        var itemNumber = i + 1;

                        if (item.CagesWeight >= item.GrossWeight && item.GrossWeight > 0)
                        {
                            validationResults.Add($"البند {itemNumber}: وزن الأقفاص لا يمكن أن يكون أكبر من أو يساوي الوزن الفلتي");
                            _logger.LogDebug("Validation failed: Item {ItemNumber} - CagesWeight ({CagesWeight}) >= GrossWeight ({GrossWeight})",
                                itemNumber, item.CagesWeight, item.GrossWeight);
                        }

                        if (item.DiscountPercentage < 0 || item.DiscountPercentage > 100)
                        {
                            validationResults.Add($"البند {itemNumber}: نسبة الخصم يجب أن تكون بين 0 و 100");
                            _logger.LogDebug("Validation failed: Item {ItemNumber} - Invalid discount percentage: {DiscountPercentage}",
                                itemNumber, item.DiscountPercentage);
                        }
                    }
                }

                // Update validation state
                foreach (var error in validationResults)
                {
                    ValidationErrors.Add(error);
                }

                HasValidationErrors = ValidationErrors.Count > 0;
                var isValid = !HasValidationErrors;

                if (HasValidationErrors && showErrors)
                {
                    UpdateStatus($"توجد {ValidationErrors.Count} أخطاء في البيانات", "ExclamationTriangle", "#DC3545");
                    _logger.LogWarning("Validation failed with {ErrorCount} errors: {Errors}",
                        ValidationErrors.Count, string.Join("; ", ValidationErrors));
                }
                else if (isValid)
                {
                    UpdateStatus("البيانات صحيحة وجاهزة للحفظ", "CheckCircle", "#28A745");
                    _logger.LogDebug("Validation passed successfully");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk invoice validation");
                return false;
            }
        }

        /// <summary>
        /// Cleanup method for resource disposal
        /// </summary>
        public void Cleanup()
        {
            try
            {
                _logger.LogDebug("POSViewModel cleanup initiated");

                // Clear collections and unsubscribe from events
                if (InvoiceItems != null)
                {
                    foreach (var item in InvoiceItems)
                    {
                        item.PropertyChanged -= InvoiceItem_PropertyChanged;
                    }
                    InvoiceItems.CollectionChanged -= InvoiceItems_CollectionChanged;
                    InvoiceItems.Clear();
                }

                Customers.Clear();
                AvailableTrucks.Clear();
                ValidationErrors.Clear();
                InvoiceSearchResults.Clear();

                _logger.LogInformation("POSViewModel cleanup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during POSViewModel cleanup");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes command bindings for UI interaction
        /// </summary>
        private void InitializeCommands()
        {
            _logger.LogDebug("POSViewModel commands initialized");
        }

        /// <summary>
        /// Initializes invoice with temporary number for immediate UI display
        /// </summary>
        private void InitializeCurrentInvoiceWithTempNumber()
        {
            // Generate temporary invoice number for immediate display
            var tempInvoiceNumber = GenerateTemporaryInvoiceNumber();

            CurrentInvoice = new Invoice
            {
                InvoiceNumber = tempInvoiceNumber,
                InvoiceDate = DateTime.Now,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            _logger.LogDebug("Invoice initialized with temporary number: {TempNumber}", tempInvoiceNumber);
        }

        /// <summary>
        /// Generates temporary invoice number for immediate UI feedback
        /// </summary>
        private string GenerateTemporaryInvoiceNumber()
        {
            var datePrefix = DateTime.Today.ToString("yyyyMMdd");
            var timeComponent = DateTime.Now.ToString("HHmm");
            return $"{datePrefix}-TEMP-{timeComponent}";
        }

        /// <summary>
        /// Initializes the invoice items collection with one default item
        /// </summary>
        private void InitializeInvoiceItems()
        {
            InvoiceItems = new ObservableCollection<InvoiceItem>();

            // Add initial item
            var initialItem = new InvoiceItem
            {
                InvoiceDate = DateTime.Today,
                GrossWeight = 0,
                CagesCount = 0,
                CageWeight = 0,
                UnitPrice = 0,
                DiscountPercentage = 0
            };

            initialItem.PropertyChanged += InvoiceItem_PropertyChanged;
            InvoiceItems.Add(initialItem);

            _logger.LogDebug("Invoice items collection initialized with initial item");
        }

        /// <summary>
        /// Calculates totals for a specific invoice item
        /// </summary>
        private void CalculateInvoiceItem(InvoiceItem item)
        {
            try
            {
                // Calculate cage-related weights
                item.CagesWeight = item.CagesCount * item.CageWeight;
                item.NetWeight = Math.Max(0, item.GrossWeight - item.CagesWeight);

                // Calculate financial amounts
                item.TotalAmount = item.NetWeight * item.UnitPrice;
                item.DiscountAmount = item.TotalAmount * (item.DiscountPercentage / 100);
                item.FinalAmount = item.TotalAmount - item.DiscountAmount;

                _logger.LogDebug("Invoice item calculated - Net Weight: {NetWeight}, Final Amount: {FinalAmount}",
                    item.NetWeight, item.FinalAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating invoice item");
            }
        }

        /// <summary>
        /// Recalculates totals with payment processing
        /// </summary>
        private void RecalculateTotals()
        {
            try
            {
                if (InvoiceItems == null || InvoiceItems.Count == 0)
                {
                    TotalNetWeight = 0;
                    TotalAmount = 0;
                    TotalDiscount = 0;
                    FinalTotal = 0;
                    CalculateRemainingBalance();
                    return;
                }

                TotalNetWeight = InvoiceItems.Sum(item => item.NetWeight);
                TotalAmount = InvoiceItems.Sum(item => item.TotalAmount);
                TotalDiscount = InvoiceItems.Sum(item => item.DiscountAmount);
                FinalTotal = InvoiceItems.Sum(item => item.FinalAmount);

                // Update current invoice totals
                CurrentInvoice.NetWeight = TotalNetWeight;
                CurrentInvoice.TotalAmount = TotalAmount;
                CurrentInvoice.FinalAmount = FinalTotal;

                // Update balance calculations
                if (SelectedCustomer != null)
                {
                    CurrentInvoice.PreviousBalance = SelectedCustomer.TotalDebt;
                    CurrentInvoice.CurrentBalance = SelectedCustomer.TotalDebt + FinalTotal;
                }

                // Calculate payment-related fields
                CalculateRemainingBalance();
                OnPropertyChanged(nameof(AmountDue));
                OnPropertyChanged(nameof(AmountDueDisplay));

                _logger.LogDebug("Totals recalculated with payment processing - Net Weight: {NetWeight}, Final Total: {FinalTotal}, Amount Due: {AmountDue}",
                    TotalNetWeight, FinalTotal, AmountDue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating totals with payment processing");
            }
        }

        /// <summary>
        /// Loads active customers from database
        /// </summary>
        private async Task LoadCustomersAsync()
        {
            try
            {
                var customers = await _unitOfWork.Customers.GetActiveCustomersAsync();

                Customers.Clear();
                foreach (var customer in customers.OrderBy(c => c.CustomerName))
                {
                    Customers.Add(customer);
                }

                _logger.LogInformation("Loaded {CustomerCount} active customers", Customers.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading customers");
                throw;
            }
        }

        /// <summary>
        /// Loads active trucks from database
        /// </summary>
        private async Task LoadTrucksAsync()
        {
            try
            {
                var trucks = await _unitOfWork.Trucks.GetActiveTrucksAsync();

                AvailableTrucks.Clear();
                foreach (var truck in trucks.OrderBy(t => t.TruckNumber))
                {
                    AvailableTrucks.Add(truck);
                }

                _logger.LogInformation("Loaded {TruckCount} active trucks", AvailableTrucks.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading trucks");
                throw;
            }
        }

        /// <summary>
        /// Generates a new unique invoice number
        /// </summary>
        private async Task GenerateNewInvoiceNumberAsync()
        {
            try
            {
                var invoiceNumber = await _unitOfWork.Invoices.GenerateInvoiceNumberAsync();
                CurrentInvoice.InvoiceNumber = invoiceNumber;

                _logger.LogDebug("Generated new invoice number: {InvoiceNumber}", invoiceNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice number");
                throw;
            }
        }

        /// <summary>
        /// Handles customer selection change events
        /// </summary>
        private void OnCustomerSelectionChanged()
        {
            try
            {
                if (SelectedCustomer != null)
                {
                    CurrentInvoice.CustomerId = SelectedCustomer.CustomerId;
                    CurrentInvoice.PreviousBalance = SelectedCustomer.TotalDebt;
                    RecalculateTotals();
                    NotifyValidationStateChanged();

                    _logger.LogDebug("Customer selected: {CustomerName}", SelectedCustomer.CustomerName);
                }
                else
                {
                    NotifyValidationStateChanged();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling customer selection change");
            }
        }

        /// <summary>
        /// Handles truck selection change events
        /// </summary>
        private void OnTruckSelectionChanged()
        {
            try
            {
                if (SelectedTruck != null)
                {
                    CurrentInvoice.TruckId = SelectedTruck.TruckId;
                    NotifyValidationStateChanged();

                    _logger.LogDebug("Truck selected: {TruckNumber}", SelectedTruck.TruckNumber);
                }
                else
                {
                    NotifyValidationStateChanged();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling truck selection change");
            }
        }

        /// <summary>
        /// Centralized method to notify validation state changes and update command execution
        /// </summary>
        private void NotifyValidationStateChanged()
        {
            try
            {
                OnPropertyChanged(nameof(CanSaveInvoice));
                SaveInvoiceCommand.NotifyCanExecuteChanged();
                SaveAndPrintInvoiceCommand.NotifyCanExecuteChanged();
                LoadSelectedInvoiceCommand.NotifyCanExecuteChanged();

                var isValid = ValidateCurrentInvoice(false);
                if (isValid)
                {
                    UpdateStatus("البيانات صحيحة وجاهزة للحفظ", "CheckCircle", "#28A745");
                }

                _logger.LogDebug("Validation state notified. CanSaveInvoice: {CanSave}", isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying validation state changes");
            }
        }

        /// <summary>
        /// Enhanced reset method with payment fields and edit mode handling
        /// </summary>
        private async Task ResetForNewInvoiceAsync()
        {
            try
            {
                // Reset edit mode
                IsEditMode = false;
                _editingInvoiceId = null;

                // Reset selections
                SelectedCustomer = null;
                SelectedTruck = null;

                // Reset invoice and items
                InitializeCurrentInvoiceWithTempNumber();
                InitializeInvoiceItems();
                ResetPaymentFields();

                // Generate new invoice number
                await GenerateNewInvoiceNumberAsync();
                CurrentDateTime = DateTime.Now;

                // Update UI
                OnPropertyChanged(nameof(CurrentModeDisplay));
                OnPropertyChanged(nameof(SaveButtonText));
                NotifyValidationStateChanged();

                UpdateStatus("جاهز لإنشاء فاتورة جديدة", "CheckCircle", "#28A745");
                _logger.LogDebug("Form reset for new invoice with edit mode handling");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting form for new invoice");
                throw;
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
        }

        /// <summary>
        /// Enhanced receipt printing with support for edit mode
        /// </summary>
        private async Task PrintBulkInvoiceAsync(Invoice invoice)
        {
            try
            {
                _logger.LogInformation("Starting enhanced bulk invoice printing for Invoice: {InvoiceNumber} (Edit Mode: {IsEditMode})",
                    invoice.InvoiceNumber, IsEditMode);

                var doc = new FlowDocument();
                doc.PagePadding = new Thickness(20, 15, 20, 15);
                doc.ColumnGap = 0;
                doc.ColumnWidth = double.PositiveInfinity;
                doc.FontFamily = new System.Windows.Media.FontFamily("Arial Unicode MS, Tahoma, Arial");
                doc.FontSize = 10;
                doc.FlowDirection = FlowDirection.RightToLeft;

                // Create receipt content (simplified for brevity)
                var headerPara = new Paragraph(new Run($"فاتورة رقم: {invoice.InvoiceNumber}"))
                {
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20)
                };
                doc.Blocks.Add(headerPara);

                if (IsEditMode)
                {
                    var editNotePara = new Paragraph(new Run("*** فاتورة محدثة ***"))
                    {
                        FontSize = 12,
                        FontWeight = FontWeights.Bold,
                        Foreground = System.Windows.Media.Brushes.Red,
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                    doc.Blocks.Add(editNotePara);
                }

                // Print the document
                await PrintDocumentAsync(doc, $"فاتورة رقم {invoice.InvoiceNumber}");

                _logger.LogInformation("Enhanced invoice printing completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing enhanced invoice: {InvoiceNumber}", invoice.InvoiceNumber);
                UpdateStatus("خطأ في طباعة الفاتورة", "ExclamationTriangle", "#DC3545");
                throw;
            }
        }

        /// <summary>
        /// Handles the actual document printing
        /// </summary>
        private async Task PrintDocumentAsync(FlowDocument document, string documentTitle)
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var printDialog = new PrintDialog();

                    if (printDialog.ShowDialog() == true)
                    {
                        document.PageHeight = printDialog.PrintableAreaHeight;
                        document.PageWidth = printDialog.PrintableAreaWidth;

                        IDocumentPaginatorSource idpSource = document;
                        printDialog.PrintDocument(idpSource.DocumentPaginator, documentTitle);

                        UpdateStatus("تم طباعة الفاتورة بنجاح", "CheckCircle", "#28A745");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during document printing");
                throw;
            }
        }

        #endregion
    }

    #region Invoice Search DTOs

    /// <summary>
    /// Enhanced data transfer object for invoice search results with selection support
    /// </summary>
    public partial class InvoiceSearchResult : ObservableObject
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string TruckNumber { get; set; } = string.Empty;
        public decimal FinalAmount { get; set; }
        public decimal NetWeight { get; set; }

        [ObservableProperty]
        private bool _isSelected = false;

        /// <summary>
        /// Display text for search results
        /// </summary>
        public string DisplayText => $"{InvoiceNumber} - {CustomerName} - {InvoiceDate:yyyy/MM/dd} - {FinalAmount:F2} USD";

        /// <summary>
        /// Secondary display information
        /// </summary>
        public string SecondaryText => $"الشاحنة: {TruckNumber} | الوزن: {NetWeight:F2} كغم";

        /// <summary>
        /// Formatted invoice date for display
        /// </summary>
        public string FormattedDate => InvoiceDate.ToString("yyyy/MM/dd");

        /// <summary>
        /// Formatted amount for display
        /// </summary>
        public string FormattedAmount => $"{FinalAmount:F2} USD";

        /// <summary>
        /// Status indicator for UI styling
        /// </summary>
        public string StatusIndicator => IsSelected ? "Selected" : "Available";
    }

    #endregion
}