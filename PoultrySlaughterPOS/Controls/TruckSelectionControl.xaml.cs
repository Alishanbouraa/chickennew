using PoultrySlaughterPOS.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace PoultrySlaughterPOS.Controls
{
    /// <summary>
    /// Specialized UserControl for truck selection with advanced validation and binding support.
    /// Implements dependency properties for MVVM pattern compliance and reusability across modules.
    /// </summary>
    public partial class TruckSelectionControl : UserControl
    {
        #region Dependency Properties

        /// <summary>
        /// Dependency property for available trucks collection
        /// </summary>
        public static readonly DependencyProperty AvailableTrucksProperty =
            DependencyProperty.Register(
                nameof(AvailableTrucks),
                typeof(ObservableCollection<Truck>),
                typeof(TruckSelectionControl),
                new PropertyMetadata(null, OnAvailableTrucksChanged));

        /// <summary>
        /// Dependency property for selected truck
        /// </summary>
        public static readonly DependencyProperty SelectedTruckProperty =
            DependencyProperty.Register(
                nameof(SelectedTruck),
                typeof(Truck),
                typeof(TruckSelectionControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedTruckChanged));

        /// <summary>
        /// Dependency property for validation message
        /// </summary>
        public static readonly DependencyProperty ValidationMessageProperty =
            DependencyProperty.Register(
                nameof(ValidationMessage),
                typeof(string),
                typeof(TruckSelectionControl),
                new PropertyMetadata(string.Empty, OnValidationMessageChanged));

        /// <summary>
        /// Dependency property for validation error state
        /// </summary>
        public static readonly DependencyProperty HasValidationErrorProperty =
            DependencyProperty.Register(
                nameof(HasValidationError),
                typeof(bool),
                typeof(TruckSelectionControl),
                new PropertyMetadata(false));

        /// <summary>
        /// Dependency property for control enabled state
        /// </summary>
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.Register(
                nameof(IsEnabled),
                typeof(bool),
                typeof(TruckSelectionControl),
                new PropertyMetadata(true, OnIsEnabledChanged));

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the collection of available trucks for selection
        /// </summary>
        public ObservableCollection<Truck> AvailableTrucks
        {
            get => (ObservableCollection<Truck>)GetValue(AvailableTrucksProperty);
            set => SetValue(AvailableTrucksProperty, value);
        }

        /// <summary>
        /// Gets or sets the currently selected truck
        /// </summary>
        public Truck? SelectedTruck
        {
            get => (Truck?)GetValue(SelectedTruckProperty);
            set => SetValue(SelectedTruckProperty, value);
        }

        /// <summary>
        /// Gets or sets the validation message to display
        /// </summary>
        public string ValidationMessage
        {
            get => (string)GetValue(ValidationMessageProperty);
            set => SetValue(ValidationMessageProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the control has a validation error
        /// </summary>
        public bool HasValidationError
        {
            get => (bool)GetValue(HasValidationErrorProperty);
            set => SetValue(HasValidationErrorProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the control is enabled
        /// </summary>
        public new bool IsEnabled
        {
            get => (bool)GetValue(IsEnabledProperty);
            set => SetValue(IsEnabledProperty, value);
        }

        #endregion

        #region Events

        /// <summary>
        /// Event raised when truck selection changes
        /// </summary>
        public event RoutedPropertyChangedEventHandler<Truck?>? TruckSelectionChanged;

        /// <summary>
        /// Event raised when validation state changes
        /// </summary>
        public event EventHandler<ValidationStateChangedEventArgs>? ValidationStateChanged;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the TruckSelectionControl
        /// </summary>
        public TruckSelectionControl()
        {
            InitializeComponent();

            // Initialize collections
            if (AvailableTrucks == null)
            {
                AvailableTrucks = new ObservableCollection<Truck>();
            }

            // Wire up ComboBox events
            TruckComboBox.SelectionChanged += TruckComboBox_SelectionChanged;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles ComboBox selection changed event
        /// </summary>
        private void TruckComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var oldTruck = e.RemovedItems.Count > 0 ? e.RemovedItems[0] as Truck : null;
            var newTruck = e.AddedItems.Count > 0 ? e.AddedItems[0] as Truck : null;

            // Update selected truck
            SelectedTruck = newTruck;

            // Validate selection
            ValidateSelection();

            // Raise selection changed event
            TruckSelectionChanged?.Invoke(this, new RoutedPropertyChangedEventArgs<Truck?>(oldTruck, newTruck));
        }

        #endregion

        #region Dependency Property Change Handlers

        /// <summary>
        /// Handles changes to the AvailableTrucks property
        /// </summary>
        private static void OnAvailableTrucksChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TruckSelectionControl control)
            {
                control.OnAvailableTrucksChanged(e.OldValue as ObservableCollection<Truck>,
                                               e.NewValue as ObservableCollection<Truck>);
            }
        }

        /// <summary>
        /// Handles changes to the SelectedTruck property
        /// </summary>
        private static void OnSelectedTruckChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TruckSelectionControl control)
            {
                control.OnSelectedTruckChanged(e.OldValue as Truck, e.NewValue as Truck);
            }
        }

        /// <summary>
        /// Handles changes to the ValidationMessage property
        /// </summary>
        private static void OnValidationMessageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TruckSelectionControl control)
            {
                control.OnValidationMessageChanged((string)e.OldValue, (string)e.NewValue);
            }
        }

        /// <summary>
        /// Handles changes to the IsEnabled property
        /// </summary>
        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TruckSelectionControl control)
            {
                control.TruckComboBox.IsEnabled = (bool)e.NewValue;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles available trucks collection changes
        /// </summary>
        private void OnAvailableTrucksChanged(ObservableCollection<Truck>? oldValue, ObservableCollection<Truck>? newValue)
        {
            // Clear selection if new collection doesn't contain current selection
            if (newValue != null && SelectedTruck != null && !newValue.Contains(SelectedTruck))
            {
                SelectedTruck = null;
            }

            // Validate current state
            ValidateSelection();
        }

        /// <summary>
        /// Handles selected truck changes
        /// </summary>
        private void OnSelectedTruckChanged(Truck? oldValue, Truck? newValue)
        {
            // Update ComboBox selection if needed
            if (TruckComboBox.SelectedItem != newValue)
            {
                TruckComboBox.SelectedItem = newValue;
            }

            // Validate selection
            ValidateSelection();
        }

        /// <summary>
        /// Handles validation message changes
        /// </summary>
        private void OnValidationMessageChanged(string oldValue, string newValue)
        {
            // Update validation error state
            HasValidationError = !string.IsNullOrEmpty(newValue);

            // Raise validation state changed event
            ValidationStateChanged?.Invoke(this, new ValidationStateChangedEventArgs(HasValidationError, newValue));
        }

        /// <summary>
        /// Validates the current truck selection
        /// </summary>
        private void ValidateSelection()
        {
            string validationMessage = string.Empty;

            // Check if truck is selected
            if (SelectedTruck == null)
            {
                validationMessage = "يجب اختيار الشاحنة";
            }
            // Check if truck is still available
            else if (AvailableTrucks != null && !AvailableTrucks.Contains(SelectedTruck))
            {
                validationMessage = "الشاحنة المحددة غير متاحة";
            }
            // Check if truck is active
            else if (!SelectedTruck.IsActive)
            {
                validationMessage = "الشاحنة المحددة غير نشطة";
            }

            ValidationMessage = validationMessage;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Clears the current truck selection
        /// </summary>
        public void ClearSelection()
        {
            SelectedTruck = null;
            TruckComboBox.SelectedItem = null;
        }

        /// <summary>
        /// Focuses the truck selection ComboBox
        /// </summary>
        public void Focus()
        {
            TruckComboBox.Focus();
        }

        /// <summary>
        /// Validates the current selection and returns validation result
        /// </summary>
        /// <returns>True if selection is valid, false otherwise</returns>
        public bool ValidateAndGetResult()
        {
            ValidateSelection();
            return !HasValidationError;
        }

        /// <summary>
        /// Refreshes the available trucks collection
        /// </summary>
        /// <param name="trucks">New collection of available trucks</param>
        public void RefreshAvailableTrucks(IEnumerable<Truck> trucks)
        {
            if (AvailableTrucks == null)
            {
                AvailableTrucks = new ObservableCollection<Truck>();
            }

            AvailableTrucks.Clear();
            foreach (var truck in trucks)
            {
                AvailableTrucks.Add(truck);
            }
        }

        #endregion
    }

    /// <summary>
    /// Event arguments for validation state changes
    /// </summary>
    public class ValidationStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets whether there is a validation error
        /// </summary>
        public bool HasError { get; }

        /// <summary>
        /// Gets the validation message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Initializes a new instance of ValidationStateChangedEventArgs
        /// </summary>
        public ValidationStateChangedEventArgs(bool hasError, string message)
        {
            HasError = hasError;
            Message = message;
        }
    }
}