using AutoKkutuLib.Constants;
using AutoKkutuLib.Database;
using AutoKkutuLib.Database.Extension;
using AutoKkutuLib.Modules;
using AutoKkutuLib.Utils;
using AutoKkutuLib.Utils.Extension;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace AutoKkutuLib.Modules.Path;

public class PathManager : IPathManager
{
	public AbstractDatabaseConnection DbConnection
	{
		get;
	}

	#region Node lists
	public ICollection<string> AttackNodes
	{
		get; private set;
	} = null!;

	public ICollection<string> EndNodes
	{
		get; private set;
	} = null!;

	public ICollection<string> KKTAttackNodes
	{
		get; private set;
	} = null!;

	public ICollection<string> KKTEndNodes
	{
		get; private set;
	} = null!;

	public ICollection<string> KkutuAttackNodes
	{
		get; private set;
	} = null!;

	public ICollection<string> KkutuEndNodes
	{
		get; private set;
	} = null!;

	public ICollection<string> ReverseAttackNodes
	{
		get; private set;
	} = null!;

	public ICollection<string> ReverseEndNodes
	{
		get; private set;
	} = null!;
	#endregion


	#region Constructor & Initialization
	public PathManager(AbstractDatabaseConnection dbConnection)
	{
		DbConnection = dbConnection;

		try
		{
			LoadNodeLists();
		}
		catch (Exception ex)
		{
			Log.Error(ex, I18n.PathFinder_Init_Error);
			DatabaseEvents.TriggerDatabaseError();
			throw;
		}
	}

	public void LoadNodeLists()
	{
		AttackNodes = DbConnection.GetNodeList(DatabaseConstants.AttackNodeIndexTableName);
		EndNodes = DbConnection.GetNodeList(DatabaseConstants.EndNodeIndexTableName);
		ReverseAttackNodes = DbConnection.GetNodeList(DatabaseConstants.ReverseAttackNodeIndexTableName);
		ReverseEndNodes = DbConnection.GetNodeList(DatabaseConstants.ReverseEndNodeIndexTableName);
		KkutuAttackNodes = DbConnection.GetNodeList(DatabaseConstants.KkutuAttackNodeIndexTableName);
		KkutuEndNodes = DbConnection.GetNodeList(DatabaseConstants.KkutuEndNodeIndexTableName);
		KKTAttackNodes = DbConnection.GetNodeList(DatabaseConstants.KKTAttackNodeIndexTableName);
		KKTEndNodes = DbConnection.GetNodeList(DatabaseConstants.KKTEndNodeIndexTableName);
	}
	#endregion

	#region Special path list push/pop
	public void AddPreviousPath(string word)
	{
		if (!string.IsNullOrWhiteSpace(word))
			PreviousPath.Add(word);
	}

	public void AddToUnsupportedWord(string word, bool isNonexistent)
	{
		if (!string.IsNullOrWhiteSpace(word))
		{
			try
			{
				PathListLock.EnterWriteLock();
				UnsupportedPathList.Add(word);
				if (isNonexistent)
					InexistentPathList.Add(word);
			}
			finally
			{
				PathListLock.ExitWriteLock();
			}
		}
	}

	public void ResetPreviousPath()
	{
		if (PreviousPath.Count > 0)
			PreviousPath.Clear();
	}
	#endregion

