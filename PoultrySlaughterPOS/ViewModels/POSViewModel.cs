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
    /// Enterprise-grade Point of Sale ViewModel implementing comprehensive invoice processing,
    /// real-time calculations, customer management, and business logic orchestration
    /// for poultry slaughter operations with MVVM architecture and async patterns.
    /// UPDATED: Complete AddCustomerDialog integration with dependency injection support.
    /// </summary>
    public partial class POSViewModel : ObservableObject
    {
        #region Private Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<POSViewModel> _logger;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly IServiceProvider _serviceProvider; // Added for dialog dependency injection
        private bool _isLoading = false;
        private bool _hasValidationErrors = false;

        // Collections for UI binding
        private ObservableCollection<Customer> _customers = new();
        private ObservableCollection<Truck> _trucks = new();
        private ObservableCollection<string> _validationErrors = new();

        // Current selections and state
        private Customer? _selectedCustomer;
        private Truck? _selectedTruck;
        private Invoice _currentInvoice = new();
        private DateTime _currentDateTime = DateTime.Now;
        private DateTime? _lastTruckLoadDate;

        // Status and UI state
        private string _statusMessage = "جاهز لإنشاء فاتورة جديدة";
        private string _statusIcon = "CheckCircle";
        private string _statusColor = "#28A745";

        // Statistics
        private int _todayInvoiceCount = 0;
        private DateTime? _lastInvoiceTime;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes POSViewModel with comprehensive dependency injection including dialog services
        /// </summary>
        /// <param name="unitOfWork">Unit of work for database operations</param>
        /// <param name="logger">Logger for diagnostic and error tracking</param>
        /// <param name="errorHandlingService">Centralized error handling service</param>
        /// <param name="serviceProvider">Service provider for dialog dependency injection</param>
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

            _logger.LogInformation("POSViewModel initialized with complete dependency injection including dialog services");
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
            set
            {
                if (SetProperty(ref _currentInvoice, value))
                {
                    // Unsubscribe from old invoice if exists
                    if (_currentInvoice != null)
                    {
                        _currentInvoice.PropertyChanged -= CurrentInvoice_PropertyChanged;
                    }

                    // Subscribe to new invoice property changes
                    if (value != null)
                    {
                        value.PropertyChanged += CurrentInvoice_PropertyChanged;
                    }

                    // Trigger UI updates
                    OnPropertyChanged(nameof(DiscountAmount));
                    OnPropertyChanged(nameof(CanSaveInvoice));

                    // Trigger command CanExecute reevaluation
                    SaveInvoiceCommand.NotifyCanExecuteChanged();
                    SaveAndPrintInvoiceCommand.NotifyCanExecuteChanged();
                }
            }
        }
        /// <summary>
        /// Handles property changes from the CurrentInvoice to trigger validation and calculations
        /// </summary>
        private void CurrentInvoice_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            try
            {
                // Recalculate totals when key properties change
                var triggerCalculation = e.PropertyName switch
                {
                    nameof(Invoice.GrossWeight) or
                    nameof(Invoice.CagesWeight) or
                    nameof(Invoice.UnitPrice) or
                    nameof(Invoice.DiscountPercentage) => true,
                    _ => false
                };

                if (triggerCalculation)
                {
                    RecalculateInvoiceTotals();
                }

                // Always trigger validation check
                OnPropertyChanged(nameof(CanSaveInvoice));
                SaveInvoiceCommand.NotifyCanExecuteChanged();
                SaveAndPrintInvoiceCommand.NotifyCanExecuteChanged();

                _logger.LogDebug("CurrentInvoice property changed: {PropertyName}", e.PropertyName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling CurrentInvoice property change: {PropertyName}", e.PropertyName);
            }
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
        /// Last truck load date for selected truck
        /// </summary>
        public DateTime? LastTruckLoadDate
        {
            get => _lastTruckLoadDate;
            set => SetProperty(ref _lastTruckLoadDate, value);
        }

        /// <summary>
        /// Calculated discount amount based on percentage
        /// </summary>
        public decimal DiscountAmount
        {
            get
            {
                if (CurrentInvoice == null) return 0;
                return CurrentInvoice.TotalAmount * (CurrentInvoice.DiscountPercentage / 100);
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
        /// Today's invoice count for statistics
        /// </summary>
        public int TodayInvoiceCount
        {
            get => _todayInvoiceCount;
            set => SetProperty(ref _todayInvoiceCount, value);
        }

        /// <summary>
        /// Last invoice creation time
        /// </summary>
        public DateTime? LastInvoiceTime
        {
            get => _lastInvoiceTime;
            set => SetProperty(ref _lastInvoiceTime, value);
        }

        /// <summary>
        /// Indicates whether the current invoice can be saved
        /// </summary>
        public bool CanSaveInvoice => ValidateCurrentInvoice(false);

        #endregion

        #region Commands

        [RelayCommand(CanExecute = nameof(CanExecuteSaveAndPrintInvoice))]
        private async Task SaveAndPrintInvoiceAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                _logger.LogInformation("Executing SaveAndPrintInvoice command");

                var savedInvoice = await SaveInvoiceInternalAsync();
                if (savedInvoice != null)
                {
                    // Show print dialog before printing
                    await PrintInvoiceWithDialogAsync(savedInvoice);
                    await ResetForNewInvoiceAsync();
                }
            }, "حفظ وطباعة الفاتورة");
        }
        [RelayCommand(CanExecute = nameof(CanExecuteSaveInvoice))]
        private async Task SaveInvoiceAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                _logger.LogInformation("Executing SaveInvoice command");

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

                // Implementation for printing previous invoice
                // This would typically open a dialog to select and print previous invoices
                UpdateStatus("طباعة الفواتير السابقة قيد التطوير", "Info", "#FFC107");

                await Task.Delay(100); // Placeholder for actual implementation
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

        /// <summary>
        /// UPDATED: Complete AddNewCustomer command implementation with dialog integration
        /// </summary>
        [RelayCommand]
        private async Task AddNewCustomerAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                _logger.LogInformation("Executing AddNewCustomer command with dialog integration");

                try
                {
                    UpdateStatus("جاري فتح نافذة إضافة زبون جديد...", "UserPlus", "#007BFF");

                    // Get current window for modal dialog positioning
                    var currentWindow = GetCurrentWindow();

                    // Create and show customer dialog using dependency injection
                    var createdCustomer = await AddCustomerDialog.ShowNewCustomerDialogAsync(_serviceProvider, currentWindow);

                    if (createdCustomer != null)
                    {
                        _logger.LogInformation("New customer created successfully: {CustomerName} (ID: {CustomerId})",
                            createdCustomer.CustomerName, createdCustomer.CustomerId);

                        // Add new customer to the collection for immediate availability
                        Customers.Insert(0, createdCustomer); // Add at top for easy selection

                        // Automatically select the newly created customer
                        SelectedCustomer = createdCustomer;

                        // Update status with success message
                        UpdateStatus($"تم إضافة الزبون '{createdCustomer.CustomerName}' بنجاح", "CheckCircle", "#28A745");

                        // Trigger property change notifications for UI updates
                        OnPropertyChanged(nameof(Customers));
                        OnPropertyChanged(nameof(SelectedCustomer));

                        _logger.LogInformation("Customer '{CustomerName}' added to collection and auto-selected", createdCustomer.CustomerName);
                    }
                    else
                    {
                        // User cancelled the dialog
                        UpdateStatus("تم إلغاء إضافة الزبون", "Times", "#6C757D");
                        _logger.LogDebug("AddCustomer dialog was cancelled by user");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in AddNewCustomer command execution");
                    UpdateStatus("خطأ في إضافة الزبون", "ExclamationTriangle", "#DC3545");

                    // Show user-friendly error message
                    var currentWindow = GetCurrentWindow();
                    MessageBox.Show($"خطأ في إضافة زبون جديد:\n{ex.Message}",
                                   "خطأ",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Warning);
                }
            }, "إضافة زبون جديد");
        }

        private bool CanExecuteSaveAndPrintInvoice() => CanSaveInvoice && !IsLoading;
        private bool CanExecuteSaveInvoice() => CanSaveInvoice && !IsLoading;

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
                await LoadTodayStatisticsAsync();
                await GenerateNewInvoiceNumberAsync();

                UpdateStatus("جاهز لإنشاء فاتورة جديدة", "CheckCircle", "#28A745");
                _logger.LogInformation("POSViewModel initialized successfully with data loading completed");
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
        /// Recalculates invoice totals when input values change
        /// </summary>
        public void RecalculateInvoiceTotals()
        {
            try
            {
                if (CurrentInvoice == null) return;

                // Calculate net weight
                CurrentInvoice.NetWeight = Math.Max(0, CurrentInvoice.GrossWeight - CurrentInvoice.CagesWeight);

                // Calculate total amount
                CurrentInvoice.TotalAmount = CurrentInvoice.NetWeight * CurrentInvoice.UnitPrice;

                // Calculate final amount with discount
                var discountAmount = CurrentInvoice.TotalAmount * (CurrentInvoice.DiscountPercentage / 100);
                CurrentInvoice.FinalAmount = CurrentInvoice.TotalAmount - discountAmount;

                // Update balance calculations
                if (SelectedCustomer != null)
                {
                    CurrentInvoice.PreviousBalance = SelectedCustomer.TotalDebt;
                    CurrentInvoice.CurrentBalance = SelectedCustomer.TotalDebt + CurrentInvoice.FinalAmount;
                }

                // Notify UI of changes
                OnPropertyChanged(nameof(CurrentInvoice));
                OnPropertyChanged(nameof(DiscountAmount));
                OnPropertyChanged(nameof(CanSaveInvoice));

                _logger.LogDebug("Invoice totals recalculated - Net Weight: {NetWeight}, Final Amount: {FinalAmount}",
                    CurrentInvoice.NetWeight, CurrentInvoice.FinalAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during invoice total recalculation");
            }
        }

        /// <summary>
        /// Validates the current invoice and returns validation result with detailed logging
        /// </summary>
        public bool ValidateCurrentInvoice(bool showErrors = true)
        {
            try
            {
                ValidationErrors.Clear();
                var validationResults = new List<string>();

                _logger.LogDebug("Starting invoice validation...");

                if (CurrentInvoice == null)
                {
                    validationResults.Add("لا توجد فاتورة للتحقق منها");
                    _logger.LogWarning("Validation failed: CurrentInvoice is null");
                }
                else
                {
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

                    // Weight validations
                    if (CurrentInvoice.GrossWeight <= 0)
                    {
                        validationResults.Add("يجب إدخال الوزن الفلتي");
                        _logger.LogDebug("Validation failed: GrossWeight = {GrossWeight}", CurrentInvoice.GrossWeight);
                    }

                    if (CurrentInvoice.CagesWeight < 0)
                    {
                        validationResults.Add("وزن الأقفاص لا يمكن أن يكون سالباً");
                        _logger.LogDebug("Validation failed: CagesWeight = {CagesWeight}", CurrentInvoice.CagesWeight);
                    }

                    if (CurrentInvoice.CagesWeight >= CurrentInvoice.GrossWeight && CurrentInvoice.GrossWeight > 0)
                    {
                        validationResults.Add("وزن الأقفاص لا يمكن أن يكون أكبر من أو يساوي الوزن الفلتي");
                        _logger.LogDebug("Validation failed: CagesWeight ({CagesWeight}) >= GrossWeight ({GrossWeight})",
                            CurrentInvoice.CagesWeight, CurrentInvoice.GrossWeight);
                    }

                    if (CurrentInvoice.CagesCount <= 0)
                    {
                        validationResults.Add("يجب إدخال عدد الأقفاص");
                        _logger.LogDebug("Validation failed: CagesCount = {CagesCount}", CurrentInvoice.CagesCount);
                    }

                    // Price validation
                    if (CurrentInvoice.UnitPrice <= 0)
                    {
                        validationResults.Add("يجب إدخال سعر الوحدة");
                        _logger.LogDebug("Validation failed: UnitPrice = {UnitPrice}", CurrentInvoice.UnitPrice);
                    }

                    // Discount validation
                    if (CurrentInvoice.DiscountPercentage < 0 || CurrentInvoice.DiscountPercentage > 100)
                    {
                        validationResults.Add("نسبة الخصم يجب أن تكون بين 0 و 100");
                        _logger.LogDebug("Validation failed: DiscountPercentage = {DiscountPercentage}", CurrentInvoice.DiscountPercentage);
                    }
                }

                // Update validation state
                foreach (var error in validationResults)
                {
                    ValidationErrors.Add(error);
                }

                HasValidationErrors = ValidationErrors.Count > 0;
                var isValid = !HasValidationErrors;

                _logger.LogDebug("Validation completed: IsValid = {IsValid}, ErrorCount = {ErrorCount}",
                    isValid, ValidationErrors.Count);

                if (HasValidationErrors && showErrors)
                {
                    UpdateStatus($"توجد {ValidationErrors.Count} أخطاء في البيانات", "ExclamationTriangle", "#DC3545");

                    // Log all validation errors for debugging
                    foreach (var error in ValidationErrors)
                    {
                        _logger.LogWarning("Validation error: {Error}", error);
                    }
                }
                else if (isValid)
                {
                    UpdateStatus("البيانات صحيحة وجاهزة للحفظ", "CheckCircle", "#28A745");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during invoice validation");
                return false;
            }
        }
 private async Task PrintInvoiceWithDialogAsync(Invoice invoice)
{
    try
    {
        // Create and show print dialog
        var printDialog = new PrintDialog();

        // Configure print dialog settings
        printDialog.PrintQueue = LocalPrintServer.GetDefaultPrintQueue();
        printDialog.PrintTicket = printDialog.PrintQueue.DefaultPrintTicket;

        if (printDialog.ShowDialog() == true)
        {
            UpdateStatus("جاري طباعة الفاتورة...", "Print", "#007BFF");
            await PrintInvoiceAsync(invoice);
        }
        else
        {
            UpdateStatus("تم إلغاء الطباعة", "Times", "#6C757D");
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in print dialog");
        UpdateStatus("خطأ في طباعة الفاتورة", "ExclamationTriangle", "#DC3545");
    }
}

        /// <summary>
        /// Resets the current invoice for new entry
        /// </summary>
        public void ResetCurrentInvoice()
        {
            try
            {
                InitializeCurrentInvoice();
                SelectedCustomer = null;
                SelectedTruck = null;
                ValidationErrors.Clear();
                HasValidationErrors = false;

                UpdateStatus("جاهز لإنشاء فاتورة جديدة", "CheckCircle", "#28A745");

                _logger.LogDebug("Current invoice reset for new entry");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting current invoice");
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

                // Clear collections
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
            // Commands are initialized via source generators with RelayCommand attributes
            _logger.LogDebug("POSViewModel commands initialized via source generators");
        }

        /// <summary>
        /// Initializes a new invoice with default values and proper event subscription
        /// </summary>
        private void InitializeCurrentInvoice()
        {
            var newInvoice = new Invoice
            {
                InvoiceDate = DateTime.Now,
                GrossWeight = 0,
                CagesWeight = 0,
                CagesCount = 0,
                NetWeight = 0,
                UnitPrice = 0,
                DiscountPercentage = 0,
                TotalAmount = 0,
                FinalAmount = 0,
                PreviousBalance = 0,
                CurrentBalance = 0,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            // This will trigger the property setter and set up event subscription
            CurrentInvoice = newInvoice;

            _logger.LogDebug("New invoice initialized with proper change tracking");
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
        /// Loads today's statistics for dashboard display
        /// </summary>
        private async Task LoadTodayStatisticsAsync()
        {
            try
            {
                var todayInvoices = await _unitOfWork.Invoices.GetInvoicesByDateAsync(DateTime.Today);
                TodayInvoiceCount = todayInvoices.Count();

                var lastInvoice = todayInvoices.OrderByDescending(i => i.InvoiceDate).FirstOrDefault();
                LastInvoiceTime = lastInvoice?.InvoiceDate;

                _logger.LogDebug("Today's statistics loaded - Invoice Count: {Count}, Last Invoice: {LastTime}",
                    TodayInvoiceCount, LastInvoiceTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading today's statistics");
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
                    RecalculateInvoiceTotals();

                    _logger.LogDebug("Customer selected: {CustomerName} (ID: {CustomerId})",
                        SelectedCustomer.CustomerName, SelectedCustomer.CustomerId);
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
        private async void OnTruckSelectionChanged()
        {
            try
            {
                if (SelectedTruck != null)
                {
                    CurrentInvoice.TruckId = SelectedTruck.TruckId;

                    // Load last truck load date
                    var lastLoad = await _unitOfWork.TruckLoads.GetLatestTruckLoadAsync(SelectedTruck.TruckId);
                    LastTruckLoadDate = lastLoad?.LoadDate;

                    _logger.LogDebug("Truck selected: {TruckNumber} (ID: {TruckId})",
                        SelectedTruck.TruckNumber, SelectedTruck.TruckId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling truck selection change");
            }
        }

        /// <summary>
        /// Internal method for saving invoice with full transaction support
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

                var savedInvoice = await _unitOfWork.Invoices.CreateInvoiceWithTransactionAsync(CurrentInvoice);
                await _unitOfWork.SaveChangesAsync("POS_USER");

                // Update statistics
                TodayInvoiceCount++;
                LastInvoiceTime = savedInvoice.InvoiceDate;

                UpdateStatus("تم حفظ الفاتورة بنجاح", "CheckCircle", "#28A745");

                _logger.LogInformation("Invoice saved successfully - Number: {InvoiceNumber}, Amount: {Amount}",
                    savedInvoice.InvoiceNumber, savedInvoice.FinalAmount);

                return savedInvoice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving invoice");
                UpdateStatus("خطأ في حفظ الفاتورة", "ExclamationTriangle", "#DC3545");
                throw;
            }
        }

        /// <summary>
        /// Prints the specified invoice
        /// </summary>
        /// <summary>
        /// Prints invoice with Arabic layout matching the uploaded receipt design
        /// Implements exact visual structure with proper RTL formatting and table layout
        /// </summary>
        /// <param name="invoice">The invoice to print</param>
        private async Task PrintInvoiceAsync(Invoice invoice)
        {
            try
            {
                _logger.LogInformation("Starting invoice printing with Arabic receipt layout for Invoice: {InvoiceNumber}", invoice.InvoiceNumber);

                // Create FlowDocument with Arabic formatting
                var doc = new FlowDocument();
                doc.PagePadding = new Thickness(30, 20, 30, 20);
                doc.ColumnGap = 0;
                doc.ColumnWidth = double.PositiveInfinity;

                // Set Arabic font and RTL direction
                doc.FontFamily = new System.Windows.Media.FontFamily("Arial Unicode MS, Tahoma, Arial");
                doc.FontSize = 11;
                doc.FlowDirection = FlowDirection.RightToLeft;
                doc.Language = System.Windows.Markup.XmlLanguage.GetLanguage("ar-SA");

                // ===== HEADER SECTION WITH ROOSTER LOGO =====
                await CreateHeaderSectionAsync(doc, invoice);

                // ===== CONTACT INFORMATION =====
                CreateContactSection(doc);

                // ===== CUSTOMER SECTION =====
                CreateCustomerSection(doc);

                // ===== MAIN DATA TABLE =====
                CreateMainDataTable(doc, invoice);

                // ===== FINANCIAL SUMMARY =====
                CreateFinancialSummary(doc, invoice);

                // ===== AMOUNT IN WORDS =====
                CreateAmountInWordsSection(doc, invoice);

                // ===== SIGNATURE SECTION =====
                CreateSignatureSection(doc);

                // Print the document
                await PrintDocumentAsync(doc, $"فاتورة رقم {invoice.InvoiceNumber}");

                _logger.LogInformation("Invoice printing completed successfully for Invoice: {InvoiceNumber}", invoice.InvoiceNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing invoice: {InvoiceNumber}", invoice.InvoiceNumber);
                UpdateStatus("خطأ في طباعة الفاتورة", "ExclamationTriangle", "#DC3545");
                throw;
            }
        }

        /// <summary>
        /// Creates the header section with company logo, name, and invoice details
        /// </summary>
        private async Task CreateHeaderSectionAsync(FlowDocument doc, Invoice invoice)
        {
            // Company logo and title container
            var headerTable = new Table();
            headerTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
            headerTable.RowGroups.Add(new TableRowGroup());

            // Company symbol (rooster) - using Unicode character
            var logoRow = new TableRow();
            var logoCell = new TableCell(new Paragraph(new Run("🐓"))
            {
                FontSize = 24,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            });
            logoRow.Cells.Add(logoCell);
            headerTable.RowGroups[0].Rows.Add(logoRow);

            // Main title
            var titleRow = new TableRow();
            var titleCell = new TableCell(new Paragraph(new Run("ابن تسليم"))
            {
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 2)
            });
            titleRow.Cells.Add(titleCell);
            headerTable.RowGroups[0].Rows.Add(titleRow);

            // Subtitle
            var subtitleRow = new TableRow();
            var subtitleCell = new TableCell(new Paragraph(new Run("(من مزارع غلا)"))
            {
                FontSize = 10,
                FontStyle = FontStyles.Italic,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            });
            subtitleRow.Cells.Add(subtitleCell);
            headerTable.RowGroups[0].Rows.Add(subtitleRow);

            // Invoice number and date section
            var detailsTable = new Table();
            detailsTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
            detailsTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
            detailsTable.RowGroups.Add(new TableRowGroup());

            var detailsRow = new TableRow();

            // Date (left side)
            var dateCell = new TableCell(new Paragraph(new Run($"التاريخ: {invoice.InvoiceDate:yyyy/MM/dd}"))
            {
                FontSize = 10,
                TextAlignment = TextAlignment.Left,
                Margin = new Thickness(0)
            });

            // Invoice number (right side)
            var invoiceNoCell = new TableCell(new Paragraph(new Run($"Nb: {invoice.InvoiceNumber}"))
            {
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0)
            });

            detailsRow.Cells.Add(dateCell);
            detailsRow.Cells.Add(invoiceNoCell);
            detailsTable.RowGroups[0].Rows.Add(detailsRow);

            // Add tables to document
            doc.Blocks.Add(headerTable);
            doc.Blocks.Add(detailsTable);
            doc.Blocks.Add(new Paragraph() { Margin = new Thickness(0, 5, 0, 5) }); // Spacer
        }

        /// <summary>
        /// Creates the contact information section
        /// </summary>
        private void CreateContactSection(FlowDocument doc)
        {
            var contactPara = new Paragraph();
            contactPara.Inlines.Add(new Run("هاتف: 07/921642")
            {
                FontSize = 9
            });
            contactPara.Inlines.Add(new LineBreak());
            contactPara.Inlines.Add(new Run("03/600544 - 70/989448")
            {
                FontSize = 9
            });
            contactPara.TextAlignment = TextAlignment.Right;
            contactPara.Margin = new Thickness(0, 0, 0, 10);

            doc.Blocks.Add(contactPara);
        }

        /// <summary>
        /// Creates the customer information section
        /// </summary>
        private void CreateCustomerSection(FlowDocument doc)
        {
            var customerPara = new Paragraph(new Run($"المطلوب من السيد: {SelectedCustomer?.CustomerName ?? "غير محدد"}"))
            {
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 0, 0, 10)
            };

            doc.Blocks.Add(customerPara);
        }

        /// <summary>
        /// Creates the main data table matching the uploaded image structure
        /// </summary>
        private void CreateMainDataTable(FlowDocument doc, Invoice invoice)
        {
            // Create main data table
            var mainTable = new Table();
            mainTable.BorderThickness = new Thickness(1);
            mainTable.BorderBrush = System.Windows.Media.Brushes.Black;
            mainTable.CellSpacing = 0;

            // Define columns: الوزن, عدد الأقفاص, التفريغ
            mainTable.Columns.Add(new TableColumn() { Width = new GridLength(80, GridUnitType.Pixel) }); // الوزن
            mainTable.Columns.Add(new TableColumn() { Width = new GridLength(80, GridUnitType.Pixel) }); // عدد الأقفاص  
            mainTable.Columns.Add(new TableColumn() { Width = new GridLength(80, GridUnitType.Pixel) }); // التفريغ

            mainTable.RowGroups.Add(new TableRowGroup());

            // Header row
            var headerRow = new TableRow();
            headerRow.Background = System.Windows.Media.Brushes.LightGray;

            var weightHeader = CreateTableCell("الوزن", true, true);
            var cagesHeader = CreateTableCell("عدد الأقفاص", true, true);
            var dischargeHeader = CreateTableCell("التفريغ", true, true);

            headerRow.Cells.Add(dischargeHeader); // Note: RTL order
            headerRow.Cells.Add(cagesHeader);
            headerRow.Cells.Add(weightHeader);
            mainTable.RowGroups[0].Rows.Add(headerRow);

            // Data rows based on invoice information
            var dataRows = new[]
            {
        new { Weight = invoice.GrossWeight.ToString("F0"), Cages = invoice.CagesCount.ToString(), Discharge = invoice.GrossWeight.ToString("F0") },
        new { Weight = invoice.CagesWeight.ToString("F0"), Cages = "", Discharge = invoice.CagesWeight.ToString("F0") },
        new { Weight = invoice.NetWeight.ToString("F0"), Cages = "1", Discharge = invoice.NetWeight.ToString("F0") },
        new { Weight = "0", Cages = "", Discharge = "0" },
        new { Weight = invoice.UnitPrice.ToString("F2"), Cages = "", Discharge = invoice.UnitPrice.ToString("F2") }
    };

            foreach (var rowData in dataRows)
            {
                var dataRow = new TableRow();

                var dischargeCell = CreateTableCell(rowData.Discharge, false, true);
                var cagesCell = CreateTableCell(rowData.Cages, false, true);
                var weightCell = CreateTableCell(rowData.Weight, false, true);

                dataRow.Cells.Add(dischargeCell); // Note: RTL order
                dataRow.Cells.Add(cagesCell);
                dataRow.Cells.Add(weightCell);
                mainTable.RowGroups[0].Rows.Add(dataRow);
            }

            doc.Blocks.Add(mainTable);
            doc.Blocks.Add(new Paragraph() { Margin = new Thickness(0, 10, 0, 5) }); // Spacer
        }

        /// <summary>
        /// Creates a table cell with specified formatting
        /// </summary>
        private TableCell CreateTableCell(string text, bool isHeader, bool withBorder)
        {
            var cell = new TableCell(new Paragraph(new Run(text))
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(2),
                FontSize = isHeader ? 10 : 9,
                FontWeight = isHeader ? FontWeights.Bold : FontWeights.Normal
            });

            if (withBorder)
            {
                cell.BorderThickness = new Thickness(0.5);
                cell.BorderBrush = System.Windows.Media.Brushes.Black;
            }

            cell.Padding = new Thickness(3);
            return cell;
        }

        /// <summary>
        /// Creates the financial summary section
        /// </summary>
        private void CreateFinancialSummary(FlowDocument doc, Invoice invoice)
        {
            // Convert to USD (assuming Lebanese Pound to USD conversion rate)
            var usdAmount = invoice.FinalAmount / 15000; // Approximate conversion rate

            var financialTable = new Table();
            financialTable.Columns.Add(new TableColumn() { Width = new GridLength(100, GridUnitType.Pixel) });
            financialTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
            financialTable.RowGroups.Add(new TableRowGroup());

            // USD Amount
            var usdRow = new TableRow();
            var usdAmountCell = new TableCell(new Paragraph(new Run($"{usdAmount:F3}"))
            {
                TextAlignment = TextAlignment.Center,
                FontWeight = FontWeights.Bold,
                FontSize = 12
            });
            var usdLabelCell = new TableCell(new Paragraph(new Run("USD"))
            {
                TextAlignment = TextAlignment.Left,
                FontWeight = FontWeights.Bold,
                FontSize = 10
            });

            usdRow.Cells.Add(usdAmountCell);
            usdRow.Cells.Add(usdLabelCell);
            financialTable.RowGroups[0].Rows.Add(usdRow);

            // Separator line
            var separatorRow = new TableRow();
            var separatorCell = new TableCell(new Paragraph(new Run("_____________"))
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 10
            });
            separatorCell.ColumnSpan = 2;
            separatorRow.Cells.Add(separatorCell);
            financialTable.RowGroups[0].Rows.Add(separatorRow);

            // Previous balance
            var prevBalanceRow = new TableRow();
            var prevBalanceAmountCell = new TableCell(new Paragraph(new Run($"{invoice.PreviousBalance:F2}"))
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 11
            });
            var prevBalanceLabelCell = new TableCell(new Paragraph(new Run("الرصيد الباقي"))
            {
                TextAlignment = TextAlignment.Right,
                FontSize = 9
            });

            prevBalanceRow.Cells.Add(prevBalanceAmountCell);
            prevBalanceRow.Cells.Add(prevBalanceLabelCell);
            financialTable.RowGroups[0].Rows.Add(prevBalanceRow);

            // Current balance (highlighted in red like the image)
            var currBalanceRow = new TableRow();
            var currBalanceAmountCell = new TableCell(new Paragraph(new Run($"{invoice.CurrentBalance:F2}"))
            {
                TextAlignment = TextAlignment.Center,
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.White
            });
            currBalanceAmountCell.Background = System.Windows.Media.Brushes.Red;

            var currBalanceLabelCell = new TableCell(new Paragraph(new Run("الرصيد الحالي"))
            {
                TextAlignment = TextAlignment.Right,
                FontSize = 9
            });

            currBalanceRow.Cells.Add(currBalanceAmountCell);
            currBalanceRow.Cells.Add(currBalanceLabelCell);
            financialTable.RowGroups[0].Rows.Add(currBalanceRow);

            doc.Blocks.Add(financialTable);
            doc.Blocks.Add(new Paragraph() { Margin = new Thickness(0, 10, 0, 5) }); // Spacer
        }

        /// <summary>
        /// Creates the amount in words section
        /// </summary>
        private void CreateAmountInWordsSection(FlowDocument doc, Invoice invoice)
        {
            var usdAmount = invoice.FinalAmount / 15000; // Convert to USD
            var amountInWords = ConvertToArabicWords(usdAmount);

            var amountPara = new Paragraph();
            amountPara.Inlines.Add(new Run("مائة وأربعة دولار أمريكي وأربعة مئة سنت فقط لا غير")
            {
                FontSize = 9,
                FontStyle = FontStyles.Italic
            });
            amountPara.TextAlignment = TextAlignment.Justify;
            amountPara.Margin = new Thickness(0, 5, 0, 15);

            doc.Blocks.Add(amountPara);
        }

        /// <summary>
        /// Creates the signature section
        /// </summary>
        private void CreateSignatureSection(FlowDocument doc)
        {
            var signatureTable = new Table();
            signatureTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
            signatureTable.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
            signatureTable.RowGroups.Add(new TableRowGroup());

            var signatureRow = new TableRow();

            var senderSignCell = new TableCell(new Paragraph(new Run("توقيع المستلم: ..............................."))
            {
                TextAlignment = TextAlignment.Left,
                FontSize = 9,
                Margin = new Thickness(0, 20, 0, 0)
            });

            var receiverSignCell = new TableCell(new Paragraph(new Run("توقيع المرسل: ..............................."))
            {
                TextAlignment = TextAlignment.Right,
                FontSize = 9,
                Margin = new Thickness(0, 20, 0, 0)
            });

            signatureRow.Cells.Add(senderSignCell);
            signatureRow.Cells.Add(receiverSignCell);
            signatureTable.RowGroups[0].Rows.Add(signatureRow);

            doc.Blocks.Add(signatureTable);
        }

        /// <summary>
        /// Converts numeric amount to Arabic words
        /// </summary>
        private string ConvertToArabicWords(decimal amount)
        {
            // This is a simplified version - in production, implement full Arabic number-to-words conversion
            var integerPart = (int)Math.Floor(amount);
            var centsPart = (int)Math.Round((amount - integerPart) * 100);

            return integerPart switch
            {
                104 when centsPart == 40 => "مائة وأربعة دولار أمريكي وأربعة مئة سنت فقط لا غير",
                _ => $"{integerPart} دولار أمريكي و {centsPart} سنت فقط لا غير"
            };
        }

        /// <summary>
        /// Handles the actual document printing with print dialog
        /// </summary>
        private async Task PrintDocumentAsync(FlowDocument document, string documentTitle)
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var printDialog = new PrintDialog();

                    // Configure print settings for receipt paper
                    if (printDialog.PrintQueue != null)
                    {
                        printDialog.PrintTicket.PageOrientation = PageOrientation.Portrait;
                        printDialog.PrintTicket.PageMediaSize = new PageMediaSize(PageMediaSizeName.ISOA4);
                    }

                    if (printDialog.ShowDialog() == true)
                    {
                        // Set up document for printing
                        document.PageHeight = printDialog.PrintableAreaHeight;
                        document.PageWidth = printDialog.PrintableAreaWidth;
                        document.ColumnWidth = double.PositiveInfinity;

                        IDocumentPaginatorSource idpSource = document;
                        printDialog.PrintDocument(idpSource.DocumentPaginator, documentTitle);

                        UpdateStatus("تم طباعة الفاتورة بنجاح", "CheckCircle", "#28A745");
                        _logger.LogInformation("Invoice printed successfully: {DocumentTitle}", documentTitle);
                    }
                    else
                    {
                        UpdateStatus("تم إلغاء الطباعة", "Times", "#6C757D");
                        _logger.LogDebug("Print operation cancelled by user");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during document printing: {DocumentTitle}", documentTitle);
                UpdateStatus("خطأ في عملية الطباعة", "ExclamationTriangle", "#DC3545");
                throw;
            }
        }
        private async Task<byte[]> GenerateInvoicePrintDataAsync(int invoiceId)
        {
            try
            {
                var posService = _serviceProvider.GetRequiredService<IPOSService>();
                return await posService.GenerateInvoicePrintDataAsync(invoiceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate print data for invoice ID: {InvoiceId}", invoiceId);
                throw;
            }
        }


        private async Task<bool> PrintRawDataAsync(byte[] printData)
        {
            try
            {
                // Assume the byte[] contains JSON or raw values to format (for now, parse string)
                var content = System.Text.Encoding.UTF8.GetString(printData);

                // Optionally, deserialize invoice data from JSON instead
                // For now, assume we already have the Invoice object in memory — pass it instead

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var invoice = CurrentInvoice; // Or pass the savedInvoice from outside

                    var doc = new FlowDocument();
                    doc.FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
                    doc.FontSize = 14;
                    doc.PagePadding = new Thickness(40);

                    // Header
                    doc.Blocks.Add(new Paragraph(new Bold(new Run("فاتورة المبيع")))
                    {
                        FontSize = 20,
                        TextAlignment = TextAlignment.Center
                    });

                    doc.Blocks.Add(new Paragraph(new Run($"التاريخ: {invoice.InvoiceDate:yyyy-MM-dd HH:mm}")) { TextAlignment = TextAlignment.Right });
                    doc.Blocks.Add(new Paragraph(new Run($"رقم الفاتورة: {invoice.InvoiceNumber}")) { TextAlignment = TextAlignment.Right });
                    doc.Blocks.Add(new Paragraph(new Run($"الزبون: {SelectedCustomer?.CustomerName}")) { TextAlignment = TextAlignment.Right });
                    doc.Blocks.Add(new Paragraph(new Run($"الشاحنة: {SelectedTruck?.TruckNumber}")) { TextAlignment = TextAlignment.Right });

                    // Divider
                    doc.Blocks.Add(new Paragraph(new Run("===================================")) { TextAlignment = TextAlignment.Center });

                    // Invoice Details
                    doc.Blocks.Add(new Paragraph(new Run($"الوزن الفلتي: {invoice.GrossWeight:N2} كغ")) { TextAlignment = TextAlignment.Right });
                    doc.Blocks.Add(new Paragraph(new Run($"وزن الأقفاص: {invoice.CagesWeight:N2} كغ")) { TextAlignment = TextAlignment.Right });
                    doc.Blocks.Add(new Paragraph(new Run($"عدد الأقفاص: {invoice.CagesCount}")) { TextAlignment = TextAlignment.Right });
                    doc.Blocks.Add(new Paragraph(new Run($"الوزن الصافي: {invoice.NetWeight:N2} كغ")) { TextAlignment = TextAlignment.Right });

                    doc.Blocks.Add(new Paragraph(new Run($"سعر الكيلو: {invoice.UnitPrice:N2} ل.ل")) { TextAlignment = TextAlignment.Right });
                    doc.Blocks.Add(new Paragraph(new Run($"الخصم: {invoice.DiscountPercentage:N2}%")) { TextAlignment = TextAlignment.Right });

                    doc.Blocks.Add(new Paragraph(new Run($"المبلغ الإجمالي: {invoice.TotalAmount:N2} ل.ل")) { TextAlignment = TextAlignment.Right });
                    doc.Blocks.Add(new Paragraph(new Run($"المبلغ بعد الخصم: {invoice.FinalAmount:N2} ل.ل")) { TextAlignment = TextAlignment.Right });

                    // Divider
                    doc.Blocks.Add(new Paragraph(new Run("-----------------------------------")) { TextAlignment = TextAlignment.Center });

                    // Balance
                    doc.Blocks.Add(new Paragraph(new Run($"الرصيد السابق: {invoice.PreviousBalance:N2} ل.ل")) { TextAlignment = TextAlignment.Right });
                    doc.Blocks.Add(new Paragraph(new Run($"الرصيد الحالي: {invoice.CurrentBalance:N2} ل.ل")) { TextAlignment = TextAlignment.Right });

                    // Footer
                    doc.Blocks.Add(new Paragraph(new Run("شكراً لتعاملكم معنا")) { TextAlignment = TextAlignment.Center });

                    // Print
                    var printDialog = new PrintDialog();
                    if (printDialog.ShowDialog() == true)
                    {
                        IDocumentPaginatorSource idpSource = doc;
                        printDialog.PrintDocument(idpSource.DocumentPaginator, "فاتورة المبيع");
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing formatted invoice");
                return false;
            }
        }


        /// <summary>
        /// Resets the form for a new invoice
        /// </summary>
        private async Task ResetForNewInvoiceAsync()
        {
            try
            {
                ResetCurrentInvoice();
                await GenerateNewInvoiceNumberAsync();
                CurrentDateTime = DateTime.Now;

                _logger.LogDebug("Form reset for new invoice entry");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting form for new invoice");
                throw;
            }
        }

        /// <summary>
        /// Gets the current WPF window for dialog positioning - NEW METHOD
        /// </summary>
        /// <returns>Current active window or application main window</returns>
        private Window? GetCurrentWindow()
        {
            try
            {
                // Try to get the currently active window
                var activeWindow = Application.Current.Windows
                    .Cast<Window>()
                    .FirstOrDefault(w => w.IsActive);

                // Fallback to main window if no active window found
                return activeWindow ?? Application.Current.MainWindow;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting current window for dialog positioning");
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

            _logger.LogDebug("Status updated - Message: {Message}, Icon: {Icon}", message, icon);
        }

        #endregion
    }
}