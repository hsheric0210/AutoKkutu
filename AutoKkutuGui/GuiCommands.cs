using System.Windows.Input;

namespace AutoKkutuGui;

public static class GuiCommands
{
	public static readonly RoutedUICommand ToggleAutoEnter = new("Toggle the Auto-enter feature", "ToggleAutoEnter", typeof(GuiCommands));
	public static readonly RoutedUICommand ToggleDelay = new("Toggle the delay feature", "ToggleDelay", typeof(GuiCommands));
	public static readonly RoutedUICommand ToggleAllDelay = new("Toggle the delay and fix delay feature", "ToggleAllDelay", typeof(GuiCommands));
}
