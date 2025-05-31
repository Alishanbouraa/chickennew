using System;

namespace PoultrySlaughterPOS.Models
{
    /// <summary>
    /// Comprehensive data model for receipt total calculations
    /// Aggregates all invoice item data for accurate receipt printing
    /// </summary>
    public class ReceiptTotals
    {
        /// <summary>
        /// Total gross weight across all invoice items
        /// </summary>
        public decimal TotalGrossWeight { get; set; } = 0;

        /// <summary>
        /// Total number of cages across all invoice items
        /// </summary>
        public int TotalCagesCount { get; set; } = 0;

        /// <summary>
        /// Total weight of all cages (calculated)
        /// </summary>
        public decimal TotalCagesWeight { get; set; } = 0;

        /// <summary>
        /// Total net weight after cage deduction
        /// </summary>
        public decimal TotalNetWeight { get; set; } = 0;

        /// <summary>
        /// Total amount before any discounts
        /// </summary>
        public decimal TotalAmountBeforeDiscount { get; set; } = 0;

        /// <summary>
        /// Total discount amount applied
        /// </summary>
        public decimal TotalDiscountAmount { get; set; } = 0;

        /// <summary>
        /// Final total amount after all discounts
        /// </summary>
        public decimal FinalTotalAmount { get; set; } = 0;

        /// <summary>
        /// Amount remaining after discount application
        /// </summary>
        public decimal AmountAfterDiscount { get; set; } = 0;

        /// <summary>
        /// Weighted average unit price across all items
        /// </summary>
        public decimal WeightedAverageUnitPrice { get; set; } = 0;

        /// <summary>
        /// Average discount percentage (weighted by amount)
        /// </summary>
        public decimal AverageDiscountPercentage { get; set; } = 0;

        /// <summary>
        /// Customer's previous balance before this invoice
        /// </summary>
        public decimal PreviousBalance { get; set; } = 0;

        /// <summary>
        /// Customer's current balance after this invoice
        /// </summary>
        public decimal CurrentBalance { get; set; } = 0;

        /// <summary>
        /// Validates that all calculated totals are mathematically consistent
        /// </summary>
        public bool IsValid()
        {
            return TotalNetWeight >= 0 &&
                   FinalTotalAmount >= 0 &&
                   TotalAmountBeforeDiscount >= TotalDiscountAmount &&
                   Math.Abs((TotalAmountBeforeDiscount - TotalDiscountAmount) - AmountAfterDiscount) < 0.01m;
        }

        /// <summary>
        /// Returns a formatted string representation for debugging
        /// </summary>
        public override string ToString()
        {
            return $"ReceiptTotals: GrossWeight={TotalGrossWeight:F2}, NetWeight={TotalNetWeight:F2}, " +
                   $"FinalAmount={FinalTotalAmount:F2}, CurrentBalance={CurrentBalance:F2}";
        }
    }
}