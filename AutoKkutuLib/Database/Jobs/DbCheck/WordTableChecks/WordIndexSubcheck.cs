using Dapper;

namespace AutoKkutuLib.Database.Jobs.DbCheck.WordTableChecks;
internal class WordIndexSubcheck : IWordTableSubcheck
{
	private readonly string columnName;
	private readonly Func<WordModel, string> columnGetter;
	private readonly Func<string, string> wordToNodeConverter;
	private readonly IDictionary<string, string> correction = new Dictionary<string, string>();

	public string SubcheckName => "Fix Invalid Word Index (" + columnName + ")";

	public WordIndexSubcheck(string columnName, Func<WordModel, string> columnGetter, Func<string, string> wordToNodeConverter)
	{
		this.columnName = columnName;
		this.columnGetter = columnGetter;
		this.wordToNodeConverter = wordToNodeConverter;
	}

	public bool Verify(WordModel entry)
	{
		var correctWordIndex = wordToNodeConverter(entry.Word);
		var currentWordIndex = columnGetter(entry);
		if (correctWordIndex != currentWordIndex)
		{
			LibLogger.Debug(SubcheckName, "Invaild {wordIndexName} column {currentWordIndex}, will be fixed to {correctWordIndex}.", columnName, currentWordIndex, correctWordIndex);
			correction.Add(entry.Word, correctWordIndex);
		}
		return false;
	}

	public int Fix(DbConnectionBase db)
	{
		var count = 0;
		foreach (var pair in correction)
		{
			var affected = db.Execute($"UPDATE {DatabaseConstants.WordTableName} SET {columnName} = @Value WHERE {DatabaseConstants.WordColumnName} = @Word;", new
			{
				pair.Value,
				Word = pair.Key
			});
			if (affected > 0)
			{
				LibLogger.Debug(SubcheckName, "Reset {column} of {word} to {to}.", columnName, pair.Key, pair.Value);
				count += affected;
			}
		}
		return count;
	}
}
