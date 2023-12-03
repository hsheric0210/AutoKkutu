using AutoKkutuLib.Database.Jobs.Node;
using AutoKkutuLib.Database.Sql;
using Dapper;
using System.Text;

namespace AutoKkutuLib.Database.Jobs.DbCheck;
internal class InvalidEndNodeCheck : DbCheckSubtaskBase
{
	private int removedEnd = 0, removedReverseEnd = 0, removedKkutuEnd = 0, removedKKTEnd = 0;

	public InvalidEndNodeCheck(DbConnectionBase db) : base(db, "Remove Invalid End Nodes")
	{
	}

	private int RemoveInvalidEndNode(NodeTypes nodeType, string additionalCondition = "", bool reverse = false)
	{
		// Issue #82
		var deljob = new NodeDeletionJob(Db, nodeType);
		var invalidList = new List<string>();
		var tableName = nodeType.ToNodeTableName();
		foreach (var endnode in Db.Query<string>($"SELECT ({DatabaseConstants.WordIndexColumnName}) FROM {tableName} ORDER BY({DatabaseConstants.WordIndexColumnName}) DESC"))
		{
			var builder = new StringBuilder($"SELECT COUNT(*) FROM {DatabaseConstants.WordTableName} WHERE {DatabaseConstants.WordColumnName} LIKE @Pattern");
			if (!string.IsNullOrEmpty(additionalCondition))
				builder.Append(" AND ").Append(additionalCondition);
			builder.Append(';');
			var count = Db.ExecuteScalar<int>(builder.ToString(), new { Pattern = reverse ? ('%' + endnode) : (endnode + '%') });
			if (count > 0)
			{
				LibLogger.Debug(CheckName, "Table {0} end node index {1} is invalid because {2} suitable word(s) are found from the database.", tableName, endnode, count);
				invalidList.Add(endnode);
			}
		}

		foreach (var invalid in invalidList)
			deljob.Execute(invalid);
		return invalidList.Count;
	}

	protected override int RunCore()
	{
		using var transaction = Db.BeginTransaction();
		removedEnd = RemoveInvalidEndNode(NodeTypes.EndWord);
		removedReverseEnd = RemoveInvalidEndNode(NodeTypes.ReverseEndWord, reverse: true);
		removedKkutuEnd = RemoveInvalidEndNode(NodeTypes.KkutuEndWord, $"LENGTH({DatabaseConstants.KkutuWordIndexColumnName}) > 3");
		removedKKTEnd = RemoveInvalidEndNode(NodeTypes.KKTEndWord, $"(LENGTH({DatabaseConstants.WordIndexColumnName}) = 2 OR LENGTH({DatabaseConstants.WordIndexColumnName}) = 3)"); // TODO: Replace with 'flags' column read and bitmask 'KKT3' verification
		transaction.Commit();
		return removedEnd + removedReverseEnd + removedKkutuEnd + removedKKTEnd;
	}

	public override void BriefResult()
	{
		LibLogger.Info(CheckName, "Removed {0} invalid end nodes.", removedEnd);
		LibLogger.Info(CheckName, "Removed {0} invalid reverse end nodes.", removedReverseEnd);
		LibLogger.Info(CheckName, "Removed {0} invalid kkutu end nodes.", removedKkutuEnd);
		LibLogger.Info(CheckName, "Removed {0} invalid kungkungtta end nodes.", removedKKTEnd);
	}
}
