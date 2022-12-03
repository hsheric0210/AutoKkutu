using AutoKkutu.Constants;
using AutoKkutu.Database;
using System.Collections.Generic;

namespace AutoKkutu.Modules.PathManager
{
	public interface IPathManager
	{
		ICollection<string>? AttackNodes
		{
			get;
		}
		ICollection<string>? EndNodes
		{
			get;
		}
		ICollection<string> InexistentPathList
		{
			get;
		}
		ICollection<string>? KKTAttackNodes
		{
			get;
		}
		ICollection<string>? KKTEndNodes
		{
			get;
		}
		ICollection<string>? KkutuAttackNodes
		{
			get;
		}
		ICollection<string>? KkutuEndNodes
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
		ICollection<string>? ReverseAttackNodes
		{
			get;
		}
		ICollection<string>? ReverseEndNodes
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
		ICollection<string> GetEndNodeForMode(GameMode mode);
		void ResetPreviousPath();
		string? UpdateDatabase();
		void UpdateNodeLists(AbstractDatabaseConnection connection);
	}
}