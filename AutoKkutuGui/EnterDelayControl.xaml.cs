using System.Globalization;
using System.Windows.Controls;
using static AutoKkutuGui.GuiUtils;

namespace AutoKkutuGui;
/// <summary>
/// Interaction logic for EnterDelayControl.xaml
/// </summary>
public partial class EnterDelayControl : Grid
{
	public EnterDelayControl()
	{
		InitializeComponent();
	}

	public void SetConfig(EnterDelayConfig config)
	{
		Delay.IsChecked = config.IsEnabled;
		StartDelayNumber.Text = config.StartDelay.ToString(CultureInfo.InvariantCulture);
		StartDelayRandomNumber.Text = config.StartDelayRandom.ToString(CultureInfo.InvariantCulture);
		DelayPerCharNumber.Text = config.DelayPerChar.ToString(CultureInfo.InvariantCulture);
		DelayPerCharRandomNumber.Text = config.DelayPerCharRandom.ToString(CultureInfo.InvariantCulture);
	}

	public EnterDelayConfig GetConfig()
	{
		var startDelay = Parse("start delay", StartDelayNumber.Text, 10);
		var startDelayRandom = Parse("start delay randomization", StartDelayRandomNumber.Text, 10);
		var delayPerChar = Parse("delay per char", DelayPerCharNumber.Text, 10);
		var delayPerCharRandom = Parse("delay per char randomization", DelayPerCharRandomNumber.Text, 10);
		return new EnterDelayConfig(Delay.IsChecked ?? false, startDelay, startDelayRandom, delayPerChar, delayPerCharRandom);
	}
}
