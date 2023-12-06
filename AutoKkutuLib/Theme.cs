namespace AutoKkutuLib;
public record Theme(int Ordinal, string Id, string Name)
{
	public int BitMaskOrdinal => Ordinal / 64 + 1;
	public int BitMaskBit => Ordinal % 64;
	public long BitMaskMask => 1L << BitMaskBit;
}
