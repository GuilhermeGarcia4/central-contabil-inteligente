using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Finance.Application;
using CentralContabil.Api.Modules.Finance.Domain;
using Microsoft.EntityFrameworkCore;

namespace CentralContabil.UnitTests;

public sealed class FinancePlanningServiceTests
{
    [Fact]
    public async Task Calculates_usage_exceeded_budget_and_isolates_users()
    {
        await using var db=Db();var userA=Guid.NewGuid();var userB=Guid.NewGuid();var category=new FinancialCategory{Name="Alimentação",Type=FinancialTransactionType.Expense};db.Add(category);
        db.MonthlyBudgets.AddRange(new MonthlyBudget{UserId=userA,Category=category,CategoryId=category.Id,Year=2026,Month=9,PlannedAmount=800},new MonthlyBudget{UserId=userB,Category=category,CategoryId=category.Id,Year=2026,Month=9,PlannedAmount=5000});
        db.FinancialTransactions.Add(new FinancialTransaction{UserId=userA,Category=category,CategoryId=category.Id,Type=FinancialTransactionType.Expense,Description="Mercado",Amount=950,TransactionDate=new(2026,9,5),PaymentMethod=FinancialPaymentMethod.Pix});await db.SaveChangesAsync();
        var result=await new FinancePlanningService(db).BudgetsAsync(userA,2026,9,default);var row=Assert.Single(result.Categories);Assert.Equal(118.75m,row.Percentage);Assert.True(row.IsExceeded);Assert.Equal(-150m,row.RemainingAmount);Assert.Equal(800m,result.TotalPlanned);
        var empty=await new FinancePlanningService(db).BudgetsAsync(userA,2026,10,default);Assert.Empty(empty.Categories);
    }
    private static AppDbContext Db()=>new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
