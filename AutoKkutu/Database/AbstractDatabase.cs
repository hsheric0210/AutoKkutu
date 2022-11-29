using AutoKkutu.Utils;
using Dapper;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace AutoKkutu.Database
{
	public abstract class AbstractDatabase : IDisposable
	{
		private AbstractDatabaseConnection? _baseConnection;

		public AbstractDatabaseConnection Connection => _baseConnection.RequireNotNull();

		static AbstractDatabase()
		{
			SqlMapper.SetTypeMap(typeof(WordModel), new CustomPropertyTypeMap(typeof(WordModel), (type, columnName) => Array.Find(type.GetProperties(), prop => prop.GetCustomAttributes(false).OfType<ColumnAttribute>().Any(attr => attr.Name == columnName))));
		}

		protected AbstractDatabase()
		{
		}

		public abstract AbstractDatabaseConnection OpenSecondaryConnection();

		public abstract string GetDBType();

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
