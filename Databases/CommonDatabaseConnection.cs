using log4net;
using System;
using System.Data;
using System.Data.Common;

namespace AutoKkutu.Databases
{
	public abstract class CommonDatabaseConnection : IDisposable
	{
		private static readonly ILog Logger = LogManager.GetLogger(nameof(CommonDatabaseConnection));

		protected CommonDatabaseConnection()
		{
		}

		public abstract void AddSequenceColumnToWordList();

		public abstract void ChangeWordListColumnType(string tableName, string columnName, string newType);

		public abstract CommonDatabaseParameter CreateParameter(string name, object value);

		public abstract CommonDatabaseParameter CreateParameter(CommonDatabaseType dataType, string name, object value);

		public abstract CommonDatabaseParameter CreateParameter(CommonDatabaseType dataType, byte precision, string name, object value);

		public abstract CommonDatabaseParameter CreateParameter(ParameterDirection direction, CommonDatabaseType dataType, byte precision, string name, object value);

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public abstract void DropWordListColumn(string columnName);

		public abstract int ExecuteNonQuery(string query, params CommonDatabaseParameter[] parameters);

		public abstract DbDataReader ExecuteReader(string query, params CommonDatabaseParameter[] parameters);

		public abstract object ExecuteScalar(string query, params CommonDatabaseParameter[] parameters);

		public abstract string GetRearrangeFuncName();

		public abstract string GetRearrangeMissionFuncName();

		public abstract string GetColumnType(string tableName, string columnName);

		public abstract string GetWordListColumnOptions();

		public abstract bool IsColumnExists(string tableName, string columnName);

		public abstract bool IsTableExists(string tablename);

		public abstract void PerformVacuum();

		public int TryExecuteNonQuery(string action, string query, params CommonDatabaseParameter[] parameters)
		{
			try
			{
				return ExecuteNonQuery(query, parameters);
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to {action}", ex);
			}
			return -1;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1168")]
		public DbDataReader TryExecuteReader(string action, string query, params CommonDatabaseParameter[] parameters)
		{
			try
			{
				return ExecuteReader(query, parameters);
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to {action}", ex);
			}
			return null;
		}

		public object TryExecuteScalar(string action, string query, params CommonDatabaseParameter[] parameters)
		{
			try
			{
				return ExecuteScalar(query, parameters);
			}
			catch (Exception ex)
			{
				Logger.Error($"Failed to {action}", ex);
			}
			return null;
		}

		protected virtual void Dispose(bool disposing)
		{
		}
	}
}
