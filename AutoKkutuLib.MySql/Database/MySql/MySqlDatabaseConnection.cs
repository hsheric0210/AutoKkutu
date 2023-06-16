using MySqlConnector;
using AutoKkutuLib.MySql.Database.MySql.Query;
using Serilog;
using AutoKkutuLib.Database.Sql;
using AutoKkutuLib.MySql.Properties;
using AutoKkutuLib.Database.Sql.Query;

namespace AutoKkutuLib.Database.MySql;

public sealed class MySqlDatabaseConnection : AbstractDatabaseConnection
{
	private QueryFactory query = null!; // It's always initialized by factory method
	public override QueryFactory Query => query;
	public override string DbType => "MySQL";

	private MySqlDatabaseConnection(MySqlConnection connection) : base(connection) { }

	public override string GetWordPriorityFuncName() => "__AutoKkutu_Rearrange";

	public override string GetMissionWordPriorityFuncName() => "__AutoKkutu_RearrangeMission";

	public override string GetWordListColumnOptions() => $"{DatabaseConstants.SequenceColumnName} INT NOT NULL AUTO_INCREMENT PRIMARY KEY, {DatabaseConstants.WordColumnName} VARCHAR(256) UNIQUE NOT NULL, {DatabaseConstants.WordIndexColumnName} CHAR(1) NOT NULL, {DatabaseConstants.ReverseWordIndexColumnName} CHAR(1) NOT NULL, {DatabaseConstants.KkutuWordIndexColumnName} VARCHAR(2) NOT NULL, {DatabaseConstants.FlagsColumnName} SMALLINT NOT NULL";

	public static MySqlDatabaseConnection? Create(string connectionString)
	{
		try
		{
			var databaseNameIndex = connectionString.IndexOf("database", StringComparison.InvariantCultureIgnoreCase) + 9;
			var databaseNameIndexEnd = connectionString.IndexOf(';', databaseNameIndex) - databaseNameIndex;
			var databaseName = connectionString.Substring(databaseNameIndex, databaseNameIndexEnd);
			Log.Information("MySQL database name is {databaseName}.", databaseName);

			// Open the raw connection and wrap
			Log.Information("Opening database connection...");
			var nativeConnection = new MySqlConnection(connectionString);
			nativeConnection.Open();
			var connection = new MySqlDatabaseConnection(nativeConnection);
			connection.query = new MySqlQueryFactory(connection, databaseName);

			// Execute initialization script
			var nameMap = new NameMapping();
			nameMap.Add("__MaxWordLength__", DatabaseConstants.MaxWordLength.ToString());
			nameMap.Add("__AutoKkutu_Rearrange__", connection.GetWordPriorityFuncName());
			nameMap.Add("__AutoKkutu_RearrangeMission__", connection.GetWordPriorityFuncName());

			connection.TryExecute(nameMap.ApplyTo(Resources.Initialization));

			// Check the database tables
			connection.CheckTable();

			Log.Information("Successfully established database connection.");

			return connection;
		}
		catch (Exception ex)
		{
			Log.Error(ex, DatabaseConstants.ErrorConnect);
			DatabaseEvents.TriggerDatabaseError();
		}

		return null;
	}
}
