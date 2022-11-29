using AutoKkutu.Utils;
using System;

namespace AutoKkutu.Databases
{
	public abstract class AbstractDatabase : IDisposable
	{
		private AbstractDatabaseConnection? _baseConnection;

		public AbstractDatabaseConnection Connection => _baseConnection.RequireNotNull();

		protected AbstractDatabase()
		{
		}

		public abstract AbstractDatabaseConnection OpenSecondaryConnection();

		public abstract string GetDBType();

		public abstract string GetWordPriorityFuncName();

		public abstract string GetMissionWordPriorityFuncName();

		protected void Initialize(AbstractDatabaseConnection defaultConnection)
		{
			if (_baseConnection != null)
				throw new InvalidOperationException($"{nameof(Connection)} is already initialized");
			_baseConnection = defaultConnection;
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
				Connection.Dispose();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
