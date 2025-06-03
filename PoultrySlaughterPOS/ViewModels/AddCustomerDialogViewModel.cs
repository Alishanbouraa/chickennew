using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Models;
using PoultrySlaughterPOS.Services.Repositories;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace PoultrySlaughterPOS.ViewModels
{
    /// <summary>
    /// BULLETPROOF: Enterprise-grade Add/Edit Customer Dialog ViewModel with resilient validation,
    /// comprehensive error handling, and graceful degradation for database connectivity issues.
    /// </summary>
    public partial class AddCustomerDialogViewModel : ObservableObject
    {
        #region Private Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AddCustomerDialogViewModel> _logger;
        private bool _isLoading = false;
        private bool _isSaving = false;
        private string _statusMessage = string.Empty;

        // Customer data properties
        private string _customerName = string.Empty;
        private string _phoneNumber = string.Empty;
        private string _address = string.Empty;
        private bool _isActive = true;

        // Edit mode tracking
        private bool _isEditMode = false;
        private Customer? _editingCustomer = null;
        private int? _editingCustomerId = null;

        // ENHANCED: Validation state with better control
        private bool _hasValidationErrors = false;
        private bool _isValidating = false;
        private bool _databaseValidationEnabled = true; // ADDED: Flag to disable database validation if connectivity issues
        private ObservableCollection<string> _validationErrors = new();

        // Dialog result
        private Customer? _createdCustomer;
        private bool? _dialogResult;

        // ENHANCED: Cancellation and debouncing
        private CancellationTokenSource? _validationCancellationTokenSource;
        private readonly object _validationLock = new object();
        private Timer? _validationDebounceTimer;

        #endregion

        #region Constructor

        public AddCustomerDialogViewModel(
            IUnitOfWork unitOfWork,
            ILogger<AddCustomerDialogViewModel> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeViewModel();
            TestDatabaseConnectivity(); // ADDED: Test connectivity on initialization

            _logger.LogInformation("BULLETPROOF AddCustomerDialogViewModel initialized with resilient validation");
        }

        #endregion

        #region Observable Properties

        /// <summary>
        /// BULLETPROOF: Customer name with debounced validation and fallback logic
        /// </summary>
        [Required(ErrorMessage = "اسم الزبون مطلوب")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "اسم الزبون يجب أن يكون بين 2 و 100 حرف")]
        public string CustomerName
        {
            get => _customerName;
            set
            {
                if (SetProperty(ref _customerName, value))
                {
                    // FIXED: Debounced validation to prevent excessive database calls
                    DebouncedValidation(() => ValidateCustomerNameAsync());
                }
            }
        }

        /// <summary>
        /// BULLETPROOF: Phone number with debounced validation and fallback logic
        /// </summary>
        [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
        [StringLength(20, ErrorMessage = "رقم الهاتف طويل جداً")]
        public string PhoneNumber
        {
            get => _phoneNumber;
            set
            {
                if (SetProperty(ref _phoneNumber, value))
                {
                    // FIXED: Debounced validation to prevent excessive database calls
                    DebouncedValidation(() => ValidatePhoneNumberAsync());
                }
            }
        }

        /// <summary>
        /// Customer address with immediate validation
        /// </summary>
        [StringLength(200, ErrorMessage = "العنوان طويل جداً")]
        public string Address
        {
            get => _address;
            set
            {
                if (SetProperty(ref _address, value))
                {
                    ValidateAddressImmediate();
                    UpdateCanSaveCustomer();
                }
            }
        }

        /// <summary>
        /// Customer active status
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        /// <summary>
        /// Indicates if dialog is in edit mode
        /// </summary>
        public bool IsEditMode
        {
            get => _isEditMode;
            private set => SetProperty(ref _isEditMode, value);
        }

        /// <summary>
        /// ENHANCED: Validation in progress indicator
        /// </summary>
        public bool IsValidating
        {
            get => _isValidating;
            set
            {
                if (SetProperty(ref _isValidating, value))
                {
                    UpdateCanSaveCustomer();
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
        /// Saving state indicator
        /// </summary>
        public bool IsSaving
        {
            get => _isSaving;
            set
            {
                if (SetProperty(ref _isSaving, value))
                {
                    UpdateCanSaveCustomer();
                }
            }
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
        /// Validation errors indicator
        /// </summary>
        public bool HasValidationErrors
        {
            get => _hasValidationErrors;
            set
            {
                if (SetProperty(ref _hasValidationErrors, value))
                {
                    UpdateCanSaveCustomer();
                }
            }
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
        /// Created or updated customer result
        /// </summary>
        public Customer? CreatedCustomer
        {
            get => _createdCustomer;
            private set => SetProperty(ref _createdCustomer, value);
        }

        /// <summary>
        /// Dialog result
        /// </summary>
        public bool? DialogResult
        {
            get => _dialogResult;
            private set => SetProperty(ref _dialogResult, value);
        }

        /// <summary>
        /// BULLETPROOF: More robust CanSaveCustomer logic with fallback validation
        /// </summary>
        public bool CanSaveCustomer =>
            !string.IsNullOrWhiteSpace(CustomerName) &&
            CustomerName.Trim().Length >= 2 &&
            CustomerName.Trim().Length <= 100 &&
            (string.IsNullOrWhiteSpace(PhoneNumber) || IsValidPhoneNumberFormat(PhoneNumber)) &&
            (string.IsNullOrWhiteSpace(Address) || Address.Trim().Length <= 200) &&
            !HasValidationErrors &&
            !IsSaving &&
            !IsValidating &&
            !IsLoading;

        /// <summary>
        /// Dialog title based on mode
        /// </summary>
        public string DialogTitle => IsEditMode ? "تعديل بيانات الزبون" : "إضافة زبون جديد";

        /// <summary>
        /// Save button text based on mode
        /// </summary>
        public string SaveButtonText => IsEditMode ? "تحديث" : "حفظ";

        #endregion

        #region Commands

        /// <summary>
        /// BULLETPROOF: Command to save customer with comprehensive validation
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSaveCustomer))]
        private async Task SaveCustomerAsync()
        {
            try
            {
                IsSaving = true;
                StatusMessage = IsEditMode ? "جاري تحديث بيانات الزبون..." : "جاري حفظ بيانات الزبون...";

                _logger.LogInformation("{Action} customer: {CustomerName}", IsEditMode ? "Updating" : "Creating", CustomerName);

                // BULLETPROOF: Final validation with fallback to basic validation if database unavailable
                if (!await ValidateAllFieldsAsync())
                {
                    StatusMessage = "يرجى تصحيح الأخطاء المذكورة أعلاه";
                    return;
                }

                if (IsEditMode && _editingCustomer != null)
                {
                    await UpdateExistingCustomerAsync();
                }
                else
                {
                    await CreateNewCustomerAsync();
                }

                DialogResult = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error {Action} customer {CustomerName}", IsEditMode ? "updating" : "creating", CustomerName);
                StatusMessage = $"خطأ في {(IsEditMode ? "تحديث" : "حفظ")} الزبون: {ex.Message}";
                DialogResult = null;
            }
            finally
            {
                IsSaving = false;
            }
        }

        /// <summary>
        /// Command to cancel the dialog
        /// </summary>
        [RelayCommand]
        private void CancelDialog()
        {
            _logger.LogDebug("Customer dialog cancelled by user");
            StatusMessage = "تم إلغاء العملية";
            CreatedCustomer = null;
            DialogResult = false;
        }

        /// <summary>
        /// Command to clear all fields
        /// </summary>
        [RelayCommand]
        private void ClearFields()
        {
            // Cancel any ongoing validation
            _validationCancellationTokenSource?.Cancel();
            _validationDebounceTimer?.Dispose();

            CustomerName = string.Empty;
            PhoneNumber = string.Empty;
            Address = string.Empty;
            IsActive = true;
            ValidationErrors.Clear();
            HasValidationErrors = false;
            StatusMessage = string.Empty;

            _logger.LogDebug("Customer dialog fields cleared");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads customer data for editing
        /// </summary>
        public void LoadCustomerForEdit(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            _editingCustomer = customer;
            _editingCustomerId = customer.CustomerId;
            IsEditMode = true;

            // Populate form with customer data
            CustomerName = customer.CustomerName;
            PhoneNumber = customer.PhoneNumber ?? string.Empty;
            Address = customer.Address ?? string.Empty;
            IsActive = customer.IsActive;

            // Clear validation errors
            ValidationErrors.Clear();
            HasValidationErrors = false;
            StatusMessage = "جاهز لتعديل بيانات الزبون";

            _logger.LogDebug("Customer loaded for editing: {CustomerId} - {CustomerName}", customer.CustomerId, customer.CustomerName);
        }

        /// <summary>
        /// Configures dialog for new customer creation
        /// </summary>
        public void ConfigureForNewCustomer()
        {
            _editingCustomer = null;
            _editingCustomerId = null;
            IsEditMode = false;

            ResetDialog();
            StatusMessage = "أدخل بيانات الزبون الجديد";

            _logger.LogDebug("Dialog configured for new customer creation");
        }

        #endregion

        #region BULLETPROOF Validation Methods

        /// <summary>
        /// BULLETPROOF: Tests database connectivity and adjusts validation strategy
        /// </summary>
        private async void TestDatabaseConnectivity()
        {
            try
            {
                // Simple connectivity test
                await _unitOfWork.Customers.GetActiveCustomerCountAsync();
                _databaseValidationEnabled = true;
                _logger.LogDebug("Database connectivity confirmed - full validation enabled");
            }
            catch (Exception ex)
            {
                _databaseValidationEnabled = false;
                _logger.LogWarning(ex, "Database connectivity issues detected - using basic validation only");
                StatusMessage = "تحذير: سيتم استخدام التحقق الأساسي فقط بسبب مشاكل الاتصال";
            }
        }

        /// <summary>
        /// BULLETPROOF: Debounced validation to prevent excessive calls
        /// </summary>
        private void DebouncedValidation(Func<Task> validationAction)
        {
            // Cancel existing timer
            _validationDebounceTimer?.Dispose();

            // Create new timer with 500ms delay
            _validationDebounceTimer = new Timer(async _ =>
            {
                try
                {
                    await validationAction();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error in debounced validation");
                }
                finally
                {
                    // Ensure UI updates on main thread
                    Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                    {
                        UpdateCanSaveCustomer();
                    }));
                }
            }, null, 500, Timeout.Infinite);
        }

        /// <summary>
        /// BULLETPROOF: Customer name validation with fallback logic
        /// </summary>
        private async Task ValidateCustomerNameAsync()
        {
            lock (_validationLock)
            {
                _validationCancellationTokenSource?.Cancel();
                _validationCancellationTokenSource = new CancellationTokenSource();
            }

            var cancellationToken = _validationCancellationTokenSource.Token;

            try
            {
                IsValidating = true;
                ClearValidationError("اسم الزبون");

                // Basic validation first (always works)
                if (string.IsNullOrWhiteSpace(CustomerName))
                {
                    AddValidationError("اسم الزبون مطلوب");
                    return;
                }

                var trimmedName = CustomerName.Trim();
                if (trimmedName.Length < 2)
                {
                    AddValidationError("اسم الزبون قصير جداً");
                    return;
                }

                if (trimmedName.Length > 100)
                {
                    AddValidationError("اسم الزبون طويل جداً");
                    return;
                }

                // BULLETPROOF: Database validation with fallback
                if (_databaseValidationEnabled)
                {
                    try
                    {
                        var existingCustomer = await _unitOfWork.Customers.GetCustomerByNameAsync(trimmedName, cancellationToken);
                        if (existingCustomer != null && existingCustomer.CustomerId != _editingCustomerId)
                        {
                            AddValidationError("اسم الزبون موجود مسبقاً");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Validation was cancelled, ignore
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Database validation failed for customer name - continuing with basic validation");
                        _databaseValidationEnabled = false; // Disable for subsequent validations

                        // Update status to inform user
                        Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
                        {
                            StatusMessage = "تحذير: تم تعطيل التحقق من قاعدة البيانات مؤقتاً";
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in customer name validation");
                AddValidationError("خطأ في التحقق من اسم الزبون");
            }
            finally
            {
                IsValidating = false;
            }
        }

        /// <summary>
        /// BULLETPROOF: Phone number validation with fallback logic
        /// </summary>
        private async Task ValidatePhoneNumberAsync()
        {
            try
            {
                IsValidating = true;
                ClearValidationError("رقم الهاتف");

                if (string.IsNullOrWhiteSpace(PhoneNumber))
                {
                    return; // Phone number is optional
                }

                var trimmedPhone = PhoneNumber.Trim();

                // Basic validation first
                if (trimmedPhone.Length > 20)
                {
                    AddValidationError("رقم الهاتف طويل جداً");
                    return;
                }

                if (!IsValidPhoneNumberFormat(trimmedPhone))
                {
                    AddValidationError("رقم الهاتف غير صحيح");
                    return;
                }

                // BULLETPROOF: Database validation with fallback
                if (_databaseValidationEnabled)
                {
                    try
                    {
                        var existingCustomer = await _unitOfWork.Customers.GetCustomerByPhoneAsync(trimmedPhone);
                        if (existingCustomer != null && existingCustomer.CustomerId != _editingCustomerId)
                        {
                            AddValidationError("رقم الهاتف مستخدم مسبقاً");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Database validation failed for phone number - continuing with basic validation");
                        _databaseValidationEnabled = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in phone number validation");
                AddValidationError("خطأ في التحقق من رقم الهاتف");
            }
            finally
            {
                IsValidating = false;
            }
        }

        /// <summary>
        /// BULLETPROOF: Immediate address validation (no database required)
        /// </summary>
        private void ValidateAddressImmediate()
        {
            ClearValidationError("العنوان");

            if (!string.IsNullOrWhiteSpace(Address) && Address.Trim().Length > 200)
            {
                AddValidationError("العنوان طويل جداً");
            }
        }

        /// <summary>
        /// BULLETPROOF: Comprehensive validation with timeout protection
        /// </summary>
        private async Task<bool> ValidateAllFieldsAsync()
        {
            try
            {
                ValidationErrors.Clear();
                HasValidationErrors = false;

                // Run all validations with timeout protection
                var validationTasks = new List<Task>
                {
                    ValidateCustomerNameAsync(),
                    ValidatePhoneNumberAsync()
                };

                // Immediate validation
                ValidateAddressImmediate();

                // Wait for async validations with timeout
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                try
                {
                    await Task.WhenAll(validationTasks).WaitAsync(timeoutCts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Validation timeout - proceeding with basic validation only");
                    _databaseValidationEnabled = false;
                }

                // Wait for any remaining validation to complete
                var waitCount = 0;
                while (IsValidating && waitCount < 50) // Max 5 seconds
                {
                    await Task.Delay(100);
                    waitCount++;
                }

                return !HasValidationErrors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in comprehensive validation");
                return false;
            }
        }

        /// <summary>
        /// BULLETPROOF: Enhanced phone number format validation
        /// </summary>
        private bool IsValidPhoneNumberFormat(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            var cleanNumber = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

            if (cleanNumber.StartsWith("+"))
            {
                cleanNumber = cleanNumber.Substring(1);
            }

            return cleanNumber.Length >= 7 && cleanNumber.Length <= 15 && cleanNumber.All(char.IsDigit);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Updates existing customer with comprehensive error handling
        /// </summary>
        private async Task UpdateExistingCustomerAsync()
        {
            _editingCustomer!.CustomerName = CustomerName.Trim();
            _editingCustomer.PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim();
            _editingCustomer.Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim();
            _editingCustomer.IsActive = IsActive;
            _editingCustomer.UpdatedDate = DateTime.Now;

            await _unitOfWork.Customers.UpdateAsync(_editingCustomer);
            await _unitOfWork.SaveChangesAsync("POS_USER");

            CreatedCustomer = _editingCustomer;
            StatusMessage = "تم تحديث بيانات الزبون بنجاح";

            _logger.LogInformation("Customer updated successfully - ID: {CustomerId}, Name: {CustomerName}",
                _editingCustomer.CustomerId, _editingCustomer.CustomerName);
        }

        /// <summary>
        /// Creates new customer with comprehensive error handling
        /// </summary>
        private async Task CreateNewCustomerAsync()
        {
            var customer = new Customer
            {
                CustomerName = CustomerName.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim(),
                Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim(),
                IsActive = IsActive,
                TotalDebt = 0,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            CreatedCustomer = await _unitOfWork.Customers.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync("POS_USER");

            StatusMessage = "تم حفظ الزبون بنجاح";

            _logger.LogInformation("Customer created successfully - ID: {CustomerId}, Name: {CustomerName}",
                CreatedCustomer.CustomerId, CreatedCustomer.CustomerName);
        }

        /// <summary>
        /// Initializes the ViewModel with default state
        /// </summary>
        private void InitializeViewModel()
        {
            ValidationErrors = new ObservableCollection<string>();
            StatusMessage = "أدخل بيانات الزبون الجديد";
            HasValidationErrors = false;
            DialogResult = null;
            IsEditMode = false;
            IsActive = true;
            IsValidating = false;
            _databaseValidationEnabled = true;
        }

        /// <summary>
        /// BULLETPROOF: Thread-safe validation error management
        /// </summary>
        private void AddValidationError(string message)
        {
            Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
            {
                if (!ValidationErrors.Contains(message))
                {
                    ValidationErrors.Add(message);
                    HasValidationErrors = true;
                }
            }));
        }

        /// <summary>
        /// BULLETPROOF: Thread-safe validation error clearing
        /// </summary>
        private void ClearValidationError(string fieldName)
        {
            Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
            {
                var errorsToRemove = ValidationErrors.Where(e => e.Contains(fieldName)).ToList();
                foreach (var error in errorsToRemove)
                {
                    ValidationErrors.Remove(error);
                }

                HasValidationErrors = ValidationErrors.Count > 0;
            }));
        }

        /// <summary>
        /// BULLETPROOF: Centralized command state update
        /// </summary>
        private void UpdateCanSaveCustomer()
        {
            Application.Current?.Dispatcher?.BeginInvoke(new Action(() =>
            {
                OnPropertyChanged(nameof(CanSaveCustomer));
                SaveCustomerCommand.NotifyCanExecuteChanged();
            }));
        }

        /// <summary>
        /// Resets the dialog to initial state
        /// </summary>
        public void ResetDialog()
        {
            // Cancel any ongoing operations
            _validationCancellationTokenSource?.Cancel();
            _validationDebounceTimer?.Dispose();

            CustomerName = string.Empty;
            PhoneNumber = string.Empty;
            Address = string.Empty;
            IsActive = true;
            ValidationErrors.Clear();
            HasValidationErrors = false;
            StatusMessage = IsEditMode ? "جاهز لتعديل بيانات الزبون" : "أدخل بيانات الزبون الجديد";
            CreatedCustomer = null;
            DialogResult = null;
            IsLoading = false;
            IsSaving = false;
            IsValidating = false;
            _databaseValidationEnabled = true;

            _logger.LogDebug("Customer dialog reset to initial state");
        }

        #endregion

        #region IDisposable Implementation

        protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            // Update command states when relevant properties change
            if (e.PropertyName == nameof(CustomerName) ||
                e.PropertyName == nameof(HasValidationErrors) ||
                e.PropertyName == nameof(IsValidating) ||
                e.PropertyName == nameof(IsSaving))
            {
                SaveCustomerCommand?.NotifyCanExecuteChanged();
            }
        }

        public void Dispose()
        {
            _validationCancellationTokenSource?.Cancel();
            _validationCancellationTokenSource?.Dispose();
            _validationDebounceTimer?.Dispose();
        }

        #endregion
    }
}