using BgGame_Lib;

namespace BgMatchFormat_Lib.Tests;

/// <summary>
/// Golden-file tests over hand-built transcripts, one per required edge case. The
/// goldens are byte-exact <c>.MAT</c>; their column geometry is verified against
/// the real BackgammonGalaxy reference exports, so they pin the format itself, not
/// merely the exporter's current output.
/// </summary>
public sealed class MatExportGoldenTests
{
    private static IReadOnlyList<MatHeaderTag> Tags(string id) => [new("Match ID", id)];

    [Fact]
    public void OpeningWonBySeatOne_StandaloneFinalLine()
    {
        // Seat One opens: both halves present on line 1. Winner is seat One (left),
        // whose column is filled, so the final Wins lands on its own unnumbered row.
        GameRecord game = new GameBuilder(matchLength: 1, player1Entering: 0, player2Entering: 0)
            .Play(MatchSeat.One, 3, 1, "8/5", "6/5")
            .Play(MatchSeat.Two, 6, 3, "24/18", "13/10")
            .EndGame(MatchSeat.One)
            .AsGameRecord();

        string mat = MatExporter.Export(
            MatchExport.ForMatch(1, "gobetzu", "torsten", [game], Tags("open-one")));

        GoldenFile.Verify("opening_seat_one.mat", mat);
    }

    [Fact]
    public void OpeningWonBySeatTwo_BlankLeftOnFirstLine()
    {
        // Seat Two opens: the left half of line 1 is blank. Winner is seat Two
        // (right), whose column is free on the last row, so the final Wins shares
        // that row.
        GameRecord game = new GameBuilder(matchLength: 1, player1Entering: 0, player2Entering: 0)
            .Play(MatchSeat.Two, 6, 5, "24/18", "18/13")
            .Play(MatchSeat.One, 3, 1, "8/5", "6/5")
            .Play(MatchSeat.Two, 6, 1, "13/7", "6/5")
            .EndGame(MatchSeat.Two)
            .AsGameRecord();

        string mat = MatExporter.Export(
            MatchExport.ForMatch(1, "gobetzu", "torsten", [game], Tags("open-two")));

        GoldenFile.Verify("opening_seat_two.mat", mat);
    }

    [Fact]
    public void NoHeaderTags_OmitsLeadingBlankLine()
    {
        GameRecord game = new GameBuilder(matchLength: 0, player1Entering: 0, player2Entering: 0)
            .Play(MatchSeat.One, 6, 5, "24/18", "13/8")
            .Play(MatchSeat.Two, 3, 1, "8/5", "6/5")
            .EndGame(MatchSeat.One)
            .AsGameRecord();

        string mat = MatExporter.Export(MatchExport.ForMoneySession("gobetzu", "torsten", [game]));

        Assert.StartsWith("0 point match\n", mat);
    }

    [Fact]
    public void Dance_RendersBareDiceHalf()
    {
        GameRecord game = new GameBuilder(matchLength: 0, player1Entering: 0, player2Entering: 0)
            .Play(MatchSeat.One, 3, 1, "8/5", "6/5")
            .Dance(MatchSeat.Two, 5, 2)
            .Play(MatchSeat.One, 6, 3, "13/7", "13/10")
            .Play(MatchSeat.Two, 2, 1, "24/23", "6/5")
            .EndGame(MatchSeat.One)
            .AsGameRecord();

        string mat = MatExporter.Export(
            MatchExport.ForMoneySession("gobetzu", "torsten", [game], Tags("dance")));

        GoldenFile.Verify("dance.mat", mat);
    }

    [Fact]
    public void DoublesFourHop_Hit_BarEntry_BearOff()
    {
        GameRecord game = new GameBuilder(matchLength: 0, player1Entering: 0, player2Entering: 0)
            .Play(MatchSeat.One, 4, 4, "13/9", "13/9", "9/5", "9/5")   // doubles: four hops
            .Play(MatchSeat.Two, 6, 5, "24/18", "18/13")
            .Play(MatchSeat.One, 3, 1, "6/3", "6/5*")                  // hit on 5
            .Play(MatchSeat.Two, 2, 1, "25/23", "6/5")                 // bar entry (25)
            .Play(MatchSeat.One, 6, 5, "6/0", "5/0")                   // bear off
            .EndGame(MatchSeat.One)
            .AsGameRecord();

        string mat = MatExporter.Export(
            MatchExport.ForMoneySession("gobetzu", "torsten", [game], Tags("dice")));

        GoldenFile.Verify("doubles_hit_bearoff.mat", mat);
    }

    [Fact]
    public void CubeSequence_DoubleTakeRedoubleTake_Win()
    {
        // Doubles => 2 (taken), Doubles => 4 (taken), single win at the 4-cube. A
        // doubler doubles pre-roll, and after a take rolls and plays that same turn.
        GameRecord game = new GameBuilder(matchLength: 0, player1Entering: 0, player2Entering: 0)
            .Play(MatchSeat.One, 6, 5, "24/18", "13/8")
            .Double(MatchSeat.Two)
            .Take()
            .Play(MatchSeat.Two, 3, 1, "8/5", "6/5")
            .Double(MatchSeat.One)
            .Take()
            .Play(MatchSeat.One, 6, 5, "6/0", "5/0")
            .EndGame(MatchSeat.One)
            .AsGameRecord();

        string mat = MatExporter.Export(
            MatchExport.ForMoneySession("gobetzu", "torsten", [game], Tags("cube-take")));

        GoldenFile.Verify("cube_take.mat", mat);
    }

