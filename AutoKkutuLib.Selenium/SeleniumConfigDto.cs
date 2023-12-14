using System.Xml.Serialization;

namespace AutoKkutuLib.Selenium;
[XmlRoot("Selenium")]
public class SeleniumConfigDto
{
	[XmlElement]
	public string JavaScriptInjectionBaseNamespace { get; set; } = "window";

	[XmlElement]
	public string MainPage { get; set; }

	[XmlElement]
	public string UserDataDir { get; set; }

	[XmlArray("Arguments")]
	[XmlArrayItem("Argument")]
	public List<string> Arguments { get; set; }

	[XmlElement]
	public string ProxyIp { get; set; }

	[XmlElement]
	public int ProxyPort { get; set; }

	[XmlElement]
	public string ProxyAuthUserName { get; set; }

	[XmlElement]
	public string ProxyAuthPassword { get; set; }

	[XmlElement]
	public string BinaryLocation { get; set; }

	[XmlElement]
	public bool LeaveBrowserRunning { get; set; }

	[XmlElement]
	public string DebuggerAddress { get; set; }

	[XmlElement]
	public string MinidumpPath { get; set; }

	[XmlElement]
	public string DriverExecutable { get; set; }

	[XmlArray("ExcludedArguments")]
	[XmlArrayItem("ExcludedArgument")]
	public List<string> ExcludedArguments { get; set; }

	[XmlArray("EncodedExtensions")]
	[XmlArrayItem("EncodedExtension")]
	public List<string> EncodedExtensions { get; set; }
}
