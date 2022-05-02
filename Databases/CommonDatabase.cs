using AutoKkutu.Databases.MySQL;
using AutoKkutu.Databases.PostgreSQL;
using AutoKkutu.Databases.SQLite;
using log4net;
using System;
using System.Configuration;

namespace AutoKkutu.Databases
{
	public abstract class CommonDatabase : IDisposable
	{
		public static readonly ILog Logger = LogManager.GetLogger(nameof(CommonDatabase));

		protected CommonDatabase()
		{
		}

		public abstract CommonDatabaseConnection OpenSecondaryConnection();

		public abstract string GetDBType();

		protected virtual void Dispose(bool disposing)
		{
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}
