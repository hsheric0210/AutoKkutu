using log4net;
using MySqlConnector;
using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace AutoKkutu.Databases.MySQL
{
	public partial class MySQLDatabaseConnection : CommonDatabaseConnection
	{
		private static readonly ILog Logger = LogManager.GetLogger(nameof(CommonDatabaseConnection));
		private readonly MySqlConnection Connection;
		private readonly string DatabaseName;

		public MySQLDatabaseConnection(MySqlConnection connection, string dbName)
		{
			Connection = connection;
			DatabaseName = dbName;
		}

		public override void AddSequenceColumnToWordList() => ExecuteNonQuery($"ALTER TABLE {DatabaseConstants.WordListTableName} ADD COLUMN seq NOT NULL AUTO_INCREMENT PRIMARY KEY;");

		public override void ChangeWordListColumnType(string tableName, string columnName, string newType) => ExecuteNonQuery($"ALTER TABLE {tableName} MODIFY {columnName} {newType}");

		public override CommonDatabaseParameter CreateParameter(string name, object value) => new MySQLDatabaseParameter(name, value);

		public override CommonDatabaseParameter CreateParameter(CommonDatabaseType dataType, string name, object value) => new MySQLDatabaseParameter(dataType, name, value);

		public override CommonDatabaseParameter CreateParameter(CommonDatabaseType dataType, byte precision, string name, object value) => new MySQLDatabaseParameter(dataType, precision, name, value);

		public override CommonDatabaseParameter CreateParameter(ParameterDirection direction, CommonDatabaseType dataType, byte precision, string name, object value) => new MySQLDatabaseParameter(direction, dataType, precision, name, value);

		public override void DropWordListColumn(string columnName) => ExecuteNonQuery($"ALTER TABLE {DatabaseConstants.WordListTableName} DROP {columnName}");

		[SuppressMessage("Security", "CA2100", Justification = "Already handled")]
		public override int ExecuteNonQuery(string query, params CommonDatabaseParameter[] parameters)
		{
			if (string.IsNullOrWhiteSpace(query))
				throw new ArgumentException(query, nameof(query));

			using var command = new MySqlCommand(query, Connection);
			if (parameters != null)
				command.Parameters.AddRange(TranslateParameter(parameters));
			return command.ExecuteNonQuery();
		}

		[SuppressMessage("Security", "CA2100", Justification = "Already handled")]
		public override DbDataReader ExecuteReader(string query, params CommonDatabaseParameter[] parameters)
		{
			if (string.IsNullOrWhiteSpace(query))
				throw new ArgumentException(query, nameof(query));

			using var command = new MySqlCommand(query, Connection);
			if (parameters != null)
				command.Parameters.AddRange(TranslateParameter(parameters));
			return command.ExecuteReader();
		}

		[SuppressMessage("Security", "CA2100", Justification = "Already handled")]
		public override object? ExecuteScalar(string query, params CommonDatabaseParameter[] parameters)
		{
			if (string.IsNullOrWhiteSpace(query))
				throw new ArgumentException(query, nameof(query));

			using var command = new MySqlCommand(query, Connection);
			if (parameters != null)
				command.Parameters.AddRange(TranslateParameter(parameters));
			return command.ExecuteScalar();
		}

		public override string GetRearrangeFuncName() => "__AutoKkutu_Rearrange";
		public override string GetRearrangeMissionFuncName() => "__AutoKkutu_RearrangeMission";

		public override string? GetColumnType(string tableName, string columnName)
		{
			try
			{
				return ExecuteScalar("SELECT data_type FROM Information_schema.columns WHERE table_name=@tableName AND column_name=@columnName;", CreateParameter("@tableName", tableName), CreateParameter("@columnName", columnName))?.ToString();
			}
			catch (Exception ex)
			{
				Logger.Info(string.Format(CultureInfo.CurrentCulture, DatabaseConstants.ErrorGetColumnType, columnName, tableName), ex);
				return "";
			}
		}

		public override string GetWordListColumnOptions() => "seq INT NOT NULL AUTO_INCREMENT PRIMARY KEY, word VARCHAR(256) UNIQUE NOT NULL, word_index CHAR(1) NOT NULL, reverse_word_index CHAR(1) NOT NULL, kkutu_index VARCHAR(2) NOT NULL, flags SMALLINT NOT NULL";

		public override bool IsColumnExists(string tableName, string columnName)
		{
			tableName ??= DatabaseConstants.WordListTableName;
			try
			{
				return Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM Information_schema.columns WHERE table_schema=@dbName AND table_name=@tableName AND column_name=@columnName;", CreateParameter("@dbName", DatabaseName), CreateParameter("@tableName", tableName), CreateParameter("@columnName", columnName)), CultureInfo.InvariantCulture) > 0;
			}
			catch (Exception ex)
			{
				Logger.Info(string.Format(CultureInfo.CurrentCulture, DatabaseConstants.ErrorIsColumnExists, columnName, tableName), ex);
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
				Logger.Info(string.Format(CultureInfo.CurrentCulture, DatabaseConstants.ErrorIsTableExists, tablename), ex);
				return false;
			}
		}

		public override void PerformVacuum()
		{
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				Connection.Dispose();
			base.Dispose(disposing);
		}

		private static MySqlParameter[] TranslateParameter(params CommonDatabaseParameter[] parameters) => (from parameter in parameters where parameter is MySQLDatabaseParameter let mysqlParam = (MySQLDatabaseParameter)parameter select mysqlParam.Translate()).ToArray();
	}
}
