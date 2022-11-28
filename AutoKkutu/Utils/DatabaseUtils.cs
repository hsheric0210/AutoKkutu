﻿using AutoKkutu.ConfigFile;
using AutoKkutu.Constants;
using AutoKkutu.Databases;
using AutoKkutu.Modules;
using Serilog;
using System;
using System.Configuration;
using System.Linq;

namespace AutoKkutu.Utils
{
	public static class DatabaseUtils
	{
		public static PathDbContext CreateDatabase(Configuration config)
		{
			if (config == null)
				throw new ArgumentNullException(nameof(config));

			DatabaseProvider prov = DatabaseProvider.Sqlite;
			string connString = $"Data Source={((SQLiteSection)config.GetSection("sqlite")).File}";

			// TODO: Remove this code and combine these multiple config entries into one config entry
			switch (((DatabaseTypeSection)config.GetSection("dbtype")).Type.ToUpperInvariant())
			{
				case "MYSQL":
				case "MARIADB":
					prov = DatabaseProvider.MySql;
					connString = ((MySQLSection)config.GetSection("mysql")).ConnectionString;
					break;

				case "POSTGRE":
				case "POSTGRESQL":
				case "PGSQL":
					prov = DatabaseProvider.PostgreSql;
					connString = ((PostgreSQLSection)config.GetSection("postgresql")).ConnectionString;
					break;
			}

			return new PathDbContext(prov, connString);
		}

		public static WordDbTypes GetWordFlags(this string word)
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
					PathManager.CheckNodePresence(null, word.GetMaFNode(), PathManager.EndWordList, WordDbTypes.MiddleEndWord, ref flags);

					// 가운뎃말잇기 공격 노드
					PathManager.CheckNodePresence(null, word.GetMaFNode(), PathManager.AttackWordList, WordDbTypes.MiddleAttackWord, ref flags);
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
					NewEndNode += Convert.ToInt32(PathManager.CheckNodePresence("middle end", word.GetMaFNode(), PathManager.EndWordList, WordDbTypes.MiddleEndWord, ref flags, true));

					// 가운뎃말잇기 공격 노드
					NewAttackNode += Convert.ToInt32(PathManager.CheckNodePresence("middle attack", word.GetMaFNode(), PathManager.AttackWordList, WordDbTypes.MiddleAttackWord, ref flags, true));
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