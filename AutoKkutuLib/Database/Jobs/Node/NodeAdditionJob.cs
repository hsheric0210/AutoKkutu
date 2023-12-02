namespace AutoKkutuLib.Database.Jobs.Node;
public sealed class NodeAdditionJob : NodeJob
{
	private readonly NodeTypes nodeTypes;

	public NodeCount Result { get; private set; }

	public NodeAdditionJob(DbConnectionBase dbConnection, NodeTypes nodeTypes) : base(dbConnection) => this.nodeTypes = nodeTypes;

	public override void Execute(string node)
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
		catch (Exception ex)
		{
			LibLogger.Error< NodeAdditionJob>(ex, "Exception on node addition: {node} for {flags}'", node, nodeTypes);
			Result.IncrementError();
		}
	}

	private void AddNodeInternal(string node, NodeTypes nodeTypes, NodeTypes targetNodeType) => Result.Increment(targetNodeType, nodeTypes.HasFlag(targetNodeType) ? Convert.ToInt32(DbConnection.Query.AddNode(targetNodeType).Execute(node)) : 0);
}
