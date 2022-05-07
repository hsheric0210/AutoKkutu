using AutoKkutu.Databases;
using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

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

		public static DbCommand RequireNotNull([NotNull] this DbCommand? command)
		{
			if (command == null)
				throw new InvalidOperationException("Command not initialized");
			return command;
		}
	}
}
