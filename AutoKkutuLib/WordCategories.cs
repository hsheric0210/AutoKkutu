namespace AutoKkutuLib;

/// <summary>
/// <para>단어의 범주(카테고리)를 나타냅니다.</para>
/// <para>단어는 해당 단어가 일반적인 단어에 속하는지, 한방 단어에 속하는지, 공격 단어에 속하는지, 미션 단어에 속하는지 등의 여부에 따라 다른 범주에 속하게 됩니다.</para>
/// </summary>
[Flags]
public enum WordCategories
{
	None = 0,
	EndWord = 1 << 0,
	AttackWord = 1 << 1,
	MissionWord = 1 << 2
}
