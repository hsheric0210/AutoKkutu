using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
			InitializeComponent();
			EndWordColor.SelectedColor = config.EndWordColor;
			AttackWordColor.SelectedColor = config.AttackWordColor;
			MissionWordColor.SelectedColor = config.MissionWordColor;
			EndMissionWordColor.SelectedColor = config.EndMissionWordColor;
			AttackMissionWordColor.SelectedColor = config.AttackMissionWordColor;
		}

		private static void UpdateConfigColor(KeyValueConfigurationCollection config, string key, Color color)
		{
			config.Remove(key);
			config.Add(key, color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2"));
		}

		private void Submit_Click(object sender, RoutedEventArgs e)
		{
			var newPref = new AutoKkutuColorPreference
			{
				EndWordColor = EndWordColor.SelectedColor ?? AutoKkutuColorPreference.DefaultEndWordColor,
				AttackWordColor = AttackWordColor.SelectedColor ?? AutoKkutuColorPreference.DefaultAttackWordColor,
				MissionWordColor = MissionWordColor.SelectedColor ?? AutoKkutuColorPreference.DefaultMissionWordColor,
				EndMissionWordColor = EndMissionWordColor.SelectedColor ?? AutoKkutuColorPreference.DefaultEndMissionWordColor,
				AttackMissionWordColor = AttackMissionWordColor.SelectedColor ?? AutoKkutuColorPreference.DefaultAttackMissionWordColor,
			};

			try
			{
				Configuration conf = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
				var collection = conf.AppSettings.Settings;
				UpdateConfigColor(collection, nameof(EndWordColor), newPref.EndWordColor);
				UpdateConfigColor(collection, nameof(AttackWordColor), newPref.AttackWordColor);
				UpdateConfigColor(collection, nameof(MissionWordColor), newPref.MissionWordColor);
				UpdateConfigColor(collection, nameof(EndMissionWordColor), newPref.EndMissionWordColor);
				UpdateConfigColor(collection, nameof(AttackMissionWordColor), newPref.AttackMissionWordColor);
				conf.Save(ConfigurationSaveMode.Modified);
				ConfigurationManager.RefreshSection(conf.AppSettings.SectionInformation.Name);
			}
			catch (Exception ex)
			{
				Logger.Error("Failed to save color preference", ex);
			}

			Dispatcher.Invoke(() =>
			{
				try
				{
					MainWindow.UpdateColorPreference(newPref);
				}
				catch (Exception ex)
				{
					Logger.Error("Failed to apply color preference", ex);
				}
			});

			Close();
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
