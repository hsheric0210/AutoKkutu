using AutoKkutuLib.Extension;
using AutoKkutuLib.Hangul;
using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class WordAdditionQuery : SqlQuery<bool>
{
	public string? Word { get; set; }
	public WordFlags? WordFlags { get; set; }

	internal WordAdditionQuery(DbConnectionBase connection) : base(connection) { }

	public bool Execute(string word, WordFlags wordFlags)
	{
		Word = word;
		WordFlags = wordFlags;
		return Execute();
	}

	public override bool Execute()
	{
		if (string.IsNullOrWhiteSpace(Word))
			throw new InvalidOperationException(nameof(Word) + " not set.");
		if (WordFlags is null)
			throw new InvalidOperationException(nameof(WordFlags) + " not set.");

		LibLogger.Debug<WordAdditionQuery>("Adding word {0} to database.", Word);
		if (Connection.ExecuteScalar<int>($"SELECT COUNT(*) FROM {DatabaseConstants.WordTableName} WHERE {DatabaseConstants.WordColumnName} = @Word;", new { Word }) > 0)
		{
			LibLogger.Debug<WordAdditionQuery>("Word {0} already exists in database.", Word);
			return false;
		}

		var count = Connection.Execute(
			$"INSERT INTO {DatabaseConstants.WordTableName}({DatabaseConstants.WordColumnName}, {DatabaseConstants.WordIndexColumnName}, {DatabaseConstants.ReverseWordIndexColumnName}, {DatabaseConstants.KkutuWordIndexColumnName}, {DatabaseConstants.TypeColumnName}, {DatabaseConstants.ThemeColumn1Name}, {DatabaseConstants.ThemeColumn2Name}, {DatabaseConstants.ThemeColumn3Name}, {DatabaseConstants.ThemeColumn4Name}, {DatabaseConstants.ChoseongColumnName}, {DatabaseConstants.MeaningColumnName}, {DatabaseConstants.FlagsColumnName}) VALUES(@Word, @LaFHead, @FaLHead, @KkutuHead, @Type, @Theme1, @Theme2, @Theme3, @Theme4, @Choseong, @Meaning, @Flags);",
			new
			{
				Word,
				LaFHead = Word.GetLaFHeadNode(),
				FaLHead = Word.GetFaLHeadNode(),
				KkutuHead = Word.GetKkutuHeadNode(),
				Type = 0,
				Theme1 = 0,
				Theme2 = 0,
				Theme3 = 0,
				Theme4 = 0,
				Choseong = Word.GetChoseong(),
				Meaning = "",
				Flags = (int)WordFlags
			});
		LibLogger.Debug<WordAdditionQuery>("Added {0} of word {1} to database with flags {2}.", count, Word, WordFlags);
		return count > 0;
	}
}
