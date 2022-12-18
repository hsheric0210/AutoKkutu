using AutoKkutuLib.Database;

namespace AutoKkutuLib.Node;

public abstract class NodeJob
{
	protected readonly AbstractDatabaseConnection dbConnection;

	protected NodeJob(AbstractDatabaseConnection dbConnection) => this.dbConnection = dbConnection;
}
