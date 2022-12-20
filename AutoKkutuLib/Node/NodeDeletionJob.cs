using AutoKkutuLib.Database;
using AutoKkutuLib.Database.Relational;
using Serilog;

namespace AutoKkutuLib.Node;
public sealed class NodeDeletionJob : NodeJob
{
	private readonly NodeTypes nodeTypes;

	public NodeCount Result { get; private set; }

	public NodeDeletionJob(AbstractDatabaseConnection dbConnection, NodeTypes nodeTypes) : base(dbConnection) => this.nodeTypes = nodeTypes;

	public void Delete(string node)
	{
		if (string.IsNullOrWhiteSpace(node))
			return;

		try
		{
			DeleteNodeInternal(node, nodeTypes, NodeTypes.EndWord); // 한방 단어
			DeleteNodeInternal(node, nodeTypes, NodeTypes.AttackWord); // 공격 단어
			DeleteNodeInternal(node, nodeTypes, NodeTypes.ReverseEndWord); // 앞말잇기 한방 단어
			DeleteNodeInternal(node, nodeTypes, NodeTypes.ReverseAttackWord); // 앞말잇기 공격 단어
			DeleteNodeInternal(node, nodeTypes, NodeTypes.KkutuEndWord); // 끄투 한방 단어
			DeleteNodeInternal(node, nodeTypes, NodeTypes.KkutuAttackWord); // 끄투 공격 단어
			DeleteNodeInternal(node, nodeTypes, NodeTypes.KKTEndWord); // 쿵쿵따 한방 단어
			DeleteNodeInternal(node, nodeTypes, NodeTypes.KKTAttackWord); // 쿵쿵따 공격 단어
		}
		catch(Exception ex)
		{
			Log.Error(ex, "Exception on node deletion: {node} for {flags}'", node, nodeTypes);
			Result.IncrementError();
		}
	}

	private void DeleteNodeInternal(string node, NodeTypes nodeTypes, NodeTypes targetNodeType) => Result.Increment(targetNodeType, nodeTypes.HasFlag(targetNodeType) ? DbConnection.DeleteNode(node, targetNodeType.ToNodeTableName()) : 0);
}
