using AutoKkutu.Constants;
using AutoKkutu.Utils.Extension;
using Dapper;
using System;

namespace AutoKkutu.Database.Extension;

public static class WordExtension
{
	public static bool AddWord(this AbstractDatabaseConnection connection, string word, WordFlags flags)
	{
		if (connection == null)
			throw new ArgumentNullException(nameof(connection));
		if (string.IsNullOrWhiteSpace(word))
			throw new ArgumentNullException(nameof(word));

		if (connection.ExecuteScalar<int>($"SELECT COUNT(*) FROM {DatabaseConstants.WordTableName} WHERE {DatabaseConstants.WordColumnName} = @Word;", new
		{
			Word = word
		}) > 0)
		{
			return false;
		}

		connection.Execute(
			$"INSERT INTO {DatabaseConstants.WordTableName}({DatabaseConstants.WordColumnName}, {DatabaseConstants.WordIndexColumnName}, {DatabaseConstants.ReverseWordIndexColumnName}, {DatabaseConstants.KkutuWordIndexColumnName}, {DatabaseConstants.FlagsColumnName}) VALUES(@Word, @LaFHead, @FaLHead, @KkutuHead, @Flags)",
			new
			{
				Word = word,
				LaFHead = word.GetLaFHeadNode(),
				FaLHead = word.GetFaLHeadNode(),
				KkutuHead = word.GetKkutuHeadNode(),
				Flags = (int)flags
			});
		return true;
	}

	public static int DeleteWord(this AbstractDatabaseConnection connection, string word)
	{
		if (connection == null)
			throw new ArgumentNullException(nameof(connection));

		return connection.Execute($"DELETE FROM {DatabaseConstants.WordTableName} WHERE {DatabaseConstants.WordColumnName} = @Word", new
		{
			Word = word
		});
	}
}
