using AutoKkutuLib.Hangul;
using Serilog;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace AutoKkutuLib.Game.Enterer;
public class Win32InputSimulator : InputSimulatorBase
{
	public const string Name = "Win32InputSimulate";

	private static readonly IImmutableDictionary<char, int> vkCodeMapping;

	private const int WM_KEYDOWN = 0x0100;
	private const int WM_KEYUP = 0x0101;
	private const int WM_IME_CONTROL = 0x283;

	private const int VK_HANGUL = 0x15;
	private const int VK_RIGHT = 0x27;
	private const int VK_RSHIFT = 0xa1;

	private const uint MAPVK_VK_TO_VSC = 0;

	static Win32InputSimulator()
	{
		// https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
		var vkBuilder = ImmutableDictionary.CreateBuilder<char, int>();
		vkBuilder.Add('\t', 0x09);
		vkBuilder.Add('\n', 0x0d);
		vkBuilder.Add('0', 0x30);
		vkBuilder.Add('1', 0x31);
		vkBuilder.Add('2', 0x32);
		vkBuilder.Add('3', 0x33);
		vkBuilder.Add('4', 0x34);
		vkBuilder.Add('5', 0x35);
		vkBuilder.Add('6', 0x36);
		vkBuilder.Add('7', 0x37);
		vkBuilder.Add('8', 0x38);
		vkBuilder.Add('9', 0x39);
		vkBuilder.Add('a', 0x41);
		vkBuilder.Add('b', 0x42);
		vkBuilder.Add('c', 0x43);
		vkBuilder.Add('d', 0x44);
		vkBuilder.Add('e', 0x45);
		vkBuilder.Add('f', 0x46);
		vkBuilder.Add('g', 0x47);
		vkBuilder.Add('h', 0x48);
		vkBuilder.Add('i', 0x49);
		vkBuilder.Add('j', 0x4A);
		vkBuilder.Add('k', 0x4B);
		vkBuilder.Add('l', 0x4C);
		vkBuilder.Add('m', 0x4D);
		vkBuilder.Add('n', 0x4E);
		vkBuilder.Add('o', 0x4F);
		vkBuilder.Add('p', 0x50);
		vkBuilder.Add('q', 0x51);
		vkBuilder.Add('r', 0x52);
		vkBuilder.Add('s', 0x53);
		vkBuilder.Add('t', 0x54);
		vkBuilder.Add('u', 0x55);
		vkBuilder.Add('v', 0x56);
		vkBuilder.Add('w', 0x57);
		vkBuilder.Add('x', 0x58);
		vkBuilder.Add('y', 0x59);
		vkBuilder.Add('z', 0x5A);
		vkBuilder.Add('*', 0x6a);
		vkBuilder.Add('+', 0x6b);
		vkBuilder.Add('-', 0x6d);
		vkBuilder.Add('.', 0x6e);
		vkBuilder.Add('/', 0x6f);
		vkCodeMapping = vkBuilder.ToImmutable();
	}

	private readonly IntPtr hkl;
	private readonly int shiftScanCode;
	private readonly int hangulImeScanCode;

	private bool hangulImeState;
	private bool shiftState;

	public Win32InputSimulator(IGame game) : base(Name, game)
	{
		hkl = GetKeyboardLayout(0);
		shiftScanCode = MapVirtualKeyEx(VK_RSHIFT, MAPVK_VK_TO_VSC, hkl);
		hangulImeScanCode = MapVirtualKeyEx(VK_HANGUL, MAPVK_VK_TO_VSC, hkl);

		//https://m.blog.naver.com/gostarst/220627552770
		hangulImeState = SendMessage(game.Browser.GetWindowHandle(), WM_IME_CONTROL, new IntPtr(0x5), new IntPtr(0)).ToInt32() != 0;
	}

	protected override void SimulationStarted()
	{
		game.Browser.SetFocus();
		game.FocusChat();
	}

