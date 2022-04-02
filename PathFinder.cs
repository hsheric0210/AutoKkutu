using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AutoKkutu
{
	class PathFinder
	{
		private static readonly string LOGIN_INSTANCE_NAME = "PathFinder";

		public static List<string> EndWordList;

		public static List<PathObject> FinalList;

		public static List<string> PreviousPath = new List<string>();

		public static List<string> AutoDBUpdateList = new List<string>();

		public static EventHandler UpdatedPath;

		public static void Init()
		{
			try
			{
				EndWordList = DatabaseManager.GetEndWordList();
			}
			catch (Exception e)
			{
				ConsoleManager.Log(ConsoleManager.LogType.Error, $"Failed to Get End word :  {e.ToString()}", LOGIN_INSTANCE_NAME);
			}
		}

		public static void AutoDBUpdate(bool IsEnabled)
		{
			if (IsEnabled)
			{
				ConsoleManager.Log(ConsoleManager.LogType.Info, "Automatically update the DB based on last game.", LOGIN_INSTANCE_NAME);
				if (AutoDBUpdateList.Count == 0)
					ConsoleManager.Log(ConsoleManager.LogType.Info, "No such element in autoupdate list.", LOGIN_INSTANCE_NAME);
				else
				{
					ConsoleManager.Log(ConsoleManager.LogType.Info, string.Format("Get {0} elements from AutoDBUpdateList.", AutoDBUpdateList.Count), LOGIN_INSTANCE_NAME);
					foreach (string i in AutoDBUpdateList)
					{
						bool isendword = EndWordList.Contains(i.Last().ToString());
						try
						{
							ConsoleManager.Log(ConsoleManager.LogType.Info, "Check and add '" + i + "' into database.", LOGIN_INSTANCE_NAME);
							DatabaseManager.AddWord(i, isendword);
						}
						catch (AggregateException)
						{
							ConsoleManager.Log(ConsoleManager.LogType.Warning, "'" + i + "' has already included in database.", LOGIN_INSTANCE_NAME);
						}
						catch (Exception ex)
						{
							ConsoleManager.Log(ConsoleManager.LogType.Error, "Can't Add element to database : " + ex.ToString(), LOGIN_INSTANCE_NAME);
						}
					}
					AutoDBUpdateList = new List<string>();
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Automatic DB Update complete.", LOGIN_INSTANCE_NAME);
				}
			}
		}

		public static void AddPreviousPath(string word)
		{
			if (!string.IsNullOrWhiteSpace(word))
				PreviousPath.Add(word);
		}

		private static List<PathObject> QualifyList(List<PathObject> input)
		{
			var result = new List<PathObject>();
			foreach (PathObject o in input)
			{
				if (PreviousPath.Contains(o.Content))
					ConsoleManager.Log(ConsoleManager.LogType.Warning, "Except ' " + o.Content + " ' because including in PreviousPath List.", LOGIN_INSTANCE_NAME);
				else
					result.Add(o);
			}
			return result;
		}

		public static void FindPath(KkutuHandler.ResponsePresentedWord i, bool UseEndWord)
		{
			bool canSubstitution = i.CanSubstitution;
			if (canSubstitution)
				ConsoleManager.Log(ConsoleManager.LogType.Info, $"Finding path for {i.Content} ({i.Substitution}).", LOGIN_INSTANCE_NAME);
			else
				ConsoleManager.Log(ConsoleManager.LogType.Info, $"Finding path for {i.Content}.", LOGIN_INSTANCE_NAME);
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
				ConsoleManager.Log(ConsoleManager.LogType.Info, string.Format("Find {0} Word.", NormalWord.Count), LOGIN_INSTANCE_NAME);
				if (UseEndWord)
				{
					ConsoleManager.Log(ConsoleManager.LogType.Info, "Endword priority enabled.", LOGIN_INSTANCE_NAME);
					EndWord = DatabaseManager.FindWord(i, true);
					ConsoleManager.Log(ConsoleManager.LogType.Info, string.Format("Find {0} Word.", EndWord.Count), LOGIN_INSTANCE_NAME);
				}
			}
			catch (Exception e)
			{
				watch.Stop();
				ConsoleManager.Log(ConsoleManager.LogType.Error, "Failed to Find Path : " + e.ToString(), LOGIN_INSTANCE_NAME);
				if (UpdatedPath != null)
					UpdatedPath(null, new UpdatedPathEventArgs(FindResult.Error, 0, 0, 0, false));
			}
			QualifiedNormalList = QualifyList(NormalWord);
			if (UseEndWord)
			{
				QualifiedEndList = QualifyList(EndWord);
				if (QualifiedEndList.Count != 0)
				{
					if (QualifiedEndList.Count > 5)
						QualifiedEndList = QualifiedEndList.Take(5).ToList();
					if (QualifiedNormalList.Count > 25)
						QualifiedNormalList = QualifiedNormalList.Take(20).ToList();
					FinalList = QualifiedEndList.Concat(QualifiedNormalList).ToList();
				}
				else
				{
					if (QualifiedNormalList.Count == 0)
					{
						watch.Stop();
						ConsoleManager.Log(ConsoleManager.LogType.Warning, "Can't find any path.", LOGIN_INSTANCE_NAME);
						if (UpdatedPath != null)
							UpdatedPath(null, new UpdatedPathEventArgs(FindResult.None, NormalWord.Count, 0, Convert.ToInt32(watch.ElapsedMilliseconds), true));
						return;
					}

					if (QualifiedNormalList.Count > 20)
						QualifiedNormalList = QualifiedNormalList.Take(20).ToList();

					FinalList = QualifiedNormalList;
				}
			}
			else
			{
				if (QualifiedNormalList.Count == 0)
				{
					watch.Stop();
					ConsoleManager.Log(ConsoleManager.LogType.Warning, "Can't find any path.", LOGIN_INSTANCE_NAME);
					if (UpdatedPath != null)
						UpdatedPath(null, new UpdatedPathEventArgs(FindResult.None, NormalWord.Count, 0, Convert.ToInt32(watch.ElapsedMilliseconds), false));
					return;
				}
				if (QualifiedNormalList.Count > 20)
					QualifiedNormalList = QualifiedNormalList.Take(20).ToList();
				FinalList = QualifiedNormalList;
			}
			watch.Stop();
			ConsoleManager.Log(ConsoleManager.LogType.Warning, string.Format("Total {0} Path Ready. ({1}ms)", FinalList.Count, watch.ElapsedMilliseconds), LOGIN_INSTANCE_NAME);
			if (UpdatedPath != null)
				UpdatedPath(null, new UpdatedPathEventArgs(FindResult.Normal, NormalWord.Count, FinalList.Count, Convert.ToInt32(watch.ElapsedMilliseconds), UseEndWord));
		}

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
				if (IsEndWord = _isEndWord)
					ToolTip = "이 패스는 선택 할 수 있는 다음 패스가 없습니다.";
				else
					ToolTip = _content;
			}
		}
	}
}
