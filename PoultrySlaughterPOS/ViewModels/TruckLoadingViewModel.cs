using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Models;
using PoultrySlaughterPOS.Services;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Windows;

namespace PoultrySlaughterPOS.ViewModels
{
    /// <summary>
    /// Enterprise-grade ViewModel for truck loading operations implementing comprehensive
    /// MVVM patterns with validation, error handling, real-time data binding, and advanced save functionality.
    /// Features enhanced debugging, transaction management, and robust error recovery mechanisms.
    /// </summary>
    public partial class TruckLoadingViewModel : ObservableValidator
    {
        #region Private Fields

        private readonly ITruckLoadingService _truckLoadingService;
        private readonly ILogger<TruckLoadingViewModel> _logger;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly Stopwatch _operationStopwatch = new();

        #endregion

        #region Observable Properties

        [ObservableProperty]
        private ObservableCollection<Truck> availableTrucks = new();

        [ObservableProperty]
        private ObservableCollection<TruckLoad> todaysTruckLoads = new();

        [ObservableProperty]
        private Truck? selectedTruck;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(0.01, 10000, ErrorMessage = "الوزن يجب أن يكون بين 0.01 و 10000 كيلو")]
        private decimal totalWeight;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Range(1, 1000, ErrorMessage = "عدد الأقفاص يجب أن يكون بين 1 و 1000")]
        private int cagesCount;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [StringLength(500, ErrorMessage = "الملاحظات يجب ألا تتجاوز 500 حرف")]
        private string notes = string.Empty;

        [ObservableProperty]
        private DateTime loadDate = DateTime.Today;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool isSaving;

        [ObservableProperty]
        private bool hasErrors;

        [ObservableProperty]
        private string statusMessage = "جاهز لتسجيل تحميل جديد";

        [ObservableProperty]
        private string validationSummary = string.Empty;

        [ObservableProperty]
        private TruckLoadSummary? loadSummary;

        [ObservableProperty]
        private decimal calculatedWeightPerCage;

        [ObservableProperty]
        private Visibility validationErrorsVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private Visibility successMessageVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private Visibility savingProgressVisibility = Visibility.Collapsed;

        [ObservableProperty]
        private string operationDuration = string.Empty;

        [ObservableProperty]
        private bool canSave = false;

        [ObservableProperty]
        private string lastSavedLoadId = string.Empty;

        #endregion

        #region Constructor

        public TruckLoadingViewModel(
            ITruckLoadingService truckLoadingService,
            ILogger<TruckLoadingViewModel> logger,
            IErrorHandlingService errorHandlingService)
        {
            _truckLoadingService = truckLoadingService ?? throw new ArgumentNullException(nameof(truckLoadingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));

            // Initialize validation
            ValidateAllProperties();

            // Setup property change handlers
            PropertyChanged += TruckLoadingViewModel_PropertyChanged;
            ErrorsChanged += TruckLoadingViewModel_ErrorsChanged;

            _logger.LogInformation("TruckLoadingViewModel initialized with enhanced save functionality and debugging capabilities");
        }

        #endregion

        #region Command Methods

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            try
            {
                _operationStopwatch.Restart();
                IsLoading = true;
                StatusMessage = "جاري تحميل البيانات...";

                await LoadAvailableTrucksAsync();
                await LoadTodaysTruckLoadsAsync();
                await LoadSummaryAsync();

                _operationStopwatch.Stop();
                OperationDuration = $"تم التحميل في {_operationStopwatch.ElapsedMilliseconds} مللي ثانية";
                StatusMessage = "تم تحميل البيانات بنجاح";

                _logger.LogInformation("Truck loading data loaded successfully in {ElapsedMs}ms", _operationStopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _operationStopwatch.Stop();
                StatusMessage = "خطأ في تحميل البيانات";
                _logger.LogError(ex, "Error loading truck loading data after {ElapsedMs}ms", _operationStopwatch.ElapsedMilliseconds);
                await _errorHandlingService.HandleExceptionAsync(ex, "LoadDataAsync");
                ShowErrorMessage("فشل في تحميل البيانات. يرجى المحاولة مرة أخرى.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand(CanExecute = nameof(CanExecuteSave))]
        private async Task SaveTruckLoadAsync()
        {
            _operationStopwatch.Restart();

            try
            {
                _logger.LogInformation("Starting truck load save operation for truck {TruckId} with weight {Weight}kg and {CagesCount} cages",
                    SelectedTruck?.TruckId, TotalWeight, CagesCount);

                IsSaving = true;
                SavingProgressVisibility = Visibility.Visible;
                StatusMessage = "جاري حفظ تحميل الشاحنة...";

                // Clear previous messages
                HideAllMessages();

                // Final validation before save
                await ValidateCurrentLoadAsync();

                if (HasErrors)
                {
                    _logger.LogWarning("Save operation cancelled due to validation errors");
                    StatusMessage = "لا يمكن الحفظ - يوجد أخطاء في البيانات";
                    return;
                }

                // Create truck load request
                var request = new TruckLoadRequest
                {
                    TruckId = SelectedTruck!.TruckId,
                    TotalWeight = TotalWeight,
                    CagesCount = CagesCount,
                    Notes = Notes,
                    LoadDate = LoadDate
                };

                _logger.LogDebug("Truck load request created: {@TruckLoadRequest}", request);

                // Save to database with enhanced error handling
                var savedLoad = await _truckLoadingService.CreateTruckLoadAsync(request);

                _operationStopwatch.Stop();
                OperationDuration = $"تم الحفظ في {_operationStopwatch.ElapsedMilliseconds} مللي ثانية";
                LastSavedLoadId = $"Load ID: {savedLoad.LoadId}";

                _logger.LogInformation("Truck load saved successfully with ID {LoadId} in {ElapsedMs}ms",
                    savedLoad.LoadId, _operationStopwatch.ElapsedMilliseconds);

                // Refresh data to show new load
                await LoadTodaysTruckLoadsAsync();
                await LoadSummaryAsync();
                await LoadAvailableTrucksAsync(); // Refresh as truck may no longer be available

                // Show success message
                ShowSuccessMessage($"تم حفظ تحميل الشاحنة بنجاح - رقم العملية: {savedLoad.LoadId}");

                // Reset form for next entry
                await Task.Delay(1500); // Show success message briefly
                ResetFormForNextEntry();

            }
            catch (ValidationException validationEx)
            {
                _operationStopwatch.Stop();
                _logger.LogWarning(validationEx, "Validation failed during save operation after {ElapsedMs}ms", _operationStopwatch.ElapsedMilliseconds);
                ShowErrorMessage($"خطأ في التحقق من البيانات: {validationEx.Message}");
                StatusMessage = "فشل في التحقق من البيانات";
            }
            catch (InvalidOperationException invalidOpEx)
            {
                _operationStopwatch.Stop();
                _logger.LogError(invalidOpEx, "Invalid operation during save after {ElapsedMs}ms", _operationStopwatch.ElapsedMilliseconds);
                ShowErrorMessage($"عملية غير صالحة: {invalidOpEx.Message}");
                StatusMessage = "عملية غير صالحة";
            }
            catch (Exception ex)
            {
                _operationStopwatch.Stop();
                _logger.LogError(ex, "Critical error during truck load save operation after {ElapsedMs}ms", _operationStopwatch.ElapsedMilliseconds);
                await _errorHandlingService.HandleExceptionAsync(ex, "SaveTruckLoadAsync");
                ShowErrorMessage("حدث خطأ غير متوقع أثناء الحفظ. يرجى المحاولة مرة أخرى أو الاتصال بالدعم الفني.");
                StatusMessage = "فشل في حفظ البيانات";
            }
            finally
            {
                IsSaving = false;
                SavingProgressVisibility = Visibility.Collapsed;
            }
        }

        [RelayCommand]
        private void ResetForm()
        {
            _logger.LogDebug("Resetting truck loading form");

            SelectedTruck = null;
            TotalWeight = 0;
            CagesCount = 0;
            Notes = string.Empty;
            LoadDate = DateTime.Today;
            CalculatedWeightPerCage = 0;
            ValidationSummary = string.Empty;
            LastSavedLoadId = string.Empty;
            OperationDuration = string.Empty;

            HideAllMessages();
            ClearErrors();
            UpdateCanSaveState();

            StatusMessage = "تم إعادة تعيين النموذج";
            _logger.LogDebug("Form reset completed");
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            _logger.LogDebug("Refreshing truck loading data");
            await LoadDataAsync();
        }

        [RelayCommand]
        private async Task ValidateCurrentLoadAsync()
        {
            try
            {
                _logger.LogDebug("Starting validation for current load data");

                // Validate all properties first
                ValidateAllProperties();

                if (SelectedTruck == null)
                {
                    ValidationSummary = "يجب اختيار الشاحنة";
                    HasErrors = true;
                    ValidationErrorsVisibility = Visibility.Visible;
                    UpdateCanSaveState();
                    return;
                }

                var request = new TruckLoadRequest
                {
                    TruckId = SelectedTruck.TruckId,
                    TotalWeight = TotalWeight,
                    CagesCount = CagesCount,
                    Notes = Notes,
                    LoadDate = LoadDate
                };

                _logger.LogDebug("Validating truck load request: {@TruckLoadRequest}", request);

                var validationResult = await _truckLoadingService.ValidateTruckLoadRequestAsync(request);

                if (!validationResult.IsValid)
                {
                    ValidationSummary = string.Join("\n", validationResult.ErrorMessages);
                    HasErrors = true;
                    ValidationErrorsVisibility = Visibility.Visible;
                    StatusMessage = "يوجد أخطاء في البيانات المدخلة";

                    _logger.LogWarning("Validation failed with {ErrorCount} errors: {Errors}",
                        validationResult.ErrorMessages.Count, string.Join(", ", validationResult.ErrorMessages));
                }
                else
                {
                    ValidationSummary = "جميع البيانات صحيحة ✓";
                    HasErrors = false;
                    ValidationErrorsVisibility = Visibility.Collapsed;
                    StatusMessage = "البيانات صالحة للحفظ";

                    _logger.LogDebug("Validation passed successfully");
                }

                UpdateCanSaveState();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during validation");
                ValidationSummary = "خطأ في التحقق من صحة البيانات";
                HasErrors = true;
                ValidationErrorsVisibility = Visibility.Visible;
                StatusMessage = "خطأ في عملية التحقق";
                UpdateCanSaveState();
            }
        }

        #endregion

        #region Private Methods

        private bool CanExecuteSave()
        {
            return CanSave && !IsSaving && !IsLoading && !HasErrors &&
                   SelectedTruck != null && TotalWeight > 0 && CagesCount > 0;
        }

        private void UpdateCanSaveState()
        {
            var canSaveNow = !HasErrors &&
                           SelectedTruck != null &&
                           TotalWeight > 0 &&
                           CagesCount > 0 &&
                           !string.IsNullOrWhiteSpace(SelectedTruck?.TruckNumber);

            if (CanSave != canSaveNow)
            {
                CanSave = canSaveNow;
                SaveTruckLoadCommand.NotifyCanExecuteChanged();
                _logger.LogDebug("CanSave state updated to {CanSave}", CanSave);
            }
        }

        private void ResetFormForNextEntry()
        {
            _logger.LogDebug("Resetting form for next entry while preserving date");

            SelectedTruck = null;
            TotalWeight = 0;
            CagesCount = 0;
            Notes = string.Empty;
            // Keep LoadDate as is for convenience
            CalculatedWeightPerCage = 0;
            ValidationSummary = string.Empty;

            HideAllMessages();
            ClearErrors();
            UpdateCanSaveState();
        }

        private async Task LoadAvailableTrucksAsync()
        {
            try
            {
                var trucks = await _truckLoadingService.GetAvailableTrucksAsync();
                AvailableTrucks.Clear();
                foreach (var truck in trucks)
                {
                    AvailableTrucks.Add(truck);
                }

                _logger.LogDebug("Loaded {Count} available trucks", AvailableTrucks.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading available trucks");
                throw;
            }
        }

        private async Task LoadTodaysTruckLoadsAsync()
        {
            try
            {
                var loads = await _truckLoadingService.GetTodaysTruckLoadsAsync();
                TodaysTruckLoads.Clear();
                foreach (var load in loads)
                {
                    TodaysTruckLoads.Add(load);
                }

                _logger.LogDebug("Loaded {Count} today's truck loads", TodaysTruckLoads.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading today's truck loads");
                throw;
            }
        }

        private async Task LoadSummaryAsync()
        {
            try
            {
                LoadSummary = await _truckLoadingService.GetLoadSummaryAsync(LoadDate);
                _logger.LogDebug("Loaded summary for date {Date}: {Summary}", LoadDate, LoadSummary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading summary");
                throw;
            }
        }

        private void UpdateCalculatedWeightPerCage()
        {
            if (CagesCount > 0 && TotalWeight > 0)
            {
                CalculatedWeightPerCage = TotalWeight / CagesCount;
                _logger.LogDebug("Weight per cage calculated: {WeightPerCage:F2}kg", CalculatedWeightPerCage);
            }
            else
            {
                CalculatedWeightPerCage = 0;
            }
        }

        private void ShowSuccessMessage(string message)
        {
            StatusMessage = message;
            SuccessMessageVisibility = Visibility.Visible;
            ValidationErrorsVisibility = Visibility.Collapsed;

            _logger.LogInformation("Success message displayed: {Message}", message);

            // Auto-hide success message after 4 seconds
            Task.Delay(4000).ContinueWith(_ =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SuccessMessageVisibility = Visibility.Collapsed;
                    if (StatusMessage == message) // Only clear if it hasn't changed
                    {
                        StatusMessage = "جاهز لتسجيل تحميل جديد";
                    }
                });
            });
        }

        private void ShowErrorMessage(string message)
        {
            ValidationSummary = message;
            HasErrors = true;
            ValidationErrorsVisibility = Visibility.Visible;
            SuccessMessageVisibility = Visibility.Collapsed;

            _logger.LogWarning("Error message displayed: {Message}", message);
        }

        private void HideAllMessages()
        {
            ValidationErrorsVisibility = Visibility.Collapsed;
            SuccessMessageVisibility = Visibility.Collapsed;
            SavingProgressVisibility = Visibility.Collapsed;
        }

        #endregion

        #region Event Handlers

        private void TruckLoadingViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(TotalWeight):
                case nameof(CagesCount):
                    UpdateCalculatedWeightPerCage();
                    _ = ValidateCurrentLoadAsync();
                    break;

                case nameof(SelectedTruck):
                    _ = ValidateCurrentLoadAsync();
                    break;

                case nameof(LoadDate):
                    _ = LoadSummaryAsync();
                    break;

                case nameof(HasErrors):
                    UpdateCanSaveState();
                    break;
            }
        }

        private void TruckLoadingViewModel_ErrorsChanged(object? sender, System.ComponentModel.DataErrorsChangedEventArgs e)
        {
            HasErrors = HasErrors;
            UpdateCanSaveState();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize the ViewModel with comprehensive data loading and debugging setup
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing TruckLoadingViewModel with enhanced save capabilities");
                await LoadDataAsync();
                UpdateCanSaveState();
                _logger.LogInformation("TruckLoadingViewModel initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error initializing TruckLoadingViewModel");
                await _errorHandlingService.HandleExceptionAsync(ex, "InitializeAsync");
            }
        }

        /// <summary>
        /// Cleanup resources and event handlers
        /// </summary>
        public void Cleanup()
        {
            AvailableTrucks.Clear();
            TodaysTruckLoads.Clear();
            PropertyChanged -= TruckLoadingViewModel_PropertyChanged;
            ErrorsChanged -= TruckLoadingViewModel_ErrorsChanged;
            _operationStopwatch.Stop();
            _logger.LogDebug("TruckLoadingViewModel cleanup completed");
        }

        #endregion

        #region Computed Properties

        /// <summary>
        /// Indicates if there are any trucks available for loading
        /// </summary>
        public bool HasAvailableTrucks => AvailableTrucks.Any();

        /// <summary>
        /// Indicates if there are any loads recorded for today
        /// </summary>
        public bool HasTodaysLoads => TodaysTruckLoads.Any();

        /// <summary>
        /// Current efficiency percentage based on weight per cage
        /// </summary>
        public double EfficiencyPercentage
        {
            get
            {
                if (CalculatedWeightPerCage == 0) return 0;

                // Optimal weight per cage is considered 20-30 kg
                const double optimalWeight = 25.0;
                var difference = Math.Abs((double)CalculatedWeightPerCage - optimalWeight);
                var efficiency = Math.Max(0, 100 - (difference * 2));
                return Math.Min(100, efficiency);
            }
        }

        /// <summary>
        /// Color indicator for weight per cage efficiency
        /// </summary>
        public string EfficiencyColor
        {
            get
            {
                var efficiency = EfficiencyPercentage;
                return efficiency switch
                {
                    >= 80 => "Green",
                    >= 60 => "Orange",
                    _ => "Red"
                };
            }
        }

        /// <summary>
        /// Comprehensive debugging information for development and troubleshooting
        /// </summary>
        public string DebugInfo => $"CanSave: {CanSave}, HasErrors: {HasErrors}, IsSaving: {IsSaving}, " +
                                  $"IsLoading: {IsLoading}, SelectedTruck: {SelectedTruck?.TruckNumber ?? "None"}, " +
                                  $"TotalWeight: {TotalWeight}, CagesCount: {CagesCount}, " +
                                  $"AvailableTrucks: {AvailableTrucks.Count}, TodaysLoads: {TodaysTruckLoads.Count}";

        #endregion
    }
}