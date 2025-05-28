using System;
using System.Collections.Generic;

namespace PoultrySlaughterPOS.Models
{
    /// <summary>
    /// Enhanced event arguments for validation state changes with comprehensive state information
    /// </summary>
    public class ValidationStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets whether the overall validation state is valid
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets whether there are validation errors
        /// </summary>
        public bool HasErrors { get; }

        /// <summary>
        /// Gets the count of validation errors
        /// </summary>
        public int ErrorCount { get; }

        /// <summary>
        /// Gets the count of validation warnings
        /// </summary>
        public int WarningCount { get; }

        /// <summary>
        /// Gets all validation messages
        /// </summary>
        public IReadOnlyList<string> AllMessages { get; }

        /// <summary>
        /// Gets the timestamp when validation state changed
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets the validation message for display
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Initializes a new instance of ValidationStateChangedEventArgs
        /// </summary>
        /// <param name="isValid">Overall validation state</param>
        /// <param name="hasErrors">Whether errors exist</param>
        /// <param name="errorCount">Count of errors</param>
        /// <param name="warningCount">Count of warnings</param>
        /// <param name="allMessages">All validation messages</param>
        public ValidationStateChangedEventArgs(
            bool isValid,
            bool hasErrors,
            int errorCount,
            int warningCount,
            List<string> allMessages)
        {
            IsValid = isValid;
            HasErrors = hasErrors;
            ErrorCount = errorCount;
            WarningCount = warningCount;
            AllMessages = allMessages.AsReadOnly();
            Timestamp = DateTime.Now;
            Message = hasErrors ? string.Join(Environment.NewLine, allMessages) : "Valid";
        }

        /// <summary>
        /// Simplified constructor for basic validation state changes
        /// </summary>
        /// <param name="hasError">Whether there is a validation error</param>
        /// <param name="message">The validation message</param>
        public ValidationStateChangedEventArgs(bool hasError, string message)
        {
            IsValid = !hasError;
            HasErrors = hasError;
            ErrorCount = hasError ? 1 : 0;
            WarningCount = 0;
            AllMessages = hasError ? new List<string> { message }.AsReadOnly() : new List<string>().AsReadOnly();
            Timestamp = DateTime.Now;
            Message = message;
        }
    }

    /// <summary>
    /// Event arguments for weight values changed event
    /// </summary>
    public class WeightValuesChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the total weight
        /// </summary>
        public decimal TotalWeight { get; }

        /// <summary>
        /// Gets the cages count
        /// </summary>
        public int CagesCount { get; }

        /// <summary>
        /// Gets the calculated weight per cage
        /// </summary>
        public decimal WeightPerCage { get; }

        /// <summary>
        /// Gets the efficiency rating percentage
        /// </summary>
        public double EfficiencyRating { get; }

        /// <summary>
        /// Initializes a new instance of WeightValuesChangedEventArgs
        /// </summary>
        public WeightValuesChangedEventArgs(decimal totalWeight, int cagesCount, decimal weightPerCage)
        {
            TotalWeight = totalWeight;
            CagesCount = cagesCount;
            WeightPerCage = weightPerCage;

            // Calculate efficiency rating (optimal range 20-30 kg per cage)
            if (weightPerCage > 0)
            {
                const double optimalWeight = 25.0;
                var difference = Math.Abs((double)weightPerCage - optimalWeight);
                EfficiencyRating = Math.Max(0, 100 - (difference * 2));
            }
            else
            {
                EfficiencyRating = 0;
            }
        }
    }

    /// <summary>
    /// Event arguments for truck selection changes
    /// </summary>
    public class TruckSelectionChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the previously selected truck
        /// </summary>
        public object? OldTruck { get; }

        /// <summary>
        /// Gets the newly selected truck
        /// </summary>
        public object? NewTruck { get; }

        /// <summary>
        /// Gets whether the selection is valid
        /// </summary>
        public bool IsValidSelection { get; }

        /// <summary>
        /// Initializes a new instance of TruckSelectionChangedEventArgs
        /// </summary>
        public TruckSelectionChangedEventArgs(object? oldTruck, object? newTruck, bool isValidSelection = true)
        {
            OldTruck = oldTruck;
            NewTruck = newTruck;
            IsValidSelection = isValidSelection;
        }
    }

    /// <summary>
    /// Event arguments for data loading state changes
    /// </summary>
    public class DataLoadingStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets whether data is currently loading
        /// </summary>
        public bool IsLoading { get; }

        /// <summary>
        /// Gets the loading status message
        /// </summary>
        public string StatusMessage { get; }

        /// <summary>
        /// Gets the loading progress percentage (0-100)
        /// </summary>
        public int ProgressPercentage { get; }

        /// <summary>
        /// Gets any error that occurred during loading
        /// </summary>
        public Exception? LoadingError { get; }

        /// <summary>
        /// Initializes a new instance of DataLoadingStateChangedEventArgs
        /// </summary>
        public DataLoadingStateChangedEventArgs(bool isLoading, string statusMessage, int progressPercentage = 0, Exception? loadingError = null)
        {
            IsLoading = isLoading;
            StatusMessage = statusMessage;
            ProgressPercentage = Math.Max(0, Math.Min(100, progressPercentage));
            LoadingError = loadingError;
        }
    }
}