using AutoKkutuLib.Database;

namespace AutoKkutuLib.Node;

public abstract class NodeJob
{
	protected AbstractDatabaseConnection DbConnection { get; }

	protected NodeJob(AbstractDatabaseConnection dbConnection) => DbConnection = dbConnection;
}
