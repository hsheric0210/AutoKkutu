using AutoKkutuLib.Hangul;
using Dapper;

namespace AutoKkutuLib.Database.Jobs.DbCheck.WordTableChecks;
internal class WordChoseongSubcheck : IWordTableSubcheck
{
	private readonly IDictionary<string, string> correction = new Dictionary<string, string>();

	public string SubcheckName => "Fix Invalid Choseong Index";

	public bool Verify(WordModel entry)
	{
		var newCho = entry.Word.GetChoseong();
		if (!string.Equals(newCho, entry.Choseong))
		{
			LibLogger.Debug(SubcheckName, "Invalid choseong '{cho}' for word '{word}' will be fixed to '{newcho}'", entry.Choseong, entry.Word, newCho);
			correction.Add(entry.Word, newCho);
		}
		return false;
	}

	public int Fix(DbConnectionBase db)
	{
		var count = 0;
		foreach (var pair in correction)
		{
			var affected = db.Execute($"UPDATE {DatabaseConstants.WordTableName} SET {DatabaseConstants.ChoseongColumnName} = @Value WHERE {DatabaseConstants.WordColumnName} = @Word;", new
			{
				pair.Value,
				Word = pair.Key
			});
			if (affected > 0)
			{
				LibLogger.Debug(SubcheckName, "Reset {column} of {word} to {to}.", DatabaseConstants.ChoseongColumnName, pair.Key, pair.Value);
				count += affected;
			}
		}
		return count;
	}
}
