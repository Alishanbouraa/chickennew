using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PoultrySlaughterPOS.Controls
{
    /// <summary>
    /// Enterprise-grade Debt Management Control providing comprehensive debt analysis,
    /// risk assessment, collection management, and payment planning capabilities.
    /// Implements advanced financial management standards with automated risk scoring and collection workflows.
    /// </summary>
    public partial class DebtManagementControl : UserControl, INotifyPropertyChanged
    {
        #region Dependency Properties

        /// <summary>
        /// Customer for debt management analysis
        /// </summary>
        public static readonly DependencyProperty CustomerProperty =
            DependencyProperty.Register(
                nameof(Customer),
                typeof(Customer),
                typeof(DebtManagementControl),
                new PropertyMetadata(null, OnCustomerChanged));

        /// <summary>
        /// Customer name for display purposes
        /// </summary>
        public static readonly DependencyProperty CustomerNameProperty =
            DependencyProperty.Register(
                nameof(CustomerName),
                typeof(string),
                typeof(DebtManagementControl),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Loading state indicator for async operations
        /// </summary>
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(
                nameof(IsLoading),
                typeof(bool),
                typeof(DebtManagementControl),
                new PropertyMetadata(false));

        #endregion

        #region Public Properties

        /// <summary>
        /// Customer for debt management analysis
        /// </summary>
        public Customer? Customer
        {
            get => (Customer?)GetValue(CustomerProperty);
            set => SetValue(CustomerProperty, value);
        }

        /// <summary>
        /// Customer name for display purposes
        /// </summary>
        public string CustomerName
        {
            get => (string)GetValue(CustomerNameProperty);
            set => SetValue(CustomerNameProperty, value);
        }

        /// <summary>
        /// Loading state indicator for async operations
        /// </summary>
        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        /// <summary>
        /// Current debt analysis data for the customer
        /// </summary>
        public DebtAnalysisData? DebtAnalysis { get; private set; }

        /// <summary>
        /// Current payment plan recommendations
        /// </summary>
        public PaymentPlanRecommendation? PaymentPlan { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// Raised when payment plan creation is requested
        /// </summary>
        public event EventHandler<PaymentPlanEventArgs>? CreatePaymentPlanRequested;

        /// <summary>
        /// Raised when partial payment recording is requested
        /// </summary>
        public event EventHandler<DebtActionEventArgs>? RecordPartialPaymentRequested;

        /// <summary>
        /// Raised when debt adjustment is requested
        /// </summary>
        public event EventHandler<DebtActionEventArgs>? AdjustDebtRequested;

        /// <summary>
        /// Raised when debt write-off is requested
        /// </summary>
        public event EventHandler<DebtActionEventArgs>? WriteOffDebtRequested;

        /// <summary>
        /// Raised when credit limit setting is requested
        /// </summary>
        public event EventHandler<CreditLimitEventArgs>? SetCreditLimitRequested;

        /// <summary>
        /// Raised when account freeze is requested
        /// </summary>
        public event EventHandler<DebtActionEventArgs>? FreezeAccountRequested;

        /// <summary>
        /// Raised when follow-up scheduling is requested
        /// </summary>
        public event EventHandler<FollowUpEventArgs>? ScheduleFollowUpRequested;

        /// <summary>
        /// Raised when debt report export is requested
        /// </summary>
        public event EventHandler<DebtReportEventArgs>? ExportDebtReportRequested;

        /// <summary>
        /// Raised when reminder sending is requested
        /// </summary>
        public event EventHandler<ReminderEventArgs>? SendReminderRequested;

        /// <summary>
        /// PropertyChanged event for INotifyPropertyChanged implementation
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region Private Fields

        private readonly ILogger<DebtManagementControl> _logger;
        private bool _isInitialized = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes DebtManagementControl with comprehensive debt management capabilities
        /// </summary>
        public DebtManagementControl()
        {
            try
            {
                InitializeComponent();

                // Initialize logger through dependency injection if available - FIXED: Use App.Services instead of App.Current.Services
                _logger = App.Services?.GetService<ILogger<DebtManagementControl>>()
                          ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<DebtManagementControl>.Instance;

                ConfigureEventHandlers();
                InitializeDebtAnalysis();

                _logger.LogDebug("DebtManagementControl initialized successfully with enterprise debt management capabilities");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Critical error initializing DebtManagementControl: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads comprehensive debt analysis for the specified customer
        /// </summary>
        /// <param name="customer">Customer to analyze debt for</param>
        public async Task LoadDebtAnalysisAsync(Customer customer)
        {
            try
            {
                if (customer == null)
                    throw new ArgumentNullException(nameof(customer), "Customer cannot be null for debt analysis");

                IsLoading = true;
                Customer = customer;
                CustomerName = customer.CustomerName;

                // Calculate debt analysis metrics
                await CalculateDebtAnalysisAsync(customer);

                // Update UI with analysis results
                UpdateDebtAnalysisDisplay();
                UpdatePaymentPlanDisplay();
                UpdateRiskAssessmentDisplay();

                _logger.LogInformation("Debt analysis loaded for customer: {CustomerName}, Current Debt: {CurrentDebt}",
                    customer.CustomerName, customer.TotalDebt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading debt analysis for customer: {CustomerName}", customer?.CustomerName);
                throw;
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Updates the debt analysis with new data
        /// </summary>
        /// <param name="debtAnalysis">Updated debt analysis data</param>
        /// <param name="paymentPlan">Updated payment plan recommendations</param>
        public void UpdateDebtAnalysis(DebtAnalysisData debtAnalysis, PaymentPlanRecommendation paymentPlan)
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    DebtAnalysis = debtAnalysis;
                    PaymentPlan = paymentPlan;

                    UpdateDebtAnalysisDisplay();
                    UpdatePaymentPlanDisplay();
                    UpdateRiskAssessmentDisplay();

                    _logger.LogDebug("Debt analysis updated for customer: {CustomerName}", CustomerName);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating debt analysis display");
                throw;
            }
        }

        /// <summary>
        /// Clears the debt analysis display
        /// </summary>
        public void ClearDebtAnalysis()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    DebtAnalysis = null;
                    PaymentPlan = null;
                    Customer = null;
                    CustomerName = string.Empty;

                    ClearDisplayValues();
                });

                _logger.LogDebug("Debt analysis display cleared successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error clearing debt analysis display");
            }
        }

        /// <summary>
        /// Adds a collection follow-up entry to the history
        /// </summary>
        /// <param name="followUp">Follow-up entry to add</param>
        public void AddCollectionEntry(CollectionFollowUp followUp)
        {
            try
            {
                // This would typically add to a collection or update the UI
                // For now, we'll just log the addition
                _logger.LogInformation("Collection follow-up added: {Type} - {Description}",
                    followUp.Type, followUp.Description);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error adding collection entry");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles create payment plan button click
        /// </summary>
        private void CreatePaymentPlanButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Customer != null && PaymentPlan != null)
                {
                    var eventArgs = new PaymentPlanEventArgs(Customer, PaymentPlan);
                    CreatePaymentPlanRequested?.Invoke(this, eventArgs);
                    _logger.LogDebug("Payment plan creation requested for customer: {CustomerName}", Customer.CustomerName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling create payment plan button click");
            }
        }

        /// <summary>
        /// Handles record partial payment button click
        /// </summary>
        private void RecordPartialPaymentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Customer != null)
                {
                    var eventArgs = new DebtActionEventArgs(Customer, DebtActionType.RecordPartialPayment);
                    RecordPartialPaymentRequested?.Invoke(this, eventArgs);
                    _logger.LogDebug("Partial payment recording requested for customer: {CustomerName}", Customer.CustomerName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling record partial payment button click");
            }
        }

        /// <summary>
        /// Handles adjust debt button click
        /// </summary>
        private void AdjustDebtButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Customer != null)
                {
                    var eventArgs = new DebtActionEventArgs(Customer, DebtActionType.AdjustDebt);
                    AdjustDebtRequested?.Invoke(this, eventArgs);
                    _logger.LogDebug("Debt adjustment requested for customer: {CustomerName}", Customer.CustomerName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling adjust debt button click");
            }
        }

        /// <summary>
        /// Handles write off debt button click
        /// </summary>
        private void WriteOffDebtButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Customer != null)
                {
                    var result = MessageBox.Show(
                        $"هل أنت متأكد من إلغاء دين العميل '{Customer.CustomerName}'؟\n\nهذا الإجراء لا يمكن التراجع عنه.",
                        "تأكيد إلغاء الدين",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        var eventArgs = new DebtActionEventArgs(Customer, DebtActionType.WriteOff);
                        WriteOffDebtRequested?.Invoke(this, eventArgs);
                        _logger.LogDebug("Debt write-off requested for customer: {CustomerName}", Customer.CustomerName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling write off debt button click");
            }
        }

        /// <summary>
        /// Handles set credit limit button click
        /// </summary>
        private void SetCreditLimitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Customer != null)
                {
                    var eventArgs = new CreditLimitEventArgs(Customer, 0); // Amount would come from dialog
                    SetCreditLimitRequested?.Invoke(this, eventArgs);
                    _logger.LogDebug("Credit limit setting requested for customer: {CustomerName}", Customer.CustomerName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling set credit limit button click");
            }
        }

        /// <summary>
        /// Handles freeze account button click
        /// </summary>
        private void FreezeAccountButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Customer != null)
                {
                    var result = MessageBox.Show(
                        $"هل أنت متأكد من تجميد حساب العميل '{Customer.CustomerName}'؟",
                        "تأكيد تجميد الحساب",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var eventArgs = new DebtActionEventArgs(Customer, DebtActionType.FreezeAccount);
                        FreezeAccountRequested?.Invoke(this, eventArgs);
                        _logger.LogDebug("Account freeze requested for customer: {CustomerName}", Customer.CustomerName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling freeze account button click");
            }
        }

        /// <summary>
        /// Handles schedule follow-up button click
        /// </summary>
        private void ScheduleFollowUpButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Customer != null)
                {
                    var eventArgs = new FollowUpEventArgs(Customer, DateTime.Today.AddDays(7), "متابعة دورية");
                    ScheduleFollowUpRequested?.Invoke(this, eventArgs);
                    _logger.LogDebug("Follow-up scheduling requested for customer: {CustomerName}", Customer.CustomerName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling schedule follow-up button click");
            }
        }

        /// <summary>
        /// Handles send reminder button click
        /// </summary>
        private void SendReminderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Customer != null)
                {
                    var eventArgs = new ReminderEventArgs(Customer, ReminderType.Email);
                    SendReminderRequested?.Invoke(this, eventArgs);
                    _logger.LogDebug("Reminder sending requested for customer: {CustomerName}", Customer.CustomerName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling send reminder button click");
            }
        }

        /// <summary>
        /// Handles export debt report button click
        /// </summary>
        private void ExportDebtReportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Customer != null && DebtAnalysis != null)
                {
                    var eventArgs = new DebtReportEventArgs(Customer, DebtAnalysis);
                    ExportDebtReportRequested?.Invoke(this, eventArgs);
                    _logger.LogDebug("Debt report export requested for customer: {CustomerName}", Customer.CustomerName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error handling export debt report button click");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Configures comprehensive event handlers for all UI elements
        /// </summary>
        private void ConfigureEventHandlers()
        {
            try
            {
                // Payment plan action handlers
                CreatePaymentPlanButton.Click += CreatePaymentPlanButton_Click;
                RecordPartialPaymentButton.Click += RecordPartialPaymentButton_Click;
                AdjustDebtButton.Click += AdjustDebtButton_Click;

                // Debt management action handlers
                WriteOffDebtButton.Click += WriteOffDebtButton_Click;
                SetCreditLimitButton.Click += SetCreditLimitButton_Click;
                FreezeAccountButton.Click += FreezeAccountButton_Click;

                // Collection and communication handlers
                ScheduleFollowUpButton.Click += ScheduleFollowUpButton_Click;
                SendReminderButton.Click += SendReminderButton_Click;
                ExportDebtReportButton.Click += ExportDebtReportButton_Click;

                _isInitialized = true;
                _logger.LogDebug("Event handlers configured successfully for DebtManagementControl");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring event handlers for DebtManagementControl");
                throw;
            }
        }

        /// <summary>
        /// Initializes debt analysis system
        /// </summary>
        private void InitializeDebtAnalysis()
        {
            try
            {
                // Initialize any required components
                _logger.LogDebug("Debt analysis system initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error initializing debt analysis system");
            }
        }

        /// <summary>
        /// Calculates comprehensive debt analysis for the customer
        /// </summary>
        private async Task CalculateDebtAnalysisAsync(Customer customer)
        {
            try
            {
                // FIXED: Add actual async operation to resolve CS1998 warning
                await Task.Run(() =>
                {
                    // This would typically involve complex calculations using repository data
                    // For now, we'll create sample analysis based on customer data

                    var accountAge = (DateTime.Now - customer.CreatedDate).Days;
                    var currentDebt = customer.TotalDebt;

                    // Calculate risk score based on debt amount and account age
                    var riskScore = CalculateRiskScore(currentDebt, accountAge);

                    DebtAnalysis = new DebtAnalysisData
                    {
                        CurrentDebt = currentDebt,
                        DaysOverdue = Math.Max(0, accountAge - 30), // Simple calculation
                        RiskScore = riskScore,
                        RiskLevel = GetRiskLevel(riskScore),
                        AgingBreakdown = CalculateAgingBreakdown(currentDebt, accountAge)
                    };

                    PaymentPlan = new PaymentPlanRecommendation
                    {
                        SuggestedMonthlyPayment = Math.Max(50, currentDebt / 6), // 6-month plan
                        PaymentDuration = currentDebt > 0 ? Math.Ceiling(currentDebt / Math.Max(50, currentDebt / 6)) : 0,
                        NextDueDate = DateTime.Today.AddDays(30)
                    };
                });

                _logger.LogDebug("Debt analysis calculated for customer: {CustomerName}, Risk Score: {RiskScore}",
                    customer.CustomerName, DebtAnalysis?.RiskScore ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating debt analysis for customer: {CustomerName}", customer.CustomerName);
                throw;
            }
        }

        /// <summary>
        /// Calculates risk score based on debt metrics
        /// </summary>
        private int CalculateRiskScore(decimal debt, int accountAge)
        {
            var score = 0;

            // Debt amount factor (0-40 points)
            if (debt > 5000) score += 40;
            else if (debt > 2000) score += 30;
            else if (debt > 1000) score += 20;
            else if (debt > 500) score += 10;

            // Account age factor (0-30 points)
            if (accountAge > 365) score += 10;
            else if (accountAge > 180) score += 5;

            // Overdue factor (0-30 points)
            var daysOverdue = Math.Max(0, accountAge - 30);
            if (daysOverdue > 90) score += 30;
            else if (daysOverdue > 60) score += 20;
            else if (daysOverdue > 30) score += 10;

            return Math.Min(100, score);
        }

        /// <summary>
        /// Determines risk level based on score
        /// </summary>
        private string GetRiskLevel(int riskScore)
        {
            return riskScore switch
            {
                >= 80 => "عالي",
                >= 60 => "متوسط",
                >= 40 => "منخفض",
                _ => "آمن"
            };
        }

        /// <summary>
        /// Calculates aging breakdown for debt analysis
        /// </summary>
        private AgingBreakdown CalculateAgingBreakdown(decimal totalDebt, int accountAge)
        {
            // Simple distribution based on account age
            return new AgingBreakdown
            {
                Amount0to30 = accountAge <= 30 ? totalDebt : totalDebt * 0.4m,
                Amount31to60 = accountAge > 30 && accountAge <= 60 ? totalDebt : totalDebt * 0.3m,
                Amount61to90 = accountAge > 60 && accountAge <= 90 ? totalDebt : totalDebt * 0.2m,
                AmountOver90 = accountAge > 90 ? totalDebt : totalDebt * 0.1m
            };
        }

        /// <summary>
        /// Updates the debt analysis display with current data
        /// </summary>
        private void UpdateDebtAnalysisDisplay()
        {
            try
            {
                if (DebtAnalysis != null)
                {
                    CurrentDebtText.Text = $"{DebtAnalysis.CurrentDebt:N2} USD";
                    DaysOverdueText.Text = $"{DebtAnalysis.DaysOverdue} يوم";
                    RiskScoreText.Text = DebtAnalysis.RiskLevel;
                    RiskProgressBar.Value = DebtAnalysis.RiskScore;

                    // Update aging breakdown
                    Aging0to30Text.Text = $"{DebtAnalysis.AgingBreakdown.Amount0to30:N2} USD";
                    Aging31to60Text.Text = $"{DebtAnalysis.AgingBreakdown.Amount31to60:N2} USD";
                    Aging61to90Text.Text = $"{DebtAnalysis.AgingBreakdown.Amount61to90:N2} USD";
                    AgingOver90Text.Text = $"{DebtAnalysis.AgingBreakdown.AmountOver90:N2} USD";

                    // Update alert based on risk level
                    UpdateDebtAlert(DebtAnalysis.RiskLevel, DebtAnalysis.CurrentDebt);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating debt analysis display");
            }
        }

        /// <summary>
        /// Updates the payment plan display
        /// </summary>
        private void UpdatePaymentPlanDisplay()
        {
            try
            {
                if (PaymentPlan != null)
                {
                    SuggestedMonthlyPaymentText.Text = $"{PaymentPlan.SuggestedMonthlyPayment:N2} USD";
                    PaymentDurationText.Text = $"{PaymentPlan.PaymentDuration} شهر";
                    NextDueDateText.Text = PaymentPlan.NextDueDate?.ToString("yyyy/MM/dd") ?? "غير محدد";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating payment plan display");
            }
        }

        /// <summary>
        /// Updates the risk assessment display
        /// </summary>
        private void UpdateRiskAssessmentDisplay()
        {
            try
            {
                if (DebtAnalysis != null)
                {
                    // Update risk progress bar color based on level
                    var riskColor = DebtAnalysis.RiskScore switch
                    {
                        >= 80 => "#DC2626", // Red
                        >= 60 => "#F59E0B", // Orange
                        >= 40 => "#3B82F6", // Blue
                        _ => "#10B981"      // Green
                    };

                    // This would require additional converter implementation
                    _logger.LogDebug("Risk assessment updated: {RiskLevel} ({RiskScore})",
                        DebtAnalysis.RiskLevel, DebtAnalysis.RiskScore);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating risk assessment display");
            }
        }

        /// <summary>
        /// Updates the debt alert based on risk assessment
        /// </summary>
        private void UpdateDebtAlert(string riskLevel, decimal debtAmount)
        {
            try
            {
                switch (riskLevel)
                {
                    case "عالي":
                        DebtStatusAlert.Style = (Style)FindResource("CriticalAlertStyle");
                        AlertTitle.Text = "مديونية عالية المخاطر";
                        UrgencyLevelText.Text = "عاجل جداً";
                        break;
                    case "متوسط":
                        DebtStatusAlert.Style = (Style)FindResource("WarningAlertStyle");
                        AlertTitle.Text = "مديونية تحتاج متابعة";
                        UrgencyLevelText.Text = "عاجل";
                        break;
                    default:
                        DebtStatusAlert.Style = (Style)FindResource("InfoAlertStyle");
                        AlertTitle.Text = "مديونية تحت السيطرة";
                        UrgencyLevelText.Text = "عادي";
                        break;
                }

                AlertDescription.Text = $"المبلغ المستحق: {debtAmount:N2} USD - يتطلب وضع خطة للمتابعة والتحصيل";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating debt alert");
            }
        }

        /// <summary>
        /// Clears all display values
        /// </summary>
        private void ClearDisplayValues()
        {
            try
            {
                CurrentDebtText.Text = "0.00 USD";
                DaysOverdueText.Text = "0 يوم";
                RiskScoreText.Text = "آمن";
                RiskProgressBar.Value = 0;

                Aging0to30Text.Text = "0.00 USD";
                Aging31to60Text.Text = "0.00 USD";
                Aging61to90Text.Text = "0.00 USD";
                AgingOver90Text.Text = "0.00 USD";

                SuggestedMonthlyPaymentText.Text = "0.00 USD";
                PaymentDurationText.Text = "0 شهر";
                NextDueDateText.Text = "غير محدد";
                LastPaymentText.Text = "لم يتم";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error clearing display values");
            }
        }

        /// <summary>
        /// Raises PropertyChanged event for data binding updates
        /// </summary>
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Static Event Handlers

        /// <summary>
        /// Handles Customer property changes
        /// </summary>
        private static void OnCustomerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DebtManagementControl control && e.NewValue is Customer customer)
            {
                control.CustomerName = customer.CustomerName;
                control._logger.LogDebug("Customer changed to: {CustomerName} for debt management", customer.CustomerName);
            }
        }

        #endregion
    }

    #region Supporting Data Classes and Enums

    /// <summary>
    /// Comprehensive debt analysis data for business intelligence
    /// </summary>
    public class DebtAnalysisData
    {
        public decimal CurrentDebt { get; set; }
        public int DaysOverdue { get; set; }
        public int RiskScore { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
        public AgingBreakdown AgingBreakdown { get; set; } = new();
        public DateTime LastPaymentDate { get; set; }
        public decimal LastPaymentAmount { get; set; }
    }

    /// <summary>
    /// Aging breakdown for debt analysis
    /// </summary>
    public class AgingBreakdown
    {
        public decimal Amount0to30 { get; set; }
        public decimal Amount31to60 { get; set; }
        public decimal Amount61to90 { get; set; }
        public decimal AmountOver90 { get; set; }
    }

    /// <summary>
    /// Payment plan recommendation data
    /// </summary>
    public class PaymentPlanRecommendation
    {
        public decimal SuggestedMonthlyPayment { get; set; }
        public decimal PaymentDuration { get; set; }
        public DateTime? NextDueDate { get; set; }
        public string PaymentFrequency { get; set; } = "شهري";
        public decimal TotalInterest { get; set; }
    }

    /// <summary>
    /// Collection follow-up entry
    /// </summary>
    public class CollectionFollowUp
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Debt action types for operations
    /// </summary>
    public enum DebtActionType
    {
        RecordPartialPayment,
        AdjustDebt,
        WriteOff,
        FreezeAccount,
        SetCreditLimit
    }

    /// <summary>
    /// Reminder types for customer communication
    /// </summary>
    public enum ReminderType
    {
        Email,
        SMS,
        Phone,
        Letter
    }

    #endregion

    #region Event Argument Classes

    /// <summary>
    /// Event arguments for payment plan requests
    /// </summary>
    public class PaymentPlanEventArgs : EventArgs
    {
        public Customer Customer { get; }
        public PaymentPlanRecommendation PaymentPlan { get; }

        public PaymentPlanEventArgs(Customer customer, PaymentPlanRecommendation paymentPlan)
        {
            Customer = customer ?? throw new ArgumentNullException(nameof(customer));
            PaymentPlan = paymentPlan ?? throw new ArgumentNullException(nameof(paymentPlan));
        }
    }

    /// <summary>
    /// Event arguments for debt action requests
    /// </summary>
    public class DebtActionEventArgs : EventArgs
    {
        public Customer Customer { get; }
        public DebtActionType ActionType { get; }

        public DebtActionEventArgs(Customer customer, DebtActionType actionType)
        {
            Customer = customer ?? throw new ArgumentNullException(nameof(customer));
            ActionType = actionType;
        }
    }

    /// <summary>
    /// Event arguments for credit limit requests
    /// </summary>
    public class CreditLimitEventArgs : EventArgs
    {
        public Customer Customer { get; }
        public decimal CreditLimit { get; }

        public CreditLimitEventArgs(Customer customer, decimal creditLimit)
        {
            Customer = customer ?? throw new ArgumentNullException(nameof(customer));
            CreditLimit = creditLimit;
        }
    }

    /// <summary>
    /// Event arguments for follow-up scheduling
    /// </summary>
    public class FollowUpEventArgs : EventArgs
    {
        public Customer Customer { get; }
        public DateTime ScheduledDate { get; }
        public string Description { get; }

        public FollowUpEventArgs(Customer customer, DateTime scheduledDate, string description)
        {
            Customer = customer ?? throw new ArgumentNullException(nameof(customer));
            ScheduledDate = scheduledDate;
            Description = description ?? string.Empty;
        }
    }

    /// <summary>
    /// Event arguments for debt report export
    /// </summary>
    public class DebtReportEventArgs : EventArgs
    {
        public Customer Customer { get; }
        public DebtAnalysisData DebtAnalysis { get; }

        public DebtReportEventArgs(Customer customer, DebtAnalysisData debtAnalysis)
        {
            Customer = customer ?? throw new ArgumentNullException(nameof(customer));
            DebtAnalysis = debtAnalysis ?? throw new ArgumentNullException(nameof(debtAnalysis));
        }
    }

    /// <summary>
    /// Event arguments for reminder requests
    /// </summary>
    public class ReminderEventArgs : EventArgs
    {
        public Customer Customer { get; }
        public ReminderType ReminderType { get; }

        public ReminderEventArgs(Customer customer, ReminderType reminderType)
        {
            Customer = customer ?? throw new ArgumentNullException(nameof(customer));
            ReminderType = reminderType;
        }
    }

    #endregion
}