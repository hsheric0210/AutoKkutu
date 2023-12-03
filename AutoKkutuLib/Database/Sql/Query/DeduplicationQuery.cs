using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class DeduplicationQuery : SqlQuery<int>
{
	public DeduplicationQuery(DbConnectionBase connection) : base(connection)
	{
	}

	// https://wiki.postgresql.org/wiki/Deleting_duplicates
	public override int Execute()
	{
		LibLogger.Debug<DeduplicationQuery>($"Deduplicating the table {DatabaseConstants.WordTableName}.");
		return Connection.Execute($"DELETE FROM {DatabaseConstants.WordTableName} WHERE {DatabaseConstants.SequenceColumnName} IN (SELECT {DatabaseConstants.SequenceColumnName} FROM (SELECT {DatabaseConstants.SequenceColumnName}, ROW_NUMBER() OVER w as rnum FROM {DatabaseConstants.WordTableName} WINDOW w AS (PARTITION BY {DatabaseConstants.WordColumnName} ORDER BY {DatabaseConstants.SequenceColumnName})) t WHERE t.rnum > 1);");
	}
}
