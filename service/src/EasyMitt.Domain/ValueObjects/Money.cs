namespace EasyMitt.Domain.ValueObjects;

public readonly record struct Money(decimal Amount, string CurrencyCode)
{
    public static Money Eur(decimal amount) => new(amount, "EUR");
}
