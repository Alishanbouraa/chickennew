// PoultrySlaughterPOS/ViewModels/PaymentDialogViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Models;
using PoultrySlaughterPOS.Services;
using PoultrySlaughterPOS.Services.Repositories;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PoultrySlaughterPOS.ViewModels
{
    /// <summary>
    /// Enterprise-grade Payment Dialog ViewModel implementing comprehensive debt settlement logic
    /// with advanced validation, real-time calculations, and transactional payment processing.
    /// 
    /// ARCHITECTURE: MVVM pattern with dependency injection, comprehensive error handling,
    /// and optimized for secure financial operations with full audit trail support.
    /// 
    /// FEATURES:
    /// - Real-time payment validation and calculation
    /// - Multiple payment method support with validation
    /// - Comprehensive error handling and user feedback
    /// - Audit logging for financial transparency
    /// - Keyboard shortcut support for productivity
    /// - Thread-safe operations with proper async/await patterns
    /// </summary>
    public partial class PaymentDialogViewModel : ObservableObject
    {
        #region Private Fields and Dependencies

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PaymentDialogViewModel> _logger;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly Customer _customer;

        // UI State Management
        private bool _isProcessing = false;
        private bool _hasValidationErrors = false;
        private bool _isInitialized = false;
        private ObservableCollection<string> _validationErrors = new();

        // Payment Data Properties
        private decimal _paymentAmount = 0;
        private string _paymentMethod = "CASH";
        private DateTime _paymentDate = DateTime.Now;
        private string _paymentNotes = string.Empty;

        // Calculated Financial Properties
        private decimal _currentDebt = 0;
        private decimal _remainingBalance = 0;
        private decimal _paymentPercentage = 0;

        // Validation State Properties
        private string _paymentAmountError = string.Empty;
        private bool _hasPaymentAmountError = false;
        private string _paymentMethodError = string.Empty;
        private bool _hasPaymentMethodError = false;
        private string _paymentDateError = string.Empty;
        private bool _hasPaymentDateError = false;

        // Dialog Result Properties
        private Payment? _createdPayment;
        private bool _dialogResult = false;

        // Available Payment Methods for ComboBox binding
        private readonly List<PaymentMethodOption> _availablePaymentMethods;

        #endregion

        #region Constructor and Initialization

        /// <summary>
        /// Initializes PaymentDialogViewModel with comprehensive dependency injection
        /// and enterprise-grade error handling setup
        /// </summary>
        /// <param name="customer">Customer entity for payment processing</param>
        /// <param name="unitOfWork">Unit of work for database operations</param>
        /// <param name="logger">Logger for comprehensive audit trail</param>
        /// <param name="errorHandlingService">Service for centralized error handling</param>
        public PaymentDialogViewModel(
            Customer customer,
            IUnitOfWork unitOfWork,
            ILogger<PaymentDialogViewModel> logger,
            IErrorHandlingService errorHandlingService)
        {
            _customer = customer ?? throw new ArgumentNullException(nameof(customer));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));

            // Initialize available payment methods
            _availablePaymentMethods = InitializePaymentMethods();

            // Initialize payment data with customer context
            InitializePaymentData();

            _logger.LogInformation("PaymentDialogViewModel initialized for customer: {CustomerName} (ID: {CustomerId}, Debt: {Debt:C})",
                customer.CustomerName, customer.CustomerId, customer.TotalDebt);
        }

        /// <summary>
        /// Initializes available payment methods for selection
        /// </summary>
        private List<PaymentMethodOption> InitializePaymentMethods()
        {
            return new List<PaymentMethodOption>
            {
                new PaymentMethodOption { Code = "CASH", DisplayName = "نقداً", Description = "دفع نقدي مباشر" },
                new PaymentMethodOption { Code = "CHECK", DisplayName = "شيك", Description = "دفع بشيك بنكي" },
                new PaymentMethodOption { Code = "BANK_TRANSFER", DisplayName = "حوالة بنكية", Description = "تحويل بنكي مباشر" },
                new PaymentMethodOption { Code = "CREDIT_CARD", DisplayName = "بطاقة ائتمان", Description = "دفع بالبطاقة الائتمانية" },
                new PaymentMethodOption { Code = "DEBIT_CARD", DisplayName = "بطاقة دفع", Description = "دفع بالبطاقة المصرفية" }
            };
        }

        #endregion

        #region Observable Properties - Customer Information

        /// <summary>
        /// Customer name for display purposes (read-only)
        /// </summary>
        public string CustomerName => _customer.CustomerName;

        /// <summary>
        /// Customer ID for reference (read-only)
        /// </summary>
        public int CustomerId => _customer.CustomerId;

        /// <summary>
        /// Current customer debt amount with real-time updates
        /// </summary>
        public decimal CurrentDebt
        {
            get => _currentDebt;
            set
            {
                if (SetProperty(ref _currentDebt, value))
                {
                    _logger.LogDebug("CurrentDebt updated to {Debt:C} for customer {CustomerId}", value, _customer.CustomerId);
                    CalculateRemainingBalance();
                    CalculatePaymentPercentage();
                    OnPropertyChanged(nameof(DebtDisplayText));
                    OnPropertyChanged(nameof(HasDebt));
                }
            }
        }

        /// <summary>
        /// Formatted debt display text for UI presentation
        /// </summary>
        public string DebtDisplayText => $"{CurrentDebt:C2}";

        /// <summary>
        /// Indicates whether customer has outstanding debt
        /// </summary>
        public bool HasDebt => CurrentDebt > 0;

        #endregion

        #region Observable Properties - Payment Data

        /// <summary>
        /// Payment amount entered by user with comprehensive validation
        /// </summary>
        [Required(ErrorMessage = "مبلغ الدفعة مطلوب")]
        [Range(0.01, double.MaxValue, ErrorMessage = "مبلغ الدفعة يجب أن يكون أكبر من صفر")]
        public decimal PaymentAmount
        {
            get => _paymentAmount;
            set
            {
                var oldValue = _paymentAmount;
                if (SetProperty(ref _paymentAmount, value))
                {
                    _logger.LogDebug("PaymentAmount changed from {OldValue:C} to {NewValue:C} for customer {CustomerId}",
                        oldValue, value, _customer.CustomerId);

                    ValidatePaymentAmount();
                    CalculateRemainingBalance();
                    CalculatePaymentPercentage();
                    UpdateCanSavePayment();
                    OnPropertyChanged(nameof(PaymentAmountDisplay));
                }
            }
        }

        /// <summary>
        /// Payment amount display string for UI binding
        /// </summary>
        public string PaymentAmountDisplay
        {
            get => PaymentAmount.ToString("F2");
            set
            {
                if (decimal.TryParse(value, out decimal amount))
                {
                    PaymentAmount = amount;
                }
            }
        }

        /// <summary>
        /// Selected payment method with validation
        /// </summary>
        [Required(ErrorMessage = "طريقة الدفع مطلوبة")]
        public string PaymentMethod
        {
            get => _paymentMethod;
            set
            {
                if (SetProperty(ref _paymentMethod, value))
                {
                    _logger.LogDebug("PaymentMethod changed to {Method} for customer {CustomerId}", value, _customer.CustomerId);
                    ValidatePaymentMethod();
                    UpdateCanSavePayment();
                    OnPropertyChanged(nameof(SelectedPaymentMethodDisplay));
                }
            }
        }

        /// <summary>
        /// Available payment methods for ComboBox binding
        /// </summary>
        public List<PaymentMethodOption> AvailablePaymentMethods => _availablePaymentMethods;

        /// <summary>
        /// Selected payment method display name
        /// </summary>
        public string SelectedPaymentMethodDisplay =>
            _availablePaymentMethods.FirstOrDefault(pm => pm.Code == PaymentMethod)?.DisplayName ?? PaymentMethod;

        /// <summary>
        /// Payment date with validation
        /// </summary>
        [Required(ErrorMessage = "تاريخ الدفع مطلوب")]
        public DateTime PaymentDate
        {
            get => _paymentDate;
            set
            {
                if (SetProperty(ref _paymentDate, value))
                {
                    _logger.LogDebug("PaymentDate changed to {Date} for customer {CustomerId}", value, _customer.CustomerId);
                    ValidatePaymentDate();
                    UpdateCanSavePayment();
                }
            }
        }

        /// <summary>
        /// Payment notes or comments (optional)
        /// </summary>
        public string PaymentNotes
        {
            get => _paymentNotes;
            set
            {
                if (SetProperty(ref _paymentNotes, value))
                {
                    _logger.LogDebug("PaymentNotes updated for customer {CustomerId}, Length: {Length}",
                        _customer.CustomerId, value?.Length ?? 0);
                    ValidatePaymentNotes();
                }
            }
        }

        #endregion

        #region Observable Properties - Calculated Values

        /// <summary>
        /// Calculated remaining balance after payment application
        /// </summary>
        public decimal RemainingBalance
        {
            get => _remainingBalance;
            set
            {
                if (SetProperty(ref _remainingBalance, value))
                {
                    OnPropertyChanged(nameof(RemainingBalanceDisplay));
                    OnPropertyChanged(nameof(WillFullySettle));
                    OnPropertyChanged(nameof(IsOverpayment));
                }
            }
        }

        /// <summary>
        /// Formatted remaining balance display text
        /// </summary>
        public string RemainingBalanceDisplay => $"{RemainingBalance:C2}";

        /// <summary>
        /// Calculated payment percentage of total debt
        /// </summary>
        public decimal PaymentPercentage
        {
            get => _paymentPercentage;
            set
            {
                if (SetProperty(ref _paymentPercentage, value))
                {
                    OnPropertyChanged(nameof(PaymentPercentageDisplay));
                }
            }
        }

        /// <summary>
        /// Formatted payment percentage display text
        /// </summary>
        public string PaymentPercentageDisplay => $"{PaymentPercentage:P1}";

        /// <summary>
        /// Indicates if payment will fully settle the debt
        /// </summary>
        public bool WillFullySettle => RemainingBalance <= 0;

        /// <summary>
        /// Indicates if payment amount exceeds current debt
        /// </summary>
        public bool IsOverpayment => PaymentAmount > CurrentDebt;

        #endregion

        #region Observable Properties - UI State

        /// <summary>
        /// Indicates whether payment processing is in progress
        /// </summary>
        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                if (SetProperty(ref _isProcessing, value))
                {
                    OnPropertyChanged(nameof(AreControlsEnabled));
                    UpdateCanSavePayment();
                    _logger.LogDebug("IsProcessing state changed to {IsProcessing} for customer {CustomerId}",
                        value, _customer.CustomerId);
                }
            }
        }

        /// <summary>
        /// Indicates whether ViewModel has been properly initialized
        /// </summary>
        public bool IsInitialized
        {
            get => _isInitialized;
            private set
            {
                if (SetProperty(ref _isInitialized, value))
                {
                    OnPropertyChanged(nameof(AreControlsEnabled));
                    _logger.LogDebug("IsInitialized state changed to {IsInitialized} for customer {CustomerId}",
                        value, _customer.CustomerId);
                }
            }
        }

        /// <summary>
        /// Indicates whether UI controls should be enabled
        /// </summary>
        public bool AreControlsEnabled => !IsProcessing && IsInitialized;

        /// <summary>
        /// Indicates whether validation errors exist
        /// </summary>
        public bool HasValidationErrors
        {
            get => _hasValidationErrors;
            set => SetProperty(ref _hasValidationErrors, value);
        }

        /// <summary>
        /// Collection of validation error messages for UI display
        /// </summary>
        public ObservableCollection<string> ValidationErrors
        {
            get => _validationErrors;
            set => SetProperty(ref _validationErrors, value);
        }

        #endregion

        #region Observable Properties - Field-Specific Validation

        /// <summary>
        /// Payment amount validation error message
        /// </summary>
        public string PaymentAmountError
        {
            get => _paymentAmountError;
            set => SetProperty(ref _paymentAmountError, value);
        }

        /// <summary>
        /// Indicates if payment amount has validation errors
        /// </summary>
        public bool HasPaymentAmountError
        {
            get => _hasPaymentAmountError;
            set => SetProperty(ref _hasPaymentAmountError, value);
        }

        /// <summary>
        /// Payment method validation error message
        /// </summary>
        public string PaymentMethodError
        {
            get => _paymentMethodError;
            set => SetProperty(ref _paymentMethodError, value);
        }

        /// <summary>
        /// Indicates if payment method has validation errors
        /// </summary>
        public bool HasPaymentMethodError
        {
            get => _hasPaymentMethodError;
            set => SetProperty(ref _hasPaymentMethodError, value);
        }

        /// <summary>
        /// Payment date validation error message
        /// </summary>
        public string PaymentDateError
        {
            get => _paymentDateError;
            set => SetProperty(ref _paymentDateError, value);
        }

        /// <summary>
        /// Indicates if payment date has validation errors
        /// </summary>
        public bool HasPaymentDateError
        {
            get => _hasPaymentDateError;
            set => SetProperty(ref _hasPaymentDateError, value);
        }

        #endregion

        #region Observable Properties - Dialog Result

        /// <summary>
        /// Indicates whether payment can be saved (all validations pass)
        /// </summary>
        public bool CanSavePayment { get; private set; }

        /// <summary>
        /// Created payment record after successful processing
        /// </summary>
        public Payment? CreatedPayment => _createdPayment;

        /// <summary>
        /// Dialog result indicating success or cancellation
        /// </summary>
        public bool DialogResult => _dialogResult;

        #endregion

        #region Commands

        /// <summary>
        /// Command to save payment with comprehensive validation and error handling
        /// </summary>
        [RelayCommand]
        private async Task SavePaymentAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                _logger.LogInformation("Initiating payment processing of {Amount:C} for customer {CustomerId} using {Method}",
                    PaymentAmount, _customer.CustomerId, PaymentMethod);

                // Final validation before processing
                if (!ValidatePaymentData())
                {
                    _logger.LogWarning("Payment validation failed for customer {CustomerId}", _customer.CustomerId);
                    return;
                }

                // Create payment entity with comprehensive data
                var payment = new Payment
                {
                    CustomerId = _customer.CustomerId,
                    Amount = PaymentAmount,
                    PaymentMethod = PaymentMethod,
                    PaymentDate = PaymentDate,
                    Notes = string.IsNullOrWhiteSpace(PaymentNotes) ? null : PaymentNotes.Trim(),
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };

                // Process payment within database transaction
                _createdPayment = await _unitOfWork.Payments.CreatePaymentWithTransactionAsync(payment);

                // Commit transaction
                await _unitOfWork.SaveChangesAsync("PAYMENT_PROCESSING");

                _dialogResult = true;

                _logger.LogInformation("Payment processed successfully. PaymentId: {PaymentId}, Amount: {Amount:C}, Customer: {CustomerId}",
                    _createdPayment.PaymentId, _createdPayment.Amount, _customer.CustomerId);

                // Close dialog with success result
                CloseDialogWithResult(true);

            }, "معالجة الدفعة");
        }

        /// <summary>
        /// Command to cancel payment dialog
        /// </summary>
        [RelayCommand]
        private void Cancel()
        {
            try
            {
                _logger.LogDebug("Payment dialog cancelled by user for customer {CustomerId}", _customer.CustomerId);
                CloseDialogWithResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during payment dialog cancellation");
                CloseDialogWithResult(false);
            }
        }

        /// <summary>
        /// Command to set payment amount to a percentage of total debt
        /// </summary>
        [RelayCommand]
        private void SetPercentage(object parameter)
        {
            try
            {
                if (parameter is string percentageStr && decimal.TryParse(percentageStr, out var percentage))
                {
                    var calculatedAmount = Math.Round(CurrentDebt * percentage, 2);
                    PaymentAmount = calculatedAmount;

                    _logger.LogDebug("Payment amount set to {Percentage:P0} of debt: {Amount:C} for customer {CustomerId}",
                        percentage, calculatedAmount, _customer.CustomerId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error setting payment percentage for customer {CustomerId}", _customer.CustomerId);
            }
        }

        /// <summary>
        /// Command to set payment amount to full debt amount
        /// </summary>
        [RelayCommand]
        private void SetFullAmount()
        {
            try
            {
                PaymentAmount = CurrentDebt;
                _logger.LogDebug("Payment amount set to full debt amount: {Amount:C} for customer {CustomerId}",
                    PaymentAmount, _customer.CustomerId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error setting full payment amount for customer {CustomerId}", _customer.CustomerId);
            }
        }

        /// <summary>
        /// Command to refresh customer debt information
        /// </summary>
        [RelayCommand]
        private async Task RefreshDebtAsync()
        {
            try
            {
                _logger.LogDebug("Refreshing debt information for customer {CustomerId}", _customer.CustomerId);
                await RefreshCustomerDebtAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing customer debt for customer {CustomerId}", _customer.CustomerId);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Refreshes customer debt information from database
        /// </summary>
        public async Task RefreshCustomerDebtAsync()
        {
            try
            {
                var currentDebt = await _unitOfWork.Customers.GetCustomerTotalDebtAsync(_customer.CustomerId);
                CurrentDebt = currentDebt;

                _logger.LogDebug("Customer debt refreshed: {Debt:C} for customer {CustomerId}",
                    currentDebt, _customer.CustomerId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing customer debt for customer {CustomerId}", _customer.CustomerId);
                throw;
            }
        }

        /// <summary>
        /// Validates all payment data comprehensively
        /// </summary>
        public bool ValidateAll()
        {
            try
            {
                ValidatePaymentAmount();
                ValidatePaymentMethod();
                ValidatePaymentDate();
                ValidatePaymentNotes();

                var isValid = ValidatePaymentData();
                _logger.LogDebug("Comprehensive validation result: {IsValid} for customer {CustomerId}",
                    isValid, _customer.CustomerId);

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during comprehensive validation for customer {CustomerId}", _customer.CustomerId);
                return false;
            }
        }

        #endregion

        #region Private Methods - Initialization

        /// <summary>
        /// Initializes payment data with current customer information
        /// </summary>
        private void InitializePaymentData()
        {
            try
            {
                CurrentDebt = _customer.TotalDebt;
                PaymentDate = DateTime.Now;
                PaymentMethod = "CASH"; // Default to cash payment

                // Set intelligent default payment amount
                if (CurrentDebt > 0)
                {
                    if (CurrentDebt <= 1000)
                    {
                        // For small debts, default to full amount
                        PaymentAmount = CurrentDebt;
                    }
                    else if (CurrentDebt <= 10000)
                    {
                        // For medium debts, default to 50%
                        PaymentAmount = Math.Round(CurrentDebt * 0.5m, 2);
                    }
                    else
                    {
                        // For large debts, default to a reasonable amount
                        PaymentAmount = Math.Round(Math.Min(5000, CurrentDebt * 0.25m), 2);
                    }
                }

                CalculateRemainingBalance();
                CalculatePaymentPercentage();

                // Mark as initialized
                IsInitialized = true;

                _logger.LogDebug("Payment data initialized for customer {CustomerId}. Debt: {Debt:C}, Default Payment: {Payment:C}",
                    _customer.CustomerId, CurrentDebt, PaymentAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing payment data for customer {CustomerId}", _customer.CustomerId);
                IsInitialized = false;
                throw;
            }
        }

        #endregion

        #region Private Methods - Validation

        /// <summary>
        /// Validates payment amount with comprehensive business rules
        /// </summary>
        private void ValidatePaymentAmount()
        {
            try
            {
                PaymentAmountError = string.Empty;
                HasPaymentAmountError = false;

                if (PaymentAmount <= 0)
                {
                    PaymentAmountError = "مبلغ الدفعة يجب أن يكون أكبر من صفر";
                    HasPaymentAmountError = true;
                }
                else if (PaymentAmount > CurrentDebt * 3) // Allow overpayment up to 300% for business flexibility
                {
                    PaymentAmountError = $"مبلغ الدفعة مرتفع جداً. الحد الأقصى المسموح: {CurrentDebt * 3:C2}";
                    HasPaymentAmountError = true;
                }
                else if (PaymentAmount > 1000000) // Business limit for single payment
                {
                    PaymentAmountError = "مبلغ الدفعة يتجاوز الحد الأقصى المسموح (1,000,000)";
                    HasPaymentAmountError = true;
                }

                UpdateValidationSummary();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during payment amount validation for customer {CustomerId}", _customer.CustomerId);
            }
        }

        /// <summary>
        /// Validates payment method selection
        /// </summary>
        private void ValidatePaymentMethod()
        {
            try
            {
                PaymentMethodError = string.Empty;
                HasPaymentMethodError = false;

                if (string.IsNullOrWhiteSpace(PaymentMethod))
                {
                    PaymentMethodError = "طريقة الدفع مطلوبة";
                    HasPaymentMethodError = true;
                }
                else if (!_availablePaymentMethods.Any(pm => pm.Code == PaymentMethod))
                {
                    PaymentMethodError = "طريقة الدفع المحددة غير صالحة";
                    HasPaymentMethodError = true;
                }

                UpdateValidationSummary();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during payment method validation for customer {CustomerId}", _customer.CustomerId);
            }
        }

        /// <summary>
        /// Validates payment date with business rules
        /// </summary>
        private void ValidatePaymentDate()
        {
            try
            {
                PaymentDateError = string.Empty;
                HasPaymentDateError = false;

                if (PaymentDate > DateTime.Now.AddDays(1))
                {
                    PaymentDateError = "تاريخ الدفع لا يمكن أن يكون في المستقبل";
                    HasPaymentDateError = true;
                }
                else if (PaymentDate < DateTime.Now.AddYears(-2))
                {
                    PaymentDateError = "تاريخ الدفع قديم جداً (أكثر من سنتين)";
                    HasPaymentDateError = true;
                }

                UpdateValidationSummary();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during payment date validation for customer {CustomerId}", _customer.CustomerId);
            }
        }

        /// <summary>
        /// Validates payment notes (optional field with length constraints)
        /// </summary>
        private void ValidatePaymentNotes()
        {
            try
            {
                // Notes are optional, but if provided, check length
                if (!string.IsNullOrWhiteSpace(PaymentNotes) && PaymentNotes.Trim().Length > 500)
                {
                    // This could be handled as a warning rather than error
                    _logger.LogDebug("Payment notes exceed recommended length for customer {CustomerId}", _customer.CustomerId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during payment notes validation for customer {CustomerId}", _customer.CustomerId);
            }
        }

        /// <summary>
        /// Validates complete payment data before processing
        /// </summary>
        private bool ValidatePaymentData()
        {
            try
            {
                ValidationErrors.Clear();

                // Amount validation
                if (PaymentAmount <= 0)
                {
                    ValidationErrors.Add("مبلغ الدفعة يجب أن يكون أكبر من صفر");
                }

                if (PaymentAmount > CurrentDebt * 3)
                {
                    ValidationErrors.Add("مبلغ الدفعة مرتفع جداً مقارنة بالدين الحالي");
                }

                if (PaymentAmount > 1000000)
                {
                    ValidationErrors.Add("مبلغ الدفعة يتجاوز الحد الأقصى المسموح");
                }

                // Payment method validation
                if (string.IsNullOrWhiteSpace(PaymentMethod))
                {
                    ValidationErrors.Add("طريقة الدفع مطلوبة");
                }
                else if (!_availablePaymentMethods.Any(pm => pm.Code == PaymentMethod))
                {
                    ValidationErrors.Add("طريقة الدفع المحددة غير صالحة");
                }

                // Date validation
                if (PaymentDate > DateTime.Now.AddDays(1))
                {
                    ValidationErrors.Add("تاريخ الدفع لا يمكن أن يكون في المستقبل");
                }

                if (PaymentDate < DateTime.Now.AddYears(-2))
                {
                    ValidationErrors.Add("تاريخ الدفع قديم جداً");
                }

                // Notes validation (length check)
                if (!string.IsNullOrWhiteSpace(PaymentNotes) && PaymentNotes.Trim().Length > 500)
                {
                    ValidationErrors.Add("ملاحظات الدفعة طويلة جداً (الحد الأقصى 500 حرف)");
                }

                // Customer validation
                if (_customer.CustomerId <= 0)
                {
                    ValidationErrors.Add("معرف الزبون غير صالح");
                }

                HasValidationErrors = ValidationErrors.Any();
                return !HasValidationErrors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during payment data validation for customer {CustomerId}", _customer.CustomerId);
                ValidationErrors.Add("خطأ في التحقق من صحة البيانات");
                HasValidationErrors = true;
                return false;
            }
        }

        #endregion

        #region Private Methods - Calculations

        /// <summary>
        /// Calculates remaining balance after payment application
        /// </summary>
        private void CalculateRemainingBalance()
        {
            try
            {
                RemainingBalance = Math.Max(0, CurrentDebt - PaymentAmount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating remaining balance for customer {CustomerId}", _customer.CustomerId);
                RemainingBalance = CurrentDebt;
            }
        }

        /// <summary>
        /// Calculates payment percentage of total debt
        /// </summary>
        private void CalculatePaymentPercentage()
        {
            try
            {
                if (CurrentDebt > 0)
                {
                    PaymentPercentage = Math.Min(1.0m, PaymentAmount / CurrentDebt);
                }
                else
                {
                    PaymentPercentage = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating payment percentage for customer {CustomerId}", _customer.CustomerId);
                PaymentPercentage = 0;
            }
        }

        #endregion

        #region Private Methods - UI State Management

        /// <summary>
        /// Updates validation summary display
        /// </summary>
        private void UpdateValidationSummary()
        {
            try
            {
                var previousErrorCount = ValidationErrors.Count;
                ValidationErrors.Clear();

                // Collect all field-specific errors
                if (HasPaymentAmountError)
                    ValidationErrors.Add(PaymentAmountError);

                if (HasPaymentMethodError)
                    ValidationErrors.Add(PaymentMethodError);

                if (HasPaymentDateError)
                    ValidationErrors.Add(PaymentDateError);

                HasValidationErrors = ValidationErrors.Any();
                UpdateCanSavePayment();

                if (ValidationErrors.Count != previousErrorCount)
                {
                    _logger.LogDebug("Validation summary updated. Error count: {ErrorCount} for customer {CustomerId}",
                        ValidationErrors.Count, _customer.CustomerId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating validation summary for customer {CustomerId}", _customer.CustomerId);
            }
        }

        /// <summary>
        /// Updates the CanSavePayment property based on current state
        /// </summary>
        private void UpdateCanSavePayment()
        {
            try
            {
                var canSave = !HasValidationErrors &&
                              PaymentAmount > 0 &&
                              !IsProcessing &&
                              IsInitialized &&
                              !string.IsNullOrWhiteSpace(PaymentMethod);

                if (CanSavePayment != canSave)
                {
                    CanSavePayment = canSave;
                    OnPropertyChanged(nameof(CanSavePayment));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating CanSavePayment state for customer {CustomerId}", _customer.CustomerId);
            }
        }

        #endregion

        #region Private Methods - Dialog Management

        /// <summary>
        /// Closes dialog with specified result
        /// </summary>
        private void CloseDialogWithResult(bool result)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var dialog = Application.Current.Windows
                        .OfType<Window>()
                        .FirstOrDefault(w => w.DataContext == this);

                    if (dialog != null)
                    {
                        dialog.DialogResult = result;
                        dialog.Close();
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing payment dialog for customer {CustomerId}", _customer.CustomerId);
            }
        }

        #endregion

        #region Private Methods - Error Handling

        /// <summary>
        /// Executes an operation with comprehensive error handling
        /// </summary>
        private async Task ExecuteWithErrorHandlingAsync(Func<Task> operation, string operationName)
        {
            try
            {
                IsProcessing = true;
                await operation();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing operation: {OperationName} for customer {CustomerId}",
                    operationName, _customer.CustomerId);

                var (success, userMessage) = await _errorHandlingService.HandleExceptionAsync(ex, operationName);

                // Show error to user on UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        userMessage,
                        "خطأ في معالجة الدفعة",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
            finally
            {
                IsProcessing = false;
            }
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Payment method option for ComboBox binding
    /// </summary>
    public class PaymentMethodOption
    {
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    #endregion
}