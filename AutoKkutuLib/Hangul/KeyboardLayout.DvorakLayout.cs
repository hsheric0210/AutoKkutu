namespace AutoKkutuLib.Hangul;

/// <summary>
/// QWERTY -> 두벌식 KS X 5002
/// </summary>
public class DvorakLayout : KeyboardLayout
{
	protected override IDictionary<char, char> HangulToAlphabetMapping => new Dictionary<char, char>()
	{
		['ㅂ'] = '\'',
		['ㅈ'] = ',',
		['ㄷ'] = '.',
		['ㄱ'] = 'p',
		['ㅅ'] = 'y',
		['ㅛ'] = 'f',
		['ㅕ'] = 'g',
		['ㅑ'] = 'c',
		['ㅐ'] = 'r',
		['ㅔ'] = 'l',
		['ㅁ'] = 'a',
		['ㄴ'] = 'e',
		['ㅇ'] = 'o',
		['ㄹ'] = 'u',
		['ㅎ'] = 'i',
		['ㅗ'] = 'd',
		['ㅓ'] = 'h',
		['ㅏ'] = 't',
		['ㅣ'] = 'n',
		['ㅋ'] = ';',
		['ㅌ'] = 'q',
		['ㅊ'] = 'j',
		['ㅍ'] = 'k',
		['ㅠ'] = 'x',
		['ㅜ'] = 'b',
		['ㅡ'] = 'm'
	};

	protected override IDictionary<char, char> HangulClusterToAlphabetMapping => new Dictionary<char, char>()
	{
		['ㅃ'] = '\'',
		['ㅉ'] = ',',
		['ㄸ'] = '.',
		['ㄲ'] = 'p',
		['ㅆ'] = 'y',
		['ㅒ'] = 'r',
		['ㅖ'] = 'l'
	};
}
