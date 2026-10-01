using System.Globalization;
using DentivaPro.Domain;

namespace DentivaPro.Domain.Tests;

[TestClass]
public sealed class MoneyTests
{
    [TestMethod]
    public void FromTaka_UsesIntegerPoishaWithoutFloatingPointRounding()
    {
        var amount = Money.FromTaka(1234.56m);

        Assert.AreEqual(123456L, amount.Poisha);
        Assert.AreEqual(1234.56m, amount.Taka);
        Assert.AreEqual("BDT", Money.CurrencyCode);
    }

    [TestMethod]
    public void FromTaka_RejectsFractionalPoisha()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => Money.FromTaka(10.001m));
    }

    [TestMethod]
    public void Arithmetic_IsCheckedAndPoishaBased()
    {
        var total = Money.FromTaka(12.34m) + Money.FromTaka(0.66m);
        Assert.AreEqual(1300L, total.Poisha);
        Assert.AreEqual(0L, (total - Money.FromTaka(13m)).Poisha);
        Assert.ThrowsExactly<OverflowException>(() => Money.FromPoisha(long.MaxValue) + Money.FromPoisha(1));
    }

    [TestMethod]
    public void BangladeshFormatting_IncludesTakaSymbolAndCultureFormattedAmount()
    {
        var culture = CultureInfo.GetCultureInfo("bn-BD");
        var formatted = Money.FromTaka(1234.5m).FormatBangladesh(culture);

        Assert.AreEqual(string.Concat("৳", 1234.5m.ToString("N2", culture)), formatted);
        Assert.IsTrue(formatted.StartsWith("৳", StringComparison.Ordinal));
        Assert.IsTrue(Money.FromTaka(1m).ToString().StartsWith("BDT ", StringComparison.Ordinal));
    }
}