	#region Node list access/update
	/// <summary>
	/// Calculate the word flags by node lists
	/// (nodeLists -> word)
	/// </summary>
	public WordFlags CalcWordFlags(string word)
	{
		if (string.IsNullOrEmpty(word))
			throw new ArgumentException(null, nameof(word));

		WordFlags flags = WordFlags.None;

		// 한방 노드
		CheckNodePresence(null, word.GetLaFTailNode(), EndNodes, WordFlags.EndWord, ref flags);

		// 공격 노드
		CheckNodePresence(null, word.GetLaFTailNode(), AttackNodes, WordFlags.AttackWord, ref flags);

		// 앞말잇기 한방 노드
		CheckNodePresence(null, word.GetFaLTailNode(), ReverseEndNodes, WordFlags.ReverseEndWord, ref flags);

		// 앞말잇기 공격 노드
		CheckNodePresence(null, word.GetFaLTailNode(), ReverseAttackNodes, WordFlags.ReverseAttackWord, ref flags);

		var wordLength = word.Length;
		if (wordLength == 2)
			flags |= WordFlags.KKT2;
		if (wordLength > 2)
		{
			// 끄투 한방 노드
			CheckNodePresence(null, word.GetKkutuTailNode(), KkutuEndNodes, WordFlags.KkutuEndWord, ref flags);

			// 끄투 공격 노드
			CheckNodePresence(null, word.GetKkutuTailNode(), KkutuAttackNodes, WordFlags.KkutuAttackWord, ref flags);

			if (wordLength == 3)
			{
				flags |= WordFlags.KKT3;

				// 쿵쿵따 한방 노드
				CheckNodePresence(null, word.GetLaFTailNode(), KKTEndNodes, WordFlags.KKTEndWord, ref flags);

				// 쿵쿵따 공격 노드
				CheckNodePresence(null, word.GetLaFTailNode(), KKTAttackNodes, WordFlags.KKTAttackWord, ref flags);
			}

			if (wordLength % 2 == 1)
			{
				// 가운뎃말잇기 한방 노드
				CheckNodePresence(null, word.GetMaFTailNode(), EndNodes, WordFlags.MiddleEndWord, ref flags);

				// 가운뎃말잇기 공격 노드
				CheckNodePresence(null, word.GetMaFTailNode(), AttackNodes, WordFlags.MiddleAttackWord, ref flags);
			}
		}
		return flags;
	}

	/// <summary>
	/// Update node lists by word
	/// (word -> nodeLists)
	/// </summary>
	public void UpdateNodeListsByWord(string word, ref WordFlags flags, ref int NewEndNode, ref int NewAttackNode)
	{
		if (string.IsNullOrEmpty(word))
			throw new ArgumentException(null, nameof(word));

		// 한방 노드
		NewEndNode += Convert.ToInt32(CheckNodePresence("end", word.GetLaFTailNode(), EndNodes, WordFlags.EndWord, ref flags, true));

		// 공격 노드
		NewAttackNode += Convert.ToInt32(CheckNodePresence("attack", word.GetLaFTailNode(), AttackNodes, WordFlags.AttackWord, ref flags, true));

		// 앞말잇기 한방 노드
		NewEndNode += Convert.ToInt32(CheckNodePresence("reverse end", word.GetFaLTailNode(), ReverseEndNodes, WordFlags.ReverseEndWord, ref flags, true));

		// 앞말잇기 공격 노드
		NewAttackNode += Convert.ToInt32(CheckNodePresence("reverse attack", word.GetFaLTailNode(), ReverseAttackNodes, WordFlags.ReverseAttackWord, ref flags, true));

		var wordLength = word.Length;
		if (word.Length == 2)
			flags |= WordFlags.KKT2;
		else if (wordLength > 2)
		{
			// 끄투 한방 노드
			NewEndNode += Convert.ToInt32(CheckNodePresence("kkutu end", word.GetKkutuTailNode(), KkutuEndNodes, WordFlags.KkutuEndWord, ref flags, true));
			NewEndNode++;

			// 끄투 공격 노드
			NewAttackNode += Convert.ToInt32(CheckNodePresence("kkutu attack", word.GetKkutuTailNode(), KkutuAttackNodes, WordFlags.KkutuAttackWord, ref flags, true));

			if (wordLength == 3)
			{
				flags |= WordFlags.KKT3;

				// 쿵쿵따 한방 노드
				NewEndNode += Convert.ToInt32(CheckNodePresence("kungkungtta end", word.GetLaFTailNode(), KKTEndNodes, WordFlags.EndWord, ref flags, true));

				// 쿵쿵따 공격 노드
				NewAttackNode += Convert.ToInt32(CheckNodePresence("kungkungtta attack", word.GetLaFTailNode(), KKTAttackNodes, WordFlags.AttackWord, ref flags, true));
			}

			if (wordLength % 2 == 1)
			{
				// 가운뎃말잇기 한방 노드
				NewEndNode += Convert.ToInt32(CheckNodePresence("middle end", word.GetMaFTailNode(), EndNodes, WordFlags.MiddleEndWord, ref flags, true));

				// 가운뎃말잇기 공격 노드
				NewAttackNode += Convert.ToInt32(CheckNodePresence("middle attack", word.GetMaFTailNode(), AttackNodes, WordFlags.MiddleAttackWord, ref flags, true));
			}
		}
	}

