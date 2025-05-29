using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace PoultrySlaughterPOS.Views
{
    /// <summary>
    /// Enterprise-grade WPF UserControl for truck loading operations with proper MVVM pattern implementation.
    /// Follows modern WPF best practices with dependency injection and comprehensive error handling.
    /// Architectural modification: Save operations removed, focusing on read-only display and validation.
    /// </summary>
    public partial class TruckLoadingView : UserControl
    {
        private readonly ILogger<TruckLoadingView> _logger;
        private TruckLoadingViewModel? _viewModel;

        /// <summary>
        /// Constructor for dependency injection container with enhanced logging
        /// </summary>
        public TruckLoadingView(TruckLoadingViewModel viewModel, ILogger<TruckLoadingView> logger)
        {
            InitializeComponent();

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            DataContext = _viewModel;

            _logger.LogDebug("TruckLoadingView initialized with ViewModel in read-only mode (save operations disabled)");
        }

        /// <summary>
        /// Default constructor for XAML designer support
        /// </summary>
        public TruckLoadingView()
        {
            InitializeComponent();

            // Design-time support
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                // Create mock data for design time
                DataContext = CreateDesignTimeViewModel();
            }
        }

        /// <summary>
        /// Handles the UserControl loaded event to initialize data
        /// </summary>
        private async void TruckLoadingView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel != null && !System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
                {
                    _logger.LogInformation("TruckLoadingView loaded, initializing ViewModel for read-only operations");
                    await _viewModel.InitializeAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during TruckLoadingView initialization");
                MessageBox.Show($"خطأ في تحميل صفحة تحميل الشاحنات:\n{ex.Message}",
                               "خطأ",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the UserControl unloaded event for cleanup
        /// </summary>
        private void TruckLoadingView_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel?.Cleanup();
                _logger.LogDebug("TruckLoadingView unloaded and cleaned up");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during TruckLoadingView cleanup");
            }
        }

        /// <summary>
        /// Creates a design-time ViewModel with mock data for XAML designer
        /// Updated to reflect read-only nature of the interface
        /// </summary>
        private static object CreateDesignTimeViewModel()
        {
            return new
            {
                AvailableTrucks = new[]
                {
                    new { TruckNumber = "TR-001", DriverName = "أحمد محمد" },
                    new { TruckNumber = "TR-002", DriverName = "محمد علي" }
                },
                TodaysTruckLoads = new[]
                {
                    new {
                        Truck = new { TruckNumber = "TR-001", DriverName = "أحمد محمد" },
                        TotalWeight = 1250.50m,
                        CagesCount = 50,
                        CreatedDate = DateTime.Now,
                        Status = "LOADED"
                    }
                },
                LoadSummary = new
                {
                    TotalTrucks = 3,
                    LoadedTrucks = 1,
                    AvailableTrucks = 2,
                    TotalWeight = 1250.50m,
                    TotalCages = 50,
                    AverageWeightPerCage = 25.01m
                },
                StatusMessage = "عرض معلومات تحميل الشاحنات (وضع القراءة فقط)",
                IsLoading = false,
                ValidationErrorsVisibility = Visibility.Collapsed,
                SuccessMessageVisibility = Visibility.Collapsed,
                HasErrors = false
            };
        }

        /// <summary>
        /// Provides access to the ViewModel for external components
        /// </summary>
        public TruckLoadingViewModel? ViewModel => _viewModel;

        /// <summary>
        /// Updates the ViewModel if needed (for navigation scenarios)
        /// </summary>
        /// <param name="viewModel">New ViewModel instance</param>
        public void UpdateViewModel(TruckLoadingViewModel viewModel)
        {
            if (viewModel == null)
                throw new ArgumentNullException(nameof(viewModel));

            _viewModel?.Cleanup();
            _viewModel = viewModel;
            DataContext = _viewModel;

            _logger.LogDebug("TruckLoadingView ViewModel updated");
        }

        /// <summary>
        /// Forces a refresh of the current view data
        /// </summary>
        public async Task RefreshAsync()
        {
            try
            {
                if (_viewModel != null)
                {
                    await _viewModel.RefreshCommand.ExecuteAsync(null);
                    _logger.LogDebug("TruckLoadingView refreshed successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing TruckLoadingView");
                throw;
            }
        }

        /// <summary>
        /// Validates current data without saving (read-only validation)
        /// </summary>
        public async Task ValidateDataAsync()
        {
            try
            {
                if (_viewModel != null)
                {
                    await _viewModel.ValidateCurrentLoadCommand.ExecuteAsync(null);
                    _logger.LogDebug("TruckLoadingView data validation completed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating data in TruckLoadingView");
                throw;
            }
        }

        /// <summary>
        /// Gets current validation status from the ViewModel
        /// </summary>
        public bool IsDataValid => _viewModel?.HasErrors == false;

        /// <summary>
        /// Gets current validation summary for external use
        /// </summary>
        public string GetValidationSummary() => _viewModel?.ValidationSummary ?? "No validation information available";
    }
}