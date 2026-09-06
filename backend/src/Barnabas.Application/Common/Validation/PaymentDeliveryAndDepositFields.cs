namespace Barnabas.Application.Common.Validation;

/// <summary>
/// The field names Barnabas refuses everywhere, because the product does not do these things.
/// </summary>
/// <remarks>
/// Barnabas brokers an introduction. Payment, delivery, and deposits are settled between the two
/// members in person, and <c>L2-034</c> and <c>L2-063</c> require that no endpoint accept them.
/// <para>
/// Declaring them rather than ignoring them is the whole point. An unrecognised field is
/// discarded silently, so a client that sent a card number would get 201 and a reasonable belief
/// that Barnabas had taken it. Naming the field in a 400 is the only answer that tells the truth.
/// </para>
/// <para>
/// The list is deliberately of names a client might plausibly send rather than an exhaustive
/// vocabulary. It cannot be complete, and it does not need to be: nothing here is a security
/// boundary, because no handler reads any of these. It exists so the refusal is legible.
/// </para>
/// </remarks>
public static class PaymentDeliveryAndDepositFields
{
    public static IReadOnlySet<string> Names { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // Payment instruments.
        "cardNumber",
        "creditCard",
        "cardholderName",
        "expiry",
        "cvv",
        "cvc",
        "iban",
        "accountNumber",
        "sortCode",
        "routingNumber",
        "paymentMethod",
        "paymentToken",

        // Delivery.
        "deliveryAddress",
        "shippingAddress",
        "postcode",

        // Deposits.
        "deposit",
        "depositAmount",
    };

    /// <summary>The refused names, together with the extra ones a particular command forbids.</summary>
    public static IReadOnlySet<string> And(params string[] alsoForbidden) =>
        new HashSet<string>(Names.Concat(alsoForbidden), StringComparer.OrdinalIgnoreCase);
}
