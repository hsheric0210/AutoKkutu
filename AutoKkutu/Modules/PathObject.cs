using AutoKkutu.Constants;
using AutoKkutu.Databases;
using AutoKkutu.Databases.Extension;
using AutoKkutu.Utils;
using Serilog;
using System.Globalization;
using System.Windows.Media;

namespace AutoKkutu.Modules
{
	public class PathObject
	{
		public string Color
		{
			get;
		} = "#FFFFFFFF";

		public string Content
		{
			get;
		}

		public bool MakeAttackAvailable
		{
			get;
		}

		public bool MakeEndAvailable
		{
			get;
		}

		public bool MakeNormalAvailable
		{
			get;
		}

		public string PrimaryImage
		{
			get;
		}

		public string SecondaryImage
		{
			get;
		}

		public string Title
		{
			get;
		}

		public string ToolTip
		{
			get;
		}

		public string Decorations => AlreadyUsed || Excluded || RemoveQueued ? "Strikethrough" : "None";

		public string FontWeight => RemoveQueued ? "Bold" : "Normal";

		public string FontStyle => RemoveQueued ? "Italic" : "Normal";

		public bool AlreadyUsed
		{
			get; set;
		}

		public bool Excluded
		{
			get; set;
		}

		public bool RemoveQueued
		{
			get; set;
		}

		public PathObject(string content, WordType flags, int missionCharCount)
		{
			AutoKkutuColorPreference colorPref = AutoKkutuMain.ColorPreference;

			Content = content;
			Title = content;

			MakeEndAvailable = !flags.HasFlag(WordType.EndWord);
			MakeAttackAvailable = !flags.HasFlag(WordType.AttackWord);
			MakeNormalAvailable = !MakeEndAvailable || !MakeAttackAvailable;

			bool isMissionWord = flags.HasFlag(WordType.MissionWord);
			string tooltipPrefix;

			int i = isMissionWord ? 0 : 1;
			Color? color = null;
			if (flags.HasFlag(WordType.EndWord))
			{
				PrimaryImage = @"images\skull.png";
			}
			else if (flags.HasFlag(WordType.AttackWord))
			{
				PrimaryImage = @"images\attack.png";
				i += 2;
			}
			else
			{
				PrimaryImage = string.Empty;
				i += 4;
			}

			switch (i)
			{
				case 0:
					// End mission word
					tooltipPrefix = I18n.PathTooltip_EndMission;
					color = colorPref.EndMissionWordColor;
					break;

				case 1:
					// End word
					tooltipPrefix = I18n.PathTooltip_End;
					color = colorPref.EndWordColor;
					break;

				case 2:
					// Attack mission word
					tooltipPrefix = I18n.PathTooltip_AttackMission;
					color = colorPref.AttackMissionWordColor;
					break;

				case 3:
					// Attack word
					tooltipPrefix = I18n.PathTooltip_Attack;
					color = colorPref.AttackWordColor;
					break;

				case 4:
					// Mission word
					color = colorPref.MissionWordColor;
					tooltipPrefix = I18n.PathTooltip_Mission;
					break;

				default:
					// Normal word
					tooltipPrefix = I18n.PathTooltip_Normal;
					break;
			}

			if (color is not null)
				Color = ((Color)color).ToString(CultureInfo.InvariantCulture);

			SecondaryImage = isMissionWord ? @"images\mission.png" : string.Empty;

			ToolTip = string.Format(tooltipPrefix, isMissionWord ? new object[2] { content, missionCharCount } : new object[1] { content });
		}
	}
}
