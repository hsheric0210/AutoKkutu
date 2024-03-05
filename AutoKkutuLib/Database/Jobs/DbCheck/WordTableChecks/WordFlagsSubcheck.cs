using AutoKkutuLib.Database.Helper;
using Dapper;

namespace AutoKkutuLib.Database.Jobs.DbCheck.WordTableChecks;
internal class WordFlagsSubcheck : IWordTableSubcheck
{
	private readonly IDictionary<string, int> correction = new Dictionary<string, int>();
	private readonly WordFlagsRecalculator recalc;

	public string SubcheckName => "Fix Invalid Word Flag Index";

	public WordFlagsSubcheck(WordFlagsRecalculator recalc) => this.recalc = recalc;

	public bool Verify(WordModel entry)
	{
		const int keepFlags = (int)(WordFlags.LoanWord | WordFlags.Dialect | WordFlags.DeadLang | WordFlags.Munhwa);
		var currentFlags = entry.Flags;
		var keptFlags = currentFlags & keepFlags;

		var correctFlags = (int)recalc.GetWordFlags(entry.Word) | keptFlags;
		if (correctFlags != currentFlags)
		{
			LibLogger.Debug(SubcheckName, "Word {word} has invaild flags {currentFlags}, will be fixed to {correctFlags}.", entry.Word, (WordFlags)currentFlags, (WordFlags)correctFlags);
			correction.Add(entry.Word, correctFlags);
		}
		return false;
	}

	public int Fix(DbConnectionBase db)
	{
		var count = 0;

		foreach (var pair in correction)
		{
			var affected = db.Execute($"UPDATE {DatabaseConstants.WordTableName} SET {DatabaseConstants.FlagsColumnName} = @Flags WHERE {DatabaseConstants.WordColumnName} = @Word;", new
			{
				Flags = pair.Value,
				Word = pair.Key
			});

			if (affected > 0)
			{
				LibLogger.Debug(SubcheckName, "Reset flags of {word} to {to}.", pair.Key, (WordFlags)pair.Value);
				count += affected;
			}
		}
		return count;
	}
}
