namespace UniversalWallet.Api.Domain.Fx;

public sealed class FxProvider
{
	public FxProvider(string name, string description, bool isAvailable)
	{
		Name = name;
		Description = description;
		IsAvailable = isAvailable;
	}

	public string Name { get; }
	public string Description { get; }
	public bool IsAvailable { get; }
}
