using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PoultrySlaughterPOS.Extensions;
using System;
using System.Windows;

namespace PoultrySlaughterPOS.Extensions
{
    /// <summary>
    /// Enterprise-grade application extensions providing dependency injection access
    /// throughout the WPF application lifecycle with proper service provider management.
    /// 
    /// CRITICAL: Resolves CS1061 errors by providing Application.Services property access
    /// for dependency injection container integration in WPF MVVM architecture.
    /// </summary>
    public static class ApplicationExtensions
    {
        #region Private Fields

        private static IServiceProvider? _serviceProvider;
        private static readonly object _lockObject = new object();

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the current service provider instance with thread-safe access.
        /// Provides enterprise-grade dependency injection access throughout the application.
        /// </summary>
        public static IServiceProvider Services
        {
            get
            {
                lock (_lockObject)
                {
                    if (_serviceProvider == null)
                    {
                        throw new InvalidOperationException(
                            "Service provider has not been initialized. " +
                            "Ensure ConfigureServiceProvider is called during application startup.");
                    }
                    return _serviceProvider;
                }
            }
        }

        /// <summary>
        /// Indicates whether the service provider has been properly configured
        /// </summary>
        public static bool IsServiceProviderConfigured => _serviceProvider != null;

        #endregion

        #region Configuration Methods

        /// <summary>
        /// Configures the service provider for application-wide dependency injection access.
        /// CRITICAL: Must be called during App.xaml.cs startup to enable Services property.
        /// </summary>
        /// <param name="app">WPF Application instance</param>
        /// <param name="serviceProvider">Configured service provider from host</param>
        /// <exception cref="ArgumentNullException">Thrown when parameters are null</exception>
        /// <exception cref="InvalidOperationException">Thrown when already configured</exception>
        public static void ConfigureServiceProvider(this Application app, IServiceProvider serviceProvider)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            lock (_lockObject)
            {
                if (_serviceProvider != null)
                {
                    throw new InvalidOperationException(
                        "Service provider has already been configured. " +
                        "ConfigureServiceProvider should only be called once during application startup.");
                }

                _serviceProvider = serviceProvider;
            }
        }

        /// <summary>
        /// Configures the service provider from an IHost instance with proper lifecycle management
        /// </summary>
        /// <param name="app">WPF Application instance</param>
        /// <param name="host">Configured host containing service provider</param>
        public static void ConfigureServiceProvider(this Application app, IHost host)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));

            app.ConfigureServiceProvider(host.Services);
        }

        /// <summary>
        /// Safely disposes the service provider during application shutdown.
        /// Ensures proper resource cleanup and prevents memory leaks.
        /// </summary>
        /// <param name="app">WPF Application instance</param>
        public static void DisposeServiceProvider(this Application app)
        {
            lock (_lockObject)
            {
                if (_serviceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                _serviceProvider = null;
            }
        }

        #endregion

        #region Service Access Methods

        /// <summary>
        /// Gets a required service of type T with enhanced error handling.
        /// Provides type-safe service resolution with comprehensive error messaging.
        /// </summary>
        /// <typeparam name="T">Service type to resolve</typeparam>
        /// <returns>Service instance of type T</returns>
        /// <exception cref="InvalidOperationException">Service provider not configured or service not found</exception>
        public static T GetRequiredService<T>(this Application app) where T : notnull
        {
            try
            {
                return Services.GetRequiredService<T>();
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Service provider"))
            {
                throw new InvalidOperationException(
                    $"Cannot resolve service of type '{typeof(T).Name}' because the service provider is not configured. " +
                    "Ensure App.ConfigureServiceProvider() is called during application startup.", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(
                    $"Service of type '{typeof(T).Name}' is not registered in the dependency injection container. " +
                    "Verify the service is properly registered in App.xaml.cs ConfigureServices method.", ex);
            }
        }

        /// <summary>
        /// Gets an optional service of type T, returning null if not found.
        /// Provides safe service resolution without exceptions for optional dependencies.
        /// </summary>
        /// <typeparam name="T">Service type to resolve</typeparam>
        /// <returns>Service instance of type T or null if not found</returns>
        public static T? GetService<T>(this Application app) where T : class
        {
            try
            {
                return Services.GetService<T>();
            }
            catch (InvalidOperationException)
            {
                // Return null for optional services when provider is not configured
                return null;
            }
        }

        /// <summary>
        /// Creates a new service scope for scoped service resolution.
        /// Essential for database operations and transactional scenarios.
        /// </summary>
        /// <param name="app">WPF Application instance</param>
        /// <returns>Service scope for scoped service resolution</returns>
        public static IServiceScope CreateScope(this Application app)
        {
            var scopeFactory = Services.GetRequiredService<IServiceScopeFactory>();
            return scopeFactory.CreateScope();
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Validates that essential services are properly registered.
        /// Provides startup validation to catch configuration issues early.
        /// </summary>
        /// <param name="app">WPF Application instance</param>
        /// <returns>True if all essential services are available</returns>
        public static bool ValidateEssentialServices(this Application app)
        {
            try
            {
                if (!IsServiceProviderConfigured)
                    return false;

                // Validate core services are available
                var scopeFactory = Services.GetService<IServiceScopeFactory>();
                if (scopeFactory == null)
                    return false;

                // Test scope creation
                using var scope = scopeFactory.CreateScope();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion
    }
}

namespace PoultrySlaughterPOS
{
    /// <summary>
    /// Application extension to provide direct Services property access.
    /// Enables clean syntax: App.Current.Services.GetRequiredService<T>()
    /// </summary>
    public partial class App
    {
        /// <summary>
        /// Provides direct access to the dependency injection service provider.
        /// CRITICAL: Resolves CS1061 'Application' does not contain definition for 'Services' errors.
        /// </summary>
        public static IServiceProvider Services => Extensions.ApplicationExtensions.Services;

        // NOTE: OnExit method removed to prevent CS0111 duplicate method error
        // The comprehensive OnExit implementation in App.xaml.cs already handles 
        // service provider disposal by calling this.DisposeServiceProvider()
    }
}