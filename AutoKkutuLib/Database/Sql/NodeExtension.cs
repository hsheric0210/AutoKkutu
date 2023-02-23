namespace AutoKkutuLib.Database.Sql;

public static class NodeExtension
{
	public static string ToNodeTableName(this NodeTypes nodeType) => nodeType switch
	{
		NodeTypes.EndWord => DatabaseConstants.EndNodeIndexTableName,
		NodeTypes.AttackWord => DatabaseConstants.AttackNodeIndexTableName,
		NodeTypes.ReverseEndWord => DatabaseConstants.ReverseEndNodeIndexTableName,
		NodeTypes.ReverseAttackWord => DatabaseConstants.ReverseAttackNodeIndexTableName,
		NodeTypes.KkutuEndWord => DatabaseConstants.KkutuEndNodeIndexTableName,
		NodeTypes.KkutuAttackWord => DatabaseConstants.KkutuAttackNodeIndexTableName,
		NodeTypes.KKTEndWord => DatabaseConstants.KKTEndNodeIndexTableName,
		NodeTypes.KKTAttackWord => DatabaseConstants.KKTAttackNodeIndexTableName,
		_ => throw new ArgumentException("Unsuppored node type: " + nodeType, nameof(nodeType))
	};
}
