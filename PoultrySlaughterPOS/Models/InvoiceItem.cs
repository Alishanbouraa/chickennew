using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace PoultrySlaughterPOS.Models
{
    /// <summary>
    /// Enterprise-grade invoice item implementation for bulk poultry sales operations.
    /// Inherits from ObservableValidator to provide comprehensive validation support
    /// with automatic property change notifications and real-time business rule enforcement.
    /// 
    /// Architecture: Uses CommunityToolkit.Mvvm ObservableValidator with proper API usage
    /// for integrated validation, field-based property generation, and optimal performance.
    /// </summary>
    public partial class InvoiceItem : ObservableValidator
    {
        #region Observable Fields with Validation (Auto-Generated Properties)

        [ObservableProperty]
        [Required(ErrorMessage = "تاريخ البند مطلوب")]
        private DateTime invoiceDate = DateTime.Today;

        [ObservableProperty]
        [Range(0.01, double.MaxValue, ErrorMessage = "الوزن الإجمالي يجب أن يكون أكبر من الصفر")]
        [Required(ErrorMessage = "الوزن الإجمالي مطلوب")]
        private decimal grossWeight = 0;

        [ObservableProperty]
        [Range(1, int.MaxValue, ErrorMessage = "عدد الأقفاص يجب أن يكون أكبر من الصفر")]
        [Required(ErrorMessage = "عدد الأقفاص مطلوب")]
        private int cagesCount = 0;

        [ObservableProperty]
        [Range(0.01, double.MaxValue, ErrorMessage = "وزن القفص يجب أن يكون أكبر من الصفر")]
        [Required(ErrorMessage = "وزن القفص مطلوب")]
        private decimal cageWeight = 0;

        [ObservableProperty]
        [Range(0.01, double.MaxValue, ErrorMessage = "سعر الوحدة يجب أن يكون أكبر من الصفر")]
        [Required(ErrorMessage = "سعر الوحدة مطلوب")]
        private decimal unitPrice = 0;

        [ObservableProperty]
        [Range(0, 100, ErrorMessage = "نسبة الخصم يجب أن تكون بين 0 و 100")]
        private decimal discountPercentage = 0;

        #endregion

        #region Calculated Properties (Manual Implementation with Validation)

        private decimal _cagesWeight = 0;
        /// <summary>
        /// Total weight of all cages (calculated: CagesCount * CageWeight)
        /// </summary>
        public decimal CagesWeight
        {
            get => _cagesWeight;
            set
            {
                if (SetProperty(ref _cagesWeight, value))
                {
                    OnPropertyChanged(nameof(WeightPerCage));
                    OnPropertyChanged(nameof(IsBusinessRuleCompliant));
                }
            }
        }

        private decimal _netWeight = 0;
        /// <summary>
        /// Net weight after subtracting cage weight (calculated: GrossWeight - CagesWeight)
        /// </summary>
        public decimal NetWeight
        {
            get => _netWeight;
            set
            {
                if (SetProperty(ref _netWeight, value))
                {
                    OnPropertyChanged(nameof(WeightPerCage));
                    OnPropertyChanged(nameof(EffectiveUnitPrice));
                    OnPropertyChanged(nameof(IsBusinessRuleCompliant));
                }
            }
        }

        private decimal _totalAmount = 0;
        /// <summary>
        /// Total amount before discount (calculated: NetWeight * UnitPrice)
        /// </summary>
        public decimal TotalAmount
        {
            get => _totalAmount;
            set
            {
                if (SetProperty(ref _totalAmount, value))
                {
                    OnPropertyChanged(nameof(PricePerCage));
                }
            }
        }

        private decimal _discountAmount = 0;
        /// <summary>
        /// Calculated discount amount (calculated: TotalAmount * DiscountPercentage / 100)
        /// </summary>
        public decimal DiscountAmount
        {
            get => _discountAmount;
            set => SetProperty(ref _discountAmount, value);
        }

        private decimal _finalAmount = 0;
        /// <summary>
        /// Final amount after discount (calculated: TotalAmount - DiscountAmount)
        /// </summary>
        public decimal FinalAmount
        {
            get => _finalAmount;
            set
            {
                if (SetProperty(ref _finalAmount, value))
                {
                    OnPropertyChanged(nameof(PricePerCage));
                    OnPropertyChanged(nameof(EffectiveUnitPrice));
                }
            }
        }

        #endregion

        #region Business Logic Properties with Advanced Validation

        /// <summary>
        /// Weight per cage (calculated: NetWeight / CagesCount if CagesCount > 0)
        /// </summary>
        public decimal WeightPerCage => CagesCount > 0 ? NetWeight / CagesCount : 0;

        /// <summary>
        /// Price per cage (calculated: FinalAmount / CagesCount if CagesCount > 0)
        /// </summary>
        public decimal PricePerCage => CagesCount > 0 ? FinalAmount / CagesCount : 0;

        /// <summary>
        /// Effective unit price after discount (calculated: FinalAmount / NetWeight if NetWeight > 0)
        /// </summary>
        public decimal EffectiveUnitPrice => NetWeight > 0 ? FinalAmount / NetWeight : 0;

        /// <summary>
        /// Comprehensive validation state indicator leveraging ObservableValidator
        /// </summary>
        public bool HasValidationErrors => HasErrors;

        /// <summary>
        /// Aggregated validation error messages with Arabic localization
        /// </summary>
        public string ValidationErrorMessage
        {
            get
            {
                if (!HasErrors)
                    return string.Empty;

                var errors = new List<string>();

                // Get framework validation errors
                foreach (var propertyName in GetErrors().Cast<string>().Distinct())
                {
                    var propertyErrors = GetErrors(propertyName).Cast<string>();
                    errors.AddRange(propertyErrors);
                }

                // Add business rule validation errors
                var businessRuleErrors = GetBusinessRuleValidationErrors();
                errors.AddRange(businessRuleErrors);

                return string.Join("; ", errors.Distinct());
            }
        }

        /// <summary>
        /// Business rule compliance indicator for enterprise workflow validation
        /// </summary>
        public bool IsBusinessRuleCompliant
        {
            get
            {
                // Check framework validation first
                if (HasValidationErrors)
                    return false;

                // Advanced business rule validation for poultry operations
                if (GrossWeight > 0 && CagesWeight >= GrossWeight)
                    return false;

                if (NetWeight <= 0 && (GrossWeight > 0 || CagesCount > 0))
                    return false;

                if (UnitPrice <= 0)
                    return false;

                return true;
            }
        }

        #endregion

        #region Constructor & Initialization with Validation

        /// <summary>
        /// Initializes a new invoice item with comprehensive validation setup
        /// </summary>
        public InvoiceItem()
        {
            InvoiceDate = DateTime.Today;

            // Subscribe to property changes for automatic calculations and validation
            PropertyChanged += OnPropertyChanged;

            // Perform initial calculations and validation
            RecalculateAllWithValidation();
        }

        /// <summary>
        /// Initializes a new invoice item with specified values and validation
        /// </summary>
        /// <param name="grossWeight">Gross weight in kg</param>
        /// <param name="cagesCount">Number of cages</param>
        /// <param name="cageWeight">Weight per cage in kg</param>
        /// <param name="unitPrice">Price per kg in USD</param>
        /// <param name="discountPercentage">Discount percentage (0-100)</param>
        public InvoiceItem(decimal grossWeight, int cagesCount, decimal cageWeight, decimal unitPrice, decimal discountPercentage = 0)
        {
            // Set initial values without triggering cascading calculations
            this.grossWeight = grossWeight;
            this.cagesCount = cagesCount;
            this.cageWeight = cageWeight;
            this.unitPrice = unitPrice;
            this.discountPercentage = discountPercentage;
            InvoiceDate = DateTime.Today;

            // Subscribe to property changes for automatic calculations and validation
            PropertyChanged += OnPropertyChanged;

            // Perform initial calculations and validation
            RecalculateAllWithValidation();
        }

        #endregion

        #region Property Change Handling with Validation

        /// <summary>
        /// Handles property changes to trigger automatic recalculations and validation
        /// </summary>
        private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Trigger recalculations based on changed property with validation
            var triggerCalculation = e.PropertyName switch
            {
                nameof(GrossWeight) or nameof(CagesCount) or nameof(CageWeight) => true,
                nameof(UnitPrice) or nameof(DiscountPercentage) => true,
                _ => false
            };

            if (triggerCalculation)
            {
                RecalculateAllWithValidation();
            }

            // Notify dependent properties of changes
            switch (e.PropertyName)
            {
                case nameof(GrossWeight) or nameof(CagesWeight):
                    OnPropertyChanged(nameof(IsBusinessRuleCompliant));
                    OnPropertyChanged(nameof(ValidationErrorMessage));
                    break;
                case nameof(HasErrors):
                    OnPropertyChanged(nameof(HasValidationErrors));
                    OnPropertyChanged(nameof(ValidationErrorMessage));
                    OnPropertyChanged(nameof(IsBusinessRuleCompliant));
                    break;
            }
        }

        #endregion

        #region Advanced Calculation Methods with Validation

        /// <summary>
        /// Performs comprehensive recalculation with integrated validation pipeline
        /// </summary>
        public void RecalculateAllWithValidation()
        {
            try
            {
                // Calculate weight-related properties with validation
                CagesWeight = CagesCount * CageWeight;
                NetWeight = Math.Max(0, GrossWeight - CagesWeight);

                // Calculate financial properties with precision handling
                TotalAmount = NetWeight * UnitPrice;
                DiscountAmount = TotalAmount * (DiscountPercentage / 100);
                FinalAmount = Math.Max(0, TotalAmount - DiscountAmount);

                // Trigger comprehensive validation using proper ObservableValidator API
                ValidateAllProperties();

                // Notify all calculated properties
                OnPropertyChanged(nameof(WeightPerCage));
                OnPropertyChanged(nameof(PricePerCage));
                OnPropertyChanged(nameof(EffectiveUnitPrice));
                OnPropertyChanged(nameof(IsBusinessRuleCompliant));
                OnPropertyChanged(nameof(ValidationErrorMessage));
            }
            catch (Exception)
            {
                // Reset to safe values if calculation fails
                ResetToSafeValues();
            }
        }

        /// <summary>
        /// Performs comprehensive validation using ObservableValidator framework
        /// </summary>
        public bool PerformFullValidation()
        {
            // Use proper ObservableValidator API for validation
            ValidateAllProperties();

            // Business rules are checked through IsBusinessRuleCompliant property
            // ObservableValidator handles the INotifyDataErrorInfo implementation automatically

            return IsBusinessRuleCompliant;
        }

        /// <summary>
        /// Gets business rule validation errors for comprehensive error reporting
        /// </summary>
        private List<string> GetBusinessRuleValidationErrors()
        {
            var errors = new List<string>();

            if (GrossWeight > 0 && CagesWeight >= GrossWeight)
            {
                errors.Add("وزن الأقفاص لا يمكن أن يكون أكبر من أو يساوي الوزن الإجمالي");
            }

            if (NetWeight <= 0 && GrossWeight > 0)
            {
                errors.Add("الوزن الصافي يجب أن يكون أكبر من الصفر");
            }

            if (CagesCount > 0 && CageWeight <= 0)
            {
                errors.Add("وزن القفص الواحد يجب أن يكون أكبر من الصفر عند وجود أقفاص");
            }

            return errors;
        }

        /// <summary>
        /// Resets calculated values to safe defaults
        /// </summary>
        private void ResetToSafeValues()
        {
            CagesWeight = 0;
            NetWeight = 0;
            TotalAmount = 0;
            DiscountAmount = 0;
            FinalAmount = 0;
        }

        #endregion

        #region Public Methods with Validation

        /// <summary>
        /// Validates the current invoice item using ObservableValidator framework
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public bool Validate()
        {
            return PerformFullValidation();
        }

        /// <summary>
        /// Resets all values to defaults with validation
        /// </summary>
        public void Reset()
        {
            // Temporarily unsubscribe to prevent cascading calculations
            PropertyChanged -= OnPropertyChanged;

            try
            {
                InvoiceDate = DateTime.Today;
                GrossWeight = 0;
                CagesCount = 0;
                CageWeight = 0;
                UnitPrice = 0;
                DiscountPercentage = 0;

                ResetToSafeValues();

                // Clear validation errors using proper ObservableValidator API
                ClearErrors();
            }
            finally
            {
                // Resubscribe and recalculate
                PropertyChanged += OnPropertyChanged;
                RecalculateAllWithValidation();
            }
        }

        /// <summary>
        /// Creates a validated deep copy of this invoice item
        /// </summary>
        /// <returns>New InvoiceItem with same values and validation state</returns>
        public InvoiceItem Clone()
        {
            var clone = new InvoiceItem(GrossWeight, CagesCount, CageWeight, UnitPrice, DiscountPercentage)
            {
                InvoiceDate = InvoiceDate
            };

            clone.PerformFullValidation();
            return clone;
        }

        /// <summary>
        /// Returns a comprehensive string representation with validation status
        /// </summary>
        public override string ToString()
        {
            var status = IsBusinessRuleCompliant ? "Valid" : "Invalid";
            return $"InvoiceItem [{status}]: {InvoiceDate:yyyy-MM-dd}, Net: {NetWeight:F2}kg, Amount: {FinalAmount:F2} USD";
        }

        /// <summary>
        /// Compares two invoice items for equality with validation state consideration
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj is not InvoiceItem other)
                return false;

            return InvoiceDate.Date == other.InvoiceDate.Date &&
                   GrossWeight == other.GrossWeight &&
                   CagesCount == other.CagesCount &&
                   CageWeight == other.CageWeight &&
                   UnitPrice == other.UnitPrice &&
                   DiscountPercentage == other.DiscountPercentage &&
                   IsBusinessRuleCompliant == other.IsBusinessRuleCompliant;
        }

        /// <summary>
        /// Returns hash code based on key properties and validation state
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(InvoiceDate.Date, GrossWeight, CagesCount, CageWeight,
                                   UnitPrice, DiscountPercentage, IsBusinessRuleCompliant);
        }

        #endregion

        #region Static Factory Methods with Validation

        /// <summary>
        /// Creates a validated sample invoice item for testing and demonstration
        /// </summary>
        public static InvoiceItem CreateSample()
        {
            var sample = new InvoiceItem(82, 3, 8, 1.80m, 0)
            {
                InvoiceDate = DateTime.Today
            };

            sample.PerformFullValidation();
            return sample;
        }

        /// <summary>
        /// Creates an empty validated invoice item for new entries
        /// </summary>
        public static InvoiceItem CreateEmpty()
        {
            return new InvoiceItem();
        }

        /// <summary>
        /// Creates validated invoice item from business data transfer object
        /// </summary>
        /// <param name="data">Data transfer object containing invoice item data</param>
        /// <returns>Configured and validated InvoiceItem instance</returns>
        public static InvoiceItem FromDataTransferObject(dynamic data)
        {
            var item = new InvoiceItem(
                grossWeight: data.GrossWeight ?? 0,
                cagesCount: data.CagesCount ?? 0,
                cageWeight: data.CageWeight ?? 0,
                unitPrice: data.UnitPrice ?? 0,
                discountPercentage: data.DiscountPercentage ?? 0
            )
            {
                InvoiceDate = data.InvoiceDate ?? DateTime.Today
            };

            item.PerformFullValidation();
            return item;
        }

        #endregion

        #region Resource Management

        /// <summary>
        /// Cleanup method for proper resource disposal with validation cleanup
        /// </summary>
        public void Dispose()
        {
            PropertyChanged -= OnPropertyChanged;

            // Clear validation errors using proper ObservableValidator API
            ClearErrors();
        }

        #endregion
    }
}