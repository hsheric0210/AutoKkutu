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
			EndWordColor = LoadColorFromConfig(nameof(EndWordColor), DefaultEndWordColor);
			AttackWordColor = LoadColorFromConfig(nameof(AttackWordColor), DefaultAttackWordColor);
			MissionWordColor = LoadColorFromConfig(nameof(MissionWordColor), DefaultMissionWordColor);
			EndMissionWordColor = LoadColorFromConfig(nameof(EndMissionWordColor), DefaultEndMissionWordColor);
			AttackMissionWordColor = LoadColorFromConfig(nameof(AttackMissionWordColor), DefaultAttackMissionWordColor);
		}

		private static Color LoadColorFromConfig(string key, Color defaultValue)
		{
			string value = ConfigurationManager.AppSettings[key];
			if (value == null || value.Length != 6 || !int.TryParse(value, NumberStyles.HexNumber, NumberFormatInfo.CurrentInfo, out var _))
				return defaultValue;

			byte red = Convert.ToByte(value.Substring(0, 2), 16);
			byte green = Convert.ToByte(value.Substring(2, 2), 16);
			byte blue = Convert.ToByte(value.Substring(4, 2), 16);
			return Color.FromRgb(red, green, blue);
		}

		public void SaveToConfig()
		{
			Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			var settings = configuration.AppSettings.Settings;
			WriteColorToConfig(settings, nameof(EndWordColor), EndWordColor);
			WriteColorToConfig(settings, nameof(AttackWordColor), AttackWordColor);
			WriteColorToConfig(settings, nameof(MissionWordColor), MissionWordColor);
			WriteColorToConfig(settings, nameof(EndMissionWordColor), EndMissionWordColor);
			WriteColorToConfig(settings, nameof(AttackMissionWordColor), AttackMissionWordColor);
			configuration.Save(ConfigurationSaveMode.Modified);
			ConfigurationManager.RefreshSection(configuration.AppSettings.SectionInformation.Name);
		}

		private static void WriteColorToConfig(KeyValueConfigurationCollection config, string key, Color color)
		{
			config.Remove(key);
			config.Add(key, string.Format(CultureInfo.InvariantCulture, "{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B));
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