	protected async override ValueTask AppendAsync(EnterOptions options, InputCommand input)
	{
		int vkCode;
		if (input.Type == InputCommandType.ImeCompositionTermination)
		{
			vkCode = VK_RIGHT;
		}
		else if (vkCodeMapping.TryGetValue(input.Key, out var vk))
		{
			vkCode = vk;
		}
		else
		{
			Log.Warning("Win32-InputSimulator: VkCode not found for character {char}", input.Key);
			return;
		}

		// ScanCode 구하기
		var scanCode = MapVirtualKeyEx(vkCode, MAPVK_VK_TO_VSC, hkl);
		if (scanCode == 0)
			Log.Warning("There're no scan code available on the vk code {vk:x} (character {char})", vkCode, input.Key);

		// Shift키
		if (shiftState && input.ShiftState == ShiftState.Release)
		{
			KeyUp(VK_RSHIFT, shiftScanCode);
			shiftState = false;
			Log.Debug("Shift released.");
		}
		else if (!shiftState && input.ShiftState == ShiftState.Press)
		{
			KeyDown(VK_RSHIFT, shiftScanCode);
			shiftState = true;
			Log.Debug("Shift pressed.");
		}

		// IME 상태 업데이트
		if (hangulImeState && input.ImeState == ImeState.English)
		{
			KeyDown(VK_HANGUL, hangulImeScanCode);
			KeyUp(VK_HANGUL, hangulImeScanCode);
			hangulImeState = false;
			Log.Debug("IME state changed to English.");
		}
		else if (!hangulImeState && input.ImeState == ImeState.Korean)
		{
			KeyDown(VK_HANGUL, hangulImeScanCode);
			KeyUp(VK_HANGUL, hangulImeScanCode);
			hangulImeState = true;
			Log.Debug("IME state changed to Korean.");
		}

		KeyDown(vkCode, scanCode);
		await Task.Delay(options.DelayBeforeKeyUp);
		KeyUp(vkCode, scanCode);
	}

	private void KeyDown(int vk, int sc) => game.Browser.SendWin32KeyEvent(WM_KEYDOWN, vk, (sc << 16) | 0x00000001);

	private void KeyUp(int vk, int sc) => game.Browser.SendWin32KeyEvent(WM_KEYUP, vk, (int)((sc << 16) | 0xC0000001));

	/// <summary>
	/// Translates (maps) a virtual-key code into a scan code or character value, or translates a scan code into a virtual-key code.
	/// The function translates the codes using the input language and an input locale identifier.
	/// Starting with Windows Vista, the high byte of the uCode value can contain either 0xe0 or 0xe1 to specify the extended scan code.
	/// </summary>
	/// <param name="uCode">
	/// The virtual key code or scan code for a key. How this value is interpreted depends on the value of the uMapType parameter.
	/// Starting with Windows Vista, the high byte of the uCode value can contain either 0xe0 or 0xe1 to specify the extended scan code.
	/// </param>
	/// <param name="uMapType">
	/// The translation to perform. The value of this parameter depends on the value of the uCode parameter.
	/// </param>
	/// <param name="dwhkl">
	/// Input locale identifier to use for translating the specified code.
	/// This parameter can be any input locale identifier previously returned by the LoadKeyboardLayout function.
	/// </param>
	/// <returns>
	/// The return value is either a scan code, a virtual-key code, or a character value, depending on the value of uCode and uMapType.
	/// If there is no translation, the return value is zero.
	/// </returns>
	/// <see>https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-mapvirtualkeyexw</see>
	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern int MapVirtualKeyEx(
	  [In] int uCode,
	  [In] uint uMapType,
	  IntPtr dwhkl
	);

	/// <summary>
	/// Retrieves the active input locale identifier (formerly called the keyboard layout).
	/// </summary>
	/// <param name="idThread">The identifier of the thread to query, or 0 for the current thread.</param>
	/// <returns>
	/// The return value is the input locale identifier for the thread.
	/// The low word contains a Language Identifier for the input language and the high word contains a device handle to the physical layout of the keyboard.
	/// </returns>
	[DllImport("user32.dll")]
	private static extern IntPtr GetKeyboardLayout([In] uint idThread);

	/// <summary>
	/// Retrieves the default window handle to the IME class.
	/// </summary>
	/// <returns>
	/// Returns the default window handle to the IME class if successful, or NULL otherwise.
	/// </returns>
	[DllImport("imm32.dll")]
	private static extern IntPtr ImmGetDefaultIMEWnd([In] IntPtr hwnd);

	/// <summary>
	/// Sends the specified message to a window or windows.
	/// The SendMessage function calls the window procedure for the specified window
	/// and does not return until the window procedure has processed the message.
	/// </summary>
	/// <param name="hWnd">
	/// A handle to the window whose window procedure will receive the message.
	/// If this parameter is HWND_BROADCAST ((HWND)0xffff), the message is sent to all top-level windows in the system,
	/// including disabled or invisible unowned windows, overlapped windows, and pop-up windows; but the message is not sent to child windows.
	/// </param>
	/// <param name="Msg">The message to be sent.</param>
	/// <param name="wParam">Additional message-specific information.</param>
	/// <param name="IParam">Additional message-specific information.</param>
	/// <returns>The return value specifies the result of the message processing; it depends on the message sent.</returns>
	[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	static extern IntPtr SendMessage(
		[In] IntPtr hWnd,
		[In] uint Msg,
		[In] IntPtr wParam,
		[In] IntPtr IParam);
}
