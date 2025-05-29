using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Data;
using PoultrySlaughterPOS.Repositories;
using PoultrySlaughterPOS.Services;
using PoultrySlaughterPOS.Services.Repositories;
using PoultrySlaughterPOS.Services.Repositories.Implementations;
using PoultrySlaughterPOS.ViewModels;
using PoultrySlaughterPOS.Views;
using PoultrySlaughterPOS.Views.Dialogs;
using Serilog;
using System.IO;
using System.Windows;

namespace PoultrySlaughterPOS
{
    /// <summary>
    /// Enterprise-grade WPF application entry point with comprehensive dependency injection,
    /// logging configuration, and service registration for the Poultry Slaughter POS system.
    /// FIXED: Proper execution strategy configuration and complete POS module integration.
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Configure Serilog with enhanced logging for enterprise operations
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .WriteTo.File("logs/pos-log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
                    .WriteTo.File("logs/errors/error-log-.txt",
                                  rollingInterval: RollingInterval.Day,
                                  restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error,
                                  retainedFileCountLimit: 90)
                    .MinimumLevel.Information()
                    .Enrich.WithThreadId()
                    .Enrich.WithMachineName()
                    .CreateLogger();

                Log.Information("Starting Poultry Slaughter POS application with complete POS module integration...");

                // Build configuration with enhanced settings support
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "Production"}.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();

                // Configure host with comprehensive dependency injection
                _host = Host.CreateDefaultBuilder()
                    .UseSerilog()
                    .ConfigureServices((context, services) =>
                    {
                        ConfigureServices(services, configuration);
                    })
                    .Build();

                // Start the host
                await _host.StartAsync();
                Log.Information("Application host started successfully with complete POS module configuration");

                // Initialize database with enhanced error handling
                using (var scope = _host.Services.CreateScope())
                {
                    var dbInitService = scope.ServiceProvider.GetRequiredService<IDatabaseInitializationService>();
                    await dbInitService.InitializeAsync();
                    Log.Information("Database initialization completed successfully");
                }

                // Create and show main window through DI
                var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                mainWindow.Show();

