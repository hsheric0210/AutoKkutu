using System.Diagnostics.CodeAnalysis;

namespace AutoKkutuLib;
public struct NodeCount
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

	public bool Equals(WordCount other)
	{
		return EndCount == other.EndCount
			&& AttackCount == other.AttackCount
			&& ReverseEndCount == other.ReverseEndCount
			&& ReverseAttackCount == other.ReverseAttackCount
			&& KkutuEndCount == other.KkutuEndCount
			&& KkutuAttackCount == other.KkutuAttackCount
			&& KKTEndCount == other.KKTEndCount
			&& KKTAttackCount == other.KKTAttackCount;
	}

	public override bool Equals([NotNullWhen(true)] object? obj) => obj is WordCount other && Equals(other);

	public override int GetHashCode()
	{
		return HashCode.Combine(HashCode.Combine(
			EndCount,
			AttackCount,
			ReverseEndCount,
			ReverseAttackCount,
			KkutuEndCount,
			KkutuAttackCount), HashCode.Combine(
				KKTEndCount,
				KKTAttackCount));
	}
}
