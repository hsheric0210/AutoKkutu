using AutoKkutu.Constants;
using AutoKkutu.Database;
using System.Collections.Generic;
using System.Threading;

namespace AutoKkutu.Modules.PathManager;
public interface IPathManager
{
	ICollection<string> AttackNodes { get; }
	AbstractDatabaseConnection DbConnection { get; }
	ICollection<string> EndNodes { get; }
	ICollection<string> InexistentPathList { get; }
	ICollection<string> KKTAttackNodes { get; }
	ICollection<string> KKTEndNodes { get; }
	ICollection<string> KkutuAttackNodes { get; }
	ICollection<string> KkutuEndNodes { get; }
	ICollection<string> NewPathList { get; }
	ReaderWriterLockSlim PathListLock { get; }
	ICollection<string> PreviousPath { get; }
	ICollection<string> ReverseAttackNodes { get; }
	ICollection<string> ReverseEndNodes { get; }
	ICollection<string> UnsupportedPathList { get; }

	void AddPreviousPath(string word);
	void AddToUnsupportedWord(string word, bool isNonexistent);
	bool CheckNodePresence(string? nodeType, string node, ICollection<string>? nodeList, WordFlags targetFlag, ref WordFlags flags, bool addIfInexistent = false);
	void UpdateNodeListsByWord(string word, ref WordFlags flags, ref int NewEndNode, ref int NewAttackNode);
	IList<PathObject> CreateQualifiedWordList(IList<PathObject> wordList);
	ICollection<string> GetEndNodeForMode(GameMode mode);
	WordFlags CalcWordFlags(string word);
	void ResetPreviousPath();
	string? UpdateDatabase();
	void LoadNodeLists();
}