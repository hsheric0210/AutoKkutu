namespace AutoKkutuLib.Game.Enterer;
public class NopEnterer : EntererBase
{
	public const string Name = "NopEnterer";

	public NopEnterer(IGame game) : base(Name, game)
	{
	}

	protected override ValueTask SendAsync(EnterInfo info) => ValueTask.CompletedTask;
}
