using System.Globalization;

namespace DentivaPro.Domain;

/// <summary>
/// BDT money stored as integer poisha. Fractional poisha and implicit rounding are rejected.
/// </summary>
public readonly record struct Money : IComparable<Money>
{
    public const string CurrencyCode = "BDT";
    public const int MinorUnitsPerTaka = 100;

    public long Poisha { get; }

    private Money(long poisha) => Poisha = poisha;

    public static Money Zero => new(0);

    public decimal Taka => Poisha / (decimal)MinorUnitsPerTaka;

    public static Money FromPoisha(long poisha) => new(poisha);

    public static Money FromTaka(decimal taka)
    {
        var poisha = taka * MinorUnitsPerTaka;
        if (poisha != decimal.Truncate(poisha))
        {
            throw new ArgumentOutOfRangeException(nameof(taka), "BDT amounts must be exact to one poisha.");
        }

        return new Money(checked(decimal.ToInt64(poisha)));
    }

    public string FormatBangladesh(CultureInfo? culture = null)
    {
        culture ??= CultureInfo.GetCultureInfo("bn-BD");
        return string.Concat("৳", Taka.ToString("N2", culture));
    }

    public int CompareTo(Money other) => Poisha.CompareTo(other.Poisha);

    public override string ToString() => $"{CurrencyCode} {Taka.ToString("N2", CultureInfo.InvariantCulture)}";

    public static Money operator +(Money left, Money right) => new(checked(left.Poisha + right.Poisha));

    public static Money operator -(Money left, Money right) => new(checked(left.Poisha - right.Poisha));

    public static Money operator -(Money value) => new(checked(-value.Poisha));

    public static bool operator <(Money left, Money right) => left.Poisha < right.Poisha;

    public static bool operator >(Money left, Money right) => left.Poisha > right.Poisha;

    public static bool operator <=(Money left, Money right) => left.Poisha <= right.Poisha;

    public static bool operator >=(Money left, Money right) => left.Poisha >= right.Poisha;
}
