using AutoKkutuLib.Extension;
using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class WordAdditionQuery : SqlQuery<bool>
{
	public string? Word { get; set; }
	public WordFlags? WordFlags { get; set; }

	internal WordAdditionQuery(AbstractDatabaseConnection connection) : base(connection) { }

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

		if (Connection.ExecuteScalar<int>($"SELECT COUNT(*) FROM {DatabaseConstants.WordTableName} WHERE {DatabaseConstants.WordColumnName} = @Word;", new { Word }) > 0)
			return false;

		Connection.Execute(
			$"INSERT INTO {DatabaseConstants.WordTableName}({DatabaseConstants.WordColumnName}, {DatabaseConstants.WordIndexColumnName}, {DatabaseConstants.ReverseWordIndexColumnName}, {DatabaseConstants.KkutuWordIndexColumnName}, {DatabaseConstants.FlagsColumnName}) VALUES(@Word, @LaFHead, @FaLHead, @KkutuHead, @Flags);",
			new
			{
				Word,
				LaFHead = Word.GetLaFHeadNode(),
				FaLHead = Word.GetFaLHeadNode(),
				KkutuHead = Word.GetKkutuHeadNode(),
				Flags = (int)WordFlags
			});
		return true;
	}
}
