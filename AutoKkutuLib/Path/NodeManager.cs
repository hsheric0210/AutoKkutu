using AutoKkutuLib.Database;
using AutoKkutuLib.Database.Extension;
using AutoKkutuLib.Extension;
using Serilog;
using System.Globalization;

namespace AutoKkutuLib.Path;

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
		CheckWordNode(word.GetLaFTailNode(), EndNodes, WordFlags.EndWord, ref flags);

		// 공격 노드
		CheckWordNode(word.GetLaFTailNode(), AttackNodes, WordFlags.AttackWord, ref flags);

		// 앞말잇기 한방 노드
		CheckWordNode(word.GetFaLTailNode(), ReverseEndNodes, WordFlags.ReverseEndWord, ref flags);

		// 앞말잇기 공격 노드
		CheckWordNode(word.GetFaLTailNode(), ReverseAttackNodes, WordFlags.ReverseAttackWord, ref flags);

		var wordLength = word.Length;
		if (wordLength == 2)
			flags |= WordFlags.KKT2;
		if (wordLength > 2)
		{
			// 끄투 한방 노드
			CheckWordNode(word.GetKkutuTailNode(), KkutuEndNodes, WordFlags.KkutuEndWord, ref flags);

			// 끄투 공격 노드
			CheckWordNode(word.GetKkutuTailNode(), KkutuAttackNodes, WordFlags.KkutuAttackWord, ref flags);

			if (wordLength == 3)
			{
				flags |= WordFlags.KKT3;

				// 쿵쿵따 한방 노드
				CheckWordNode(word.GetLaFTailNode(), KKTEndNodes, WordFlags.KKTEndWord, ref flags);

				// 쿵쿵따 공격 노드
				CheckWordNode(word.GetLaFTailNode(), KKTAttackNodes, WordFlags.KKTAttackWord, ref flags);
			}

			if (wordLength % 2 == 1)
			{
				// 가운뎃말잇기 한방 노드
				CheckWordNode(word.GetMaFTailNode(), EndNodes, WordFlags.MiddleEndWord, ref flags);

				// 가운뎃말잇기 공격 노드
				CheckWordNode(word.GetMaFTailNode(), AttackNodes, WordFlags.MiddleAttackWord, ref flags);
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
		NewEndNode += Convert.ToInt32(UpdateNodeListByWord("end", word.GetLaFTailNode(), EndNodes, WordFlags.EndWord, ref flags));

		// 공격 노드
		NewAttackNode += Convert.ToInt32(UpdateNodeListByWord("attack", word.GetLaFTailNode(), AttackNodes, WordFlags.AttackWord, ref flags));

		// 앞말잇기 한방 노드
		NewEndNode += Convert.ToInt32(UpdateNodeListByWord("reverse end", word.GetFaLTailNode(), ReverseEndNodes, WordFlags.ReverseEndWord, ref flags));

		// 앞말잇기 공격 노드
		NewAttackNode += Convert.ToInt32(UpdateNodeListByWord("reverse attack", word.GetFaLTailNode(), ReverseAttackNodes, WordFlags.ReverseAttackWord, ref flags));

		var wordLength = word.Length;
		if (word.Length == 2)
			flags |= WordFlags.KKT2;
		else if (wordLength > 2)
		{
			// 끄투 한방 노드
			NewEndNode += Convert.ToInt32(UpdateNodeListByWord("kkutu end", word.GetKkutuTailNode(), KkutuEndNodes, WordFlags.KkutuEndWord, ref flags));
			NewEndNode++;

			// 끄투 공격 노드
			NewAttackNode += Convert.ToInt32(UpdateNodeListByWord("kkutu attack", word.GetKkutuTailNode(), KkutuAttackNodes, WordFlags.KkutuAttackWord, ref flags));

			if (wordLength == 3)
			{
				flags |= WordFlags.KKT3;

				// 쿵쿵따 한방 노드
				NewEndNode += Convert.ToInt32(UpdateNodeListByWord("kungkungtta end", word.GetLaFTailNode(), KKTEndNodes, WordFlags.EndWord, ref flags));

				// 쿵쿵따 공격 노드
				NewAttackNode += Convert.ToInt32(UpdateNodeListByWord("kungkungtta attack", word.GetLaFTailNode(), KKTAttackNodes, WordFlags.AttackWord, ref flags));
			}

			if (wordLength % 2 == 1)
			{
				// 가운뎃말잇기 한방 노드
				NewEndNode += Convert.ToInt32(UpdateNodeListByWord("middle end", word.GetMaFTailNode(), EndNodes, WordFlags.MiddleEndWord, ref flags));

				// 가운뎃말잇기 공격 노드
				NewAttackNode += Convert.ToInt32(UpdateNodeListByWord("middle attack", word.GetMaFTailNode(), AttackNodes, WordFlags.MiddleAttackWord, ref flags));
			}
		}
	}

	/// <summary>
	/// nodeList -> node
	/// </summary>
	private static void CheckWordNode(string node, ICollection<string> nodeList, WordFlags targetFlag, ref WordFlags flags)
	{
		if (string.IsNullOrWhiteSpace(node))
			return;

		if (nodeList.Contains(node))
			flags |= targetFlag;
	}

	/// <summary>
	/// node -> nodeList
	/// </summary>
	private static bool UpdateNodeListByWord(string nodeType, string node, ICollection<string> nodeList, WordFlags targetFlag, ref WordFlags flags)
	{
		if (string.IsNullOrWhiteSpace(node) || string.IsNullOrEmpty(nodeType))
			return false;

		if (!nodeList.Contains(node) && flags.HasFlag(targetFlag))
		{
			nodeList.Add(node);
			Log.Information(string.Format(CultureInfo.CurrentCulture, I18n.PathFinder_AddNode, nodeType, node));
			return true;
		}
		return false;
	}
	#endregion

	#region Node addition
	/// <summary>
	/// 데이터베이스에 노드를 추가합니다.
	/// </summary>
	/// <param name="node">추가할 노드</param>
	/// <param name="types">추가할 노드의 속성들</param>
	/// <returns>데이터베이스에 추가된 노드의 총 갯수</returns>
	public int AddNode(string node, NodeTypes types)
	{
		if (DbConnection is null || string.IsNullOrWhiteSpace(node))
			return 0;

		var affected = 0;

		affected += AddNodeInternal(node, types, NodeTypes.EndWord); // 한방 단어
		affected += AddNodeInternal(node, types, NodeTypes.AttackWord); // 공격 단어
		affected += AddNodeInternal(node, types, NodeTypes.ReverseEndWord); // 앞말잇기 한방 단어
		affected += AddNodeInternal(node, types, NodeTypes.ReverseAttackWord); // 앞말잇기 공격 단어
		affected += AddNodeInternal(node, types, NodeTypes.KkutuEndWord); // 끄투 한방 단어
		affected += AddNodeInternal(node, types, NodeTypes.KkutuAttackWord); // 끄투 공격 단어
		affected += AddNodeInternal(node, types, NodeTypes.KKTEndWord); // 쿵쿵따 한방 단어
		affected += AddNodeInternal(node, types, NodeTypes.KKTAttackWord); // 쿵쿵따 공격 단어

		return affected;
	}

	private int AddNodeInternal(string node, NodeTypes nodeTypes, NodeTypes targetNodeType) => nodeTypes.HasFlag(targetNodeType) ? Convert.ToInt32(DbConnection.AddNode(node, targetNodeType)) : 0;
	#endregion

	#region Node deletion
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

		var affected = 0;

		affected += DeleteNodeInternal(node, types, NodeTypes.EndWord); // 한방 단어
		affected += DeleteNodeInternal(node, types, NodeTypes.AttackWord); // 공격 단어
		affected += DeleteNodeInternal(node, types, NodeTypes.ReverseEndWord); // 앞말잇기 한방 단어
		affected += DeleteNodeInternal(node, types, NodeTypes.ReverseAttackWord); // 앞말잇기 공격 단어
		affected += DeleteNodeInternal(node, types, NodeTypes.KkutuEndWord); // 끄투 한방 단어
		affected += DeleteNodeInternal(node, types, NodeTypes.KkutuAttackWord); // 끄투 공격 단어
		affected += DeleteNodeInternal(node, types, NodeTypes.KKTEndWord); // 쿵쿵따 한방 단어
		affected += DeleteNodeInternal(node, types, NodeTypes.KKTAttackWord); // 쿵쿵따 공격 단어

		return affected;
	}

	private int DeleteNodeInternal(string node, NodeTypes nodeTypes, NodeTypes targetNodeType) => nodeTypes.HasFlag(targetNodeType) ? DbConnection.DeleteNode(node, targetNodeType.ToNodeTableName()) : 0;
	#endregion
}
