using AutoKkutuLib.Hangul;

namespace AutoKkutuLib.Game.Enterer;
public abstract partial class NativeInputSimulator : InputSimulatorBase
{
	public static event EventHandler? FocusToBrowser;

	private bool hangulImeState;
	private bool shiftState;

	protected NativeInputSimulator(string name, IGame game) : base(name, game)
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
		SetFocus();

		// 참고: 입력 도중에 사용자가 임의로 한영 키를 전환하는 것에 대해서는 대응이 불가능합니다

		//https://m.blog.naver.com/gostarst/220627552770
		var state1 = SendMessage(ImmGetDefaultIMEWnd(game.Browser.GetWindowHandle()), WM_IME_CONTROL, new IntPtr(IMC_GETOPENSTATUS), new IntPtr(0)).ToInt32() != 0;
		LibLogger.Debug(EntererName, "Initial Hangul IME state (WM_IME_CONTROL.IMC_GETOPENSTATUS): {state}", hangulImeState);

		//https://kdsoft-zeros.tistory.com/160
		var imeHandle = ImmGetContext(game.Browser.GetWindowHandle());
		int dwConversion = 0, dwSentence = 0;
		ImmGetConversionStatus(imeHandle, ref dwConversion, ref dwSentence);
		var state2 = dwConversion != 0;
		LibLogger.Debug(EntererName, "Initial Hangul IME state (ImmGetConversionStatus): {state}", hangulImeState);

		hangulImeState = state1 || state2;
	}

	protected async override ValueTask AppendAsync(EnterOptions options, InputCommand input)
	{
		// Shift키
		if (shiftState && input.ShiftState == ShiftState.Release)
		{
			KeyPress(SPECIAL_RSHIFT);
			shiftState = false;
			LibLogger.Debug(EntererName, "Shift released.");
		}
		else if (!shiftState && input.ShiftState == ShiftState.Press)
		{
			KeyDown(SPECIAL_RSHIFT);
			shiftState = true;
			LibLogger.Debug(EntererName, "Shift pressed.");
		}

		// IME 상태 업데이트
		if (hangulImeState && input.ImeState == ImeState.English)
		{
			KeyPress(SPECIAL_HANGUL);
			hangulImeState = false;
			LibLogger.Debug(EntererName, "IME state changed to English.");
		}
		else if (!hangulImeState && input.ImeState == ImeState.Korean)
		{
			KeyPress(SPECIAL_HANGUL);
			hangulImeState = true;
			LibLogger.Debug(EntererName, "IME state changed to Korean.");
		}

		var ch = input.Type == InputCommandType.ImeCompositionTermination ? SPECIAL_RIGHT : input.Key;
		KeyDown(ch);
		if (options.GetMaxDelayBeforeNextChar() > 0)
		{
			SetFocus();
			FlushInputBuffer();
		}

		await Task.Delay(options.GetDelayBeforeKeyUp());

		KeyUp(ch);
		if (options.GetMaxDelayBeforeNextChar() > 0)
		{
			SetFocus();
			FlushInputBuffer();
		}
	}

	protected override async ValueTask SimulationFinished()
	{
		if (shiftState)
		{
			LibLogger.Verbose(EntererName, "Released shift key as the input simulation finished.");
			KeyUp(SPECIAL_RSHIFT); // Release shift key
		}

		FlushInputBuffer();
	}

	protected abstract void FlushInputBuffer();

	protected override void SubmitInput()
	{
		KeyPress('\n');
		FlushInputBuffer();
	}

	protected override void ClearInput()
	{
		// Select all (CTRL+A)
		KeyDown(SPECIAL_LCTRL);
		KeyPress('a');
		KeyUp(SPECIAL_LCTRL);

		// Delete
		KeyPress('\b');
		FlushInputBuffer();
	}

	protected abstract void KeyPress(char ch);
	protected abstract void KeyUp(char ch);
	protected abstract void KeyDown(char ch);
}
