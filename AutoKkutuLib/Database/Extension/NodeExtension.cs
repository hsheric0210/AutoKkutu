using AutoKkutuLib.Constants;
using Dapper;
using Serilog;

namespace AutoKkutuLib.Database.Extension;

public static class NodeExtension
{
	#region Node addition
	public static bool AddNode(this AbstractDatabaseConnection connection, string node, NodeTypes nodeType) => connection.AddNode(node, nodeType.GetNodeTableName());

	public static bool AddNode(this AbstractDatabaseConnection connection, string node, string tableName)
	{
		if (connection == null)
			throw new ArgumentNullException(nameof(connection));
		if (string.IsNullOrWhiteSpace(node))
			throw new ArgumentNullException(nameof(node));

		if (string.IsNullOrWhiteSpace(tableName))
			tableName = DatabaseConstants.EndNodeIndexTableName;

		string nodeString;
		if (tableName.Equals(DatabaseConstants.KkutuWordIndexColumnName, StringComparison.Ordinal))
			nodeString = node[..2];
		else
			nodeString = node[0].ToString();

		if (connection.ExecuteScalar<int>($"SELECT COUNT(*) FROM {tableName} WHERE {DatabaseConstants.WordIndexColumnName} = @Node;", new
		{
			Node = nodeString
		}) > 0)
		{
			return false;
		}

		connection.Execute($"INSERT INTO {tableName}({DatabaseConstants.WordIndexColumnName}) VALUES(@Node)", new
		{
			Node = nodeString
		});
		return true;
	}
	#endregion

	#region Node deletion
	public static int DeleteNode(this AbstractDatabaseConnection connection, string node, NodeTypes nodeType) => connection.DeleteNode(node, nodeType.GetNodeTableName());

	public static int DeleteNode(this AbstractDatabaseConnection connection, string node, string tableName)
	{
		if (connection == null)
			throw new ArgumentNullException(nameof(connection));
		if (string.IsNullOrWhiteSpace(node))
			throw new ArgumentNullException(nameof(node));
		if (string.IsNullOrEmpty(tableName))
			throw new ArgumentException("Empty table name", nameof(tableName));

		return connection.Execute($"DELETE FROM {tableName} WHERE {DatabaseConstants.WordIndexColumnName} = @Node", new
		{
			Node = node
		});
	}
	#endregion

	#region Query node list
	public static ICollection<string> GetNodeList(this AbstractDatabaseConnection connection, NodeTypes nodeType) => connection.GetNodeList(nodeType.GetNodeTableName());

	public static ICollection<string> GetNodeList(this AbstractDatabaseConnection connection, string tableName)
	{
		if (connection == null)
			throw new ArgumentNullException(nameof(connection));

		List<string> result = connection.Query<string>($"SELECT {DatabaseConstants.WordIndexColumnName} FROM {tableName}").AsList();
		Log.Information("Found Total {0} nodes in {1}.", result.Count, tableName);
		return result;
	}
	#endregion

	#region NodeType to node table name conversion
	public static string GetNodeTableName(this NodeTypes nodeType) => nodeType switch
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
	#endregion
}
