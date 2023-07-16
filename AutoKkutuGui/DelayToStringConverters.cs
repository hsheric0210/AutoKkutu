using System;
using System.Globalization;
using System.Windows.Data;

namespace AutoKkutuGui;
public class DelayToStringConverter : IMultiValueConverter
{
	protected virtual int PostCalculate(int delayMillis) => delayMillis;

	// parameter: String Format; must follow the template - 'min {0} max {1}'
	/// <summary>
	/// Receives base delay and randomization percentage and returns the minimum/maximum delay in formatted string.
	/// </summary>
	/// <param name="values">Base delay and Randomization percentage</param>
	/// <param name="targetType"></param>
	/// <param name="parameter">Output string format with two parameters; first parameter is minimum and second parameter is maximum delay</param>
	/// <param name="culture"></param>
	/// <returns></returns>
	public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
	{
		if (values == null || values.Length != 2)
			return "Invalid value";
		if (!int.TryParse((string)values[0], out var delay) || !int.TryParse((string)values[1], out var randomRatio))
			return string.Format(culture, (string)parameter, '-', '-'); // Failed to parse

		int a = delay + delay * randomRatio / 100, b = delay - delay * randomRatio / 100;

		// Post-calculation hook
		a = PostCalculate(a);
		b = PostCalculate(b);

		if (a < 0 || b < 0)
			return string.Format(culture, (string)parameter, "inf", "inf"); // Failed to post-process
		return string.Format(culture, (string)parameter, Math.Min(a, b), Math.Max(a, b));
	}

	public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException("This is an one-way converter");
}

public class DelayToCharPerSecondConverter : DelayToStringConverter
{
	protected override int PostCalculate(int delayMillis) => delayMillis == 0 ? -1 : 1000 / delayMillis;
}

public class DelayToCharPerMinuteConverter : DelayToStringConverter
{
	protected override int PostCalculate(int delayMillis) => delayMillis == 0 ? -1 : 60000 / delayMillis;
}
