using AutoKkutuLib.Database;

namespace AutoKkutuLib.Word;

public abstract class WordJob
{
	protected AbstractDatabaseConnection DbConnection { get; }

	public WordJob(AbstractDatabaseConnection dbConnection) => DbConnection = dbConnection;
}
