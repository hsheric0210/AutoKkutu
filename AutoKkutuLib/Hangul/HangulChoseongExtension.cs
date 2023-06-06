namespace AutoKkutuLib.Hangul;
internal static class HangulChoseongExtension
{
	/// <summary>
	/// 오직 초성만을 추출하는 용도에만 특화된 최적화된 함수.
	/// </summary>
	/// <param name="character">초성을 추출할 문자</param>
	/// <returns>만약 입력된 문자가 한글이라면 추출된 초성, 한글이 아니라면 원 문자를 그대로 반환합니다.</returns>
	internal static char GetChoseong(this char character)
	{
		var unicodeIndex = Convert.ToUInt16(character);
		if (unicodeIndex.IsHangulSyllable())
			return HangulConstants.InitialConsonantTable[(unicodeIndex - HangulConstants.HangulSyllablesOrigin) / 588];
		// 자모는 굳이 처리해야 할 필요 X
		return character;
	}

	/// <summary>
	/// 한글 문자열에서 초성만을 추출합니다.
	/// (예: '가나다라마바사' -> 'ㄱㄴㄷㄹㅁㅂㅅ')
	/// </summary>
	/// <param name="str">초성을 추출할 문자열</param>
	/// <returns>초성만이 남아있는 문자열</returns>
	internal static string GetChoseong(this string str) => string.Concat(str.Select(c => c.GetChoseong()));
}
