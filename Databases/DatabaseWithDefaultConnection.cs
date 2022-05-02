using System;

namespace AutoKkutu.Databases
{
	public abstract class DatabaseWithDefaultConnection : CommonDatabase
	{
		public CommonDatabaseConnection DefaultConnection
		{
			get; private set;
		}

		protected DatabaseWithDefaultConnection()
		{
		}

		public abstract void CheckConnectionType(CommonDatabaseConnection connection);

		public int ExecuteNonQuery(string query, params CommonDatabaseParameter[] parameters)
		{
			CheckDefaultConnectionEstablished();
			return DefaultConnection.ExecuteNonQuery(query, parameters);
		}

		public CommonDatabaseReader ExecuteReader(string query, params CommonDatabaseParameter[] parameters)
		{
			CheckDefaultConnectionEstablished();
			return DefaultConnection.ExecuteReader(query, parameters);
		}

		public object ExecuteScalar(string query, params CommonDatabaseParameter[] parameters)
		{
			CheckDefaultConnectionEstablished();
			return DefaultConnection.ExecuteScalar(query, parameters);
		}

		/// <summary>
		/// Register the default connection
		/// </summary>
		/// <param name="defaultConnection"></param>
		/// <exception cref="InvalidOperationException"></exception>
		public void RegisterDefaultConnection(CommonDatabaseConnection defaultConnection)
		{
			if (defaultConnection != null)
				throw new InvalidOperationException($"{nameof(DefaultConnection)} is already initialized");
			DefaultConnection = defaultConnection;
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
