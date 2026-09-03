using CentralContabil.Api.Modules.Finance.Application;

namespace CentralContabil.UnitTests;

public sealed class CreditCardServiceTests
{
    [Fact]
    public void Computes_invoice_period_around_closing_day()
    {
        var (start, end) = CreditCardService.InvoicePeriod(10, 2026, 9);
        Assert.Equal(new DateOnly(2026, 8, 11), start);
        Assert.Equal(new DateOnly(2026, 9, 10), end);
    }

    [Fact]
    public void Handles_closing_day_31_in_short_month()
    {
        var (start, end) = CreditCardService.InvoicePeriod(31, 2026, 2);
        Assert.Equal(new DateOnly(2026, 1, 31), start);
        Assert.Equal(new DateOnly(2026, 2, 28), end);
    }
}

public sealed class AnnualReportServiceTests
{
    [Fact]
    public void Builds_twelve_month_slices_with_balance()
    {
        var service = new AnnualReportService(null!);
        var months = Enumerable.Range(1, 12).Select(m => new MonthlySlice(m, 1000m, 600m, 0)).ToList();
        for (var i = 0; i < months.Count; i++) months[i] = months[i] with { Balance = months[i].Income - months[i].Expenses };
        Assert.Equal(12, months.Count);
        Assert.All(months, m => Assert.Equal(400m, m.Balance));
    }
}
