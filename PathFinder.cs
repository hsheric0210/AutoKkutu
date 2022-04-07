﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AutoKkutu
{
	// TODO: 미션 감지 및 단어 선호도 조정 기능 추가
	class PathFinder
	{
		private static readonly string LOGIN_INSTANCE_NAME = "PathFinder";

		public static List<string> EndWordList;

		public static List<PathObject> FinalList;

		public static List<string> PreviousPath = new List<string>();

		public static List<string> WrongPathList = new List<string>();

		public static List<string> NewPathList = new List<string>();

		public static EventHandler UpdatedPath;

		public static Config CurrentConfig;
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

		public static void UpdateConfig(Config newConfig)
		{
			CurrentConfig = newConfig;
		}

		public static void AutoDBUpdate()
		{
			if (!CurrentConfig.AutoDBUpdate)
				return;

			ConsoleManager.Log(ConsoleManager.LogType.Info, "Automatically update the DB based on last game.", LOGIN_INSTANCE_NAME);
			if (NewPathList.Count + WrongPathList.Count == 0)
				ConsoleManager.Log(ConsoleManager.LogType.Warning, "No such element in autoupdate list.", LOGIN_INSTANCE_NAME);
			else
			{
				ConsoleManager.Log(ConsoleManager.LogType.Info, string.Format("Get {0} elements from NewPathList.", NewPathList.Count), LOGIN_INSTANCE_NAME);
				foreach (string word in NewPathList)
				{
					bool isEndWord = EndWordList.Contains(word.Last().ToString());
					try
					{
						ConsoleManager.Log(ConsoleManager.LogType.Info, $"Check and add '{word}' into database.", LOGIN_INSTANCE_NAME);
						if (DatabaseManager.AddWord(word, isEndWord))
							ConsoleManager.Log(ConsoleManager.LogType.Info, $"Added '{word}' into database.", LOGIN_INSTANCE_NAME);
					}
					catch (Exception ex)
					{
						ConsoleManager.Log(ConsoleManager.LogType.Error, $"Can't add '{word}' to database : " + ex.ToString(), LOGIN_INSTANCE_NAME);
					}
				}
				NewPathList = new List<string>();

				ConsoleManager.Log(ConsoleManager.LogType.Info, string.Format("Get {0} elements from WrongPathList.", WrongPathList.Count), LOGIN_INSTANCE_NAME);
				foreach (string word in WrongPathList)
				{
					try
					{
						ConsoleManager.Log(ConsoleManager.LogType.Info, $"Delete '{word}' from database.", LOGIN_INSTANCE_NAME);
						DatabaseManager.DeleteWord(word);
					}
					catch (Exception ex)
					{
						ConsoleManager.Log(ConsoleManager.LogType.Error, $"Can't delete '{word}' from database : " + ex.ToString(), LOGIN_INSTANCE_NAME);
					}
				}
				WrongPathList = new List<string>();

				ConsoleManager.Log(ConsoleManager.LogType.Info, "Automatic DB Update complete.", LOGIN_INSTANCE_NAME);
			}
		}

		public static void AddPreviousPath(string word)
		{
			if (!string.IsNullOrWhiteSpace(word))
				PreviousPath.Add(word);
		}

		public static void AddToWrongWord(string word)
		{
			if (!string.IsNullOrWhiteSpace(word))
				WrongPathList.Add(word);
		}

		private static List<PathObject> QualifyList(List<PathObject> input)
		{
			var result = new List<PathObject>();
			foreach (PathObject o in input)
			{
				if (WrongPathList.Contains(o.Content))
					ConsoleManager.Log(ConsoleManager.LogType.Warning, "Excluded '" + o.Content + "' because it is wrong word.", LOGIN_INSTANCE_NAME);
				else if (!CurrentConfig.ReturnMode && PreviousPath.Contains(o.Content))
					ConsoleManager.Log(ConsoleManager.LogType.Warning, "Excluded '" + o.Content + "' because it is previously used.", LOGIN_INSTANCE_NAME);
				else
					result.Add(o);
			}
			return result;
		}

		public static void FindPath(CommonHandler.ResponsePresentedWord i, string missionChar, int wordPreference, bool useEndWord, bool reverseMode)
		{
			if (i.CanSubstitution)
				ConsoleManager.Log(ConsoleManager.LogType.Info, $"Finding path for {i.Content} ({i.Substitution}).", LOGIN_INSTANCE_NAME);
			else
				ConsoleManager.Log(ConsoleManager.LogType.Info, $"Finding path for {i.Content}.", LOGIN_INSTANCE_NAME);
			var watch = new Stopwatch();
			watch.Start();
			FinalList = new List<PathObject>();
			var WordList = new List<PathObject>();
			var QualifiedNormalList = new List<PathObject>();
			try
			{
				if (wordPreference == Config.WORDPREFERENCE_BY_LENGTH_INDEX)
				{
					WordList = DatabaseManager.FindWord(i, missionChar, useEndWord ? 2 : 0, reverseMode);
					ConsoleManager.Log(ConsoleManager.LogType.Info, string.Format("Found {0} words.", WordList.Count), LOGIN_INSTANCE_NAME);
				}
				else
				{
					if (useEndWord)
					{
						WordList = DatabaseManager.FindWord(i, missionChar, 1, reverseMode);
						ConsoleManager.Log(ConsoleManager.LogType.Info, string.Format("Find {0} words (EndWord inclued).", WordList.Count), LOGIN_INSTANCE_NAME);
					}
					else
					{
						WordList = DatabaseManager.FindWord(i, missionChar, 0, reverseMode);
						ConsoleManager.Log(ConsoleManager.LogType.Info, string.Format("Found {0} words (EndWord excluded).", WordList.Count), LOGIN_INSTANCE_NAME);
					}
					// TODO: Attack word search here
					// AttackWord = DatabaseManager.FindWord(i, 3); // 3 for only attack words
					// ConsoleManager.Log(ConsoleManager.LogType.Info, string.Format("Found {0} attack words.", NormalWord.Count), LOGIN_INSTANCE_NAME);
				}
			}
			catch (Exception e)
			{
				watch.Stop();
				ConsoleManager.Log(ConsoleManager.LogType.Error, "Failed to Find Path : " + e.ToString(), LOGIN_INSTANCE_NAME);
				if (UpdatedPath != null)
					UpdatedPath(null, new UpdatedPathEventArgs(FindResult.Error, 0, 0, 0, false));
			}
			QualifiedNormalList = QualifyList(WordList);

			if (QualifiedNormalList.Count == 0)
			{
				watch.Stop();
				ConsoleManager.Log(ConsoleManager.LogType.Warning, "Can't find any path.", LOGIN_INSTANCE_NAME);
				if (UpdatedPath != null)
					UpdatedPath(null, new UpdatedPathEventArgs(FindResult.None, WordList.Count, 0, Convert.ToInt32(watch.ElapsedMilliseconds), false));
				return;
			}
			if (QualifiedNormalList.Count > 20)
				QualifiedNormalList = QualifiedNormalList.Take(20).ToList();

			FinalList = QualifiedNormalList;
			watch.Stop();
			ConsoleManager.Log(ConsoleManager.LogType.Info, string.Format("Total {0} words are ready. ({1}ms)", FinalList.Count, watch.ElapsedMilliseconds), LOGIN_INSTANCE_NAME);
			if (UpdatedPath != null)
				UpdatedPath(null, new UpdatedPathEventArgs(FindResult.Normal, WordList.Count, FinalList.Count, Convert.ToInt32(watch.ElapsedMilliseconds), useEndWord));
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
					ToolTip = "이 단어는 한방 단어로, 이을 수 있는 다음 단어가 없습니다.";
				else
					ToolTip = _content;
			}
		}
	}
}
