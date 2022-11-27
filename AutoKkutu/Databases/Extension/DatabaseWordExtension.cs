using AutoKkutu.Constants;
using AutoKkutu.Utils;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace AutoKkutu.Databases.Extension
{
	public static class DatabaseWordExtension
	{
		public static bool AddWord(this DbSet<WordModel> table, string word, WordDatabaseAttributes flags)
		{
			if (table == null)
				throw new ArgumentNullException(nameof(table));
			if (string.IsNullOrWhiteSpace(word))
				throw new ArgumentNullException(nameof(word));

			if (table.Any(w => string.Equals(w.Word, word, StringComparison.OrdinalIgnoreCase)))
				return false;

			table.Add(new WordModel()
			{
				Word = word,
				WordIndex = word.GetLaFHeadNode(),
				ReverseWordIndex = word.GetFaLHeadNode(),
				KkutuWorldIndex = word.GetKkutuHeadNode(),
				Flags = (int)flags
			});

			return true;
		}

		public static void DeleteWord(this DbSet<WordModel> table, string word)
		{
			if (table == null)
				throw new ArgumentNullException(nameof(table));
			table.RemoveRange(table.Where(w => string.Equals(w.Word, word, StringComparison.OrdinalIgnoreCase)));
		}
	}
}
