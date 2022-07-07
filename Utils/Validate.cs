using AutoKkutu.Databases;
using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace AutoKkutu.Utils
{
	public static class Validate
	{
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

		public static DbCommand RequireNotNull([NotNull] this DbCommand? command)
		{
			if (command == null)
				throw new InvalidOperationException(I18n.Validate_DatabaseCommand);
			return command;
		}
	}
}
