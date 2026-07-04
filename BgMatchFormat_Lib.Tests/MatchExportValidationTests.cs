using BgGame_Lib;

namespace BgMatchFormat_Lib.Tests;

/// <summary>
/// Tests that the <see cref="MatchExport"/> factories fail fast on inputs that
/// would corrupt the <c>.MAT</c> grammar or misrepresent the match state.
/// </summary>
public sealed class MatchExportValidationTests
{
    private static GameRecord ShortGameWonBy(MatchSeat winner, int p1Entering, int p2Entering, int cubePoints = 1)
    {
        GameResultKind kind = cubePoints switch
        {
            1 => GameResultKind.WinSingle,
            2 => GameResultKind.WinGammon,
            _ => GameResultKind.WinBackgammon,
        };
        return new GameBuilder(matchLength: 0, p1Entering, p2Entering)
            .Play(MatchSeat.One, 6, 5, "24/18", "13/8")
            .Play(MatchSeat.Two, 3, 1, "8/5", "6/5")
            .EndGame(winner, kind)
            .AsGameRecord();
    }

    [Fact]
    public void ForMatch_NonPositiveLength_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MatchExport.ForMatch(0, "a", "b", [ShortGameWonBy(MatchSeat.One, 0, 0)]));
    }

    [Fact]
    public void ForMatch_EmptyGames_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            MatchExport.ForMatch(3, "a", "b", []));
    }

    [Fact]
    public void ForMatch_GamesDoNotCompleteMatch_Throws()
    {
        // One game worth 1 point does not finish a 3-point match.
        GameRecord game = ShortGameWonBy(MatchSeat.One, 0, 0);
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            MatchExport.ForMatch(3, "a", "b", [game]));
        Assert.Contains("do not complete", ex.Message);
    }

    [Fact]
    public void ForMatch_CompletingGames_Succeeds()
    {
        GameRecord game = ShortGameWonBy(MatchSeat.One, 0, 0, cubePoints: 3);   // 3 points -> completes
        MatchExport export = MatchExport.ForMatch(3, "a", "b", [game]);
        Assert.True(export.AppendsMatchSuffix);
    }

    [Theory]
    [InlineData("bad\nname")]
    [InlineData("carriage\rreturn")]
    [InlineData("")]
    public void PlayerName_MalformedOrEmpty_Throws(string name)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            MatchExport.ForMoneySession(name, "ok", [ShortGameWonBy(MatchSeat.One, 0, 0)]));
    }

    [Fact]
    public void HeaderTag_QuoteInValue_Throws()
    {
        var tags = new[] { new MatHeaderTag("Site", "has\"quote") };
        Assert.Throws<ArgumentException>(() =>
            MatchExport.ForMoneySession("a", "b", [ShortGameWonBy(MatchSeat.One, 0, 0)], tags));
    }

    [Fact]
    public void HeaderTag_NewlineInName_Throws()
    {
        var tags = new[] { new MatHeaderTag("Si\nte", "x") };
        Assert.Throws<ArgumentException>(() =>
            MatchExport.ForMoneySession("a", "b", [ShortGameWonBy(MatchSeat.One, 0, 0)], tags));
    }

    [Fact]
    public void HeaderTag_EmptyValueAllowed()
    {
        var tags = new[] { new MatHeaderTag("Comment", "") };
        MatchExport export = MatchExport.ForMoneySession("a", "b", [ShortGameWonBy(MatchSeat.One, 0, 0)], tags);
        Assert.Single(export.Tags);
    }

    [Fact]
    public void ForForfeit_MoneySession_Allowed()
    {
        // A money session (length 0) can be forfeited: no match-final line, but a
        // forfeit comment. This must be representable for session-2 consumers.
        MatchExport export = MatchExport.ForForfeit(
            0, "a", "b", [ShortGameWonBy(MatchSeat.One, 0, 0)], partialGame: null, MatchSeat.Two);

        Assert.False(export.AppendsMatchSuffix);
        Assert.Equal(MatchSeat.Two, export.ForfeitWinner);
    }

    [Fact]
    public void ForMoneySession_ForcesZeroLength()
    {
        MatchExport export = MatchExport.ForMoneySession("a", "b", [ShortGameWonBy(MatchSeat.One, 0, 0)]);
        Assert.Equal(0, export.MatchLength);
        Assert.False(export.AppendsMatchSuffix);
    }
}
