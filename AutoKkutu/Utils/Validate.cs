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
	}
}
