using CentralContabil.Api.Modules.Shared.Domain;

namespace CentralContabil.Api.Modules.Finance.Domain;

public enum FinancialTransactionType { Income, Expense }
public enum FinancialPaymentMethod { Cash, Pix, DebitCard, CreditCard, BankTransfer, Boleto, Other }
public enum FinancialRecurrenceFrequency { Monthly, Weekly, Yearly, Custom }
public enum FinancialAccountType { Checking, Digital, Cash, Wallet, Savings }
public enum ScheduledTransactionStatus { Pending, Paid, Received }
public enum ExplanationProfile { Simple, Detailed, Both }

public sealed class UserPreference : AuditableEntity
{
    public Guid UserId { get; set; }
    public ExplanationProfile ExplanationProfile { get; set; } = ExplanationProfile.Simple;
    public bool AlertBills { get; set; } = true;
    public bool AlertInvoices { get; set; } = true;
    public bool AlertBudget { get; set; } = true;
    public bool AlertGoals { get; set; } = true;
    public bool AlertInstallments { get; set; } = true;
    public bool AlertWeeklySummary { get; set; } = true;
    public bool AlertMonthlySummary { get; set; } = true;
}

public sealed class FinancialCategory : Entity
{
    public Guid? UserId { get; set; }
    public required string Name { get; set; }
    public FinancialTransactionType Type { get; set; }
    public string? Icon { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class FinancialTransaction : AuditableEntity
{
    public Guid UserId { get; set; }
    public FinancialTransactionType Type { get; set; }
    public Guid CategoryId { get; set; }
    public FinancialCategory Category { get; set; } = null!;
    public required string Description { get; set; }
    public decimal Amount { get; set; }
    public DateOnly TransactionDate { get; set; }
    public FinancialPaymentMethod PaymentMethod { get; set; }
    public bool IsRecurring { get; set; }
    public Guid? RecurrenceId { get; set; }
    public FinancialRecurrence? Recurrence { get; set; }
    public Guid? InstallmentPlanId { get; set; }
    public FinancialInstallmentPlan? InstallmentPlan { get; set; }
    public int? InstallmentNumber { get; set; }
    public int? InstallmentCount { get; set; }
    public Guid? CreditCardId { get; set; }
    public CreditCard? CreditCard { get; set; }
    public Guid? AccountId { get; set; }
    public FinancialAccount? Account { get; set; }
    public string? Notes { get; set; }
}

public sealed class CreditCard : AuditableEntity
{
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public string? Bank { get; set; }
    public decimal Limit { get; set; }
    public int ClosingDay { get; set; }
    public int DueDay { get; set; }
    public string? Last4 { get; set; }
    public string? Color { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<FinancialTransaction> Transactions { get; set; } = [];
}

public sealed class FinancialAccount : AuditableEntity
{
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public FinancialAccountType Type { get; set; }
    public decimal Balance { get; set; }
    public string? Color { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<FinancialTransaction> Transactions { get; set; } = [];
}

public sealed class ScheduledTransaction : AuditableEntity
{
    public Guid UserId { get; set; }
    public FinancialTransactionType Type { get; set; }
    public Guid CategoryId { get; set; }
    public FinancialCategory Category { get; set; } = null!;
    public required string Description { get; set; }
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    public ScheduledTransactionStatus Status { get; set; }
    public Guid? AccountId { get; set; }
    public FinancialAccount? Account { get; set; }
    public string? Notes { get; set; }
}

public sealed class Debt : AuditableEntity
{
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public decimal OriginalAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public int? InstallmentCount { get; set; }
    public decimal? InterestRate { get; set; }
    public DateOnly? DueDate { get; set; }
    public string? Institution { get; set; }
    public string? Notes { get; set; }
    public bool IsPaid { get; set; }
}

public sealed class EmergencyReserve : AuditableEntity
{
    public Guid UserId { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public int? TargetMonths { get; set; }
    public string? Notes { get; set; }
}

public sealed class FinancialRecurrence : AuditableEntity
{
    public Guid UserId { get; set; }
    public FinancialTransactionType Type { get; set; }
    public Guid CategoryId { get; set; }
    public FinancialCategory Category { get; set; } = null!;
    public required string Description { get; set; }
    public decimal Amount { get; set; }
    public FinancialPaymentMethod PaymentMethod { get; set; }
    public FinancialRecurrenceFrequency Frequency { get; set; } = FinancialRecurrenceFrequency.Monthly;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public DateOnly NextOccurrence { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public ICollection<FinancialTransaction> Transactions { get; set; } = [];
}

public sealed class FinancialCategoryPreference : AuditableEntity
{
    public Guid UserId { get; set; }
    public required string NormalizedText { get; set; }
    public FinancialTransactionType Type { get; set; }
    public Guid CategoryId { get; set; }
    public FinancialCategory Category { get; set; } = null!;
    public int UsageCount { get; set; } = 1;
}

public sealed class MonthlyBudget : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid CategoryId { get; set; }
    public FinancialCategory Category { get; set; } = null!;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal PlannedAmount { get; set; }
}

public sealed class FinancialGoal : AuditableEntity
{
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public DateOnly? TargetDate { get; set; }
    public string? Description { get; set; }
    public bool IsCompleted { get; set; }
    public ICollection<FinancialGoalContribution> Contributions { get; set; } = [];
}

public sealed class FinancialGoalContribution : Entity
{
    public Guid GoalId { get; set; }
    public FinancialGoal Goal { get; set; } = null!;
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class FinancialInstallmentPlan : Entity
{
    public Guid UserId { get; set; }
    public required string Description { get; set; }
    public Guid CategoryId { get; set; }
    public FinancialCategory Category { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public int InstallmentCount { get; set; }
    public DateOnly FirstDueDate { get; set; }
    public FinancialPaymentMethod PaymentMethod { get; set; } = FinancialPaymentMethod.CreditCard;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<FinancialTransaction> Transactions { get; set; } = [];
}

public static class FinancialDefaultCategories
{
    private static readonly (string Name, FinancialTransactionType Type, string Icon)[] Definitions =
    [
        ("Moradia", FinancialTransactionType.Expense, "home"), ("Alimentação", FinancialTransactionType.Expense, "food"),
        ("Transporte", FinancialTransactionType.Expense, "transport"), ("Saúde", FinancialTransactionType.Expense, "health"),
        ("Educação", FinancialTransactionType.Expense, "education"), ("Lazer", FinancialTransactionType.Expense, "leisure"),
        ("Compras", FinancialTransactionType.Expense, "shopping"), ("Assinaturas", FinancialTransactionType.Expense, "subscription"),
        ("Contas", FinancialTransactionType.Expense, "bill"), ("Impostos", FinancialTransactionType.Expense, "tax"),
        ("Dívidas", FinancialTransactionType.Expense, "debt"), ("Pets", FinancialTransactionType.Expense, "pet"),
        ("Presentes", FinancialTransactionType.Expense, "gift"), ("Outros", FinancialTransactionType.Expense, "other"),
        ("Salário", FinancialTransactionType.Income, "salary"), ("Freelance", FinancialTransactionType.Income, "freelance"),
        ("Benefícios", FinancialTransactionType.Income, "benefit"), ("Investimentos", FinancialTransactionType.Income, "investment"),
        ("Vendas", FinancialTransactionType.Income, "sale"), ("Reembolso", FinancialTransactionType.Income, "refund"),
        ("Outras receitas", FinancialTransactionType.Income, "other")
    ];

    public static IReadOnlyList<FinancialCategory> All { get; } = Definitions.Select((item, index) => new FinancialCategory
    {
        Id = Guid.Parse($"40000000-0000-0000-0000-{index + 1:000000000000}"),
        UserId = null,
        Name = item.Name,
        Type = item.Type,
        Icon = item.Icon,
        IsDefault = true,
        IsActive = true,
        CreatedAt = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero)
    }).ToArray();
}
