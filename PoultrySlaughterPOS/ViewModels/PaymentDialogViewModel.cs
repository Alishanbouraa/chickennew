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
    /// Optimized for secure financial operations with full audit trail support.
    /// </summary>
    public partial class PaymentDialogViewModel : ObservableObject
    {
        #region Private Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PaymentDialogViewModel> _logger;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly Customer _customer;

        // UI State
        private bool _isProcessing = false;
        private bool _hasValidationErrors = false;
        private ObservableCollection<string> _validationErrors = new();

        // Payment Data
        private decimal _paymentAmount = 0;
        private string _paymentMethod = "CASH";
        private DateTime _paymentDate = DateTime.Now;
        private string _paymentNotes = string.Empty;

        // Calculated Fields
        private decimal _currentDebt = 0;
        private decimal _remainingBalance = 0;
        private decimal _paymentPercentage = 0;

        // Validation Errors
        private string _paymentAmountError = string.Empty;
        private bool _hasPaymentAmountError = false;

        // Dialog Result
        private Payment? _createdPayment;
        private bool _dialogResult = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes PaymentDialogViewModel with comprehensive dependency injection
        /// </summary>
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

            InitializePaymentData();
            _logger.LogInformation("PaymentDialogViewModel initialized for customer: {CustomerName} (Debt: {Debt})",
                customer.CustomerName, customer.TotalDebt);
        }

        #endregion

        #region Observable Properties

        /// <summary>
        /// Customer name for display purposes
        /// </summary>
        public string CustomerName => _customer.CustomerName;

        /// <summary>
        /// Current customer debt amount
        /// </summary>
        public decimal CurrentDebt
        {
            get => _currentDebt;
            set => SetProperty(ref _currentDebt, value);
        }

        /// <summary>
        /// Payment amount entered by user
        /// </summary>
        [Required(ErrorMessage = "مبلغ الدفعة مطلوب")]
        [Range(0.01, double.MaxValue, ErrorMessage = "مبلغ الدفعة يجب أن يكون أكبر من صفر")]
        public decimal PaymentAmount
        {
            get => _paymentAmount;
            set
            {
                if (SetProperty(ref _paymentAmount, value))
                {
                    ValidatePaymentAmount();
                    CalculateRemainingBalance();
                    CalculatePaymentPercentage();
                    OnPropertyChanged(nameof(CanSavePayment));
                }
            }
        }

        /// <summary>
        /// Selected payment method
        /// </summary>
        public string PaymentMethod
        {
            get => _paymentMethod;
            set => SetProperty(ref _paymentMethod, value);
        }

        /// <summary>
        /// Payment date
        /// </summary>
        public DateTime PaymentDate
        {
            get => _paymentDate;
            set => SetProperty(ref _paymentDate, value);
        }

        /// <summary>
        /// Payment notes or comments
        /// </summary>
        public string PaymentNotes
        {
            get => _paymentNotes;
            set => SetProperty(ref _paymentNotes, value);
        }

        /// <summary>
        /// Calculated remaining balance after payment
        /// </summary>
        public decimal RemainingBalance
        {
            get => _remainingBalance;
            set => SetProperty(ref _remainingBalance, value);
        }

        /// <summary>
        /// Calculated payment percentage of total debt
        /// </summary>
        public decimal PaymentPercentage
        {
            get => _paymentPercentage;
            set => SetProperty(ref _paymentPercentage, value);
        }

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
        /// Processing state indicator
        /// </summary>
        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
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
        /// Indicates whether payment can be saved
        /// </summary>
        public bool CanSavePayment => !HasValidationErrors && PaymentAmount > 0 && !IsProcessing;

        /// <summary>
        /// Created payment after successful processing
        /// </summary>
        public Payment? CreatedPayment => _createdPayment;

        /// <summary>
        /// Dialog result indicating success or cancellation
        /// </summary>
        public bool DialogResult => _dialogResult;

        #endregion

        #region Commands

        [RelayCommand]
        private async Task SavePaymentAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                _logger.LogInformation("Processing payment of {Amount} for customer {CustomerId}",
                    PaymentAmount, _customer.CustomerId);

                // Final validation
                if (!ValidatePaymentData())
                {
                    _logger.LogWarning("Payment validation failed for customer {CustomerId}", _customer.CustomerId);
                    return;
                }

                // Create payment object
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

                // Process payment with transaction
                _createdPayment = await _unitOfWork.Payments.CreatePaymentWithTransactionAsync(payment);
                await _unitOfWork.SaveChangesAsync("PAYMENT_PROCESSING");

                _dialogResult = true;

                _logger.LogInformation("Payment processed successfully. PaymentId: {PaymentId}, Amount: {Amount}",
                    _createdPayment.PaymentId, _createdPayment.Amount);

                // Close dialog
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Application.Current.Windows.Cast<Window>().FirstOrDefault(w => w.DataContext == this) is Window dialog)
                    {
                        dialog.DialogResult = true;
                        dialog.Close();
                    }
                });

            }, "معالجة الدفعة");
        }

        [RelayCommand]
        private void Cancel()
        {
            try
            {
                _logger.LogDebug("Payment dialog cancelled for customer {CustomerId}", _customer.CustomerId);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Application.Current.Windows.Cast<Window>().FirstOrDefault(w => w.DataContext == this) is Window dialog)
                    {
                        dialog.DialogResult = false;
                        dialog.Close();
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during payment dialog cancellation");
            }
        }

        [RelayCommand]
        private void SetPercentage(object parameter)
        {
            try
            {
                if (parameter is string percentageStr && decimal.TryParse(percentageStr, out var percentage))
                {
                    PaymentAmount = Math.Round(CurrentDebt * percentage, 2);
                    _logger.LogDebug("Payment amount set to {Percentage:P0} of debt: {Amount}",
                        percentage, PaymentAmount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error setting payment percentage");
            }
        }

        [RelayCommand]
        private void SetFullAmount()
        {
            try
            {
                PaymentAmount = CurrentDebt;
                _logger.LogDebug("Payment amount set to full debt amount: {Amount}", PaymentAmount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error setting full payment amount");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Refreshes customer debt information
        /// </summary>
        public async Task RefreshCustomerDebtAsync()
        {
            try
            {
                var currentDebt = await _unitOfWork.Customers.GetCustomerTotalDebtAsync(_customer.CustomerId);
                CurrentDebt = currentDebt;
                CalculateRemainingBalance();
                CalculatePaymentPercentage();

                _logger.LogDebug("Customer debt refreshed: {Debt}", currentDebt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing customer debt for customer {CustomerId}", _customer.CustomerId);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes payment data with current customer information
        /// </summary>
        private void InitializePaymentData()
        {
            try
            {
                CurrentDebt = _customer.TotalDebt;
                PaymentDate = DateTime.Now;
                PaymentMethod = "CASH";

                // Set default payment amount to full debt if reasonable amount
                if (CurrentDebt > 0 && CurrentDebt <= 10000)
                {
                    PaymentAmount = CurrentDebt;
                }

                CalculateRemainingBalance();
                CalculatePaymentPercentage();

                _logger.LogDebug("Payment data initialized for customer {CustomerId}. Debt: {Debt}",
                    _customer.CustomerId, CurrentDebt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing payment data");
            }
        }

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
                else if (PaymentAmount > CurrentDebt * 2) // Allow overpayment up to 200% for flexibility
                {
                    PaymentAmountError = $"مبلغ الدفعة مرتفع جداً. الحد الأقصى المسموح: {CurrentDebt * 2:N2}";
                    HasPaymentAmountError = true;
                }
                else if (PaymentAmount > 1000000) // Business limit
                {
                    PaymentAmountError = "مبلغ الدفعة يتجاوز الحد الأقصى المسموح (1,000,000)";
                    HasPaymentAmountError = true;
                }

                UpdateValidationSummary();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during payment amount validation");
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

                if (PaymentAmount > CurrentDebt * 2)
                {
                    ValidationErrors.Add("مبلغ الدفعة مرتفع جداً مقارنة بالدين الحالي");
                }

                // Payment method validation
                if (string.IsNullOrWhiteSpace(PaymentMethod))
                {
                    ValidationErrors.Add("طريقة الدفع مطلوبة");
                }

                // Date validation
                if (PaymentDate > DateTime.Now.AddDays(1))
                {
                    ValidationErrors.Add("تاريخ الدفع لا يمكن أن يكون في المستقبل");
                }

                if (PaymentDate < DateTime.Now.AddYears(-1))
                {
                    ValidationErrors.Add("تاريخ الدفع قديم جداً");
                }

                // Notes validation (optional but length check)
                if (!string.IsNullOrWhiteSpace(PaymentNotes) && PaymentNotes.Trim().Length > 500)
                {
                    ValidationErrors.Add("ملاحظات الدفعة طويلة جداً (الحد الأقصى 500 حرف)");
                }

                HasValidationErrors = ValidationErrors.Any();
                return !HasValidationErrors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during payment data validation");
                ValidationErrors.Add("خطأ في التحقق من صحة البيانات");
                HasValidationErrors = true;
                return false;
            }
        }

        /// <summary>
        /// Calculates remaining balance after payment
        /// </summary>
        private void CalculateRemainingBalance()
        {
            try
            {
                RemainingBalance = Math.Max(0, CurrentDebt - PaymentAmount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating remaining balance");
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
                _logger.LogWarning(ex, "Error calculating payment percentage");
                PaymentPercentage = 0;
            }
        }

        /// <summary>
        /// Updates validation summary display
        /// </summary>
        private void UpdateValidationSummary()
        {
            try
            {
                ValidationErrors.Clear();

                if (HasPaymentAmountError)
                {
                    ValidationErrors.Add(PaymentAmountError);
                }

                HasValidationErrors = ValidationErrors.Any();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating validation summary");
            }
        }

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
                _logger.LogError(ex, "Error executing operation: {OperationName}", operationName);
                var (success, userMessage) = await _errorHandlingService.HandleExceptionAsync(ex, operationName);

                // Show error to user
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
}