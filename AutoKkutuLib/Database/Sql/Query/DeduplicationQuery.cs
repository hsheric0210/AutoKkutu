using Dapper;

namespace AutoKkutuLib.Database.Sql.Query;
public class DeduplicationQuery : SqlQuery<int>
{
	internal DeduplicationQuery(AbstractDatabaseConnection connection) : base(connection)
	{
	}

	// https://wiki.postgresql.org/wiki/Deleting_duplicates
	public override int Execute() => Connection.Execute($"DELETE FROM {DatabaseConstants.WordTableName} WHERE seq IN (SELECT seq FROM (SELECT seq, ROW_NUMBER() OVER w as rnum FROM {DatabaseConstants.WordTableName} WINDOW w AS (PARTITION BY word ORDER BY seq)) t WHERE t.rnum > 1);");
}
