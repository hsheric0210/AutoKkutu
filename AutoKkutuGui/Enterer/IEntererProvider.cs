using System.Collections.Immutable;

namespace AutoKkutuGui.Enterer;

public interface IEntererProvider
{
	IImmutableList<EntererSupplier> GetEntererSuppliers();
}
