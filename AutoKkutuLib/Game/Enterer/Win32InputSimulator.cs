using AutoKkutuLib.Hangul;
using System.Runtime.InteropServices;

namespace AutoKkutuLib.Game.Enterer;
public class Win32InputSimulator : NativeInputSimulator
{
	public const string Name = "Win32InputSimulate";

	private IList<INPUT> inputList = new List<INPUT>();

	public Win32InputSimulator(IGame game) : base(Name, game)
	{
	}

	protected override async ValueTask SimulationStarted()
	{
		inputList.Clear();
		await base.SimulationStarted();
	}

	protected override void FlushInputBuffer()
	{
		if (inputList.Count > 0)
		{
			if (SendInput((uint)inputList.Count, inputList.ToArray(), Marshal.SizeOf(typeof(INPUT))) == 0)
				LibLogger.Error<Win32InputSimulator>("SendInput is blocked by other thread.");
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
