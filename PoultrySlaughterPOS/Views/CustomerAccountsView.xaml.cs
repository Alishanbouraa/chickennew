using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.ViewModels;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PoultrySlaughterPOS.Views
{
    /// <summary>
    /// Enterprise-grade Customer Accounts View implementing comprehensive customer management interface
    /// with advanced search capabilities, real-time filtering, and integrated transaction management.
    /// Optimized for high-performance operations with keyboard shortcuts and accessibility support.
    /// </summary>
    public partial class CustomerAccountsView : UserControl
    {
        #region Private Fields

        private readonly ILogger<CustomerAccountsView> _logger;
        private CustomerAccountsViewModel? _viewModel;
        private bool _isInitialized = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes CustomerAccountsView with dependency injection support
        /// </summary>
        public CustomerAccountsView()
        {
            try
            {
                InitializeComponent();

                // Initialize logger if available through service provider - FIXED: Use App.Services instead of App.Current.Services
                _logger = App.Services?.GetService<ILogger<CustomerAccountsView>>()
                          ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<CustomerAccountsView>.Instance;

                ConfigureEventHandlers();
                ConfigureKeyboardShortcuts();

                _logger.LogDebug("CustomerAccountsView initialized successfully");
            }
            catch (Exception ex)
            {
                // Fallback logging if logger not available
                System.Diagnostics.Debug.WriteLine($"Error initializing CustomerAccountsView: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Constructor with explicit ViewModel injection for DI container support
        /// </summary>
        /// <param name="viewModel">Pre-configured CustomerAccountsViewModel instance</param>
        public CustomerAccountsView(CustomerAccountsViewModel viewModel) : this()
        {
            SetViewModel(viewModel);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the ViewModel and initializes the view with comprehensive data loading
        /// </summary>
        /// <param name="viewModel">CustomerAccountsViewModel instance</param>
        public void SetViewModel(CustomerAccountsViewModel viewModel)
        {
            try
            {
                _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
                DataContext = _viewModel;

                _logger.LogInformation("ViewModel set successfully for CustomerAccountsView");

                // Initialize the ViewModel asynchronously
                _ = InitializeViewModelAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting ViewModel for CustomerAccountsView");
                throw;
            }
        }

        /// <summary>
        /// Manual initialization method for cases where automatic initialization is not desired
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                if (_isInitialized)
                {
                    _logger.LogDebug("CustomerAccountsView already initialized, skipping");
                    return;
                }

                if (_viewModel != null)
                {
                    await _viewModel.InitializeAsync();
                    _isInitialized = true;
                    _logger.LogInformation("CustomerAccountsView initialization completed successfully");
                }
                else
                {
                    _logger.LogWarning("Cannot initialize CustomerAccountsView: ViewModel is null");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during CustomerAccountsView initialization");
                throw;
            }
        }

        /// <summary>
        /// Cleanup method for proper resource disposal
        /// </summary>
        public void Cleanup()
        {
            try
            {
                _viewModel?.Cleanup();
                _isInitialized = false;
                _logger.LogDebug("CustomerAccountsView cleanup completed");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during CustomerAccountsView cleanup");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the Loaded event to initialize the view when it becomes visible
        /// </summary>
        private async void CustomerAccountsView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_isInitialized && _viewModel != null)
                {
                    await InitializeAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during CustomerAccountsView Loaded event");
                MessageBox.Show(
                    "حدث خطأ أثناء تحميل صفحة الزبائن. يرجى المحاولة مرة أخرى.",
                    "خطأ في التحميل",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Handles the Unloaded event for cleanup operations
        /// </summary>
        private void CustomerAccountsView_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Cleanup();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during CustomerAccountsView Unloaded event");
            }
        }

        /// <summary>
        /// Handles keyboard shortcuts for enhanced user productivity
        /// </summary>
        private void CustomerAccountsView_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (_viewModel == null) return;

                // Handle keyboard shortcuts
                switch (e.Key)
                {
                    case Key.F1:
                        // Add new customer (F1)
                        if (_viewModel.AddNewCustomerCommand.CanExecute(null))
                        {
                            _viewModel.AddNewCustomerCommand.Execute(null);
                            e.Handled = true;
                        }
                        break;

                    case Key.F2:
                        // Edit selected customer (F2)
                        if (_viewModel.EditCustomerCommand.CanExecute(null))
                        {
                            _viewModel.EditCustomerCommand.Execute(null);
                            e.Handled = true;
                        }
                        break;

                    case Key.F5:
                        // Refresh data (F5)
                        if (_viewModel.RefreshDataCommand.CanExecute(null))
                        {
                            _viewModel.RefreshDataCommand.Execute(null);
                            e.Handled = true;
                        }
                        break;

                    case Key.Delete:
                        // Delete selected customer (Delete key)
                        if (_viewModel.DeleteCustomerCommand.CanExecute(null))
                        {
                            _viewModel.DeleteCustomerCommand.Execute(null);
                            e.Handled = true;
                        }
                        break;

                    case Key.Escape:
                        // Clear filters (Escape)
                        if (_viewModel.ClearFiltersCommand.CanExecute(null))
                        {
                            _viewModel.ClearFiltersCommand.Execute(null);
                            e.Handled = true;
                        }
                        break;

                    default:
                        // Handle Ctrl key combinations
                        if (Keyboard.Modifiers == ModifierKeys.Control)
                        {
                            switch (e.Key)
                            {
                                case Key.F:
                                    // Focus search box (Ctrl+F)
                                    SearchTextBox?.Focus();
                                    SearchTextBox?.SelectAll();
                                    e.Handled = true;
                                    break;

                                case Key.R:
                                    // Refresh data (Ctrl+R)
                                    if (_viewModel.RefreshDataCommand.CanExecute(null))
                                    {
                                        _viewModel.RefreshDataCommand.Execute(null);
                                        e.Handled = true;
                                    }
                                    break;
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling keyboard shortcut: {Key}", e.Key);
            }
        }

        /// <summary>
        /// Handles DataGrid row double-click for quick customer editing
        /// </summary>
        private void CustomersDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_viewModel?.SelectedCustomer != null &&
                    _viewModel.EditCustomerCommand.CanExecute(null))
                {
                    _viewModel.EditCustomerCommand.Execute(null);
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling DataGrid double-click");
            }
        }

        /// <summary>
        /// Handles search text box KeyDown for enhanced search experience
        /// </summary>
        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                // Handle Enter key to focus on customer list
                if (e.Key == Key.Enter && CustomersDataGrid != null)
                {
                    CustomersDataGrid.Focus();

                    // Select first customer if available
                    if (CustomersDataGrid.Items.Count > 0)
                    {
                        CustomersDataGrid.SelectedIndex = 0;
                        CustomersDataGrid.ScrollIntoView(CustomersDataGrid.SelectedItem);
                    }

                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling search text box KeyDown event");
            }
        }

        /// <summary>
        /// Handles DataGrid selection changes for improved user experience
        /// </summary>
        private void CustomersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // Ensure selected customer details are visible when selection changes
                if (_viewModel?.SelectedCustomer != null &&
                    !_viewModel.IsCustomerDetailsVisible)
                {
                    _viewModel.ShowCustomerDetailsCommand?.Execute(null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling DataGrid selection change");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Configures event handlers for UI elements
        /// </summary>
        private void ConfigureEventHandlers()
        {
            try
            {
                // Subscribe to view lifecycle events
                Loaded += CustomerAccountsView_Loaded;
                Unloaded += CustomerAccountsView_Unloaded;

                // Subscribe to keyboard events
                KeyDown += CustomerAccountsView_KeyDown;

                _logger.LogDebug("Event handlers configured successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring event handlers");
                throw;
            }
        }

        /// <summary>
        /// Configures keyboard shortcuts for enhanced productivity
        /// </summary>
        private void ConfigureKeyboardShortcuts()
        {
            try
            {
                // Enable keyboard focus for the user control
                Focusable = true;

                // Set up input bindings for additional shortcuts if needed
                InputBindings.Clear();

                // Example: Add custom input binding for Ctrl+N (New Customer)
                var newCustomerBinding = new KeyBinding(
                    new RelayCommand(() => _viewModel?.AddNewCustomerCommand?.Execute(null)),
                    Key.N,
                    ModifierKeys.Control);

                InputBindings.Add(newCustomerBinding);

                _logger.LogDebug("Keyboard shortcuts configured successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error configuring keyboard shortcuts");
            }
        }

        /// <summary>
        /// Initializes the ViewModel asynchronously with proper error handling
        /// </summary>
        private async Task InitializeViewModelAsync()
        {
            try
            {
                if (_viewModel != null && !_isInitialized)
                {
                    await _viewModel.InitializeAsync();
                    _isInitialized = true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during ViewModel initialization");

                // Show user-friendly error message
                MessageBox.Show(
                    "حدث خطأ أثناء تحميل بيانات الزبائن. يرجى التحقق من الاتصال بقاعدة البيانات والمحاولة مرة أخرى.",
                    "خطأ في التحميل",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Simple relay command implementation for input bindings
        /// </summary>
        private class RelayCommand : ICommand
        {
            private readonly Action _execute;
            private readonly Func<bool>? _canExecute;

            public RelayCommand(Action execute, Func<bool>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

            public void Execute(object? parameter) => _execute();

            public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}