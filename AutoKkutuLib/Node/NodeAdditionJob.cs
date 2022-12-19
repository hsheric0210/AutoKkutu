using AutoKkutuLib.Database;
using AutoKkutuLib.Database.Extension;
using Serilog;

namespace AutoKkutuLib.Node;
public sealed class NodeAdditionJob : NodeJob
{
	private readonly NodeTypes nodeTypes;

	public NodeCount Result { get; private set; }

	public NodeAdditionJob(AbstractDatabaseConnection dbConnection, NodeTypes nodeTypes) : base(dbConnection) => this.nodeTypes = nodeTypes;

	public void Add(string node)
	{
		if (string.IsNullOrWhiteSpace(node))
			return;

		try
		{
			AddNodeInternal(node, nodeTypes, NodeTypes.EndWord); // 한방 단어
			AddNodeInternal(node, nodeTypes, NodeTypes.AttackWord); // 공격 단어
			AddNodeInternal(node, nodeTypes, NodeTypes.ReverseEndWord); // 앞말잇기 한방 단어
			AddNodeInternal(node, nodeTypes, NodeTypes.ReverseAttackWord); // 앞말잇기 공격 단어
			AddNodeInternal(node, nodeTypes, NodeTypes.KkutuEndWord); // 끄투 한방 단어
			AddNodeInternal(node, nodeTypes, NodeTypes.KkutuAttackWord); // 끄투 공격 단어
			AddNodeInternal(node, nodeTypes, NodeTypes.KKTEndWord); // 쿵쿵따 한방 단어
			AddNodeInternal(node, nodeTypes, NodeTypes.KKTAttackWord); // 쿵쿵따 공격 단어
		}
		catch(Exception ex)
		{
			Log.Error(ex, "Exception on node addition: {node} for {flags}'", node, nodeTypes);
			Result.IncrementError();
		}
	}

	private void AddNodeInternal(string node, NodeTypes nodeTypes, NodeTypes targetNodeType) => Result.Increment(targetNodeType, nodeTypes.HasFlag(targetNodeType) ? Convert.ToInt32(DbConnection.AddNode(node, targetNodeType)) : 0);
}
