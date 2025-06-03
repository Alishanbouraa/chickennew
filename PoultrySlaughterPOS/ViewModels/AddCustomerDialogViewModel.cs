// File: PoultrySlaughterPOS/ViewModels/AddCustomerDialogViewModel.cs

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
    /// BULLETPROOF: Fixed validation system with proper thread synchronization
    /// and enhanced error state management for reliable customer dialog operations.
    /// </summary>
    public partial class AddCustomerDialogViewModel : ObservableObject
    {
        #region Private Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AddCustomerDialogViewModel> _logger;
        private readonly Dispatcher _dispatcher;

        // Customer data properties
        private string _customerName = string.Empty;
        private string _phoneNumber = string.Empty;
        private string _address = string.Empty;
        private bool _isActive = true;

        // Edit mode tracking
        private bool _isEditMode = false;
        private Customer? _editingCustomer = null;
        private int? _editingCustomerId = null;

        // State management
        private bool _isLoading = false;
        private bool _isSaving = false;
        private bool _isValidating = false;
        private string _statusMessage = string.Empty;

        // FIXED: Enhanced validation state management
        private bool _hasValidationErrors = false;
        private bool _databaseValidationEnabled = true;
        private readonly ObservableCollection<string> _validationErrors = new();
        private readonly HashSet<string> _currentErrors = new();
        private readonly object _validationLock = new object();

        // Dialog result
        private Customer? _createdCustomer;
        private bool? _dialogResult;

        // FIXED: Improved cancellation and debouncing
        private CancellationTokenSource? _validationCancellationTokenSource;
        private Timer? _nameValidationTimer;
        private Timer? _phoneValidationTimer;

        #endregion

        #region Constructor

        public AddCustomerDialogViewModel(
            IUnitOfWork unitOfWork,
            ILogger<AddCustomerDialogViewModel> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

            InitializeViewModel();
            _ = TestDatabaseConnectivityAsync(); // Fire and forget, but logged

            _logger.LogInformation("BULLETPROOF AddCustomerDialogViewModel initialized with enhanced validation");
        }

        #endregion

        #region Observable Properties

        /// <summary>
        /// FIXED: Customer name with improved debounced validation
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
                    ClearFieldValidationErrors("اسم الزبون");
                    ScheduleNameValidation();
                }
            }
        }

        /// <summary>
        /// FIXED: Phone number with improved debounced validation
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
                    ClearFieldValidationErrors("رقم الهاتف");
                    SchedulePhoneValidation();
                }
            }
        }

        /// <summary>
        /// FIXED: Address with immediate validation
        /// </summary>
        [StringLength(200, ErrorMessage = "العنوان طويل جداً")]
        public string Address
        {
            get => _address;
            set
            {
                if (SetProperty(ref _address, value))
                {
                    ClearFieldValidationErrors("العنوان");
                    ValidateAddressImmediate();
                    UpdateCanSaveCustomer();
                }
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            private set => SetProperty(ref _isEditMode, value);
        }

        public bool IsValidating
        {
            get => _isValidating;
            private set
            {
                if (SetProperty(ref _isValidating, value))
                {
                    UpdateCanSaveCustomer();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

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

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool HasValidationErrors
        {
            get => _hasValidationErrors;
            private set
            {
                if (SetProperty(ref _hasValidationErrors, value))
                {
                    UpdateCanSaveCustomer();
                }
            }
        }

        public ObservableCollection<string> ValidationErrors => _validationErrors;

        public Customer? CreatedCustomer
        {
            get => _createdCustomer;
            private set => SetProperty(ref _createdCustomer, value);
        }

        public bool? DialogResult
        {
            get => _dialogResult;
            private set => SetProperty(ref _dialogResult, value);
        }

        /// <summary>
        /// FIXED: More robust CanSaveCustomer logic
        /// </summary>
        public bool CanSaveCustomer
        {
            get
            {
                return !string.IsNullOrWhiteSpace(CustomerName) &&
                       CustomerName.Trim().Length >= 2 &&
                       CustomerName.Trim().Length <= 100 &&
                       (string.IsNullOrWhiteSpace(PhoneNumber) || IsValidPhoneNumberFormat(PhoneNumber)) &&
                       (string.IsNullOrWhiteSpace(Address) || Address.Trim().Length <= 200) &&
                       !HasValidationErrors &&
                       !IsSaving &&
                       !IsValidating &&
                       !IsLoading;
            }
        }

        public string DialogTitle => IsEditMode ? "تعديل بيانات الزبون" : "إضافة زبون جديد";
        public string SaveButtonText => IsEditMode ? "تحديث" : "حفظ";

        #endregion

        #region Commands

        [RelayCommand(CanExecute = nameof(CanSaveCustomer))]
        private async Task SaveCustomerAsync()
        {
            try
            {
                IsSaving = true;
                StatusMessage = IsEditMode ? "جاري تحديث بيانات الزبون..." : "جاري حفظ بيانات الزبون...";

                _logger.LogInformation("{Action} customer: {CustomerName}", IsEditMode ? "Updating" : "Creating", CustomerName);

                // FIXED: Final validation with proper error handling
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

        [RelayCommand]
        private void CancelDialog()
        {
            _logger.LogDebug("Customer dialog cancelled by user");
            StatusMessage = "تم إلغاء العملية";
            CreatedCustomer = null;
            DialogResult = false;
        }

        [RelayCommand]
        private void ClearFields()
        {
            CancelAllValidation();
            CustomerName = string.Empty;
            PhoneNumber = string.Empty;
            Address = string.Empty;
            IsActive = true;
            ClearAllValidationErrors();
            StatusMessage = string.Empty;

            _logger.LogDebug("Customer dialog fields cleared");
        }

        #endregion

        #region FIXED: Validation Methods

        /// <summary>
        /// FIXED: Robust database connectivity test
        /// </summary>
        private async Task TestDatabaseConnectivityAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _unitOfWork.Customers.GetActiveCustomerCountAsync(cts.Token);
                _databaseValidationEnabled = true;
                _logger.LogDebug("Database connectivity confirmed - full validation enabled");
            }
            catch (Exception ex)
            {
                _databaseValidationEnabled = false;
                _logger.LogWarning(ex, "Database connectivity issues detected - using basic validation only");

                await _dispatcher.BeginInvoke(() =>
                {
                    StatusMessage = "تحذير: سيتم استخدام التحقق الأساسي فقط بسبب مشاكل الاتصال";
                });
            }
        }

        /// <summary>
        /// FIXED: Scheduled name validation with proper debouncing
        /// </summary>
        private void ScheduleNameValidation()
        {
            _nameValidationTimer?.Dispose();

            _nameValidationTimer = new Timer(async _ =>
            {
                try
                {
                    await ValidateCustomerNameAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error in scheduled name validation");
                }
            }, null, 750, Timeout.Infinite); // 750ms debounce
        }

        /// <summary>
        /// FIXED: Scheduled phone validation with proper debouncing
        /// </summary>
        private void SchedulePhoneValidation()
        {
            _phoneValidationTimer?.Dispose();

            _phoneValidationTimer = new Timer(async _ =>
            {
                try
                {
                    await ValidatePhoneNumberAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error in scheduled phone validation");
                }
            }, null, 750, Timeout.Infinite); // 750ms debounce
        }

        /// <summary>
        /// FIXED: Customer name validation with proper error handling
        /// </summary>
        private async Task ValidateCustomerNameAsync()
        {
            var cancellationToken = CancelCurrentValidation();

            try
            {
                await _dispatcher.BeginInvoke(() => IsValidating = true);

                // Basic validation first
                var trimmedName = CustomerName?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(trimmedName))
                {
                    AddValidationError("اسم الزبون مطلوب");
                    return;
                }

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

                // Database validation if enabled
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
                        return; // Validation was cancelled
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Database validation failed for customer name - continuing with basic validation");
                        _databaseValidationEnabled = false;

                        await _dispatcher.BeginInvoke(() =>
                        {
                            StatusMessage = "تحذير: تم تعطيل التحقق من قاعدة البيانات مؤقتاً";
                        });
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
                await _dispatcher.BeginInvoke(() =>
                {
                    IsValidating = false;
                    UpdateCanSaveCustomer();
                });
            }
        }

        /// <summary>
        /// FIXED: Phone number validation with proper error handling
        /// </summary>
        private async Task ValidatePhoneNumberAsync()
        {
            var cancellationToken = CancelCurrentValidation();

            try
            {
                await _dispatcher.BeginInvoke(() => IsValidating = true);

                var trimmedPhone = PhoneNumber?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(trimmedPhone))
                {
                    return; // Phone is optional
                }

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

                // Database validation if enabled
                if (_databaseValidationEnabled)
                {
                    try
                    {
                        var existingCustomer = await _unitOfWork.Customers.GetCustomerByPhoneAsync(trimmedPhone, cancellationToken);
                        if (existingCustomer != null && existingCustomer.CustomerId != _editingCustomerId)
                        {
                            AddValidationError("رقم الهاتف مستخدم مسبقاً");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        return; // Validation was cancelled
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
                await _dispatcher.BeginInvoke(() =>
                {
                    IsValidating = false;
                    UpdateCanSaveCustomer();
                });
            }
        }

        /// <summary>
        /// FIXED: Immediate address validation
        /// </summary>
        private void ValidateAddressImmediate()
        {
            if (!string.IsNullOrWhiteSpace(Address) && Address.Trim().Length > 200)
            {
                AddValidationError("العنوان طويل جداً");
            }
        }

        /// <summary>
        /// FIXED: Comprehensive validation with timeout protection
        /// </summary>
        private async Task<bool> ValidateAllFieldsAsync()
        {
            try
            {
                ClearAllValidationErrors();

                // Run immediate validations
                ValidateAddressImmediate();

                // Wait for any pending async validations to complete
                await Task.Delay(100); // Small delay to allow debounced validations to start

                // Wait for validation to complete with timeout
                var timeout = DateTime.Now.AddSeconds(5);
                while (IsValidating && DateTime.Now < timeout)
                {
                    await Task.Delay(50);
                }

                // Force final validation if still pending
                if (!string.IsNullOrWhiteSpace(CustomerName))
                {
                    await ValidateCustomerNameAsync();
                }

                if (!string.IsNullOrWhiteSpace(PhoneNumber))
                {
                    await ValidatePhoneNumberAsync();
                }

                return !HasValidationErrors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in comprehensive validation");
                return false;
            }
        }

        #endregion

        #region FIXED: Error Management Methods

        /// <summary>
        /// FIXED: Thread-safe validation error addition
        /// </summary>
        private void AddValidationError(string message)
        {
            _dispatcher.BeginInvoke(() =>
            {
                lock (_validationLock)
                {
                    if (!_currentErrors.Contains(message))
                    {
                        _currentErrors.Add(message);
                        ValidationErrors.Add(message);
                        HasValidationErrors = true;
                    }
                }
            });
        }

        /// <summary>
        /// FIXED: Clear validation errors for specific field
        /// </summary>
        private void ClearFieldValidationErrors(string fieldName)
        {
            _dispatcher.BeginInvoke(() =>
            {
                lock (_validationLock)
                {
                    var errorsToRemove = _currentErrors.Where(e => e.Contains(fieldName)).ToList();
                    foreach (var error in errorsToRemove)
                    {
                        _currentErrors.Remove(error);
                        ValidationErrors.Remove(error);
                    }
                    HasValidationErrors = _currentErrors.Count > 0;
                }
            });
        }

        /// <summary>
        /// FIXED: Clear all validation errors
        /// </summary>
        private void ClearAllValidationErrors()
        {
            _dispatcher.BeginInvoke(() =>
            {
                lock (_validationLock)
                {
                    _currentErrors.Clear();
                    ValidationErrors.Clear();
                    HasValidationErrors = false;
                }
            });
        }

        /// <summary>
        /// FIXED: Cancel current validation and return new token
        /// </summary>
        private CancellationToken CancelCurrentValidation()
        {
            _validationCancellationTokenSource?.Cancel();
            _validationCancellationTokenSource = new CancellationTokenSource();
            return _validationCancellationTokenSource.Token;
        }

        /// <summary>
        /// FIXED: Cancel all validation operations
        /// </summary>
        private void CancelAllValidation()
        {
            _validationCancellationTokenSource?.Cancel();
            _nameValidationTimer?.Dispose();
            _phoneValidationTimer?.Dispose();
        }

        #endregion

        #region Helper Methods

        private bool IsValidPhoneNumberFormat(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            var cleanNumber = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
            if (cleanNumber.StartsWith("+"))
                cleanNumber = cleanNumber.Substring(1);

            return cleanNumber.Length >= 7 && cleanNumber.Length <= 15 && cleanNumber.All(char.IsDigit);
        }

        private void UpdateCanSaveCustomer()
        {
            _dispatcher.BeginInvoke(() =>
            {
                OnPropertyChanged(nameof(CanSaveCustomer));
                SaveCustomerCommand.NotifyCanExecuteChanged();
            });
        }

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

        private void InitializeViewModel()
        {
            StatusMessage = "أدخل بيانات الزبون الجديد";
            HasValidationErrors = false;
            DialogResult = null;
            IsEditMode = false;
            IsActive = true;
            IsValidating = false;
            _databaseValidationEnabled = true;
        }

        public void LoadCustomerForEdit(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            _editingCustomer = customer;
            _editingCustomerId = customer.CustomerId;
            IsEditMode = true;

            CustomerName = customer.CustomerName;
            PhoneNumber = customer.PhoneNumber ?? string.Empty;
            Address = customer.Address ?? string.Empty;
            IsActive = customer.IsActive;

            ClearAllValidationErrors();
            StatusMessage = "جاهز لتعديل بيانات الزبون";

            _logger.LogDebug("Customer loaded for editing: {CustomerId} - {CustomerName}", customer.CustomerId, customer.CustomerName);
        }

        public void ConfigureForNewCustomer()
        {
            _editingCustomer = null;
            _editingCustomerId = null;
            IsEditMode = false;
            ResetDialog();
            StatusMessage = "أدخل بيانات الزبون الجديد";

            _logger.LogDebug("Dialog configured for new customer creation");
        }

        public void ResetDialog()
        {
            CancelAllValidation();
            CustomerName = string.Empty;
            PhoneNumber = string.Empty;
            Address = string.Empty;
            IsActive = true;
            ClearAllValidationErrors();
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
            CancelAllValidation();
            _validationCancellationTokenSource?.Dispose();
        }

        #endregion
    }
}