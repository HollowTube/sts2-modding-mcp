using MegaCrit.Sts2.Core.Combat;

namespace MCPTest;

/// <summary>
/// STS2 v0.107 removed <c>CombatManager.IsPlayPhase</c>; the turn phase now lives per player on
/// <c>PlayerCombatState.Phase</c>. This rebuilds the old meaning: combat is running, it is the
/// player side's turn, and at least one player is still in the Play phase (in singleplayer, the
/// local player).
/// </summary>
/// <remarks>
/// Adapted from Gennadiyev/STS2MCP (MIT License, Copyright (c) Gennadiyev),
/// https://github.com/Gennadiyev/STS2MCP/pull/123.
/// </remarks>
public static class CombatPhase
{
    public static bool IsPlayPhase(this CombatManager? manager)
    {
        if (manager is not { IsInProgress: true }) return false;
        var state = manager.DebugOnlyGetState();
        if (state == null || state.CurrentSide != CombatSide.Player) return false;
        foreach (var player in state.Players)
        {
            if (player?.PlayerCombatState?.Phase == PlayerTurnPhase.Play)
                return true;
        }
        return false;
    }
}
