namespace AutoKkutuLib.Hangul;

/// <summary>
/// QWERTY -> 두벌식 KS X 5002
/// </summary>
public class QwertyLayout : KeyboardLayout
{
	protected override IDictionary<char, char> HangulToAlphabetMapping => new Dictionary<char, char>()
	{
		['ㅂ'] = 'q',
		['ㅈ'] = 'w',
		['ㄷ'] = 'e',
		['ㄱ'] = 'r',
		['ㅅ'] = 't',
		['ㅛ'] = 'y',
		['ㅕ'] = 'u',
		['ㅑ'] = 'i',
		['ㅐ'] = 'o',
		['ㅔ'] = 'p',
		['ㅁ'] = 'a',
		['ㄴ'] = 's',
		['ㅇ'] = 'd',
		['ㄹ'] = 'f',
		['ㅎ'] = 'g',
		['ㅗ'] = 'h',
		['ㅓ'] = 'j',
		['ㅏ'] = 'k',
		['ㅣ'] = 'l',
		['ㅋ'] = 'z',
		['ㅌ'] = 'x',
		['ㅊ'] = 'c',
		['ㅍ'] = 'v',
		['ㅠ'] = 'b',
		['ㅜ'] = 'n',
		['ㅡ'] = 'm'
	};

	protected override IDictionary<char, char> HangulClusterToAlphabetMapping => new Dictionary<char, char>()
	{
		['ㅃ'] = 'q',
		['ㅉ'] = 'w',
		['ㄸ'] = 'e',
		['ㄲ'] = 'r',
		['ㅆ'] = 't',
		['ㅒ'] = 'o',
		['ㅖ'] = 'p',
	};
}
