namespace AutoKkutuLib.Database.Jobs.Word;

public abstract class BatchWordJob
{
	protected DbConnectionBase DbConnection { get; }

	public BatchWordJob(DbConnectionBase dbConnection) => DbConnection = dbConnection;

	public abstract WordCount Execute(IEnumerable<string> wordList);
}
