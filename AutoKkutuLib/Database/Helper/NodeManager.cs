using AutoKkutuLib.Extension;
using System.Globalization;

namespace AutoKkutuLib.Database.Helper;

public class NodeManager
{
	public DbConnectionBase DbConnection
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
	public NodeManager(DbConnectionBase dbConnection)
	{
		DbConnection = dbConnection;

		try
		{
			LoadNodeLists(dbConnection);
		}
		catch (Exception ex)
		{
			LibLogger.Error<NodeManager>(ex, I18n.PathFinder_Init_Error);
			DatabaseEvents.TriggerDatabaseError();
			throw;
		}
	}

	public void LoadNodeLists(DbConnectionBase connection)
	{
		var query = connection.Query.ListNode();
		AttackNodes = query.Execute(DatabaseConstants.AttackNodeIndexTableName);
		EndNodes = query.Execute(DatabaseConstants.EndNodeIndexTableName);
		ReverseAttackNodes = query.Execute(DatabaseConstants.ReverseAttackNodeIndexTableName);
		ReverseEndNodes = query.Execute(DatabaseConstants.ReverseEndNodeIndexTableName);
		KkutuAttackNodes = query.Execute(DatabaseConstants.KkutuAttackNodeIndexTableName);
		KkutuEndNodes = query.Execute(DatabaseConstants.KkutuEndNodeIndexTableName);
		KKTAttackNodes = query.Execute(DatabaseConstants.KKTAttackNodeIndexTableName);
		KKTEndNodes = query.Execute(DatabaseConstants.KKTEndNodeIndexTableName);
	}
	#endregion

	public ICollection<string> GetEndNodeForMode(GameMode mode) => mode switch
	{
		GameMode.FirstAndLast => ReverseEndNodes,
		GameMode.Kkutu => KkutuEndNodes,
		_ => EndNodes,
	};


	#region Node list access/update
	/// <summary>
	/// Calculate the word flags by node lists
	/// (nodeLists -> word)
	/// </summary>
	public WordFlags GetWordNodeFlags(string word, WordFlags flags = WordFlags.None)
	{
		if (string.IsNullOrEmpty(word))
			throw new ArgumentException(null, nameof(word));

		var wordLength = word.Length;

		// 한방 노드
		GetWordNodeFlagsInternal(word.GetLaFTailNode(), EndNodes, WordFlags.EndWord, ref flags);

		// 공격 노드
		GetWordNodeFlagsInternal(word.GetLaFTailNode(), AttackNodes, WordFlags.AttackWord, ref flags);

		// 앞말잇기 한방 노드
		GetWordNodeFlagsInternal(word.GetFaLTailNode(), ReverseEndNodes, WordFlags.ReverseEndWord, ref flags);

		// 앞말잇기 공격 노드
		GetWordNodeFlagsInternal(word.GetFaLTailNode(), ReverseAttackNodes, WordFlags.ReverseAttackWord, ref flags);


		if (flags.HasFlag(WordFlags.KKT2) || flags.HasFlag(WordFlags.KKT3))
		{
			// 쿵쿵따 한방 노드
			GetWordNodeFlagsInternal(word.GetLaFTailNode(), KKTEndNodes, WordFlags.KKTEndWord, ref flags);

			// 쿵쿵따 공격 노드
			GetWordNodeFlagsInternal(word.GetLaFTailNode(), KKTAttackNodes, WordFlags.KKTAttackWord, ref flags);
		}

		if (wordLength > 2 && wordLength % 2 == 1)
		{
			// 가운뎃말잇기 한방 노드
			GetWordNodeFlagsInternal(word.GetMaFTailNode(), EndNodes, WordFlags.MiddleEndWord, ref flags);

			// 가운뎃말잇기 공격 노드
			GetWordNodeFlagsInternal(word.GetMaFTailNode(), AttackNodes, WordFlags.MiddleAttackWord, ref flags);
		}

		if (wordLength >= 4)
		{
			// 끄투 한방 노드
			GetWordNodeFlagsInternal(word.GetKkutuTailNode(), KkutuEndNodes, WordFlags.KkutuEndWord, ref flags);

			// 끄투 공격 노드
			GetWordNodeFlagsInternal(word.GetKkutuTailNode(), KkutuAttackNodes, WordFlags.KkutuAttackWord, ref flags);
		}

		return flags;
	}

	/// <summary>
	/// nodeList -> node
	/// </summary>
	private static void GetWordNodeFlagsInternal(string node, ICollection<string> nodeList, WordFlags targetFlag, ref WordFlags flagsOut)
	{
		if (string.IsNullOrWhiteSpace(node))
			return;

		if (nodeList.Contains(node))
			flagsOut |= targetFlag;
	}

