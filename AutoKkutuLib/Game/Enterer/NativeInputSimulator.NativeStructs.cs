using System.Runtime.InteropServices;

namespace AutoKkutuLib.Game.Enterer;
public partial class NativeInputSimulator
{
	// Sending Keystrokes to Other Apps with Windows API and C# 
	// ; https://dzone.com/articles/sending-keys-other-apps

	// inputsimulator/WindowsInput/
	// ; https://github.com/michaelnoonan/inputsimulator/tree/master/WindowsInput

	// https://www.sysnet.pe.kr/2/0/12469 에서 가져옴

	[StructLayout(LayoutKind.Sequential)]
	internal struct MOUSEINPUT
	{
		public int X;
		public int Y;
		public uint MouseData;
		public uint Flags;
		public uint Time;
		public IntPtr ExtraInfo;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct KEYBDINPUT
	{
		public ushort Vk;
		public ushort Scan;
		public uint Flags;
		public uint Time;
		public IntPtr ExtraInfo;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct HARDWAREINPUT
	{
		public uint Msg;
		public ushort ParamL;
		public ushort ParamH;
	}

	[StructLayout(LayoutKind.Explicit)]
	internal struct MOUSEKEYBDHARDWAREINPUT
	{
		[FieldOffset(0)]
		public HARDWAREINPUT Hardware;

		[FieldOffset(0)]
		public KEYBDINPUT Keyboard;

		[FieldOffset(0)]
		public MOUSEINPUT Mouse;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct INPUT
	{
		public uint Type;
		public MOUSEKEYBDHARDWAREINPUT Data;
	}
}