	// TODO: Remove 'noteType' parameter, replace it with enum instead.
	public bool CheckNodePresence(string? nodeType, string node, ICollection<string>? nodeList, WordFlags targetFlag, ref WordFlags flags, bool addIfInexistent = false)
	{
		if (addIfInexistent && string.IsNullOrEmpty(nodeType) || string.IsNullOrWhiteSpace(node) || nodeList == null)
			return false;

		var exists = nodeList.Contains(node);
		if (exists)
			flags |= targetFlag;
		else if (addIfInexistent && flags.HasFlag(targetFlag))
		{
			nodeList.Add(node);
			Log.Information(string.Format(CultureInfo.CurrentCulture, I18n.PathFinder_AddNode, nodeType, node));
			return true;
		}
		return false;
	}
	#endregion


	public ICollection<string> GetEndNodeForMode(GameMode mode) => mode switch
	{
		GameMode.FirstAndLast => ReverseEndNodes,
		GameMode.Kkutu => KkutuEndNodes,
		_ => EndNodes,
	};

	#region Single word processing (internal)
	private enum AddWordResultType
	{
		Success,
		Duplicate,
		Failed
	}

	private sealed record AddWordResult(AddWordResultType ResultType, int NewEndNode = 0, int NewAttackNode = 0);

	private AddWordResult AddSingleWord(string word)
	{
		try
		{
			int newEnd = 0, newAttack = 0;
			WordFlags flags = WordFlags.None;
			UpdateNodeListsByWord(word, ref flags, ref newEnd, ref newAttack);

			Log.Information("Adding {word} into database... (flags: {flags})", word, flags);
			if (DbConnection.AddWord(word, flags))
			{
				Log.Information("Successfully Add {word} to database!", word);
				return new AddWordResult(AddWordResultType.Success, newEnd, newAttack);
			}
			else
			{
				Log.Warning("{word} already exists on database.", word);
				return new AddWordResult(AddWordResultType.Duplicate, newEnd, newAttack);
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to add {word} to the database.", word);
			return new AddWordResult(AddWordResultType.Failed);
		}
	}

	private bool RemoveSingleWord(string word)
	{
		if (string.IsNullOrWhiteSpace(word))
			return false;

		try
		{
			return DbConnection.DeleteWord(word) > 0;
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to remove {word} from the database.", word);
			return false;
		}
	}
	#endregion

	#region Batch processing
	private struct BatchResult
	{
		public int SuccessCount;
		public int DuplicateCount;
		public int FailedCount;
		public int NewEndNode;
		public int NewAttackNode;
	}

	public void BatchAddWord(string[] wordList, BatchJobOptions batchOptions)
	{
		if (wordList == null)
			throw new ArgumentNullException(nameof(wordList));

		var onlineVerify = batchOptions.HasFlag(BatchJobOptions.VerifyBeforeAdd);
		if (onlineVerify && string.IsNullOrWhiteSpace(JSEvaluator.EvaluateJS("document.getElementById('dict-output').style")))
			// FIXME: Replace with event
			// MessageBox.Show("끄투 사전 창을 감지하지 못했습니다.\n끄투 사전 창을 키십시오.", _namespace, MessageBoxButton.OK, MessageBoxImage.Warning);
			return;

		new DatabaseImportEventArgs("Batch Add Words").TriggerDatabaseImportStart();

		Log.Information("{0} elements queued.", wordList.Length);

		Task.Run(() =>
		{
			BatchResult result = PerformBatchAddWord(wordList, onlineVerify);

			var message = $"{result.SuccessCount} succeed / {result.NewEndNode} new end nodes / {result.NewAttackNode} new attack nodes / {result.DuplicateCount} duplicated / {result.FailedCount} failed";
			Log.Information("Database Operation Complete: {0}", message);
			new DatabaseImportEventArgs("Batch Add Word", message).TriggerDatabaseImportDone();
		});
	}

	private BatchResult PerformBatchAddWord(string[] wordlist, bool onlineVerify)
	{
		var result = new BatchResult();
		foreach (var word in wordlist)
		{
			if (string.IsNullOrWhiteSpace(word))
				continue;

			// Check word length
			if (word.Length <= 1)
			{
				Log.Warning("{word} is too short to add!", word);
				result.FailedCount++;
				continue;
			}

			if (!onlineVerify || word.VerifyWordOnline())
			{
				var singleResult = AddSingleWord(word);
				switch (singleResult.ResultType)
				{
					case AddWordResultType.Success:
						result.SuccessCount++;
						break;

					case AddWordResultType.Duplicate:
						result.DuplicateCount++;
						break;

					default:
						result.FailedCount++;
						break;
				}

				result.NewEndNode += singleResult.NewEndNode;
				result.NewAttackNode += singleResult.NewAttackNode;
			}
		}

		return result;
	}

	public void BatchAddNode(string content, bool remove, NodeTypes type)
	{
		if (DbConnection == null || string.IsNullOrWhiteSpace(content))
			return;

		var NodeList = content.Trim().Split(Environment.NewLine.ToCharArray());

		var SuccessCount = 0;
		var DuplicateCount = 0;
		var FailedCount = 0;

		new DatabaseImportEventArgs(remove ? "Batch Remove Node" : "Batch Add Node").TriggerDatabaseImportStart();

		Log.Information("{0} elements queued.", NodeList.Length);
		foreach (var node in NodeList)
		{
			if (string.IsNullOrWhiteSpace(node))
				continue;

			try
			{
				if (remove)
					SuccessCount += DeleteNode(node, type);
				else if (AddNode(node, type))
				{
					Log.Information("Successfully add node {node}!", node[0]);
					SuccessCount++;
				}
				else
				{
					Log.Warning("{node} already exists.", node[0]);
					DuplicateCount++;
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to add node {node}!", node[0]);
				FailedCount++;
			}
		}

		var message = $"{SuccessCount} succeed / {DuplicateCount} duplicated / {FailedCount} failed";
		Log.Information("Database Operation Complete: {0}", message);
		new DatabaseImportEventArgs(remove ? "Batch Remove Node" : "Batch Add Node", message).TriggerDatabaseImportDone();
	}

	public void BatchRemoveWord(string[] wordlist)
	{
		if (wordlist == null)
			throw new ArgumentNullException(nameof(wordlist));

		new DatabaseImportEventArgs("Batch Remove Word").TriggerDatabaseImportStart();

		Log.Information("{0} elements queued.", wordlist.Length);

		Task.Run(() =>
		{
			int SuccessCount = 0, FailedCount = 0;
			foreach (var word in wordlist)
			{
				if (RemoveSingleWord(word))
					SuccessCount++;
				else
					FailedCount++;
			}

			var message = $"{SuccessCount} deleted / {FailedCount} failed";
			Log.Information("Batch remove operation complete: {0}", message);
			new DatabaseImportEventArgs("Batch Remove Word", message).TriggerDatabaseImportDone();

			// FIXME: Replace with event
			// MessageBox.Show($"성공적으로 작업을 수행했습니다. \n{message}", _namespace, MessageBoxButton.OK, MessageBoxImage.Exclamation);
		});
	}
	#endregion

	#region Node addition/removal
	/// <summary>
	/// 데이터베이스에 노드를 추가합니다.
	/// </summary>
	/// <param name="node">추가할 노드</param>
	/// <param name="types">추가할 노드의 속성들</param>
	/// <returns>데이터베이스에 추가된 노드의 총 갯수</returns>
	public bool AddNode(string node, NodeTypes types)
	{
		if (DbConnection == null || string.IsNullOrWhiteSpace(node))
			return false;

		var result = false;

		// 한방 단어
		result |= types.HasFlag(NodeTypes.EndWord) && DbConnection.AddNode(node, DatabaseConstants.EndNodeIndexTableName);

		// 공격 단어
		result |= types.HasFlag(NodeTypes.AttackWord) && DbConnection.AddNode(node, DatabaseConstants.AttackNodeIndexTableName);

		// 앞말잇기 한방 단어
		result |= types.HasFlag(NodeTypes.ReverseEndWord) && DbConnection.AddNode(node, DatabaseConstants.ReverseEndNodeIndexTableName);

		// 앞말잇기 공격 단어
		result |= types.HasFlag(NodeTypes.ReverseAttackWord) && DbConnection.AddNode(node, DatabaseConstants.ReverseAttackNodeIndexTableName);

		// 끄투 한방 단어
		result |= types.HasFlag(NodeTypes.KkutuEndWord) && DbConnection.AddNode(node, DatabaseConstants.KkutuEndNodeIndexTableName);

		// 끄투 공격 단어
		result |= types.HasFlag(NodeTypes.KkutuAttackWord) && DbConnection.AddNode(node, DatabaseConstants.KkutuAttackNodeIndexTableName);

		// 쿵쿵따 한방 단어
		result |= types.HasFlag(NodeTypes.KKTEndWord) && DbConnection.AddNode(node, DatabaseConstants.KKTEndNodeIndexTableName);

		// 쿵쿵따 공격 단어
		result |= types.HasFlag(NodeTypes.KKTAttackWord) && DbConnection.AddNode(node, DatabaseConstants.KKTAttackNodeIndexTableName);

		return result;
	}

	/// <summary>
	/// 데이터베이스에서 노드를 삭제합니다.
	/// </summary>
	/// <param name="node">삭제할 노드</param>
	/// <param name="types">삭제할 노드의 속성들</param>
	/// <returns>데이터베이스에서 삭제된 노드의 총 갯수</returns>
	public int DeleteNode(string node, NodeTypes types)
	{
		if (DbConnection == null || string.IsNullOrWhiteSpace(node))
			return -1;

		var count = 0;

		// 한방 단어
		if (types.HasFlag(NodeTypes.EndWord))
			count += DbConnection.DeleteNode(node, DatabaseConstants.EndNodeIndexTableName);

		// 공격 단어
		if (types.HasFlag(NodeTypes.AttackWord))
			count += DbConnection.DeleteNode(node, DatabaseConstants.AttackNodeIndexTableName);

		// 앞말잇기 한방 단어
		if (types.HasFlag(NodeTypes.ReverseEndWord))
			count += DbConnection.DeleteNode(node, DatabaseConstants.ReverseEndNodeIndexTableName);

		// 앞말잇기 공격 단어
		if (types.HasFlag(NodeTypes.ReverseAttackWord))
			count += DbConnection.DeleteNode(node, DatabaseConstants.ReverseAttackNodeIndexTableName);

		// 끄투 한방 단어
		if (types.HasFlag(NodeTypes.KkutuEndWord))
			count += DbConnection.DeleteNode(node, DatabaseConstants.KkutuEndNodeIndexTableName);

		// 끄투 공격 단어
		if (types.HasFlag(NodeTypes.KkutuAttackWord))
			count += DbConnection.DeleteNode(node, DatabaseConstants.KkutuAttackNodeIndexTableName);

		// 쿵쿵따 한방 단어
		if (types.HasFlag(NodeTypes.KKTEndWord))
			count += DbConnection.DeleteNode(node, DatabaseConstants.KKTEndNodeIndexTableName);

		// 쿵쿵따 공격 단어
		if (types.HasFlag(NodeTypes.KKTAttackWord))
			count += DbConnection.DeleteNode(node, DatabaseConstants.KKTAttackNodeIndexTableName);

		return count;
	}
	#endregion
}
