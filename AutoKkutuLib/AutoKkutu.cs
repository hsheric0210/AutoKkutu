using AutoKkutuLib.Database;
using AutoKkutuLib.HandlerManagement;
using AutoKkutuLib.Handlers;
using AutoKkutuLib.Path;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoKkutuLib;

// TODO: Implement the global initializer and management here
// like CefSharp.Cef
public class AutoKkutu : IDisposable
{
	private bool disposedValue;

	public NodeManager NodeManager { get; }
	public SpecialPathList SpecialPathList { get; }
	public PathFinder PathFinder { get; }

	public IHandlerManager HandlerManager { get; }
	public AutoEnter AutoEnter { get; }

	public AutoKkutu(AbstractDatabase db, IHandlerManager handlerManager)
	{
		SpecialPathList = new SpecialPathList();
		NodeManager = new NodeManager(db.Connection);
		PathFinder = new PathFinder(NodeManager, SpecialPathList);

		HandlerManager = handlerManager;
		AutoEnter = new AutoEnter(HandlerManager);
	}

	public AutoKkutu(AbstractDatabase db, AbstractHandler handler) : this(db, new HandlerManager(handler))
	{
	}

	#region Disposal
	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
			{
				HandlerManager.Dispose();
			}
			disposedValue = true;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
	#endregion
}
