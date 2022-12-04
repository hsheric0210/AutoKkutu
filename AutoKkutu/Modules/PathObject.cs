using AutoKkutu.Constants;
using AutoKkutu.Database;
using AutoKkutu.Database.Extension;
using AutoKkutu.Utils;
using AutoKkutu.Utils.Extension;
using Serilog;
using System.Globalization;
using System.Windows.Media;

namespace AutoKkutu.Modules;

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

	public PathObject(string content, WordCategories flags, int missionCharCount)
	{
		AutoKkutuColorPreference colorPref = AutoKkutuMain.ColorPreference;

		Content = content;
		Title = content;

		MakeEndAvailable = !flags.HasFlag(WordCategories.EndWord);
		MakeAttackAvailable = !flags.HasFlag(WordCategories.AttackWord);
		MakeNormalAvailable = !MakeEndAvailable || !MakeAttackAvailable;

		var isMissionWord = flags.HasFlag(WordCategories.MissionWord);
		string tooltipPrefix;

		var i = isMissionWord ? 0 : 1;
		Color? color = null;
		if (flags.HasFlag(WordCategories.EndWord))
		{
			PrimaryImage = @"images\skull.png";
		}
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

		ToolTip = string.Format(CultureInfo.CurrentCulture, tooltipPrefix, isMissionWord ? new object[2] { content, missionCharCount
} : new object[1] { content
});
	}

	public void MakeAttack(GameMode mode, AbstractDatabaseConnection connection)
	{
		var node = ToNode(mode);
		connection.DeleteNode(node, GetEndWordListTableName(mode));
		if (connection.AddNode(node, GetAttackWordListTableName(mode)))
			Log.Information(I18n.PathMark_Success, node, I18n.PathMark_Attack, mode);
		else
			Log.Warning(I18n.PathMark_AlreadyDone, node, I18n.PathMark_Attack, mode);
	}

	public void MakeEnd(GameMode mode, AbstractDatabaseConnection connection)
	{
		var node = ToNode(mode);
		connection.DeleteNode(node, GetAttackWordListTableName(mode));
		if (connection.AddNode(node, GetEndWordListTableName(mode)))
			Log.Information(I18n.PathMark_Success, node, I18n.PathMark_End, mode);
		else
			Log.Warning(I18n.PathMark_AlreadyDone, node, I18n.PathMark_End, mode);
	}

	public void MakeNormal(GameMode mode, AbstractDatabaseConnection connection)
	{
		var node = ToNode(mode);
		var endWord = connection.DeleteNode(node, GetEndWordListTableName(mode)) > 0;
		var attackWord = connection.DeleteNode(node, GetAttackWordListTableName(mode)) > 0;
		if (endWord || attackWord)
			Log.Information(I18n.PathMark_Success, node, I18n.PathMark_Normal, mode);
		else
			Log.Warning(I18n.PathMark_AlreadyDone, node, I18n.PathMark_Normal, mode);
	}

	private static string GetAttackWordListTableName(GameMode mode) => mode switch
	{
		GameMode.FirstAndLast => DatabaseConstants.ReverseAttackNodeIndexTableName,
		GameMode.Kkutu => DatabaseConstants.KkutuAttackNodeIndexTableName,
		_ => DatabaseConstants.AttackNodeIndexTableName,
	};

	private static string GetEndWordListTableName(GameMode mode) => mode switch
	{
		GameMode.FirstAndLast => DatabaseConstants.ReverseEndNodeIndexTableName,
		GameMode.Kkutu => DatabaseConstants.KkutuEndNodeIndexTableName,
		_ => DatabaseConstants.EndNodeIndexTableName,
	};

	private string ToNode(GameMode mode)
	{
		switch (mode)
		{
			case GameMode.FirstAndLast:
				return Content.GetFaLTailNode();

			case GameMode.MiddleAndFirst:
				if (Content.Length % 2 == 1)
					return Content.GetMaFTailNode();
				break;

			case GameMode.Kkutu:
				if (Content.Length > 2)
					return Content.GetKkutuTailNode();
				break;
		}

		return Content.GetLaFTailNode();
	}
}
