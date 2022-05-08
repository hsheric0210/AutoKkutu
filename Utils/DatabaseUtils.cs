using AutoKkutu.ConfigFile;
using AutoKkutu.Constants;
using AutoKkutu.Databases;
using AutoKkutu.Databases.MySQL;
using AutoKkutu.Databases.PostgreSQL;
using AutoKkutu.Databases.SQLite;
using log4net;
using System;
using System.Configuration;
using System.Linq;

namespace AutoKkutu.Utils
{
	public static class DatabaseUtils
	{
		private static readonly ILog Logger = LogManager.GetLogger("Database Exts");

		public static DatabaseWithDefaultConnection CreateDatabase(Configuration config)
		{
			if (config == null)
				throw new ArgumentNullException(nameof(config));

			switch (((DatabaseTypeSection)config.GetSection("dbtype")).Type.ToUpperInvariant())
			{
				case "MYSQL":
					string mysqlConnectionString = ((MySQLSection)config.GetSection("mysql")).ConnectionString;
					Logger.InfoFormat("MySQL selected: {0}", mysqlConnectionString);
					return new MySQLDatabase(mysqlConnectionString);

				case "MARIADB":
					string mariadbConnectionString = ((MySQLSection)config.GetSection("mysql")).ConnectionString;
					Logger.InfoFormat("MariaDB selected: {0}", mariadbConnectionString);
					return new MariaDBDatabase(mariadbConnectionString);

				case "POSTGRESQL":
				case "PGSQL":
					string pgsqlConnectionString = ((PostgreSQLSection)config.GetSection("postgresql")).ConnectionString;
					Logger.InfoFormat("PostgreSQL selected: {0}", pgsqlConnectionString);
					return new PostgreSQLDatabase(pgsqlConnectionString);
			}

			string file = ((SQLiteSection)config.GetSection("sqlite")).File;
			Logger.InfoFormat("SQLite selected: File={0}", file);
			return new SQLiteDatabase(file);
		}

		public static WordDatabaseAttributes GetFlags(string word)
		{
			if (string.IsNullOrEmpty(word))
				throw new ArgumentException(null, nameof(word));

			WordDatabaseAttributes flags = WordDatabaseAttributes.None;

			// 한방 노드
			PathFinder.CheckNodePresence(null, word.GetLaFTailNode(), PathFinder.EndWordList, WordDatabaseAttributes.EndWord, ref flags);

			// 공격 노드
			PathFinder.CheckNodePresence(null, word.GetLaFTailNode(), PathFinder.AttackWordList, WordDatabaseAttributes.AttackWord, ref flags);

			// 앞말잇기 한방 노드
			PathFinder.CheckNodePresence(null, word.GetFaLTailNode(), PathFinder.ReverseEndWordList, WordDatabaseAttributes.ReverseEndWord, ref flags);

			// 앞말잇기 공격 노드
			PathFinder.CheckNodePresence(null, word.GetFaLTailNode(), PathFinder.ReverseAttackWordList, WordDatabaseAttributes.ReverseAttackWord, ref flags);

			int wordLength = word.Length;
			if (wordLength == 2)
			{
				flags |= WordDatabaseAttributes.KKT2;
			}
			if (wordLength > 2)
			{
				// 끄투 한방 노드
				PathFinder.CheckNodePresence(null, word.GetKkutuTailNode(), PathFinder.KkutuEndWordList, WordDatabaseAttributes.KkutuEndWord, ref flags);

				// 끄투 공격 노드
				PathFinder.CheckNodePresence(null, word.GetKkutuTailNode(), PathFinder.KkutuAttackWordList, WordDatabaseAttributes.KkutuAttackWord, ref flags);

				if (wordLength == 3)
				{
					flags |= WordDatabaseAttributes.KKT3;

					// 쿵쿵따 한방 노드
					PathFinder.CheckNodePresence(null, word.GetLaFTailNode(), PathFinder.KKTEndWordList, WordDatabaseAttributes.KKTEndWord, ref flags);

					// 쿵쿵따 공격 노드
					PathFinder.CheckNodePresence(null, word.GetLaFTailNode(), PathFinder.KKTAttackWordList, WordDatabaseAttributes.KKTAttackWord, ref flags);
				}

				if (wordLength % 2 == 1)
				{
					// 가운뎃말잇기 한방 노드
					PathFinder.CheckNodePresence(null, word.GetMaFNode(), PathFinder.EndWordList, WordDatabaseAttributes.MiddleEndWord, ref flags);

					// 가운뎃말잇기 공격 노드
					PathFinder.CheckNodePresence(null, word.GetMaFNode(), PathFinder.AttackWordList, WordDatabaseAttributes.MiddleAttackWord, ref flags);
				}
			}
			return flags;
		}

