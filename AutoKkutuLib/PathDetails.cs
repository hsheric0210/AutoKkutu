using System.Diagnostics.CodeAnalysis;

namespace AutoKkutuLib;

public readonly struct PathDetails
{
	public static PathDetails Empty { get; } = new PathDetails(WordCondition.Empty, PathFlags.None, false, 0);

	private readonly PathFlags flags;

	public WordCondition Condition { get; }
	public bool ReuseAlreadyUsed { get; }
	public int MaxDisplayed { get; }

	public PathDetails(WordCondition condition, PathFlags flags, bool reuse, int maxDisplay)
	{
		this.flags = flags;

		Condition = condition;
		ReuseAlreadyUsed = reuse;
		MaxDisplayed = maxDisplay;
	}

	public static implicit operator WordCondition(PathDetails param) => param.Condition;

	public bool HasFlag(PathFlags flag) => flags.HasFlag(flag);
	public PathDetails WithFlags(PathFlags flags) => new(Condition, this.flags | flags, ReuseAlreadyUsed, MaxDisplayed);
	public PathDetails WithoutFlags(PathFlags flags) => new(Condition, this.flags & ~flags, ReuseAlreadyUsed, MaxDisplayed);

	public override bool Equals([NotNullWhen(true)] object? obj) => obj is PathDetails other && IsSimilar(other) && Condition.Equals(other.Condition) && flags == other.flags && MaxDisplayed == other.MaxDisplayed;

	public bool IsSimilar(PathDetails other) => Condition.IsSimilar(other.Condition)
		&& ReuseAlreadyUsed == other.ReuseAlreadyUsed
		&& HasFlag(PathFlags.UseEndWord) == other.HasFlag(PathFlags.UseEndWord)
		&& HasFlag(PathFlags.UseAttackWord) == other.HasFlag(PathFlags.UseAttackWord)
		&& HasFlag(PathFlags.MissionWordExists) == other.HasFlag(PathFlags.MissionWordExists);

	public override int GetHashCode() => HashCode.Combine(flags, Condition, ReuseAlreadyUsed, MaxDisplayed);

	public static bool operator ==(PathDetails left, PathDetails right) => left.Equals(right);

	public static bool operator !=(PathDetails left, PathDetails right) => !(left == right);
}

[Flags]
public enum PathFlags
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
	/// 나 바로 이전 턴 유저의 입력 단어를 기반으로 미리 검색을 수행했을 때 설정되는 플래그.
	/// </summary>
	/// <remarks>
	/// 이 플래그가 설정되어 있고 자동 입력 수행 시, '자동 입력 완료 시 자동 전송' 기능을 비활성화해야 함.
	/// '내 턴이 와서 이전 턴에 Pre-search한 결과를 사용할 때'는 이 플래그를 설정해서는 안됨.
	/// </remarks>
	PreSearch = 1 << 5
}

public enum PathFindResultType
{
	Found,
	NotFound,
	Error
}
