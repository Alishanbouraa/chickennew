using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace PoultrySlaughterPOS.Views
{
    /// <summary>
    /// Enterprise-grade WPF UserControl for truck loading operations with comprehensive MVVM implementation,
    /// advanced error handling, debugging capabilities, and robust save functionality.
    /// Implements modern WPF patterns with dependency injection and enterprise-level logging.
    /// </summary>
    public partial class TruckLoadingView : UserControl
    {
        private readonly ILogger<TruckLoadingView> _logger;
        private TruckLoadingViewModel? _viewModel;

        /// <summary>
        /// Constructor for dependency injection container with enhanced logging and debugging support
        /// </summary>
        public TruckLoadingView(TruckLoadingViewModel viewModel, ILogger<TruckLoadingView> logger)
        {
            InitializeComponent();

            // FIXED: Initialize logger field to resolve CS8618 warning
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            DataContext = _viewModel;

            // Enable debug features in debug builds
            EnableDebugFeaturesInDebugBuild();

            _logger.LogDebug("TruckLoadingView initialized with enhanced save functionality and debug capabilities");
        }

        /// <summary>
        /// Default constructor for XAML designer support
        /// </summary>
        public TruckLoadingView()
        {
            InitializeComponent();

            // FIXED: Initialize logger field for designer support
            _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<TruckLoadingView>.Instance;

            // Design-time support
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                // Create enhanced mock data for design time
                DataContext = CreateEnhancedDesignTimeViewModel();
            }
        }

        /// <summary>
        /// Handles the UserControl loaded event to initialize data with enhanced error handling
        /// </summary>
        private async void TruckLoadingView_Loaded(object sender, RoutedEventArgs e)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (_viewModel != null && !System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
                {
                    _logger.LogInformation("TruckLoadingView loaded, initializing ViewModel with save functionality");

                    // Subscribe to ViewModel events for enhanced debugging
                    SubscribeToViewModelEvents();

                    await _viewModel.InitializeAsync();

                    stopwatch.Stop();
                    _logger.LogInformation("TruckLoadingView initialization completed successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Critical error during TruckLoadingView initialization after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

                MessageBox.Show($"خطأ حرج في تحميل صفحة تحميل الشاحنات:\n{ex.Message}\n\nتفاصيل إضافية متوفرة في سجلات النظام.",
                               "خطأ حرج",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);

                // Attempt graceful degradation
                await AttemptGracefulDegradation();
            }
        }
        /// <summary>
        /// Sets the ViewModel for this view and establishes data binding
        /// ADDED: Missing SetViewModel method required by MainWindow
        /// </summary>
        /// <param name="viewModel">TruckLoadingViewModel instance</param>
        public void SetViewModel(TruckLoadingViewModel viewModel)
        {
            try
            {
                _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
                DataContext = _viewModel;

                // Subscribe to ViewModel events
                SubscribeToViewModelEvents();

                _logger.LogInformation("TruckLoadingView ViewModel set successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting ViewModel for TruckLoadingView");
                throw;
            }
        }

        /// <summary>
        /// Asynchronously initializes the view with comprehensive data loading
        /// ADDED: Missing InitializeAsync method required by MainWindow
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                if (_viewModel != null && !System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
                {
                    _logger.LogInformation("TruckLoadingView async initialization started");

                    await _viewModel.InitializeAsync();

                    _logger.LogInformation("TruckLoadingView async initialization completed successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during TruckLoadingView async initialization");
                throw;
            }
        }

        /// <summary>
        /// Comprehensive cleanup method for resource disposal
        /// ADDED: Missing Cleanup method required by MainWindow
        /// </summary>
        public void Cleanup()
        {
            try
            {
                _logger.LogDebug("TruckLoadingView cleanup initiated");

                // Unsubscribe from ViewModel events
                UnsubscribeFromViewModelEvents();

                // Cleanup ViewModel
                _viewModel?.Cleanup();

                _logger.LogDebug("TruckLoadingView cleanup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during TruckLoadingView cleanup");
            }
        }
        /// <summary>
        /// Handles the UserControl unloaded event for comprehensive cleanup
        /// </summary>
        private void TruckLoadingView_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Unsubscribe from ViewModel events
                UnsubscribeFromViewModelEvents();

                _viewModel?.Cleanup();
                _logger.LogDebug("TruckLoadingView unloaded and cleaned up successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Non-critical error during TruckLoadingView cleanup");
            }
        }

        /// <summary>
        /// Handles debug button click for comprehensive debugging information display
        /// </summary>
        private async void DebugButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel == null)
                {
                    ShowDebugMessage("ViewModel is null - critical error state");
                    return;
                }

                var debugInfo = GenerateComprehensiveDebugReport();

                _logger.LogDebug("Debug information requested: {DebugInfo}", debugInfo);

                // Display debug information in a formatted dialog
                var debugWindow = new Window
                {
                    Title = "معلومات التشخيص - Truck Loading Debug Info",
                    Width = 800,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = Window.GetWindow(this),
                    Content = new ScrollViewer
                    {
                        Content = new TextBlock
                        {
                            Text = debugInfo,
                            Margin = new Thickness(20),
                            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                            FontSize = 12,
                            TextWrapping = TextWrapping.Wrap
                        }
                    }
                };

                debugWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error displaying debug information");
                ShowDebugMessage($"Error in debug display: {ex.Message}");
            }
        }

        #region Private Methods

        /// <summary>
        /// Enables debug features when running in debug configuration
        /// </summary>
        private void EnableDebugFeaturesInDebugBuild()
        {
#if DEBUG
            DebugButton.Visibility = Visibility.Visible;
            DebugInfoPanel.Visibility = Visibility.Visible;
            _logger.LogDebug("Debug features enabled for development build");
#endif
        }

        /// <summary>
        /// Subscribes to ViewModel events for enhanced debugging and monitoring
        /// </summary>
        private void SubscribeToViewModelEvents()
        {
            if (_viewModel == null) return;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            _viewModel.ErrorsChanged += ViewModel_ErrorsChanged;

            _logger.LogDebug("Subscribed to ViewModel events for enhanced monitoring");
        }

        /// <summary>
        /// Unsubscribes from ViewModel events during cleanup
        /// </summary>
        private void UnsubscribeFromViewModelEvents()
        {
            if (_viewModel == null) return;

            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            _viewModel.ErrorsChanged -= ViewModel_ErrorsChanged;

            _logger.LogDebug("Unsubscribed from ViewModel events");
        }

        /// <summary>
        /// Handles ViewModel property changes for debugging and monitoring
        /// </summary>
        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName)) return;

            // Log significant property changes for debugging
            switch (e.PropertyName)
            {
                case nameof(TruckLoadingViewModel.IsSaving):
                    _logger.LogDebug("IsSaving property changed to: {IsSaving}", _viewModel?.IsSaving);
                    break;

                case nameof(TruckLoadingViewModel.HasErrors):
                    _logger.LogDebug("HasErrors property changed to: {HasErrors}", _viewModel?.HasErrors);
                    break;

                case nameof(TruckLoadingViewModel.CanSave):
                    _logger.LogDebug("CanSave property changed to: {CanSave}", _viewModel?.CanSave);
                    break;

                case nameof(TruckLoadingViewModel.SelectedTruck):
                    _logger.LogDebug("SelectedTruck changed to: {TruckNumber}",
                        _viewModel?.SelectedTruck?.TruckNumber ?? "None");
                    break;
            }
        }

        /// <summary>
        /// Handles ViewModel validation errors for debugging
        /// </summary>
        private void ViewModel_ErrorsChanged(object? sender, System.ComponentModel.DataErrorsChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName)) return;

            _logger.LogDebug("Validation errors changed for property: {PropertyName}", e.PropertyName);
        }

        /// <summary>
        /// Generates comprehensive debug report for troubleshooting
        /// </summary>
        private string GenerateComprehensiveDebugReport()
        {
            var report = new System.Text.StringBuilder();
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

            report.AppendLine("═══════════════════════════════════════");
            report.AppendLine("    TRUCK LOADING DEBUG REPORT");
            report.AppendLine("═══════════════════════════════════════");
            report.AppendLine($"Timestamp: {timestamp}");
            report.AppendLine($"Thread ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}");
            report.AppendLine();

            if (_viewModel == null)
            {
                report.AppendLine("❌ CRITICAL: ViewModel is NULL");
                return report.ToString();
            }

            // ViewModel State
            report.AppendLine("📊 VIEWMODEL STATE:");
            report.AppendLine($"   • CanSave: {_viewModel.CanSave}");
            report.AppendLine($"   • HasErrors: {_viewModel.HasErrors}");
            report.AppendLine($"   • IsSaving: {_viewModel.IsSaving}");
            report.AppendLine($"   • IsLoading: {_viewModel.IsLoading}");
            report.AppendLine($"   • Status Message: {_viewModel.StatusMessage}");
            report.AppendLine($"   • Last Saved ID: {_viewModel.LastSavedLoadId}");
            report.AppendLine($"   • Operation Duration: {_viewModel.OperationDuration}");
            report.AppendLine();

            // Current Data
            report.AppendLine("📝 CURRENT DATA:");
            report.AppendLine($"   • Selected Truck: {_viewModel.SelectedTruck?.TruckNumber ?? "None"} ({_viewModel.SelectedTruck?.DriverName ?? "N/A"})");
            report.AppendLine($"   • Total Weight: {_viewModel.TotalWeight:F2} kg");
            report.AppendLine($"   • Cages Count: {_viewModel.CagesCount}");
            report.AppendLine($"   • Weight Per Cage: {_viewModel.CalculatedWeightPerCage:F2} kg");
            report.AppendLine($"   • Efficiency: {_viewModel.EfficiencyPercentage:F1}%");
            report.AppendLine($"   • Load Date: {_viewModel.LoadDate:yyyy-MM-dd}");
            report.AppendLine($"   • Notes Length: {_viewModel.Notes?.Length ?? 0} characters");
            report.AppendLine();

            // Collections State
            report.AppendLine("📋 COLLECTIONS STATE:");
            report.AppendLine($"   • Available Trucks: {_viewModel.AvailableTrucks?.Count ?? 0}");
            report.AppendLine($"   • Today's Loads: {_viewModel.TodaysTruckLoads?.Count ?? 0}");
            report.AppendLine();

            // Validation State
            report.AppendLine("✅ VALIDATION STATE:");
            report.AppendLine($"   • Validation Summary: {_viewModel.ValidationSummary}");

            if (_viewModel.HasErrors)
            {
                report.AppendLine("   • Validation Errors Present: YES");
                // Add specific validation errors if available
                var errors = _viewModel.GetErrors(null);
                if (errors != null)
                {
                    foreach (var error in errors)
                    {
                        report.AppendLine($"     - {error}");
                    }
                }
            }
            else
            {
                report.AppendLine("   • Validation Errors Present: NO");
            }
            report.AppendLine();

            // Load Summary
            if (_viewModel.LoadSummary != null)
            {
                report.AppendLine("📈 LOAD SUMMARY:");
                report.AppendLine($"   • Total Trucks: {_viewModel.LoadSummary.TotalTrucks}");
                report.AppendLine($"   • Loaded Trucks: {_viewModel.LoadSummary.LoadedTrucks}");
                report.AppendLine($"   • Available Trucks: {_viewModel.LoadSummary.AvailableTrucks}");
                report.AppendLine($"   • Total Weight: {_viewModel.LoadSummary.TotalWeight:F2} kg");
                report.AppendLine($"   • Total Cages: {_viewModel.LoadSummary.TotalCages}");
                report.AppendLine($"   • Avg Weight/Cage: {_viewModel.LoadSummary.AverageWeightPerCage:F2} kg");
                report.AppendLine();
            }

            // UI State
            report.AppendLine("🖥️  UI STATE:");
            report.AppendLine($"   • Validation Errors Visible: {_viewModel.ValidationErrorsVisibility == Visibility.Visible}");
            report.AppendLine($"   • Success Message Visible: {_viewModel.SuccessMessageVisibility == Visibility.Visible}");
            report.AppendLine($"   • Saving Progress Visible: {_viewModel.SavingProgressVisibility == Visibility.Visible}");
            report.AppendLine();

            // System Information
            report.AppendLine("🔧 SYSTEM INFO:");
            report.AppendLine($"   • Environment: {System.Environment.MachineName}");
            report.AppendLine($"   • OS Version: {System.Environment.OSVersion}");
            report.AppendLine($"   • .NET Version: {System.Environment.Version}");
            report.AppendLine($"   • Working Memory: {System.Environment.WorkingSet / (1024 * 1024):F1} MB");
            report.AppendLine();

            // Debug ViewModel Info
            report.AppendLine("🐛 ADVANCED DEBUG:");
            report.AppendLine($"   • Debug Info: {_viewModel.DebugInfo}");
            report.AppendLine();

            report.AppendLine("═══════════════════════════════════════");

            return report.ToString();
        }

        /// <summary>
        /// Attempts graceful degradation when critical errors occur
        /// </summary>
        private async Task AttemptGracefulDegradation()
        {
            try
            {
                _logger.LogInformation("Attempting graceful degradation after critical error");

                // Show user-friendly error state
                if (_viewModel != null)
                {
                    _viewModel.StatusMessage = "حدث خطأ في التحميل - يرجى إعادة المحاولة";
                    _viewModel.IsLoading = false;
                    _viewModel.IsSaving = false;
                }

                // Attempt to clear form to prevent data corruption
                await Task.Delay(1000); // Brief delay for UI to stabilize

                _logger.LogInformation("Graceful degradation completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to perform graceful degradation");
            }
        }

        /// <summary>
        /// Shows debug messages in development environment
        /// </summary>
        private static void ShowDebugMessage(string message)
        {
#if DEBUG
            MessageBox.Show(message, "Debug Information", MessageBoxButton.OK, MessageBoxImage.Information);
#endif
        }

        /// <summary>
        /// Creates enhanced design-time ViewModel with save functionality mock data
        /// </summary>
        private static object CreateEnhancedDesignTimeViewModel()
        {
            return new
            {
                AvailableTrucks = new[]
                {
                    new { TruckNumber = "TR-001", DriverName = "أحمد محمد", TruckId = 1 },
                    new { TruckNumber = "TR-002", DriverName = "محمد علي", TruckId = 2 },
                    new { TruckNumber = "TR-003", DriverName = "علي حسن", TruckId = 3 }
                },
                TodaysTruckLoads = new[]
                {
                    new {
                        LoadId = 101,
                        Truck = new { TruckNumber = "TR-001", DriverName = "أحمد محمد" },
                        TotalWeight = 1250.50m,
                        CagesCount = 50,
                        CreatedDate = DateTime.Now.AddHours(-2),
                        Status = "LOADED"
                    },
                    new {
                        LoadId = 102,
                        Truck = new { TruckNumber = "TR-002", DriverName = "محمد علي" },
                        TotalWeight = 980.25m,
                        CagesCount = 40,
                        CreatedDate = DateTime.Now.AddHours(-1),
                        Status = "IN_TRANSIT"
                    }
                },
                LoadSummary = new
                {
                    TotalTrucks = 3,
                    LoadedTrucks = 2,
                    AvailableTrucks = 1,
                    TotalWeight = 2230.75m,
                    TotalCages = 90,
                    AverageWeightPerCage = 24.78m
                },
                StatusMessage = "جاهز لتسجيل تحميل جديد",
                IsLoading = false,
                IsSaving = false,
                CanSave = true,
                ValidationErrorsVisibility = Visibility.Collapsed,
                SuccessMessageVisibility = Visibility.Collapsed,
                SavingProgressVisibility = Visibility.Collapsed,
                HasErrors = false,
                TotalWeight = 0m,
                CagesCount = 0,
                CalculatedWeightPerCage = 0m,
                EfficiencyPercentage = 0.0,
                LoadDate = DateTime.Today,
                Notes = "",
                ValidationSummary = "",
                LastSavedLoadId = "",
                OperationDuration = ""
            };
        }

        #endregion

        #region Public Methods

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

            // Cleanup previous ViewModel
            UnsubscribeFromViewModelEvents();
            _viewModel?.Cleanup();

            // Set new ViewModel
            _viewModel = viewModel;
            DataContext = _viewModel;

            // Subscribe to new ViewModel events
            SubscribeToViewModelEvents();

            _logger.LogDebug("TruckLoadingView ViewModel updated successfully");
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
        /// Validates current data and returns validation result
        /// </summary>
        public async Task<bool> ValidateDataAsync()
        {
            try
            {
                if (_viewModel != null)
                {
                    await _viewModel.ValidateCurrentLoadCommand.ExecuteAsync(null);
                    _logger.LogDebug("TruckLoadingView data validation completed");
                    return !_viewModel.HasErrors;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating data in TruckLoadingView");
                throw;
            }
        }

        /// <summary>
        /// Attempts to save the current truck load with comprehensive error handling
        /// </summary>
        public async Task<bool> SaveCurrentLoadAsync()
        {
            try
            {
                if (_viewModel?.SaveTruckLoadCommand?.CanExecute(null) == true)
                {
                    await _viewModel.SaveTruckLoadCommand.ExecuteAsync(null);
                    _logger.LogDebug("Save operation completed from external call");
                    return !_viewModel.HasErrors;
                }

                _logger.LogWarning("Save operation cannot be executed - command not available or conditions not met");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving current load from external call");
                throw;
            }
        }

        /// <summary>
        /// Gets current validation status from the ViewModel
        /// </summary>
        public bool IsDataValid => _viewModel?.HasErrors == false;

        /// <summary>
        /// Gets current save capability status
        /// </summary>
        public bool CanSave => _viewModel?.CanSave == true;

        /// <summary>
        /// Gets current validation summary for external use
        /// </summary>
        public string GetValidationSummary() => _viewModel?.ValidationSummary ?? "No validation information available";

        /// <summary>
        /// Gets comprehensive debug information for external troubleshooting
        /// </summary>
        public string GetDebugInformation() => GenerateComprehensiveDebugReport();

        #endregion
    }
}