		public static void CorrectFlags(string word, ref WordDatabaseAttributes flags, ref int NewEndNode, ref int NewAttackNode)
		{
			if (string.IsNullOrEmpty(word))
				throw new ArgumentException(null, nameof(word));

			// 한방 노드
			NewEndNode += Convert.ToInt32(PathFinder.CheckNodePresence("end", word.GetLaFTailNode(), PathFinder.EndWordList, WordDatabaseAttributes.EndWord, ref flags, true));

			// 공격 노드
			NewAttackNode += Convert.ToInt32(PathFinder.CheckNodePresence("attack", word.GetLaFTailNode(), PathFinder.AttackWordList, WordDatabaseAttributes.AttackWord, ref flags, true));

			// 앞말잇기 한방 노드
			NewEndNode += Convert.ToInt32(PathFinder.CheckNodePresence("reverse end", word.GetFaLTailNode(), PathFinder.ReverseEndWordList, WordDatabaseAttributes.ReverseEndWord, ref flags, true));

			// 앞말잇기 공격 노드
			NewAttackNode += Convert.ToInt32(PathFinder.CheckNodePresence("reverse attack", word.GetFaLTailNode(), PathFinder.ReverseAttackWordList, WordDatabaseAttributes.ReverseAttackWord, ref flags, true));

			int wordLength = word.Length;
			if (word.Length == 2)
			{
				flags |= WordDatabaseAttributes.KKT2;
			}
			else if (wordLength > 2)
			{
				// 끄투 한방 노드
				NewEndNode += Convert.ToInt32(PathFinder.CheckNodePresence("kkutu end", word.GetKkutuTailNode(), PathFinder.KkutuEndWordList, WordDatabaseAttributes.KkutuEndWord, ref flags, true));
				NewEndNode++;

				// 끄투 공격 노드
				NewAttackNode += Convert.ToInt32(PathFinder.CheckNodePresence("kkutu attack", word.GetKkutuTailNode(), PathFinder.KkutuAttackWordList, WordDatabaseAttributes.KkutuAttackWord, ref flags, true));

				if (wordLength == 3)
				{
					flags |= WordDatabaseAttributes.KKT3;

					// 쿵쿵따 한방 노드
					NewEndNode += Convert.ToInt32(PathFinder.CheckNodePresence("kungkungtta end", word.GetLaFTailNode(), PathFinder.KKTEndWordList, WordDatabaseAttributes.EndWord, ref flags, true));

					// 쿵쿵따 공격 노드
					NewAttackNode += Convert.ToInt32(PathFinder.CheckNodePresence("kungkungtta attack", word.GetLaFTailNode(), PathFinder.KKTAttackWordList, WordDatabaseAttributes.AttackWord, ref flags, true));
				}

				if (wordLength % 2 == 1)
				{
					// 가운뎃말잇기 한방 노드
					NewEndNode += Convert.ToInt32(PathFinder.CheckNodePresence("middle end", word.GetMaFNode(), PathFinder.EndWordList, WordDatabaseAttributes.MiddleEndWord, ref flags, true));

					// 가운뎃말잇기 공격 노드
					NewAttackNode += Convert.ToInt32(PathFinder.CheckNodePresence("middle attack", word.GetMaFNode(), PathFinder.AttackWordList, WordDatabaseAttributes.MiddleAttackWord, ref flags, true));
				}
			}
		}

		public static string GetLaFHeadNode(this string word)
		{
			if (word == null)
				throw new ArgumentNullException(nameof(word));

			return word[0].ToString();
		}

		public static string GetFaLHeadNode(this string word)
		{
			if (word == null)
				throw new ArgumentNullException(nameof(word));

			return word.Last().ToString();
		}

		public static string GetKkutuHeadNode(this string word)
		{
			if (word == null)
				throw new ArgumentNullException(nameof(word));

			if (word.Length >= 4)
				return word[..2];
			return word.Length >= 3 ? word[0].ToString() : "";
		}

		public static string GetLaFTailNode(this string word) => GetFaLHeadNode(word);

		public static string GetFaLTailNode(this string word) => GetLaFHeadNode(word);

		public static string GetKkutuTailNode(this string word)
		{
			if (word == null)
				throw new ArgumentNullException(nameof(word));

			return word.Length >= 4 ? word.Substring(word.Length - 3, 2) : word.Last().ToString();
		}

		public static string GetMaFNode(this string word)
		{
			if (word == null)
				throw new ArgumentNullException(nameof(word));

			return word[(word.Length - 1) / 2].ToString();
		}
	}
}
