using AutoKkutu.Databases.Extension;
using System;
using System.Data.Common;

namespace AutoKkutu.Databases
{
	public abstract class DatabaseWithDefaultConnection : CommonDatabase
	{
		private CommonDatabaseConnection? _defaultConnection;

		public CommonDatabaseConnection DefaultConnection => _defaultConnection.RequireNotNull();

		protected DatabaseWithDefaultConnection()
		{
		}

		public abstract void CheckConnectionType(CommonDatabaseConnection connection);

		/// <summary>
		/// Register the default connection
		/// </summary>
		/// <param name="defaultConnection"></param>
		/// <exception cref="InvalidOperationException"></exception>
		public void RegisterDefaultConnection(CommonDatabaseConnection defaultConnection)
		{
			if (_defaultConnection != null)
				throw new InvalidOperationException($"{nameof(DefaultConnection)} is already initialized");
			_defaultConnection = defaultConnection;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				DefaultConnection.Dispose();
			base.Dispose(disposing);
		}

		private void CheckDefaultConnectionEstablished()
		{
			if (DefaultConnection == null)
				throw new InvalidOperationException("Connection is not established yet");
		}
	}
}
