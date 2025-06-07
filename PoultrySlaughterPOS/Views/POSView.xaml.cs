// PoultrySlaughterPOS/Views/POSView.xaml.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.ViewModels;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace PoultrySlaughterPOS.Views
{
    /// <summary>
    /// Enhanced Point of Sale view with comprehensive invoice search, edit capabilities, and modern scrollable design.
    /// Implements MVVM patterns with optimized performance, accessibility, and advanced user experience features.
    /// </summary>
    public partial class POSView : UserControl
    {
        #region Private Fields

        private readonly ILogger<POSView> _logger;
        private POSViewModel? _viewModel;
        private bool _isInitialized = false;
        private ScrollViewer? _mainScrollViewer;
        private DataGrid? _invoiceDataGrid;
        private TextBox? _invoiceSearchTextBox;

        #endregion

        #region Constructor

        /// <summary>
        /// Enhanced constructor with improved initialization for search and edit capabilities
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

                // Configure enhanced view properties
                ConfigureEnhancedViewProperties();

                // Wire up comprehensive event handlers
                WireUpEnhancedEventHandlers();

                _logger.LogInformation("Enhanced POSView initialized successfully with search and edit capabilities");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Critical error during enhanced POSView initialization");

                MessageBox.Show($"خطأ حرج في تحميل واجهة نقطة البيع المحدثة:\n{ex.Message}",
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

        /// <summary>
        /// Reference to the main scroll viewer for programmatic scrolling
        /// </summary>
        public ScrollViewer? MainScrollViewer => _mainScrollViewer;

        #endregion

        #region Enhanced Public Methods

        /// <summary>
        /// Sets focus to the customer selection control with enhanced targeting
        /// </summary>
        public void FocusCustomerSelection()
        {
            try
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        // Target the specific ComboBox with exact name matching
                        var customerComboBox = CustomerSelectionComboBox;

                        if (customerComboBox != null)
                        {
                            customerComboBox.Focus();
                            _logger.LogDebug("Focus set to customer selection ComboBox");
                        }
                        else
                        {
                            // Fallback to first focusable element
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
                _logger.LogWarning(ex, "Error in enhanced FocusCustomerSelection method");
            }
        }

        /// <summary>
        /// NEW: Sets focus to the invoice search input with enhanced targeting
        /// </summary>
        public void FocusInvoiceSearch()
        {
            try
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (_invoiceSearchTextBox != null)
                        {
                            _invoiceSearchTextBox.Focus();
                            _invoiceSearchTextBox.SelectAll();
                            _logger.LogDebug("Focus set to invoice search TextBox");
                        }
                        else
                        {
                            // Try to find by name if cached reference is null
                            var searchBox = FindName("InvoiceSearchTextBox") as TextBox;
                            if (searchBox != null)
                            {
                                searchBox.Focus();
                                searchBox.SelectAll();
                                _logger.LogDebug("Focus set to invoice search TextBox via FindName");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error setting focus to invoice search");
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in FocusInvoiceSearch method");
            }
        }

        /// <summary>
        /// Scrolls to a specific section of the page
        /// </summary>
        /// <param name="section">Target section to scroll to</param>
        public void ScrollToSection(POSSection section)
        {
            try
            {
                if (_mainScrollViewer == null) return;

                double targetOffset = section switch
                {
                    POSSection.Header => 0,
                    POSSection.InvoiceSearch => 120,
                    POSSection.EditModeIndicator => 200,
                    POSSection.CustomerSelection => 300,
                    POSSection.InvoiceItems => 500,
                    POSSection.Summary => 800,
                    POSSection.Actions => 1000,
                    _ => 0
                };

                _mainScrollViewer.ScrollToVerticalOffset(targetOffset);
                _logger.LogDebug("Scrolled to section: {Section} at offset: {Offset}", section, targetOffset);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error scrolling to section: {Section}", section);
            }
        }

        /// <summary>
        /// Focuses on the invoice items data grid
        /// </summary>
        public void FocusInvoiceItems()
        {
            try
            {
                if (_invoiceDataGrid != null)
                {
                    _invoiceDataGrid.Focus();

                    // Select first row if available
                    if (_invoiceDataGrid.Items.Count > 0)
                    {
                        _invoiceDataGrid.SelectedIndex = 0;
                        _invoiceDataGrid.ScrollIntoView(_invoiceDataGrid.SelectedItem);
                    }

                    _logger.LogDebug("Focus set to invoice items DataGrid");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error focusing invoice items DataGrid");
            }
        }

        /// <summary>
        /// Enhanced initialization with improved performance and search capabilities
        /// </summary>
        public async Task InitializeViewAsync()
        {
            try
            {
                if (_isInitialized)
                {
                    _logger.LogDebug("Enhanced POSView already initialized, skipping re-initialization");
                    return;
                }

                _logger.LogInformation("Initializing enhanced POSView with comprehensive data loading and search capabilities");

                // Cache important UI elements for performance
                CacheUIElements();

                // Initialize ViewModel data
                if (_viewModel != null)
                {
                    await _viewModel.InitializeAsync();
                }

                // Configure initial UI state
                ConfigureInitialUIState();

                _isInitialized = true;
                _logger.LogInformation("Enhanced POSView initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during enhanced POSView initialization");

                MessageBox.Show($"خطأ في تحميل بيانات واجهة نقطة البيع:\n{ex.Message}",
                               "خطأ في التحميل",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                throw;
            }
        }

        /// <summary>
        /// Enhanced refresh with UI state preservation and search state management
        /// </summary>
        public async Task RefreshViewAsync()
        {
            try
            {
                _logger.LogDebug("Refreshing enhanced POSView data and UI state");

                // Preserve scroll position and search state
                double currentScrollPosition = _mainScrollViewer?.VerticalOffset ?? 0;
                bool wasSearchVisible = _viewModel?.IsInvoiceSearchVisible ?? false;
                string currentSearchTerm = _viewModel?.InvoiceSearchTerm ?? string.Empty;

                if (_viewModel != null)
                {
                    await _viewModel.InitializeAsync();

                    // Restore search state if it was active
                    if (wasSearchVisible)
                    {
                        _viewModel.IsInvoiceSearchVisible = true;
                        if (!string.IsNullOrEmpty(currentSearchTerm))
                        {
                            _viewModel.InvoiceSearchTerm = currentSearchTerm;
                        }
                    }
                }

                // Restore scroll position
                if (_mainScrollViewer != null)
                {
                    _mainScrollViewer.ScrollToVerticalOffset(currentScrollPosition);
                }

                _logger.LogDebug("Enhanced POSView refresh completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing enhanced POSView");
                throw;
            }
        }

        /// <summary>
        /// Forces explicit TextBox visibility properties on DataGrid cells
        /// Emergency fix for text visibility issues - applies runtime property enforcement
        /// </summary>
        public void ForceTextBoxVisibility()
        {
            try
            {
                if (_invoiceDataGrid == null) return;

                _logger.LogInformation("Applying emergency TextBox visibility fix");

                // Method 1: Force properties on all existing TextBoxes
                ApplyExplicitTextBoxProperties();

                // Method 2: Subscribe to cell editing events for runtime enforcement
                SubscribeToDataGridEvents();

                // Method 3: Force immediate layout update
                _invoiceDataGrid.InvalidateVisual();
                _invoiceDataGrid.UpdateLayout();

                _logger.LogInformation("Emergency TextBox visibility fix applied successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying emergency TextBox visibility fix");
            }
        }

        /// <summary>
        /// NEW: Toggles invoice search visibility with smooth UI transitions
        /// </summary>
        public void ToggleInvoiceSearchVisibility()
        {
            try
            {
                if (_viewModel != null)
                {
                    var isCurrentlyVisible = _viewModel.IsInvoiceSearchVisible;
                    _viewModel.IsInvoiceSearchVisible = !isCurrentlyVisible;

                    // Focus search box when opening
                    if (_viewModel.IsInvoiceSearchVisible)
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            FocusInvoiceSearch();
                            ScrollToSection(POSSection.InvoiceSearch);
                        }), System.Windows.Threading.DispatcherPriority.Loaded);
                    }

                    _logger.LogDebug("Invoice search visibility toggled to: {IsVisible}", _viewModel.IsInvoiceSearchVisible);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling invoice search visibility");
            }
        }

        /// <summary>
        /// NEW: Handles edit mode state changes with UI updates
        /// </summary>
        public void HandleEditModeChange()
        {
            try
            {
                if (_viewModel != null)
                {
                    if (_viewModel.IsEditMode)
                    {
                        // Scroll to show edit mode indicator
                        ScrollToSection(POSSection.EditModeIndicator);

                        // Update window title or other UI elements to reflect edit mode
                        var window = Window.GetWindow(this);
                        if (window != null)
                        {
                            window.Title = $"نقطة البيع - تعديل الفاتورة: {_viewModel.CurrentInvoice?.InvoiceNumber}";
                        }
                    }
                    else
                    {
                        // Reset to normal mode
                        var window = Window.GetWindow(this);
                        if (window != null)
                        {
                            window.Title = "نقطة البيع - فاتورة جديدة";
                        }
                    }

                    _logger.LogDebug("Edit mode state handled: {IsEditMode}", _viewModel.IsEditMode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling edit mode change");
            }
        }

        #endregion

        #region Enhanced Event Handlers

        /// <summary>
        /// Enhanced view loaded event with improved initialization and search setup
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

                // Subscribe to ViewModel property changes for UI updates
                if (_viewModel != null)
                {
                    _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                }

                _logger.LogDebug("Enhanced POSView loaded event handled successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in enhanced POSView_Loaded event handler");
            }
        }

        /// <summary>
        /// Enhanced keyboard shortcuts handling with search capabilities
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
                        ScrollToSection(POSSection.CustomerSelection);
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

                    case Key.F4:
                        // Focus on invoice items
                        FocusInvoiceItems();
                        ScrollToSection(POSSection.InvoiceItems);
                        e.Handled = true;
                        break;

                    case Key.F5:
                        // Refresh view shortcut
                        _ = RefreshViewAsync();
                        e.Handled = true;
                        break;

                    // NEW: Search functionality shortcuts
                    case Key.F when (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control:
                        // Ctrl+F: Toggle invoice search
                        if (_viewModel.ToggleInvoiceSearchCommand.CanExecute(null))
                        {
                            _viewModel.ToggleInvoiceSearchCommand.Execute(null);
                            FocusInvoiceSearch();
                        }
                        e.Handled = true;
                        break;

                    case Key.Escape:
                        // Escape: Clear search or exit edit mode
                        if (_viewModel.IsInvoiceSearchVisible)
                        {
                            if (_viewModel.ClearInvoiceSearchCommand.CanExecute(null))
                            {
                                _viewModel.ClearInvoiceSearchCommand.Execute(null);
                            }
                        }
                        else if (_viewModel.IsEditMode)
                        {
                            if (_viewModel.NewInvoiceCommand.CanExecute(null))
                            {
                                var result = MessageBox.Show(
                                    "هل تريد إلغاء تعديل الفاتورة والعودة لإنشاء فاتورة جديدة؟",
                                    "تأكيد الإلغاء",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Question);

                                if (result == MessageBoxResult.Yes)
                                {
                                    _viewModel.NewInvoiceCommand.Execute(null);
                                }
                            }
                        }
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

                    // NEW: Quick payment shortcuts
                    case Key.P when (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt:
                        // Alt+P: Set full payment
                        if (_viewModel.SetFullPaymentCommand.CanExecute(null))
                        {
                            _viewModel.SetFullPaymentCommand.Execute(null);
                        }
                        e.Handled = true;
                        break;

                    case Key.D1 when (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt:
                        // Alt+1: Set 25% payment
                        if (_viewModel.SetPercentagePaymentCommand.CanExecute("0.25"))
                        {
                            _viewModel.SetPercentagePaymentCommand.Execute("0.25");
                        }
                        e.Handled = true;
                        break;

                    case Key.D2 when (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt:
                        // Alt+2: Set 50% payment
                        if (_viewModel.SetPercentagePaymentCommand.CanExecute("0.50"))
                        {
                            _viewModel.SetPercentagePaymentCommand.Execute("0.50");
                        }
                        e.Handled = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling enhanced keyboard shortcut: {Key}", e.Key);
            }
        }

        /// <summary>
        /// Enhanced property change handling with UI optimizations and search support
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

                    case nameof(POSViewModel.InvoiceItems):
                        HandleInvoiceItemsChanged();
                        break;

                    case nameof(POSViewModel.IsInvoiceSearchVisible):
                        HandleSearchVisibilityChanged();
                        break;

                    case nameof(POSViewModel.IsEditMode):
                        HandleEditModeChange();
                        break;

                    case nameof(POSViewModel.InvoiceSearchResults):
                        HandleSearchResultsChanged();
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling enhanced ViewModel property change: {PropertyName}", e.PropertyName);
            }
        }

        /// <summary>
        /// NEW: Handles search visibility changes
        /// </summary>
        private void HandleSearchVisibilityChanged()
        {
            try
            {
                if (_viewModel?.IsInvoiceSearchVisible == true)
                {
                    // Focus search input when becoming visible
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        FocusInvoiceSearch();
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                }

                _logger.LogDebug("Search visibility changed: {IsVisible}", _viewModel?.IsInvoiceSearchVisible);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling search visibility change");
            }
        }

        /// <summary>
        /// NEW: Handles search result selection changes
        /// </summary>
        private void HandleSearchResultsChanged()
        {
            try
            {
                var resultsCount = _viewModel?.InvoiceSearchResults?.Count ?? 0;
                _logger.LogDebug("Search results changed: {Count} results found", resultsCount);

                // Auto-scroll to search results if any found
                if (resultsCount > 0 && _viewModel?.IsInvoiceSearchVisible == true)
                {
                    ScrollToSection(POSSection.InvoiceSearch);

                    // Auto-focus first result for keyboard navigation
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (_viewModel?.InvoiceSearchResults?.Count > 0 && _viewModel.SelectedInvoiceSearchResult == null)
                        {
                            _viewModel.SelectedInvoiceSearchResult = _viewModel.InvoiceSearchResults[0];
                        }
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling search results change");
            }
        }

        #endregion

        #region Enhanced Private Methods

        /// <summary>
        /// Sets the ViewModel for this view and establishes data binding
        /// </summary>
        /// <param name="viewModel">POSViewModel instance</param>
        public void SetViewModel(POSViewModel viewModel)
        {
            try
            {
                _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
                DataContext = _viewModel;

                // Wire up ViewModel event handlers if needed
                if (_viewModel != null)
                {
                    _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                }

                _logger.LogInformation("POSView ViewModel set successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting ViewModel for POSView");
                throw;
            }
        }

        /// <summary>
        /// Asynchronously initializes the view with comprehensive data loading
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                if (!_isInitialized)
                {
                    await InitializeViewAsync();
                }

                _logger.LogInformation("POSView async initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during POSView async initialization");
                throw;
            }
        }

        /// <summary>
        /// Enhanced view properties configuration with search support
        /// </summary>
        private void ConfigureEnhancedViewProperties()
        {
            try
            {
                // Configure focus management
                Focusable = true;

                // Configure keyboard handling
                KeyDown += POSView_KeyDown;

                // Enable touch scrolling
                IsManipulationEnabled = true;

                _logger.LogDebug("Enhanced POSView properties configured successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error configuring enhanced view properties");
            }
        }

        /// <summary>
        /// Enhanced event handlers wiring with search capabilities
        /// </summary>
        private void WireUpEnhancedEventHandlers()
        {
            try
            {
                // View lifecycle events
                Loaded += POSView_Loaded;
                Unloaded += POSView_Unloaded;

                _logger.LogDebug("Enhanced event handlers wired up successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error wiring up enhanced event handlers");
            }
        }

        /// <summary>
        /// Caches important UI elements for performance optimization and search functionality
        /// </summary>
        private void CacheUIElements()
        {
            try
            {
                // Find and cache the main scroll viewer
                _mainScrollViewer = FindVisualChild<ScrollViewer>(this);

                // Cache the DataGrid
                _invoiceDataGrid = InvoiceItemsDataGrid;

                // Cache the search TextBox
                _invoiceSearchTextBox = FindName("InvoiceSearchTextBox") as TextBox;

                _logger.LogDebug("UI elements cached successfully including search components");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error caching UI elements");
            }
        }

        /// <summary>
        /// Enhanced initial UI state configuration with automatic TextBox visibility fix and search setup
        /// </summary>
        private void ConfigureInitialUIState()
        {
            try
            {
                // Set initial focus
                FocusCustomerSelection();

                // Configure DataGrid for optimal performance
                if (_invoiceDataGrid != null)
                {
                    _invoiceDataGrid.EnableRowVirtualization = true;
                    _invoiceDataGrid.EnableColumnVirtualization = true;
                }

                // Configure search TextBox if available
                if (_invoiceSearchTextBox != null)
                {
                    _invoiceSearchTextBox.TextChanged += (s, e) =>
                    {
                        // Additional search enhancement could be added here
                    };
                }

                // Apply automatic TextBox visibility fix
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ForceTextBoxVisibility();
                }), System.Windows.Threading.DispatcherPriority.Loaded);

                _logger.LogDebug("Enhanced initial UI state configured successfully with search support");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error configuring enhanced initial UI state");
            }
        }

        /// <summary>
        /// Handles invoice items collection changes with search context awareness
        /// </summary>
        private void HandleInvoiceItemsChanged()
        {
            try
            {
                if (_invoiceDataGrid != null && _viewModel?.InvoiceItems != null)
                {
                    // Auto-scroll to show new items
                    if (_viewModel.InvoiceItems.Count > 0)
                    {
                        var lastItem = _viewModel.InvoiceItems[_viewModel.InvoiceItems.Count - 1];
                        _invoiceDataGrid.ScrollIntoView(lastItem);
                    }
                }

                _logger.LogDebug("Invoice items changed handling completed");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling invoice items change");
            }
        }

        /// <summary>
        /// Enhanced loading state handling
        /// </summary>
        private void HandleLoadingStateChanged()
        {
            try
            {
                if (_viewModel?.IsLoading == true)
                {
                    Cursor = Cursors.Wait;
                    IsEnabled = false;
                }
                else
                {
                    Cursor = Cursors.Arrow;
                    IsEnabled = true;
                }

                _logger.LogDebug("Enhanced loading state changed: {IsLoading}", _viewModel?.IsLoading);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling enhanced loading state change");
            }
        }

        /// <summary>
        /// Enhanced validation state handling
        /// </summary>
        private void HandleValidationStateChanged()
        {
            try
            {
                // Additional validation UI feedback implementation
                _logger.LogDebug("Enhanced validation state changed: {HasErrors}", _viewModel?.HasValidationErrors);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling enhanced validation state change");
            }
        }

        /// <summary>
        /// Enhanced status message handling
        /// </summary>
        private void HandleStatusMessageChanged()
        {
            try
            {
                _logger.LogDebug("Enhanced status message changed: {StatusMessage}", _viewModel?.StatusMessage);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling enhanced status message change");
            }
        }

        /// <summary>
        /// Applies explicit properties to all TextBox controls in DataGrid
        /// </summary>
        private void ApplyExplicitTextBoxProperties()
        {
            try
            {
                var textBoxes = FindAllVisualChildren<TextBox>(_invoiceDataGrid);

                foreach (var textBox in textBoxes)
                {
                    // Force explicit color values that cannot be overridden
                    textBox.SetValue(TextBox.ForegroundProperty, new SolidColorBrush(Color.FromRgb(31, 41, 55))); // #1F2937
                    textBox.SetValue(TextBox.BackgroundProperty, new SolidColorBrush(Colors.White));
                    textBox.SetValue(TextBox.CaretBrushProperty, new SolidColorBrush(Color.FromRgb(59, 130, 246))); // #3B82F6
                    textBox.SetValue(TextBox.SelectionBrushProperty, new SolidColorBrush(Color.FromRgb(191, 219, 254))); // #BFDBFE
                    textBox.SetValue(TextBox.SelectionTextBrushProperty, new SolidColorBrush(Color.FromRgb(31, 41, 55))); // #1F2937

                    // Force immediate rendering update
                    textBox.InvalidateVisual();
                    textBox.UpdateLayout();
                }

                _logger.LogDebug("Applied explicit properties to {Count} TextBox controls", textBoxes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error applying explicit TextBox properties");
            }
        }

        /// <summary>
        /// Subscribes to DataGrid events for runtime TextBox property enforcement
        /// </summary>
        private void SubscribeToDataGridEvents()
        {
            try
            {
                if (_invoiceDataGrid == null) return;

                // Event 1: Cell preparation for editing
                _invoiceDataGrid.PreparingCellForEdit += (sender, e) =>
                {
                    try
                    {
                        if (e.EditingElement is TextBox textBox)
                        {
                            EnforceTextBoxProperties(textBox);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error in PreparingCellForEdit event handler");
                    }
                };

                // Event 2: Beginning edit mode
                _invoiceDataGrid.BeginningEdit += (sender, e) =>
                {
                    try
                    {
                        // Force properties on any TextBox in the editing cell
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            var cell = GetDataGridCell(_invoiceDataGrid, e.Row, e.Column);
                            if (cell != null)
                            {
                                var textBox = FindVisualChild<TextBox>(cell);
                                if (textBox != null)
                                {
                                    EnforceTextBoxProperties(textBox);
                                }
                            }
                        }), System.Windows.Threading.DispatcherPriority.Render);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error in BeginningEdit event handler");
                    }
                };

                // Event 3: Row loading
                _invoiceDataGrid.LoadingRow += (sender, e) =>
                {
                    try
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            var textBoxes = FindAllVisualChildren<TextBox>(e.Row);
                            foreach (var textBox in textBoxes)
                            {
                                EnforceTextBoxProperties(textBox);
                            }
                        }), System.Windows.Threading.DispatcherPriority.Loaded);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error in LoadingRow event handler");
                    }
                };

                _logger.LogDebug("DataGrid event handlers subscribed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error subscribing to DataGrid events");
            }
        }

        /// <summary>
        /// Enforces explicit TextBox properties that guarantee visibility
        /// </summary>
        /// <param name="textBox">Target TextBox control</param>
        private void EnforceTextBoxProperties(TextBox textBox)
        {
            try
            {
                // Clear any inherited or applied styles
                textBox.ClearValue(TextBox.StyleProperty);

                // Apply explicit properties with high priority
                textBox.SetCurrentValue(TextBox.ForegroundProperty, new SolidColorBrush(Color.FromRgb(31, 41, 55)));
                textBox.SetCurrentValue(TextBox.BackgroundProperty, new SolidColorBrush(Colors.White));
                textBox.SetCurrentValue(TextBox.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(209, 213, 219)));
                textBox.SetCurrentValue(TextBox.BorderThicknessProperty, new Thickness(1.0));
                textBox.SetCurrentValue(TextBox.PaddingProperty, new Thickness(6.0, 4.0, 6.0, 4.0));
                textBox.SetCurrentValue(TextBox.FontSizeProperty, 14.0);
                textBox.SetCurrentValue(TextBox.FontWeightProperty, FontWeights.Medium);
                textBox.SetCurrentValue(TextBox.TextAlignmentProperty, TextAlignment.Center);
                textBox.SetCurrentValue(TextBox.CaretBrushProperty, new SolidColorBrush(Color.FromRgb(59, 130, 246)));
                textBox.SetCurrentValue(TextBox.SelectionBrushProperty, new SolidColorBrush(Color.FromRgb(191, 219, 254)));
                textBox.SetCurrentValue(TextBox.SelectionTextBrushProperty, new SolidColorBrush(Color.FromRgb(31, 41, 55)));

                // Force immediate update
                textBox.InvalidateVisual();
                textBox.UpdateLayout();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error enforcing TextBox properties");
            }
        }

        /// <summary>
        /// Finds all visual children of specified type within a parent element
        /// </summary>
        /// <typeparam name="T">Type of children to find</typeparam>
        /// <param name="parent">Parent element to search</param>
        /// <returns>List of found children</returns>
        private List<T> FindAllVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            var children = new List<T>();

            try
            {
                if (parent == null) return children;

                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);

                    if (child is T typedChild)
                    {
                        children.Add(typedChild);
                    }

                    children.AddRange(FindAllVisualChildren<T>(child));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error finding visual children of type: {Type}", typeof(T).Name);
            }

            return children;
        }

        /// <summary>
        /// Gets a specific DataGrid cell for row and column intersection
        /// </summary>
        /// <param name="dataGrid">Target DataGrid</param>
        /// <param name="row">Target row</param>
        /// <param name="column">Target column</param>
        /// <returns>DataGrid cell or null if not found</returns>
        private DataGridCell? GetDataGridCell(DataGrid dataGrid, DataGridRow row, DataGridColumn column)
        {
            try
            {
                if (row != null)
                {
                    int columnIndex = dataGrid.Columns.IndexOf(column);
                    if (columnIndex >= 0)
                    {
                        var presenter = FindVisualChild<DataGridCellsPresenter>(row);
                        if (presenter != null)
                        {
                            return presenter.ItemContainerGenerator.ContainerFromIndex(columnIndex) as DataGridCell;
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting DataGrid cell");
                return null;
            }
        }

        /// <summary>
        /// Enhanced visual child finder with performance optimization
        /// </summary>
        /// <typeparam name="T">Type of child to find</typeparam>
        /// <param name="parent">Parent element</param>
        /// <returns>Found child element or null</returns>
        private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            try
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);

                    if (child is T typedChild)
                    {
                        return typedChild;
                    }

                    var foundChild = FindVisualChild<T>(child);
                    if (foundChild != null)
                    {
                        return foundChild;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error finding visual child of type: {Type}", typeof(T).Name);
                return null;
            }
        }

        /// <summary>
        /// Enhanced first focusable element finder
        /// </summary>
        /// <param name="parent">Parent element to search</param>
        /// <returns>First focusable element or null</returns>
        private FrameworkElement? FindFirstFocusableElement(DependencyObject parent)
        {
            try
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);

                    if (child is FrameworkElement element &&
                        element.Focusable &&
                        element.IsEnabled &&
                        element.Visibility == Visibility.Visible)
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
        /// Enhanced unloaded event handler
        /// </summary>
        private void POSView_Unloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Unsubscribe from ViewModel events
                if (_viewModel != null)
                {
                    _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                }

                _logger.LogDebug("Enhanced POSView unloaded event handled");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in enhanced POSView_Unloaded event handler");
            }
        }

        /// <summary>
        /// Enhanced cleanup method with search state cleanup
        /// </summary>
        public void Cleanup()
        {
            try
            {
                _logger.LogDebug("Enhanced POSView cleanup initiated");

                // Clear cached references
                _mainScrollViewer = null;
                _invoiceDataGrid = null;
                _invoiceSearchTextBox = null;

                // Cleanup ViewModel
                if (_viewModel is IDisposable disposableViewModel)
                {
                    disposableViewModel.Dispose();
                }

                _viewModel?.Cleanup();
                _isInitialized = false;

                _logger.LogDebug("Enhanced POSView cleanup completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during enhanced POSView cleanup");
            }
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Enhanced factory method for creating POSView with proper dependency injection
        /// </summary>
        /// <param name="serviceProvider">Service provider for dependency resolution</param>
        /// <returns>Configured enhanced POSView instance</returns>
        public static POSView CreateInstance(IServiceProvider serviceProvider)
        {
            try
            {
                var logger = serviceProvider.GetService<ILogger<POSView>>();
                logger?.LogInformation("Creating enhanced POSView instance via factory method");

                var view = serviceProvider.GetRequiredService<POSView>();
                return view;
            }
            catch (Exception ex)
            {
                var logger = serviceProvider.GetService<ILogger<POSView>>();
                logger?.LogError(ex, "Error in enhanced CreateInstance factory method");
                throw;
            }
        }

        #endregion
    }

    /// <summary>
    /// Enhanced enumeration for POSView sections for programmatic navigation including search
    /// </summary>
    public enum POSSection
    {
        Header,
        InvoiceSearch,
        EditModeIndicator,
        CustomerSelection,
        InvoiceItems,
        Summary,
        Actions
    }
}