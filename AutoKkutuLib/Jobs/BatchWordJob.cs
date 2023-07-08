using AutoKkutuLib.Database;

namespace AutoKkutuLib.Word;

public abstract class BatchWordJob
{
	protected AbstractDatabaseConnection DbConnection { get; }

	public BatchWordJob(AbstractDatabaseConnection dbConnection) => DbConnection = dbConnection;

	public abstract WordCount Execute(string[] wordList);
}
