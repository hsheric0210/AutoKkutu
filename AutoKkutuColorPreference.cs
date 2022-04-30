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

		public Color EndWordColor
		{
			get; set;
		}
		public Color AttackWordColor
		{
			get; set;
		}
		public Color MissionWordColor
		{
			get; set;
		}
		public Color EndMissionWordColor
		{
			get; set;
		}
		public Color AttackMissionWordColor
		{
			get; set;
		}

		public AutoKkutuColorPreference()
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

		public override bool Equals(object obj)
		{
			if (!(obj is AutoKkutuColorPreference))
				return false;
			AutoKkutuColorPreference other = (AutoKkutuColorPreference)obj;
			return EndWordColor == other.EndWordColor
				&& AttackWordColor == other.AttackWordColor
				&& MissionWordColor == other.MissionWordColor
				&& EndMissionWordColor == other.EndMissionWordColor
				&& AttackMissionWordColor == other.AttackMissionWordColor;
		}
	}
}
