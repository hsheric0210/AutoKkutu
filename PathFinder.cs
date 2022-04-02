using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace AutoKkutu
{
	class PathFinder
	{
		public static void Init()
		{
			try
			{
				EndWordList = DatabaseManager.GetEndWordList();
			}
			catch (Exception e)
			{
				ConsoleManager.Log(ConsoleManager.LogType.Error, "Failed to Get End word :  " + e.ToString(), _loginstancename);
			}
		}

		public static void AutoDBUpdate(bool IsEnabled)
		{
			bool flag = !IsEnabled;
			if (!flag)
			{
				ConsoleManager.Log(ConsoleManager.LogType.Info, "Automatically update the DB based on last game..", _loginstancename);
				bool flag2 = AutoDBUpdateList.Count == 0;
				if (flag2)
				{
					ConsoleManager.Log(ConsoleManager.LogType.Info, "No such element in autoupdate list.", _loginstancename);
				}
				else
				{
					ConsoleManager.Log(ConsoleManager.LogType.Info, string.Format("Get {0} elements from AutoDBUpdateList.", AutoDBUpdateList.Count), _loginstancename);
					foreach (string i in AutoDBUpdateList)
					{
						bool flag3 = EndWordList.Contains(i.Last().ToString());
						bool isendword = flag3;
						try
						{
							ConsoleManager.Log(ConsoleManager.LogType.Info, "Check and add '" + i + "' into database.", _loginstancename);
							DatabaseManager.AddWord(i, isendword);
						}
						catch (AggregateException)
						{
							ConsoleManager.Log(ConsoleManager.LogType.Warning, "'" + i + "' has already included in database.", _loginstancename);
						}
						catch (Exception e2)
						{
							ConsoleManager.Log(ConsoleManager.LogType.Error, "Can't Add element to database : " + e2.ToString(), _loginstancename);
						}
					}
					AutoDBUpdateList = new List<string>();
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Automatic DB Update complete.", _loginstancename);
				}
			}
		}

		public static void AddPreviousPath(string word)
		{
			bool flag = string.IsNullOrWhiteSpace(word);
			if (!flag)
			{
				bool flag2 = EndWordList.Contains(word[word.Length - 1].ToString());
				if (flag2)
				{
				}
				PreviousPath.Add(word);
			}
		}

		private static List<PathObject> QualifyList(List<PathObject> input)
		{
			var result = new List<PathObject>();
			foreach (PathObject o in input)
			{
				bool flag = PreviousPath.Contains(o.Content);
				if (flag)
				{
					ConsoleManager.Log(ConsoleManager.LogType.Warning, "Except ' " + o.Content + " ' because including in PreviousPath List.", _loginstancename);
				}
				else
				{
					result.Add(o);
				}
			}
			return result;
		}

		public static void FindPath(KkutuHandler.ResponsePresentedWord i, bool UseEndWord)
		{
			bool canSubstitution = i.CanSubstitution;
			if (canSubstitution)
			{
				ConsoleManager.Log(ConsoleManager.LogType.Info, string.Concat(new string[]
				{
					"Finding path for ",
					i.Content,
					" (",
					i.Substitution,
					")."
				}), _loginstancename);
			}
			else
			{
				ConsoleManager.Log(ConsoleManager.LogType.Info, "Finding path for " + i.Content + ".", _loginstancename);
			}
			var watch = new Stopwatch();
			watch.Start();
			FinalList = new List<PathObject>();
			var NormalWord = new List<PathObject>();
			var EndWord = new List<PathObject>();
			var QualifiedNormalList = new List<PathObject>();
			var QualifiedEndList = new List<PathObject>();
			try
			{
				NormalWord = DatabaseManager.FindWord(i, false);
				ConsoleManager.Log(ConsoleManager.LogType.Info, string.Format("Find {0} Word.", NormalWord.Count), _loginstancename);
				if (UseEndWord)
				{
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Endword priority enabled.", _loginstancename);
					EndWord = DatabaseManager.FindWord(i, true);
					ConsoleManager.Log(ConsoleManager.LogType.Info, string.Format("Find {0} Word.", EndWord.Count), _loginstancename);
				}
			}
			catch (Exception e)
			{
				watch.Stop();
				ConsoleManager.Log(ConsoleManager.LogType.Error, "Failed to Find Path : " + e.ToString(), _loginstancename);
				bool flag = UpdatedPath != null;
				if (flag)
				{
					UpdatedPath(null, new UpdatedPathEventArgs(FindResult.Error, 0, 0, 0, false));
				}
			}
			QualifiedNormalList = QualifyList(NormalWord);
			if (UseEndWord)
			{
				QualifiedEndList = QualifyList(EndWord);
				bool flag2 = QualifiedEndList.Count != 0;
				if (flag2)
				{
					bool flag3 = QualifiedEndList.Count > 5;
					if (flag3)
					{
						QualifiedEndList = QualifiedEndList.Take(5).ToList();
					}
					bool flag4 = QualifiedNormalList.Count > 25;
					if (flag4)
					{
						QualifiedNormalList = QualifiedNormalList.Take(20).ToList();
					}
					FinalList = QualifiedEndList.Concat(QualifiedNormalList).ToList();
				}
				else
				{
					bool flag5 = QualifiedNormalList.Count == 0;
					if (flag5)
					{
						watch.Stop();
						ConsoleManager.Log(ConsoleManager.LogType.Warning, "Can't find any path.", _loginstancename);
						bool flag6 = UpdatedPath != null;
						if (flag6)
						{
							UpdatedPath(null, new UpdatedPathEventArgs(FindResult.None, NormalWord.Count, 0, Convert.ToInt32(watch.ElapsedMilliseconds), true));
						}
						return;
					}
					bool flag7 = QualifiedNormalList.Count > 20;
					if (flag7)
					{
						QualifiedNormalList = QualifiedNormalList.Take(20).ToList();
					}
					FinalList = QualifiedNormalList;
				}
			}
			else
			{
				bool flag8 = QualifiedNormalList.Count == 0;
				if (flag8)
				{
					watch.Stop();
					ConsoleManager.Log(ConsoleManager.LogType.Warning, "Can't find any path.", _loginstancename);
					bool flag9 = UpdatedPath != null;
					if (flag9)
					{
						UpdatedPath(null, new UpdatedPathEventArgs(FindResult.None, NormalWord.Count, 0, Convert.ToInt32(watch.ElapsedMilliseconds), false));
					}
					return;
				}
				bool flag10 = QualifiedNormalList.Count > 20;
				if (flag10)
				{
					QualifiedNormalList = QualifiedNormalList.Take(20).ToList();
				}
				FinalList = QualifiedNormalList;
			}
			watch.Stop();
			ConsoleManager.Log(ConsoleManager.LogType.Warning, string.Format("Total {0} Path Ready. ({1}ms)", FinalList.Count, watch.ElapsedMilliseconds), _loginstancename);
			bool flag11 = UpdatedPath != null;
			if (flag11)
			{
				UpdatedPath(null, new UpdatedPathEventArgs(FindResult.Normal, NormalWord.Count, FinalList.Count, Convert.ToInt32(watch.ElapsedMilliseconds), UseEndWord));
			}
		}

		private static readonly string _loginstancename = "PathFinder";

		public static List<string> EndWordList;

		public static List<PathObject> FinalList;

		public static List<string> PreviousPath = new List<string>();

		public static List<string> AutoDBUpdateList = new List<string>();

		public static EventHandler UpdatedPath;

		public enum FindResult
		{
			Normal,
			None,
			Error
		}

		public class UpdatedPathEventArgs : EventArgs
		{
			public UpdatedPathEventArgs(FindResult arg, int totalWordCount = 0, int calcWordCount = 0, int time = 0, bool isUseEndWord = false)
			{
				Result = arg;
				TotalWordCount = totalWordCount;
				CalcWordCount = calcWordCount;
				Time = time;
				IsUseEndWord = isUseEndWord;
			}

			public FindResult Result;

			public int TotalWordCount;

			public int CalcWordCount;

			public int Time;

			public bool IsUseEndWord;
		}

		public class PathObject
		{
			public string Title
			{
				get; private set;
			}

			public string ToolTip
			{
				get; private set;
			}

			public string Content
			{
				get; private set;
			}

			public bool IsEndWord
			{
				get; private set;
			}

			public PathObject(string _content, bool _isEndWord)
			{
				Content = _content;
				Title = _content;
				IsEndWord = _isEndWord;
				bool isEndWord = IsEndWord;
				if (isEndWord)
					ToolTip = "이 패스는 선택 할 수 있는 다음 패스가 없습니다.";
				else
					ToolTip = _content;
			}
		}
	}
}
