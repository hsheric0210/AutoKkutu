using Microsoft.Data.Sqlite;
using System.Data;
using System;

namespace AutoKkutu.Databases.SQLite
{
	public class SQLiteDatabaseParameter : CommonDatabaseParameter
	{
		public SQLiteDatabaseParameter(string name, object value) : base(name, value)
		{
		}

		public SQLiteDatabaseParameter(CommonDatabaseType dataType, string name, object value) : base(dataType, name, value)
		{
		}

		public SQLiteDatabaseParameter(CommonDatabaseType dataType, byte precision, string name, object value) : base(dataType, precision, name, value)
		{
		}

		public SQLiteDatabaseParameter(ParameterDirection direction, CommonDatabaseType dataType, byte precision, string name, object value) : base(direction, dataType, precision, name, value)
		{
		}

		public SqliteParameter Translate()
		{
			var parameter = new SqliteParameter();
			parameter.ParameterName = Name;
			parameter.Value = Value;
			parameter.SqliteType = TranslateDataType();
			parameter.Precision = Precision;
			return parameter;
		}

		public SqliteType TranslateDataType()
		{
			switch (DataType)
			{
				case CommonDatabaseType.Int16:
				case CommonDatabaseType.Int32:
					return SqliteType.Integer;

				case CommonDatabaseType.Char:
				case CommonDatabaseType.VarChar:
					return SqliteType.Text;
			}

			throw new NotSupportedException(DataType?.ToString());
		}
	}
}
