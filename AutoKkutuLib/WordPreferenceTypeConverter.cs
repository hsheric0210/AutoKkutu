using System.ComponentModel;
using System.Globalization;

namespace AutoKkutuLib;

public class WordPreferenceTypeConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

	public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

	public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		if (value is string text)
		{
			var pieces = text.Split(';');
			var pieceCount = pieces.Length;
			var attributes = new WordCategories[pieceCount];
			for (var i = 0; i < pieceCount; i++)
			{
				var piece = pieces[i];
				if (!int.TryParse(piece, out var pieceInt))
					throw new InvalidOperationException($"Failed to parse WordPreference: Failed to parse number '{piece}' at piece index {i}");
				attributes[i] = (WordCategories)pieceInt;
			}

			return new WordPreference(attributes);
		}

		return base.ConvertFrom(context, culture, value);
	}

	public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
	{
		if (destinationType == null)
			throw new ArgumentNullException(nameof(destinationType));

		return value is WordPreference pref && CanConvertTo(context, destinationType)
			? string.Join(";", from attrib in pref.GetAttributes() select (int)attrib)
			: base.ConvertTo(context, culture, value, destinationType);
	}
}
