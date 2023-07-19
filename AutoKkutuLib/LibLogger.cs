/*
from:                LibLogger.(Verbose|Debug|Info|Warn|Error|Fatal)\((.+)\)
replace2type_param:  LibLogger.$1<Type>($2)
replace2param:       LibLogger.$1(nameof(Type), $2)
 */
using Serilog;

namespace AutoKkutuLib;

/// <summary>
/// 라이브러리의 로깅을 담당합니다.
/// 로깅 시 대상 모듈의 이름을 지정하기 위하여 만들어졌으며
/// 또한 로깅 라이브러리를 변경하거나 업그레이드하더라도 호환성을 보장하기 위한 일종의 어댑터 레이어 역할도 담당합니다.
/// </summary>
public static class LibLogger
{
	private readonly static IDictionary<string, ILogger> moduleMapping = new Dictionary<string, ILogger>();

	private static ILogger GetLogger(string module)
	{
		if (!moduleMapping.ContainsKey(module))
			moduleMapping[module] = Log.ForContext("Module", module);

		return moduleMapping[module];
	}

	#region Verbose - with module parameter
	public static void Verbose(string module, string message) => GetLogger(module).Verbose(message);

	public static void Verbose<T>(string module, string message, T propertyValue) => GetLogger(module).Verbose(message, propertyValue);

	public static void Verbose<T0, T1>(string module, string message, T0 propertyValue0, T1 propertyValue1) => GetLogger(module).Verbose(message, propertyValue0, propertyValue1);

