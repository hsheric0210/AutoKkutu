using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace AutoKkutu
{
	class DatabaseManager
	{
		public static string GetDBInfo()
		{
			return "Offline Database (Sqlite)";
		}

		public static void Init()
		{
			bool isInited = _isInited;
			if (!isInited)
			{
				try
				{
					ConsoleManager.Log(ConsoleManager.LogType.Info, "_currentOperationMode = DBMethod.Local.", "DatabaseManager");
					var _fileinfo = new FileInfo(_sqlLiteDBLocation);
					bool flag3 = !_fileinfo.Exists;
					if (flag3)
					{
						ConsoleManager.Log(ConsoleManager.LogType.Info, _sqlLiteDBLocation + " does not exist. Create new file.", "DatabaseManager");
						FileStream fs = File.Create(_sqlLiteDBLocation);
						fs.Close();
					}
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Opening _sqlLiteConnection.", "DatabaseManager");
					_sqlLiteConnection = new SqliteConnection("Data Source=" + _sqlLiteDBLocation);
					_sqlLiteConnection.Open();

					CheckTable();
					ConsoleManager.Log(ConsoleManager.LogType.Info, "DB Open Complete.", "DatabaseManager");
				}
				catch (Exception e)
				{
					bool flag5 = DBError != null;
					if (flag5)
					{
						DBError(null, EventArgs.Empty);
					}
					ConsoleManager.Log(ConsoleManager.LogType.Error, "Failed to connect DB : " + e.ToString(), "DatabaseManager");
				}
			}
		}

		public static List<string> GetEndWordList()
		{
			var result = new List<string>();

			var command2 = new SqliteCommand("SELECT * FROM endword_list", _sqlLiteConnection);
			using (SqliteDataReader reader2 = command2.ExecuteReader())
			{
				while (reader2.Read())
				{
					result.Add(reader2["word_index"].ToString());
				}
			}
			ConsoleManager.Log(ConsoleManager.LogType.Info, string.Format("Found Total {0} end word.", result.Count), "DatabaseManager");
			return result;
		}

		private static void ExecucteCommand(string command)
		{
			try
			{
				var commandObject2 = new SqliteCommand(command, _sqlLiteConnection);
				commandObject2.ExecuteNonQuery();
			}
			catch (Exception e)
			{
				ConsoleManager.Log(ConsoleManager.LogType.Error, "Failed to Execute DB Command ' " + command + " ' : " + e.ToString(), "DatabaseManager");
			}
		}

		public static void DeleteWord(string word)
		{
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Delete '" + word + "' from db...", "DatabaseManager");
			ExecucteCommand("DELETE FROM word_list WHERE word = '" + word + "'");
		}

		public static void AddEndWord(string node)
		{
			if (string.IsNullOrWhiteSpace(node))
				throw new ArgumentNullException("node");

			if (int.TryParse(new SqliteCommand(string.Format("SELECT COUNT(*) FROM endword_list WHERE word_index = '{0}';", node[0]), _sqlLiteConnection).ExecuteScalar().ToString(), out int i) && i > 0)
				throw new Exception("Found same word_index from db.");

			ExecucteCommand(string.Format("INSERT INTO endword_list(word_index) VALUES('{0}')", node[0]));
		}

		[DebuggerStepThrough]
		public static void CheckDB(bool UseOnlineDB)
		{
			DatabaseManager.< CheckDB > d__21 < CheckDB > d__ = new DatabaseManager.< CheckDB > d__21();
			< CheckDB > d__.UseOnlineDB = UseOnlineDB;
			< CheckDB > d__.<> t__builder = AsyncVoidMethodBuilder.Create();
			< CheckDB > d__.<> 1__state = -1;
			< CheckDB > d__.<> t__builder.Start < DatabaseManager.< CheckDB > d__21 > (ref < CheckDB > d__);
		}

		private static bool CheckElement(string i)
		{
			bool flag = !DatabaseManagement.KkutuDicCheck(i.Trim());
			bool result;
			if (flag)
			{
				ExecucteCommand("DELETE FROM word_list WHERE word = '" + i + "'");
				result = false;
			}
			else
			{
				result = true;
			}
			return result;
		}

		public static List<PathFinder.PathObject> FindWord(KkutuHandler.ResponsePresentedWord data, bool FindEndWordOnly)
		{
			int UseEndWord;
			if (FindEndWordOnly)
			{
				UseEndWord = 1;
			}
			else
			{
				UseEndWord = 0;
			}
			var result = new List<PathFinder.PathObject>();


			bool canSubstitution2 = data.CanSubstitution;
			SqliteCommand command2;
			if (canSubstitution2)
			{
				command2 = new SqliteCommand(string.Format("SELECT * FROM word_list WHERE (word_index = '{0}' OR word_index = '{1})' AND is_endword = {2} ORDER BY LENGTH(word) DESC LIMIT {3}", new object[]
				{
						data.Content,
						data.Substitution,
						UseEndWord,
						128
				}), _sqlLiteConnection);
			}
			else
			{
				command2 = new SqliteCommand(string.Format("SELECT * FROM word_list WHERE word_index = '{0}' AND is_endword = {1} ORDER BY LENGTH(word) DESC LIMIT {2}", data.Content, UseEndWord, 128), _sqlLiteConnection);
			}
			using (SqliteDataReader reader2 = command2.ExecuteReader())
			{
				while (reader2.Read())
				{
					result.Add(new PathFinder.PathObject(reader2["word"].ToString().Trim(), System.Convert.ToBoolean(System.Convert.ToInt32(reader2["is_endword"]))));
				}
			}

			return result;
		}

		public static void AddWord(string word, bool IsEndword)
		{
			if (string.IsNullOrWhiteSpace(word))
				throw new ArgumentNullException("word");

			int _isEndWord;
			if (IsEndword)
				_isEndWord = 1;
			else
				_isEndWord = 0;

			if (int.TryParse(new SqliteCommand("SELECT COUNT(*) FROM word_list WHERE word = '" + word + "';", _sqlLiteConnection).ExecuteScalar().ToString(), out int i) && i > 0)
				throw new AggregateException("Found same word from db.");

			ExecucteCommand(string.Format("INSERT INTO word_list(word_index, word, is_endword) VALUES('{0}', '{1}', {2} )", word[0], word, _isEndWord));
		}

		private static void CheckTable()
		{
			bool flag = !CheckTable_Check("word_list");
			if (flag)
			{
				MakeTable("word_list");
			}
			bool flag2 = !CheckTable_Check("endword_list");
			if (flag2)
			{
				MakeTable("endword_list");
			}
		}

		private static void MakeTable(string tablename)
		{
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Create Table : " + tablename, "DatabaseManager");
			bool flag = tablename == "word_list";
			if (flag)
			{
				ExecucteCommand("CREATE TABLE word_list (word VARCHAR(60) NOT NULL, word_index CHAR(1) NOT NULL, is_endword TINYINT(1) NOT NULL);");
			}
			else
			{
				bool flag2 = tablename == "endword_list";
				if (flag2)
				{
					ExecucteCommand("CREATE TABLE endword_list (word_index CHAR(1) NOT NULL);");
				}
			}
		}

		private static bool CheckTable_Check(string tablename)
		{
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Check Table : " + tablename, "DatabaseManager");
			try
			{
				return int.TryParse(new SqliteCommand("SELECT COUNT(*) FROM sqlite_master WHERE name='" + tablename + "';", _sqlLiteConnection).ExecuteScalar().ToString(), out int i) && i > 0;
			}
			catch (Exception e2)
			{
				ConsoleManager.Log(ConsoleManager.LogType.Info, "Failed to Execute Check DB Table ' " + tablename + " ' : " + e2.ToString(), "DatabaseManager");
				return false;
			}
		}

		private static readonly bool _isInited = false;

		private static string _serverName;

		private static string _id;

		private static string _password;

		private static string _db;

		private static string _port;

		private const string _loginstancename = "Database";

		private const int MAX_DATA = 128;

		private static SqliteConnection _sqlLiteConnection;

		private static readonly string _sqlLiteDBLocation = Environment.CurrentDirectory + "\\path.sqlite";

		public static EventHandler DBError;
	}
}
