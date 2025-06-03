using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Services.Repositories;
using PoultrySlaughterPOS.ViewModels;
using PoultrySlaughterPOS.Views;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace PoultrySlaughterPOS
{
    /// <summary>
    /// Enterprise-grade MainWindow implementation with comprehensive navigation system,
    /// complete customer management integration, advanced resource management, and
    /// professional dashboard analytics for the Poultry Slaughter POS system.
    /// 
    /// ENHANCED: Complete customer management navigation, comprehensive statistics loading,
    /// and enterprise-grade resource lifecycle management.
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Private Fields

        private readonly ILogger<MainWindow> _logger;
        private readonly IServiceProvider _serviceProvider;
        private DispatcherTimer? _timeUpdateTimer;
        private DispatcherTimer? _statisticsRefreshTimer;

        // View and ViewModel instances with complete customer management support
        private TruckLoadingView? _truckLoadingView;
        private TruckLoadingViewModel? _truckLoadingViewModel;
        private POSView? _posView;
        private POSViewModel? _posViewModel;
        private CustomerAccountsView? _customerAccountsView;
        private CustomerAccountsViewModel? _customerAccountsViewModel;

        // Navigation state management
        private bool _isNavigating = false;
        private string _currentView = "Dashboard";

        // Statistics caching
        private DateTime _lastStatisticsUpdate = DateTime.MinValue;
        private readonly TimeSpan _statisticsRefreshInterval = TimeSpan.FromMinutes(5);

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes MainWindow with comprehensive dependency injection and enterprise features
        /// </summary>
        public MainWindow(ILogger<MainWindow> logger, IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            try
            {
                InitializeComponent();
                InitializeApplicationSystems();
                InitializeEventHandlers();

                _logger.LogInformation("MainWindow initialized successfully with complete customer management support");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Critical failure during MainWindow initialization");
                throw new ApplicationException("Failed to initialize main application window", ex);
            }
        }

        #endregion

        #region Initialization Methods

        /// <summary>
        /// Initializes core application systems and timers
        /// </summary>
        private void InitializeApplicationSystems()
        {
            try
            {
                // Initialize time display system
                InitializeTimeDisplay();

                // Initialize statistics refresh system
                InitializeStatisticsSystem();

                // Load initial dashboard
                LoadInitialDashboard();

                _logger.LogDebug("Application systems initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing application systems");
                throw;
            }
        }

        /// <summary>
        /// Initializes comprehensive event handlers for window lifecycle
        /// </summary>
        private void InitializeEventHandlers()
        {
            try
            {
                // Window lifecycle events
                Closing += MainWindow_Closing;
                Loaded += MainWindow_Loaded;
                Activated += MainWindow_Activated;

                // Navigation event handlers are already wired in XAML
                _logger.LogDebug("Event handlers initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing event handlers");
                throw;
            }
        }

        /// <summary>
        /// Initializes time display system with high-precision updates
        /// </summary>
        private void InitializeTimeDisplay()
        {
            try
            {
                // Update time immediately
                UpdateTimeDisplay();

                // Setup high-precision timer for time updates
                _timeUpdateTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1)
                };
                _timeUpdateTimer.Tick += (s, e) => UpdateTimeDisplay();
                _timeUpdateTimer.Start();

                _logger.LogDebug("Time display system initialized");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error initializing time display system");
            }
        }

        /// <summary>
        /// Initializes statistics refresh system for real-time dashboard updates
        /// </summary>
        private void InitializeStatisticsSystem()
        {
            try
            {
                // Setup statistics refresh timer
                _statisticsRefreshTimer = new DispatcherTimer
                {
                    Interval = _statisticsRefreshInterval
                };
                _statisticsRefreshTimer.Tick += async (s, e) => await RefreshDashboardStatisticsAsync();
                _statisticsRefreshTimer.Start();

                _logger.LogDebug("Statistics refresh system initialized");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error initializing statistics system");
            }
        }

        /// <summary>
        /// Loads initial dashboard with comprehensive statistics
        /// </summary>
        private async void LoadInitialDashboard()
        {
            try
            {
                ShowDashboard();
                await LoadDashboardStatisticsAsync();
                _logger.LogInformation("Initial dashboard loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading initial dashboard");
                UpdateStatusMessage("خطأ في تحميل لوحة التحكم", isError: true);
            }
        }

        #endregion

        #region Navigation Event Handlers

        /// <summary>
        /// Handles dashboard navigation with comprehensive state management
        /// </summary>
        private async void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            if (_isNavigating) return;

            try
            {
                await NavigateToViewAsync("Dashboard", () =>
                {
                    ShowDashboard();
                    return Task.CompletedTask;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to dashboard");
                UpdateStatusMessage("خطأ في التنقل إلى لوحة التحكم", isError: true);
            }
        }

        /// <summary>
        /// Handles truck loading navigation with comprehensive view management
        /// </summary>
        private async void TruckLoading_Click(object sender, RoutedEventArgs e)
        {
            if (_isNavigating) return;

            try
            {
                await NavigateToViewAsync("TruckLoading", async () =>
                {
                    UpdateStatusMessage("جاري تحميل صفحة الشاحنات...");

                    // Create or reuse truck loading view
                    if (_truckLoadingView == null)
                    {
                        _truckLoadingViewModel = _serviceProvider.GetRequiredService<TruckLoadingViewModel>();
                        _truckLoadingView = _serviceProvider.GetRequiredService<TruckLoadingView>();
                        _truckLoadingView.SetViewModel(_truckLoadingViewModel);

                        _logger.LogInformation("Truck Loading view created and configured successfully");
                    }

                    // Set content and initialize
                    DynamicContentPresenter.Content = _truckLoadingView;
                    DynamicContentPresenter.Visibility = Visibility.Visible;

                    if (_truckLoadingView != null)
                    {
                        await _truckLoadingView.InitializeAsync();
                    }

                    UpdateStatusMessage("صفحة الشاحنات جاهزة");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to Truck Loading view");
                HandleNavigationError("تحميل الشاحنات", ex);
            }
        }

        /// <summary>
        /// Handles POS sales navigation with comprehensive view management
        /// </summary>
        private async void POSSales_Click(object sender, RoutedEventArgs e)
        {
            if (_isNavigating) return;

            try
            {
                await NavigateToViewAsync("POSSales", async () =>
                {
                    UpdateStatusMessage("جاري تحميل نقطة البيع...");

                    // Create or reuse POS view
                    if (_posView == null)
                    {
                        _posViewModel = _serviceProvider.GetRequiredService<POSViewModel>();
                        _posView = _serviceProvider.GetRequiredService<POSView>();
                        _posView.SetViewModel(_posViewModel);

                        _logger.LogInformation("POS view created and configured successfully");
                    }

                    // Set content and initialize
                    DynamicContentPresenter.Content = _posView;
                    DynamicContentPresenter.Visibility = Visibility.Visible;

                    if (_posView != null)
                    {
                        await _posView.InitializeAsync();
                    }

                    UpdateStatusMessage("نقطة البيع جاهزة");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to POS Sales view");
                HandleNavigationError("نقطة البيع", ex);
            }
        }

        /// <summary>
        /// Handles Customer Accounts navigation with comprehensive customer management integration
        /// ENHANCED: Complete customer management navigation with advanced error handling
        /// </summary>
        private async void CustomerAccounts_Click(object sender, RoutedEventArgs e)
        {
            if (_isNavigating) return;

            try
            {
                await NavigateToViewAsync("CustomerAccounts", async () =>
                {
                    UpdateStatusMessage("جاري تحميل صفحة الزبائن...");

                    // Create or reuse customer accounts view
                    if (_customerAccountsView == null)
                    {
                        _customerAccountsViewModel = _serviceProvider.GetRequiredService<CustomerAccountsViewModel>();
                        _customerAccountsView = _serviceProvider.GetRequiredService<CustomerAccountsView>();
                        _customerAccountsView.SetViewModel(_customerAccountsViewModel);

                        _logger.LogInformation("Customer Accounts view created and configured successfully");
                    }

                    // Set content and make visible
                    DynamicContentPresenter.Content = _customerAccountsView;
                    DynamicContentPresenter.Visibility = Visibility.Visible;

                    // Initialize customer management system
                    if (_customerAccountsView != null)
                    {
                        await _customerAccountsView.InitializeAsync();
                    }

                    UpdateStatusMessage("صفحة الزبائن جاهزة");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to Customer Accounts view");
                HandleNavigationError("حسابات الزبائن", ex);
            }
        }

        /// <summary>
        /// Handles transaction history navigation (future implementation)
        /// </summary>
        private void TransactionHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatusMessage("تاريخ المعاملات قيد التطوير...");
                MessageBox.Show(
                    "وحدة تاريخ المعاملات قيد التطوير وستكون متاحة في الإصدار القادم.",
                    "قريباً",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                _logger.LogInformation("Transaction History navigation attempted - feature under development");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in Transaction History navigation");
            }
        }

        /// <summary>
        /// Handles reports navigation (future implementation)
        /// </summary>
        private void Reports_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatusMessage("وحدة التقارير قيد التطوير...");
                MessageBox.Show(
                    "وحدة التقارير قيد التطوير وستكون متاحة في الإصدار القادم.",
                    "قريباً",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                _logger.LogInformation("Reports navigation attempted - feature under development");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in Reports navigation");
            }
        }

        /// <summary>
        /// Handles reconciliation navigation (future implementation)
        /// </summary>
        private void Reconciliation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatusMessage("وحدة التسوية قيد التطوير...");
                MessageBox.Show(
                    "وحدة التسوية قيد التطوير وستكون متاحة في الإصدار القادم.",
                    "قريباً",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                _logger.LogInformation("Reconciliation navigation attempted - feature under development");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in Reconciliation navigation");
            }
        }

        #endregion

        #region Navigation Management

        /// <summary>
        /// Generic navigation handler with comprehensive state management and error handling
        /// </summary>
        private async Task NavigateToViewAsync(string viewName, Func<Task> navigationAction)
        {
            if (_isNavigating)
            {
                _logger.LogWarning("Navigation already in progress, ignoring navigation to {ViewName}", viewName);
                return;
            }

            try
            {
                _isNavigating = true;

                // Hide dashboard content during navigation
                DashboardContent.Visibility = Visibility.Collapsed;

                // Execute navigation action
                await navigationAction();

                // Update current view state
                _currentView = viewName;

                _logger.LogInformation("Successfully navigated to {ViewName}", viewName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Navigation to {ViewName} failed", viewName);

                // Revert to dashboard on navigation failure
                ShowDashboard();
                throw;
            }
            finally
            {
                _isNavigating = false;
            }
        }

        /// <summary>
        /// Shows dashboard with comprehensive analytics display
        /// </summary>
        private void ShowDashboard()
        {
            try
            {
                // Hide dynamic content
                DynamicContentPresenter.Visibility = Visibility.Collapsed;
                DynamicContentPresenter.Content = null;

                // Show dashboard
                DashboardContent.Visibility = Visibility.Visible;

                // Update current view state
                _currentView = "Dashboard";

                UpdateStatusMessage("لوحة التحكم جاهزة");
                _logger.LogDebug("Dashboard displayed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error displaying dashboard");
                UpdateStatusMessage("خطأ في عرض لوحة التحكم", isError: true);
            }
        }

        /// <summary>
        /// Handles navigation errors with user-friendly messaging
        /// </summary>
        private void HandleNavigationError(string moduleName, Exception ex)
        {
            var userMessage = $"حدث خطأ أثناء تحميل وحدة {moduleName}. يرجى المحاولة مرة أخرى.";

            UpdateStatusMessage($"خطأ في تحميل {moduleName}", isError: true);

            MessageBox.Show(
                userMessage,
                "خطأ في التنقل",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            // Revert to dashboard
            ShowDashboard();
        }

        #endregion

        #region Dashboard Statistics

        /// <summary>
        /// Loads comprehensive dashboard statistics with customer management integration
        /// ENHANCED: Complete customer statistics integration with advanced analytics
        /// </summary>
        private async Task LoadDashboardStatisticsAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                UpdateStatusMessage("جاري تحميل إحصائيات النظام...");

                // Load all statistics concurrently for optimal performance
                var activeTrucksTask = unitOfWork.Trucks.GetActiveTrucksAsync();
                var todayInvoicesTask = unitOfWork.Invoices.GetInvoicesByDateRangeAsync(DateTime.Today, DateTime.Today.AddDays(1));
                var activeCustomersTask = unitOfWork.Customers.GetActiveCustomerCountAsync();
                var debtSummaryTask = unitOfWork.Customers.GetDebtSummaryAsync();

                // Wait for all statistics to load
                await Task.WhenAll(activeTrucksTask, todayInvoicesTask, activeCustomersTask, debtSummaryTask);

                // Extract results
                var activeTrucks = await activeTrucksTask;
                var todayInvoices = await todayInvoicesTask;
                var activeCustomersCount = await activeCustomersTask;
                var (totalDebt, customersWithDebt) = await debtSummaryTask;

                // Update UI elements
                Dispatcher.Invoke(() =>
                {
                    // Update existing counters
                    ActiveTrucksCount.Text = activeTrucks.Count().ToString();
                    TodayInvoicesCount.Text = todayInvoices.Count().ToString();
                    ActiveCustomersCount.Text = activeCustomersCount.ToString();

                    // Update status with comprehensive information
                    if (totalDebt > 0)
                    {
                        StatusTextBlock.Text = $"النظام جاهز - {activeCustomersCount} زبون نشط - إجمالي الديون: {totalDebt:N2} USD";
                    }
                    else
                    {
                        StatusTextBlock.Text = $"النظام جاهز - {activeCustomersCount} زبون نشط - لا توجد ديون معلقة";
                    }
                });

                // Update last refresh time
                _lastStatisticsUpdate = DateTime.Now;

                _logger.LogInformation("Dashboard statistics loaded successfully - Trucks: {Trucks}, Customers: {Customers}, Invoices: {Invoices}, Debt: {Debt:C}",
                    activeTrucks.Count(), activeCustomersCount, todayInvoices.Count(), totalDebt);

                UpdateStatusMessage("إحصائيات النظام محدثة");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard statistics");
                UpdateStatusMessage("خطأ في تحميل إحصائيات النظام", isError: true);

                // Set default values on error
                Dispatcher.Invoke(() =>
                {
                    ActiveTrucksCount.Text = "0";
                    TodayInvoicesCount.Text = "0";
                    ActiveCustomersCount.Text = "0";
                    StatusTextBlock.Text = "خطأ في تحميل إحصائيات النظام";
                });
            }
        }

        /// <summary>
        /// Refreshes dashboard statistics if needed
        /// </summary>
        private async Task RefreshDashboardStatisticsAsync()
        {
            try
            {
                if (_currentView == "Dashboard" &&
                    DateTime.Now - _lastStatisticsUpdate > _statisticsRefreshInterval)
                {
                    await LoadDashboardStatisticsAsync();
                    _logger.LogDebug("Dashboard statistics auto-refreshed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during automatic statistics refresh");
            }
        }

        #endregion

        #region Resource Management

        /// <summary>
        /// Comprehensive cleanup of all view resources and timers
        /// ENHANCED: Complete resource cleanup including customer management components
        /// </summary>
        private void CleanupViewResources()
        {
            try
            {
                _logger.LogInformation("Starting comprehensive resource cleanup");

                // Cleanup existing views
                _truckLoadingView?.Cleanup();
                _posView?.Cleanup();
                _customerAccountsView?.Cleanup();

                // Clear view references
                _truckLoadingView = null;
                _truckLoadingViewModel = null;
                _posView = null;
                _posViewModel = null;
                _customerAccountsView = null;
                _customerAccountsViewModel = null;

                // Clear dynamic content
                DynamicContentPresenter.Content = null;
                DynamicContentPresenter.Visibility = Visibility.Collapsed;

                // Stop and dispose timers
                _timeUpdateTimer?.Stop();
                _timeUpdateTimer = null;

                _statisticsRefreshTimer?.Stop();
                _statisticsRefreshTimer = null;

                _logger.LogInformation("Resource cleanup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during resource cleanup");
            }
        }

        #endregion

        #region Window Event Handlers

        /// <summary>
        /// Handles window loaded event with comprehensive initialization
        /// </summary>
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("MainWindow loaded successfully");

                // Perform any additional post-load initialization
                await Task.Delay(100); // Brief delay to ensure UI is fully rendered

                UpdateStatusMessage("التطبيق جاهز للاستخدام");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during window loaded event");
            }
        }

        /// <summary>
        /// Handles window activation for statistics refresh
        /// </summary>
        private async void MainWindow_Activated(object sender, EventArgs e)
        {
            try
            {
                // Refresh statistics when window becomes active (if needed)
                if (_currentView == "Dashboard" &&
                    DateTime.Now - _lastStatisticsUpdate > TimeSpan.FromMinutes(2))
                {
                    await LoadDashboardStatisticsAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during window activation");
            }
        }

        /// <summary>
        /// Handles window closing with comprehensive cleanup and state preservation
        /// </summary>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                _logger.LogInformation("Application closing initiated - performing comprehensive cleanup");

                // Show closing message
                UpdateStatusMessage("جاري إغلاق التطبيق...");

                // Perform comprehensive resource cleanup
                CleanupViewResources();

                _logger.LogInformation("Application cleanup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during application closing cleanup");

                // Don't prevent closure due to cleanup errors
                e.Cancel = false;
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Updates time display with high precision
        /// </summary>
        private void UpdateTimeDisplay()
        {
            try
            {
                if (TimeTextBlock != null)
                {
                    TimeTextBlock.Text = DateTime.Now.ToString("HH:mm:ss");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating time display");
            }
        }

        /// <summary>
        /// Updates status message with enhanced user feedback
        /// </summary>
        private void UpdateStatusMessage(string message, bool isError = false)
        {
            try
            {
                if (DatabaseStatusText != null)
                {
                    DatabaseStatusText.Text = message;

                    // Log based on message type
                    if (isError)
                    {
                        _logger.LogWarning("Status message (Error): {Message}", message);
                    }
                    else
                    {
                        _logger.LogDebug("Status message: {Message}", message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating status message");
            }
        }

        #endregion
    }
}