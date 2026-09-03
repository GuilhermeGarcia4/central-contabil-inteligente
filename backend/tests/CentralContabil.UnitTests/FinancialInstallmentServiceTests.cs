using CentralContabil.Api.Modules.Finance.Application;

namespace CentralContabil.UnitTests;

public sealed class FinancialInstallmentServiceTests
{
    [Fact]
    public void Splits_notebook_purchase_into_twelve_equal_installments()
    {
        var result = FinancialInstallmentService.Split(3600m, 12);
        Assert.Equal(12, result.Count);
        Assert.All(result, value => Assert.Equal(300m, value));
        Assert.Equal(3600m, result.Sum());
    }

    [Fact]
    public void Distributes_rounding_cents_without_changing_total()
    {
        var result = FinancialInstallmentService.Split(100m, 3);
        Assert.Equal([33.34m, 33.33m, 33.33m], result);
        Assert.Equal(100m, result.Sum());
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(100, 1)]
    public void Rejects_invalid_values(decimal total, int count) => Assert.Throws<ArgumentOutOfRangeException>(() => FinancialInstallmentService.Split(total, count));
}
