namespace AutoKkutuLib.Game.Enterer;
public class DelayedInstantEnterer : EntererBase
{
	public const string Name = "DelayedInstant";

	public DelayedInstantEnterer(IGame game) : base(Name, game)
	{
	}

	protected override async ValueTask SendAsync(EnterInfo info)
	{
		var delayStartAfterCharInput = false;
		if (info.Options.CustomParameters is Parameter param)
			delayStartAfterCharInput = param.DelayStartAfterCharEnterEnabled;

		try
		{
			var totalDelay = info.GetTotalDelay();
			if (delayStartAfterCharInput)
			{
				if (InputStopwatch.ElapsedMilliseconds < totalDelay)
				{
					await Task.Delay((int)(totalDelay - InputStopwatch.ElapsedMilliseconds), CancelToken);
					LibLogger.Debug(EntererName, "Waiting: (delay: {delay}) - (elaspsed:{elapsed}) = {realDelay}ms", totalDelay, InputStopwatch.ElapsedMilliseconds, totalDelay);
				}
			}
			else
			{
				var delayBetweenInput = (int)(totalDelay - InputStopwatch.ElapsedMilliseconds);
				var delay = Math.Max(totalDelay, delayBetweenInput); // Failsafe to prevent way-too-fast input
				LibLogger.Debug(EntererName, "Waiting: max(delay: {delay}, delayBetweenInput: {delayBetweenInput}) = {realDelay}ms", totalDelay, delayBetweenInput, delay);
				await Task.Delay(delay, CancelToken);
			}
			EnterInstantly(info.Content, info.PathInfo);
		}
		catch (TaskCanceledException)
		{
			// ignored
		}
	}

	public readonly struct Parameter : IEquatable<Parameter>
	{
		public readonly bool DelayStartAfterCharEnterEnabled { get; }
		public Parameter(bool delayStartAfterCharEnterEnabled) => DelayStartAfterCharEnterEnabled = delayStartAfterCharEnterEnabled;

		public override string ToString() => $"{{{nameof(DelayStartAfterCharEnterEnabled)}: {DelayStartAfterCharEnterEnabled}}}";

		public override bool Equals(object? obj) => obj is Parameter parameter && Equals(parameter);
		public bool Equals(Parameter other) => DelayStartAfterCharEnterEnabled == other.DelayStartAfterCharEnterEnabled;
		public override int GetHashCode() => HashCode.Combine(DelayStartAfterCharEnterEnabled);

		public static bool operator ==(Parameter left, Parameter right) => left.Equals(right);
		public static bool operator !=(Parameter left, Parameter right) => !(left == right);
	}
}
