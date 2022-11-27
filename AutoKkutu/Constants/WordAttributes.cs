using System;

namespace AutoKkutu.Constants
{
	[Flags]
	public enum WordAttributes
	{
		None = 0,
		EndWord = 1 << 0,
		AttackWord = 1 << 1,
		MissionWord = 1 << 2
	}
}
