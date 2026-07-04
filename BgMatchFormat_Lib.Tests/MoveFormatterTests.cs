using BgDataTypes_Lib;

namespace BgMatchFormat_Lib.Tests;

/// <summary>
/// Unit tests for move rendering, driven from raw <see cref="Move"/> integers (not
/// parsed notation) so a parse/format bug cannot hide symmetrically.
/// </summary>
public sealed class MoveFormatterTests
{
    [Fact]
    public void PlainMove() => Assert.Equal("8/5", MoveFormatter.FormatMove(new Move(8, 5)));

    [Fact]
    public void Hit_NegativeToPt_RendersStar() => Assert.Equal("13/7*", MoveFormatter.FormatMove(new Move(13, -7)));

    [Fact]
    public void BearOff_ZeroToPt() => Assert.Equal("6/0", MoveFormatter.FormatMove(new Move(6, 0)));

    [Fact]
    public void BarEntry_FromTwentyFive() => Assert.Equal("25/23", MoveFormatter.FormatMove(new Move(25, 23)));

    [Fact]
    public void BarEntry_WithHit() => Assert.Equal("25/23*", MoveFormatter.FormatMove(new Move(25, -23)));

    [Fact]
    public void DiceHalf_WithMoves()
    {
        var play = new Play();
        play.Add(new Move(8, 5));
        play.Add(new Move(6, 5));
        Assert.Equal("31: 8/5 6/5", MoveFormatter.DiceHalf(3, 1, play));
    }

    [Fact]
    public void DiceHalf_DoublesFourHops()
    {
        var play = new Play();
        play.Add(new Move(13, 9));
        play.Add(new Move(13, 9));
        play.Add(new Move(9, 5));
        play.Add(new Move(9, 5));
        Assert.Equal("44: 13/9 13/9 9/5 9/5", MoveFormatter.DiceHalf(4, 4, play));
    }

    [Fact]
    public void DiceHalf_Dance_IsBareDice() => Assert.Equal("52:", MoveFormatter.DiceHalf(5, 2, new Play()));
}
