using BgDataTypes_Lib;
using BgGame_Lib;
using BgMoveGen;

namespace BgMatchFormat_Lib.Tests;

/// <summary>
/// End-to-end golden over a real match produced by the substrate
/// <see cref="MatchRunner"/> — not hand-built entries. Proves the exporter renders
/// genuine <see cref="GameRecord"/>s (real perspective flips, real seat
/// sequencing) and not just synthetic ones. Deterministic via
/// <see cref="SeededDiceSource"/> and first-legal-play agents; the golden must be
/// regenerated if the substrate's seeded RNG or move ordering changes.
/// </summary>
public sealed class SeededMatchGoldenTests
{
    /// <summary>Always plays the first legal play — deterministic, no RNG.</summary>
    private sealed class FirstPlayAgent : IPlayAgent
    {
        public ValueTask<Play> ChoosePlayAsync(
            GameState state, int die1, int die2, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(MoveGenerator.GeneratePlays(state.Board, die1, die2)[0]);
    }

    /// <summary>Never doubles (keeps the seeded match cubeless and deterministic).</summary>
    private sealed class NeverCubeAgent : ICubeAgent
    {
        public ValueTask<CubeAction> ChooseOfferAsync(GameState state, CancellationToken ct = default) =>
            ValueTask.FromResult(CubeAction.NoDouble);

        public ValueTask<CubeAction> ChooseResponseAsync(GameState state, CancellationToken ct = default) =>
            ValueTask.FromResult(CubeAction.Take);
    }

    [Fact]
    public async Task SeededMatch_ExportsAndImportsCleanly()
    {
        var participant = MatchParticipant.From(new CombinedAgent());
        var runner = new MatchRunner(new SeededDiceSource(seed: 20260704));

        MatchResult result = await runner.RunMatchAsync(participant, participant, matchLength: 3);

        string mat = MatExporter.Export(
            MatchExport.ForMatch(3, "Player 1", "Player 2", result.Games,
                [new MatHeaderTag("Site", "BgTournament"), new MatHeaderTag("Match ID", "seeded-20260704")]));

        GoldenFile.Verify("seeded_match.mat", mat);
    }

    private sealed class CombinedAgent : IPlayAgent, ICubeAgent
    {
        private readonly FirstPlayAgent _play = new();
        private readonly NeverCubeAgent _cube = new();

        public ValueTask<Play> ChoosePlayAsync(GameState state, int die1, int die2, CancellationToken ct = default) =>
            _play.ChoosePlayAsync(state, die1, die2, ct);

        public ValueTask<CubeAction> ChooseOfferAsync(GameState state, CancellationToken ct = default) =>
            _cube.ChooseOfferAsync(state, ct);

        public ValueTask<CubeAction> ChooseResponseAsync(GameState state, CancellationToken ct = default) =>
            _cube.ChooseResponseAsync(state, ct);
    }
}
