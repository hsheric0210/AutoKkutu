using System.Windows.Input;

namespace AutoKkutuGui;

public static class AutoKkutuCommands
{
	public static readonly RoutedUICommand ToggleAutoEnter = new("Toggle the Auto-enter feature", "ToggleAutoEnter", typeof(AutoKkutuCommands));
	public static readonly RoutedUICommand ToggleDelay = new("Toggle the delay feature", "ToggleDelay", typeof(AutoKkutuCommands));
	public static readonly RoutedUICommand ToggleAllDelay = new("Toggle the delay and fix delay feature", "ToggleAllDelay", typeof(AutoKkutuCommands));
}
