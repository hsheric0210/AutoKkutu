using System.Runtime.InteropServices;

namespace AutoKkutuLib.Game.Enterer;
public partial class NativeInputSimulator
{
	protected const char SPECIAL_HANGUL = '∈';
	protected const char SPECIAL_RIGHT = '∋';
	protected const char SPECIAL_RSHIFT = '⊆';
	protected const char SPECIAL_LCTRL = '⊇';

	protected const uint WM_IME_CONTROL = 643;

	protected const uint IMC_GETOPENSTATUS = 0x5;

	// Native imports

	[DllImport("imm32.dll")]
	private static extern IntPtr ImmGetContext(IntPtr hwnd);

	[DllImport("imm32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool ImmGetConversionStatus(IntPtr himc, ref int lpdw, ref int lpdw2);

	[DllImport("imm32.dll")]
	private static extern IntPtr ImmGetDefaultIMEWnd(IntPtr hWnd);

	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	static extern IntPtr SendMessage([In] IntPtr hWnd, [In] uint Msg, [In] IntPtr wParam, [In] IntPtr IParam);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool SetForegroundWindow([In] IntPtr hWnd);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern IntPtr GetForegroundWindow();
}