                Log.Information("Poultry Slaughter POS application started successfully with complete POS integration");
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Critical failure during application startup");
                MessageBox.Show($"فشل في تشغيل البرنامج:\n{ex.Message}",
                               "خطأ حرج",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
                Shutdown();
            }
        }

        /// <summary>
        /// Configures all services for dependency injection with enterprise-grade patterns.
        /// FIXED: Complete POS module service registration and repository dependencies.
        /// </summary>
        /// <param name="services">Service collection for DI container</param>
        /// <param name="configuration">Application configuration</param>
        private void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Register configuration
            services.AddSingleton(configuration);

            // ✅ FIXED: Conditional execution strategy configuration
            var useTransactionalOperations = configuration.GetValue<bool>("Database:UseExplicitTransactions", true);
            var enableRetryOnFailure = configuration.GetValue<bool>("Database:EnableRetryOnFailure", false);

            Log.Information("Database configuration - UseExplicitTransactions: {UseTransactions}, EnableRetryOnFailure: {EnableRetry}",
                useTransactionalOperations, enableRetryOnFailure);

            // ✅ FIXED: Primary DbContext registration with conditional retry strategy
            services.AddDbContext<PoultryDbContext>(options =>
            {
                ConfigureDbContextOptions(options, configuration, enableRetryOnFailure, useTransactionalOperations);
            }, ServiceLifetime.Scoped);

            // ✅ FIXED: Manual DbContextFactory registration with consistent configuration
            services.AddSingleton<IDbContextFactory<PoultryDbContext>>(serviceProvider =>
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                return new PoultryDbContextFactory(connectionString!, enableRetryOnFailure, useTransactionalOperations);
            });

            // ✅ COMPLETE: Register all repositories with comprehensive dependency injection
            services.AddScoped<ITruckRepository, TruckRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<ITruckLoadRepository, TruckLoadRepository>();
            services.AddScoped<IDailyReconciliationRepository, DailyReconciliationRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();

            // ✅ FIXED: Register Unit of Work with proper DbContext injection
            services.AddScoped<IUnitOfWork>(serviceProvider =>
            {
                var context = serviceProvider.GetRequiredService<PoultryDbContext>();
                var contextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PoultryDbContext>>();
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

                return new UnitOfWork(context, contextFactory, loggerFactory);
            });

            // ✅ NEW: Register business services with comprehensive error handling support
            services.AddTransient<IDatabaseInitializationService, DatabaseInitializationService>();
            services.AddScoped<IErrorHandlingService, ErrorHandlingService>();
            services.AddScoped<ITruckLoadingService, TruckLoadingService>();

            // ✅ NEW: Register POS Service with complete business logic integration
            services.AddScoped<IPOSService, POSService>();

            // ✅ NEW: Register ViewModels with proper scoping for MVVM pattern
            services.AddTransient<TruckLoadingViewModel>();
            services.AddTransient<POSViewModel>();
            services.AddTransient<AddCustomerDialogViewModel>();

            // ✅ NEW: Register Views with dependency injection support
            services.AddTransient<TruckLoadingView>();
            services.AddTransient<POSView>();
            services.AddTransient<AddCustomerDialog>();

            // ✅ FIXED: Register MainWindow as TRANSIENT
            services.AddTransient<MainWindow>();

            // Configure enhanced logging with Serilog integration
            services.AddLogging(builder =>
            {
                builder.AddSerilog();
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
                builder.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.Warning);
            });

            // Register additional services for future expansion
            ConfigureAdditionalServices(services, configuration);

            Log.Information("Service configuration completed successfully with {ServiceCount} services registered (complete POS module)",
                           services.Count);
        }

        /// <summary>
        /// Centralized DbContext options configuration with FIXED execution strategy handling
        /// </summary>
        /// <param name="options">DbContext options builder</param>
        /// <param name="configuration">Application configuration</param>
        /// <param name="enableRetryOnFailure">Whether to enable automatic retry on failure</param>
        /// <param name="useTransactionalOperations">Whether to support explicit transactions</param>
        private static void ConfigureDbContextOptions(DbContextOptionsBuilder options, IConfiguration configuration,
            bool enableRetryOnFailure, bool useTransactionalOperations)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            options.UseSqlServer(connectionString, sqlOptions =>
            {
                // ✅ FIXED: Conditional retry configuration based on transaction usage
                if (enableRetryOnFailure && !useTransactionalOperations)
                {
                    // Enable retry only when NOT using explicit transactions
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);

                    Log.Information("SQL Server retry strategy enabled (explicit transactions disabled)");
                }
                else if (useTransactionalOperations)
                {
                    // Disable retry when using explicit transactions to avoid conflicts
                    Log.Information("SQL Server retry strategy disabled to support explicit transactions");
                }

                sqlOptions.CommandTimeout(60);
            });

            // ✅ FIXED: Optimized settings for transactional vs non-transactional usage
            options.EnableSensitiveDataLogging(false);
            options.EnableServiceProviderCaching(true);
            options.LogTo(Console.WriteLine, LogLevel.Warning);

            // Set tracking behavior based on transaction usage
            options.UseQueryTrackingBehavior(useTransactionalOperations ?
                QueryTrackingBehavior.TrackAll : QueryTrackingBehavior.NoTracking);
        }

        /// <summary>
        /// Configures additional services for extensibility and future feature development
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Application configuration</param>
        private void ConfigureAdditionalServices(IServiceCollection services, IConfiguration configuration)
        {
            // Memory caching for performance optimization
            services.AddMemoryCache(options =>
            {
                options.SizeLimit = 100; // Limit cache size for offline scenarios
            });

            // Configuration options pattern for strongly-typed settings
            services.Configure<ApplicationSettings>(configuration.GetSection("Application"));

            // Add HTTP client factory for future web service integration (currently unused)
            services.AddHttpClient();

            Log.Debug("Additional services configured successfully");
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            try
            {
                Log.Information("Shutting down Poultry Slaughter POS application...");

                if (_host != null)
                {
                    await _host.StopAsync(TimeSpan.FromSeconds(10));
                    _host.Dispose();
                    Log.Information("Application host stopped successfully");
                }

                base.OnExit(e);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred during application shutdown");
            }
            finally
            {
                Log.Information("Poultry Slaughter POS application shutdown completed");
                Log.CloseAndFlush();
            }
        }
    }

    /// <summary>
    /// Enterprise-grade DbContextFactory implementation with FIXED execution strategy configuration.
    /// Resolves conflicts between retry strategies and explicit transaction management.
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
        /// Creates DbContext options with FIXED execution strategy configuration for factory pattern
        /// </summary>
        /// <returns>Configured DbContext options</returns>
        private DbContextOptions<PoultryDbContext> CreateDbContextOptions()
        {
            var optionsBuilder = new DbContextOptionsBuilder<PoultryDbContext>();

            optionsBuilder.UseSqlServer(_connectionString, sqlOptions =>
            {
                // ✅ FIXED: Apply retry strategy only when explicit transactions are not used
                if (_enableRetryOnFailure && !_useTransactionalOperations)
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                }

                sqlOptions.CommandTimeout(60);
            });

            // Optimized settings for factory-created contexts
            optionsBuilder.EnableSensitiveDataLogging(false);
            optionsBuilder.EnableServiceProviderCaching(false);  // Disabled for thread safety

            // ✅ FIXED: Set tracking behavior based on transaction usage
            optionsBuilder.UseQueryTrackingBehavior(_useTransactionalOperations ?
                QueryTrackingBehavior.TrackAll : QueryTrackingBehavior.NoTracking);

            return optionsBuilder.Options;
        }

        /// <summary>
        /// Creates a new DbContext instance with factory-optimized configuration
        /// </summary>
        /// <returns>New PoultryDbContext instance</returns>
        public PoultryDbContext CreateDbContext()
        {
            return new PoultryDbContext(_options);
        }

        /// <summary>
        /// Asynchronously creates a new DbContext instance
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>New PoultryDbContext instance</returns>
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
        public int CommandTimeoutSeconds { get; set; } = 60;
        public bool EnableDetailedLogging { get; set; } = false;
        public string Theme { get; set; } = "Light";
        public string Language { get; set; } = "ar-SA";
    }
}