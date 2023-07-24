using MySqlConnector;
using AutoKkutuLib.MySql.Database.MySql.Query;
using AutoKkutuLib.Database.Sql;
using AutoKkutuLib.MySql.Properties;
using AutoKkutuLib.Database.Sql.Query;
using System.Text;

namespace AutoKkutuLib.Database.MySql;

public sealed class MySqlDbConnection : DbConnectionBase
{
	private QueryFactory query = null!; // It's always initialized by factory method
	public override QueryFactory Query => query;
	public override string DbType => "MySQL";

	private MySqlDbConnection(MySqlConnection connection) : base(connection) { }

	public override string GetWordPriorityFuncName() => "__AutoKkutu_Rearrange";

	public override string GetMissionWordPriorityFuncName() => "__AutoKkutu_RearrangeMission";

	public override string GetWordListColumnOptions()
	{
		var builder = new StringBuilder();
		builder.Append(DatabaseConstants.SequenceColumnName).Append(" INT NOT NULL AUTO_INCREMENT PRIMARY KEY, ");
		builder.Append(DatabaseConstants.WordColumnName).Append(" VARCHAR(256) UNIQUE NOT NULL, ");
		builder.Append(DatabaseConstants.WordIndexColumnName).Append(" CHAR(1) NOT NULL, ");
		builder.Append(DatabaseConstants.ReverseWordIndexColumnName).Append(" CHAR(1) NOT NULL, ");
		builder.Append(DatabaseConstants.KkutuWordIndexColumnName).Append(" VARCHAR(2) NOT NULL, ");
		builder.Append(DatabaseConstants.TypeColumnName).Append(" INT NOT NULL, ");
		builder.Append(DatabaseConstants.ThemeColumn1Name).Append(" BIGINT NOT NULL, ");
		builder.Append(DatabaseConstants.ThemeColumn2Name).Append(" BIGINT NOT NULL, ");
		builder.Append(DatabaseConstants.ThemeColumn3Name).Append(" BIGINT NOT NULL, ");
		builder.Append(DatabaseConstants.ThemeColumn4Name).Append(" BIGINT NOT NULL, ");
		builder.Append(DatabaseConstants.ChoseongColumnName).Append(" VARCHAR(256) NOT NULL, ");
		builder.Append(DatabaseConstants.MeaningColumnName).Append(" TEXT NOT NULL, ");
		builder.Append(DatabaseConstants.FlagsColumnName).Append(" INT NOT NULL");
		return builder.ToString();
	}

	public static MySqlDbConnection? Create(string connectionString)
	{
		try
		{
			var databaseNameIndex = connectionString.IndexOf("database", StringComparison.InvariantCultureIgnoreCase) + 9;
			var databaseNameIndexEnd = connectionString.IndexOf(';', databaseNameIndex) - databaseNameIndex;
			var databaseName = connectionString.Substring(databaseNameIndex, databaseNameIndexEnd);
			LibLogger.Info<MySqlDbConnection>("MySQL database name is {databaseName}.", databaseName);

			// Open the raw connection and wrap
			LibLogger.Info<MySqlDbConnection>("Opening database connection...");
			var nativeConnection = new MySqlConnection(connectionString);
			nativeConnection.Open();
			var connection = new MySqlDbConnection(nativeConnection);
			connection.query = new MySqlQueryFactory(connection, databaseName);

			// Execute initialization script
			var nameMap = new NameMapping();
			nameMap.Add("__MaxWordLength__", DatabaseConstants.MaxWordLength.ToString());
			nameMap.Add("__AutoKkutu_Rearrange__", connection.GetWordPriorityFuncName());
			nameMap.Add("__AutoKkutu_RearrangeMission__", connection.GetMissionWordPriorityFuncName());

			connection.TryExecute(nameMap.ApplyTo(Resources.Initialization));

			// Check the database tables
			connection.CheckTable();

			LibLogger.Info<MySqlDbConnection>("Successfully established database connection.");

			return connection;
		}
		catch (Exception ex)
		{
			LibLogger.Error<MySqlDbConnection>(ex, DatabaseConstants.ErrorConnect);
			DatabaseEvents.TriggerDatabaseError();
		}

		return null;
	}
}
