using AutoKkutu.Databases;
using System;
using System.Data;
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

		public static AbstractDatabaseConnection RequireNotNull([NotNull] this AbstractDatabaseConnection? handler)
		{
			if (handler == null)
				throw new InvalidOperationException(I18n.Validate_DatabaseConnection);
			return handler;
		}

		public static IDbConnection RequireNotNull([NotNull] this IDbConnection? handler)
		{
			if (handler == null)
				throw new InvalidOperationException(I18n.Validate_DatabaseConnection);
			return handler;
		}
	}
}
