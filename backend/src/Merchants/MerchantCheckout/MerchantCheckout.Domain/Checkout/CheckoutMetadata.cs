namespace AfriWallet.Merchants.Checkout.Domain.Checkout;

public sealed class CheckoutMetadata
{
    private readonly Dictionary<string, string> _items = new(StringComparer.OrdinalIgnoreCase);

    public CheckoutMetadata(IReadOnlyDictionary<string, string>? items = null)
    {
        if (items is null)
            return;
        if (items.Count > 20)
            throw new ArgumentException("Checkout metadata cannot contain more than 20 entries.", nameof(items));

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Key))
                throw new ArgumentException("Metadata key cannot be empty.", nameof(items));
            if (item.Key.Length > 64)
                throw new ArgumentException("Metadata key is too long.", nameof(items));
            if (item.Value.Length > 256)
                throw new ArgumentException("Metadata value is too long.", nameof(items));
            _items[item.Key.Trim()] = item.Value.Trim();
        }
    }

    public IReadOnlyDictionary<string, string> Items => _items;
}
