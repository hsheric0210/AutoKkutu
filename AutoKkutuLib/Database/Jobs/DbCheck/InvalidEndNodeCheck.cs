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

	private int RemoveInvalidEndNode(string wordListColumnName, NodeTypes nodeType, string additionalCondition = "")
	{
		// Issue #82
		var deljob = new NodeDeletionJob(Db, nodeType);
		var invalidList = new List<string>();
		foreach (var endnode in Db.Query<string>($"SELECT ({DatabaseConstants.WordIndexColumnName}) FROM {nodeType.ToNodeTableName()} ORDER BY({DatabaseConstants.WordIndexColumnName}) DESC"))
		{
			var builder = new StringBuilder($"SELECT COUNT(*) FROM {DatabaseConstants.WordTableName} WHERE {wordListColumnName} = @Node");
			if (!string.IsNullOrEmpty(additionalCondition))
				builder.Append(" AND ").Append(additionalCondition);
			builder.Append(';');

			var count = Db.ExecuteScalar<int>(builder.ToString(), new { Node = endnode });
			if (count > 0)
			{
				LibLogger.Info<DbCheckJob>("End node {0} is invalid because {1} suitable words are found from the database.", endnode, count);
				invalidList.Add(endnode);
			}
		}

		foreach (var invalid in invalidList)
			deljob.Execute(invalid);
		return invalidList.Count;
	}

	protected override int RunCore()
	{
		removedEnd = RemoveInvalidEndNode(DatabaseConstants.WordIndexColumnName, NodeTypes.EndWord);
		removedReverseEnd = RemoveInvalidEndNode(DatabaseConstants.ReverseWordIndexColumnName, NodeTypes.ReverseEndWord);
		removedKkutuEnd = RemoveInvalidEndNode(DatabaseConstants.KkutuWordIndexColumnName, NodeTypes.KkutuEndWord, $"LENGTH({DatabaseConstants.KkutuWordIndexColumnName}) > 3");
		removedKKTEnd = RemoveInvalidEndNode(DatabaseConstants.WordIndexColumnName, NodeTypes.KKTEndWord, $"(LENGTH({DatabaseConstants.WordIndexColumnName}) = 2 OR LENGTH({DatabaseConstants.WordIndexColumnName}) = 3)"); // TODO: Replace with 'flags' column read and bitmask 'KKT3' verification
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
