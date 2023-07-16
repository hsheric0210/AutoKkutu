using Serilog;

namespace AutoKkutuLib.Game.Enterer;
public class DelayedInstantEnterer : EntererBase
{
	public const string Name = "DelayedInstant";

	public DelayedInstantEnterer(IGame game) : base(Name, game)
	{
	}

	protected override async ValueTask SendAsync(EnterInfo info)
	{
		var totalDelay = info.GetTotalDelay();
		if (info.Options.DelayStartAfterCharEnterEnabled)
		{
			if (InputStopwatch.ElapsedMilliseconds < totalDelay)
			{
				await Task.Delay((int)(totalDelay - InputStopwatch.ElapsedMilliseconds));
				Log.Debug("Waiting: (delay: {delay}) - (elaspsed:{elapsed}) = {realDelay}ms", totalDelay, InputStopwatch.ElapsedMilliseconds, totalDelay);
			}
		}
		else
		{
			var delayBetweenInput = (int)(totalDelay - InputStopwatch.ElapsedMilliseconds);
			var delay = Math.Max(totalDelay, delayBetweenInput); // Failsafe to prevent way-too-fast input
			Log.Debug("Waiting: max(delay: {delay}, delayBetweenInput: {delayBetweenInput}) = {realDelay}ms", totalDelay, delayBetweenInput, delay);
			await Task.Delay(delay);
		}

		EnterInstantly(info.Content, info.PathInfo);
	}
}
