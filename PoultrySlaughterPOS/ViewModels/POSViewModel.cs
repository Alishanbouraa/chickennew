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

            // ✅ FIXED: Initialize with temporary invoice number immediately
            InitializeCurrentInvoiceWithTempNumber();

            InitializeInvoiceItems();

            _logger.LogInformation("POSViewModel initialized with immediate invoice number display");
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

                // ✅ FIXED: Notify command state changes
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

                        // ✅ FIXED: Notify command state changes
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

                // ✅ FIXED: Generate real invoice number and update UI
                await GenerateAndSetRealInvoiceNumberAsync();

                UpdateStatus("جاهز لإنشاء فاتورة جديدة", "CheckCircle", "#28A745");
                _logger.LogInformation("POSViewModel initialized successfully with real invoice number");
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

                // ✅ Update the CurrentInvoice with real number - triggers UI binding update
                CurrentInvoice.InvoiceNumber = realInvoiceNumber;

                // ✅ Explicitly notify UI of property change
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

                        // ✅ Log individual item states for debugging
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
        /// Initializes invoice with temporary number for immediate UI display
        /// </summary>
        private void InitializeCurrentInvoiceWithTempNumber()
        {
            // Generate temporary invoice number for immediate display
            var tempInvoiceNumber = GenerateTemporaryInvoiceNumber();

            CurrentInvoice = new Invoice
            {
                InvoiceNumber = tempInvoiceNumber, // ✅ Immediate non-empty display
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

                    // ✅ FIXED: Notify command state changes
                    NotifyValidationStateChanged();

                    _logger.LogDebug("Customer selected: {CustomerName}", SelectedCustomer.CustomerName);
                }
                else
                {
                    // ✅ Customer deselected - notify command state
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

                    // ✅ FIXED: Notify command state changes
                    NotifyValidationStateChanged();

                    _logger.LogDebug("Truck selected: {TruckNumber}", SelectedTruck.TruckNumber);
                }
                else
                {
                    // ✅ Truck deselected - notify command state
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
                // ✅ Notify UI that CanSaveInvoice property may have changed
                OnPropertyChanged(nameof(CanSaveInvoice));

                // ✅ Notify RelayCommands that their CanExecute state may have changed
                SaveInvoiceCommand.NotifyCanExecuteChanged();
                SaveAndPrintInvoiceCommand.NotifyCanExecuteChanged();

                // ✅ Optional: Update status message based on current validation state
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
        /// Enhanced receipt printing with individual cage entries matching uploaded image design
        /// </summary>
        private async Task PrintBulkInvoiceAsync(Invoice invoice)
        {
            try
            {
                _logger.LogInformation("Starting enhanced bulk invoice printing with individual cage entries for Invoice: {InvoiceNumber}", invoice.InvoiceNumber);

                var doc = new FlowDocument();
                doc.PagePadding = new Thickness(20, 15, 20, 15);
                doc.ColumnGap = 0;
                doc.ColumnWidth = double.PositiveInfinity;
                doc.FontFamily = new System.Windows.Media.FontFamily("Arial Unicode MS, Tahoma, Arial");
                doc.FontSize = 10;
                doc.FlowDirection = FlowDirection.RightToLeft;

                // ===== EXACT HEADER SECTION =====
                CreateExactReceiptHeader(doc, invoice);

                // ===== CONTACT INFORMATION =====
                CreateContactInfo(doc);

                // ===== CUSTOMER SECTION =====
                CreateCustomerSection(doc);

                // ===== INDIVIDUAL CAGE DATA TABLE =====
                CreateIndividualCageDataTable(doc);

                // ===== AMOUNT IN WORDS =====
                CreateAmountInWords(doc, invoice);

                // ===== SIGNATURE LINES =====
                CreateSignatureLines(doc);

                // Print the document
                await PrintDocumentAsync(doc, $"فاتورة رقم {invoice.InvoiceNumber}");

                _logger.LogInformation("Enhanced individual cage receipt printing completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing enhanced bulk invoice: {InvoiceNumber}", invoice.InvoiceNumber);
                UpdateStatus("خطأ في طباعة الفاتورة", "ExclamationTriangle", "#DC3545");
                throw;
            }
        }
        /// <summary>
        /// Creates exact receipt header matching the uploaded image format
        /// </summary>
        private void CreateExactReceiptHeader(FlowDocument doc, Invoice invoice)
        {
            // Header table with exact three-column layout
            var headerTable = new Table();
            headerTable.Columns.Add(new TableColumn() { Width = new GridLength(100, GridUnitType.Pixel) }); // Phone numbers
            headerTable.Columns.Add(new TableColumn() { Width = new GridLength(200, GridUnitType.Pixel) }); // Company logo
            headerTable.Columns.Add(new TableColumn() { Width = new GridLength(100, GridUnitType.Pixel) }); // Receipt details
            headerTable.RowGroups.Add(new TableRowGroup());

            var headerRow = new TableRow();

            // ✅ LEFT COLUMN: Phone numbers (exact format from image)
            var phoneCell = new TableCell();
            phoneCell.Blocks.Add(new Paragraph(new Run("هاتف: 07/921642"))
            {
                FontSize = 9,
                Margin = new Thickness(0),
                TextAlignment = TextAlignment.Right
            });
            phoneCell.Blocks.Add(new Paragraph(new Run("03/600544 - 70/989448"))
            {
                FontSize = 9,
                Margin = new Thickness(0, 2, 0, 0),
                TextAlignment = TextAlignment.Right
            });

            // ✅ CENTER COLUMN: Company logo and name (exact format from image)
            var logoCell = new TableCell();
            logoCell.Blocks.Add(new Paragraph(new Run("🐓"))
            {
                FontSize = 24,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 3)
            });
            logoCell.Blocks.Add(new Paragraph(new Run("ابن تسليم"))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0)
            });
            logoCell.Blocks.Add(new Paragraph(new Run("(من مزارع غلا)"))
            {
                FontSize = 9,
                FontStyle = FontStyles.Italic,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 2, 0, 0)
            });

            // ✅ RIGHT COLUMN: Receipt details (exact format from image)
            var detailsCell = new TableCell();
            detailsCell.Blocks.Add(new Paragraph(new Run($"Nb: {invoice.InvoiceNumber}"))
            {
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Left,
                Margin = new Thickness(0)
            });
            detailsCell.Blocks.Add(new Paragraph(new Run($"التاريخ: {invoice.InvoiceDate:yyyy/MM/dd}"))
            {
                FontSize = 9,
                TextAlignment = TextAlignment.Left,
                Margin = new Thickness(0, 3, 0, 0)
            });

            headerRow.Cells.Add(phoneCell);
            headerRow.Cells.Add(logoCell);
            headerRow.Cells.Add(detailsCell);
            headerTable.RowGroups[0].Rows.Add(headerRow);

            doc.Blocks.Add(headerTable);

            // Add border line after header
            var borderPara = new Paragraph()
            {
                BorderBrush = System.Windows.Media.Brushes.Black,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Margin = new Thickness(0, 8, 0, 12)
            };
            doc.Blocks.Add(borderPara);
        }

        /// <summary>
        /// Creates the individual cage data table matching exact uploaded image structure
        /// </summary>
        private void CreateIndividualCageDataTable(FlowDocument doc)
        {
            var table = new Table();
            table.BorderThickness = new Thickness(1);
            table.BorderBrush = System.Windows.Media.Brushes.Black;
            table.CellSpacing = 0;

            // ✅ EXACT COLUMN STRUCTURE from uploaded image
            table.Columns.Add(new TableColumn() { Width = new GridLength(70, GridUnitType.Pixel) }); // النتج
            table.Columns.Add(new TableColumn() { Width = new GridLength(140, GridUnitType.Pixel) }); // Description
            table.Columns.Add(new TableColumn() { Width = new GridLength(80, GridUnitType.Pixel) }); // عدد الأقفاص
            table.Columns.Add(new TableColumn() { Width = new GridLength(70, GridUnitType.Pixel) }); // الوزن

            table.RowGroups.Add(new TableRowGroup());

            // ✅ CREATE TABLE HEADERS (exact Arabic text from image)
            CreateExactTableHeaders(table);

            // ✅ INDIVIDUAL CAGE ENTRIES (separate row for each cage/invoice item)
            CreateIndividualCageRows(table);

            // ✅ SEPARATOR ROW FOR TOTALS
            CreateTotalsSeparatorRow(table);

            // ✅ CALCULATION ROWS (exact format from image)
            CreateCalculationRows(table);

            doc.Blocks.Add(table);
            doc.Blocks.Add(new Paragraph() { Margin = new Thickness(0, 10, 0, 8) });

            // ✅ BALANCE SECTION with exact formatting
            CreateExactBalanceSection(doc);
        }

        /// <summary>
        /// Creates exact table headers matching the uploaded image
        /// </summary>
        private void CreateExactTableHeaders(Table table)
        {
            var headerRow = new TableRow();

            // Headers exactly as shown in the uploaded image
            var headers = new[] { "النتج", "Description", "عدد الأقفاص", "الوزن" };

            foreach (var headerText in headers)
            {
                var headerCell = new TableCell(new Paragraph(new Run(headerText))
                {
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(2),
                    FontSize = 10,
                    FontWeight = FontWeights.Bold
                });

                headerCell.BorderThickness = new Thickness(1);
                headerCell.BorderBrush = System.Windows.Media.Brushes.Black;
                headerCell.Background = System.Windows.Media.Brushes.LightGray;
                headerCell.Padding = new Thickness(4, 8, 4, 8);

                headerRow.Cells.Add(headerCell);
            }

            table.RowGroups[0].Rows.Add(headerRow);
        }
        private void CreateIndividualCageRows(Table table)
        {
            if (InvoiceItems == null || InvoiceItems.Count == 0)
            {
                _logger.LogWarning("No invoice items found for individual cage rows");
                return;
            }

            int cageNumber = 1;
            foreach (var item in InvoiceItems)
            {
                // Skip items with no meaningful data
                if (item.GrossWeight <= 0 && item.CagesCount <= 0)
                    continue;

                var cageRow = new TableRow();

                // ✅ COLUMN 1: Individual gross weight for this cage entry
                cageRow.Cells.Add(CreateIndividualDataCell(item.GrossWeight.ToString("F0")));

                // ✅ COLUMN 2: Description with cage number
                var description = $"قفص رقم {cageNumber}";
                if (item.CagesCount > 1)
                {
                    description = $"أقفاص {cageNumber}-{cageNumber + item.CagesCount - 1}";
                }
                cageRow.Cells.Add(CreateIndividualDataCell(description));

                // ✅ COLUMN 3: Individual cage count for this entry
                cageRow.Cells.Add(CreateIndividualDataCell(item.CagesCount.ToString()));

                // ✅ COLUMN 4: Individual weight for this entry
                cageRow.Cells.Add(CreateIndividualDataCell(item.GrossWeight.ToString("F0")));

                // Apply cage row styling (light background)
                foreach (var cell in cageRow.Cells)
                {
                    cell.Background = System.Windows.Media.Brushes.WhiteSmoke;
                }

                table.RowGroups[0].Rows.Add(cageRow);
                cageNumber += item.CagesCount;
            }
        }
        /// <summary>
        /// Creates the totals separator row with bold formatting
        /// </summary>
        private void CreateTotalsSeparatorRow(Table table)
        {
            var separatorRow = new TableRow();

            // Calculate actual totals from invoice items
            var totalGrossWeight = InvoiceItems?.Sum(i => i.GrossWeight) ?? 0;
            var totalCagesCount = InvoiceItems?.Sum(i => i.CagesCount) ?? 0;

            // ✅ BOLD TOTALS ROW (matching image format)
            separatorRow.Cells.Add(CreateBoldDataCell(totalGrossWeight.ToString("F0")));
            separatorRow.Cells.Add(CreateBoldDataCell("الوزن التام"));
            separatorRow.Cells.Add(CreateBoldDataCell(totalCagesCount.ToString()));
            separatorRow.Cells.Add(CreateBoldDataCell(totalGrossWeight.ToString("F0")));

            // Apply special styling for totals separator
            foreach (var cell in separatorRow.Cells)
            {
                cell.BorderThickness = new Thickness(1, 2, 1, 1); // Thicker top border
                cell.Background = System.Windows.Media.Brushes.LightBlue;
            }

            table.RowGroups[0].Rows.Add(separatorRow);
        }

        /// <summary>
        /// Creates calculation rows exactly matching the uploaded image format
        /// </summary>
        private void CreateCalculationRows(Table table)
        {
            // Calculate actual values from invoice items
            var aggregatedData = CalculateReceiptTotals();

            // ✅ CALCULATION ROWS exactly as shown in uploaded image
            var calculationRows = new[]
            {
        new { Value = aggregatedData.TotalCagesWeight.ToString("F0"), Description = "وزن الأقفاص", Col3 = "", Col4 = "" },
        new { Value = aggregatedData.TotalNetWeight.ToString("F0"), Description = "الإجمالي", Col3 = "", Col4 = "" },
        new { Value = aggregatedData.AverageDiscountPercentage.ToString("F0"), Description = "الخصم %", Col3 = "", Col4 = "" },
        new { Value = aggregatedData.AmountAfterDiscount.ToString("F0"), Description = "الباقي بعد الخصم", Col3 = "", Col4 = "" },
        new { Value = aggregatedData.WeightedAverageUnitPrice.ToString("F2"), Description = "سعر الوحدة", Col3 = "", Col4 = "" },
        new { Value = aggregatedData.FinalTotalAmount.ToString("F3"), Description = "المجموع", Col3 = "", Col4 = "USD" }
    };

            foreach (var rowData in calculationRows)
            {
                var row = new TableRow();

                row.Cells.Add(CreateCalculationDataCell(rowData.Value, rowData.Description == "المجموع"));
                row.Cells.Add(CreateCalculationDataCell(rowData.Description, rowData.Description == "المجموع"));
                row.Cells.Add(CreateCalculationDataCell(rowData.Col3, false));
                row.Cells.Add(CreateCalculationDataCell(rowData.Col4, rowData.Description == "المجموع"));

                // Special highlighting for final total row
                if (rowData.Description == "المجموع")
                {
                    foreach (var cell in row.Cells)
                    {
                        cell.Background = System.Windows.Media.Brushes.LightYellow;
                    }
                }

                table.RowGroups[0].Rows.Add(row);
            }
        }

        /// <summary>
        /// Creates exact balance section matching uploaded image format
        /// </summary>
        private void CreateExactBalanceSection(FlowDocument doc)
        {
            var balanceTable = new Table();
            balanceTable.BorderThickness = new Thickness(1);
            balanceTable.BorderBrush = System.Windows.Media.Brushes.Black;
            balanceTable.CellSpacing = 0;

            balanceTable.Columns.Add(new TableColumn() { Width = new GridLength(100, GridUnitType.Pixel) });
            balanceTable.Columns.Add(new TableColumn() { Width = new GridLength(200, GridUnitType.Pixel) });
            balanceTable.RowGroups.Add(new TableRowGroup());

            // Calculate balance values
            var aggregatedData = CalculateReceiptTotals();

            // ✅ PREVIOUS BALANCE ROW
            var prevRow = new TableRow();
            prevRow.Cells.Add(CreateBalanceCell(aggregatedData.PreviousBalance.ToString("F2"), false));
            prevRow.Cells.Add(CreateBalanceCell("الرصيد الباقي", false));
            balanceTable.RowGroups[0].Rows.Add(prevRow);

            // ✅ CURRENT BALANCE ROW (red background as shown in image)
            var currRow = new TableRow();
            prevRow.Cells.Add(CreateBalanceCell(aggregatedData.CurrentBalance.ToString("F2"), true));
            currRow.Cells.Add(CreateBalanceCell("الرصيد الحالي", true));
            balanceTable.RowGroups[0].Rows.Add(currRow);

            doc.Blocks.Add(balanceTable);
        }

        /// <summary>
        /// Enhanced amount in words with proper Arabic number conversion
        /// </summary>
        private void CreateAmountInWords(FlowDocument doc, Invoice invoice)
        {
            var totalAmount = InvoiceItems?.Sum(i => i.FinalAmount) ?? 0;
            var amountInWords = ConvertAmountToArabicWords(totalAmount);

            var amountPara = new Paragraph(new Run(amountInWords))
            {
                FontSize = 9,
                FontStyle = FontStyles.Italic,
                TextAlignment = TextAlignment.Justify,
                Margin = new Thickness(0, 10, 0, 10),
                Padding = new Thickness(8),
                Background = System.Windows.Media.Brushes.WhiteSmoke,
                BorderBrush = System.Windows.Media.Brushes.Gray,
                BorderThickness = new Thickness(1)
            };
            doc.Blocks.Add(amountPara);
        }

        /// <summary>
        /// Helper method to create individual cage data cells
        /// </summary>
        private TableCell CreateIndividualDataCell(string text)
        {
            var paragraph = new Paragraph(new Run(text ?? ""))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(1),
                FontSize = 9
            };

            var cell = new TableCell(paragraph);
            cell.BorderThickness = new Thickness(1);
            cell.BorderBrush = System.Windows.Media.Brushes.Black;
            cell.Padding = new Thickness(3, 6, 3, 6);

            return cell;
        }

        /// <summary>
        /// Helper method to create bold data cells for totals
        /// </summary>
        private TableCell CreateBoldDataCell(string text)
        {
            var paragraph = new Paragraph(new Run(text ?? ""))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(1),
                FontSize = 9,
                FontWeight = FontWeights.Bold
            };

            var cell = new TableCell(paragraph);
            cell.BorderThickness = new Thickness(1);
            cell.BorderBrush = System.Windows.Media.Brushes.Black;
            cell.Padding = new Thickness(3, 6, 3, 6);

            return cell;
        }

        /// <summary>
        /// Helper method to create calculation data cells
        /// </summary>
        private TableCell CreateCalculationDataCell(string text, bool isTotal = false)
        {
            var paragraph = new Paragraph(new Run(text ?? ""))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(1),
                FontSize = 9,
                FontWeight = isTotal ? FontWeights.Bold : FontWeights.Normal
            };

            var cell = new TableCell(paragraph);
            cell.BorderThickness = new Thickness(1);
            cell.BorderBrush = System.Windows.Media.Brushes.Black;
            cell.Padding = new Thickness(3, 6, 3, 6);

            return cell;
        }

        /// <summary>
        /// Helper method to create balance section cells
        /// </summary>
        private TableCell CreateBalanceCell(string text, bool isCurrentBalance)
        {
            var paragraph = new Paragraph(new Run(text ?? ""))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(1),
                FontSize = 10,
                FontWeight = FontWeights.Bold
            };

            var cell = new TableCell(paragraph);
            cell.BorderThickness = new Thickness(1);
            cell.BorderBrush = System.Windows.Media.Brushes.Black;
            cell.Padding = new Thickness(6, 8, 6, 8);

            // ✅ RED BACKGROUND for current balance (matching uploaded image)
            if (isCurrentBalance)
            {
                cell.Background = System.Windows.Media.Brushes.Red;
                cell.Foreground = System.Windows.Media.Brushes.White;
            }

            return cell;
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
        /// Creates customer information section with actual selected customer data
        /// </summary>
        private void CreateCustomerSection(FlowDocument doc)
        {
            var customerName = SelectedCustomer?.CustomerName ?? "غير محدد";
            var customerPhone = SelectedCustomer?.PhoneNumber ?? "";

            var customerPara = new Paragraph(new Run($"المطلوب من السيد: {customerName}"))
            {
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 0, 0, 4)
            };
            doc.Blocks.Add(customerPara);

            // Add phone number if available
            if (!string.IsNullOrEmpty(customerPhone))
            {
                var phonePara = new Paragraph(new Run($"الهاتف: {customerPhone}"))
                {
                    FontSize = 8,
                    TextAlignment = TextAlignment.Right,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                doc.Blocks.Add(phonePara);
            }
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

            // ✅ FIXED: Define columns matching the receipt image exactly
            table.Columns.Add(new TableColumn() { Width = new GridLength(60, GridUnitType.Pixel) }); // النتج
            table.Columns.Add(new TableColumn() { Width = new GridLength(120, GridUnitType.Pixel) }); // Description
            table.Columns.Add(new TableColumn() { Width = new GridLength(80, GridUnitType.Pixel) }); // عدد الأقفاص
            table.Columns.Add(new TableColumn() { Width = new GridLength(60, GridUnitType.Pixel) }); // الوزن

            table.RowGroups.Add(new TableRowGroup());

            // ✅ ADD: Create table headers matching the receipt image
            CreateTableHeaders(table);

            // ✅ ENHANCED: Calculate actual totals from invoice items
            var aggregatedData = CalculateReceiptTotals();

            // ✅ FIXED: Create data rows with actual calculated values from POS interface
            var dataRows = new[]
            {
        new {
            Value = aggregatedData.TotalGrossWeight.ToString("F0"),
            Description = "الوزن التام",
            CageCount = aggregatedData.TotalCagesCount.ToString(),
            Weight = aggregatedData.TotalGrossWeight.ToString("F0")
        },
        new {
            Value = aggregatedData.TotalCagesWeight.ToString("F0"),
            Description = "وزن الأقفاص",
            CageCount = "",
            Weight = ""
        },
        new {
            Value = aggregatedData.TotalNetWeight.ToString("F0"),
            Description = "الإجمالي",
            CageCount = "",
            Weight = ""
        },
        new {
            Value = aggregatedData.AverageDiscountPercentage.ToString("F0"),
            Description = "الخصم %",
            CageCount = "",
            Weight = ""
        },
        new {
            Value = aggregatedData.AmountAfterDiscount.ToString("F0"),
            Description = "الباقي بعد الخصم",
            CageCount = "",
            Weight = ""
        },
        new {
            Value = aggregatedData.WeightedAverageUnitPrice.ToString("F2"),
            Description = "سعر الوحدة",
            CageCount = "",
            Weight = ""
        },
        new {
            Value = aggregatedData.FinalTotalAmount.ToString("F3"),
            Description = "المجموع",
            CageCount = "",
            Weight = "USD"
        }
    };

            // Add data rows to table
            foreach (var rowData in dataRows)
            {
                var row = new TableRow();
                row.Cells.Add(CreateDataCell(rowData.Value));
                row.Cells.Add(CreateDataCell(rowData.Description));
                row.Cells.Add(CreateDataCell(rowData.CageCount));
                row.Cells.Add(CreateDataCell(rowData.Weight));
                table.RowGroups[0].Rows.Add(row);
            }

            doc.Blocks.Add(table);
            doc.Blocks.Add(new Paragraph() { Margin = new Thickness(0, 8, 0, 4) });

            // Create balance section with actual customer data
            CreateBalanceSection(doc, aggregatedData);
        }
        /// <summary>
        /// Creates table headers matching the receipt image structure
        /// </summary>
        private void CreateTableHeaders(Table table)
        {
            var headerRow = new TableRow();

            // Create header cells with proper Arabic text
            var headers = new[] { "النتج", "Description", "عدد الأقفاص", "الوزن" };

            foreach (var headerText in headers)
            {
                var headerCell = new TableCell(new Paragraph(new Run(headerText))
                {
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(1),
                    FontSize = 9,
                    FontWeight = FontWeights.Bold
                });

                headerCell.BorderThickness = new Thickness(0.5);
                headerCell.BorderBrush = System.Windows.Media.Brushes.Black;
                headerCell.Background = System.Windows.Media.Brushes.LightGray;
                headerCell.Padding = new Thickness(4, 6, 4, 6);

                headerRow.Cells.Add(headerCell);
            }

            table.RowGroups[0].Rows.Add(headerRow);
        }

        /// <summary>
        /// Enhanced calculation method supporting individual cage display
        /// </summary>
        private ReceiptTotals CalculateReceiptTotals()
        {
            if (InvoiceItems == null || InvoiceItems.Count == 0)
            {
                _logger.LogWarning("No invoice items found for receipt calculation");
                return new ReceiptTotals();
            }

            var totals = new ReceiptTotals();

            // ✅ AGGREGATE all values from individual cage entries
            foreach (var item in InvoiceItems)
            {
                totals.TotalGrossWeight += item.GrossWeight;
                totals.TotalCagesCount += item.CagesCount;
                totals.TotalCagesWeight += item.CagesWeight;
                totals.TotalNetWeight += item.NetWeight;
                totals.TotalAmountBeforeDiscount += item.TotalAmount;
                totals.TotalDiscountAmount += item.DiscountAmount;
                totals.FinalTotalAmount += item.FinalAmount;
            }

            // ✅ CALCULATE weighted averages for accurate receipt display
            var totalWeightForPricing = InvoiceItems.Where(item => item.NetWeight > 0).Sum(item => item.NetWeight);
            if (totalWeightForPricing > 0)
            {
                totals.WeightedAverageUnitPrice = InvoiceItems
                    .Where(item => item.NetWeight > 0)
                    .Sum(item => item.UnitPrice * item.NetWeight) / totalWeightForPricing;
            }

            if (totals.TotalAmountBeforeDiscount > 0)
            {
                totals.AverageDiscountPercentage = InvoiceItems
                    .Where(item => item.TotalAmount > 0)
                    .Sum(item => item.DiscountPercentage * item.TotalAmount) / totals.TotalAmountBeforeDiscount;
            }

            // ✅ BALANCE calculations with customer data
            totals.AmountAfterDiscount = totals.FinalTotalAmount;
            totals.PreviousBalance = SelectedCustomer?.TotalDebt ?? 0;
            totals.CurrentBalance = totals.PreviousBalance + totals.FinalTotalAmount;

            _logger.LogInformation("Enhanced receipt totals calculated for {ItemCount} individual cage entries - Total: ${Amount}",
                InvoiceItems.Count, totals.FinalTotalAmount);

            return totals;
        }

        private string ConvertAmountToArabicWords(decimal amount)
        {
            try
            {
                // ✅ ENHANCED Arabic number-to-words conversion
                int dollars = (int)Math.Floor(amount);
                int cents = (int)Math.Round((amount - dollars) * 100);

                var dollarsInWords = ConvertIntegerToArabicWords(dollars);
                var centsInWords = ConvertIntegerToArabicWords(cents);

                return $"{dollarsInWords} دولار أمريكي و{centsInWords} سنت فقط لا غير";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error converting amount to Arabic words: {Amount}", amount);
                return $"{amount:F2} دولار أمريكي فقط لا غير";
            }
        }

        /// <summary>
        /// Converts integer to Arabic words (simplified implementation)
        /// </summary>
        private string ConvertIntegerToArabicWords(int number)
        {
            if (number == 0) return "صفر";

            var ones = new[] { "", "واحد", "اثنان", "ثلاثة", "أربعة", "خمسة", "ستة", "سبعة", "ثمانية", "تسعة" };
            var tens = new[] { "", "", "عشرون", "ثلاثون", "أربعون", "خمسون", "ستون", "سبعون", "ثمانون", "تسعون" };
            var hundreds = new[] { "", "مائة", "مائتان", "ثلاثمائة", "أربعمائة", "خمسمائة", "ستمائة", "سبعمائة", "ثمانمائة", "تسعمائة" };

            if (number >= 1000)
            {
                return $"{ConvertIntegerToArabicWords(number / 1000)} ألف {ConvertIntegerToArabicWords(number % 1000)}".Trim();
            }

            var result = "";

            if (number >= 100)
            {
                result += hundreds[number / 100] + " ";
                number %= 100;
            }

            if (number >= 20)
            {
                result += tens[number / 10] + " ";
                number %= 10;
            }
            else if (number >= 11)
            {
                var teens = new[] { "", "أحد عشر", "اثنا عشر", "ثلاثة عشر", "أربعة عشر", "خمسة عشر", "ستة عشر", "سبعة عشر", "ثمانية عشر", "تسعة عشر" };
                result += teens[number - 10] + " ";
                number = 0;
            }
            else if (number == 10)
            {
                result += "عشرة ";
                number = 0;
            }

            if (number > 0)
            {
                result += ones[number] + " ";
            }

            return result.Trim();
        }
        private void CreateBalanceSection(FlowDocument doc, ReceiptTotals totals)
        {
            var balanceTable = new Table();
            balanceTable.Columns.Add(new TableColumn() { Width = new GridLength(100, GridUnitType.Pixel) });
            balanceTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
            balanceTable.RowGroups.Add(new TableRowGroup());

            // Previous balance row
            var prevRow = new TableRow();
            prevRow.Cells.Add(CreateDataCell(totals.PreviousBalance.ToString("F2")));
            prevRow.Cells.Add(CreateDataCell("الرصيد الباقي"));
            balanceTable.RowGroups[0].Rows.Add(prevRow);

            // Current balance row (highlighted in red)
            var currRow = new TableRow();
            var currBalanceCell = CreateDataCell(totals.CurrentBalance.ToString("F2"));
            currBalanceCell.Background = System.Windows.Media.Brushes.Red;
            currBalanceCell.Foreground = System.Windows.Media.Brushes.White;
            currRow.Cells.Add(currBalanceCell);
            currRow.Cells.Add(CreateDataCell("الرصيد الحالي"));
            balanceTable.RowGroups[0].Rows.Add(currRow);

            doc.Blocks.Add(balanceTable);
        }

        /// <summary>
        /// Creates a formatted data cell with enhanced styling for receipt printing
        /// </summary>
        private TableCell CreateDataCell(string text, bool isHeader = false, bool isHighlighted = false)
        {
            var paragraph = new Paragraph(new Run(text ?? ""))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(1),
                FontSize = isHeader ? 9 : 8
            };

            if (isHeader)
            {
                paragraph.FontWeight = FontWeights.Bold;
            }

            var cell = new TableCell(paragraph);
            cell.BorderThickness = new Thickness(0.5);
            cell.BorderBrush = System.Windows.Media.Brushes.Black;
            cell.Padding = new Thickness(3, 4, 3, 4);

            if (isHighlighted)
            {
                cell.Background = System.Windows.Media.Brushes.LightYellow;
            }

            return cell;
        }

        // ✅ ADDED: Comprehensive public API for UI layer integration
        public void RecalculateInvoiceTotals()
        {
            try
            {
                // Force recalculation of all invoice items with validation
                if (InvoiceItems != null)
                {
                    foreach (var item in InvoiceItems)
                    {
                        CalculateInvoiceItem(item);
                    }
                }

                RecalculateTotals(); // Internal aggregate calculation

                // ✅ Update command execution state for UI responsiveness
                NotifyValidationStateChanged();

                _logger.LogDebug("Invoice totals recalculated and command state updated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during invoice totals recalculation");
            }
        }

        public void TriggerValidationCheck()
        {
            try
            {
                _logger.LogInformation("Manual validation check triggered");
                var isValid = ValidateCurrentInvoice(true);
                NotifyValidationStateChanged();

                _logger.LogInformation("Manual validation result: {IsValid}", isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during manual validation check");
            }
        }
        public void LogCurrentState()
        {
            try
            {
                _logger.LogInformation("=== CURRENT POS STATE DEBUG ===");
                _logger.LogInformation("Customer Selected: {Customer}", SelectedCustomer?.CustomerName ?? "NONE");
                _logger.LogInformation("Truck Selected: {Truck}", SelectedTruck?.TruckNumber ?? "NONE");
                _logger.LogInformation("Invoice Items Count: {Count}", InvoiceItems?.Count ?? 0);
                _logger.LogInformation("CanSaveInvoice: {CanSave}", CanSaveInvoice);
                _logger.LogInformation("IsLoading: {IsLoading}", IsLoading);
                _logger.LogInformation("HasValidationErrors: {HasErrors}", HasValidationErrors);

                if (InvoiceItems != null)
                {
                    for (int i = 0; i < InvoiceItems.Count; i++)
                    {
                        var item = InvoiceItems[i];
                        _logger.LogInformation("Item {Index}: GW={GrossWeight}, CC={CagesCount}, CW={CageWeight}, UP={UnitPrice}, NW={NetWeight}",
                            i + 1, item.GrossWeight, item.CagesCount, item.CageWeight, item.UnitPrice, item.NetWeight);
                    }
                }

                _logger.LogInformation("=== END DEBUG STATE ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging current state");
            }
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