namespace AutoKkutuLib;
public struct NodeCount : IEquatable<NodeCount>
{
	public int TotalCount { get; private set; }
	public int TotalError { get; private set; }

	public int TotalEndCount { get; private set; }
	public int TotalAttackCount { get; private set; }

	public int EndCount { get; private set; }
	public int AttackCount { get; private set; }
	public int ReverseEndCount { get; private set; }
	public int ReverseAttackCount { get; private set; }
	public int KkutuEndCount { get; private set; }
	public int KkutuAttackCount { get; private set; }
	public int KKTEndCount { get; private set; }
	public int KKTAttackCount { get; private set; }

	public void IncrementError() => TotalError++;

	public void Increment(NodeTypes nodeType, int count)
	{
		TotalCount += count;
		int end = 0, attack = 0;
		switch (nodeType)
		{
			case NodeTypes.EndWord:
				EndCount += end = count;
				break;
			case NodeTypes.AttackWord:
				AttackCount += attack = count;
				break;
			case NodeTypes.ReverseEndWord:
				ReverseEndCount += end = count;
				break;
			case NodeTypes.ReverseAttackWord:
				ReverseAttackCount += attack = count;
				break;
			case NodeTypes.KkutuEndWord:
				KkutuEndCount += end = count;
				break;
			case NodeTypes.KkutuAttackWord:
				KkutuAttackCount += attack = count;
				break;
			case NodeTypes.KKTEndWord:
				KKTEndCount += end = count;
				break;
			case NodeTypes.KKTAttackWord:
				KKTAttackCount += attack = count;
				break;
		}
		TotalEndCount += end;
		TotalAttackCount += attack;
	}

	public NodeCount Combine(NodeCount other)
	{
		return new NodeCount()
		{
			TotalCount = TotalCount + other.TotalCount,
			TotalError = TotalError + other.TotalError,
			TotalEndCount = TotalEndCount + other.TotalEndCount,
			TotalAttackCount = TotalAttackCount + other.TotalAttackCount,
			EndCount = EndCount + other.EndCount,
			AttackCount = AttackCount + other.AttackCount,
			ReverseEndCount = ReverseEndCount + other.ReverseEndCount,
			ReverseAttackCount = ReverseAttackCount + other.ReverseAttackCount,
			KkutuEndCount = KkutuEndCount + other.KkutuEndCount,
			KkutuAttackCount = KkutuAttackCount + other.KkutuAttackCount,
			KKTEndCount = KKTEndCount + other.KKTEndCount,
			KKTAttackCount = KKTAttackCount + other.KKTAttackCount
		};
	}

	public override bool Equals(object? obj) => obj is NodeCount count && Equals(count);
	public bool Equals(NodeCount other) => TotalCount == other.TotalCount
		&& TotalError == other.TotalError
		&& TotalEndCount == other.TotalEndCount
		&& TotalAttackCount == other.TotalAttackCount
		&& EndCount == other.EndCount
		&& AttackCount == other.AttackCount
		&& ReverseEndCount == other.ReverseEndCount
		&& ReverseAttackCount == other.ReverseAttackCount
		&& KkutuEndCount == other.KkutuEndCount
		&& KkutuAttackCount == other.KkutuAttackCount
		&& KKTEndCount == other.KKTEndCount
		&& KKTAttackCount == other.KKTAttackCount;

	public override int GetHashCode()
	{
		var hash = new HashCode();
		hash.Add(TotalCount);
		hash.Add(TotalError);
		hash.Add(TotalEndCount);
		hash.Add(TotalAttackCount);
		hash.Add(EndCount);
		hash.Add(AttackCount);
		hash.Add(ReverseEndCount);
		hash.Add(ReverseAttackCount);
		hash.Add(KkutuEndCount);
		hash.Add(KkutuAttackCount);
		hash.Add(KKTEndCount);
		hash.Add(KKTAttackCount);
		return hash.ToHashCode();
	}

	public static bool operator ==(NodeCount left, NodeCount right) => left.Equals(right);
	public static bool operator !=(NodeCount left, NodeCount right) => !(left == right);
	public static NodeCount operator +(NodeCount left, NodeCount right) => left.Combine(right);
}
