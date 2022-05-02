using log4net;
using Npgsql;
using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace AutoKkutu.Databases.PostgreSQL
{
	public partial class PostgreSQLDatabaseConnection : CommonDatabaseConnection
	{
		private static readonly ILog Logger = LogManager.GetLogger(nameof(CommonDatabaseConnection));
		private readonly NpgsqlConnection Connection;

		public PostgreSQLDatabaseConnection(NpgsqlConnection connection) => Connection = connection;

		public override void AddSequenceColumnToWordList() => ExecuteNonQuery($"ALTER TABLE {DatabaseConstants.WordListTableName} ADD COLUMN seq SERIAL PRIMARY KEY");

		public override void ChangeWordListColumnType(string tableName, string columnName, string newType) => ExecuteNonQuery($"ALTER TABLE {tableName} ALTER COLUMN {columnName} TYPE {newType}");

		public override CommonDatabaseParameter CreateParameter(string name, object value) => new PostgreSQLDatabaseParameter(name, value);

		public override CommonDatabaseParameter CreateParameter(CommonDatabaseType dataType, string name, object value) => new PostgreSQLDatabaseParameter(dataType, name, value);

		public override CommonDatabaseParameter CreateParameter(CommonDatabaseType dataType, byte precision, string name, object value) => new PostgreSQLDatabaseParameter(dataType, precision, name, value);

		public override CommonDatabaseParameter CreateParameter(ParameterDirection direction, CommonDatabaseType dataType, byte precision, string name, object value) => new PostgreSQLDatabaseParameter(direction, dataType, precision, name, value);

		public override void DropWordListColumn(string columnName) => ExecuteNonQuery($"ALTER TABLE {DatabaseConstants.WordListTableName} DROP COLUMN {columnName}");

		[SuppressMessage("Security", "CA2100", Justification = "Already handled")]
		public override int ExecuteNonQuery(string query, params CommonDatabaseParameter[] parameters)
		{
			if (string.IsNullOrWhiteSpace(query))
				throw new ArgumentException(query, nameof(query));

			using (var command = new NpgsqlCommand(query, Connection))
			{
				if (parameters != null)
					foreach (var parameter in TranslateParameter(parameters))
						command.Parameters.Add(parameter);
				return command.ExecuteNonQuery();
			}
		}

		[SuppressMessage("Security", "CA2100", Justification = "Already handled")]
		public override CommonDatabaseReader ExecuteReader(string query, params CommonDatabaseParameter[] parameters)
		{
			if (string.IsNullOrWhiteSpace(query))
				throw new ArgumentException(query, nameof(query));

			using (var command = new NpgsqlCommand(query, Connection))
			{
				if (parameters != null)
					foreach (var parameter in TranslateParameter(parameters))
						command.Parameters.Add(parameter);
				return new PostgreSQLDatabaseReader(command.ExecuteReader());
			}
		}

		[SuppressMessage("Security", "CA2100", Justification = "Already handled")]
		public override object ExecuteScalar(string query, params CommonDatabaseParameter[] parameters)
		{
			if (string.IsNullOrWhiteSpace(query))
				throw new ArgumentException(query, nameof(query));

			using (var command = new NpgsqlCommand(query, Connection))
			{
				if (parameters != null)
					foreach (var parameter in TranslateParameter(parameters))
						command.Parameters.Add(parameter);
				return command.ExecuteScalar();
			}
		}

		public override string GetRearrangeFuncName() => "__AutoKkutu_Rearrange";
		public override string GetRearrangeMissionFuncName() => "__AutoKkutu_RearrangeMission";

		public override string GetColumnType(string tableName, string columnName)
		{
			tableName = tableName ?? DatabaseConstants.WordListTableName;
			try
			{
				return ExecuteScalar("SELECT data_type FROM information_schema.columns WHERE table_name=@tableName AND column_name=@columnName;", CreateParameter("@tableName", tableName), CreateParameter("@columnName", columnName)).ToString();
			}
			catch (Exception ex)
			{
				Logger.Error(string.Format(CultureInfo.CurrentCulture, DatabaseConstants.ErrorGetColumnType, columnName, tableName), ex);
			}
			return "";
		}

		public override string GetWordListColumnOptions() => "seq SERIAL PRIMARY KEY, word CHAR VARYING(256) UNIQUE NOT NULL, word_index CHAR(1) NOT NULL, reverse_word_index CHAR(1) NOT NULL, kkutu_index VARCHAR(2) NOT NULL, flags SMALLINT NOT NULL";

		public override bool IsColumnExists(string tableName, string columnName)
		{
			tableName = tableName ?? DatabaseConstants.WordListTableName;
			try
			{
				return Convert.ToInt32(ExecuteScalar($"SELECT COUNT(*) FROM information_schema.columns WHERE table_name='{tableName}' AND column_name='{columnName}';"), CultureInfo.InvariantCulture) > 0;
			}
			catch (Exception ex)
			{
				Logger.Error(string.Format(CultureInfo.CurrentCulture, DatabaseConstants.ErrorIsColumnExists, columnName, tableName), ex);
				return false;
			}
		}

		public override bool IsTableExists(string tablename)
		{
			try
			{
				return Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM information_schema.tables WHERE table_name=@tableName;", CreateParameter("@tableName", tablename)), CultureInfo.InvariantCulture) > 0;
			}
			catch (Exception ex)
			{
				Logger.Error(string.Format(CultureInfo.CurrentCulture, DatabaseConstants.ErrorIsTableExists, tablename), ex);
				return false;
			}
		}

		public override void PerformVacuum() => ExecuteNonQuery("VACUUM");

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				Connection.Dispose();
			base.Dispose(disposing);
		}

		private static NpgsqlParameter[] TranslateParameter(params CommonDatabaseParameter[] parameters) => (from parameter in parameters where parameter is PostgreSQLDatabaseParameter let pgsqlParam = (PostgreSQLDatabaseParameter)parameter select pgsqlParam.Translate()).ToArray();
	}
}
