using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace AutoKkutuLib.Game.Enterer;
public partial class Win32InputSimulator
{
	private static readonly IImmutableDictionary<char, ushort> vkCodeMapping;
	private static readonly IDictionary<ushort, ushort> scanCodeMapping;

	private static readonly IntPtr keyboardLayout;

	private const uint MAPVK_VK_TO_VSC = 0;

	static Win32InputSimulator()
	{
		// https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
		var vkBuilder = ImmutableDictionary.CreateBuilder<char, ushort>();
		vkBuilder.Add('\b', 0x08);
		vkBuilder.Add('\t', 0x09);
		vkBuilder.Add('\n', 0x0d);
		vkBuilder.Add(SPECIAL_HANGUL, 0x15); // VK_HANGUL
		vkBuilder.Add(' ', 0x20);
		vkBuilder.Add(SPECIAL_RIGHT, 0x27); // VK_RIGHT
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
		vkBuilder.Add(SPECIAL_RSHIFT, 0xa1); // VK_RSHIFT
		vkBuilder.Add(SPECIAL_LCTRL, 0xa2); // VK_LCTRL
		vkCodeMapping = vkBuilder.ToImmutable();

		keyboardLayout = GetKeyboardLayout(0);
		var scBuilder = new Dictionary<ushort, ushort>();

		foreach ((var _, var vkCode) in vkCodeMapping)
			scBuilder.Add(vkCode, MapVirtualKeyEx(vkCode, MAPVK_VK_TO_VSC, keyboardLayout));
		scanCodeMapping = scBuilder;
	}

	private static ushort GetScanCode(ushort vkCode)
	{
		if (!scanCodeMapping.ContainsKey(vkCode))
			scanCodeMapping.Add(vkCode, MapVirtualKeyEx(vkCode, MAPVK_VK_TO_VSC, keyboardLayout));

		return scanCodeMapping[vkCode];
	}

	[DllImport("user32.dll")]
	private static extern IntPtr GetKeyboardLayout([In] uint idThread);

	[DllImport("user32.dll", CharSet = CharSet.Auto)]
	private static extern ushort MapVirtualKeyEx([In] int uCode, [In] uint uMapType, IntPtr dwhkl);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern uint SendInput(uint numberOfInputs, INPUT[] inputs, int sizeOfInputStructure);
}
