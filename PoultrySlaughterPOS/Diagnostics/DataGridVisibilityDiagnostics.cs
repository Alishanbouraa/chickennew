using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Data;
using System.ComponentModel;
using System.Windows.Controls.Primitives;

namespace PoultrySlaughterPOS.Diagnostics
{
    /// <summary>
    /// Enterprise-grade diagnostic system for DataGrid text visibility issues
    /// Provides comprehensive analysis and resolution for TextBox rendering problems
    /// File: src/Diagnostics/DataGridVisibilityDiagnostics.cs
    /// </summary>
    public class DataGridVisibilityDiagnostics
    {
        #region Private Fields

        private readonly ILogger<DataGridVisibilityDiagnostics> _logger;

        #endregion

        #region Constructor

        public DataGridVisibilityDiagnostics(ILogger<DataGridVisibilityDiagnostics> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Public Diagnostic Methods

        /// <summary>
        /// Comprehensive analysis of DataGrid TextBox visibility issues
        /// </summary>
        /// <param name="dataGrid">Target DataGrid for analysis</param>
        /// <returns>Diagnostic report with recommended fixes</returns>
        public DataGridDiagnosticReport AnalyzeTextBoxVisibility(DataGrid dataGrid)
        {
            try
            {
                _logger.LogInformation("Starting comprehensive DataGrid TextBox visibility analysis");

                var report = new DataGridDiagnosticReport
                {
                    AnalysisTimestamp = DateTime.UtcNow,
                    DataGridName = dataGrid.Name ?? "Unknown"
                };

                // Phase 1: DataGrid-level analysis
                AnalyzeDataGridProperties(dataGrid, report);

                // Phase 2: Cell-level analysis
                AnalyzeCellProperties(dataGrid, report);

                // Phase 3: TextBox-level analysis
                AnalyzeTextBoxProperties(dataGrid, report);

                // Phase 4: System-level analysis
                AnalyzeSystemProperties(report);

                // Generate recommendations
                GenerateResolutionRecommendations(report);

                _logger.LogInformation("DataGrid visibility analysis completed. Issues found: {IssueCount}",
                                       report.IssuesFound.Count);

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during DataGrid visibility analysis");
                throw;
            }
        }

        /// <summary>
        /// Applies enterprise-grade fix for TextBox visibility issues
        /// </summary>
        /// <param name="dataGrid">Target DataGrid</param>
        /// <returns>Success status of the fix operation</returns>
        public bool ApplyTextBoxVisibilityFix(DataGrid dataGrid)
        {
            try
            {
                _logger.LogInformation("Applying comprehensive TextBox visibility fix to DataGrid: {Name}",
                                       dataGrid.Name);

                // Fix 1: Force explicit TextBox styling
                ApplyExplicitTextBoxStyling(dataGrid);

                // Fix 2: Override DataGrid cell templates
                OverrideDataGridCellTemplates(dataGrid);

                // Fix 3: Apply runtime text property enforcement
                ApplyRuntimeTextPropertyEnforcement(dataGrid);

                // Fix 4: Configure optimal rendering settings
                ConfigureOptimalRenderingSettings(dataGrid);

                _logger.LogInformation("TextBox visibility fix applied successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying TextBox visibility fix");
                return false;
            }
        }

        #endregion

        #region Private Analysis Methods

        /// <summary>
        /// Analyzes DataGrid-level properties affecting text visibility
        /// </summary>
        private void AnalyzeDataGridProperties(DataGrid dataGrid, DataGridDiagnosticReport report)
        {
            try
            {
                // Check DataGrid background/foreground
                var background = dataGrid.Background;
                var foreground = dataGrid.Foreground;

                report.DataGridBackground = background?.ToString() ?? "Null";
                report.DataGridForeground = foreground?.ToString() ?? "Null";

                // Check for conflicting styles
                if (dataGrid.Style != null)
                {
                    report.IssuesFound.Add("Custom DataGrid style detected - may override cell properties");
                }

                // Check rendering options
                report.UseLayoutRounding = dataGrid.UseLayoutRounding;
                report.SnapsToDevicePixels = dataGrid.SnapsToDevicePixels;

                _logger.LogDebug("DataGrid properties analyzed: Background={Background}, Foreground={Foreground}",
                                 report.DataGridBackground, report.DataGridForeground);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error analyzing DataGrid properties");
                report.IssuesFound.Add($"Error analyzing DataGrid properties: {ex.Message}");
            }
        }

        /// <summary>
        /// Analyzes DataGrid cell-level properties
        /// </summary>
        private void AnalyzeCellProperties(DataGrid dataGrid, DataGridDiagnosticReport report)
        {
            try
            {
                if (dataGrid.CellStyle != null)
                {
                    report.HasCustomCellStyle = true;

                    // Check for background/foreground overrides
                    foreach (Setter setter in dataGrid.CellStyle.Setters)
                    {
                        if (setter.Property == Control.BackgroundProperty)
                        {
                            report.CellStyleBackground = setter.Value?.ToString() ?? "Null";
                        }
                        else if (setter.Property == Control.ForegroundProperty)
                        {
                            report.CellStyleForeground = setter.Value?.ToString() ?? "Null";
                        }
                    }
                }

                _logger.LogDebug("Cell properties analyzed: CustomStyle={HasCustom}, Background={Background}",
                                 report.HasCustomCellStyle, report.CellStyleBackground);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error analyzing cell properties");
                report.IssuesFound.Add($"Error analyzing cell properties: {ex.Message}");
            }
        }

        /// <summary>
        /// Analyzes TextBox-specific properties within DataGrid cells
        /// </summary>
        private void AnalyzeTextBoxProperties(DataGrid dataGrid, DataGridDiagnosticReport report)
        {
            try
            {
                // Find first TextBox in DataGrid for analysis
                var textBox = FindFirstTextBoxInDataGrid(dataGrid);

                if (textBox != null)
                {
                    report.TextBoxBackground = textBox.Background?.ToString() ?? "Null";
                    report.TextBoxForeground = textBox.Foreground?.ToString() ?? "Null";
                    report.TextBoxBorderBrush = textBox.BorderBrush?.ToString() ?? "Null";
                    report.TextBoxCaretBrush = textBox.CaretBrush?.ToString() ?? "Null";

                    // Check for inherited values
                    if (textBox.Foreground == DependencyProperty.UnsetValue)
                    {
                        report.IssuesFound.Add("TextBox Foreground property is unset - may inherit problematic values");
                    }

                    if (textBox.Background == DependencyProperty.UnsetValue)
                    {
                        report.IssuesFound.Add("TextBox Background property is unset - may inherit problematic values");
                    }
                }
                else
                {
                    report.IssuesFound.Add("No TextBox controls found in DataGrid - template issue detected");
                }

                _logger.LogDebug("TextBox properties analyzed: Count found={Count}",
                                 textBox != null ? 1 : 0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error analyzing TextBox properties");
                report.IssuesFound.Add($"Error analyzing TextBox properties: {ex.Message}");
            }
        }

        /// <summary>
        /// Analyzes system-level properties affecting text rendering
        /// </summary>
        private void AnalyzeSystemProperties(DataGridDiagnosticReport report)
        {
            try
            {
                // Check system DPI settings
                var dpiXProperty = typeof(SystemParameters).GetProperty("DpiX",
                                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                var dpiYProperty = typeof(SystemParameters).GetProperty("DpiY",
                                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                report.SystemDpiX = (int)(dpiXProperty?.GetValue(null) ?? 96);
                report.SystemDpiY = (int)(dpiYProperty?.GetValue(null) ?? 96);

                // Check for high DPI issues
                if (report.SystemDpiX > 120 || report.SystemDpiY > 120)
                {
                    report.IssuesFound.Add("High DPI detected - may cause text rendering issues");
                }

                // Check system theme
                var isHighContrast = SystemParameters.HighContrast;
                report.IsHighContrastMode = isHighContrast;

                if (isHighContrast)
                {
                    report.IssuesFound.Add("High contrast mode detected - may override text colors");
                }

                _logger.LogDebug("System properties analyzed: DPI={DpiX}x{DpiY}, HighContrast={HighContrast}",
                                 report.SystemDpiX, report.SystemDpiY, report.IsHighContrastMode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error analyzing system properties");
                report.IssuesFound.Add($"Error analyzing system properties: {ex.Message}");
            }
        }

        #endregion

        #region Private Fix Methods

        /// <summary>
        /// Applies explicit TextBox styling to override inheritance issues
        /// </summary>
        private void ApplyExplicitTextBoxStyling(DataGrid dataGrid)
        {
            try
            {
                var textBoxes = FindAllTextBoxesInDataGrid(dataGrid);

                foreach (var textBox in textBoxes)
                {
                    // Force explicit color values
                    textBox.Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55)); // #1F2937
                    textBox.Background = new SolidColorBrush(Colors.White);
                    textBox.CaretBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246)); // #3B82F6
                    textBox.SelectionBrush = new SolidColorBrush(Color.FromRgb(191, 219, 254)); // #BFDBFE
                    textBox.SelectionTextBrush = new SolidColorBrush(Color.FromRgb(31, 41, 55)); // #1F2937

                    // Force layout update
                    textBox.InvalidateVisual();
                    textBox.UpdateLayout();
                }

                _logger.LogDebug("Explicit TextBox styling applied to {Count} controls", textBoxes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error applying explicit TextBox styling");
            }
        }

        /// <summary>
        /// Overrides DataGrid cell templates to ensure proper TextBox configuration
        /// </summary>
        private void OverrideDataGridCellTemplates(DataGrid dataGrid)
        {
            try
            {
                foreach (var column in dataGrid.Columns)
                {
                    if (column is DataGridTemplateColumn templateColumn)
                    {
                        // Create new cell template with explicit TextBox configuration
                        var newTemplate = CreateOptimizedCellTemplate();
                        templateColumn.CellTemplate = newTemplate;
                    }
                }

                _logger.LogDebug("DataGrid cell templates overridden for {Count} columns", dataGrid.Columns.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error overriding DataGrid cell templates");
            }
        }

        /// <summary>
        /// Applies runtime text property enforcement using attached behaviors
        /// </summary>
        private void ApplyRuntimeTextPropertyEnforcement(DataGrid dataGrid)
        {
            try
            {
                // Subscribe to cell editing events for runtime property enforcement
                dataGrid.BeginningEdit += (sender, e) =>
                {
                    if (e.EditingEventArgs is System.Windows.Input.TextCompositionEventArgs ||
                        e.EditingEventArgs is System.Windows.Input.KeyEventArgs)
                    {
                        EnforceTextBoxProperties(dataGrid, e.Row, e.Column);
                    }
                };

                dataGrid.PreparingCellForEdit += (sender, e) =>
                {
                    if (e.EditingElement is TextBox textBox)
                    {
                        EnforceExplicitTextBoxProperties(textBox);
                    }
                };

                _logger.LogDebug("Runtime text property enforcement configured");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error applying runtime text property enforcement");
            }
        }

        /// <summary>
        /// Configures optimal rendering settings for the DataGrid
        /// </summary>
        private void ConfigureOptimalRenderingSettings(DataGrid dataGrid)
        {
            try
            {
                // Enable optimal rendering options
                dataGrid.UseLayoutRounding = true;
                dataGrid.SnapsToDevicePixels = true;

                // Force render mode optimization
                RenderOptions.SetBitmapScalingMode(dataGrid, BitmapScalingMode.HighQuality);
                RenderOptions.SetClearTypeHint(dataGrid, ClearTypeHint.Enabled);

                // Force layout update
                dataGrid.InvalidateVisual();
                dataGrid.UpdateLayout();

                _logger.LogDebug("Optimal rendering settings configured");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error configuring optimal rendering settings");
            }
        }

        #endregion

        #region Private Utility Methods

        /// <summary>
        /// Finds the first TextBox control within the DataGrid
        /// </summary>
        private TextBox? FindFirstTextBoxInDataGrid(DataGrid dataGrid)
        {
            try
            {
                return FindVisualChild<TextBox>(dataGrid);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error finding first TextBox in DataGrid");
                return null;
            }
        }

        /// <summary>
        /// Finds all TextBox controls within the DataGrid
        /// </summary>
        private List<TextBox> FindAllTextBoxesInDataGrid(DataGrid dataGrid)
        {
            var textBoxes = new List<TextBox>();

            try
            {
                FindVisualChildren<TextBox>(dataGrid, textBoxes);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error finding all TextBoxes in DataGrid");
            }

            return textBoxes;
        }

        /// <summary>
        /// Creates an optimized cell template with explicit TextBox configuration
        /// </summary>
        private DataTemplate CreateOptimizedCellTemplate()
        {
            var template = new DataTemplate();

            var factory = new FrameworkElementFactory(typeof(TextBox));
            factory.SetValue(TextBox.BackgroundProperty, new SolidColorBrush(Colors.White));
            factory.SetValue(TextBox.ForegroundProperty, new SolidColorBrush(Color.FromRgb(31, 41, 55)));
            factory.SetValue(TextBox.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(209, 213, 219)));
            factory.SetValue(TextBox.BorderThicknessProperty, new Thickness(1.0));
            factory.SetValue(TextBox.PaddingProperty, new Thickness(6.0, 4.0, 6.0, 4.0));
            factory.SetValue(TextBox.TextAlignmentProperty, TextAlignment.Center);
            factory.SetValue(TextBox.FontSizeProperty, 14.0);
            factory.SetValue(TextBox.CaretBrushProperty, new SolidColorBrush(Color.FromRgb(59, 130, 246)));

            template.VisualTree = factory;
            return template;
        }

        /// <summary>
        /// Enforces TextBox properties for a specific cell
        /// </summary>
        private void EnforceTextBoxProperties(DataGrid dataGrid, DataGridRow row, DataGridColumn column)
        {
            try
            {
                var cell = GetDataGridCell(dataGrid, row, column);
                if (cell != null)
                {
                    var textBox = FindVisualChild<TextBox>(cell);
                    if (textBox != null)
                    {
                        EnforceExplicitTextBoxProperties(textBox);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error enforcing TextBox properties for cell");
            }
        }

        /// <summary>
        /// Enforces explicit properties on a TextBox control
        /// </summary>
        private void EnforceExplicitTextBoxProperties(TextBox textBox)
        {
            try
            {
                textBox.Foreground = new SolidColorBrush(Color.FromRgb(31, 41, 55));
                textBox.Background = new SolidColorBrush(Colors.White);
                textBox.CaretBrush = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                textBox.SelectionBrush = new SolidColorBrush(Color.FromRgb(191, 219, 254));
                textBox.SelectionTextBrush = new SolidColorBrush(Color.FromRgb(31, 41, 55));

                textBox.InvalidateVisual();
                textBox.UpdateLayout();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error enforcing explicit TextBox properties");
            }
        }

        /// <summary>
        /// Generates resolution recommendations based on analysis
        /// </summary>
        private void GenerateResolutionRecommendations(DataGridDiagnosticReport report)
        {
            try
            {
                if (report.IssuesFound.Count == 0)
                {
                    report.Recommendations.Add("No issues detected - TextBox visibility should be working correctly");
                    return;
                }

                if (report.IssuesFound.Any(i => i.Contains("Foreground property is unset")))
                {
                    report.Recommendations.Add("Apply explicit Foreground property to all TextBox controls");
                }

                if (report.IssuesFound.Any(i => i.Contains("High DPI detected")))
                {
                    report.Recommendations.Add("Configure DPI-aware text rendering settings");
                }

                if (report.IssuesFound.Any(i => i.Contains("High contrast mode")))
                {
                    report.Recommendations.Add("Implement high contrast theme compatibility");
                }

                if (report.HasCustomCellStyle)
                {
                    report.Recommendations.Add("Review DataGrid cell style for property conflicts");
                }

                _logger.LogDebug("Generated {Count} recommendations", report.Recommendations.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error generating resolution recommendations");
            }
        }

        /// <summary>
        /// Generic visual tree traversal helper
        /// </summary>
        private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;

                var childResult = FindVisualChild<T>(child);
                if (childResult != null)
                    return childResult;
            }
            return null;
        }

        /// <summary>
        /// Finds all visual children of specified type
        /// </summary>
        private void FindVisualChildren<T>(DependencyObject parent, List<T> children) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    children.Add(typedChild);

                FindVisualChildren(child, children);
            }
        }

        /// <summary>
        /// Gets specific DataGrid cell
        /// </summary>
        private DataGridCell? GetDataGridCell(DataGrid dataGrid, DataGridRow row, DataGridColumn column)
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

        #endregion
    }

    /// <summary>
    /// Comprehensive diagnostic report for DataGrid text visibility issues
    /// </summary>
    public class DataGridDiagnosticReport
    {
        public DateTime AnalysisTimestamp { get; set; }
        public string DataGridName { get; set; } = string.Empty;
        public string DataGridBackground { get; set; } = string.Empty;
        public string DataGridForeground { get; set; } = string.Empty;
        public bool HasCustomCellStyle { get; set; }
        public string CellStyleBackground { get; set; } = string.Empty;
        public string CellStyleForeground { get; set; } = string.Empty;
        public string TextBoxBackground { get; set; } = string.Empty;
        public string TextBoxForeground { get; set; } = string.Empty;
        public string TextBoxBorderBrush { get; set; } = string.Empty;
        public string TextBoxCaretBrush { get; set; } = string.Empty;
        public int SystemDpiX { get; set; }
        public int SystemDpiY { get; set; }
        public bool IsHighContrastMode { get; set; }
        public bool UseLayoutRounding { get; set; }
        public bool SnapsToDevicePixels { get; set; }
        public List<string> IssuesFound { get; set; } = new List<string>();
        public List<string> Recommendations { get; set; } = new List<string>();
    }
}