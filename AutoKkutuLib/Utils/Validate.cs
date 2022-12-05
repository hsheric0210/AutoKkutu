using AutoKkutuLib.Database;
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace AutoKkutuLib.Utils;

public static class Validate
{
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
