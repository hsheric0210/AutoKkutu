using AutoKkutuGui.Enterer;
using AutoKkutuGui.Plugin;
using AutoKkutuLib.Browser;
using Serilog;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
namespace AutoKkutuGui;
internal sealed class PluginLoader
{
	public IImmutableList<IEntererProvider> EntererProviders { get; }

	public IImmutableList<IDomHandlerProvider> DomHandlerProviders { get; }

	public IImmutableList<IWebSocketHandlerProvider> WebSocketHandlerProviders { get; }

	public PluginLoader(string pluginFolder, BrowserBase browser)
	{
		var entererProvs = ImmutableList.CreateBuilder<IEntererProvider>();
		var domHandlerProvs = ImmutableList.CreateBuilder<IDomHandlerProvider>();
		var wsHandlerProvs = ImmutableList.CreateBuilder<IWebSocketHandlerProvider>();

		if (File.Exists(pluginFolder))
		{
			// https://stackoverflow.com/a/1395226
			var attr = File.GetAttributes(pluginFolder);
			if (!attr.HasFlag(FileAttributes.Directory))
				throw new ArgumentException($"Plugin folder {pluginFolder} is a file, not a directory");

			foreach (var assemblyFile in Directory.EnumerateFiles(pluginFolder, "*.dll", SearchOption.TopDirectoryOnly))
			{
				try
				{
					// TODO: Add AMSI scan support to prevent malicious plugins
					var assembly = Assembly.Load(assemblyFile); // If any error occurs, use LoadFrom() instead.
					var plg = (IPlugin?)assembly.CreateInstance("PluginMain", false);
					if (plg == null)
						throw new FileLoadException($"Plugin instance creation failure - File {assemblyFile} type 'Plugin'");

					var entererProv = plg.GetEntererProvider();
					if (entererProv != null)
						entererProvs.Add(entererProv);

					var domHandlerProv = plg.GetDomHandlerProvider(browser);
					if (domHandlerProv != null)
						domHandlerProvs.Add(domHandlerProv);

					var wsHandlerProv = plg.GetWebSocketHandlerProvider(browser);
					if (wsHandlerProv != null)
						wsHandlerProvs.Add(wsHandlerProv);
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Error loading plugin assembly {assembly}", assemblyFile);
				}
			}
		}
		else
		{
			Directory.CreateDirectory(pluginFolder);
		}

		EntererProviders = entererProvs.ToImmutable();
		DomHandlerProviders = domHandlerProvs.ToImmutable();
		WebSocketHandlerProviders = wsHandlerProvs.ToImmutable();
	}
}