	/// <summary>
	/// Update node lists by word (word -> nodeLists); Only called when adding words to database
	/// </summary>
	public WordCount UpdateNodeListsByWord(string word, ref WordFlags flagsInOut)
	{
		if (string.IsNullOrEmpty(word))
			throw new ArgumentException(null, nameof(word));

		var count = new WordCount();

		// 한방 노드
		UpdateNodeListsByWordInternal(word.GetLaFTailNode(), EndNodes, WordFlags.EndWord, flagsInOut, ref count);

		// 공격 노드
		UpdateNodeListsByWordInternal(word.GetLaFTailNode(), AttackNodes, WordFlags.AttackWord, flagsInOut, ref count);

		// 앞말잇기 한방 노드
		UpdateNodeListsByWordInternal(word.GetFaLTailNode(), ReverseEndNodes, WordFlags.ReverseEndWord, flagsInOut, ref count);

		// 앞말잇기 공격 노드
		UpdateNodeListsByWordInternal(word.GetFaLTailNode(), ReverseAttackNodes, WordFlags.ReverseAttackWord, flagsInOut, ref count);

		var wordLength = word.Length;
		if (word.Length == 2)
			flagsInOut |= WordFlags.KKT2;
		else if (wordLength > 2)
		{
			// 끄투 한방 노드
			UpdateNodeListsByWordInternal(word.GetKkutuTailNode(), KkutuEndNodes, WordFlags.KkutuEndWord, flagsInOut, ref count);

			// 끄투 공격 노드
			UpdateNodeListsByWordInternal(word.GetKkutuTailNode(), KkutuAttackNodes, WordFlags.KkutuAttackWord, flagsInOut, ref count);

			if (wordLength == 3)
			{
				flagsInOut |= WordFlags.KKT3;

				// 쿵쿵따 한방 노드
				UpdateNodeListsByWordInternal(word.GetLaFTailNode(), KKTEndNodes, WordFlags.EndWord, flagsInOut, ref count);

				// 쿵쿵따 공격 노드
				UpdateNodeListsByWordInternal(word.GetLaFTailNode(), KKTAttackNodes, WordFlags.AttackWord, flagsInOut, ref count);
			}

			if (wordLength % 2 == 1)
			{
				// 가운뎃말잇기 한방 노드
				UpdateNodeListsByWordInternal(word.GetMaFTailNode(), EndNodes, WordFlags.MiddleEndWord, flagsInOut, ref count);

				// 가운뎃말잇기 공격 노드
				UpdateNodeListsByWordInternal(word.GetMaFTailNode(), AttackNodes, WordFlags.MiddleAttackWord, flagsInOut, ref count);
			}
		}
		return count;
	}

	/// <summary>
	/// node -> nodeList
	/// </summary>
	private void UpdateNodeListsByWordInternal(string node, ICollection<string> nodeList, WordFlags targetFlag, WordFlags flagsIn, ref WordCount count)
	{
		if (string.IsNullOrWhiteSpace(node))
			return;

		if (!nodeList.Contains(node) && flagsIn.HasFlag(targetFlag))
		{
			nodeList.Add(node);
			NodeTypes? nodeType = null;
			switch (targetFlag)
			{
				case WordFlags.EndWord:
				case WordFlags.MiddleEndWord:
					nodeType = NodeTypes.EndWord;
					break;
				case WordFlags.AttackWord:
				case WordFlags.MiddleAttackWord:
					nodeType = NodeTypes.AttackWord;
					break;
				case WordFlags.ReverseEndWord:
					nodeType = NodeTypes.ReverseEndWord;
					break;
				case WordFlags.ReverseAttackWord:
					nodeType = NodeTypes.ReverseAttackWord;
					break;
				case WordFlags.KkutuEndWord:
					nodeType = NodeTypes.KkutuEndWord;
					break;
				case WordFlags.KkutuAttackWord:
					nodeType = NodeTypes.KkutuAttackWord;
					break;
				case WordFlags.KKTEndWord:
					nodeType = NodeTypes.KKTEndWord;
					break;
				case WordFlags.KKTAttackWord:
					nodeType = NodeTypes.KKTAttackWord;
					break;
			}
			if (nodeType != null)
				DbConnection.Query.AddNode((NodeTypes)nodeType).Execute(node);
			count.Increment(targetFlag, 1);
			LibLogger.Info<NodeManager>(string.Format(CultureInfo.CurrentCulture, I18n.PathFinder_AddNode, targetFlag, node));
		}
	}
	#endregion
}
