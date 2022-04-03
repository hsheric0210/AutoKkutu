﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace AutoKkutu
{
	[SuppressUnmanagedCodeSecurity]
	internal class ConsoleManager
	{

		private const string Kernel32_DllName = "kernel32.dll";

		private const int MF_BYCOMMAND = 0;

		public const int SC_CLOSE = 61536;

		public enum LogType
		{
			Info,
			Warning,
			Error
		}

		[DllImport("kernel32.dll")]
		private static extern bool AllocConsole();

		[DllImport("kernel32.dll")]
		private static extern bool FreeConsole();

		[DllImport("kernel32.dll")]
		private static extern IntPtr GetConsoleWindow();

		[DllImport("kernel32.dll")]
		private static extern int GetConsoleOutputCP();

		public static bool HasConsole => GetConsoleWindow() != IntPtr.Zero;

		[DllImport("user32.dll")]
		public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

		[DllImport("user32.dll")]
		private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

		public static void Show()
		{
			if (!Debugger.IsAttached && !HasConsole)
			{
				AllocConsole();
				InvalidateOutAndError();
				Console.InputEncoding = Encoding.UTF8;
				Console.OutputEncoding = Encoding.UTF8;
				DeleteMenu(GetSystemMenu(GetConsoleWindow(), false), 61536, 0);
				Console.WriteLine("*** AutoKkutu v1.0 (Inspired from KKutu Helper)");
				Console.WriteLine("- Verbose console enabled because debugger is not attached.");
				Console.WriteLine("");
			}
		}

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

		private static void InvalidateOutAndError()
		{
			Type type = typeof(Console);
			FieldInfo _out = type.GetField("_out", BindingFlags.Static | BindingFlags.NonPublic);
			FieldInfo _error = type.GetField("_error", BindingFlags.Static | BindingFlags.NonPublic);
			MethodInfo _InitializeStdOutError = type.GetMethod("InitializeStdOutError", BindingFlags.Static | BindingFlags.NonPublic);
			Debug.Assert(_out != null);
			Debug.Assert(_error != null);
			Debug.Assert(_InitializeStdOutError != null);
			_out.SetValue(null, null);
			_error.SetValue(null, null);
			_InitializeStdOutError.Invoke(null, new object[] { true });
		}

		private static void SetOutAndErrorNull()
		{
			Console.SetOut(TextWriter.Null);
			Console.SetError(TextWriter.Null);
		}

		public static void Log(LogType Type, string content, string module) => Console.WriteLine($"[{Type}] {module} | {content}");
	}
}