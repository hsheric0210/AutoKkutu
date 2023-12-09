using System.Runtime.InteropServices;

namespace AutoKkutuLib.Game.Enterer;
public partial class Win32InputSimulator : NativeInputSimulator
{
	public const string Name = "Win32InputSimulate";

	private readonly IList<INPUT> inputList = new List<INPUT>();

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

	private void AddInput(char ch, uint flags)
	{
		if (vkCodeMapping.TryGetValue(ch, out var vkCode))
		{
			var input = new INPUT { Type = 1 };
			input.Data.Keyboard = new KEYBDINPUT
			{
				Vk = vkCode,
				Scan = GetScanCode(vkCode),
				Flags = flags, // 0 to down, 2 to up
				Time = 0,
				ExtraInfo = IntPtr.Zero
			};

			inputList.Add(input);
			return;
		}

		LibLogger.Warn(EntererName, "VkCode not found for character {char}", ch);
	}

	protected override void KeyPress(char ch)
	{
		KeyDown(ch);
		KeyUp(ch);
	}

	protected override void KeyUp(char ch) => AddInput(ch, 2);

	protected override void KeyDown(char ch) => AddInput(ch, 0);
}
