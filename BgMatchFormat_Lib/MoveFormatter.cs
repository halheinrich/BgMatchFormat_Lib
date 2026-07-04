using System.Text;
using BgDataTypes_Lib;
using BgGame_Lib;

namespace BgMatchFormat_Lib;

/// <summary>
/// Renders a played turn's dice-and-moves half-cell directly from the substrate
/// <see cref="Play"/> / <see cref="Move"/> primitives.
/// </summary>
/// <remarks>
/// Deliberately <em>not</em> <c>BgMoveGen.MoveNotationFormatter</c>: that renderer
/// emits <c>bar/22</c>, <c>6/off</c>, and <c>8/5(2)</c> chain-collapsed notation,
/// none of which is legal Jellyfish <c>.MAT</c>. The <c>.MAT</c> dialect is purely
/// numeric — bar is point <c>25</c>, bear-off is <c>X/0</c>, every hop is written
/// separately (no <c>(n)</c> counts), and a hit is a trailing <c>*</c>.
/// </remarks>
internal static class MoveFormatter
{
    /// <summary>
    /// The dice-prefixed half-cell for one turn, e.g. <c>31: 8/5 6/5</c>. An empty
    /// <paramref name="play"/> (a dance — no legal moves) renders as bare
    /// <c>DD:</c> with no moves. Carries no leading space; the dice column is the
    /// unshifted half-cell origin.
    /// </summary>
    public static string DiceHalf(int die1, int die2, Play play)
    {
        var sb = new StringBuilder();
        sb.Append(die1).Append(die2).Append(':');
        for (int i = 0; i < play.Count; i++)
        {
            sb.Append(' ').Append(FormatMove(play[i]));
        }
        return sb.ToString();
    }

    /// <summary>
    /// One hop as <c>from/to</c>, numeric-only. A negative <see cref="Move.ToPt"/>
    /// encodes a hit landing on <c>|ToPt|</c> and renders a trailing <c>*</c>; a
    /// zero <c>ToPt</c> is a bear-off (<c>from/0</c>); <c>from == 25</c> is bar entry.
    /// </summary>
    public static string FormatMove(Move move)
    {
        bool hit = move.ToPt < 0;
        int to = hit ? -move.ToPt : move.ToPt;
        return hit ? $"{move.FrPt}/{to}*" : $"{move.FrPt}/{to}";
    }
}
