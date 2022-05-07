using Microsoft.Data.Sqlite;
using System;
using System.Data.Common;

namespace AutoKkutu.Databases.SQLite
{
	public class SQLiteDatabaseParameter : CommonDatabaseParameter
	{
		public SQLiteDatabaseParameter(string name, object? value) : base(name, value)
		{
		}

		public SQLiteDatabaseParameter(CommonDatabaseType dataType, string name, object? value) : base(dataType, name, value)
		{
		}

		public SQLiteDatabaseParameter(CommonDatabaseType dataType, byte precision, string name, object? value) : base(dataType, precision, name, value)
		{
		}

		public override DbParameter Translate()
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

		private SqliteType? TranslateDataType()
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
