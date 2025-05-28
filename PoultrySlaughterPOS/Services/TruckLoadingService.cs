using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Models;
using PoultrySlaughterPOS.Services.Repositories;
using System.ComponentModel.DataAnnotations;

namespace PoultrySlaughterPOS.Services
{
    /// <summary>
    /// Enterprise-grade truck loading service providing comprehensive business logic
    /// for truck loading operations with validation, concurrency control, and audit trails
    /// </summary>
    public interface ITruckLoadingService
    {
        Task<IEnumerable<Truck>> GetAvailableTrucksAsync(CancellationToken cancellationToken = default);
        Task<TruckLoad?> GetTruckCurrentLoadAsync(int truckId, CancellationToken cancellationToken = default);
        Task<TruckLoad> CreateTruckLoadAsync(TruckLoadRequest request, CancellationToken cancellationToken = default);
        Task<bool> UpdateTruckLoadAsync(int loadId, TruckLoadRequest request, CancellationToken cancellationToken = default);
        Task<bool> CompleteTruckLoadAsync(int loadId, CancellationToken cancellationToken = default);
        Task<IEnumerable<TruckLoad>> GetTodaysTruckLoadsAsync(CancellationToken cancellationToken = default);
        Task<ValidationResult> ValidateTruckLoadRequestAsync(TruckLoadRequest request, CancellationToken cancellationToken = default);
        Task<TruckLoadSummary> GetLoadSummaryAsync(DateTime date, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Request model for truck loading operations with comprehensive validation
    /// </summary>
    public class TruckLoadRequest
    {
        [Required(ErrorMessage = "يجب اختيار الشاحنة")]
        [Range(1, int.MaxValue, ErrorMessage = "يجب اختيار شاحنة صالحة")]
        public int TruckId { get; set; }

        [Required(ErrorMessage = "يجب إدخال الوزن الإجمالي")]
        [Range(0.01, 10000, ErrorMessage = "الوزن يجب أن يكون بين 0.01 و 10000 كيلو")]
        public decimal TotalWeight { get; set; }

        [Required(ErrorMessage = "يجب إدخال عدد الأقفاص")]
        [Range(1, 1000, ErrorMessage = "عدد الأقفاص يجب أن يكون بين 1 و 1000")]
        public int CagesCount { get; set; }

        [StringLength(500, ErrorMessage = "الملاحظات يجب ألا تتجاوز 500 حرف")]
        public string? Notes { get; set; }

        public DateTime LoadDate { get; set; } = DateTime.Today;
    }

    /// <summary>
    /// Summary model for truck loading operations
    /// </summary>
    public class TruckLoadSummary
    {
        public int TotalTrucks { get; set; }
        public int LoadedTrucks { get; set; }
        public int AvailableTrucks { get; set; }
        public decimal TotalWeight { get; set; }
        public int TotalCages { get; set; }
        public decimal AverageWeightPerCage { get; set; }
        public DateTime SummaryDate { get; set; }
    }

    public class TruckLoadingService : ITruckLoadingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TruckLoadingService> _logger;
        private readonly IErrorHandlingService _errorHandlingService;

        public TruckLoadingService(
            IUnitOfWork unitOfWork,
            ILogger<TruckLoadingService> logger,
            IErrorHandlingService errorHandlingService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));
        }

        public async Task<IEnumerable<Truck>> GetAvailableTrucksAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Retrieving available trucks for loading");

                var availableTrucks = new List<Truck>();
                var activeTrucks = await _unitOfWork.Trucks.GetActiveTrucksAsync(cancellationToken);

                foreach (var truck in activeTrucks)
                {
                    var isAvailable = await _unitOfWork.Trucks.IsTruckAvailableForLoadingAsync(truck.TruckId, cancellationToken);
                    if (isAvailable)
                    {
                        availableTrucks.Add(truck);
                    }
                }

