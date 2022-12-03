using AutoKkutu.ConfigFile;
using AutoKkutu.Constants;
using AutoKkutu.Database;
using AutoKkutu.Database.MySQL;
using AutoKkutu.Database.PostgreSQL;
using AutoKkutu.Database.SQLite;
using AutoKkutu.Modules.PathManager;
using AutoKkutu.Utils.Extension;
using Serilog;
using System;
using System.Configuration;

namespace AutoKkutu.Utils
{
	public static class DatabaseUtils
	{
		public static AbstractDatabase CreateDatabase(Configuration config)
		{
			if (config == null)
				throw new ArgumentNullException(nameof(config));

			switch (((DatabaseTypeSection)config.GetSection("dbtype")).Type.ToUpperInvariant())
			{
				case "MARIADB":
				case "MYSQL":
					string mysqlConnectionString = ((MySQLSection)config.GetSection("mysql")).ConnectionString;
					Log.Information("MySQL selected: {connString}", mysqlConnectionString);
					return new MySqlDatabase(mysqlConnectionString);

				case "POSTGRESQL":
				case "POSTGRES":
				case "POSTGRE":
				case "PGSQL":
					string pgsqlConnectionString = ((PostgreSQLSection)config.GetSection("postgresql")).ConnectionString;
					Log.Information("PostgreSQL selected: {connString}", pgsqlConnectionString);
					return new PostgreSqlDatabase(pgsqlConnectionString);
			}

			string file = ((SQLiteSection)config.GetSection("sqlite")).File;
			Log.Information("SQLite selected: File={file}", file);
			return new SqliteDatabase(file);
		}

		public static WordDbTypes GetFlags(string word)
		{
			if (string.IsNullOrEmpty(word))
				throw new ArgumentException(null, nameof(word));

			WordDbTypes flags = WordDbTypes.None;

			// 한방 노드
			PathManager.CheckNodePresence(null, word.GetLaFTailNode(), PathManager.EndWordList, WordDbTypes.EndWord, ref flags);

			// 공격 노드
			PathManager.CheckNodePresence(null, word.GetLaFTailNode(), PathManager.AttackWordList, WordDbTypes.AttackWord, ref flags);

			// 앞말잇기 한방 노드
			PathManager.CheckNodePresence(null, word.GetFaLTailNode(), PathManager.ReverseEndWordList, WordDbTypes.ReverseEndWord, ref flags);

			// 앞말잇기 공격 노드
			PathManager.CheckNodePresence(null, word.GetFaLTailNode(), PathManager.ReverseAttackWordList, WordDbTypes.ReverseAttackWord, ref flags);

			int wordLength = word.Length;
			if (wordLength == 2)
			{
				flags |= WordDbTypes.KKT2;
			}
			if (wordLength > 2)
			{
				// 끄투 한방 노드
				PathManager.CheckNodePresence(null, word.GetKkutuTailNode(), PathManager.KkutuEndWordList, WordDbTypes.KkutuEndWord, ref flags);

				// 끄투 공격 노드
				PathManager.CheckNodePresence(null, word.GetKkutuTailNode(), PathManager.KkutuAttackWordList, WordDbTypes.KkutuAttackWord, ref flags);

				if (wordLength == 3)
				{
					flags |= WordDbTypes.KKT3;

					// 쿵쿵따 한방 노드
					PathManager.CheckNodePresence(null, word.GetLaFTailNode(), PathManager.KKTEndWordList, WordDbTypes.KKTEndWord, ref flags);

					// 쿵쿵따 공격 노드
					PathManager.CheckNodePresence(null, word.GetLaFTailNode(), PathManager.KKTAttackWordList, WordDbTypes.KKTAttackWord, ref flags);
				}

				if (wordLength % 2 == 1)
				{
					// 가운뎃말잇기 한방 노드
					PathManager.CheckNodePresence(null, word.GetMaFTailNode(), PathManager.EndWordList, WordDbTypes.MiddleEndWord, ref flags);

					// 가운뎃말잇기 공격 노드
					PathManager.CheckNodePresence(null, word.GetMaFTailNode(), PathManager.AttackWordList, WordDbTypes.MiddleAttackWord, ref flags);
				}
			}
			return flags;
		}

		public static void CorrectFlags(string word, ref WordDbTypes flags, ref int NewEndNode, ref int NewAttackNode)
		{
			if (string.IsNullOrEmpty(word))
				throw new ArgumentException(null, nameof(word));

			// 한방 노드
			NewEndNode += Convert.ToInt32(PathManager.CheckNodePresence("end", word.GetLaFTailNode(), PathManager.EndWordList, WordDbTypes.EndWord, ref flags, true));

			// 공격 노드
			NewAttackNode += Convert.ToInt32(PathManager.CheckNodePresence("attack", word.GetLaFTailNode(), PathManager.AttackWordList, WordDbTypes.AttackWord, ref flags, true));

			// 앞말잇기 한방 노드
			NewEndNode += Convert.ToInt32(PathManager.CheckNodePresence("reverse end", word.GetFaLTailNode(), PathManager.ReverseEndWordList, WordDbTypes.ReverseEndWord, ref flags, true));

			// 앞말잇기 공격 노드
			NewAttackNode += Convert.ToInt32(PathManager.CheckNodePresence("reverse attack", word.GetFaLTailNode(), PathManager.ReverseAttackWordList, WordDbTypes.ReverseAttackWord, ref flags, true));

			int wordLength = word.Length;
			if (word.Length == 2)
			{
				flags |= WordDbTypes.KKT2;
			}
			else if (wordLength > 2)
			{
				// 끄투 한방 노드
				NewEndNode += Convert.ToInt32(PathManager.CheckNodePresence("kkutu end", word.GetKkutuTailNode(), PathManager.KkutuEndWordList, WordDbTypes.KkutuEndWord, ref flags, true));
				NewEndNode++;

				// 끄투 공격 노드
				NewAttackNode += Convert.ToInt32(PathManager.CheckNodePresence("kkutu attack", word.GetKkutuTailNode(), PathManager.KkutuAttackWordList, WordDbTypes.KkutuAttackWord, ref flags, true));

				if (wordLength == 3)
				{
					flags |= WordDbTypes.KKT3;

					// 쿵쿵따 한방 노드
					NewEndNode += Convert.ToInt32(PathManager.CheckNodePresence("kungkungtta end", word.GetLaFTailNode(), PathManager.KKTEndWordList, WordDbTypes.EndWord, ref flags, true));

					// 쿵쿵따 공격 노드
					NewAttackNode += Convert.ToInt32(PathManager.CheckNodePresence("kungkungtta attack", word.GetLaFTailNode(), PathManager.KKTAttackWordList, WordDbTypes.AttackWord, ref flags, true));
				}

				if (wordLength % 2 == 1)
				{
					// 가운뎃말잇기 한방 노드
					NewEndNode += Convert.ToInt32(PathManager.CheckNodePresence("middle end", word.GetMaFTailNode(), PathManager.EndWordList, WordDbTypes.MiddleEndWord, ref flags, true));

					// 가운뎃말잇기 공격 노드
					NewAttackNode += Convert.ToInt32(PathManager.CheckNodePresence("middle attack", word.GetMaFTailNode(), PathManager.AttackWordList, WordDbTypes.MiddleAttackWord, ref flags, true));
				}
			}
		}
	}
}
