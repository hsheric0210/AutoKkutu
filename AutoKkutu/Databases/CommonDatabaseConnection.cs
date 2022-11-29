﻿using System;

namespace AutoKkutu.Databases
{
	public abstract class CommonDatabaseConnection : IDisposable
	{
		protected CommonDatabaseConnection()
		{
		}

		public abstract void AddSequenceColumnToWordList();

		public abstract void ChangeWordListColumnType(string tableName, string columnName, string newType);

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public abstract void DropWordListColumn(string columnName);

		public abstract string GetRearrangeFuncName();

		public abstract string GetRearrangeMissionFuncName();

		public abstract string? GetColumnType(string tableName, string columnName);

		public abstract string GetWordListColumnOptions();

		public abstract bool IsColumnExists(string tableName, string columnName);

		public abstract bool IsTableExists(string tablename);

		public abstract void PerformVacuum();

		protected virtual void Dispose(bool disposing)
		{
		}
	}
}
