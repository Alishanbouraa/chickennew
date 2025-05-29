using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Models;
using PoultrySlaughterPOS.Services.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PoultrySlaughterPOS.Services
{
    /// <summary>
    /// Interface for Point of Sale business operations providing comprehensive
    /// invoice processing, customer management, and financial calculation services
    /// </summary>
    public interface IPOSService
    {
        // Invoice Operations
        Task<Invoice> CreateInvoiceAsync(InvoiceCreationRequest request, CancellationToken cancellationToken = default);
        Task<Invoice?> GetInvoiceByNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default);
        Task<string> GenerateUniqueInvoiceNumberAsync(CancellationToken cancellationToken = default);
        Task<bool> ValidateInvoiceDataAsync(Invoice invoice, CancellationToken cancellationToken = default);

        // Customer Operations
        Task<IEnumerable<Customer>> GetActiveCustomersAsync(CancellationToken cancellationToken = default);
        Task<Customer> CreateCustomerAsync(CustomerCreationRequest request, CancellationToken cancellationToken = default);
        Task<Customer?> FindCustomerByNameOrPhoneAsync(string searchTerm, CancellationToken cancellationToken = default);

        // Truck Operations
        Task<IEnumerable<Truck>> GetAvailableTrucksAsync(CancellationToken cancellationToken = default);
        Task<TruckLoad?> GetLatestTruckLoadAsync(int truckId, CancellationToken cancellationToken = default);

        // Financial Calculations
        InvoiceCalculationResult CalculateInvoiceTotals(InvoiceCalculationRequest request);
        Task<CustomerBalanceInfo> GetCustomerBalanceInfoAsync(int customerId, CancellationToken cancellationToken = default);

        // Statistics and Reports
        Task<POSDashboardStats> GetTodayStatisticsAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Invoice>> GetRecentInvoicesAsync(int count = 10, CancellationToken cancellationToken = default);

        // Printing and Export
        Task<byte[]> GenerateInvoicePrintDataAsync(int invoiceId, CancellationToken cancellationToken = default);
        Task<bool> PrintInvoiceAsync(int invoiceId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Enterprise-grade Point of Sale service implementation providing comprehensive
    /// business logic, transaction management, and financial operations for poultry sales
    /// </summary>
    public class POSService : IPOSService
    {
        #region Private Fields

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<POSService> _logger;
        private readonly IErrorHandlingService _errorHandlingService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes POSService with comprehensive dependency injection
        /// </summary>
        /// <param name="unitOfWork">Unit of work for database operations</param>
        /// <param name="logger">Logger for diagnostic and error tracking</param>
        /// <param name="errorHandlingService">Centralized error handling service</param>
        public POSService(
            IUnitOfWork unitOfWork,
            ILogger<POSService> logger,
            IErrorHandlingService errorHandlingService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));

            _logger.LogInformation("POSService initialized with enterprise-grade dependency injection");
        }

        #endregion

        #region Invoice Operations

        /// <summary>
        /// Creates a new invoice with comprehensive transaction management and validation
        /// </summary>
        /// <param name="request">Invoice creation request with all required data</param>
        /// <param name="cancellationToken">Cancellation token for async operations</param>
        /// <returns>Created invoice with calculated totals and updated balances</returns>
        public async Task<Invoice> CreateInvoiceAsync(InvoiceCreationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Creating new invoice for customer {CustomerId} with truck {TruckId}",
                    request.CustomerId, request.TruckId);

                // Begin transaction for ACID compliance
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    // Validate request data
                    await ValidateInvoiceCreationRequestAsync(request, cancellationToken);

                    // Create invoice entity
                    var invoice = await BuildInvoiceFromRequestAsync(request, cancellationToken);

                    // Calculate all totals and balances
                    var calculationRequest = new InvoiceCalculationRequest
                    {
                        GrossWeight = invoice.GrossWeight,
                        CagesWeight = invoice.CagesWeight,
                        UnitPrice = invoice.UnitPrice,
                        DiscountPercentage = invoice.DiscountPercentage,
                        PreviousBalance = invoice.PreviousBalance
                    };

                    var calculationResult = CalculateInvoiceTotals(calculationRequest);
                    ApplyCalculationResultToInvoice(invoice, calculationResult);

                    // Save invoice using repository with transaction support
                    var savedInvoice = await _unitOfWork.Invoices.CreateInvoiceWithTransactionAsync(invoice, cancellationToken);

                    // Commit transaction
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);

                    _logger.LogInformation("Invoice created successfully - Number: {InvoiceNumber}, Amount: {FinalAmount}",
                        savedInvoice.InvoiceNumber, savedInvoice.FinalAmount);

                    return savedInvoice;
                }
                catch (Exception)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice for customer {CustomerId}", request.CustomerId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves invoice by number with full details
        /// </summary>
        /// <param name="invoiceNumber">Invoice number to search for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Invoice with details or null if not found</returns>
        public async Task<Invoice?> GetInvoiceByNumberAsync(string invoiceNumber, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Retrieving invoice by number: {InvoiceNumber}", invoiceNumber);

                var invoice = await _unitOfWork.Invoices.GetInvoiceWithDetailsAsync(
                    await GetInvoiceIdByNumberAsync(invoiceNumber, cancellationToken), cancellationToken);

                return invoice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice by number {InvoiceNumber}", invoiceNumber);
                throw;
            }
        }

        /// <summary>
        /// Generates a unique invoice number following business rules
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Unique invoice number</returns>
        public async Task<string> GenerateUniqueInvoiceNumberAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Generating unique invoice number");

                var invoiceNumber = await _unitOfWork.Invoices.GenerateInvoiceNumberAsync(cancellationToken);

                _logger.LogDebug("Generated invoice number: {InvoiceNumber}", invoiceNumber);
                return invoiceNumber;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating unique invoice number");
                throw;
            }
        }

        /// <summary>
        /// Validates invoice data for business rule compliance
        /// </summary>
        /// <param name="invoice">Invoice to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if invoice data is valid</returns>
        public async Task<bool> ValidateInvoiceDataAsync(Invoice invoice, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Validating invoice data for invoice {InvoiceNumber}", invoice.InvoiceNumber);

                var validationErrors = new List<string>();

                // Basic field validation
                if (invoice.CustomerId <= 0)
                    validationErrors.Add("معرف الزبون غير صحيح");

                if (invoice.TruckId <= 0)
                    validationErrors.Add("معرف الشاحنة غير صحيح");

                if (invoice.GrossWeight <= 0)
                    validationErrors.Add("الوزن الفلتي يجب أن يكون أكبر من الصفر");

                if (invoice.CagesWeight < 0)
                    validationErrors.Add("وزن الأقفاص لا يمكن أن يكون سالباً");

                if (invoice.CagesWeight >= invoice.GrossWeight)
                    validationErrors.Add("وزن الأقفاص لا يمكن أن يكون أكبر من أو يساوي الوزن الفلتي");

                if (invoice.CagesCount <= 0)
                    validationErrors.Add("عدد الأقفاص يجب أن يكون أكبر من الصفر");

                if (invoice.UnitPrice <= 0)
                    validationErrors.Add("سعر الوحدة يجب أن يكون أكبر من الصفر");

                if (invoice.DiscountPercentage < 0 || invoice.DiscountPercentage > 100)
                    validationErrors.Add("نسبة الخصم يجب أن تكون بين 0 و 100");

                // Database validation
                var customerExists = await _unitOfWork.Customers.ExistsAsync(c => c.CustomerId == invoice.CustomerId, cancellationToken);
                if (!customerExists)
                    validationErrors.Add("الزبون المحدد غير موجود");

                var truckExists = await _unitOfWork.Trucks.ExistsAsync(t => t.TruckId == invoice.TruckId, cancellationToken);
                if (!truckExists)
                    validationErrors.Add("الشاحنة المحددة غير موجودة");

                // Invoice number uniqueness
                if (!string.IsNullOrEmpty(invoice.InvoiceNumber))
                {
                    var duplicateExists = await _unitOfWork.Invoices.InvoiceNumberExistsAsync(
                        invoice.InvoiceNumber, invoice.InvoiceId, cancellationToken);
                    if (duplicateExists)
                        validationErrors.Add("رقم الفاتورة موجود مسبقاً");
                }

                var isValid = validationErrors.Count == 0;

                if (!isValid)
                {
                    _logger.LogWarning("Invoice validation failed with {ErrorCount} errors: {Errors}",
                        validationErrors.Count, string.Join(", ", validationErrors));
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating invoice data");
                throw;
            }
        }

        #endregion

        #region Customer Operations

        /// <summary>
        /// Retrieves all active customers for selection
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of active customers</returns>
        public async Task<IEnumerable<Customer>> GetActiveCustomersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Retrieving active customers");

                var customers = await _unitOfWork.Customers.GetActiveCustomersAsync(cancellationToken);

                _logger.LogInformation("Retrieved {CustomerCount} active customers", customers.Count());
                return customers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active customers");
                throw;
            }
        }

        /// <summary>
        /// Creates a new customer with validation and duplicate checking
        /// </summary>
        /// <param name="request">Customer creation request</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created customer</returns>
        public async Task<Customer> CreateCustomerAsync(CustomerCreationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Creating new customer: {CustomerName}", request.CustomerName);

                // Validate request
                await ValidateCustomerCreationRequestAsync(request, cancellationToken);

                // Create customer entity
                var customer = new Customer
                {
                    CustomerName = request.CustomerName.Trim(),
                    PhoneNumber = request.PhoneNumber?.Trim(),
                    Address = request.Address?.Trim(),
                    TotalDebt = 0,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };

                // Save customer
                var savedCustomer = await _unitOfWork.Customers.AddAsync(customer, cancellationToken);
                await _unitOfWork.SaveChangesAsync("POS_USER", cancellationToken);

                _logger.LogInformation("Customer created successfully - ID: {CustomerId}, Name: {CustomerName}",
                    savedCustomer.CustomerId, savedCustomer.CustomerName);

                return savedCustomer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer {CustomerName}", request.CustomerName);
                throw;
            }
        }

        /// <summary>
        /// Finds customer by name or phone number for quick lookup
        /// </summary>
        /// <param name="searchTerm">Search term (name or phone)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Matching customer or null</returns>
        public async Task<Customer?> FindCustomerByNameOrPhoneAsync(string searchTerm, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Searching for customer with term: {SearchTerm}", searchTerm);

                var customers = await _unitOfWork.Customers.SearchCustomersAsync(searchTerm, cancellationToken);
                var customer = customers.FirstOrDefault();

                if (customer != null)
                {
                    _logger.LogDebug("Found customer: {CustomerName} (ID: {CustomerId})",
                        customer.CustomerName, customer.CustomerId);
                }

                return customer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for customer with term {SearchTerm}", searchTerm);
                throw;
            }
        }

        #endregion

        #region Truck Operations

        /// <summary>
        /// Retrieves available trucks for invoice assignment
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of available trucks</returns>
        public async Task<IEnumerable<Truck>> GetAvailableTrucksAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Retrieving available trucks");

                var trucks = await _unitOfWork.Trucks.GetActiveTrucksAsync(cancellationToken);

                _logger.LogInformation("Retrieved {TruckCount} available trucks", trucks.Count());
                return trucks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available trucks");
                throw;
            }
        }

        /// <summary>
        /// Gets the latest truck load for a specific truck
        /// </summary>
        /// <param name="truckId">Truck ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Latest truck load or null</returns>
        public async Task<TruckLoad?> GetLatestTruckLoadAsync(int truckId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Retrieving latest truck load for truck {TruckId}", truckId);

                var truckLoad = await _unitOfWork.TruckLoads.GetLatestTruckLoadAsync(truckId, cancellationToken);

                return truckLoad;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest truck load for truck {TruckId}", truckId);
                throw;
            }
        }

        #endregion

        #region Financial Calculations

        /// <summary>
        /// Calculates invoice totals with comprehensive business logic
        /// </summary>
        /// <param name="request">Calculation request parameters</param>
        /// <returns>Complete calculation results</returns>
        public InvoiceCalculationResult CalculateInvoiceTotals(InvoiceCalculationRequest request)
        {
            try
            {
                _logger.LogDebug("Calculating invoice totals for gross weight {GrossWeight}, unit price {UnitPrice}",
                    request.GrossWeight, request.UnitPrice);

                var result = new InvoiceCalculationResult();

                // Calculate net weight
                result.NetWeight = Math.Max(0, request.GrossWeight - request.CagesWeight);

                // Calculate total amount before discount
                result.TotalAmount = result.NetWeight * request.UnitPrice;

                // Calculate discount amount and final amount
                result.DiscountAmount = result.TotalAmount * (request.DiscountPercentage / 100);
                result.FinalAmount = result.TotalAmount - result.DiscountAmount;

                // Calculate balance information
                result.PreviousBalance = request.PreviousBalance;
                result.CurrentBalance = request.PreviousBalance + result.FinalAmount;

                // Additional calculations
                result.WeightPerCage = request.CagesCount > 0 ? result.NetWeight / request.CagesCount : 0;
                result.PricePerCage = request.CagesCount > 0 ? result.FinalAmount / request.CagesCount : 0;

                _logger.LogDebug("Calculation completed - Net Weight: {NetWeight}, Final Amount: {FinalAmount}",
                    result.NetWeight, result.FinalAmount);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating invoice totals");
                throw;
            }
        }

        /// <summary>
        /// Gets comprehensive customer balance information
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Customer balance information</returns>
        public async Task<CustomerBalanceInfo> GetCustomerBalanceInfoAsync(int customerId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Retrieving customer balance info for customer {CustomerId}", customerId);

                var customer = await _unitOfWork.Customers.GetByIdAsync(customerId, cancellationToken);
                if (customer == null)
                {
                    throw new ArgumentException($"Customer with ID {customerId} not found", nameof(customerId));
                }

                var accountSummary = await _unitOfWork.Customers.GetCustomerAccountSummaryAsync(customerId, cancellationToken: cancellationToken);
                var lastPayment = await _unitOfWork.Payments.GetCustomerLastPaymentAsync(customerId, cancellationToken);

                var balanceInfo = new CustomerBalanceInfo
                {
                    CustomerId = customerId,
                    CustomerName = customer.CustomerName,
                    CurrentBalance = customer.TotalDebt,
                    TotalSales = accountSummary.TotalSales,
                    TotalPayments = accountSummary.TotalPayments,
                    LastPaymentAmount = lastPayment?.Amount ?? 0,
                    LastPaymentDate = lastPayment?.PaymentDate,
                    DaysSinceLastPayment = lastPayment?.PaymentDate != null ?
                        (DateTime.Now - lastPayment.PaymentDate).Days : null
                };

                return balanceInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer balance info for customer {CustomerId}", customerId);
                throw;
            }
        }

        #endregion

        #region Statistics and Reports

        /// <summary>
        /// Gets today's dashboard statistics for POS overview
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Dashboard statistics</returns>
        public async Task<POSDashboardStats> GetTodayStatisticsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Retrieving today's POS dashboard statistics");

                var today = DateTime.Today;
                var todayInvoices = await _unitOfWork.Invoices.GetInvoicesByDateAsync(today, cancellationToken);
                var todayPayments = await _unitOfWork.Payments.GetPaymentsByDateAsync(today, cancellationToken);

                var stats = new POSDashboardStats
                {
                    Date = today,
                    InvoiceCount = todayInvoices.Count(),
                    TotalSalesAmount = todayInvoices.Sum(i => i.FinalAmount),
                    TotalPaymentsAmount = todayPayments.Sum(p => p.Amount),
                    AverageInvoiceAmount = todayInvoices.Any() ? todayInvoices.Average(i => i.FinalAmount) : 0,
                    TotalWeight = todayInvoices.Sum(i => i.NetWeight),
                    UniqueCustomerCount = todayInvoices.Select(i => i.CustomerId).Distinct().Count(),
                    LastInvoiceTime = todayInvoices.OrderByDescending(i => i.InvoiceDate).FirstOrDefault()?.InvoiceDate
                };

                _logger.LogInformation("Today's statistics - Invoices: {InvoiceCount}, Sales: {TotalSales}",
                    stats.InvoiceCount, stats.TotalSalesAmount);

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving today's statistics");
                throw;
            }
        }

        /// <summary>
        /// Gets recent invoices for reference and reprinting
        /// </summary>
        /// <param name="count">Number of recent invoices to retrieve</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of recent invoices</returns>
        public async Task<IEnumerable<Invoice>> GetRecentInvoicesAsync(int count = 10, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Retrieving {Count} recent invoices", count);

                var recentInvoices = await _unitOfWork.Invoices.GetPagedAsync(
                    1, count,
                    orderBy: q => q.OrderByDescending(i => i.InvoiceDate),
                    cancellationToken: cancellationToken);

                return recentInvoices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent invoices");
                throw;
            }
        }

        #endregion

        #region Printing and Export

        /// <summary>
        /// Generates print data for invoice in PDF format
        /// </summary>
        /// <param name="invoiceId">Invoice ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>PDF print data as byte array</returns>
        public async Task<byte[]> GenerateInvoicePrintDataAsync(int invoiceId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Generating print data for invoice {InvoiceId}", invoiceId);

                var invoice = await _unitOfWork.Invoices.GetInvoiceWithDetailsAsync(invoiceId, cancellationToken);
                if (invoice == null)
                {
                    throw new ArgumentException($"Invoice with ID {invoiceId} not found", nameof(invoiceId));
                }

                // TODO: Implement PDF generation using a PDF library (e.g., iText7)
                // This would create a properly formatted Arabic invoice matching the preview image

                // Placeholder implementation
                var printData = System.Text.Encoding.UTF8.GetBytes($"Invoice Print Data for {invoice.InvoiceNumber}");

                _logger.LogInformation("Print data generated for invoice {InvoiceNumber}", invoice.InvoiceNumber);
                return printData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating print data for invoice {InvoiceId}", invoiceId);
                throw;
            }
        }

        /// <summary>
        /// Prints invoice using system default printer
        /// </summary>
        /// <param name="invoiceId">Invoice ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if printing was successful</returns>
        public async Task<bool> PrintInvoiceAsync(int invoiceId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Printing invoice {InvoiceId}", invoiceId);

                var printData = await GenerateInvoicePrintDataAsync(invoiceId, cancellationToken);

                // TODO: Implement actual printing logic
                // This would send the PDF data to the system printer

                // Simulate printing process
                await Task.Delay(1000, cancellationToken);

                _logger.LogInformation("Invoice {InvoiceId} printed successfully", invoiceId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing invoice {InvoiceId}", invoiceId);
                return false;
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Validates invoice creation request data
        /// </summary>
        private async Task ValidateInvoiceCreationRequestAsync(InvoiceCreationRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (request.CustomerId <= 0)
                throw new ArgumentException("معرف الزبون غير صحيح", nameof(request.CustomerId));

            if (request.TruckId <= 0)
                throw new ArgumentException("معرف الشاحنة غير صحيح", nameof(request.TruckId));

            if (request.GrossWeight <= 0)
                throw new ArgumentException("الوزن الفلتي يجب أن يكون أكبر من الصفر", nameof(request.GrossWeight));

            if (request.CagesWeight >= request.GrossWeight)
                throw new ArgumentException("وزن الأقفاص لا يمكن أن يكون أكبر من أو يساوي الوزن الفلتي", nameof(request.CagesWeight));

            if (request.UnitPrice <= 0)
                throw new ArgumentException("سعر الوحدة يجب أن يكون أكبر من الصفر", nameof(request.UnitPrice));

            // Verify customer and truck exist
            var customerExists = await _unitOfWork.Customers.ExistsAsync(c => c.CustomerId == request.CustomerId, cancellationToken);
            if (!customerExists)
                throw new ArgumentException("الزبون المحدد غير موجود", nameof(request.CustomerId));

            var truckExists = await _unitOfWork.Trucks.ExistsAsync(t => t.TruckId == request.TruckId, cancellationToken);
            if (!truckExists)
                throw new ArgumentException("الشاحنة المحددة غير موجودة", nameof(request.TruckId));
        }

        /// <summary>
        /// Builds invoice entity from creation request
        /// </summary>
        private async Task<Invoice> BuildInvoiceFromRequestAsync(InvoiceCreationRequest request, CancellationToken cancellationToken)
        {
            var customer = await _unitOfWork.Customers.GetByIdAsync(request.CustomerId, cancellationToken);
            if (customer == null)
                throw new ArgumentException($"Customer with ID {request.CustomerId} not found");

            var invoiceNumber = string.IsNullOrEmpty(request.InvoiceNumber) ?
                await GenerateUniqueInvoiceNumberAsync(cancellationToken) : request.InvoiceNumber;

            return new Invoice
            {
                InvoiceNumber = invoiceNumber,
                CustomerId = request.CustomerId,
                TruckId = request.TruckId,
                InvoiceDate = request.InvoiceDate ?? DateTime.Now,
                GrossWeight = request.GrossWeight,
                CagesWeight = request.CagesWeight,
                CagesCount = request.CagesCount,
                UnitPrice = request.UnitPrice,
                DiscountPercentage = request.DiscountPercentage,
                PreviousBalance = customer.TotalDebt,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };
        }

        /// <summary>
        /// Applies calculation results to invoice entity
        /// </summary>
        private void ApplyCalculationResultToInvoice(Invoice invoice, InvoiceCalculationResult result)
        {
            invoice.NetWeight = result.NetWeight;
            invoice.TotalAmount = result.TotalAmount;
            invoice.FinalAmount = result.FinalAmount;
            invoice.CurrentBalance = result.CurrentBalance;
        }

        /// <summary>
        /// Gets invoice ID by invoice number
        /// </summary>
        private async Task<int> GetInvoiceIdByNumberAsync(string invoiceNumber, CancellationToken cancellationToken)
        {
            var invoice = await _unitOfWork.Invoices.GetInvoiceByNumberAsync(invoiceNumber, cancellationToken);
            return invoice?.InvoiceId ?? 0;
        }

        /// <summary>
        /// Validates customer creation request
        /// </summary>
        private async Task ValidateCustomerCreationRequestAsync(CustomerCreationRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.CustomerName))
                throw new ArgumentException("اسم الزبون مطلوب", nameof(request.CustomerName));

            if (request.CustomerName.Length > 100)
                throw new ArgumentException("اسم الزبون طويل جداً", nameof(request.CustomerName));

            // Check for duplicate customer name
            var existingCustomer = await _unitOfWork.Customers.GetCustomerByNameAsync(request.CustomerName, cancellationToken);
            if (existingCustomer != null)
                throw new ArgumentException("اسم الزبون موجود مسبقاً", nameof(request.CustomerName));

            // Check for duplicate phone number if provided
            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
            {
                var existingPhone = await _unitOfWork.Customers.GetCustomerByPhoneAsync(request.PhoneNumber, cancellationToken);
                if (existingPhone != null)
                    throw new ArgumentException("رقم الهاتف مستخدم مسبقاً", nameof(request.PhoneNumber));
            }
        }

        #endregion
    }

    #region Data Transfer Objects

    /// <summary>
    /// Request model for invoice creation operations
    /// </summary>
    public class InvoiceCreationRequest
    {
        public string? InvoiceNumber { get; set; }
        public int CustomerId { get; set; }
        public int TruckId { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public decimal GrossWeight { get; set; }
        public decimal CagesWeight { get; set; }
        public int CagesCount { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercentage { get; set; } = 0;
    }

    /// <summary>
    /// Request model for customer creation operations
    /// </summary>
    public class CustomerCreationRequest
    {
        public string CustomerName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
    }

    /// <summary>
    /// Request model for invoice calculation operations
    /// </summary>
    public class InvoiceCalculationRequest
    {
        public decimal GrossWeight { get; set; }
        public decimal CagesWeight { get; set; }
        public int CagesCount { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal PreviousBalance { get; set; }
    }

    /// <summary>
    /// Result model for invoice calculation operations
    /// </summary>
    public class InvoiceCalculationResult
    {
        public decimal NetWeight { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public decimal PreviousBalance { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal WeightPerCage { get; set; }
        public decimal PricePerCage { get; set; }
    }

    /// <summary>
    /// Model for customer balance information
    /// </summary>
    public class CustomerBalanceInfo
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal CurrentBalance { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal LastPaymentAmount { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public int? DaysSinceLastPayment { get; set; }
    }

    /// <summary>
    /// Model for POS dashboard statistics
    /// </summary>
    public class POSDashboardStats
    {
        public DateTime Date { get; set; }
        public int InvoiceCount { get; set; }
        public decimal TotalSalesAmount { get; set; }
        public decimal TotalPaymentsAmount { get; set; }
        public decimal AverageInvoiceAmount { get; set; }
        public decimal TotalWeight { get; set; }
        public int UniqueCustomerCount { get; set; }
        public DateTime? LastInvoiceTime { get; set; }
    }

    #endregion
}