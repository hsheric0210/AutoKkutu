using AutoKkutu.Constants;
using AutoKkutu.Utils;
using System;
using System.Globalization;

namespace AutoKkutu.Databases.Extension
{
	public static class WordExtension
	{
		public static bool AddWord(this CommonDatabaseConnection connection, string word, WordDatabaseAttributes flags)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));
			if (string.IsNullOrWhiteSpace(word))
				throw new ArgumentNullException(nameof(word));

			if (Convert.ToInt32(connection.ExecuteScalar($"SELECT COUNT(*) FROM {DatabaseConstants.WordListTableName} WHERE {DatabaseConstants.WordColumnName} = @word;", connection.CreateParameter("@word", word)), CultureInfo.InvariantCulture) > 0)
				return false;

			connection.ExecuteNonQuery(
				$"INSERT INTO {DatabaseConstants.WordListTableName}({DatabaseConstants.WordIndexColumnName}, {DatabaseConstants.ReverseWordIndexColumnName}, {DatabaseConstants.KkutuWordIndexColumnName}, {DatabaseConstants.WordColumnName}, {DatabaseConstants.FlagsColumnName}) VALUES(@lafHead, @falHead, @kkutuHead, @word, {(int)flags})",
				connection.CreateParameter(CommonDatabaseType.Character, 1, "@lafHead", word.GetLaFHeadNode()),
				connection.CreateParameter(CommonDatabaseType.Character, 1, "@falHead", word.GetFaLHeadNode()),
				connection.CreateParameter(CommonDatabaseType.CharacterVarying, 2, "@kkutuHead", word.GetKkutuHeadNode()),
				connection.CreateParameter("@word", word));
			return true;
		}

		public static int DeleteWord(this CommonDatabaseConnection connection, string word)
		{
			if (connection == null)
				throw new ArgumentNullException(nameof(connection));

			return connection.ExecuteNonQuery($"DELETE FROM {DatabaseConstants.WordListTableName} WHERE {DatabaseConstants.WordColumnName} = @word", connection.CreateParameter("@word", word));
		}
	}
}
