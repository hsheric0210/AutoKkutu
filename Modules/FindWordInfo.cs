using AutoKkutu.Constants;
using System;

namespace AutoKkutu.Modules
{
	public struct FindWordInfo : IEquatable<FindWordInfo>
	{
		public string MissionChar
		{
			get; set;
		}

		public GameMode Mode
		{
			get; set;
		}

		public PathFinderOptions PathFinderFlags
		{
			get; set;
		}

		public ResponsePresentedWord Word
		{
			get; set;
		}

		public WordPreference WordPreference
		{
			get; set;
		}

		public static bool operator !=(FindWordInfo left, FindWordInfo right) => !(left == right);

		public static bool operator ==(FindWordInfo left, FindWordInfo right) => left.Equals(right);

		public override bool Equals(object? obj) => obj is FindWordInfo other && Equals(other);

		public bool Equals(FindWordInfo other) =>
				MissionChar.Equals(other.MissionChar, StringComparison.OrdinalIgnoreCase)
				&& Mode == other.Mode
				&& PathFinderFlags == other.PathFinderFlags
				&& Word == other.Word
				&& WordPreference == other.WordPreference;

		public override int GetHashCode() => HashCode.Combine(MissionChar, Mode, PathFinderFlags, Word, WordPreference);
	}
}
