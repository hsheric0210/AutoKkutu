using Npgsql;
using NpgsqlTypes;
using System;
using System.Data.Common;

namespace AutoKkutu.Databases.PostgreSQL
{
	public class PostgreSQLDatabaseParameter : CommonDatabaseParameter
	{
		public PostgreSQLDatabaseParameter(string name, object? value) : base(name, value)
		{
		}

		public PostgreSQLDatabaseParameter(CommonDatabaseType dataType, string name, object? value) : base(dataType, name, value)
		{
		}

		public PostgreSQLDatabaseParameter(CommonDatabaseType dataType, byte precision, string name, object? value) : base(dataType, precision, name, value)
		{
		}

		public override DbParameter Translate()
		{
			var parameter = new NpgsqlParameter
			{
				ParameterName = Name,
				Value = Value,
				Precision = Precision
			};

			NpgsqlDbType? datatype = TranslateDataType();
			if (datatype != null)
				parameter.NpgsqlDbType = (NpgsqlDbType)datatype;

			return parameter;
		}

		public NpgsqlDbType? TranslateDataType()
		{
			if (DataType == null)
				return null;

			return DataType switch
			{
				CommonDatabaseType.SmallInt => NpgsqlDbType.Smallint,
				CommonDatabaseType.MiddleInt => NpgsqlDbType.Integer,
				CommonDatabaseType.Character => NpgsqlDbType.Char,
				CommonDatabaseType.CharacterVarying => NpgsqlDbType.Varchar,
				_ => throw new NotSupportedException(DataType.ToString()),
			};
		}
	}
}
