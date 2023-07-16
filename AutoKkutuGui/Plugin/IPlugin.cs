using AutoKkutuGui.Enterer;
using AutoKkutuLib.Browser;

namespace AutoKkutuGui.Plugin;
public interface IPlugin
{
	string PluginName { get; }

	IEntererProvider? GetEntererProvider();

	IDomHandlerProvider? GetDomHandlerProvider(BrowserBase browser);

	IWebSocketHandlerProvider? GetWebSocketHandlerProvider(BrowserBase browser);
}
