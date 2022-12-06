namespace AutoKkutuLib.Path;

[Flags]
public enum BatchJobOptions
{
	/// <summary>
	/// The default action, add words to the database.
	/// </summary>
	None = 0,

	/// <summary>
	/// Remove words from the database.
	/// </summary>
	Remove = 1 << 0,

	/// <summary>
	/// Check if the word really exists and available in current server before add it to the database.
	/// </summary>
	VerifyBeforeAdd = 1 << 1
}
