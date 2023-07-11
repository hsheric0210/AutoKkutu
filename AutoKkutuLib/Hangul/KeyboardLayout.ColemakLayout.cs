namespace AutoKkutuLib.Hangul;

/// <summary>
/// QWERTY -> 두벌식 KS X 5002
/// </summary>
public class ColemakLayout : KeyboardLayout
{
	protected override IDictionary<char, char> HangulToAlphabetMapping => new Dictionary<char, char>()
	{
		['ㅂ'] = 'q',
		['ㅈ'] = 'w',
		['ㄷ'] = 'f',
		['ㄱ'] = 'p',
		['ㅅ'] = 'g',
		['ㅛ'] = 'j',
		['ㅕ'] = 'l',
		['ㅑ'] = 'u',
		['ㅐ'] = 'y',
		['ㅔ'] = ';',
		['ㅁ'] = 'a',
		['ㄴ'] = 'r',
		['ㅇ'] = 's',
		['ㄹ'] = 't',
		['ㅎ'] = 'd',
		['ㅗ'] = 'h',
		['ㅓ'] = 'n',
		['ㅏ'] = 'e',
		['ㅣ'] = 'i',
		['ㅋ'] = 'z',
		['ㅌ'] = 'x',
		['ㅊ'] = 'c',
		['ㅍ'] = 'v',
		['ㅠ'] = 'b',
		['ㅜ'] = 'k',
		['ㅡ'] = 'm'
	};

	protected override IDictionary<char, char> HangulClusterToAlphabetMapping => new Dictionary<char, char>()
	{
		['ㅃ'] = 'q',
		['ㅉ'] = 'w',
		['ㄸ'] = 'f',
		['ㄲ'] = 'p',
		['ㅆ'] = 'g',
		['ㅒ'] = 'y',
		['ㅖ'] = ';',
	};
}
