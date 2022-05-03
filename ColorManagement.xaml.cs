using log4net;
using System;
using System.Windows;

namespace AutoKkutu
{
	/// <summary>
	/// ColorManagement.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class ColorManagement : Window
	{
		private static readonly ILog Logger = LogManager.GetLogger(nameof(ColorManagement));

		public ColorManagement(AutoKkutuColorPreference config)
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
			var newPreference = new AutoKkutuColorPreference
			{
				EndWordColor = EndWordColor.SelectedColor ?? AutoKkutuColorPreference.DefaultEndWordColor,
				AttackWordColor = AttackWordColor.SelectedColor ?? AutoKkutuColorPreference.DefaultAttackWordColor,
				MissionWordColor = MissionWordColor.SelectedColor ?? AutoKkutuColorPreference.DefaultMissionWordColor,
				EndMissionWordColor = EndMissionWordColor.SelectedColor ?? AutoKkutuColorPreference.DefaultEndMissionWordColor,
				AttackMissionWordColor = AttackMissionWordColor.SelectedColor ?? AutoKkutuColorPreference.DefaultAttackMissionWordColor,
			};

			try
			{
				newPreference.SaveToConfig();
			}
			catch (Exception ex)
			{
				Logger.Error("Failed to save color preference", ex);
			}

			Dispatcher.Invoke(() =>
			{
				try
				{
					MainWindow.UpdateColorPreference(newPreference);
				}
				catch (Exception ex)
				{
					Logger.Error("Failed to apply color preference", ex);
				}
			});

			Close();
		}

		private void OnCancel(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
