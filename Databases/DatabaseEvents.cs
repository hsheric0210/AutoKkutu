using System;

namespace AutoKkutu.Databases
{
	public static class DatabaseEvents
	{
		public static event EventHandler<DatabaseImportEventArgs> DatabaseImportStart;

		public static event EventHandler<DatabaseImportEventArgs> DatabaseImportDone;

		public static event EventHandler DatabaseIntegrityCheckStart;

		public static event EventHandler<DataBaseIntegrityCheckDoneEventArgs> DatabaseIntegrityCheckDone;

		public static event EventHandler DatabaseError;

		public static void TriggerDatabaseImportStart(this DatabaseImportEventArgs args)
		{
			if (DatabaseImportStart != null)
				DatabaseImportStart(null, args);
		}

		public static void TriggerDatabaseImportDone(this DatabaseImportEventArgs args)
		{
			if (DatabaseImportDone != null)
				DatabaseImportDone(null, args);
		}

		public static void TriggerDatabaseIntegrityCheckStart()
		{
			if (DatabaseIntegrityCheckStart != null)
				DatabaseIntegrityCheckStart(null, EventArgs.Empty);
		}

		public static void TriggerDatabaseIntegrityCheckDone(this DataBaseIntegrityCheckDoneEventArgs args)
		{
			if (DatabaseIntegrityCheckDone != null)
				DatabaseIntegrityCheckDone(null, args);
		}

		public static void TriggerDatabaseError()
		{
			if (DatabaseError != null)
				DatabaseError(null, EventArgs.Empty);
		}
	}

	public class DatabaseImportEventArgs : EventArgs
	{
		public string Name
		{
			get; private set;
		}

		public string Result
		{
			get; private set;
		}

		public DatabaseImportEventArgs(string name) => Name = name;

		public DatabaseImportEventArgs(string name, string result)
		{
			Name = name;
			Result = result;
		}
	}

	public class DataBaseIntegrityCheckDoneEventArgs : EventArgs
	{
		public string Result
		{
			get; private set;
		}

		public DataBaseIntegrityCheckDoneEventArgs(string result)
		{
			Result = result;
		}
	}
}
