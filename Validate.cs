using AutoKkutu.Databases;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoKkutu
{
	public static class Validate
	{
		public static AutoKkutuConfiguration RequireNotNull([NotNull] this AutoKkutuConfiguration? config)
		{
			if (config == null)
				throw new InvalidOperationException("Config is not set");
			return config;
		}

		public static AutoKkutuColorPreference RequireNotNull([NotNull] this AutoKkutuColorPreference? colorPref)
		{
			if (colorPref == null)
				throw new InvalidOperationException("Color preference is not set");
			return colorPref;
		}

		public static CommonHandler RequireNotNull([NotNull] this CommonHandler? handler)
		{
			if (handler == null)
				throw new InvalidOperationException("Handler is not registered");
			return handler;
		}

		public static CommonDatabaseConnection RequireNotNull([NotNull] this CommonDatabaseConnection? handler)
		{
			if (handler == null)
				throw new InvalidOperationException("Database connection is not established");
			return handler;
		}

		public static DbDataReader RequireNotNull([NotNull] this DbDataReader? reader)
		{
			if (reader == null)
				throw new InvalidOperationException("Reader not open");
			return reader;
		}
	}
}
