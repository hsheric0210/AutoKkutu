using AutoKkutuLib;
using AutoKkutuLib.Database;
using AutoKkutuLib.Database.Extension;
using AutoKkutuLib.Extension;
using Serilog;
using System;
using System.Globalization;
using System.Windows.Media;

namespace AutoKkutuGui;

public record GuiPathObject
{
	private readonly PathObject pathObject;

	public string Color
	{
		get;
	} = "#FFFFFFFF";

	public string Content => pathObject.Content;

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

	public string Title => pathObject.Content;

	public string ToolTip
	{
		get;
	}

	public string Decorations => pathObject.AlreadyUsed || pathObject.Excluded || pathObject.RemoveQueued ? "Strikethrough" : "None";

	public string FontWeight => pathObject.RemoveQueued ? "Bold" : "Normal";

	public string FontStyle => pathObject.RemoveQueued ? "Italic" : "Normal";

	public GuiPathObject(PathObject pathObject)
	{
		if (pathObject is null)
			throw new ArgumentNullException(nameof(pathObject));

		this.pathObject = pathObject;

		ColorPreference colorPref = Main.ColorPreference;

		WordCategories flags = pathObject.Categories;
		MakeEndAvailable = !flags.HasFlag(WordCategories.EndWord);
		MakeAttackAvailable = !flags.HasFlag(WordCategories.AttackWord);
		MakeNormalAvailable = !MakeEndAvailable || !MakeAttackAvailable;

		var isMissionWord = flags.HasFlag(WordCategories.MissionWord);
		string tooltipPrefix;

		var i = isMissionWord ? 0 : 1;
		Color? color = null;
		if (flags.HasFlag(WordCategories.EndWord))
			PrimaryImage = @"images\skull.png";
		else if (flags.HasFlag(WordCategories.AttackWord))
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

		ToolTip = string.Format(CultureInfo.CurrentCulture, tooltipPrefix, isMissionWord ? new object[2] { pathObject.Content, pathObject.MissionCharCount } : new object[1] { pathObject.Content });
	}
}
