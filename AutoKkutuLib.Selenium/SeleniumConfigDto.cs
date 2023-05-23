using System.Xml.Serialization;

namespace AutoKkutuLib.Selenium;
[XmlRoot("Selenium")]
public class SeleniumConfigDto
{
	[XmlElement]
	public string MainPage { get; set; }

	[XmlElement]
	public string BinaryLocation { get; set; }

	[XmlElement]
	public bool LeaveBrowserRunning { get; set; }

	[XmlElement]
	public string DebuggerAddress { get; set; }

	[XmlElement]
	public string MinidumpPath { get; set; }

	[XmlElement]
	public string UserDataDir { get; set; }

	[XmlElement]
	public string DriverExecutable { get; set; }

	[XmlElement]
	public string BrowserExecutable { get; set; }

	[XmlArray("Arguments")]
	[XmlArrayItem("Argument")]
	public List<string> Arguments { get; set; }

	[XmlArray("ExcludedArguments")]
	[XmlArrayItem("ExcludedArgument")]
	public List<string> ExcludedArguments { get; set; }

	[XmlArray("EncodedExtensions")]
	[XmlArrayItem("EncodedExtension")]
	public List<string> EncodedExtensions { get; set; }
}
