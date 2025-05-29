using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.RegularExpressions;

namespace PoultrySlaughterPOS.Data
{
    /// <summary>
    /// Production-grade design-time DbContext factory implementing IDesignTimeDbContextFactory
    /// for Entity Framework Core migrations and scaffolding operations.
    /// 
    /// This implementation completely bypasses the application's dependency injection container
    /// and host infrastructure to resolve design-time context creation conflicts during migration execution.
    /// 
    /// Architecture Benefits:
    /// - Eliminates HostAbortedException during EF Core tooling operations
    /// - Provides isolated configuration management for design-time scenarios
    /// - Maintains full SQL Server LocalDB connectivity for offline operations
    /// - Supports both Package Manager Console and .NET CLI migration workflows
    /// </summary>
    public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PoultryDbContext>
    {
        /// <summary>
        /// Creates a PoultryDbContext instance specifically optimized for design-time operations.
        /// Used exclusively by EF Core tooling (migrations, scaffolding, database updates) when the 
        /// application dependency injection container is unavailable or conflicts with host startup.
        /// </summary>
        /// <param name="args">Command-line arguments passed from EF Core tooling infrastructure</param>
        /// <returns>Fully configured PoultryDbContext instance for design-time operations</returns>
        public PoultryDbContext CreateDbContext(string[] args)
        {
            // Step 1: Build design-time configuration with intelligent fallback mechanisms
            var configuration = BuildDesignTimeConfiguration();

            // Step 2: Extract and validate database connection string
            var connectionString = ExtractConnectionStringWithValidation(configuration);

            // Step 3: Create and configure DbContext options with migration-optimized settings
            var optionsBuilder = new DbContextOptionsBuilder<PoultryDbContext>();
            ConfigureDesignTimeDbContextOptions(optionsBuilder, connectionString);

            // Step 4: Log design-time context creation for diagnostic purposes
            LogDesignTimeContextCreation(connectionString);

            return new PoultryDbContext(optionsBuilder.Options);
        }

        /// <summary>
        /// Constructs comprehensive configuration infrastructure for design-time operations
        /// with intelligent project root detection and environment-specific overrides.
        /// </summary>
        /// <returns>Configured IConfiguration instance with design-time settings</returns>
        private static IConfiguration BuildDesignTimeConfiguration()
        {
            var projectRoot = LocateProjectRootDirectory();

            var configurationBuilder = new ConfigurationBuilder()
                .SetBasePath(projectRoot)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
                .AddJsonFile("appsettings.Migration.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables("POULTRY_POS_");

            // Add command-line arguments if available (for advanced migration scenarios)
            var environmentArgs = Environment.GetCommandLineArgs()
                .Where(arg => arg.StartsWith("--"))
                .ToArray();

            if (environmentArgs.Length > 0)
            {
                configurationBuilder.AddCommandLine(environmentArgs);
            }

            return configurationBuilder.Build();
        }

        /// <summary>
        /// Implements intelligent project root directory detection algorithm for reliable
        /// configuration file access across different execution contexts (IDE, CLI, CI/CD).
        /// </summary>
        /// <returns>Absolute path to project root directory</returns>
        private static string LocateProjectRootDirectory()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var searchDirectory = new DirectoryInfo(currentDirectory);

            // Search upward through directory hierarchy for project indicators
            while (searchDirectory != null)
            {
                var projectIndicators = new[]
                {
                    "appsettings.json",
                    "PoultrySlaughterPOS.csproj",
                    "PoultrySlaughterPOS.sln"
                };

                if (projectIndicators.Any(indicator =>
                    File.Exists(Path.Combine(searchDirectory.FullName, indicator))))
                {
                    return searchDirectory.FullName;
                }

                searchDirectory = searchDirectory.Parent;
            }

            // Fallback to current directory if project root not found
            Console.WriteLine($"Warning: Project root not detected, using current directory: {currentDirectory}");
            return currentDirectory;
        }

        /// <summary>
        /// Extracts database connection string from configuration with comprehensive validation
        /// and intelligent fallback logic for various deployment scenarios.
        /// </summary>
        /// <param name="configuration">Configuration instance containing connection strings</param>
        /// <returns>Validated database connection string</returns>
        private static string ExtractConnectionStringWithValidation(IConfiguration configuration)
        {
            // Primary: Use explicitly configured connection string
            var primaryConnectionString = configuration.GetConnectionString("DefaultConnection");

            if (!string.IsNullOrWhiteSpace(primaryConnectionString))
            {
                ValidateConnectionStringFormat(primaryConnectionString);
                return primaryConnectionString;
            }

            // Secondary: Attempt alternative connection string keys
            var alternativeKeys = new[] { "Database", "SqlServer", "LocalDB" };

            foreach (var key in alternativeKeys)
            {
                var alternativeConnectionString = configuration.GetConnectionString(key);
                if (!string.IsNullOrWhiteSpace(alternativeConnectionString))
                {
                    Console.WriteLine($"Using alternative connection string key: {key}");
                    ValidateConnectionStringFormat(alternativeConnectionString);
                    return alternativeConnectionString;
                }
            }

            // Tertiary: Generate development-optimized LocalDB connection string
            var fallbackConnectionString = GenerateDevelopmentConnectionString();
            Console.WriteLine($"Using generated development connection string: {fallbackConnectionString}");

            return fallbackConnectionString;
        }

        /// <summary>
        /// Validates connection string format and essential components for SQL Server LocalDB connectivity.
        /// </summary>
        /// <param name="connectionString">Connection string to validate</param>
        private static void ValidateConnectionStringFormat(string connectionString)
        {
            var requiredComponents = new[] { "Server", "Database" };

            foreach (var component in requiredComponents)
            {
                if (!connectionString.Contains(component, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Connection string validation failed: Missing required component '{component}'. " +
                        $"Connection string: {connectionString}");
                }
            }
        }

        /// <summary>
        /// Generates development-optimized LocalDB connection string with enterprise-grade configuration.
        /// </summary>
        /// <returns>Fully configured LocalDB connection string</returns>
        private static string GenerateDevelopmentConnectionString()
        {
            var databaseName = "PoultrySlaughterPOS_Development";
            var serverInstance = Environment.GetEnvironmentVariable("LOCALDB_INSTANCE") ?? "MSSQLLocalDB";

            return $"Server=(localdb)\\{serverInstance};" +
                   $"Database={databaseName};" +
                   $"Trusted_Connection=true;" +
                   $"MultipleActiveResultSets=true;" +
                   $"TrustServerCertificate=true;" +
                   $"ConnectRetryCount=3;" +
                   $"ConnectRetryInterval=5;";
        }

        /// <summary>
        /// Configures DbContext options with migration-optimized settings for enhanced
        /// performance and reliability during design-time operations.
        /// </summary>
        /// <param name="optionsBuilder">DbContextOptionsBuilder to configure</param>
        /// <param name="connectionString">Validated database connection string</param>
        private static void ConfigureDesignTimeDbContextOptions(
            DbContextOptionsBuilder<PoultryDbContext> optionsBuilder,
            string connectionString)
        {
            optionsBuilder.UseSqlServer(connectionString, sqlServerOptions =>
            {
                // Migration-optimized SQL Server configuration
                sqlServerOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);

                sqlServerOptions.CommandTimeout(600); // Extended timeout for complex migrations
                sqlServerOptions.MigrationsAssembly("PoultrySlaughterPOS");

                // Enable advanced SQL Server features for enterprise scenarios
                sqlServerOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
            });

            // Design-time optimization configuration
            optionsBuilder.EnableSensitiveDataLogging(true);  // Enhanced diagnostics for migration troubleshooting
            optionsBuilder.EnableDetailedErrors(true);        // Comprehensive error reporting
            optionsBuilder.EnableServiceProviderCaching(false); // Disable for design-time thread safety
            optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll); // Full entity tracking for migrations

