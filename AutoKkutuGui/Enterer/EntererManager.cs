using AutoKkutuLib.Game;
using AutoKkutuLib.Game.Enterer;
using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace AutoKkutuGui.Enterer;
public class EntererManager
{
    private readonly IImmutableList<IEntererProvider> providers;
    private IImmutableDictionary<string, IEnterer>? enterers;

    public event EventHandler<EnterFinishedEventArgs>? EnterFinished;
    public event EventHandler<InputDelayEventArgs>? InputDelayApply;

    public EntererManager(IImmutableList<IEntererProvider> providers) => this.providers = providers ?? throw new ArgumentNullException(nameof(providers));

    private IImmutableDictionary<string, IEnterer> GetEnterers(IGame game)
    {
        if (enterers == null)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, IEnterer>();
            foreach (var provider in providers)
            {
                foreach (var entererSupplier in provider.GetEntererSuppliers())
                {
                    var enterer = entererSupplier(game);
                    enterer.EnterFinished += Enterer_AutoEntered;
                    enterer.InputDelayApply += Enterer_InputDelayApply;
                    builder.Add(enterer.EntererName, enterer);
                }
            }
            enterers = builder.ToImmutable();
        }

        return enterers;
    }

    public IEnterer? GetEnterer(IGame game, string name) => GetEnterers(game).GetValueOrDefault(name);

    public bool TryGetEnterer(IGame game, string name, [MaybeNullWhen(false)] out IEnterer enterer) => GetEnterers(game).TryGetValue(name, out enterer);

    /// <summary>
    /// 게임이 변경되는 등, 게임 캐시를 초기화해야 할 상황일 때 호출합니다.
    /// </summary>
    public void InvalidateGameCache() => enterers = null;

    // Event redirects
    private void Enterer_AutoEntered(object? sender, EnterFinishedEventArgs e) => EnterFinished?.Invoke(sender, e);
    private void Enterer_InputDelayApply(object? sender, InputDelayEventArgs e) => InputDelayApply?.Invoke(sender, e);
}

public delegate IEnterer EntererSupplier(IGame name);
