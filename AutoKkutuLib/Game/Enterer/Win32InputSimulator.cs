using AutoKkutuLib.Hangul;
using Serilog;
using System.Runtime.InteropServices;

namespace AutoKkutuLib.Game.Enterer;
public partial class Win32InputSimulator : InputSimulatorBase
{
	public static event EventHandler? FocusToBrowser;

	public const string Name = "Win32InputSimulate";

	private bool hangulImeState;
	private bool shiftState;

	private IList<INPUT> inputList = new List<INPUT>();

	public Win32InputSimulator(IGame game) : base(Name, game)
	{
	}

	private void SetFocus()
	{
		SetForegroundWindow(game.Browser.GetWindowHandle());
		game.Browser.SetFocus();
		game.FocusChat();
		FocusToBrowser?.Invoke(null, EventArgs.Empty);
	}

	protected override async ValueTask SimulationStarted()
	{
		inputList.Clear();
		SetFocus();

		// 참고: 입력 도중에 사용자가 임의로 한영 키를 전환하는 것에 대해서는 대응이 불가능합니다

		//https://m.blog.naver.com/gostarst/220627552770
		var state1 = SendMessage(ImmGetDefaultIMEWnd(game.Browser.GetWindowHandle()), WM_IME_CONTROL, new IntPtr(IMC_GETOPENSTATUS), new IntPtr(0)).ToInt32() != 0;
		Log.Debug("Initial Hangul IME state (WM_IME_CONTROL.IMC_GETOPENSTATUS): {state}", hangulImeState);

		//https://kdsoft-zeros.tistory.com/160
		var imeHandle = ImmGetContext(game.Browser.GetWindowHandle());
		int dwConversion = 0, dwSentence = 0;
		ImmGetConversionStatus(imeHandle, ref dwConversion, ref dwSentence);
		var state2 = dwConversion != 0;
		Log.Debug("Initial Hangul IME state (ImmGetConversionStatus): {state}", hangulImeState);

		hangulImeState = state1 || state2;
	}

	protected async override ValueTask AppendAsync(EnterOptions options, InputCommand input)
	{
		ushort vkCode;
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

		// Shift키
		if (shiftState && input.ShiftState == ShiftState.Release)
		{
			KeyUp(VK_RSHIFT);
			shiftState = false;
			Log.Debug("Shift released.");
		}
		else if (!shiftState && input.ShiftState == ShiftState.Press)
		{
			KeyDown(VK_RSHIFT);
			shiftState = true;
			Log.Debug("Shift pressed.");
		}

		// IME 상태 업데이트
		if (hangulImeState && input.ImeState == ImeState.English)
		{
			KeyDown(VK_HANGUL);
			KeyUp(VK_HANGUL);
			hangulImeState = false;
			Log.Debug("IME state changed to English.");
		}
		else if (!hangulImeState && input.ImeState == ImeState.Korean)
		{
			KeyDown(VK_HANGUL);
			KeyUp(VK_HANGUL);
			hangulImeState = true;
			Log.Debug("IME state changed to Korean.");
		}

		KeyDown(vkCode);
		if (options.GetMaxDelayPerChar() > 0)
		{
			SetFocus();
			FlushInputBuffer();
		}

		await Task.Delay(options.DelayBeforeKeyUp);

		KeyUp(vkCode);
		if (options.GetMaxDelayPerChar() > 0)
		{
			SetFocus();
			FlushInputBuffer();
		}
	}

	protected override async ValueTask SimulationFinished()
	{
		if (shiftState)
		{
			Log.Verbose("Released shift key as the input simulation finished.");
			KeyUp(VK_RSHIFT); // Release shift key
		}

		FlushInputBuffer();
	}

	private void FlushInputBuffer()
	{
		if (inputList.Count > 0)
		{
			if (SendInput((uint)inputList.Count, inputList.ToArray(), Marshal.SizeOf(typeof(INPUT))) == 0)
				Log.Error("SendInput is blocked by other thread.");
			inputList.Clear();
			//await Task.Delay(30); // todo: make end-delay configurable
		}
	}

	protected override void SubmitInput()
	{
		KeyPress(vkCodeMapping['\n']);
		FlushInputBuffer();
	}

	protected override void ClearInput()
	{
		// Select all (CTRL+A)
		KeyDown(VK_LCTRL);
		KeyPress(vkCodeMapping['a']);
		KeyUp(VK_LCTRL);

		// Delete
		KeyPress(vkCodeMapping['\b']);
		FlushInputBuffer();
	}

	private void KeyPress(ushort vkCode)
	{
		KeyDown(vkCode);
		KeyUp(vkCode);
	}

	private void KeyDown(ushort vkCode)
	{
		var input = new INPUT { Type = 1 };
		input.Data.Keyboard = new KEYBDINPUT
		{
			Vk = vkCode,
			Scan = GetScanCode(vkCode),
			Flags = 0,
			Time = 0,
			ExtraInfo = IntPtr.Zero
		};

		inputList.Add(input);
	}

	private void KeyUp(ushort vkCode)
	{
		var input = new INPUT { Type = 1 };
		input.Data.Keyboard = new KEYBDINPUT
		{
			Vk = vkCode,
			Scan = GetScanCode(vkCode),
			Flags = 2,
			Time = 0,
			ExtraInfo = IntPtr.Zero
		};

		inputList.Add(input);
	}
}
