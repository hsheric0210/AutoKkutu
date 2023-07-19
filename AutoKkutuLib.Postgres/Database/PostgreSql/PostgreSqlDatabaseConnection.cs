using AutoKkutuLib.Database.Sql;
using AutoKkutuLib.Database.Sql.Query;
using AutoKkutuLib.Postgres.Database.PostgreSql.Query;
using AutoKkutuLib.Postgres.Properties;
using Npgsql;

namespace AutoKkutuLib.Database.PostgreSql;

public sealed class PostgreSqlDatabaseConnection : AbstractDatabaseConnection
{
	private QueryFactory query = null!; // It's always initialized by factory method
	public override QueryFactory Query => query;
	public override string DbType => "PostgreSQL";

	private PostgreSqlDatabaseConnection(NpgsqlConnection connection) : base(connection) { }

	public override string GetWordPriorityFuncName() => "__AutoKkutu_Rearrange";

	public override string GetMissionWordPriorityFuncName() => "__AutoKkutu_RearrangeMission";

	public override string GetWordListColumnOptions() => $"{DatabaseConstants.SequenceColumnName} SERIAL PRIMARY KEY, {DatabaseConstants.WordColumnName} CHAR VARYING(256) UNIQUE NOT NULL, {DatabaseConstants.WordIndexColumnName} CHAR(1) NOT NULL, {DatabaseConstants.ReverseWordIndexColumnName} CHAR(1) NOT NULL, {DatabaseConstants.KkutuWordIndexColumnName} VARCHAR(2) NOT NULL, {DatabaseConstants.FlagsColumnName} SMALLINT NOT NULL";

	public static PostgreSqlDatabaseConnection? Create(string connectionString)
	{
		try
		{
			// Open the connection
			LibLogger.Info<PostgreSqlDatabaseConnection>("Opening database connection...");
			var nativeConnection = new NpgsqlConnection(connectionString);
			nativeConnection.Open();
			var connection = new PostgreSqlDatabaseConnection(nativeConnection);
			connection.query = new PostgreSqlQueryFactory(connection);

			// Execute initialization script
			var nameMap = new NameMapping();
			nameMap.Add("__MaxWordLength__", DatabaseConstants.MaxWordLength.ToString());
			nameMap.Add("__AutoKkutu_Rearrange__", connection.GetWordPriorityFuncName());
			nameMap.Add("__AutoKkutu_RearrangeMission__", connection.GetWordPriorityFuncName());

			connection.TryExecute(nameMap.ApplyTo(Resources.Initialization));

			// Check the database tables
			connection.CheckTable();

			LibLogger.Info<PostgreSqlDatabaseConnection>("Successfully established database connection.");

			return connection;
		}
		catch (Exception ex)
		{
			LibLogger.Error<PostgreSqlDatabaseConnection>(ex, DatabaseConstants.ErrorConnect);
			DatabaseEvents.TriggerDatabaseError();
		}

		return null;
	}
}
