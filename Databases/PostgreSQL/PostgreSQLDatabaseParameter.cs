using Npgsql;
using NpgsqlTypes;
using System;
using System.Data;

namespace AutoKkutu.Databases.PostgreSQL
{
	public class PostgreSQLDatabaseParameter : CommonDatabaseParameter
	{
		public PostgreSQLDatabaseParameter(string name, object value) : base(name, value)
		{
		}

		public PostgreSQLDatabaseParameter(CommonDatabaseType dataType, string name, object value) : base(dataType, name, value)
		{
		}

		public PostgreSQLDatabaseParameter(CommonDatabaseType dataType, byte precision, string name, object value) : base(dataType, precision, name, value)
		{
		}

		public PostgreSQLDatabaseParameter(ParameterDirection direction, CommonDatabaseType dataType, byte precision, string name, object value) : base(direction, dataType, precision, name, value)
		{
		}

		public NpgsqlParameter Translate()
		{
			var parameter = new NpgsqlParameter();
			parameter.ParameterName = Name;
			parameter.Value = Value;
			parameter.NpgsqlDbType = TranslateDataType();
			parameter.Precision = Precision;
			return parameter;
		}

		public NpgsqlDbType TranslateDataType()
		{
			switch (DataType)
			{
				case CommonDatabaseType.Int16:
					return NpgsqlDbType.Smallint;

				case CommonDatabaseType.Int32:
					return NpgsqlDbType.Integer;

				case CommonDatabaseType.Char:
					return NpgsqlDbType.Char;

				case CommonDatabaseType.VarChar:
					return NpgsqlDbType.Varchar;
			}

			throw new NotSupportedException(DataType?.ToString());
		}
	}
}
