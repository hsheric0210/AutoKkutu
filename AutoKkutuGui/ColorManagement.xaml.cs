using Serilog;
using System;
using System.Windows;

namespace AutoKkutuGui;

// TODO: ColorManagement 창 완전 제거 후 ConfigWindow와 통합.
/// <summary>
/// ColorManagement.xaml에 대한 상호 작용 논리
/// </summary>
public partial class ColorManagement : Window
{
	public ColorManagement(ColorPreference config)
	{
		if (config == null)
			throw new ArgumentNullException(nameof(config));

		InitializeComponent();
		EndWordColor.SelectedColor = config.EndWordColor;
		AttackWordColor.SelectedColor = config.AttackWordColor;
		MissionWordColor.SelectedColor = config.MissionWordColor;
		EndMissionWordColor.SelectedColor = config.EndMissionWordColor;
		AttackMissionWordColor.SelectedColor = config.AttackMissionWordColor;
	}

	private void OnSubmit(object sender, RoutedEventArgs e)
	{
		var newPref = new ColorPreference
		{
			EndWordColor = EndWordColor.SelectedColor ?? ColorPreference.DefaultEndWordColor,
			AttackWordColor = AttackWordColor.SelectedColor ?? ColorPreference.DefaultAttackWordColor,
			MissionWordColor = MissionWordColor.SelectedColor ?? ColorPreference.DefaultMissionWordColor,
			EndMissionWordColor = EndMissionWordColor.SelectedColor ?? ColorPreference.DefaultEndMissionWordColor,
			AttackMissionWordColor = AttackMissionWordColor.SelectedColor ?? ColorPreference.DefaultAttackMissionWordColor,
		};

		try
		{
			Settings config = Settings.Default;
			config.EndWordColor = newPref.EndWordColor.ToDrawingColor();
			config.AttackWordColor = newPref.AttackWordColor.ToDrawingColor();
			config.MissionWordColor = newPref.MissionWordColor.ToDrawingColor();
			config.EndMissionWordColor = newPref.EndMissionWordColor.ToDrawingColor();
			config.AttackMissionWordColor = newPref.AttackMissionWordColor.ToDrawingColor();
			Settings.Default.Save();
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Failed to save the configuration.");
		}

		Main.ColorPreference = newPref;
		Close();
	}

	private void OnCancel(object sender, RoutedEventArgs e) => Close();
}
