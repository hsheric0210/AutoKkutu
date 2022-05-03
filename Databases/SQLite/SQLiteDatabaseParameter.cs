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
			var parameter = new SqliteParameter
			{
				ParameterName = Name,
				Value = Value,
				Precision = Precision
			};

			SqliteType? datatype = TranslateDataType();
			if (datatype != null)
				parameter.SqliteType = (SqliteType)datatype;

			return parameter;
		}

		public SqliteType? TranslateDataType()
		{
			if (DataType == null)
				return null;

			return DataType switch
			{
				CommonDatabaseType.SmallInt or CommonDatabaseType.MiddleInt => SqliteType.Integer,
				CommonDatabaseType.Character or CommonDatabaseType.CharacterVarying => SqliteType.Text,
				_ => throw new NotSupportedException(DataType.ToString()),
			};
		}
	}
}
