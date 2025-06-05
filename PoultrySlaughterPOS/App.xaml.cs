using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Data;
using PoultrySlaughterPOS.Services;
using PoultrySlaughterPOS.Services.Repositories;
using PoultrySlaughterPOS.Services.Repositories.Implementations;
using PoultrySlaughterPOS.ViewModels;
using PoultrySlaughterPOS.Views;
using PoultrySlaughterPOS.Views.Dialogs;
using PoultrySlaughterPOS.Controls;
using PoultrySlaughterPOS.Extensions;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using PoultrySlaughterPOS.Repositories;

namespace PoultrySlaughterPOS
{
    /// <summary>
    /// Enterprise-grade WPF application entry point with comprehensive dependency injection,
    /// logging configuration, customer management integration, and service registration 
    /// for the complete Poultry Slaughter POS system.
    /// 
    /// ENHANCED: Complete customer management integration with Services property resolution
    /// and all supporting controls with proper application lifecycle management.
    /// </summary>
    public partial class App : Application
    {
        #region Private Fields

        private IHost? _host;
        private readonly object _lockObject = new object();

        #endregion

        #region Application Lifecycle

        /// <summary>
        /// Handles application startup with comprehensive initialization and error handling
        /// </summary>
        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Configure enterprise-grade logging with Serilog
                ConfigureLogging();

                Log.Information("=== Poultry Slaughter POS Application Starting ===");
                Log.Information("Startup initiated with complete customer management integration");

                // Build comprehensive configuration system
                var configuration = BuildConfiguration();

                // Configure and build host with complete service registration
                _host = await BuildHostAsync(configuration);

                // CRITICAL: Configure Services property for application-wide DI access
                this.ConfigureServiceProvider(_host);

                // Initialize database with enhanced error handling
                await InitializeDatabaseAsync();

                // Create and display main window through dependency injection
                await ShowMainWindowAsync();

