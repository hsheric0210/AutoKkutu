using System;
using System.Windows.Media;

namespace AutoKkutu;

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

	public override int GetHashCode() => HashCode.Combine(EndWordColor, AttackWordColor, MissionWordColor, EndMissionWordColor, AttackMissionWordColor);

	public override bool Equals(object? obj) => obj is AutoKkutuColorPreference other
			&& EndWordColor == other.EndWordColor
			&& AttackWordColor == other.AttackWordColor
			&& MissionWordColor == other.MissionWordColor
			&& EndMissionWordColor == other.EndMissionWordColor
			&& AttackMissionWordColor == other.AttackMissionWordColor;
}
