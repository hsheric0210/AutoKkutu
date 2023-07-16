using AutoKkutuLib;
using System;
using System.Globalization;
using System.Windows.Media;

namespace AutoKkutuGui;

public record GuiPathObject
{
	public PathObject Underlying { get; }

	public string Color
	{
		get;
	} = "#FFFFFFFF";

	public string Content => Underlying.Content;

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

	public string Title => Underlying.Content;

	public string ToolTip
	{
		get;
	}

	public bool AlreadyUsed => Underlying.AlreadyUsed;
	public bool Excluded => Underlying.Excluded;
	public bool RemoveQueued => Underlying.RemoveQueued;

	public string Decorations => Underlying.AlreadyUsed || Underlying.Excluded || Underlying.RemoveQueued ? "Strikethrough" : "None";

	public string FontWeight => Underlying.RemoveQueued ? "Bold" : "Normal";

	public string FontStyle => Underlying.RemoveQueued ? "Italic" : "Normal";

	public GuiPathObject(PathObject pathObject)
	{
		if (pathObject is null)
			throw new ArgumentNullException(nameof(pathObject));

		Underlying = pathObject;

		var prefs = Main.GetInstance().Preference;

		var flags = pathObject.Categories;
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
				color = prefs.EndMissionWordColor;
				break;

			case 1:
				// End word
				tooltipPrefix = I18n.PathTooltip_End;
				color = prefs.EndWordColor;
				break;

			case 2:
				// Attack mission word
				tooltipPrefix = I18n.PathTooltip_AttackMission;
				color = prefs.AttackMissionWordColor;
				break;

			case 3:
				// Attack word
				tooltipPrefix = I18n.PathTooltip_Attack;
				color = prefs.AttackWordColor;
				break;

			case 4:
				// Mission word
				color = prefs.MissionWordColor;
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
