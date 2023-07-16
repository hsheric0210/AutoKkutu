namespace AutoKkutuLib;
public struct WordCount : IEquatable<WordCount>
{
	public int TotalCount { get; private set; }
	public int TotalError { get; private set; }

	public int TotalEndCount { get; private set; }
	public int TotalAttackCount { get; private set; }

	public int EndCount { get; private set; }
	public int AttackCount { get; private set; }
	public int ReverseEndCount { get; private set; }
	public int ReverseAttackCount { get; private set; }
	public int MiddleEndCount { get; private set; }
	public int MiddleAttackCount { get; private set; }
	public int KkutuEndCount { get; private set; }
	public int KkutuAttackCount { get; private set; }
	public int KKTEndCount { get; private set; }
	public int KKTAttackCount { get; private set; }

	public void IncrementError() => TotalError++;

	public void Increment(WordFlags wordFlags, int count)
	{
		TotalCount += count;
		int end = 0, attack = 0;
		switch (wordFlags)
		{
			case WordFlags.EndWord:
				EndCount += end = count;
				break;
			case WordFlags.AttackWord:
				AttackCount += attack = count;
				break;
			case WordFlags.ReverseEndWord:
				ReverseEndCount += end = count;
				break;
			case WordFlags.ReverseAttackWord:
				ReverseAttackCount += attack = count;
				break;
			case WordFlags.MiddleEndWord:
				MiddleEndCount += end = count;
				break;
			case WordFlags.MiddleAttackWord:
				MiddleAttackCount += attack = count;
				break;
			case WordFlags.KkutuEndWord:
				KkutuEndCount += end = count;
				break;
			case WordFlags.KkutuAttackWord:
				KkutuAttackCount += attack = count;
				break;
			case WordFlags.KKTEndWord:
				KKTEndCount += end = count;
				break;
			case WordFlags.KKTAttackWord:
				KKTAttackCount += attack = count;
				break;
		}
		TotalEndCount += end;
		TotalAttackCount += attack;
	}

	public override bool Equals(object? obj) => obj is WordCount other && Equals(other);

	public bool Equals(WordCount other) => TotalCount == other.TotalCount
		&& TotalError == other.TotalError
		&& TotalEndCount == other.TotalEndCount
		&& TotalAttackCount == other.TotalAttackCount
		&& EndCount == other.EndCount
		&& AttackCount == other.AttackCount
		&& ReverseEndCount == other.ReverseEndCount
		&& ReverseAttackCount == other.ReverseAttackCount
		&& MiddleEndCount == other.MiddleEndCount
		&& MiddleAttackCount == other.MiddleAttackCount
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
		hash.Add(MiddleEndCount);
		hash.Add(MiddleAttackCount);
		hash.Add(KkutuEndCount);
		hash.Add(KkutuAttackCount);
		hash.Add(KKTEndCount);
		hash.Add(KKTAttackCount);
		return hash.ToHashCode();
	}

	public static bool operator ==(WordCount left, WordCount right) => left.Equals(right);
	public static bool operator !=(WordCount left, WordCount right) => !(left == right);
}
