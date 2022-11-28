using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace AutoKkutu.Constants
{
	public class WordPreferenceTypeConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

		public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

		public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
		{
			if (value is string text)
			{
				string[] pieces = text.Split(';');
				int pieceCount = pieces.Length;
				var attributes = new WordType[pieceCount];
				for (int i = 0; i < pieceCount; i++)
				{
					string? piece = pieces[i];
					if (!int.TryParse(piece, out int pieceInt))
						throw new InvalidOperationException($"Failed to parse WordPreference: Failed to parse number '{piece}' at piece index {i}");
					attributes[i] = (WordType)pieceInt;
				}

				return new WordPreference(attributes);
			}

			return base.ConvertFrom(context, culture, value);
		}

		public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
		{
			if (destinationType == null)
				throw new ArgumentNullException(nameof(destinationType));

			if (value is WordPreference pref && CanConvertTo(context, destinationType))
				return string.Join(";", from attrib in pref.GetAttributes() select (int)attrib);

			return base.ConvertTo(context, culture, value, destinationType);
		}
	}
}
