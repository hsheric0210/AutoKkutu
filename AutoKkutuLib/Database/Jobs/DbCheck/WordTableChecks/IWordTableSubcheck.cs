namespace AutoKkutuLib.Database.Jobs.DbCheck.WordTableChecks;
internal interface IWordTableSubcheck
{
	string SubcheckName { get; }

	/// <summary>
	/// 단어를 검사합니다. 문제가 발견될 시 즉각적으로 수정하는 대신 목록에 저장해 두었다가 나중에 <c>Fix()</c>를 호출할 때 일괄적으로 수정합니다.
	/// </summary>
	/// <param name="entry">검사할 단어</param>
	/// <returns>
	/// <c>true</c>를 반환할 시, 해당 단어를 다음 검사에 넘기지 않고 그대로 종료합니다. (루프문의 continue와 동일)
	/// <c>false</c> 반환 시 해당 단어에 대해 다음 검사를 계속해서 실행해 나갑니다.
	/// 단어 자체가 잘못된 경우와 같이 해당 단어가 삭제되어야 할 대상이 아닌 이상 해당 함수는 항상 <c>false</c>를 반환해야 합니다.
	/// </returns>
	bool Verify(WordModel entry);

	/// <summary>
	/// <c>Verify()</c>를 실행함으로서 발견한 문제들을 일괄적으로 수정합니다.
	/// </summary>
	int Fix(DbConnectionBase db);
}
