using CefSharp;
using System.Xml.Serialization;

namespace AutoKkutuLib.CefSharp;
[XmlRoot("CefSharp")]
public class CefConfigDto
{
	[XmlElement]
	public string JavaScriptInjectionBaseNamespace { get; set; } = "window";

	[XmlElement]
	public string MainPage { get; set; }

	[XmlElement]
	public string LogFile { get; set; }

	[XmlElement]
	public string UserAgent { get; set; }

	[XmlElement]
	public string CachePath { get; set; }

	[XmlElement]
	public string RootCachePath { get; set; }

	[XmlElement]
	public bool IgnoreCertificateErrors { get; set; }

	[XmlArray("CommandLineArguments")]
	[XmlArrayItem("Argument")]
	public List<string> CefCommandLineArgs { get; set; }

	[XmlElement]
	public string ProxyIp { get; set; }

	[XmlElement]
	public int ProxyPort { get; set; }

	[XmlElement]
	public string ProxyAuthUserName { get; set; }

	[XmlElement]
	public string ProxyAuthPassword { get; set; }

	[XmlElement]
	public LogSeverity LogSeverity { get; set; }

	[XmlElement]
	public string ResourcesDirPath { get; set; }

	[XmlElement]
	public string JavascriptFlags { get; set; }

	[XmlElement]
	public bool PackLoadingDisabled { get; set; }

	[XmlElement]
	public string UserAgentProduct { get; set; }

	[XmlElement]
	public string LocalesDirPath { get; set; }

	[XmlElement]
	public int RemoteDebuggingPort { get; set; }

	[XmlElement]
	public bool WindowlessRenderingEnabled { get; set; } = true;

	[XmlElement]
	public bool PersistSessionCookies { get; set; }

	[XmlElement]
	public bool PersistUserPreferences { get; set; }

	[XmlElement]
	public string AcceptLanguageList { get; set; }

	[XmlElement]
	public uint BackgroundColor { get; set; }

	[XmlElement]
	public int UncaughtExceptionStackSize { get; set; }

	[XmlElement]
	public string Locale { get; set; }

	[XmlElement]
	public string CookieableSchemesList { get; set; }

	[XmlElement]
	public bool ChromeRuntime { get; set; }

	[XmlElement]
	public bool CommandLineArgsDisabled { get; set; }

	[XmlElement]
	public bool MultiThreadedMessageLoop { get; set; }

	[XmlElement]
	public bool ExternalMessagePump { get; set; }

	[XmlElement]
	public bool CookieableSchemesExcludeDefaults { get; set; }
}
