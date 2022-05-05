using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace AutoKkutu
{
	public static class ConsoleManager
	{
		private const string User32_DllName = "user32.dll";
		private const string Kernel32_DllName = "kernel32.dll";
		private const string ProgramIntroduction = $"*** AutoKkutu v{MainWindow.VERSION}";
		private const string ConsoleIntroduction = "- Verbose console enabled because debugger is not attached.";

		private const int MF_BYCOMMAND = 0;

		private const int SC_CLOSE = 61536;

		public static bool HasConsole => GetConsoleWindow() != IntPtr.Zero;

		[DllImport(Kernel32_DllName)]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		private static extern bool AllocConsole();

		[DllImport(Kernel32_DllName)]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		private static extern bool FreeConsole();

		[DllImport(Kernel32_DllName)]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		private static extern IntPtr GetConsoleWindow();

		[DllImport(Kernel32_DllName)]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		private static extern int GetConsoleOutputCP();

		[DllImport(User32_DllName)]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		private static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

		[DllImport(User32_DllName)]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

		public static void Show()
		{
			if (!Debugger.IsAttached && !HasConsole)
			{
				if (!AllocConsole())
					DrawErrorBox("Failed to allocate the console.");
				Console.InputEncoding = Encoding.UTF8;
				Console.OutputEncoding = Encoding.UTF8;
				if (DeleteMenu(GetSystemMenu(GetConsoleWindow(), false), SC_CLOSE, MF_BYCOMMAND) == 0)
					DrawErrorBox("Failed to delete console menu items.");
				Console.WriteLine(ProgramIntroduction);
				Console.WriteLine(ConsoleIntroduction);
				Console.WriteLine("");
			}
		}

		private static void DrawErrorBox(string errorMessage) => MessageBox.Show(errorMessage, nameof(ConsoleManager), MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);

		public static void Hide()
		{
			if (HasConsole)
			{
				SetOutAndErrorNull();
				FreeConsole();
			}
		}

		public static void Toggle()
		{
			if (HasConsole)
				Hide();
			else
				Show();
		}

		private static void SetOutAndErrorNull()
		{
			Console.SetOut(TextWriter.Null);
			Console.SetError(TextWriter.Null);
		}
	}
}