                Log.Information("=== Poultry Slaughter POS Application Started Successfully ===");
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                HandleCriticalStartupError(ex);
            }
        }

        /// <summary>
        /// Handles application exit with comprehensive cleanup
        /// </summary>
        protected override async void OnExit(ExitEventArgs e)
        {
            try
            {
                Log.Information("=== Poultry Slaughter POS Application Shutdown Initiated ===");

                if (_host != null)
                {
                    await _host.StopAsync(TimeSpan.FromSeconds(10));
                    _host.Dispose();
                    Log.Information("Application host stopped and disposed successfully");
                }

                // Dispose service provider to prevent resource leaks
                this.DisposeServiceProvider();

                base.OnExit(e);
                Log.Information("=== Poultry Slaughter POS Application Shutdown Completed ===");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred during application shutdown");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        #endregion

        #region Configuration Methods

        /// <summary>
        /// Configures enterprise-grade logging with comprehensive output targets
        /// </summary>
        private static void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/pos-application-.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/errors/error-log-.txt",
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error,
                    retainedFileCountLimit: 90,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/debug/debug-log-.txt",
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug,
                    retainedFileCountLimit: 7,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
                .Enrich.WithThreadId()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .CreateLogger();
        }

        /// <summary>
        /// Builds comprehensive configuration with environment support
        /// </summary>
        private static IConfiguration BuildConfiguration()
        {
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables("POULTRY_POS_")
                .Build();
        }

        /// <summary>
        /// Builds application host with comprehensive service registration
        /// </summary>
        private static async Task<IHost> BuildHostAsync(IConfiguration configuration)
        {
            var host = Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((context, services) =>
                {
                    ConfigureAllServices(services, configuration);
                })
                .Build();

            await host.StartAsync();
            Log.Information("Application host built and started successfully with complete customer management integration");

            return host;
        }

        #endregion

        #region Service Configuration

        /// <summary>
        /// Configures all services for dependency injection with complete customer management integration.
        /// ENHANCED: Complete service registration including all customer management components.
        /// </summary>
        /// <param name="services">Service collection for DI container</param>
        /// <param name="configuration">Application configuration</param>
        private static void ConfigureAllServices(IServiceCollection services, IConfiguration configuration)
        {
            // Register configuration
            services.AddSingleton(configuration);

            // Configure database and data access layer
            ConfigureDatabaseServices(services, configuration);

            // Configure repository layer
            ConfigureRepositoryServices(services);

            // Configure business logic layer
            ConfigureBusinessServices(services);

            // Configure presentation layer
            ConfigurePresentationServices(services);

            // Configure cross-cutting concerns
            ConfigureCrossCuttingServices(services, configuration);

            Log.Information("Service configuration completed successfully with {ServiceCount} services registered (complete customer management integration)",
                           services.Count);
        }

        /// <summary>
        /// Configures database and Entity Framework services with enterprise patterns
        /// </summary>
        private static void ConfigureDatabaseServices(IServiceCollection services, IConfiguration configuration)
        {
            // Database configuration settings
            var useTransactionalOperations = configuration.GetValue<bool>("Database:UseExplicitTransactions", true);
            var enableRetryOnFailure = configuration.GetValue<bool>("Database:EnableRetryOnFailure", false);
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection connection string is required");

            Log.Information("Database configuration - UseExplicitTransactions: {UseTransactions}, EnableRetryOnFailure: {EnableRetry}",
                useTransactionalOperations, enableRetryOnFailure);

            // Primary DbContext registration with conditional retry strategy
            services.AddDbContext<PoultryDbContext>(options =>
            {
                ConfigureDbContextOptions(options, connectionString, enableRetryOnFailure, useTransactionalOperations);
            }, ServiceLifetime.Scoped);

            // DbContextFactory registration for advanced scenarios
            services.AddSingleton<IDbContextFactory<PoultryDbContext>>(serviceProvider =>
            {
                return new PoultryDbContextFactory(connectionString, enableRetryOnFailure, useTransactionalOperations);
            });

            // Database initialization service
            services.AddTransient<IDatabaseInitializationService, DatabaseInitializationService>();
        }

        /// <summary>
        /// Configures repository layer with comprehensive customer management support
        /// </summary>
        private static void ConfigureRepositoryServices(IServiceCollection services)
        {
            // Core repository registrations
            services.AddScoped<ITruckRepository, TruckRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<ITruckLoadRepository, TruckLoadRepository>();
            services.AddScoped<IDailyReconciliationRepository, DailyReconciliationRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            services.AddScoped<ITransactionProcessingService, TransactionProcessingService>();
            // Unit of Work pattern implementation
            services.AddScoped<IUnitOfWork>(serviceProvider =>
            {
                var context = serviceProvider.GetRequiredService<PoultryDbContext>();
                var contextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PoultryDbContext>>();
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

                return new UnitOfWork(context, contextFactory, loggerFactory);
            });

            Log.Debug("Repository layer configured with comprehensive data access patterns");
        }

        /// <summary>
        /// Configures business logic services with enterprise patterns
        /// </summary>
        private static void ConfigureBusinessServices(IServiceCollection services)
        {
            // Core business services
            services.AddScoped<IErrorHandlingService, ErrorHandlingService>();
            services.AddScoped<ITruckLoadingService, TruckLoadingService>();
            services.AddScoped<IPOSService, POSService>();

            // Customer management business services (future expansion)
            // services.AddScoped<ICustomerService, CustomerService>();
            // services.AddScoped<IPaymentService, PaymentService>();
            // services.AddScoped<IDebtManagementService, DebtManagementService>();

            Log.Debug("Business logic layer configured with comprehensive service patterns");
        }

        /// <summary>
        /// Configures presentation layer with complete customer management integration
        /// </summary>
        private static void ConfigurePresentationServices(IServiceCollection services)
        {
            // Main application ViewModels
            services.AddTransient<TruckLoadingViewModel>();
            services.AddTransient<POSViewModel>();
            services.AddTransient<CustomerAccountsViewModel>();

            // Dialog ViewModels for customer management
            services.AddTransient<AddCustomerDialogViewModel>();

            // Main application Views
            services.AddTransient<TruckLoadingView>();
            services.AddTransient<POSView>();
            services.AddTransient<CustomerAccountsView>();

            // Dialog Views for customer management workflow
            services.AddTransient<AddCustomerDialog>();

            // Customer Management Controls
            services.AddTransient<CustomerListControl>();
            services.AddTransient<CustomerDetailsControl>();
            services.AddTransient<AccountStatementControl>();
            services.AddTransient<PaymentHistoryControl>();
            services.AddTransient<DebtManagementControl>();

            // Main application window (transient for proper lifecycle management)
            services.AddTransient<MainWindow>();

            Log.Debug("Presentation layer configured with complete customer management integration");
        }

        /// <summary>
        /// Configures cross-cutting concerns and additional services
        /// </summary>
        private static void ConfigureCrossCuttingServices(IServiceCollection services, IConfiguration configuration)
        {
            // Enhanced logging configuration
            services.AddLogging(builder =>
            {
                builder.AddSerilog();
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
                builder.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.Warning);
                builder.AddFilter("Microsoft.EntityFrameworkCore.Model.Validation", LogLevel.Warning);
            });

            // Memory caching for performance optimization
            services.AddMemoryCache(options =>
            {
                options.SizeLimit = 1000; // Reasonable limit for offline scenarios
                options.CompactionPercentage = 0.25; // Compact when 75% full
            });

            // Configuration options pattern for strongly-typed settings
            services.Configure<ApplicationSettings>(configuration.GetSection("Application"));
            services.Configure<DatabaseSettings>(configuration.GetSection("Database"));

            // HTTP client factory for future web service integration
            services.AddHttpClient("DefaultClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            Log.Debug("Cross-cutting concerns configured successfully");
        }

        /// <summary>
        /// Centralized DbContext options configuration with execution strategy handling
        /// </summary>
        private static void ConfigureDbContextOptions(DbContextOptionsBuilder options, string connectionString,
            bool enableRetryOnFailure, bool useTransactionalOperations)
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                // Configure retry strategy based on transaction usage
                if (enableRetryOnFailure && !useTransactionalOperations)
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: new[] { -2, 1205, 1222 }); // Common transient error codes

                    Log.Information("SQL Server retry strategy enabled (explicit transactions disabled)");
                }
                else if (useTransactionalOperations)
                {
                    Log.Information("SQL Server retry strategy disabled to support explicit transactions");
                }

                sqlOptions.CommandTimeout(120); // Increased timeout for complex operations
                sqlOptions.MigrationsAssembly("PoultrySlaughterPOS");
            });

            // Performance and debugging configuration
            options.EnableSensitiveDataLogging(false); // Security: Never enable in production
            options.EnableServiceProviderCaching(true);
            options.EnableDetailedErrors(true);

            // Query tracking configuration based on transaction usage
            options.UseQueryTrackingBehavior(useTransactionalOperations ?
                QueryTrackingBehavior.TrackAll : QueryTrackingBehavior.NoTracking);

            // Advanced configuration for performance
            options.LogTo(message => Log.Debug("EF Core: {Message}", message), LogLevel.Debug);
        }

        #endregion

        #region Initialization Methods

        /// <summary>
        /// Initializes database with comprehensive error handling and validation
        /// </summary>
        private async Task InitializeDatabaseAsync()
        {
            try
            {
                using var scope = Services.CreateScope();
                var dbInitService = scope.ServiceProvider.GetRequiredService<IDatabaseInitializationService>();

                Log.Information("Database initialization started");
                await dbInitService.InitializeAsync();
                Log.Information("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Critical failure during database initialization");
                throw new ApplicationException("Database initialization failed. Please check the connection string and ensure SQL Server is running.", ex);
            }
        }

        /// <summary>
        /// Creates and displays the main window through dependency injection
        /// </summary>
        private async Task ShowMainWindowAsync()
        {
            try
            {
                var mainWindow = Services.GetRequiredService<MainWindow>();

                // Set as main window for proper application lifecycle
                MainWindow = mainWindow;

                // Show the window
                mainWindow.Show();

                Log.Information("Main window created and displayed successfully");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Critical failure creating main window");
                throw new ApplicationException("Failed to create main application window", ex);
            }
        }

        #endregion

        #region Error Handling

        /// <summary>
        /// Handles critical startup errors with user-friendly messaging
        /// </summary>
        private void HandleCriticalStartupError(Exception ex)
        {
            Log.Fatal(ex, "=== CRITICAL FAILURE DURING APPLICATION STARTUP ===");

            var errorMessage = ex switch
            {
                InvalidOperationException => "خطأ في إعدادات التطبيق أو قاعدة البيانات",
                UnauthorizedAccessException => "ليس لديك صلاحية لتشغيل التطبيق",
                FileNotFoundException => "ملفات التطبيق مفقودة أو تالفة",
                _ => "فشل حرج في تشغيل التطبيق"
            };

            MessageBox.Show(
                $"{errorMessage}\n\nتفاصيل الخطأ:\n{ex.Message}\n\nيرجى التواصل مع الدعم الفني.",
                "خطأ حرج في التطبيق",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            // Ensure proper cleanup
            try
            {
                _host?.Dispose();
            }
            catch (Exception cleanupEx)
            {
                Log.Error(cleanupEx, "Error during emergency cleanup");
            }

            // Force application shutdown
            Environment.Exit(1);
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Enterprise-grade DbContextFactory implementation with execution strategy configuration.
    /// Resolves conflicts between retry strategies and explicit transaction management.
    /// ENHANCED: Complete implementation with proper error handling and resource management.
    /// </summary>
    public class PoultryDbContextFactory : IDbContextFactory<PoultryDbContext>
    {
        private readonly string _connectionString;
        private readonly DbContextOptions<PoultryDbContext> _options;
        private readonly bool _enableRetryOnFailure;
        private readonly bool _useTransactionalOperations;

        public PoultryDbContextFactory(string connectionString, bool enableRetryOnFailure = false, bool useTransactionalOperations = true)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _enableRetryOnFailure = enableRetryOnFailure;
            _useTransactionalOperations = useTransactionalOperations;
            _options = CreateDbContextOptions();
        }

        /// <summary>
        /// Creates DbContext options with execution strategy configuration for factory pattern
        /// </summary>
        private DbContextOptions<PoultryDbContext> CreateDbContextOptions()
        {
            var optionsBuilder = new DbContextOptionsBuilder<PoultryDbContext>();

            optionsBuilder.UseSqlServer(_connectionString, sqlOptions =>
            {
                // Apply retry strategy only when explicit transactions are not used
                if (_enableRetryOnFailure && !_useTransactionalOperations)
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: new[] { -2, 1205, 1222 });
                }

                sqlOptions.CommandTimeout(120);
                sqlOptions.MigrationsAssembly("PoultrySlaughterPOS");
            });

            // Optimized settings for factory-created contexts
            optionsBuilder.EnableSensitiveDataLogging(false);
            optionsBuilder.EnableServiceProviderCaching(false);  // Disabled for thread safety
            optionsBuilder.EnableDetailedErrors(true);

            // Set tracking behavior based on transaction usage
            optionsBuilder.UseQueryTrackingBehavior(_useTransactionalOperations ?
                QueryTrackingBehavior.TrackAll : QueryTrackingBehavior.NoTracking);

            return optionsBuilder.Options;
        }

        /// <summary>
        /// Creates a new DbContext instance with factory-optimized configuration
        /// </summary>
        public PoultryDbContext CreateDbContext()
        {
            return new PoultryDbContext(_options);
        }

        /// <summary>
        /// Asynchronously creates a new DbContext instance
        /// </summary>
        public Task<PoultryDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CreateDbContext());
        }
    }

    /// <summary>
    /// Application settings configuration model for strongly-typed configuration
    /// </summary>
    public class ApplicationSettings
    {
        public string Name { get; set; } = "Poultry Slaughter POS";
        public string Version { get; set; } = "1.0.0";
        public int MaxRetryAttempts { get; set; } = 3;
        public int CommandTimeoutSeconds { get; set; } = 120;
        public bool EnableDetailedLogging { get; set; } = false;
        public string Theme { get; set; } = "Light";
        public string Language { get; set; } = "ar-SA";
        public bool EnablePerformanceCounters { get; set; } = false;
        public int CacheExpirationMinutes { get; set; } = 30;
    }

    /// <summary>
    /// Database settings configuration model
    /// </summary>
    public class DatabaseSettings
    {
        public bool UseExplicitTransactions { get; set; } = true;
        public bool EnableRetryOnFailure { get; set; } = false;
        public int CommandTimeoutSeconds { get; set; } = 120;
        public int MaxRetryCount { get; set; } = 3;
        public int MaxRetryDelaySeconds { get; set; } = 5;
        public bool EnableDetailedErrors { get; set; } = true;
        public bool EnableSensitiveDataLogging { get; set; } = false;
    }

    #endregion
}