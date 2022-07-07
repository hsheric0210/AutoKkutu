using AutoKkutu.Databases;
using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace AutoKkutu.Utils
{
	public static class Validate
	{
		public static AutoKkutuConfiguration RequireNotNull([NotNull] this AutoKkutuConfiguration? config)
		{
			if (config == null)
				throw new InvalidOperationException(I18n.Validate_Config);
			return config;
		}

		public static AutoKkutuColorPreference RequireNotNull([NotNull] this AutoKkutuColorPreference? colorPref)
		{
			if (colorPref == null)
				throw new InvalidOperationException(I18n.Validate_ColorPrefs);
			return colorPref;
		}

		public static CommonHandler RequireNotNull([NotNull] this CommonHandler? handler)
		{
			if (handler == null)
				throw new InvalidOperationException(I18n.Validate_Handler);
			return handler;
		}

		public static CommonDatabaseConnection RequireNotNull([NotNull] this CommonDatabaseConnection? handler)
		{
			if (handler == null)
				throw new InvalidOperationException(I18n.Validate_DatabaseConnection);
			return handler;
		}

		public static DbDataReader RequireNotNull([NotNull] this DbDataReader? reader)
		{
			if (reader == null)
				throw new InvalidOperationException(I18n.Validate_DatabaseReader);
			return reader;
		}

		public static DbCommand RequireNotNull([NotNull] this DbCommand? command)
		{
			if (command == null)
				throw new InvalidOperationException(I18n.Validate_DatabaseCommand);
			return command;
		}
	}
}
