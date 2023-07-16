namespace AutoKkutuLib.Database.Jobs.Word;

public abstract class BatchWordJob
{
	protected AbstractDatabaseConnection DbConnection { get; }

	public BatchWordJob(AbstractDatabaseConnection dbConnection) => DbConnection = dbConnection;

	public abstract WordCount Execute(string[] wordList);
}
