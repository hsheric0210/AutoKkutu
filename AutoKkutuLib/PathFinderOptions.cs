namespace AutoKkutuLib;

[Flags]
public enum PathFinderOptions
{
	None = 0,
	UseEndWord = 1 << 0,
	UseAttackWord = 1 << 1,
	DryRun = 1 << 2,
	ManualSearch = 1 << 3,
	MissionWordExists = 1 << 4
}
