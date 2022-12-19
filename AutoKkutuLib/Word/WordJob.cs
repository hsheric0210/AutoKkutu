using AutoKkutuLib.Database;
using AutoKkutuLib.Database.Extension;
using AutoKkutuLib.Game.Extension;
using AutoKkutuLib.Node;
using Serilog;

namespace AutoKkutuLib.Word;

public abstract class WordJob
{
	protected AbstractDatabaseConnection DbConnection { get; }

	public WordJob(AbstractDatabaseConnection dbConnection) => DbConnection = dbConnection;
}
