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
using System.Threading.Tasks;

namespace PoultrySlaughterPOS.ViewModels
{
    /// <summary>
    /// Enterprise-grade Add/Edit Customer Dialog ViewModel implementing comprehensive customer creation and editing
    /// with real-time validation, duplicate checking, and business rule enforcement.
    /// Optimized for POS workflow integration with async operations and error handling.
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
        private bool _isActive = true; // ADDED: Missing IsActive property

        // Edit mode tracking
        private bool _isEditMode = false; // ADDED: Track if in edit mode
        private Customer? _editingCustomer = null; // ADDED: Customer being edited
        private int? _editingCustomerId = null; // ADDED: ID for duplicate checking

        // Validation state
        private bool _hasValidationErrors = false;
        private ObservableCollection<string> _validationErrors = new();

        // Dialog result
        private Customer? _createdCustomer;
        private bool? _dialogResult;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes AddCustomerDialogViewModel with comprehensive dependency injection
        /// </summary>
        /// <param name="unitOfWork">Unit of work for database operations</param>
        /// <param name="logger">Logger for diagnostic and error tracking</param>
        public AddCustomerDialogViewModel(
            IUnitOfWork unitOfWork,
            ILogger<AddCustomerDialogViewModel> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeViewModel();

            _logger.LogInformation("AddCustomerDialogViewModel initialized with enterprise-grade validation and edit mode support");
        }

        #endregion

        #region Observable Properties

