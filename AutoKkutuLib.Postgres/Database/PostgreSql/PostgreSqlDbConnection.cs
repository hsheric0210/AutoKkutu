using AutoKkutuLib.Database.Sql;
using AutoKkutuLib.Database.Sql.Query;
using AutoKkutuLib.Postgres.Database.PostgreSql.Query;
using AutoKkutuLib.Postgres.Properties;
using Npgsql;
using System.Text;

namespace AutoKkutuLib.Database.PostgreSql;

public sealed class PostgreSqlDbConnection : DbConnectionBase
{
	private QueryFactory query = null!; // It's always initialized by factory method
	public override QueryFactory Query => query;
	public override string DbType => "PostgreSQL";

	private PostgreSqlDbConnection(NpgsqlConnection connection) : base(connection) { }

	public override string GetWordPriorityFuncName() => "__AutoKkutu_Rearrange";

	public override string GetMissionWordPriorityFuncName() => "__AutoKkutu_RearrangeMission";

	public override string GetWordListColumnOptions()
	{
		var builder = new StringBuilder();
		builder.Append(DatabaseConstants.SequenceColumnName).Append(" SERIAL PRIMARY KEY, ");
		builder.Append(DatabaseConstants.WordColumnName).Append(" CHAR VARYING(256) UNIQUE NOT NULL, ");
		builder.Append(DatabaseConstants.WordIndexColumnName).Append(" CHAR(1) NOT NULL, ");
		builder.Append(DatabaseConstants.ReverseWordIndexColumnName).Append(" CHAR(1) NOT NULL, ");
		builder.Append(DatabaseConstants.KkutuWordIndexColumnName).Append(" CHAR VARYING(2) NOT NULL, ");
		builder.Append(DatabaseConstants.TypeColumnName).Append(" INT NOT NULL, ");
		builder.Append(DatabaseConstants.ThemeColumn1Name).Append(" BIGINT NOT NULL, ");
		builder.Append(DatabaseConstants.ThemeColumn2Name).Append(" BIGINT NOT NULL, ");
		builder.Append(DatabaseConstants.ThemeColumn3Name).Append(" BIGINT NOT NULL, ");
		builder.Append(DatabaseConstants.ThemeColumn4Name).Append(" BIGINT NOT NULL, ");
		builder.Append(DatabaseConstants.ChoseongColumnName).Append(" CHAR VARYING(256) NOT NULL, ");
		builder.Append(DatabaseConstants.MeaningColumnName).Append(" TEXT NOT NULL, ");
		builder.Append(DatabaseConstants.FlagsColumnName).Append(" INT NOT NULL");
		return builder.ToString();
	}

	public static PostgreSqlDbConnection? Create(string connectionString)
	{
		try
		{
			// Open the connection
			LibLogger.Info<PostgreSqlDbConnection>("Opening database connection...");
			var nativeConnection = new NpgsqlConnection(connectionString);
			nativeConnection.Open();
			var connection = new PostgreSqlDbConnection(nativeConnection);
			connection.query = new PostgreSqlQueryFactory(connection);

			// Execute initialization script
			var nameMap = new NameMapping();
			nameMap.Add("__MaxWordLength__", DatabaseConstants.MaxWordLength.ToString());
			nameMap.Add("__AutoKkutu_Rearrange__", connection.GetWordPriorityFuncName());
			nameMap.Add("__AutoKkutu_RearrangeMission__", connection.GetWordPriorityFuncName());

			connection.TryExecute(nameMap.ApplyTo(Resources.Initialization));

			// Check the database tables
			connection.CheckTable();

			LibLogger.Info<PostgreSqlDbConnection>("Successfully established database connection.");

			return connection;
		}
		catch (Exception ex)
		{
			LibLogger.Error<PostgreSqlDbConnection>(ex, DatabaseConstants.ErrorConnect);
			DatabaseEvents.TriggerDatabaseError();
		}

		return null;
	}
}
