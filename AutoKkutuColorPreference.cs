using System;
using System.Configuration;
using System.Globalization;
using System.Windows.Media;

namespace AutoKkutu
{
	public class AutoKkutuColorPreference
	{
		public static readonly Color DefaultEndWordColor = Color.FromRgb(0xFF, 0x11, 0x00);
		public static readonly Color DefaultAttackWordColor = Color.FromRgb(0xFF, 0x80, 0x00);
		public static readonly Color DefaultMissionWordColor = Color.FromRgb(0x40, 0xFF, 0x40);
		public static readonly Color DefaultEndMissionWordColor = Color.FromRgb(0x20, 0xC0, 0xA8);
		public static readonly Color DefaultAttackMissionWordColor = Color.FromRgb(0xFF, 0xFF, 0x40);

		public Color EndWordColor = DefaultEndWordColor;
		public Color AttackWordColor = DefaultAttackWordColor;
		public Color MissionWordColor = DefaultMissionWordColor;
		public Color EndMissionWordColor = DefaultEndMissionWordColor;
		public Color AttackMissionWordColor = DefaultAttackMissionWordColor;

		public AutoKkutuColorPreference()
		{
			// HTML color format: '#' + Convert.ToString(EndWordColor.ToArgb(), 16);
		}

		public void LoadFromConfig()
		{
			EndWordColor = FromConfig(nameof(EndWordColor), DefaultEndWordColor);
			AttackWordColor = FromConfig(nameof(AttackWordColor), DefaultAttackWordColor);
			MissionWordColor = FromConfig(nameof(MissionWordColor), DefaultMissionWordColor);
			EndMissionWordColor = FromConfig(nameof(EndMissionWordColor), DefaultEndMissionWordColor);
			AttackMissionWordColor = FromConfig(nameof(AttackMissionWordColor), DefaultAttackMissionWordColor);
		}

		private static Color FromConfig(string key, Color defaultValue)
		{
			string value = ConfigurationManager.AppSettings[key];
			if (value.Length != 6 || !int.TryParse(value, NumberStyles.HexNumber, NumberFormatInfo.CurrentInfo, out var color))
				return defaultValue;

			byte red = Convert.ToByte(value.Substring(0, 2), 16);
			byte green = Convert.ToByte(value.Substring(2, 2), 16);
			byte blue = Convert.ToByte(value.Substring(4, 2), 16);
			return Color.FromRgb(red, green, blue);
		}

		public override int GetHashCode() => HashCode.Combine(EndWordColor, AttackWordColor, MissionWordColor, EndMissionWordColor, AttackMissionWordColor);
	}
}
