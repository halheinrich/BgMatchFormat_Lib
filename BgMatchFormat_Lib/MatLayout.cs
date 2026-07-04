using System.Text;

namespace BgMatchFormat_Lib;

/// <summary>
/// The single home for <c>.MAT</c> line geometry: column positions and the
/// assembly of every line kind. Every width lives here once.
/// </summary>
/// <remarks>
/// Columns are 0-indexed. Verified byte-for-byte against real
/// BackgammonGalaxy exports:
/// <list type="bullet">
/// <item>The move-number field is <c>%3d)</c> (4 chars), then one separator space,
/// so a first half-cell begins at column 5.</item>
/// <item>The second half-cell begins at column <see cref="SecondColumnStart"/>
/// (33) on both score and move-pair lines.</item>
/// <item>Cube and result half-cells carry a single leading space (baked into their
/// text by <see cref="GameFormatter"/>), which shifts them one column right of a
/// dice half — to columns 6 and 34.</item>
/// </list>
/// </remarks>
internal static class MatLayout
{
    /// <summary>The 0-indexed column at which the second (player 2) half-cell begins.</summary>
    public const int SecondColumnStart = 33;

    /// <summary>A header comment line: <c>; [Name "Value"]</c>.</summary>
    public static string HeaderLine(MatHeaderTag tag) => $"; [{tag.Name} \"{tag.Value}\"]";

    /// <summary>The match-length line: <c>N point match</c> (<c>0</c> = money session).</summary>
    public static string MatchLine(int matchLength) => $"{matchLength} point match";

    /// <summary>A game header line: <c> Game N</c>.</summary>
    public static string GameLine(int gameNumber) => $" Game {gameNumber}";

    /// <summary>
    /// The per-game entering-score line: <c> P1 : s1{pad}P2 : s2</c>. The first
    /// half starts at column 1 (a single leading space); the second at
    /// <see cref="SecondColumnStart"/>.
    /// </summary>
    public static string ScoreLine(string player1, int score1, string player2, int score2)
    {
        var sb = new StringBuilder();
        sb.Append(' ').Append(player1).Append(" : ").Append(score1);
        PadToSecondColumn(sb);
        sb.Append(player2).Append(" : ").Append(score2);
        return sb.ToString();
    }

    /// <summary>
    /// A move-pair row. <paramref name="number"/> is the row's label, or
    /// <see langword="null"/> for an unnumbered (result-only) row. Either half may
    /// be <see langword="null"/>. When the right half is absent the line ends after
    /// the left half — never padded, so no trailing whitespace is emitted.
    /// </summary>
    public static string Row(int? number, string? left, string? right)
    {
        var sb = new StringBuilder();
        sb.Append(number is int n ? $"{n,3})" : "    ");   // 4-char number field
        sb.Append(' ');                                    // separator; first half begins at col 5
        if (left is not null) sb.Append(left);
        if (right is not null)
        {
            PadToSecondColumn(sb);
            sb.Append(right);
        }
        return sb.ToString();
    }

    private static void PadToSecondColumn(StringBuilder sb)
    {
        // Normally pad out to the second column. If the first half already reaches
        // it (a rare long play, or a name wider than the column), fall back to a
        // single separating space so the halves never collide and the line stays
        // parseable.
        int target = Math.Max(SecondColumnStart, sb.Length + 1);
        sb.Append(' ', target - sb.Length);
    }
}
