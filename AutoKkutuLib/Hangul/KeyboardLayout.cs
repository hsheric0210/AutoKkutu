
namespace AutoKkutuLib.Hangul;
public abstract class KeyboardLayout
{
	public static readonly KeyboardLayout QWERTY = new QwertyLayout();
	public static readonly KeyboardLayout Dvorak = new DvorakLayout();
	public static readonly KeyboardLayout Colemak = new ColemakLayout();

	protected abstract IDictionary<char, char> HangulToAlphabetMapping { get; }
	protected abstract IDictionary<char, char> HangulClusterToAlphabetMapping { get; }

	/// <summary>
	/// 현재 키보드 레이아웃을 활용하여 한글을 입력할 때, 두벌식 표준 기준 어떤 키를 입력해야 하는지를 나타냅니다.
	/// </summary>
	/// <param name="hangul">입력하려는 한글 글자입니다.</param>
	/// <returns>첫 번째 원소는 현재 키보드 레이아웃에서 입력해야 하는 키, 두 번째 원소는 SHIFT키를 눌러야 하는지의 여부입니다.</returns>
	public (char, bool) HangulToAlphabet(char hangul)
	{
		if (HangulToAlphabetMapping.TryGetValue(hangul, out var ch))
			return (ch, false);

		if (HangulClusterToAlphabetMapping.TryGetValue(hangul, out ch))
			return (ch, true);

		return (hangul, false);
	}
}