            // Configure logging for design-time operations with structured output
            optionsBuilder.LogTo(message =>
            {
                if (message.Contains("Executing DbCommand", StringComparison.OrdinalIgnoreCase) ||
                    message.Contains("Executed DbCommand", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[EF-Migration] {DateTime.Now:HH:mm:ss.fff} {message}");
                }
            }, LogLevel.Information);
        }

        /// <summary>
        /// Logs design-time context creation with comprehensive diagnostic information.
        /// </summary>
        /// <param name="connectionString">Connection string being used (sanitized for logging)</param>
        private static void LogDesignTimeContextCreation(string connectionString)
        {
            var sanitizedConnectionString = SanitizeConnectionStringForLogging(connectionString);

            Console.WriteLine("==================================================");
            Console.WriteLine("EF Core Design-Time Context Factory Initialized");
            Console.WriteLine("==================================================");
            Console.WriteLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            Console.WriteLine($"Process: {Environment.ProcessPath ?? "Unknown"}");
            Console.WriteLine($"Working Directory: {Directory.GetCurrentDirectory()}");
            Console.WriteLine($"Connection: {sanitizedConnectionString}");
            Console.WriteLine($"EF Core Version: {typeof(DbContext).Assembly.GetName().Version}");
            Console.WriteLine("==================================================");
        }

        /// <summary>
        /// Sanitizes connection string for secure logging by removing sensitive authentication information.
        /// </summary>
        /// <param name="connectionString">Original connection string</param>
        /// <returns>Sanitized connection string safe for logging</returns>
        private static string SanitizeConnectionStringForLogging(string connectionString)
        {
            var sensitivePatterns = new[]
            {
                @"Password\s*=\s*[^;]+",
                @"PWD\s*=\s*[^;]+",
                @"User\s+ID\s*=\s*[^;]+",
                @"UID\s*=\s*[^;]+"
            };

            var sanitized = connectionString;

            foreach (var pattern in sensitivePatterns)
            {
                sanitized = Regex.Replace(
                    sanitized, pattern, "***REDACTED***",
                    RegexOptions.IgnoreCase);
            }

            return sanitized;
        }
    }
}