                _logger.LogInformation("Found {Count} available trucks for loading", availableTrucks.Count);
                return availableTrucks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available trucks");
                await _errorHandlingService.HandleExceptionAsync(ex, "GetAvailableTrucksAsync");
                throw;
            }
        }

        public async Task<TruckLoad?> GetTruckCurrentLoadAsync(int truckId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _unitOfWork.TruckLoads.GetTruckCurrentLoadAsync(truckId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current load for truck {TruckId}", truckId);
                await _errorHandlingService.HandleExceptionAsync(ex, "GetTruckCurrentLoadAsync");
                throw;
            }
        }

        public async Task<TruckLoad> CreateTruckLoadAsync(TruckLoadRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Creating truck load for truck {TruckId} with weight {Weight} and {CagesCount} cages",
                    request.TruckId, request.TotalWeight, request.CagesCount);

                // Validate the request
                var validationResult = await ValidateTruckLoadRequestAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var errorMessage = string.Join("; ", validationResult.ErrorMessages);
                    throw new ValidationException($"Validation failed: {errorMessage}");
                }

                // Begin transaction for data consistency
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                try
                {
                    // Create the truck load entity
                    var truckLoad = new TruckLoad
                    {
                        TruckId = request.TruckId,
                        LoadDate = request.LoadDate,
                        TotalWeight = request.TotalWeight,
                        CagesCount = request.CagesCount,
                        Notes = request.Notes,
                        Status = "LOADED",
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now
                    };

                    // Save the truck load
                    var createdLoad = await _unitOfWork.TruckLoads.AddAsync(truckLoad, cancellationToken);
                    await _unitOfWork.SaveChangesAsync("SYSTEM", cancellationToken);

                    // Commit the transaction
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);

                    _logger.LogInformation("Successfully created truck load {LoadId} for truck {TruckId}",
                        createdLoad.LoadId, request.TruckId);

                    return createdLoad;
                }
                catch (Exception)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating truck load for truck {TruckId}", request.TruckId);
                await _errorHandlingService.HandleExceptionAsync(ex, "CreateTruckLoadAsync");
                throw;
            }
        }

        public async Task<bool> UpdateTruckLoadAsync(int loadId, TruckLoadRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Updating truck load {LoadId}", loadId);

                // Validate the request
                var validationResult = await ValidateTruckLoadRequestAsync(request, cancellationToken);
                if (!validationResult.IsValid)
                {
                    var errorMessage = string.Join("; ", validationResult.ErrorMessages);
                    throw new ValidationException($"Validation failed: {errorMessage}");
                }

                // Get the existing load
                var existingLoad = await _unitOfWork.TruckLoads.GetByIdAsync(loadId, cancellationToken);
                if (existingLoad == null)
                {
                    _logger.LogWarning("Truck load {LoadId} not found for update", loadId);
                    return false;
                }

                // Update the load properties
                existingLoad.TotalWeight = request.TotalWeight;
                existingLoad.CagesCount = request.CagesCount;
                existingLoad.Notes = request.Notes;
                existingLoad.UpdatedDate = DateTime.Now;

                // Save changes
                await _unitOfWork.TruckLoads.UpdateAsync(existingLoad, cancellationToken);
                var rowsAffected = await _unitOfWork.SaveChangesAsync("SYSTEM", cancellationToken);

                _logger.LogInformation("Successfully updated truck load {LoadId}", loadId);
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating truck load {LoadId}", loadId);
                await _errorHandlingService.HandleExceptionAsync(ex, "UpdateTruckLoadAsync");
                throw;
            }
        }

        public async Task<bool> CompleteTruckLoadAsync(int loadId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Completing truck load {LoadId}", loadId);

                var success = await _unitOfWork.TruckLoads.UpdateLoadStatusAsync(loadId, "COMPLETED", cancellationToken);

                if (success)
                {
                    _logger.LogInformation("Successfully completed truck load {LoadId}", loadId);
                }
                else
                {
                    _logger.LogWarning("Failed to complete truck load {LoadId}", loadId);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing truck load {LoadId}", loadId);
                await _errorHandlingService.HandleExceptionAsync(ex, "CompleteTruckLoadAsync");
                throw;
            }
        }

        public async Task<IEnumerable<TruckLoad>> GetTodaysTruckLoadsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _unitOfWork.TruckLoads.GetTruckLoadsByDateAsync(DateTime.Today, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving today's truck loads");
                await _errorHandlingService.HandleExceptionAsync(ex, "GetTodaysTruckLoadsAsync");
                throw;
            }
        }

        public async Task<ValidationResult> ValidateTruckLoadRequestAsync(TruckLoadRequest request, CancellationToken cancellationToken = default)
        {
            var result = new ValidationResult();

            try
            {
                // Basic model validation
                var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(request);
                var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

                if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
                {
                    foreach (var validationError in validationResults)
                    {
                        result.ErrorMessages.Add(validationError.ErrorMessage ?? "خطأ في التحقق من صحة البيانات");
                    }
                }

                // Business rule validations
                if (request.TruckId > 0)
                {
                    // Check if truck exists and is active
                    var truck = await _unitOfWork.Trucks.GetByIdAsync(request.TruckId, cancellationToken);
                    if (truck == null)
                    {
                        result.ErrorMessages.Add("الشاحنة المحددة غير موجودة");
                    }
                    else if (!truck.IsActive)
                    {
                        result.ErrorMessages.Add("الشاحنة المحددة غير نشطة");
                    }
                    else
                    {
                        // Check if truck is available for loading
                        var isAvailable = await _unitOfWork.Trucks.IsTruckAvailableForLoadingAsync(request.TruckId, cancellationToken);
                        if (!isAvailable)
                        {
                            result.ErrorMessages.Add("الشاحنة محملة بالفعل أو في حالة نقل");
                        }
                    }
                }

                // Weight validation
                if (request.TotalWeight > 0 && request.CagesCount > 0)
                {
                    var weightPerCage = request.TotalWeight / request.CagesCount;
                    if (weightPerCage < 5 || weightPerCage > 100)
                    {
                        result.ErrorMessages.Add($"متوسط وزن القفص ({weightPerCage:F2} كيلو) غير طبيعي. يجب أن يكون بين 5 و 100 كيلو");
                    }
                }

                // Date validation
                if (request.LoadDate.Date > DateTime.Today)
                {
                    result.ErrorMessages.Add("لا يمكن تسجيل تحميل في المستقبل");
                }

                result.IsValid = !result.ErrorMessages.Any();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating truck load request");
                result.ErrorMessages.Add("خطأ في التحقق من صحة البيانات");
                result.IsValid = false;
                return result;
            }
        }

        public async Task<TruckLoadSummary> GetLoadSummaryAsync(DateTime date, CancellationToken cancellationToken = default)
        {
            try
            {
                var loads = await _unitOfWork.TruckLoads.GetTruckLoadsByDateAsync(date, cancellationToken);
                var allTrucks = await _unitOfWork.Trucks.GetActiveTrucksAsync(cancellationToken);

                var summary = new TruckLoadSummary
                {
                    SummaryDate = date,
                    TotalTrucks = allTrucks.Count(),
                    LoadedTrucks = loads.Count(),
                    AvailableTrucks = allTrucks.Count() - loads.Count(),
                    TotalWeight = loads.Sum(l => l.TotalWeight),
                    TotalCages = loads.Sum(l => l.CagesCount)
                };

                if (summary.TotalCages > 0)
                {
                    summary.AverageWeightPerCage = summary.TotalWeight / summary.TotalCages;
                }

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating load summary for date {Date}", date);
                await _errorHandlingService.HandleExceptionAsync(ex, "GetLoadSummaryAsync");
                throw;
            }
        }
    }

    /// <summary>
    /// Validation result container for truck loading operations
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> ErrorMessages { get; set; } = new List<string>();
    }
}