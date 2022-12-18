using AutoKkutuLib.Database;
using AutoKkutuLib.Database.Extension;
using AutoKkutuLib.Extension;
using Serilog;
using System.Globalization;

namespace AutoKkutuLib.Node;

public class NodeManager
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
	public NodeManager(AbstractDatabaseConnection dbConnection)
	{
		DbConnection = dbConnection;

		try
		{
			LoadNodeLists(dbConnection);
		}
		catch (Exception ex)
		{
			Log.Error(ex, I18n.PathFinder_Init_Error);
			DatabaseEvents.TriggerDatabaseError();
			throw;
		}
	}

	public void LoadNodeLists(AbstractDatabaseConnection connection)
	{
		AttackNodes = connection.GetNodeList(DatabaseConstants.AttackNodeIndexTableName);
		EndNodes = connection.GetNodeList(DatabaseConstants.EndNodeIndexTableName);
		ReverseAttackNodes = connection.GetNodeList(DatabaseConstants.ReverseAttackNodeIndexTableName);
		ReverseEndNodes = connection.GetNodeList(DatabaseConstants.ReverseEndNodeIndexTableName);
		KkutuAttackNodes = connection.GetNodeList(DatabaseConstants.KkutuAttackNodeIndexTableName);
		KkutuEndNodes = connection.GetNodeList(DatabaseConstants.KkutuEndNodeIndexTableName);
		KKTAttackNodes = connection.GetNodeList(DatabaseConstants.KKTAttackNodeIndexTableName);
		KKTEndNodes = connection.GetNodeList(DatabaseConstants.KKTEndNodeIndexTableName);
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
	public WordFlags CalcWordFlags(string word)
	{
		if (string.IsNullOrEmpty(word))
			throw new ArgumentException(null, nameof(word));

		WordFlags flags = WordFlags.None;

		// 한방 노드
		CalcWordFlagsInternal(word.GetLaFTailNode(), EndNodes, WordFlags.EndWord, ref flags);

		// 공격 노드
		CalcWordFlagsInternal(word.GetLaFTailNode(), AttackNodes, WordFlags.AttackWord, ref flags);

		// 앞말잇기 한방 노드
		CalcWordFlagsInternal(word.GetFaLTailNode(), ReverseEndNodes, WordFlags.ReverseEndWord, ref flags);

		// 앞말잇기 공격 노드
		CalcWordFlagsInternal(word.GetFaLTailNode(), ReverseAttackNodes, WordFlags.ReverseAttackWord, ref flags);

		var wordLength = word.Length;
		if (wordLength == 2)
			flags |= WordFlags.KKT2;
		if (wordLength > 2)
		{
			// 끄투 한방 노드
			CalcWordFlagsInternal(word.GetKkutuTailNode(), KkutuEndNodes, WordFlags.KkutuEndWord, ref flags);

			// 끄투 공격 노드
			CalcWordFlagsInternal(word.GetKkutuTailNode(), KkutuAttackNodes, WordFlags.KkutuAttackWord, ref flags);

			if (wordLength == 3)
			{
				flags |= WordFlags.KKT3;

				// 쿵쿵따 한방 노드
				CalcWordFlagsInternal(word.GetLaFTailNode(), KKTEndNodes, WordFlags.KKTEndWord, ref flags);

				// 쿵쿵따 공격 노드
				CalcWordFlagsInternal(word.GetLaFTailNode(), KKTAttackNodes, WordFlags.KKTAttackWord, ref flags);
			}

			if (wordLength % 2 == 1)
			{
				// 가운뎃말잇기 한방 노드
				CalcWordFlagsInternal(word.GetMaFTailNode(), EndNodes, WordFlags.MiddleEndWord, ref flags);

				// 가운뎃말잇기 공격 노드
				CalcWordFlagsInternal(word.GetMaFTailNode(), AttackNodes, WordFlags.MiddleAttackWord, ref flags);
			}
		}
		return flags;
	}

	/// <summary>
	/// nodeList -> node
	/// </summary>
	private static void CalcWordFlagsInternal(string node, ICollection<string> nodeList, WordFlags targetFlag, ref WordFlags flagsOut)
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
	private static void UpdateNodeListsByWordInternal(string node, ICollection<string> nodeList, WordFlags targetFlag, WordFlags flagsIn, ref WordCount count)
	{
		if (string.IsNullOrWhiteSpace(node))
			return;

		if (!nodeList.Contains(node) && flagsIn.HasFlag(targetFlag))
		{
			nodeList.Add(node);
			count.Increment(targetFlag, 1);
			Log.Information(string.Format(CultureInfo.CurrentCulture, I18n.PathFinder_AddNode, targetFlag, node));
		}
	}
	#endregion
}
