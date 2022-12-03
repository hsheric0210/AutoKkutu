using AutoKkutu.Constants;
using AutoKkutu.Database;
using System.Collections.Generic;

namespace AutoKkutu.Modules.PathManager
{
	public interface IPathManager
	{
		ICollection<string>? AttackWordList
		{
			get;
		}
		ICollection<string>? EndWordList
		{
			get;
		}
		ICollection<string> InexistentPathList
		{
			get;
		}
		ICollection<string>? KKTAttackWordList
		{
			get;
		}
		ICollection<string>? KKTEndWordList
		{
			get;
		}
		ICollection<string>? KkutuAttackWordList
		{
			get;
		}
		ICollection<string>? KkutuEndWordList
		{
			get;
		}
		ICollection<string> NewPathList
		{
			get;
		}
		ICollection<string> PreviousPath
		{
			get;
		}
		ICollection<string>? ReverseAttackWordList
		{
			get;
		}
		ICollection<string>? ReverseEndWordList
		{
			get;
		}
		ICollection<string> UnsupportedPathList
		{
			get;
		}

		void AddPreviousPath(string word);
		void AddToUnsupportedWord(string word, bool isNonexistent);
		bool CheckNodePresence(string? nodeType, string item, ICollection<string>? nodeList, WordDbTypes theFlag, ref WordDbTypes flags, bool tryAdd = false);
		string? ConvertToPresentedWord(string path);
		IList<PathObject> CreateQualifiedWordList(IList<PathObject> wordList);
		void ResetPreviousPath();
		string? UpdateDatabase();
		void UpdateNodeLists(AbstractDatabaseConnection connection);
	}
}