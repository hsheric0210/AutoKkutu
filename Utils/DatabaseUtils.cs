using AutoKkutu.Databases;
using AutoKkutu.Databases.MySQL;
using AutoKkutu.Databases.PostgreSQL;
using AutoKkutu.Databases.SQLite;
using log4net;
using System;
using System.Configuration;
using System.Linq;
using static AutoKkutu.Constants;

namespace AutoKkutu.Utils
{
	// TODO: Split into FlagExtension and NodeExtension
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

		public static WordFlags GetFlags(string word)
		{
			if (string.IsNullOrEmpty(word))
				throw new ArgumentException(null, nameof(word));

			WordFlags flags = WordFlags.None;

			// 한방 노드
			PathFinder.CheckNodePresence(null, word.GetLaFTailNode(), PathFinder.EndWordList, WordFlags.EndWord, ref flags);

			// 공격 노드
			PathFinder.CheckNodePresence(null, word.GetLaFTailNode(), PathFinder.AttackWordList, WordFlags.AttackWord, ref flags);

			// 앞말잇기 한방 노드
			PathFinder.CheckNodePresence(null, word.GetFaLTailNode(), PathFinder.ReverseEndWordList, WordFlags.ReverseEndWord, ref flags);

			// 앞말잇기 공격 노드
			PathFinder.CheckNodePresence(null, word.GetFaLTailNode(), PathFinder.ReverseAttackWordList, WordFlags.ReverseAttackWord, ref flags);

			if (word.Length > 2)
			{
				// 끄투 한방 노드
				PathFinder.CheckNodePresence(null, word.GetKkutuTailNode(), PathFinder.KkutuEndWordList, WordFlags.KkutuEndWord, ref flags);

				// 끄투 공격 노드
				PathFinder.CheckNodePresence(null, word.GetKkutuTailNode(), PathFinder.KkutuAttackWordList, WordFlags.KkutuAttackWord, ref flags);

				if (word.Length % 2 == 1)
				{
					// 가운뎃말잇기 한방 노드
					PathFinder.CheckNodePresence(null, word.GetMaFNode(), PathFinder.EndWordList, WordFlags.MiddleEndWord, ref flags);

					// 가운뎃말잇기 공격 노드
					PathFinder.CheckNodePresence(null, word.GetMaFNode(), PathFinder.AttackWordList, WordFlags.MiddleAttackWord, ref flags);
				}
			}
			return flags;
		}

		public static void CorrectFlags(string word, ref WordFlags flags, ref int NewEndNode, ref int NewAttackNode)
		{
			if (string.IsNullOrEmpty(word))
				throw new ArgumentException(null, nameof(word));

			// 한방 노드
			if (PathFinder.CheckNodePresence("end", word.GetLaFTailNode(), PathFinder.EndWordList, WordFlags.EndWord, ref flags, true))
				NewEndNode++;

			// 공격 노드
			if (PathFinder.CheckNodePresence("attack", word.GetLaFTailNode(), PathFinder.AttackWordList, WordFlags.AttackWord, ref flags, true))
				NewAttackNode++;

			// 앞말잇기 한방 노드
			if (PathFinder.CheckNodePresence("reverse end", word.GetFaLTailNode(), PathFinder.ReverseEndWordList, WordFlags.ReverseEndWord, ref flags, true))
				NewEndNode++;

			// 앞말잇기 공격 노드
			if (PathFinder.CheckNodePresence("reverse attack", word.GetFaLTailNode(), PathFinder.ReverseAttackWordList, WordFlags.ReverseAttackWord, ref flags, true))
				NewAttackNode++;
			if (word.Length > 2)
			{
				// 끄투 한방 노드
				if (PathFinder.CheckNodePresence("kkutu end", word.GetKkutuTailNode(), PathFinder.KkutuEndWordList, WordFlags.KkutuEndWord, ref flags, true))
					NewEndNode++;

				// 끄투 공격 노드
				if (PathFinder.CheckNodePresence("kkutu attack", word.GetKkutuTailNode(), PathFinder.KkutuAttackWordList, WordFlags.KkutuAttackWord, ref flags, true))
					NewAttackNode++;

				if (word.Length % 2 == 1)
				{
					// 가운뎃말잇기 한방 노드
					if (PathFinder.CheckNodePresence("middle end", word.GetMaFNode(), PathFinder.EndWordList, WordFlags.MiddleEndWord, ref flags, true))
						NewEndNode++;

					// 가운뎃말잇기 공격 노드
					if (PathFinder.CheckNodePresence("middle attack", word.GetMaFNode(), PathFinder.AttackWordList, WordFlags.MiddleAttackWord, ref flags, true))
						NewAttackNode++;
				}
			}
		}

		public static string GetLaFHeadNode(this string word) => word.First().ToString();

		public static string GetFaLHeadNode(this string word) => word.Last().ToString();

		public static string GetKkutuHeadNode(this string word)
		{
			if (word == null)
				throw new ArgumentNullException(nameof(word));

			if (word.Length >= 4)
				return word.Substring(0, 2);
			return (word.Length >= 3 ? word.First().ToString() : "");
		}

		public static string GetLaFTailNode(this string word) => word.Last().ToString();

		public static string GetFaLTailNode(this string word) => word.First().ToString();

		public static string GetKkutuTailNode(this string word) => word.Length >= 4 ? word.Substring(word.Length - 3, 2) : word.Last().ToString();

		public static string GetMaFNode(this string word) => word[(word.Length - 1) / 2].ToString();
	}
}
