using Serilog;
using System;

namespace AutoKkutu.Databases
{
	public abstract class CommonDatabase : IDisposable
	{

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
