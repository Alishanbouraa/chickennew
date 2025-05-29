using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Data;
using PoultrySlaughterPOS.Services;
using PoultrySlaughterPOS.Views;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PoultrySlaughterPOS
{
    /// <summary>
    /// Enterprise-grade main window with comprehensive navigation system and real-time dashboard.
    /// Implements modern WPF patterns with dependency injection and robust error handling.
    /// UPDATED: Complete POS module integration with navigation support.
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Private Fields

        private readonly IDbContextFactory<PoultryDbContext> _contextFactory;
        private readonly ILogger<MainWindow> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly DispatcherTimer _clockTimer;
        private readonly DispatcherTimer _statusUpdateTimer;

        // Current active page tracking
        private UserControl? _currentPage;
        private string _currentPageName = "Dashboard";

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for Dependency Injection with comprehensive service resolution
        /// </summary>
        public MainWindow(
            IDbContextFactory<PoultryDbContext> contextFactory,
            ILogger<MainWindow> logger,
            IServiceProvider serviceProvider)
        {
            InitializeComponent();

            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _errorHandlingService = _serviceProvider.GetRequiredService<IErrorHandlingService>();

            // Configure window properties
            Title = "نظام إدارة مسلخ الدجاج - Poultry Slaughter POS";
            WindowState = WindowState.Maximized;

            // Initialize timers for real-time updates
            _clockTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _clockTimer.Tick += ClockTimer_Tick;

            _statusUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(5)
            };
            _statusUpdateTimer.Tick += StatusUpdateTimer_Tick;

            // Wire up events
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;

            _logger.LogInformation("MainWindow initialized successfully with complete POS navigation integration");
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles window loaded event with comprehensive initialization
        /// </summary>
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("MainWindow loaded, starting initialization with POS module support");

                // Start real-time clock
                _clockTimer.Start();
                UpdateClock();

                // Initialize dashboard with database verification
                await InitializeDashboardAsync();

                // Start periodic status updates
                _statusUpdateTimer.Start();

                _logger.LogInformation("MainWindow initialization completed successfully with POS integration");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during MainWindow initialization");
                await _errorHandlingService.HandleExceptionAsync(ex, "MainWindow_Loaded");

                StatusTextBlock.Text = $"خطأ في تحميل النظام: {ex.Message}";
                DatabaseStatusText.Text = "خطأ في الاتصال بقاعدة البيانات";

                MessageBox.Show($"خطأ في تحميل النظام:\n{ex.Message}",
                               "خطأ",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Handles window closing event with proper cleanup
        /// </summary>
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                _logger.LogInformation("MainWindow closing, performing cleanup");

                // Stop timers
                _clockTimer?.Stop();
                _statusUpdateTimer?.Stop();

                // Cleanup current page
                if (_currentPage is IDisposable disposablePage)
                {
                    disposablePage.Dispose();
                }

                _logger.LogInformation("MainWindow cleanup completed");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during MainWindow cleanup");
            }
        }

        /// <summary>
        /// Updates the real-time clock display
        /// </summary>
        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            UpdateClock();
        }

        /// <summary>
        /// Performs periodic status updates for dashboard statistics
        /// </summary>
        private async void StatusUpdateTimer_Tick(object? sender, EventArgs e)
        {
            if (_currentPageName == "Dashboard")
            {
                await UpdateDashboardStatisticsAsync();
            }
        }

        #endregion

        #region Navigation Methods

        /// <summary>
        /// Navigates to the Dashboard view
        /// </summary>
        private async void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToPageAsync("Dashboard");
        }

        /// <summary>
        /// Navigates to the Truck Loading view
        /// </summary>
        private async void TruckLoading_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToPageAsync("TruckLoading");
        }

        /// <summary>
        /// Navigates to the POS Sales view - FULLY IMPLEMENTED
        /// </summary>
        private async void POSSales_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToPageAsync("POSSales");
        }

        /// <summary>
        /// Navigates to the Customer Accounts view (placeholder)
        /// </summary>
        private async void CustomerAccounts_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToPageAsync("CustomerAccounts");
        }

        /// <summary>
        /// Navigates to the Transaction History view (placeholder)
        /// </summary>
        private async void TransactionHistory_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToPageAsync("TransactionHistory");
        }

        /// <summary>
        /// Navigates to the Reports view (placeholder)
        /// </summary>
        private async void Reports_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToPageAsync("Reports");
        }

        /// <summary>
        /// Navigates to the Reconciliation view (placeholder)
        /// </summary>
        private async void Reconciliation_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToPageAsync("Reconciliation");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes the dashboard with database connection verification and statistics
        /// </summary>
        private async Task InitializeDashboardAsync()
        {
            try
            {
                StatusTextBlock.Text = "جاري التحقق من قاعدة البيانات...";

                // Test database connection using factory pattern
                using var context = await _contextFactory.CreateDbContextAsync();

                // Verify database connectivity and get initial statistics
                var trucksCount = await context.Trucks.CountAsync(t => t.IsActive);
                var customersCount = await context.Customers.CountAsync(c => c.IsActive);
                var todayInvoicesCount = await context.Invoices
                    .CountAsync(i => i.InvoiceDate.Date == DateTime.Today);

                // Update dashboard statistics
                ActiveTrucksCount.Text = trucksCount.ToString();
                ActiveCustomersCount.Text = customersCount.ToString();
                TodayInvoicesCount.Text = todayInvoicesCount.ToString();

                StatusTextBlock.Text = $"قاعدة البيانات متصلة بنجاح | الشاحنات: {trucksCount} | الزبائن: {customersCount} | فواتير اليوم: {todayInvoicesCount}";
                DatabaseStatusText.Text = "متصل بقاعدة البيانات";

                _logger.LogInformation("Dashboard initialized - Trucks: {TrucksCount}, Customers: {CustomersCount}, Today's Invoices: {InvoicesCount}",
                    trucksCount, customersCount, todayInvoicesCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing dashboard");
                StatusTextBlock.Text = $"خطأ في الاتصال بقاعدة البيانات: {ex.Message}";
                DatabaseStatusText.Text = "خطأ في الاتصال";
                throw;
            }
        }

        /// <summary>
        /// Updates dashboard statistics without full reinitialization
        /// </summary>
        private async Task UpdateDashboardStatisticsAsync()
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var todayInvoicesCount = await context.Invoices
                    .CountAsync(i => i.InvoiceDate.Date == DateTime.Today);

                TodayInvoicesCount.Text = todayInvoicesCount.ToString();

                _logger.LogDebug("Dashboard statistics updated - Today's Invoices: {InvoicesCount}", todayInvoicesCount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating dashboard statistics");
            }
        }

        /// <summary>
        /// Updates the real-time clock display
        /// </summary>
        private void UpdateClock()
        {
            TimeTextBlock.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        /// <summary>
        /// Handles navigation between different pages with proper cleanup and initialization
        /// UPDATED: Complete POS module integration added
        /// </summary>
        /// <param name="pageName">Name of the page to navigate to</param>
        private async Task NavigateToPageAsync(string pageName)
        {
            try
            {
                _logger.LogInformation("Navigating to page: {PageName}", pageName);

                // Cleanup current page if it's disposable
                if (_currentPage is IDisposable disposablePage)
                {
                    disposablePage.Dispose();
                }

                _currentPage = null;
                _currentPageName = pageName;

                // Show/hide appropriate content
                switch (pageName)
                {
                    case "Dashboard":
                        DashboardContent.Visibility = Visibility.Visible;
                        DynamicContentPresenter.Visibility = Visibility.Collapsed;
                        DynamicContentPresenter.Content = null;
                        await UpdateDashboardStatisticsAsync();
                        break;

                    case "TruckLoading":
                        await LoadTruckLoadingPageAsync();
                        break;

                    case "POSSales":
                        await LoadPOSPageAsync();
                        break;

                    case "CustomerAccounts":
                    case "TransactionHistory":
                    case "Reports":
                    case "Reconciliation":
                        LoadPlaceholderPage(pageName);
                        break;

                    default:
                        _logger.LogWarning("Unknown page requested: {PageName}", pageName);
                        break;
                }

                _logger.LogInformation("Successfully navigated to page: {PageName}", pageName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to page: {PageName}", pageName);
                await _errorHandlingService.HandleExceptionAsync(ex, $"NavigateToPageAsync - {pageName}");

                MessageBox.Show($"خطأ في تحميل الصفحة {pageName}:\n{ex.Message}",
                               "خطأ في التنقل",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Loads the truck loading page with proper dependency injection
        /// </summary>
        private async Task LoadTruckLoadingPageAsync()
        {
            try
            {
                // Get truck loading view from DI container
                var truckLoadingView = _serviceProvider.GetRequiredService<TruckLoadingView>();

                // Set as current page
                _currentPage = truckLoadingView;

                // Update content display
                DashboardContent.Visibility = Visibility.Collapsed;
                DynamicContentPresenter.Content = truckLoadingView;
                DynamicContentPresenter.Visibility = Visibility.Visible;

                // Initialize the view if it has an initialization method
                if (truckLoadingView.ViewModel != null)
                {
                    await truckLoadingView.ViewModel.InitializeAsync();
                }

                _logger.LogInformation("Truck loading page loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading truck loading page");
                throw;
            }
        }

        /// <summary>
        /// Loads the POS Sales page with comprehensive dependency injection and initialization
        /// NEWLY IMPLEMENTED: Complete POS module integration
        /// </summary>
        private async Task LoadPOSPageAsync()
        {
            try
            {
                _logger.LogInformation("Loading POS Sales page with full dependency injection");

                // Get POS view from DI container
                var posView = _serviceProvider.GetRequiredService<POSView>();

                // Set as current page
                _currentPage = posView;

                // Update content display
                DashboardContent.Visibility = Visibility.Collapsed;
                DynamicContentPresenter.Content = posView;
                DynamicContentPresenter.Visibility = Visibility.Visible;

                // Initialize the POS view with comprehensive data loading
                if (posView.ViewModel != null)
                {
                    await posView.ViewModel.InitializeAsync();
                    _logger.LogInformation("POS ViewModel initialized with customer and truck data");
                }

                // Set focus to the first input field for improved UX
                posView.FocusCustomerSelection();

                _logger.LogInformation("POS Sales page loaded successfully with full functionality");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading POS Sales page");

                // Provide user-friendly error message
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                MessageBox.Show($"خطأ في تحميل صفحة نقطة البيع:\n{errorMessage}\n\nيرجى التأكد من توفر البيانات المطلوبة.",
                               "خطأ في تحميل نقطة البيع",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);

                // Fallback to dashboard
                await NavigateToPageAsync("Dashboard");
                throw;
            }
        }

        /// <summary>
        /// Loads a placeholder page for features not yet implemented
        /// </summary>
        /// <param name="pageName">Name of the page</param>
        private void LoadPlaceholderPage(string pageName)
        {
            var placeholderContent = new Border
            {
                Background = System.Windows.Media.Brushes.White,
                Child = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "🚧",
                            FontSize = 48,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 0, 0, 20)
                        },
                        new TextBlock
                        {
                            Text = $"صفحة {GetArabicPageName(pageName)}",
                            FontSize = 24,
                            FontWeight = FontWeights.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 0, 0, 10)
                        },
                        new TextBlock
                        {
                            Text = "هذه الصفحة قيد التطوير",
                            FontSize = 16,
                            Foreground = System.Windows.Media.Brushes.Gray,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 0, 0, 20)
                        },
                        new TextBlock
                        {
                            Text = "سيتم إضافة هذه الميزة في التحديثات القادمة",
                            FontSize = 14,
                            Foreground = System.Windows.Media.Brushes.Gray,
                            HorizontalAlignment = HorizontalAlignment.Center
                        }
                    }
                }
            };

            DashboardContent.Visibility = Visibility.Collapsed;
            DynamicContentPresenter.Content = placeholderContent;
            DynamicContentPresenter.Visibility = Visibility.Visible;

            _logger.LogDebug("Loaded placeholder page for: {PageName}", pageName);
        }

        /// <summary>
        /// Converts English page names to Arabic for display
        /// </summary>
        /// <param name="pageName">English page name</param>
        /// <returns>Arabic page name</returns>
        private static string GetArabicPageName(string pageName)
        {
            return pageName switch
            {
                "POSSales" => "نقطة البيع",
                "CustomerAccounts" => "حسابات الزبائن",
                "TransactionHistory" => "تاريخ المعاملات",
                "Reports" => "التقارير",
                "Reconciliation" => "التسوية",
                _ => pageName
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Programmatically navigates to a specific page (for external use)
        /// </summary>
        /// <param name="pageName">Page name to navigate to</param>
        public async Task NavigateToAsync(string pageName)
        {
            await NavigateToPageAsync(pageName);
        }

        /// <summary>
        /// Gets the current active page name
        /// </summary>
        public string CurrentPageName => _currentPageName;

        /// <summary>
        /// Forces a refresh of the current page content
        /// </summary>
        public async Task RefreshCurrentPageAsync()
        {
            await NavigateToPageAsync(_currentPageName);
        }

        /// <summary>
        /// Quick navigation method for POS Sales (can be called from external shortcuts)
        /// </summary>
        public async Task NavigateToPOSAsync()
        {
            await NavigateToPageAsync("POSSales");
        }

        #endregion
    }
}