        /// <summary>
        /// Customer name with real-time validation
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
                    OnPropertyChanged(nameof(CanSaveCustomer));
                    _ = ValidateCustomerNameAsync();
                }
            }
        }

        /// <summary>
        /// Phone number with format validation
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
                    OnPropertyChanged(nameof(CanSaveCustomer));
                    _ = ValidatePhoneNumberAsync();
                }
            }
        }

        /// <summary>
        /// Customer address
        /// </summary>
        [StringLength(200, ErrorMessage = "العنوان طويل جداً")]
        public string Address
        {
            get => _address;
            set
            {
                if (SetProperty(ref _address, value))
                {
                    OnPropertyChanged(nameof(CanSaveCustomer));
                }
            }
        }

        /// <summary>
        /// ADDED: Customer active status
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        /// <summary>
        /// ADDED: Indicates if dialog is in edit mode
        /// </summary>
        public bool IsEditMode
        {
            get => _isEditMode;
            private set => SetProperty(ref _isEditMode, value);
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
            set => SetProperty(ref _isSaving, value);
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
        /// Created or updated customer result (null if cancelled)
        /// </summary>
        public Customer? CreatedCustomer
        {
            get => _createdCustomer;
            private set => SetProperty(ref _createdCustomer, value);
        }

        /// <summary>
        /// Dialog result (true = saved, false = cancelled, null = pending)
        /// </summary>
        public bool? DialogResult
        {
            get => _dialogResult;
            private set => SetProperty(ref _dialogResult, value);
        }

        /// <summary>
        /// Indicates whether the customer can be saved (validation passed)
        /// </summary>
        public bool CanSaveCustomer =>
            !string.IsNullOrWhiteSpace(CustomerName) &&
            !HasValidationErrors &&
            !IsSaving;

        /// <summary>
        /// ADDED: Dialog title based on mode
        /// </summary>
        public string DialogTitle => IsEditMode ? "تعديل بيانات الزبون" : "إضافة زبون جديد";

        /// <summary>
        /// ADDED: Save button text based on mode
        /// </summary>
        public string SaveButtonText => IsEditMode ? "تحديث" : "حفظ";

        #endregion

        #region Commands

        /// <summary>
        /// Command to save the customer (create or update)
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExecuteSaveCustomer))]
        private async Task SaveCustomerAsync()
        {
            try
            {
                IsSaving = true;
                StatusMessage = IsEditMode ? "جاري تحديث بيانات الزبون..." : "جاري حفظ بيانات الزبون...";

                _logger.LogInformation("{Action} customer: {CustomerName}", IsEditMode ? "Updating" : "Creating", CustomerName);

                // Final validation before saving
                if (!await ValidateAllFieldsAsync())
                {
                    StatusMessage = "يرجى تصحيح الأخطاء المذكورة أعلاه";
                    return;
                }

                if (IsEditMode && _editingCustomer != null)
                {
                    // Update existing customer
                    _editingCustomer.CustomerName = CustomerName.Trim();
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
                else
                {
                    // Create new customer
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
            CustomerName = string.Empty;
            PhoneNumber = string.Empty;
            Address = string.Empty;
            IsActive = true;
            ValidationErrors.Clear();
            HasValidationErrors = false;
            StatusMessage = string.Empty;

            _logger.LogDebug("Customer dialog fields cleared");
        }

        private bool CanExecuteSaveCustomer() => CanSaveCustomer;

        #endregion

        #region Public Methods

        /// <summary>
        /// ADDED: Loads customer data for editing
        /// </summary>
        /// <param name="customer">Customer to edit</param>
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
        /// ADDED: Configures dialog for new customer creation
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

        #region Validation Methods

        /// <summary>
        /// Validates customer name for uniqueness and format
        /// </summary>
        private async Task ValidateCustomerNameAsync()
        {
            try
            {
                ClearValidationError("اسم الزبون");

                if (string.IsNullOrWhiteSpace(CustomerName))
                {
                    AddValidationError("اسم الزبون مطلوب");
                    return;
                }

                if (CustomerName.Trim().Length < 2)
                {
                    AddValidationError("اسم الزبون قصير جداً");
                    return;
                }

                if (CustomerName.Trim().Length > 100)
                {
                    AddValidationError("اسم الزبون طويل جداً");
                    return;
                }

                // Check for duplicate name (exclude current customer in edit mode)
                var existingCustomer = await _unitOfWork.Customers.GetCustomerByNameAsync(CustomerName.Trim());
                if (existingCustomer != null && existingCustomer.CustomerId != _editingCustomerId)
                {
                    AddValidationError("اسم الزبون موجود مسبقاً");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error validating customer name");
                AddValidationError("خطأ في التحقق من اسم الزبون");
            }
        }

        /// <summary>
        /// Validates phone number for uniqueness and format
        /// </summary>
        private async Task ValidatePhoneNumberAsync()
        {
            try
            {
                ClearValidationError("رقم الهاتف");

                if (string.IsNullOrWhiteSpace(PhoneNumber))
                {
                    return; // Phone number is optional
                }

                if (PhoneNumber.Trim().Length > 20)
                {
                    AddValidationError("رقم الهاتف طويل جداً");
                    return;
                }

                // Basic phone number format validation
                if (!IsValidPhoneNumber(PhoneNumber.Trim()))
                {
                    AddValidationError("رقم الهاتف غير صحيح");
                    return;
                }

                // Check for duplicate phone number (exclude current customer in edit mode)
                var existingCustomer = await _unitOfWork.Customers.GetCustomerByPhoneAsync(PhoneNumber.Trim());
                if (existingCustomer != null && existingCustomer.CustomerId != _editingCustomerId)
                {
                    AddValidationError("رقم الهاتف مستخدم مسبقاً");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error validating phone number");
                AddValidationError("خطأ في التحقق من رقم الهاتف");
            }
        }

        /// <summary>
        /// Validates all fields comprehensively
        /// </summary>
        private async Task<bool> ValidateAllFieldsAsync()
        {
            ValidationErrors.Clear();

            await ValidateCustomerNameAsync();
            await ValidatePhoneNumberAsync();

            // Additional business rule validations
            if (!string.IsNullOrWhiteSpace(Address) && Address.Trim().Length > 200)
            {
                AddValidationError("العنوان طويل جداً");
            }

            HasValidationErrors = ValidationErrors.Count > 0;
            return !HasValidationErrors;
        }

        /// <summary>
        /// Validates phone number format using business rules
        /// </summary>
        /// <param name="phoneNumber">Phone number to validate</param>
        /// <returns>True if phone number is valid</returns>
        private bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // Remove common formatting characters
            var cleanNumber = phoneNumber.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

            // Basic validation: should contain only digits and optional + at start
            if (cleanNumber.StartsWith("+"))
            {
                cleanNumber = cleanNumber.Substring(1);
            }

            // Should be between 7 and 15 digits
            if (cleanNumber.Length < 7 || cleanNumber.Length > 15)
                return false;

            // Should contain only digits
            return cleanNumber.All(char.IsDigit);
        }

        #endregion

        #region Helper Methods

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
        }

        /// <summary>
        /// Adds a validation error message
        /// </summary>
        /// <param name="message">Error message to add</param>
        private void AddValidationError(string message)
        {
            if (!ValidationErrors.Contains(message))
            {
                ValidationErrors.Add(message);
                HasValidationErrors = true;
                OnPropertyChanged(nameof(CanSaveCustomer));
            }
        }

        /// <summary>
        /// Clears validation errors containing the specified text
        /// </summary>
        /// <param name="fieldName">Field name or error text to clear</param>
        private void ClearValidationError(string fieldName)
        {
            var errorsToRemove = ValidationErrors.Where(e => e.Contains(fieldName)).ToList();
            foreach (var error in errorsToRemove)
            {
                ValidationErrors.Remove(error);
            }

            HasValidationErrors = ValidationErrors.Count > 0;
            OnPropertyChanged(nameof(CanSaveCustomer));
        }

        /// <summary>
        /// Resets the dialog to initial state
        /// </summary>
        public void ResetDialog()
        {
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

            _logger.LogDebug("Customer dialog reset to initial state");
        }

        #endregion
    }
}