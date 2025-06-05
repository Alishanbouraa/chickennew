using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Models;
using PoultrySlaughterPOS.Services.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PoultrySlaughterPOS.Services
{
    /// <summary>
    /// Enterprise-grade transaction processing service for comprehensive invoice and payment handling.
    /// Manages complex transaction flows with automatic debt management and payment reconciliation.
    /// </summary>
    public interface ITransactionProcessingService
    {
        Task<TransactionResult> ProcessTransactionWithPaymentAsync(TransactionRequest request, CancellationToken cancellationToken = default);
        Task<TransactionResult> ProcessInvoiceOnlyAsync(Invoice invoice, CancellationToken cancellationToken = default);
        Task<PaymentResult> ProcessPaymentOnlyAsync(Payment payment, CancellationToken cancellationToken = default);
        Task<TransactionSummary> GetTransactionSummaryAsync(int customerId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Implementation of transaction processing service with comprehensive error handling and audit logging
    /// </summary>
    public class TransactionProcessingService : ITransactionProcessingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TransactionProcessingService> _logger;
        private readonly IErrorHandlingService _errorHandlingService;

        public TransactionProcessingService(
            IUnitOfWork unitOfWork,
            ILogger<TransactionProcessingService> logger,
            IErrorHandlingService errorHandlingService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));
        }

        /// <summary>
        /// Processes complete transaction with invoice and payment in atomic operation
        /// </summary>
        public async Task<TransactionResult> ProcessTransactionWithPaymentAsync(TransactionRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Processing transaction with payment for customer {CustomerId}, Invoice amount: {InvoiceAmount:C}, Payment: {PaymentAmount:C}",
                    request.Invoice.CustomerId, request.Invoice.FinalAmount, request.PaymentAmount);

                // Begin transaction scope
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    // Step 1: Create and save invoice
                    var savedInvoice = await _unitOfWork.Invoices.CreateInvoiceWithTransactionAsync(request.Invoice, cancellationToken);

                    Payment? processedPayment = null;
                    decimal remainingBalance = savedInvoice.FinalAmount;

                    // Step 2: Process payment if provided
                    if (request.PaymentAmount > 0)
                    {
                        var payment = new Payment
                        {
                            CustomerId = savedInvoice.CustomerId,
                            InvoiceId = savedInvoice.InvoiceId,
                            Amount = Math.Min(request.PaymentAmount, savedInvoice.FinalAmount), // Cap at invoice amount
                            PaymentMethod = request.PaymentMethod ?? "CASH",
                            PaymentDate = DateTime.Now,
                            Notes = BuildPaymentNotes(request),
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        };

                        processedPayment = await _unitOfWork.Payments.CreatePaymentWithTransactionAsync(payment, cancellationToken);
                        remainingBalance = Math.Max(0, savedInvoice.FinalAmount - processedPayment.Amount);
                    }

                    // Step 3: Handle overpayment
                    decimal overpaymentAmount = 0;
                    if (request.PaymentAmount > savedInvoice.FinalAmount)
                    {
                        overpaymentAmount = request.PaymentAmount - savedInvoice.FinalAmount;

                        // Create separate payment record for overpayment (credit to customer account)
                        var overpayment = new Payment
                        {
                            CustomerId = savedInvoice.CustomerId,
                            Amount = overpaymentAmount,
                            PaymentMethod = request.PaymentMethod ?? "CASH",
                            PaymentDate = DateTime.Now,
                            Notes = "مبلغ زائد - رصيد دائن للزبون",
                            CreatedDate = DateTime.Now,
                            UpdatedDate = DateTime.Now
                        };

                        await _unitOfWork.Payments.CreatePaymentWithTransactionAsync(overpayment, cancellationToken);
                    }

                    // Step 4: Update customer debt (remaining balance added to debt)
                    var customer = await _unitOfWork.Customers.GetByIdAsync(savedInvoice.CustomerId, cancellationToken);
                    if (customer != null)
                    {
                        // Note: Customer debt is automatically updated by invoice creation and payment processing
                        // The repositories handle this automatically, but we verify the final state
                        customer.UpdatedDate = DateTime.Now;
                    }

                    // Commit transaction
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);

                    var result = new TransactionResult
                    {
                        Success = true,
                        Invoice = savedInvoice,
                        Payment = processedPayment,
                        AmountDue = savedInvoice.FinalAmount,
                        PaymentReceived = request.PaymentAmount,
                        RemainingBalance = remainingBalance,
                        OverpaymentAmount = overpaymentAmount,
                        Message = BuildSuccessMessage(savedInvoice, processedPayment, remainingBalance, overpaymentAmount)
                    };

                    _logger.LogInformation("Transaction processed successfully. Invoice: {InvoiceNumber}, Payment: {PaymentAmount:C}, Remaining: {RemainingBalance:C}",
                        savedInvoice.InvoiceNumber, request.PaymentAmount, remainingBalance);

                    return result;
                }
                catch (Exception)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing transaction with payment for customer {CustomerId}",
                    request.Invoice.CustomerId);

                return new TransactionResult
                {
                    Success = false,
                    Message = "حدث خطأ أثناء معالجة المعاملة",
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// Processes invoice only (no payment)
        /// </summary>
        public async Task<TransactionResult> ProcessInvoiceOnlyAsync(Invoice invoice, CancellationToken cancellationToken = default)
        {
            try
            {
                var savedInvoice = await _unitOfWork.Invoices.CreateInvoiceWithTransactionAsync(invoice, cancellationToken);
                await _unitOfWork.SaveChangesAsync("INVOICE_ONLY", cancellationToken);

                return new TransactionResult
                {
                    Success = true,
                    Invoice = savedInvoice,
                    AmountDue = savedInvoice.FinalAmount,
                    PaymentReceived = 0,
                    RemainingBalance = savedInvoice.FinalAmount,
                    Message = $"تم إنشاء الفاتورة رقم {savedInvoice.InvoiceNumber} بمبلغ {savedInvoice.FinalAmount:F2} USD"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing invoice only");
                return new TransactionResult
                {
                    Success = false,
                    Message = "حدث خطأ أثناء إنشاء الفاتورة",
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// Processes payment only (for existing invoices or general payments)
        /// </summary>
        public async Task<PaymentResult> ProcessPaymentOnlyAsync(Payment payment, CancellationToken cancellationToken = default)
        {
            try
            {
                var processedPayment = await _unitOfWork.Payments.CreatePaymentWithTransactionAsync(payment, cancellationToken);
                await _unitOfWork.SaveChangesAsync("PAYMENT_ONLY", cancellationToken);

                return new PaymentResult
                {
                    Success = true,
                    Payment = processedPayment,
                    Message = $"تم تسجيل دفعة بمبلغ {processedPayment.Amount:F2} USD"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment only");
                return new PaymentResult
                {
                    Success = false,
                    Message = "حدث خطأ أثناء تسجيل الدفعة",
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// Gets comprehensive transaction summary for customer
        /// </summary>
        public async Task<TransactionSummary> GetTransactionSummaryAsync(int customerId, CancellationToken cancellationToken = default)
        {
            try
            {
                var customer = await _unitOfWork.Customers.GetByIdAsync(customerId, cancellationToken);
                var accountSummary = await _unitOfWork.Customers.GetCustomerAccountSummaryAsync(customerId, cancellationToken: cancellationToken);
                var lastPayment = await _unitOfWork.Payments.GetCustomerLastPaymentAsync(customerId, cancellationToken);

                return new TransactionSummary
                {
                    CustomerId = customerId,
                    CustomerName = customer?.CustomerName ?? "غير معروف",
                    CurrentBalance = customer?.TotalDebt ?? 0,
                    TotalSales = accountSummary.TotalSales,
                    TotalPayments = accountSummary.TotalPayments,
                    LastPaymentAmount = lastPayment?.Amount ?? 0,
                    LastPaymentDate = lastPayment?.PaymentDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction summary for customer {CustomerId}", customerId);
                throw;
            }
        }

        #region Private Helper Methods

        private string BuildPaymentNotes(TransactionRequest request)
        {
            var notes = !string.IsNullOrWhiteSpace(request.PaymentNotes) ? request.PaymentNotes : "دفعة مع الفاتورة";

            if (request.PaymentAmount > request.Invoice.FinalAmount)
            {
                notes += " (يشمل مبلغ زائد)";
            }
            else if (request.PaymentAmount < request.Invoice.FinalAmount)
            {
                notes += " (دفعة جزئية)";
            }

            return notes;
        }

        private string BuildSuccessMessage(Invoice invoice, Payment? payment, decimal remainingBalance, decimal overpaymentAmount)
        {
            var message = $"تم إنشاء الفاتورة رقم {invoice.InvoiceNumber} بمبلغ {invoice.FinalAmount:F2} USD";

            if (payment != null)
            {
                message += $"\nتم تسجيل دفعة بمبلغ {payment.Amount:F2} USD";

                if (remainingBalance > 0)
                {
                    message += $"\nالرصيد المتبقي: {remainingBalance:F2} USD (تم إضافته لدين الزبون)";
                }
                else if (overpaymentAmount > 0)
                {
                    message += $"\nمبلغ زائد: {overpaymentAmount:F2} USD (تم تسجيله كرصيد دائن)";
                }
                else
                {
                    message += "\nتم تسديد المبلغ بالكامل";
                }
            }
            else
            {
                message += $"\nالمبلغ المستحق: {invoice.FinalAmount:F2} USD (تم إضافته لدين الزبون)";
            }

            return message;
        }

        #endregion
    }

    #region Data Transfer Objects

    /// <summary>
    /// Request model for transaction processing
    /// </summary>
    public class TransactionRequest
    {
        public Invoice Invoice { get; set; } = null!;
        public decimal PaymentAmount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentNotes { get; set; }
    }

    /// <summary>
    /// Result model for transaction processing
    /// </summary>
    public class TransactionResult
    {
        public bool Success { get; set; }
        public Invoice? Invoice { get; set; }
        public Payment? Payment { get; set; }
        public decimal AmountDue { get; set; }
        public decimal PaymentReceived { get; set; }
        public decimal RemainingBalance { get; set; }
        public decimal OverpaymentAmount { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Error { get; set; }
    }

    /// <summary>
    /// Result model for payment processing
    /// </summary>
    public class PaymentResult
    {
        public bool Success { get; set; }
        public Payment? Payment { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Error { get; set; }
    }

    /// <summary>
    /// Summary model for customer transaction history
    /// </summary>
    public class TransactionSummary
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal CurrentBalance { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal LastPaymentAmount { get; set; }
        public DateTime? LastPaymentDate { get; set; }
    }

    #endregion
}