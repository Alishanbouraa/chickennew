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
        /// Dependency property for validation message text
        /// </summary>
        public static readonly DependencyProperty ValidationMessageTextProperty =
            DependencyProperty.Register(
                nameof(ValidationMessageText),
                typeof(string),
                typeof(TruckSelectionControl),
                new PropertyMetadata(string.Empty, OnValidationMessageTextChanged));

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
        public static readonly DependencyProperty IsControlEnabledProperty =
            DependencyProperty.Register(
                nameof(IsControlEnabled),
                typeof(bool),
                typeof(TruckSelectionControl),
                new PropertyMetadata(true, OnIsControlEnabledChanged));

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
        /// Gets or sets the validation message text to display
        /// </summary>
        public string ValidationMessageText
        {
            get => (string)GetValue(ValidationMessageTextProperty);
            set => SetValue(ValidationMessageTextProperty, value);
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
        public bool IsControlEnabled
        {
            get => (bool)GetValue(IsControlEnabledProperty);
            set => SetValue(IsControlEnabledProperty, value);
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
        /// Handles changes to the ValidationMessageText property
        /// </summary>
        private static void OnValidationMessageTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TruckSelectionControl control)
            {
                control.OnValidationMessageTextChanged((string)e.OldValue, (string)e.NewValue);
            }
        }

        /// <summary>
        /// Handles changes to the IsControlEnabled property
        /// </summary>
        private static void OnIsControlEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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
        /// Handles validation message text changes
        /// </summary>
        private void OnValidationMessageTextChanged(string oldValue, string newValue)
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
            string validationMessageText = string.Empty;

            // Check if truck is selected
            if (SelectedTruck == null)
            {
                validationMessageText = "يجب اختيار الشاحنة";
            }
            // Check if truck is still available
            else if (AvailableTrucks != null && !AvailableTrucks.Contains(SelectedTruck))
            {
                validationMessageText = "الشاحنة المحددة غير متاحة";
            }
            // Check if truck is active
            else if (!SelectedTruck.IsActive)
            {
                validationMessageText = "الشاحنة المحددة غير نشطة";
            }

            ValidationMessageText = validationMessageText;
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
        public new void Focus()
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

        /// <summary>
        /// Sets the selected truck by ID
        /// </summary>
        /// <param name="truckId">ID of the truck to select</param>
        /// <returns>True if truck was found and selected, false otherwise</returns>
        public bool SelectTruckById(int truckId)
        {
            if (AvailableTrucks != null)
            {
                var truck = AvailableTrucks.FirstOrDefault(t => t.TruckId == truckId);
                if (truck != null)
                {
                    SelectedTruck = truck;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the validation summary for the current state
        /// </summary>
        /// <returns>Validation summary string</returns>
        public string GetValidationSummary()
        {
            if (HasValidationError)
            {
                return $"خطأ في اختيار الشاحنة: {ValidationMessageText}";
            }
            else if (SelectedTruck != null)
            {
                return $"تم اختيار الشاحنة: {SelectedTruck.TruckNumber} - {SelectedTruck.DriverName}";
            }
            else
            {
                return "لم يتم اختيار شاحنة";
            }
        }

        #endregion
    }
}