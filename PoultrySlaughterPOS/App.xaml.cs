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
using Serilog;
using System.IO;
using System.Windows;

namespace PoultrySlaughterPOS
{
    /// <summary>
    /// Enterprise-grade WPF application entry point with comprehensive dependency injection,
    /// logging configuration, and service registration for the Poultry Slaughter POS system
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

                Log.Information("Starting Poultry Slaughter POS application...");

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
                Log.Information("Application host started successfully");

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

                Log.Information("Poultry Slaughter POS application started successfully");
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
        /// Configures all services for dependency injection with enterprise-grade patterns
        /// </summary>
        /// <param name="services">Service collection for DI container</param>
        /// <param name="configuration">Application configuration</param>
        private void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Register configuration
            services.AddSingleton(configuration);

            // Configure Entity Framework DbContext with optimized settings for offline operations
            services.AddDbContext<PoultryDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                    sqlOptions.CommandTimeout(60); // 60 seconds timeout for complex operations
                });

                options.EnableSensitiveDataLogging(false);
                options.EnableServiceProviderCaching(false);
                options.LogTo(Console.WriteLine, LogLevel.Warning);

                // Enhanced performance for offline scenarios
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            }, ServiceLifetime.Scoped);

            // Configure DbContextFactory for BaseRepository implementations with optimized connection pooling
            services.AddDbContextFactory<PoultryDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                    sqlOptions.CommandTimeout(60);
                });

                options.EnableSensitiveDataLogging(false);
                options.EnableServiceProviderCaching(false);
                options.LogTo(Console.WriteLine, LogLevel.Warning);
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            }, ServiceLifetime.Scoped);

            // Register repositories with comprehensive dependency injection patterns
            services.AddScoped<ITruckRepository, TruckRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<ITruckLoadRepository, TruckLoadRepository>();
            services.AddScoped<IDailyReconciliationRepository, DailyReconciliationRepository>();
            services.AddScoped<IAuditLogRepository, AuditLogRepository>();

            // Register Unit of Work with explicit factory registration for proper constructor injection
            services.AddScoped<IUnitOfWork>(serviceProvider =>
            {
                var context = serviceProvider.GetRequiredService<PoultryDbContext>();
                var contextFactory = serviceProvider.GetRequiredService<IDbContextFactory<PoultryDbContext>>();
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

                return new UnitOfWork(context, contextFactory, loggerFactory);
            });

            // Register business services with comprehensive error handling support
            services.AddTransient<IDatabaseInitializationService, DatabaseInitializationService>();
            services.AddScoped<IErrorHandlingService, ErrorHandlingService>();
            services.AddScoped<ITruckLoadingService, TruckLoadingService>();

            // Register ViewModels with proper scoping for MVVM pattern
            services.AddTransient<TruckLoadingViewModel>();

            // Register Views with dependency injection support
            services.AddTransient<TruckLoadingView>();

            // Register main window and shell components
            services.AddSingleton<MainWindow>();

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

            Log.Information("Service configuration completed successfully with {ServiceCount} services registered",
                           services.Count);
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

            // Background services for periodic tasks (if needed)
            // services.AddHostedService<DatabaseMaintenanceService>();

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

        /// <summary>
        /// Configures global exception handling for unhandled exceptions
        /// </summary>
        private void ConfigureGlobalExceptionHandling()
        {
            // Global exception handling
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var exception = args.ExceptionObject as Exception;
                Log.Fatal(exception, "Unhandled domain exception occurred");

                MessageBox.Show($"حدث خطأ غير متوقع في النظام:\n{exception?.Message}",
                               "خطأ غير متوقع",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
            };

            DispatcherUnhandledException += (sender, args) =>
            {
                Log.Error(args.Exception, "Unhandled dispatcher exception occurred");

                MessageBox.Show($"حدث خطأ في واجهة المستخدم:\n{args.Exception.Message}",
                               "خطأ في الواجهة",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);

                args.Handled = true; // Prevent application crash
            };
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