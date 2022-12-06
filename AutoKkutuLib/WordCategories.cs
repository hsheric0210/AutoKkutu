namespace AutoKkutuLib;

[Flags]
public enum WordCategories
{
	None = 0,
	EndWord = 1 << 0,
	AttackWord = 1 << 1,
	MissionWord = 1 << 2
}