	public static void Verbose<T0, T1, T2>(string module, string message, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => GetLogger(module).Verbose(message, propertyValue0, propertyValue1, propertyValue2);

	public static void Verbose(string module, string message, params object?[]? propertyValues) => GetLogger(module).Verbose(message, propertyValues);
	#endregion

	#region Verbose - with Module type parameter
	public static void Verbose<M>(string message) => GetLogger(typeof(M).Name).Verbose(message);

	public static void Verbose<M, T>(string message, T propertyValue) => GetLogger(typeof(M).Name).Verbose(message, propertyValue);

	public static void Verbose<M, T0, T1>(string message, T0 propertyValue0, T1 propertyValue1) => GetLogger(typeof(M).Name).Verbose(message, propertyValue0, propertyValue1);

	public static void Verbose<M, T0, T1, T2>(string message, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => GetLogger(typeof(M).Name).Verbose(message, propertyValue0, propertyValue1, propertyValue2);

	public static void Verbose<M>(string message, params object?[]? propertyValues) => GetLogger(typeof(M).Name).Verbose(message, propertyValues);
	#endregion

	#region Debug - with module parameter
	public static void Debug(string module, string message) => GetLogger(module).Debug(message);

	public static void Debug<T>(string module, string message, T propertyValue) => GetLogger(module).Debug(message, propertyValue);

	public static void Debug<T0, T1>(string module, string message, T0 propertyValue0, T1 propertyValue1) => GetLogger(module).Debug(message, propertyValue0, propertyValue1);

	public static void Debug<T0, T1, T2>(string module, string message, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => GetLogger(module).Debug(message, propertyValue0, propertyValue1, propertyValue2);

	public static void Debug(string module, string message, params object?[]? propertyValues) => GetLogger(module).Debug(message, propertyValues);
	#endregion

	#region Debug - with Module type parameter
	public static void Debug<M>(string message) => GetLogger(typeof(M).Name).Debug(message);

	public static void Debug<M, T>(string message, T propertyValue) => GetLogger(typeof(M).Name).Debug(message, propertyValue);

	public static void Debug<M, T0, T1>(string message, T0 propertyValue0, T1 propertyValue1) => GetLogger(typeof(M).Name).Debug(message, propertyValue0, propertyValue1);

	public static void Debug<M, T0, T1, T2>(string message, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => GetLogger(typeof(M).Name).Debug(message, propertyValue0, propertyValue1, propertyValue2);

	public static void Debug<M>(string message, params object?[]? propertyValues) => GetLogger(typeof(M).Name).Debug(message, propertyValues);
	#endregion

	#region Info - with module parameter
	public static void Info(string module, string message) => GetLogger(module).Information(message);

	public static void Info<T>(string module, string message, T propertyValue) => GetLogger(module).Information(message, propertyValue);

	public static void Info<T0, T1>(string module, string message, T0 propertyValue0, T1 propertyValue1) => GetLogger(module).Information(message, propertyValue0, propertyValue1);

	public static void Info<T0, T1, T2>(string module, string message, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => GetLogger(module).Information(message, propertyValue0, propertyValue1, propertyValue2);

	public static void Info(string module, string message, params object?[]? propertyValues) => GetLogger(module).Information(message, propertyValues);
	#endregion

	#region Info - with Module type parameter
	public static void Info<M>(string message) => GetLogger(typeof(M).Name).Information(message);

	public static void Info<M, T>(string message, T propertyValue) => GetLogger(typeof(M).Name).Information(message, propertyValue);

	public static void Info<M, T0, T1>(string message, T0 propertyValue0, T1 propertyValue1) => GetLogger(typeof(M).Name).Information(message, propertyValue0, propertyValue1);

	public static void Info<M, T0, T1, T2>(string message, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => GetLogger(typeof(M).Name).Information(message, propertyValue0, propertyValue1, propertyValue2);

	public static void Info<M>(string message, params object?[]? propertyValues) => GetLogger(typeof(M).Name).Information(message, propertyValues);
	#endregion

	#region Warn - with module parameter
	public static void Warn(string module, string message) => GetLogger(module).Warning(message);

	public static void Warn<T>(string module, string message, T propertyValue) => GetLogger(module).Warning(message, propertyValue);

	public static void Warn<T0, T1>(string module, string message, T0 propertyValue0, T1 propertyValue1) => GetLogger(module).Warning(message, propertyValue0, propertyValue1);

	public static void Warn<T0, T1, T2>(string module, string message, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => GetLogger(module).Warning(message, propertyValue0, propertyValue1, propertyValue2);

	public static void Warn(string module, string message, params object?[]? propertyValues) => GetLogger(module).Warning(message, propertyValues);
	#endregion

	#region Warn - with Module type parameter
	public static void Warn<M>(string message) => GetLogger(typeof(M).Name).Warning(message);

	public static void Warn<M, T>(string message, T propertyValue) => GetLogger(typeof(M).Name).Warning(message, propertyValue);

	public static void Warn<M, T0, T1>(string message, T0 propertyValue0, T1 propertyValue1) => GetLogger(typeof(M).Name).Warning(message, propertyValue0, propertyValue1);

	public static void Warn<M, T0, T1, T2>(string message, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => GetLogger(typeof(M).Name).Warning(message, propertyValue0, propertyValue1, propertyValue2);

	public static void Warn<M>(string message, params object?[]? propertyValues) => GetLogger(typeof(M).Name).Warning(message, propertyValues);
	#endregion

	#region Warn - with module and exception parameter
	public static void Warn(string module, Exception? exception, string message) => GetLogger(module).Warning(exception, message);

	public static void Warn<T>(string module, Exception? exception, string message, T propertyValue) => GetLogger(module).Warning(exception, message, propertyValue);

	public static void Warn<T0, T1>(string module, Exception? exception, string message, T0 propertyValue0, T1 propertyValue1) => GetLogger(module).Warning(exception, message, propertyValue0, propertyValue1);

	public static void Warn<T0, T1, T2>(string module, Exception? exception, string message, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => GetLogger(module).Warning(exception, message, propertyValue0, propertyValue1, propertyValue2);

	public static void Warn(string module, Exception? exception, string message, params object?[]? propertyValues) => GetLogger(module).Warning(exception, message, propertyValues);
	#endregion

	#region Warn - with Module type parameter and exception parameter
	public static void Warn<M>(Exception? exception, string message) => GetLogger(typeof(M).Name).Warning(exception, message);

	public static void Warn<M, T>(Exception? exception, string message, T propertyValue) => GetLogger(typeof(M).Name).Warning(exception, message, propertyValue);

	public static void Warn<M, T0, T1>(Exception? exception, string message, T0 propertyValue0, T1 propertyValue1) => GetLogger(typeof(M).Name).Warning(exception, message, propertyValue0, propertyValue1);

	public static void Warn<M, T0, T1, T2>(Exception? exception, string message, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => GetLogger(typeof(M).Name).Warning(exception, message, propertyValue0, propertyValue1, propertyValue2);

	public static void Warn<M>(Exception? exception, string message, params object?[]? propertyValues) => GetLogger(typeof(M).Name).Warning(exception, message, propertyValues);
	#endregion

	#region Error - with module parameter
	public static void Error(string module, string message) => GetLogger(module).Error(message);

	public static void Error<T>(string module, string message, T propertyValue) => GetLogger(module).Error(message, propertyValue);

	public static void Error<T0, T1>(string module, string message, T0 propertyValue0, T1 propertyValue1) => GetLogger(module).Error(message, propertyValue0, propertyValue1);

	public static void Error<T0, T1, T2>(string module, string message, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => GetLogger(module).Error(message, propertyValue0, propertyValue1, propertyValue2);

	public static void Error(string module, string message, params object?[]? propertyValues) => GetLogger(module).Error(message, propertyValues);
	#endregion

	#region Error - with Module type parameter
	public static void Error<M>(string message) => GetLogger(typeof(M).Name).Error(message);

	public static void Error<M, T>(string message, T propertyValue) => GetLogger(typeof(M).Name).Error(message, propertyValue);

	public static void Error<M, T0, T1>(string message, T0 propertyValue0, T1 propertyValue1) => GetLogger(typeof(M).Name).Error(message, propertyValue0, propertyValue1);

	public static void Error<M, T0, T1, T2>(string message, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => GetLogger(typeof(M).Name).Error(message, propertyValue0, propertyValue1, propertyValue2);

	public static void Error<M>(string message, params object?[]? propertyValues) => GetLogger(typeof(M).Name).Error(message, propertyValues);
	#endregion

	#region Error - with module and exception parameter
	public static void Error(string module, Exception? exception, string message) => GetLogger(module).Error(exception, message);

	public static void Error<T>(string module, Exception? exception, string message, T propertyValue) => GetLogger(module).Error(exception, message, propertyValue);

	public static void Error<T0, T1>(string module, Exception? exception, string message, T0 propertyValue0, T1 propertyValue1) => GetLogger(module).Error(exception, message, propertyValue0, propertyValue1);

	public static void Error<T0, T1, T2>(string module, Exception? exception, string message, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => GetLogger(module).Error(exception, message, propertyValue0, propertyValue1, propertyValue2);

	public static void Error(string module, Exception? exception, string message, params object?[]? propertyValues) => GetLogger(module).Error(exception, message, propertyValues);
	#endregion

	#region Error - with Module type parameter and exception parameter
	public static void Error<M>(Exception? exception, string message) => GetLogger(typeof(M).Name).Error(exception, message);

	public static void Error<M, T>(Exception? exception, string message, T propertyValue) => GetLogger(typeof(M).Name).Error(exception, message, propertyValue);

	public static void Error<M, T0, T1>(Exception? exception, string message, T0 propertyValue0, T1 propertyValue1) => GetLogger(typeof(M).Name).Error(exception, message, propertyValue0, propertyValue1);

	public static void Error<M, T0, T1, T2>(Exception? exception, string message, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => GetLogger(typeof(M).Name).Error(exception, message, propertyValue0, propertyValue1, propertyValue2);

	public static void Error<M>(Exception? exception, string message, params object?[]? propertyValues) => GetLogger(typeof(M).Name).Error(exception, message, propertyValues);
	#endregion

	#region Fatal - with module parameter
	public static void Fatal(string module, string message) => GetLogger(module).Fatal(message);

	public static void Fatal<T>(string module, string message, T propertyValue) => GetLogger(module).Fatal(message, propertyValue);

	public static void Fatal<T0, T1>(string module, string message, T0 propertyValue0, T1 propertyValue1) => GetLogger(module).Fatal(message, propertyValue0, propertyValue1);

	public static void Fatal<T0, T1, T2>(string module, string message, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => GetLogger(module).Fatal(message, propertyValue0, propertyValue1, propertyValue2);

	public static void Fatal(string module, string message, params object?[]? propertyValues) => GetLogger(module).Fatal(message, propertyValues);
	#endregion

	#region Fatal - with Module type parameter
	public static void Fatal<M>(string message) => GetLogger(typeof(M).Name).Fatal(message);

	public static void Fatal<M, T>(string message, T propertyValue) => GetLogger(typeof(M).Name).Fatal(message, propertyValue);

	public static void Fatal<M, T0, T1>(string message, T0 propertyValue0, T1 propertyValue1) => GetLogger(typeof(M).Name).Fatal(message, propertyValue0, propertyValue1);

	public static void Fatal<M, T0, T1, T2>(string message, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => GetLogger(typeof(M).Name).Fatal(message, propertyValue0, propertyValue1, propertyValue2);

	public static void Fatal<M>(string message, params object?[]? propertyValues) => GetLogger(typeof(M).Name).Fatal(message, propertyValues);
	#endregion

	#region Fatal - with module and exception parameter
	public static void Fatal(string module, Exception? exception, string message) => GetLogger(module).Fatal(exception, message);

	public static void Fatal<T>(string module, Exception? exception, string message, T propertyValue) => GetLogger(module).Fatal(exception, message, propertyValue);

	public static void Fatal<T0, T1>(string module, Exception? exception, string message, T0 propertyValue0, T1 propertyValue1) => GetLogger(module).Fatal(exception, message, propertyValue0, propertyValue1);

	public static void Fatal<T0, T1, T2>(string module, Exception? exception, string message, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => GetLogger(module).Fatal(exception, message, propertyValue0, propertyValue1, propertyValue2);

	public static void Fatal(string module, Exception? exception, string message, params object?[]? propertyValues) => GetLogger(module).Fatal(exception, message, propertyValues);
	#endregion

	#region Fatal - with Module type parameter and exception parameter
	public static void Fatal<M>(Exception? exception, string message) => GetLogger(typeof(M).Name).Fatal(exception, message);

	public static void Fatal<M, T>(Exception? exception, string message, T propertyValue) => GetLogger(typeof(M).Name).Fatal(exception, message, propertyValue);

	public static void Fatal<M, T0, T1>(Exception? exception, string message, T0 propertyValue0, T1 propertyValue1) => GetLogger(typeof(M).Name).Fatal(exception, message, propertyValue0, propertyValue1);

	public static void Fatal<M, T0, T1, T2>(Exception? exception, string message, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => GetLogger(typeof(M).Name).Fatal(exception, message, propertyValue0, propertyValue1, propertyValue2);

	public static void Fatal<M>(Exception? exception, string message, params object?[]? propertyValues) => GetLogger(typeof(M).Name).Fatal(exception, message, propertyValues);
	#endregion
}
