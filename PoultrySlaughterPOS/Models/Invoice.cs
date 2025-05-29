using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PoultrySlaughterPOS.Models
{
    [Table("INVOICES")]
    public partial class Invoice : ObservableObject
    {
        [Key]
        public int InvoiceId { get; set; }

        [Required]
        [StringLength(20)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public int TruckId { get; set; }

        [Required]
        public DateTime InvoiceDate { get; set; } = DateTime.Now;

        private decimal _grossWeight;
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal GrossWeight
        {
            get => _grossWeight;
            set => SetProperty(ref _grossWeight, value);
        }

        private decimal _cagesWeight;
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal CagesWeight
        {
            get => _cagesWeight;
            set => SetProperty(ref _cagesWeight, value);
        }

        private int _cagesCount;
        [Required]
        public int CagesCount
        {
            get => _cagesCount;
            set => SetProperty(ref _cagesCount, value);
        }

        private decimal _netWeight;
        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal NetWeight
        {
            get => _netWeight;
            set => SetProperty(ref _netWeight, value);
        }

        private decimal _unitPrice;
        [Required]
        [Column(TypeName = "decimal(8,2)")]
        public decimal UnitPrice
        {
            get => _unitPrice;
            set => SetProperty(ref _unitPrice, value);
        }

        private decimal _totalAmount;
        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }

        private decimal _discountPercentage = 0;
        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercentage
        {
            get => _discountPercentage;
            set => SetProperty(ref _discountPercentage, value);
        }

        private decimal _finalAmount;
        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal FinalAmount
        {
            get => _finalAmount;
            set => SetProperty(ref _finalAmount, value);
        }

        private decimal _previousBalance = 0;
        [Column(TypeName = "decimal(12,2)")]
        public decimal PreviousBalance
        {
            get => _previousBalance;
            set => SetProperty(ref _previousBalance, value);
        }

        private decimal _currentBalance;
        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal CurrentBalance
        {
            get => _currentBalance;
            set => SetProperty(ref _currentBalance, value);
        }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime UpdatedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; } = null!;

        [ForeignKey("TruckId")]
        public virtual Truck Truck { get; set; } = null!;

        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}