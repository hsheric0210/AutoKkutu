using System.Runtime.InteropServices;

namespace AutoKkutuLib.Game.Enterer;
public partial class Win32InputSimulator
{
	[DllImport("user32.dll", SetLastError = true)]
	private static extern uint SendInput(uint numberOfInputs, INPUT[] inputs, int sizeOfInputStructure);
}
