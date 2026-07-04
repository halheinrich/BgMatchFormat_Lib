using System.Text;
using BgGame_Lib;

namespace BgMatchFormat_Lib;

/// <summary>
/// Exports a played match to Jellyfish-format <c>.MAT</c> text.
/// </summary>
/// <remarks>
/// The layout follows the dialect emitted by BackgammonGalaxy (verified against
/// real exports) and imports cleanly into GNU Backgammon and eXtreme Gammon.
/// Output uses <c>\n</c> line endings and a single trailing newline, with no
/// trailing whitespace on any line — do not route it through
/// <see cref="StringBuilder.AppendLine()"/>, whose platform newline would emit
/// <c>\r\n</c> on Windows.
/// </remarks>
public static class MatExporter
{
    private const char Lf = '\n';

    /// <summary>
    /// Renders <paramref name="match"/> as a complete <c>.MAT</c> document.
    /// </summary>
    /// <param name="match">The match to export, built through one of the
    /// <see cref="MatchExport"/> factory methods.</param>
    /// <returns>The <c>.MAT</c> text.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="match"/> is null.</exception>
    public static string Export(MatchExport match)
    {
        ArgumentNullException.ThrowIfNull(match);

        var sb = new StringBuilder();

        foreach (MatHeaderTag tag in match.Tags)
            AppendLine(sb, MatLayout.HeaderLine(tag));

        if (match.Tags.Count > 0)
            AppendLine(sb, string.Empty);                   // blank line separating the header block
        AppendLine(sb, MatLayout.MatchLine(match.MatchLength));

        int gameNumber = 1;
        IReadOnlyList<GameRecord> completed = match.CompletedGames;
        for (int i = 0; i < completed.Count; i++)
        {
            bool isLastCompleted = i == completed.Count - 1 && match.PartialGame is null;
            bool suffix = isLastCompleted && match.AppendsMatchSuffix;
            WriteGame(sb, ref gameNumber, completed[i].Transcript.Entries, match, suffix);
        }

        // The in-flight forfeited game exports its moves with no result line. If
        // forfeit struck before its first entry there is no block to write.
        if (match.PartialGame is { Entries.Count: > 0 } partial)
            WriteGame(sb, ref gameNumber, partial.Entries, match, appendMatchSuffix: false);

        // A forfeit is recorded as a comment so the body stays strictly standard.
        if (match.ForfeitWinner is MatchSeat winner)
            AppendLine(sb, $"; {PlayerName(match, winner)} wins by forfeit");

        return sb.ToString();
    }

    private static void WriteGame(
        StringBuilder sb, ref int gameNumber,
        IReadOnlyList<TranscriptEntry> entries, MatchExport match, bool appendMatchSuffix)
    {
        AppendLine(sb, string.Empty);                       // blank line before each game
        AppendLine(sb, MatLayout.GameLine(gameNumber));

        (int player1, int player2) = GameFormatter.EnteringScores(entries[0]);
        AppendLine(sb, MatLayout.ScoreLine(match.Player1Name, player1, match.Player2Name, player2));

        int rowNumber = 1;
        foreach (GridRow row in GameFormatter.BuildRows(entries, appendMatchSuffix))
        {
            int? label = row.Numbered ? rowNumber++ : null;
            AppendLine(sb, MatLayout.Row(label, row.Left, row.Right));
        }

        gameNumber++;
    }

    private static void AppendLine(StringBuilder sb, string line) => sb.Append(line).Append(Lf);

    private static string PlayerName(MatchExport match, MatchSeat seat) =>
        seat == MatchSeat.One ? match.Player1Name : match.Player2Name;
}
