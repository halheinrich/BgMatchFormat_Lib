using BgDataTypes_Lib;
using BgGame_Lib;

namespace BgMatchFormat_Lib;

/// <summary>
/// Turns one game's transcript into packed <c>.MAT</c> rows, and resolves a
/// game's entering scores. Owns the <c>.MAT</c> action vocabulary
/// (<c>Doubles =&gt; N</c> / <c>Takes</c> / <c>Drops</c> / <c>Wins</c>); the
/// packing itself lives in <see cref="ColumnGrid"/>.
/// </summary>
internal static class GameFormatter
{
    /// <summary>
    /// Resolves the absolute (player&#160;1, player&#160;2) scores as the game
    /// begins, from the game's first transcript entry. The substrate snapshot is
    /// on-roll-relative, so the pair is unswapped when seat One is on roll and
    /// swapped otherwise — the single source of the score, no extra parameter.
    /// </summary>
    public static (int Player1, int Player2) EnteringScores(TranscriptEntry first)
    {
        MatchSnapshot match = first.State.Match;
        return first.OnRollSeat == MatchSeat.One
            ? (match.OnRollScore, match.OpponentScore)
            : (match.OpponentScore, match.OnRollScore);
    }

    /// <summary>
    /// Packs the game's entries into rows. Set <paramref name="appendMatchSuffix"/>
    /// on the match-deciding game so its winning result reads
    /// <c>Wins N point and the match</c>.
    /// </summary>
    public static IReadOnlyList<GridRow> BuildRows(
        IReadOnlyList<TranscriptEntry> entries, bool appendMatchSuffix)
    {
        var grid = new ColumnGrid();
        foreach (TranscriptEntry entry in entries)
        {
            switch (entry)
            {
                case PlayTranscriptEntry play:
                    grid.Place(
                        ColumnFor(play.OnRollSeat),
                        MoveFormatter.DiceHalf(play.Die1, play.Die2, play.ChosenPlay),
                        countsForNumber: true);
                    break;

                // A NoDouble is an implicit "declined to double, rolled on" — it
                // has no .MAT line, so it is skipped entirely. A transcript reader
                // must not expect every entry to render.
                case CubeTranscriptEntry { Action: CubeAction.NoDouble }:
                    break;

                case CubeTranscriptEntry cube:
                    grid.Place(ColumnFor(cube.ActingSeat), CubeHalf(cube), countsForNumber: true);
                    break;

                case GameEndedTranscriptEntry ended:
                    grid.Place(
                        ColumnFor(ended.Winner),
                        ResultHalf(ended.Result.Points, appendMatchSuffix),
                        countsForNumber: false);
                    break;
            }
        }

        return grid.Build();
    }

    private static MatColumn ColumnFor(MatchSeat seat) =>
        seat == MatchSeat.One ? MatColumn.Left : MatColumn.Right;

    private static string CubeHalf(CubeTranscriptEntry cube) => cube.Action switch
    {
        // The offer's snapshot is taken before the cube is applied, so CubeSize is
        // the pre-double value and the cube is being raised to twice it.
        CubeAction.Double => $" Doubles => {2 * cube.State.CubeSize}",
        CubeAction.Take => " Takes",
        CubeAction.Pass => " Drops",
        _ => throw new InvalidOperationException(
            $"Unexpected cube action in transcript: {cube.Action}."),
    };

    // Singular "point" always — matching the reference exports verbatim (they
    // write "Wins 2 point", "Wins 4 point"), which import cleanly into GNU
    // Backgammon and XG.
    private static string ResultHalf(int points, bool appendMatchSuffix) =>
        appendMatchSuffix ? $" Wins {points} point and the match" : $" Wins {points} point";
}
