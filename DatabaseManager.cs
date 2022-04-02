using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Data.Sqlite;

namespace AutoKkutu
{
	internal class DatabaseManager
	{
		public static string GetDBInfo() => "Offline Database (Sqlite)";

		private const string LOG_MODULE_NAME = "DatabaseManager";

		public static void Init()
		{
			bool isInited = _isInited;
			if (!isInited)
			{
				try
				{
					ConsoleManager.Log(ConsoleManager.LogType.Info, "_currentOperationMode = DBMethod.Local.", LOG_MODULE_NAME);
					var _fileinfo = new FileInfo(_sqlLiteDBLocation);
					bool flag3 = !_fileinfo.Exists;
					if (flag3)
					{
						ConsoleManager.Log(ConsoleManager.LogType.Info, _sqlLiteDBLocation + " does not exist. Create new file.", LOG_MODULE_NAME);
						FileStream fs = File.Create(_sqlLiteDBLocation);
						fs.Close();
					}
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Opening _sqlLiteConnection.", LOG_MODULE_NAME);
					_sqlLiteConnection = new SqliteConnection("Data Source=" + _sqlLiteDBLocation);
					_sqlLiteConnection.Open();

					CheckTable();
					ConsoleManager.Log(ConsoleManager.LogType.Info, "DB Open Complete.", LOG_MODULE_NAME);
				}
				catch (Exception e)
				{
					bool flag5 = DBError != null;
					if (flag5)
					{
						DBError(null, EventArgs.Empty);
					}
					ConsoleManager.Log(ConsoleManager.LogType.Error, "Failed to connect DB : " + e.ToString(), LOG_MODULE_NAME);
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
			ConsoleManager.Log(ConsoleManager.LogType.Info, string.Format("Found Total {0} end word.", result.Count), LOG_MODULE_NAME);
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
				ConsoleManager.Log(ConsoleManager.LogType.Error, "Failed to Execute DB Command ' " + command + " ' : " + e.ToString(), LOG_MODULE_NAME);
			}
		}

		public static void DeleteWord(string word)
		{
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Delete '" + word + "' from db...", LOG_MODULE_NAME);
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

		public static async void CheckDB(bool UseOnlineDB)
		{
			if (string.IsNullOrWhiteSpace(DatabaseManagement.EvaluateJS("document.getElementById('dict-output').style")))
			{
				MessageBox.Show("사전 창을 감지하지 못했습니다.\n끄투 사전 창을 키십시오.", "데이터베이스 관리자", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Database Intergrity Check....\nIt will be very long task.", LOG_MODULE_NAME);
			int dbTotalCount;

			var command2 = new SqliteCommand("SELECT COUNT(*) FROM word_list", _sqlLiteConnection);
			int.TryParse(command2.ExecuteScalar().ToString(), out dbTotalCount);

			ConsoleManager.Log(ConsoleManager.LogType.Info, $"Database has Total {dbTotalCount} elements.", LOG_MODULE_NAME);
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Getting all elements from database..", LOG_MODULE_NAME);
			int elementCount = 0;
			int SuccessCount = 0;
			int FailedCount = 0;


			ConsoleManager.Log(ConsoleManager.LogType.Info, "Opening _ChecksqlLiteConnection.", LOG_MODULE_NAME);
			var _ChecksqlLiteConnection = new SqliteConnection("Data Source=" + _sqlLiteDBLocation);
			_ChecksqlLiteConnection.Open();
			var command = new SqliteCommand("SELECT * FROM word_list ORDER BY(word) DESC", _ChecksqlLiteConnection);
			using (SqliteDataReader reader = command.ExecuteReader())
			{
				while (reader.Read())
				{
					elementCount++;
					string content = reader["word"].ToString();
					ConsoleManager.Log(ConsoleManager.LogType.Info, $"Total {dbTotalCount} of {elementCount} ({content})", LOG_MODULE_NAME);
					if (content.Length == 1 || int.TryParse(content[0].ToString(), out int _) || content[0] == '[' || content[0] == ' ' || content[0] == '-' || content[0] == '.')
					{
						ConsoleManager.Log(ConsoleManager.LogType.Info, "Word is too short or first char is numberic. Remove.", LOG_MODULE_NAME);
						ExecucteCommand("DELETE FROM word_list WHERE word = '" + content + "'");
						FailedCount++;
						continue;
					}
					if (UseOnlineDB && !CheckElement(content))
					{
						FailedCount++;
						continue;
					}
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Index Check...", LOG_MODULE_NAME);
					if (content[0].ToString() != reader["word_index"].ToString())
					{
						ConsoleManager.Log(ConsoleManager.LogType.Info, "Invaild Word Index. Fixing it..", LOG_MODULE_NAME);
						ExecucteCommand($"UPDATE word_list SET word_index = '{content[0]}' WHERE word = '{content}';");
					}
					bool IsEndWord = PathFinder.EndWordList.Contains(content.Last().ToString());
					int Is_endWord = Convert.ToInt32(IsEndWord);
					if (IsEndWord != Convert.ToBoolean(reader["is_endword"].ToString()))
					{
						ConsoleManager.Log(ConsoleManager.LogType.Info, "Invaild EndWord Tag. Fixing it..", LOG_MODULE_NAME);
						ExecucteCommand("UPDATE word_list SET is_endword = '" + Is_endWord + "' WHERE word = '" + content + "';");
					}
					SuccessCount++;
				}
			}
			_ChecksqlLiteConnection.Close();

			ConsoleManager.Log(ConsoleManager.LogType.Info, $"Total {dbTotalCount} / Success {SuccessCount} / Failed {FailedCount}.", LOG_MODULE_NAME);
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Database Check Completed.", LOG_MODULE_NAME);
		}

		private static bool CheckElement(string i)
		{
			bool flag = !DatabaseManagement.KkutuOnlineDictCheck(i.Trim());
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

		public static List<PathFinder.PathObject> FindWord(CommonHandler.ResponsePresentedWord data, bool preferEndWord)
		{
			int UseEndWord;
			if (preferEndWord)
				UseEndWord = 1;
			else
				UseEndWord = 0;
			var result = new List<PathFinder.PathObject>();
			string command;
			if (data.CanSubstitution)
				command = string.Format("SELECT * FROM word_list WHERE (word_index = '{0}' OR word_index = '{1}') AND is_endword = {2} ORDER BY LENGTH(word) DESC LIMIT {3}", new object[]
								{
						data.Content,
						data.Substitution,
						UseEndWord,
						128
								});
			else
				command = string.Format("SELECT * FROM word_list WHERE word_index = '{0}' AND is_endword = {1} ORDER BY LENGTH(word) DESC LIMIT {2}", data.Content, UseEndWord, 128);
			ConsoleManager.Log(ConsoleManager.LogType.Verbose, "SQL Query: " + command, LOG_MODULE_NAME);
			using (SqliteDataReader reader2 = new SqliteCommand(command, _sqlLiteConnection).ExecuteReader())
			{
				while (reader2.Read())
				{
					result.Add(new PathFinder.PathObject(reader2["word"].ToString().Trim(), Convert.ToBoolean(Convert.ToInt32(reader2["is_endword"]))));
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
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Create Table : " + tablename, LOG_MODULE_NAME);
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
			ConsoleManager.Log(ConsoleManager.LogType.Info, "Check Table : " + tablename, LOG_MODULE_NAME);
			try
			{
				return int.TryParse(new SqliteCommand("SELECT COUNT(*) FROM sqlite_master WHERE name='" + tablename + "';", _sqlLiteConnection).ExecuteScalar().ToString(), out int i) && i > 0;
			}
			catch (Exception e2)
			{
				ConsoleManager.Log(ConsoleManager.LogType.Info, "Failed to Execute Check DB Table ' " + tablename + " ' : " + e2.ToString(), LOG_MODULE_NAME);
				return false;
			}
		}

		private static readonly bool _isInited = false;

		private static SqliteConnection _sqlLiteConnection;

		private static readonly string _sqlLiteDBLocation = Environment.CurrentDirectory + "\\path.sqlite";

		public static EventHandler DBError;
	}
}