    [Fact]
    public void CubeSequence_DoubleDrop_EndsGame()
    {
        // Drop in the left column puts the winner's Wins on the same row's right.
        GameRecord game = new GameBuilder(matchLength: 0, player1Entering: 0, player2Entering: 0)
            .Play(MatchSeat.One, 6, 5, "8/2", "8/3")
            .Double(MatchSeat.Two)
            .Drop()
            .EndGame(MatchSeat.Two)
            .AsGameRecord();

        string mat = MatExporter.Export(
            MatchExport.ForMoneySession("gobetzu", "torsten", [game], Tags("cube-drop")));

        GoldenFile.Verify("cube_drop.mat", mat);
    }

    [Fact]
    public void MoneySession_MultipleGames_NoFinalLine()
    {
        GameRecord game1 = new GameBuilder(matchLength: 0, player1Entering: 0, player2Entering: 0)
            .Play(MatchSeat.One, 6, 5, "24/18", "13/8")
            .Play(MatchSeat.Two, 3, 1, "8/5", "6/5")
            .EndGame(MatchSeat.One)
            .AsGameRecord();

        GameRecord game2 = new GameBuilder(matchLength: 0, player1Entering: 0, player2Entering: 0)
            .Play(MatchSeat.Two, 5, 3, "8/3", "6/3")
            .Play(MatchSeat.One, 4, 2, "24/20", "13/11")
            .Play(MatchSeat.Two, 6, 1, "13/7", "8/7")
            .EndGame(MatchSeat.Two, GameResultKind.WinBackgammon)   // 3 points at the 1-cube
            .AsGameRecord();

        string mat = MatExporter.Export(
            MatchExport.ForMoneySession("gobetzu", "torsten", [game1, game2], Tags("money")));

        GoldenFile.Verify("money_session.mat", mat);
    }

    [Fact]
    public void CompletedMatch_ScoreProgression_AndTheMatch()
    {
        GameRecord game1 = new GameBuilder(matchLength: 3, player1Entering: 0, player2Entering: 0)
            .Play(MatchSeat.One, 6, 5, "24/18", "13/8")
            .Play(MatchSeat.Two, 3, 1, "8/5", "6/5")
            .EndGame(MatchSeat.One)                                 // 1-0
            .AsGameRecord();

        GameRecord game2 = new GameBuilder(matchLength: 3, player1Entering: 1, player2Entering: 0)
            .Play(MatchSeat.One, 3, 1, "8/5", "6/5")
            .Play(MatchSeat.Two, 6, 4, "24/18", "13/9")
            .Double(MatchSeat.One)
            .Take()
            .Play(MatchSeat.One, 5, 2, "6/1", "6/4")
            .EndGame(MatchSeat.One)                                 // +2 at the 2-cube -> 3-0
            .AsGameRecord();

        string mat = MatExporter.Export(
            MatchExport.ForMatch(3, "gobetzu", "torsten", [game1, game2], Tags("match")));

        GoldenFile.Verify("match_complete.mat", mat);
    }

    [Fact]
    public void Forfeit_WithPartialGame_MovesThenComment()
    {
        GameRecord completed = new GameBuilder(matchLength: 3, player1Entering: 0, player2Entering: 0)
            .Play(MatchSeat.One, 6, 5, "24/18", "13/8")
            .Play(MatchSeat.Two, 3, 1, "8/5", "6/5")
            .EndGame(MatchSeat.Two)                                 // 0-1
            .AsGameRecord();

        Transcript partial = new GameBuilder(matchLength: 3, player1Entering: 0, player2Entering: 1)
            .Play(MatchSeat.One, 4, 2, "24/20", "13/11")
            .Play(MatchSeat.Two, 6, 3, "24/18", "13/10")
            .AsPartialTranscript();

        string mat = MatExporter.Export(
            MatchExport.ForForfeit(3, "gobetzu", "torsten", [completed], partial, MatchSeat.One, Tags("forfeit")));

        GoldenFile.Verify("forfeit_partial.mat", mat);
    }

    [Fact]
    public void Forfeit_EmptyPartialGame_OmitsBlockKeepsComment()
    {
        GameRecord completed = new GameBuilder(matchLength: 3, player1Entering: 0, player2Entering: 0)
            .Play(MatchSeat.One, 6, 5, "24/18", "13/8")
            .Play(MatchSeat.Two, 3, 1, "8/5", "6/5")
            .EndGame(MatchSeat.Two)
            .AsGameRecord();

        // A partial transcript with no entries: forfeit struck before the game's
        // first play, so no game block is emitted — only the comment.
        Transcript emptyPartial = new GameBuilder(matchLength: 3, player1Entering: 0, player2Entering: 1)
            .AsPartialTranscript();

        string mat = MatExporter.Export(
            MatchExport.ForForfeit(3, "gobetzu", "torsten", [completed], emptyPartial, MatchSeat.One, Tags("forfeit-empty")));

        GoldenFile.Verify("forfeit_empty.mat", mat);
    }
}
