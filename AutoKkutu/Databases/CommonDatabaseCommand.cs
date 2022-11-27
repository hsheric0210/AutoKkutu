using AutoKkutu.Utils;
using NLog;
using System;
using System.Data.Common;
using System.Globalization;
using System.Linq;

namespace AutoKkutu.Databases
{
	public abstract class CommonDatabaseCommand : IDisposable
	{
		private static readonly Logger Logger = LogManager.GetLogger(nameof(CommonDatabaseCommand));

		protected DbCommand? Command
		{
			get; set;
		}

		/// <summary>
		/// True if the preparation/compilation of the query should be disabled.
		/// </summary>
		private readonly bool NoPrepare;

		protected CommonDatabaseCommand(bool noPrepare)
		{
			NoPrepare = noPrepare;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public int ExecuteNonQuery() => Command.RequireNotNull().ExecuteNonQuery();

		public DbDataReader ExecuteReader() => Command.RequireNotNull().ExecuteReader();

		public object? ExecuteScalar() => Command.RequireNotNull().ExecuteScalar();

		public int TryExecuteNonQuery(string action)
		{
			try
			{
				return ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				Logger.Error(ex, CultureInfo.CurrentCulture, "Failed to {0}", action);
			}
			return -1;
		}

		public DbDataReader? TryExecuteReader(string action)
		{
			try
			{
				return ExecuteReader();
			}
			catch (Exception ex)
			{
				Logger.Error(ex, CultureInfo.CurrentCulture, "Failed to {0}", action);
			}
			return null;
		}

		public object? TryExecuteScalar(string action)
		{
			try
			{
				return ExecuteScalar();
			}
			catch (Exception ex)
			{
				Logger.Error(ex, CultureInfo.CurrentCulture, "Failed to {0}", action);
			}
			return null;
		}

		public void TryPrepare()
		{
			if (!NoPrepare)
				Command.RequireNotNull().Prepare();
		}

		public void UpdateParameter(string name, object? value) => Command.RequireNotNull().Parameters[name].Value = value;

		protected void SetCommand(DbCommand command) => Command = command; protected virtual void Dispose(bool disposing)
		{
			if (disposing)
				Command.RequireNotNull().Dispose();
		}

		public void AddParameters(params CommonDatabaseParameter[] parameters) => Command.RequireNotNull().Parameters.AddRange(TranslateParameters(parameters));

		protected abstract DbParameter[] TranslateParameters(params CommonDatabaseParameter[] parameters);

		protected static T[] TranslateParameters<T>(params CommonDatabaseParameter[] parameters) where T : DbParameter => (from parameter in parameters let translated = parameter.Translate() where translated is T select (T)translated).ToArray();
	}
}
