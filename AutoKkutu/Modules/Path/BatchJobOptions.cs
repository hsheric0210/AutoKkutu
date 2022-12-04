using System;

namespace AutoKkutu.Modules.PathManager
{
	[Flags]
	public enum BatchJobOptions
	{
		None = 0,

		/// <summary>
		/// Remove words from the database.
		/// </summary>
		Remove = 1 << 0,

		/// <summary>
		/// Check if the word really exists and available in current server before adding it to the database.
		/// </summary>
		VerifyBeforeAdd = 1 << 1
	}
}
