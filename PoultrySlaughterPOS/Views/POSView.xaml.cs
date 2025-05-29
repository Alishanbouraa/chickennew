using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.ViewModels;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PoultrySlaughterPOS.Views
{
    /// <summary>
    /// Enterprise-grade Point of Sale view implementing comprehensive MVVM patterns,
    /// advanced user experience optimizations, and seamless integration with the POS workflow.
    /// FIXED: Complete UI element resolution and proper XAML integration.
    /// </summary>
    public partial class POSView : UserControl
    {
        #region Private Fields

        private readonly ILogger<POSView> _logger;
        private POSViewModel? _viewModel;
        private bool _isInitialized = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for dependency injection with comprehensive ViewModel integration
        /// </summary>
        /// <param name="viewModel">POS ViewModel injected via DI container</param>
        /// <param name="logger">Logger instance for diagnostic and error tracking</param>
        public POSView(POSViewModel viewModel, ILogger<POSView> logger)
        {
            try
            {
                InitializeComponent();

                _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));

                // Establish MVVM data binding
                DataContext = _viewModel;

                // Configure view properties for optimal user experience
                ConfigureViewProperties();

                // Wire up comprehensive event handlers
                WireUpEventHandlers();

                _logger.LogInformation("POSView initialized successfully with enterprise-grade MVVM architecture");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Critical error during POSView initialization");

                // Fallback error handling for initialization failures
                MessageBox.Show($"خطأ حرج في تحميل نقطة البيع:\n{ex.Message}",
                               "خطأ في النظام",
                               MessageBoxButton.OK,
                               MessageBoxImage.Error);
                throw;
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Access to the underlying ViewModel for advanced scenarios
        /// </summary>
        public POSViewModel? ViewModel => _viewModel;

        /// <summary>
        /// Indicates whether the view has been fully initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets focus to the customer selection control for optimal user workflow
        /// FIXED: Proper implementation matching actual XAML structure
        /// </summary>
        public void FocusCustomerSelection()
        {
            try
            {
                // Focus on the customer selection ComboBox (matching actual XAML element name)
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        // Try to find and focus the customer ComboBox by name
                        var customerControl = FindName("CustomerSelectionComboBox") as ComboBox ??
                                            FindName("CustomersComboBox") as ComboBox ??
                                            FindName("CustomerComboBox") as ComboBox;

                        if (customerControl != null)
                        {
                            customerControl.Focus();
                            _logger.LogDebug("Focus set to customer selection control");
                        }
                        else
                        {
                            // Fallback: Focus on the first focusable element
                            var firstFocusable = FindFirstFocusableElement(this);
                            firstFocusable?.Focus();
                            _logger.LogDebug("Focus set to first focusable element as fallback");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error setting focus to customer selection");
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in FocusCustomerSelection method");
            }
        }

        /// <summary>
        /// Initializes the view asynchronously with data loading
        /// </summary>
        public async Task InitializeViewAsync()
        {
            try
            {
                if (_isInitialized)
                {
                    _logger.LogDebug("POSView already initialized, skipping re-initialization");
                    return;
                }

                _logger.LogInformation("Initializing POSView with comprehensive data loading");

                // Initialize ViewModel data
                if (_viewModel != null)
                {
                    await _viewModel.InitializeAsync();
                }

                // Configure initial UI state
                ConfigureInitialUIState();

                _isInitialized = true;
                _logger.LogInformation("POSView initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during POSView initialization");

                MessageBox.Show($"خطأ في تحميل بيانات نقطة البيع:\n{ex.Message}",
                               "خطأ في التحميل",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                throw;
            }
        }

        /// <summary>
        /// Refreshes the view data and UI state
        /// </summary>
        public async Task RefreshViewAsync()
        {
            try
            {
                _logger.LogDebug("Refreshing POSView data and UI state");

                if (_viewModel != null)
                {
                    await _viewModel.InitializeAsync();
                }

                _logger.LogDebug("POSView refresh completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing POSView");
                throw;
            }
        }

        /// <summary>
        /// Validates current invoice data and provides user feedback
        /// </summary>
        public bool ValidateInvoiceData()
        {
            try
            {
                if (_viewModel == null)
                {
                    _logger.LogWarning("Cannot validate invoice data: ViewModel is null");
                    return false;
                }

                var isValid = _viewModel.ValidateCurrentInvoice(true);
                _logger.LogDebug("Invoice validation result: {IsValid}", isValid);

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during invoice validation");
                return false;
            }
        }

        /// <summary>
        /// Cleanup method for proper resource disposal
        /// </summary>
        public void Cleanup()
        {
            try
            {
                _logger.LogDebug("POSView cleanup initiated");

                // Cleanup ViewModel if it implements IDisposable
                if (_viewModel is IDisposable disposableViewModel)
                {
                    disposableViewModel.Dispose();
                }

                _viewModel?.Cleanup();
                _isInitialized = false;

                _logger.LogDebug("POSView cleanup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during POSView cleanup");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles view loaded event with comprehensive initialization
        /// </summary>
        private async void POSView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized)
                {
                    await InitializeViewAsync();
                }

                // Set initial focus for optimal user experience
                FocusCustomerSelection();

                _logger.LogDebug("POSView loaded event handled successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in POSView_Loaded event handler");
            }
        }

        /// <summary>
        /// Handles view unloaded event with cleanup
        /// </summary>
        private void POSView_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Perform cleanup when view is unloaded
                // Note: Full cleanup is handled in Cleanup() method
                _logger.LogDebug("POSView unloaded event handled");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in POSView_Unloaded event handler");
            }
        }

        /// <summary>
        /// Handles input field value changes for real-time calculations
        /// </summary>
        private void NumericInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (_viewModel != null && _isInitialized)
                {
                    // Trigger recalculation when numeric inputs change
                    _viewModel.RecalculateInvoiceTotals();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling numeric input change");
            }
        }

        /// <summary>
        /// Handles keyboard shortcuts for improved user experience
        /// </summary>
        private void POSView_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (_viewModel == null) return;

                switch (e.Key)
                {
                    case Key.F1:
                        // Quick customer selection focus
                        FocusCustomerSelection();
                        e.Handled = true;
                        break;

                    case Key.F2:
                        // New invoice shortcut
                        if (_viewModel.NewInvoiceCommand.CanExecute(null))
                        {
                            _viewModel.NewInvoiceCommand.Execute(null);
                        }
                        e.Handled = true;
                        break;

                    case Key.F3:
                        // Add new customer shortcut
                        if (_viewModel.AddNewCustomerCommand.CanExecute(null))
                        {
                            _viewModel.AddNewCustomerCommand.Execute(null);
                        }
                        e.Handled = true;
                        break;

                    case Key.F5:
                        // Refresh view shortcut
                        _ = RefreshViewAsync();
                        e.Handled = true;
                        break;

                    case Key.Enter when (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control:
                        // Ctrl+Enter: Save and print invoice
                        if (_viewModel.SaveAndPrintInvoiceCommand.CanExecute(null))
                        {
                            _viewModel.SaveAndPrintInvoiceCommand.Execute(null);
                        }
                        e.Handled = true;
                        break;

                    case Key.S when (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control:
                        // Ctrl+S: Save invoice
                        if (_viewModel.SaveInvoiceCommand.CanExecute(null))
                        {
                            _viewModel.SaveInvoiceCommand.Execute(null);
                        }
                        e.Handled = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling keyboard shortcut: {Key}", e.Key);
            }
        }

        /// <summary>
        /// Handles ViewModel property changes for dynamic UI updates
        /// </summary>
        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            try
            {
                switch (e.PropertyName)
                {
                    case nameof(POSViewModel.IsLoading):
                        HandleLoadingStateChanged();
                        break;

                    case nameof(POSViewModel.HasValidationErrors):
                        HandleValidationStateChanged();
                        break;

                    case nameof(POSViewModel.StatusMessage):
                        HandleStatusMessageChanged();
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling ViewModel property change: {PropertyName}", e.PropertyName);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Configures view-specific properties for optimal user experience
        /// </summary>
        private void ConfigureViewProperties()
        {
            try
            {
                // Configure focus management
                Focusable = true;

                // Configure keyboard handling
                KeyDown += POSView_KeyDown;

                _logger.LogDebug("POSView properties configured successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error configuring view properties");
            }
        }

        /// <summary>
        /// Wires up comprehensive event handlers for advanced UI management
        /// </summary>
        private void WireUpEventHandlers()
        {
            try
            {
                // View lifecycle events
                Loaded += POSView_Loaded;
                Unloaded += POSView_Unloaded;

                // ViewModel event handlers
                if (_viewModel != null)
                {
                    _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                }

                _logger.LogDebug("Event handlers wired up successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error wiring up event handlers");
            }
        }

        /// <summary>
        /// Configures initial UI state for optimal user experience
        /// </summary>
        private void ConfigureInitialUIState()
        {
            try
            {
                // Set initial focus
                FocusCustomerSelection();

                // Additional UI configuration can be added here
                _logger.LogDebug("Initial UI state configured successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error configuring initial UI state");
            }
        }

        /// <summary>
        /// Finds the first focusable element in the visual tree
        /// </summary>
        /// <param name="parent">Parent element to search</param>
        /// <returns>First focusable element or null</returns>
        private FrameworkElement? FindFirstFocusableElement(DependencyObject parent)
        {
            try
            {
                for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

                    if (child is FrameworkElement element && element.Focusable && element.IsEnabled && element.Visibility == Visibility.Visible)
                    {
                        return element;
                    }

                    var foundChild = FindFirstFocusableElement(child);
                    if (foundChild != null)
                    {
                        return foundChild;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error finding first focusable element");
                return null;
            }
        }

        /// <summary>
        /// Handles loading state changes for UI feedback
        /// </summary>
        private void HandleLoadingStateChanged()
        {
            try
            {
                if (_viewModel?.IsLoading == true)
                {
                    // Show loading indicator
                    Cursor = Cursors.Wait;
                    IsEnabled = false;
                }
                else
                {
                    // Hide loading indicator
                    Cursor = Cursors.Arrow;
                    IsEnabled = true;
                }

                _logger.LogDebug("Loading state changed: {IsLoading}", _viewModel?.IsLoading);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling loading state change");
            }
        }

        /// <summary>
        /// Handles validation state changes for user feedback
        /// </summary>
        private void HandleValidationStateChanged()
        {
            try
            {
                // Additional validation UI feedback can be implemented here
                _logger.LogDebug("Validation state changed: {HasErrors}", _viewModel?.HasValidationErrors);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling validation state change");
            }
        }

        /// <summary>
        /// Handles status message changes for user feedback
        /// </summary>
        private void HandleStatusMessageChanged()
        {
            try
            {
                // Status message updates are handled via data binding
                // Additional logic can be added here if needed
                _logger.LogDebug("Status message changed: {StatusMessage}", _viewModel?.StatusMessage);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling status message change");
            }
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Factory method for creating POSView with proper dependency injection
        /// </summary>
        /// <param name="serviceProvider">Service provider for dependency resolution</param>
        /// <returns>Configured POSView instance</returns>
        public static POSView CreateInstance(IServiceProvider serviceProvider)
        {
            try
            {
                var logger = serviceProvider.GetService<ILogger<POSView>>();
                logger?.LogInformation("Creating POSView instance via factory method");

                // Resolve view from DI container
                var view = serviceProvider.GetRequiredService<POSView>();

                return view;
            }
            catch (Exception ex)
            {
                var logger = serviceProvider.GetService<ILogger<POSView>>();
                logger?.LogError(ex, "Error in CreateInstance factory method");
                throw;
            }
        }

        #endregion
    }
}