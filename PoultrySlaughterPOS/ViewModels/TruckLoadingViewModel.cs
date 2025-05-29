using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Models;
using PoultrySlaughterPOS.Services;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Windows;

namespace PoultrySlaughterPOS.ViewModels
{
    /// <summary>
    /// Enterprise-grade ViewModel for truck loading operations implementing comprehensive
    /// MVVM patterns with validation, error handling, and real-time data binding.
    /// Save functionality has been architecturally removed while maintaining data integrity.
    /// Inherits from ObservableValidator for proper validation framework integration.
    /// </summary>
    public partial class TruckLoadingViewModel : ObservableValidator
    {
        #region Private Fields

        private readonly ITruckLoadingService _truckLoadingService;
        private readonly ILogger<TruckLoadingViewModel> _logger;
        private readonly IErrorHandlingService _errorHandlingService;

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
        private bool hasErrors;

        [ObservableProperty]
        private string statusMessage = "عرض معلومات تحميل الشاحنات";

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

            _logger.LogInformation("TruckLoadingViewModel initialized in view-only mode (save functionality disabled)");
        }

        #endregion

        #region Command Methods

        [RelayCommand]
        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "جاري تحميل البيانات...";

                await LoadAvailableTrucksAsync();
                await LoadTodaysTruckLoadsAsync();
                await LoadSummaryAsync();

                StatusMessage = "تم تحميل البيانات بنجاح";
                _logger.LogInformation("Truck loading data loaded successfully");
            }
            catch (Exception ex)
            {
                StatusMessage = "خطأ في تحميل البيانات";
                _logger.LogError(ex, "Error loading truck loading data");
                await _errorHandlingService.HandleExceptionAsync(ex, "LoadDataAsync");
                ShowErrorMessage("فشل في تحميل البيانات. يرجى المحاولة مرة أخرى.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void ResetForm()
        {
            SelectedTruck = null;
            TotalWeight = 0;
            CagesCount = 0;
            Notes = string.Empty;
            LoadDate = DateTime.Today;
            CalculatedWeightPerCage = 0;
            ValidationSummary = string.Empty;
            ValidationErrorsVisibility = Visibility.Collapsed;
            SuccessMessageVisibility = Visibility.Collapsed;

            // Clear validation errors
            ClearErrors();

            StatusMessage = "تم إعادة تعيين النموذج";
            _logger.LogDebug("Form reset completed");
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadDataAsync();
        }

        [RelayCommand]
        private async Task ValidateCurrentLoadAsync()
        {
            try
            {
                // Validate all properties first
                ValidateAllProperties();

                if (SelectedTruck == null)
                {
                    ValidationSummary = "يجب اختيار الشاحنة";
                    HasErrors = true;
                    ValidationErrorsVisibility = Visibility.Visible;
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

                var validationResult = await _truckLoadingService.ValidateTruckLoadRequestAsync(request);

                if (!validationResult.IsValid)
                {
                    ValidationSummary = string.Join("\n", validationResult.ErrorMessages);
                    HasErrors = true;
                    ValidationErrorsVisibility = Visibility.Visible;
                    StatusMessage = "يوجد أخطاء في البيانات المدخلة";
                }
                else
                {
                    ValidationSummary = "جميع البيانات صحيحة ✓";
                    HasErrors = false;
                    ValidationErrorsVisibility = Visibility.Collapsed;
                    StatusMessage = "البيانات صالحة للحفظ";
                    ShowSuccessMessage("تم التحقق من صحة البيانات بنجاح");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating current load");
                ValidationSummary = "خطأ في التحقق من صحة البيانات";
                HasErrors = true;
                ValidationErrorsVisibility = Visibility.Visible;
                StatusMessage = "خطأ في عملية التحقق";
            }
        }

        #endregion

        #region Private Methods

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
                _logger.LogDebug("Loaded summary for date {Date}", LoadDate);
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

            // Auto-hide success message after 3 seconds
            Task.Delay(3000).ContinueWith(_ =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SuccessMessageVisibility = Visibility.Collapsed;
                });
            });
        }

        private void ShowErrorMessage(string message)
        {
            ValidationSummary = message;
            HasErrors = true;
            ValidationErrorsVisibility = Visibility.Visible;
            SuccessMessageVisibility = Visibility.Collapsed;
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
            }
        }

        private void TruckLoadingViewModel_ErrorsChanged(object? sender, System.ComponentModel.DataErrorsChangedEventArgs e)
        {
            HasErrors = HasErrors;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize the ViewModel with data loading
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                await LoadDataAsync();
                _logger.LogInformation("TruckLoadingViewModel initialized successfully in view-only mode");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing TruckLoadingViewModel");
                await _errorHandlingService.HandleExceptionAsync(ex, "InitializeAsync");
            }
        }

        /// <summary>
        /// Cleanup resources when ViewModel is disposed
        /// </summary>
        public void Cleanup()
        {
            AvailableTrucks.Clear();
            TodaysTruckLoads.Clear();
            PropertyChanged -= TruckLoadingViewModel_PropertyChanged;
            ErrorsChanged -= TruckLoadingViewModel_ErrorsChanged;
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

        #endregion
    }
}