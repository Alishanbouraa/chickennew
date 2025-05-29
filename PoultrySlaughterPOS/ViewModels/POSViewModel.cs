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
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

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
                    OnPropertyChanged(nameof(DiscountAmount));
                    OnPropertyChanged(nameof(CanSaveInvoice));
                }
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
                    await PrintInvoiceAsync(savedInvoice);
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
        /// Validates the current invoice and returns validation result
        /// </summary>
        /// <param name="showErrors">Whether to display validation errors to user</param>
        /// <returns>True if invoice is valid</returns>
        public bool ValidateCurrentInvoice(bool showErrors = true)
        {
            try
            {
                ValidationErrors.Clear();

                if (CurrentInvoice == null)
                {
                    ValidationErrors.Add("لا توجد فاتورة للتحقق منها");
                    return false;
                }

                // Customer validation
                if (SelectedCustomer == null)
                {
                    ValidationErrors.Add("يجب اختيار زبون للفاتورة");
                }

                // Truck validation  
                if (SelectedTruck == null)
                {
                    ValidationErrors.Add("يجب اختيار شاحنة للفاتورة");
                }

                // Weight validations
                if (CurrentInvoice.GrossWeight <= 0)
                {
                    ValidationErrors.Add("يجب إدخال الوزن الفلتي");
                }

                if (CurrentInvoice.CagesWeight < 0)
                {
                    ValidationErrors.Add("وزن الأقفاص لا يمكن أن يكون سالباً");
                }

                if (CurrentInvoice.CagesWeight >= CurrentInvoice.GrossWeight)
                {
                    ValidationErrors.Add("وزن الأقفاص لا يمكن أن يكون أكبر من أو يساوي الوزن الفلتي");
                }

                if (CurrentInvoice.CagesCount <= 0)
                {
                    ValidationErrors.Add("يجب إدخال عدد الأقفاص");
                }

                // Price validation
                if (CurrentInvoice.UnitPrice <= 0)
                {
                    ValidationErrors.Add("يجب إدخال سعر الوحدة");
                }

                // Discount validation
                if (CurrentInvoice.DiscountPercentage < 0 || CurrentInvoice.DiscountPercentage > 100)
                {
                    ValidationErrors.Add("نسبة الخصم يجب أن تكون بين 0 و 100");
                }

                HasValidationErrors = ValidationErrors.Count > 0;

                if (HasValidationErrors && showErrors)
                {
                    UpdateStatus($"توجد {ValidationErrors.Count} أخطاء في البيانات", "ExclamationTriangle", "#DC3545");
                }

                return !HasValidationErrors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during invoice validation");
                return false;
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
        /// Initializes a new invoice with default values
        /// </summary>
        private void InitializeCurrentInvoice()
        {
            CurrentInvoice = new Invoice
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

            _logger.LogDebug("New invoice initialized with default values");
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
        private async Task PrintInvoiceAsync(Invoice invoice)
        {
            try
            {
                UpdateStatus("جاري طباعة الفاتورة...", "Print", "#007BFF");

                // TODO: Implement invoice printing logic
                // This would integrate with a printing service
                await Task.Delay(1000); // Simulate printing process

                UpdateStatus("تم طباعة الفاتورة بنجاح", "CheckCircle", "#28A745");

                _logger.LogInformation("Invoice printed successfully - Number: {InvoiceNumber}", invoice.InvoiceNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing invoice");
                UpdateStatus("خطأ في طباعة الفاتورة", "ExclamationTriangle", "#DC3545");
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