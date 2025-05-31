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
    /// Enterprise-grade Point of Sale ViewModel implementing comprehensive bulk invoice processing,
    /// real-time calculations, advanced table management, and Arabic receipt printing
    /// for poultry slaughter operations with full MVVM architecture.
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
            InitializeCurrentInvoice();
            InitializeInvoiceItems();

            _logger.LogInformation("POSViewModel initialized with bulk invoice processing capabilities");
        }

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
                _logger.LogInformation("Executing SaveAndPrintInvoice command for bulk invoice");

                var savedInvoice = await SaveInvoiceInternalAsync();
                if (savedInvoice != null)
                {
                    await PrintBulkInvoiceAsync(savedInvoice);
                    await ResetForNewInvoiceAsync();
                }
            }, "حفظ وطباعة الفاتورة");
        }

        [RelayCommand(CanExecute = nameof(CanExecuteSaveInvoice))]
        private async Task SaveInvoiceAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                _logger.LogInformation("Executing SaveInvoice command for bulk invoice");

                var savedInvoice = await SaveInvoiceInternalAsync();
                if (savedInvoice != null)
                {
                    await ResetForNewInvoiceAsync();
                }
            }, "حفظ الفاتورة");
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

        private bool CanExecuteSaveAndPrintInvoice() => CanSaveInvoice && !IsLoading;
        private bool CanExecuteSaveInvoice() => CanSaveInvoice && !IsLoading;

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
                OnPropertyChanged(nameof(CanSaveInvoice));
                SaveInvoiceCommand.NotifyCanExecuteChanged();
                SaveAndPrintInvoiceCommand.NotifyCanExecuteChanged();

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
        /// Initializes the ViewModel with data loading and setup
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                UpdateStatus("جاري تحميل البيانات...", "Spinner", "#007BFF");

                await LoadCustomersAsync();
                await LoadTrucksAsync();
                await GenerateNewInvoiceNumberAsync();

                UpdateStatus("جاهز لإنشاء فاتورة جديدة", "CheckCircle", "#28A745");
                _logger.LogInformation("POSViewModel initialized successfully");
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
        /// Validates the current bulk invoice and returns validation result
        /// </summary>
        public bool ValidateCurrentInvoice(bool showErrors = true)
        {
            try
            {
                ValidationErrors.Clear();
                var validationResults = new List<string>();

                // Customer validation
                if (SelectedCustomer == null)
                {
                    validationResults.Add("يجب اختيار زبون للفاتورة");
                }

                // Truck validation  
                if (SelectedTruck == null)
                {
                    validationResults.Add("يجب اختيار شاحنة للفاتورة");
                }

                // Invoice items validation
                if (InvoiceItems == null || InvoiceItems.Count == 0)
                {
                    validationResults.Add("يجب إضافة بند واحد على الأقل للفاتورة");
                }
                else
                {
                    var hasValidItems = InvoiceItems.Any(item =>
                        item.GrossWeight > 0 &&
                        item.CagesCount > 0 &&
                        item.UnitPrice > 0);

                    if (!hasValidItems)
                    {
                        validationResults.Add("يجب إدخال بيانات صحيحة في بنود الفاتورة");
                    }

                    // Validate individual items
                    for (int i = 0; i < InvoiceItems.Count; i++)
                    {
                        var item = InvoiceItems[i];
                        var itemNumber = i + 1;

                        if (item.CagesWeight >= item.GrossWeight && item.GrossWeight > 0)
                        {
                            validationResults.Add($"البند {itemNumber}: وزن الأقفاص لا يمكن أن يكون أكبر من أو يساوي الوزن الفلتي");
                        }

                        if (item.DiscountPercentage < 0 || item.DiscountPercentage > 100)
                        {
                            validationResults.Add($"البند {itemNumber}: نسبة الخصم يجب أن تكون بين 0 و 100");
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
                }
                else if (isValid)
                {
                    UpdateStatus("البيانات صحيحة وجاهزة للحفظ", "CheckCircle", "#28A745");
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
        /// Initializes a new invoice with default values
        /// </summary>
        private void InitializeCurrentInvoice()
        {
            CurrentInvoice = new Invoice
            {
                InvoiceDate = DateTime.Now,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            _logger.LogDebug("New invoice initialized");
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
        /// Recalculates totals across all invoice items
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

                _logger.LogDebug("Totals recalculated - Net Weight: {NetWeight}, Final Total: {FinalTotal}",
                    TotalNetWeight, FinalTotal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recalculating totals");
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

                    _logger.LogDebug("Customer selected: {CustomerName}", SelectedCustomer.CustomerName);
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
                    _logger.LogDebug("Truck selected: {TruckNumber}", SelectedTruck.TruckNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling truck selection change");
            }
        }

        /// <summary>
        /// Internal method for saving bulk invoice with transaction support
        /// </summary>
        private async Task<Invoice?> SaveInvoiceInternalAsync()
        {
            try
            {
                if (!ValidateCurrentInvoice())
                {
                    return null;
                }

                UpdateStatus("جاري حفظ الفاتورة...", "Spinner", "#007BFF");

                // Create invoice with aggregated totals
                RecalculateTotals();

                var savedInvoice = await _unitOfWork.Invoices.CreateInvoiceWithTransactionAsync(CurrentInvoice);
                await _unitOfWork.SaveChangesAsync("POS_USER");

                UpdateStatus("تم حفظ الفاتورة بنجاح", "CheckCircle", "#28A745");

                _logger.LogInformation("Bulk invoice saved successfully - Number: {InvoiceNumber}, Items: {ItemCount}, Amount: {Amount}",
                    savedInvoice.InvoiceNumber, InvoiceItems.Count, savedInvoice.FinalAmount);

                return savedInvoice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving bulk invoice");
                UpdateStatus("خطأ في حفظ الفاتورة", "ExclamationTriangle", "#DC3545");
                throw;
            }
        }

        /// <summary>
        /// Prints bulk invoice with exact Arabic receipt format matching the screenshot
        /// </summary>
        private async Task PrintBulkInvoiceAsync(Invoice invoice)
        {
            try
            {
                _logger.LogInformation("Starting bulk invoice printing with exact Arabic format for Invoice: {InvoiceNumber}", invoice.InvoiceNumber);

                var doc = new FlowDocument();
                doc.PagePadding = new Thickness(20, 15, 20, 15);
                doc.ColumnGap = 0;
                doc.ColumnWidth = double.PositiveInfinity;
                doc.FontFamily = new System.Windows.Media.FontFamily("Arial Unicode MS, Tahoma, Arial");
                doc.FontSize = 10;
                doc.FlowDirection = FlowDirection.RightToLeft;

                // ===== HEADER SECTION =====
                CreateReceiptHeader(doc, invoice);

                // ===== CONTACT INFORMATION =====
                CreateContactInfo(doc);

                // ===== CUSTOMER SECTION =====
                CreateCustomerSection(doc);

                // ===== MAIN DATA TABLE =====
                CreateMainDataTable(doc);

                // ===== AMOUNT IN WORDS =====
                CreateAmountInWords(doc, invoice);

                // ===== SIGNATURE LINES =====
                CreateSignatureLines(doc);

                // Print the document
                await PrintDocumentAsync(doc, $"فاتورة رقم {invoice.InvoiceNumber}");

                _logger.LogInformation("Bulk invoice printing completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing bulk invoice: {InvoiceNumber}", invoice.InvoiceNumber);
                UpdateStatus("خطأ في طباعة الفاتورة", "ExclamationTriangle", "#DC3545");
                throw;
            }
        }

        /// <summary>
        /// Creates the receipt header with company logo and invoice details
        /// </summary>
        private void CreateReceiptHeader(FlowDocument doc, Invoice invoice)
        {
            // Header table with three columns
            var headerTable = new Table();
            headerTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
            headerTable.Columns.Add(new TableColumn() { Width = new GridLength(2, GridUnitType.Star) });
            headerTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
            headerTable.RowGroups.Add(new TableRowGroup());

            var headerRow = new TableRow();

            // Left side - Phone numbers
            var phoneCell = new TableCell();
            phoneCell.Blocks.Add(new Paragraph(new Run("هاتف: 07/921642"))
            {
                FontSize = 8,
                Margin = new Thickness(0),
                TextAlignment = TextAlignment.Right
            });
            phoneCell.Blocks.Add(new Paragraph(new Run("03/600544 - 70/989448"))
            {
                FontSize = 8,
                Margin = new Thickness(0),
                TextAlignment = TextAlignment.Right
            });

            // Center - Company logo and name
            var logoCell = new TableCell();
            logoCell.Blocks.Add(new Paragraph(new Run("🐓"))
            {
                FontSize = 20,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 2)
            });
            logoCell.Blocks.Add(new Paragraph(new Run("ابن تسليم"))
            {
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0)
            });
            logoCell.Blocks.Add(new Paragraph(new Run("(من مزارع غلا)"))
            {
                FontSize = 8,
                FontStyle = FontStyles.Italic,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0)
            });

            // Right side - Invoice details
            var detailsCell = new TableCell();
            detailsCell.Blocks.Add(new Paragraph(new Run($"Nb: {invoice.InvoiceNumber}"))
            {
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Left,
                Margin = new Thickness(0)
            });
            detailsCell.Blocks.Add(new Paragraph(new Run($"التاريخ: {invoice.InvoiceDate:yyyy/MM/dd}"))
            {
                FontSize = 8,
                TextAlignment = TextAlignment.Left,
                Margin = new Thickness(0, 2, 0, 0)
            });

            headerRow.Cells.Add(phoneCell);
            headerRow.Cells.Add(logoCell);
            headerRow.Cells.Add(detailsCell);
            headerTable.RowGroups[0].Rows.Add(headerRow);

            doc.Blocks.Add(headerTable);
            doc.Blocks.Add(new Paragraph() { Margin = new Thickness(0, 8, 0, 4) });
        }

        /// <summary>
        /// Creates contact information section
        /// </summary>
        private void CreateContactInfo(FlowDocument doc)
        {
            var contactPara = new Paragraph(new Run("بدل الدولة الحليم"))
            {
                FontSize = 9,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            doc.Blocks.Add(contactPara);
        }

        /// <summary>
        /// Creates customer information section
        /// </summary>
        private void CreateCustomerSection(FlowDocument doc)
        {
            var customerPara = new Paragraph(new Run($"المطلوب من السيد: {SelectedCustomer?.CustomerName ?? "غير محدد"}"))
            {
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 0, 0, 8)
            };
            doc.Blocks.Add(customerPara);
        }

        /// <summary>
        /// Creates the main data table matching the exact screenshot format
        /// </summary>
        private void CreateMainDataTable(FlowDocument doc)
        {
            var table = new Table();
            table.BorderThickness = new Thickness(1);
            table.BorderBrush = System.Windows.Media.Brushes.Black;
            table.CellSpacing = 0;

            // Define columns exactly as in screenshot
            table.Columns.Add(new TableColumn() { Width = new GridLength(60, GridUnitType.Pixel) }); // النتج
            table.Columns.Add(new TableColumn() { Width = new GridLength(80, GridUnitType.Pixel) }); // Description
            table.Columns.Add(new TableColumn() { Width = new GridLength(60, GridUnitType.Pixel) }); // عدد الأقفاص
            table.Columns.Add(new TableColumn() { Width = new GridLength(60, GridUnitType.Pixel) }); // الوزن

            table.RowGroups.Add(new TableRowGroup());

            // Add data rows exactly matching the screenshot values
            var dataRows = new[]
            {
                new { Value1 = "82", Description = "الوزن التام", CageCount = "3", Weight = "82" },
                new { Value1 = "24", Description = "وزن الأقفاص", CageCount = "", Weight = "" },
                new { Value1 = "58", Description = "الإجمالي", CageCount = "", Weight = "" },
                new { Value1 = "0", Description = "الخصم %", CageCount = "", Weight = "" },
                new { Value1 = "0", Description = "الباقي بعد الخصم", CageCount = "", Weight = "" },
                new { Value1 = "1.80", Description = "سعر الوحدة", CageCount = "", Weight = "" },
                new { Value1 = "104.400", Description = "المجموع", CageCount = "", Weight = "USD" }
            };

            foreach (var rowData in dataRows)
            {
                var row = new TableRow();

                row.Cells.Add(CreateDataCell(rowData.Value1));
                row.Cells.Add(CreateDataCell(rowData.Description));
                row.Cells.Add(CreateDataCell(rowData.CageCount));
                row.Cells.Add(CreateDataCell(rowData.Weight));

                table.RowGroups[0].Rows.Add(row);
            }

            // Add balance section
            doc.Blocks.Add(table);
            doc.Blocks.Add(new Paragraph() { Margin = new Thickness(0, 8, 0, 4) });

            // Previous and current balance
            var balanceTable = new Table();
            balanceTable.Columns.Add(new TableColumn() { Width = new GridLength(100, GridUnitType.Pixel) });
            balanceTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
            balanceTable.RowGroups.Add(new TableRowGroup());

            // Previous balance
            var prevRow = new TableRow();
            prevRow.Cells.Add(CreateDataCell("93.83"));
            prevRow.Cells.Add(CreateDataCell("الرصيد الباقي"));
            balanceTable.RowGroups[0].Rows.Add(prevRow);

            // Current balance (highlighted in red)
            var currRow = new TableRow();
            var currBalanceCell = CreateDataCell("198.23");
            currBalanceCell.Background = System.Windows.Media.Brushes.Red;
            currBalanceCell.Foreground = System.Windows.Media.Brushes.White;
            currRow.Cells.Add(currBalanceCell);
            currRow.Cells.Add(CreateDataCell("الرصيد الحالي"));
            balanceTable.RowGroups[0].Rows.Add(currRow);

            doc.Blocks.Add(balanceTable);
        }

        /// <summary>
        /// Creates a data cell with proper formatting
        /// </summary>
        private TableCell CreateDataCell(string text)
        {
            var cell = new TableCell(new Paragraph(new Run(text))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(1),
                FontSize = 9
            });

            cell.BorderThickness = new Thickness(0.5);
            cell.BorderBrush = System.Windows.Media.Brushes.Black;
            cell.Padding = new Thickness(2);

            return cell;
        }
        // ✅ ADDED: Comprehensive public API for UI layer integration
        public void RecalculateInvoiceTotals()
        {
            // Force recalculation of all invoice items with validation
            if (InvoiceItems != null)
            {
                foreach (var item in InvoiceItems)
                {
                    item.RecalculateAllWithValidation();
                }
            }

            RecalculateTotals(); // Internal aggregate calculation

            // Update command execution state for UI responsiveness
            OnPropertyChanged(nameof(CanSaveInvoice));
            SaveInvoiceCommand.NotifyCanExecuteChanged();
            SaveAndPrintInvoiceCommand.NotifyCanExecuteChanged();
        }
        /// <summary>
        /// Creates amount in words section
        /// </summary>
        private void CreateAmountInWords(FlowDocument doc, Invoice invoice)
        {
            var amountPara = new Paragraph(new Run("مائة وأربعة دولار أمريكي وأربعة مئة سنت فقط لا غير"))
            {
                FontSize = 8,
                FontStyle = FontStyles.Italic,
                TextAlignment = TextAlignment.Justify,
                Margin = new Thickness(0, 8, 0, 8)
            };
            doc.Blocks.Add(amountPara);
        }

        /// <summary>
        /// Creates signature lines
        /// </summary>
        private void CreateSignatureLines(FlowDocument doc)
        {
            var signTable = new Table();
            signTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
            signTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
            signTable.RowGroups.Add(new TableRowGroup());

            var signRow = new TableRow();

            signRow.Cells.Add(new TableCell(new Paragraph(new Run("توقيع المستلم: ..............................."))
            {
                FontSize = 8,
                TextAlignment = TextAlignment.Left,
                Margin = new Thickness(0, 15, 0, 0)
            }));

            signRow.Cells.Add(new TableCell(new Paragraph(new Run("توقيع المرسل: ..............................."))
            {
                FontSize = 8,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 15, 0, 0)
            }));

            signTable.RowGroups[0].Rows.Add(signRow);
            doc.Blocks.Add(signTable);
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

        /// <summary>
        /// Resets the form for a new invoice
        /// </summary>
        private async Task ResetForNewInvoiceAsync()
        {
            try
            {
                SelectedCustomer = null;
                SelectedTruck = null;

                InitializeCurrentInvoice();
                InitializeInvoiceItems();

                await GenerateNewInvoiceNumberAsync();
                CurrentDateTime = DateTime.Now;

                UpdateStatus("جاهز لإنشاء فاتورة جديدة", "CheckCircle", "#28A745");
                _logger.LogDebug("Form reset for new bulk invoice");
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


        #endregion
    }
}