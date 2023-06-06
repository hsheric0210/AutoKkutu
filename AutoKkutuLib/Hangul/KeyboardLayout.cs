namespace AutoKkutuLib.Hangul;
public abstract class KeyboardLayout
{
	public static readonly KeyboardLayout QWERTY = new QwertyLayout();
	public static readonly KeyboardLayout Dvorak = new DvorakLayout();
	public static readonly KeyboardLayout Colemak = new ColemakLayout();

	protected abstract IDictionary<char, char> HangulToAlphabetMapping { get; }
	protected abstract IDictionary<char, char> HangulClusterToAlphabetMapping { get; }

	public (char, bool) HangulToAlphabet(char original)
	{
		if (HangulToAlphabetMapping.TryGetValue(original, out var ch))
			return (ch, false);

		if (HangulClusterToAlphabetMapping.TryGetValue(original, out ch))
			return (ch, true);

		return (original, false);
	}
}
