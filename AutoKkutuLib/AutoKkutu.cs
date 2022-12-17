using AutoKkutuLib.Database;
using AutoKkutuLib.HandlerManagement;
using AutoKkutuLib.Path;

namespace AutoKkutuLib;

public class AutoKkutu : IDisposable
{
	private bool disposedValue;
	private IHandlerManager? handlerManager;
	private AutoEnter? autoEnter;

	public NodeManager NodeManager { get; }
	public SpecialPathList SpecialPathList { get; }
	public PathFinder PathFinder { get; }

	public IHandlerManager HandlerManager => handlerManager ?? throw new InvalidOperationException("HandlerManager is not initalized yet! Call SetHandlerManager() to initialize it.");
	public AutoEnter AutoEnter => autoEnter ?? throw new InvalidOperationException("AutoEnter is not initalized yet! Call SetHandlerManager() to initialize it.");

	public AutoKkutu(AbstractDatabase db)
	{
		SpecialPathList = new SpecialPathList();
		NodeManager = new NodeManager(db.Connection);
		PathFinder = new PathFinder(NodeManager, SpecialPathList);
	}

	public void SetHandlerManager(IHandlerManager handlerManager)
	{
		this.handlerManager = handlerManager;
		autoEnter = new AutoEnter(handlerManager);
	}

	#region Disposal
	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
			{
				handlerManager?.Dispose();
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
