using Serilog;

namespace AutoKkutuGui;
internal static class GuiUtils
{
	internal static int Parse(string elementName, string valueString, int defaultValue)
	{
		if (int.TryParse(valueString, out var _delay))
			return _delay;

		Log.Warning("Can't parse {elemName} value {string}, will be reset to {default:l}.", elementName, valueString, defaultValue);
		return defaultValue;
	}
}
