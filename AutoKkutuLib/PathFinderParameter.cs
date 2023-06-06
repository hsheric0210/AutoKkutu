namespace AutoKkutuLib;

public readonly struct PathFinderParameter
{
	public static PathFinderParameter Empty { get; } = new PathFinderParameter(WordCondition.Empty, PathFinderFlags.None, false, 0);

	private readonly PathFinderFlags flags;

	public WordCondition Condition { get; }
	public bool ReuseAlreadyUsed { get; }
	public int MaxDisplayed { get; }

	public PathFinderParameter(WordCondition condition, PathFinderFlags flags, bool reuse, int maxDisplay)
	{
		this.flags = flags;

		Condition = condition;
		ReuseAlreadyUsed = reuse;
		MaxDisplayed = maxDisplay;
	}

	public static implicit operator WordCondition(PathFinderParameter param) => param.Condition;

	public bool HasFlag(PathFinderFlags flag) => flags.HasFlag(flag);
	public PathFinderParameter WithFlags(PathFinderFlags flags) => new(Condition, this.flags | flags, ReuseAlreadyUsed, MaxDisplayed);
	public PathFinderParameter WithoutFlags(PathFinderFlags flags) => new(Condition, this.flags & ~flags, ReuseAlreadyUsed, MaxDisplayed);
}

[Flags]
public enum PathFinderFlags
{
	None = 0,

	/// <summary>
	/// 검색 시 한방 단어 사용
	/// </summary>
	UseEndWord = 1 << 0,

	/// <summary>
	/// 검색 시 공격 단어 사용
	/// </summary>
	UseAttackWord = 1 << 1,

	/// <summary>
	/// 이번 검색의 단어를 자동입력하지 않도록 설정합니다
	/// </summary>
	DryRun = 1 << 2,

	/// <summary>
	/// 수동 검색된 Path
	/// </summary>
	ManualSearch = 1 << 3,

	/// <summary>
	/// 검색 시 미션 글자 고려
	/// </summary>
	MissionWordExists = 1 << 4,

	/// <summary>
	/// 이전 턴 유저의 입력 단어를 기반으로 미리 검색을 수행했을 때 설정되는 플래그.
	/// 이 플래그가 설정되어 있을 때는 Path Update를 받자마자 자동 입력하는 기능을 잠시 중지해야 함
	/// </summary>
	PreSearch = 1 << 5,

	/// <summary>
	/// Path 만료 검사에서 재검색을 수행하지 않도록 합니다
	/// </summary>
	NoRescan = 1 << 6,
}

public enum PathFindResultType
{
	Found,
	NotFound,
	Error
}
