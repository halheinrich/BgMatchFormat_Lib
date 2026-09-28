using System.Collections;
using BgGame_Lib;

namespace BgMatchFormat_Lib.Tests;

/// <summary>
/// No public member hands out a live mutable collection or array behind a
/// read-only interface (halheinrich/backgammon#273's collection rider): each
/// such member of <see cref="MatchExport"/> hands out nothing a cast can make
/// writable, and an export holds a copy of what its caller passed, never the
/// caller's list — so nothing can change an export after it is validated.
/// </summary>
public sealed class ReadOnlyCollectionTests
{
    /// <summary>
    /// Every way a cast could write <paramref name="list"/> fails: it is no
    /// array and no <see cref="List{T}"/>, and through <see cref="IList{T}"/>
    /// and <see cref="IList"/> it is read-only — an element set included,
    /// which an array seen as <see cref="IList{T}"/> would allow.
    /// </summary>
    private static void AssertNotWritable<T>(IReadOnlyList<T> list)
    {
        Assert.NotEmpty(list);   // so the element set below is exercised
        Assert.False(list is T[], "an array's elements are writable through a cast");
        Assert.False(list is List<T>, "a List is writable through a cast");

        var generic = Assert.IsAssignableFrom<IList<T>>(list);
        Assert.True(generic.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => generic[0] = generic[0]);
        Assert.Throws<NotSupportedException>(() => generic.Add(generic[0]));
        Assert.Throws<NotSupportedException>(() => generic.Clear());

        var nonGeneric = Assert.IsAssignableFrom<IList>(list);
        Assert.True(nonGeneric.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => nonGeneric[0] = nonGeneric[0]);
    }

    /// <summary>A played game seat One wins, completing a 1-point match.</summary>
    private static GameRecord Game() =>
        new GameBuilder(matchLength: 1, player1Entering: 0, player2Entering: 0)
            .Play(MatchSeat.One, 3, 1, "8/5", "6/5")
            .Play(MatchSeat.Two, 6, 3, "24/18", "13/10")
            .EndGame(MatchSeat.One)
            .AsGameRecord();

    /// <summary>
    /// The export each factory builds from <paramref name="games"/> and
    /// <paramref name="tags"/>: every factory takes both, so every factory's
    /// copy is pinned.
    /// </summary>
    private static MatchExport Build(
        string factory, IReadOnlyList<GameRecord> games, IEnumerable<MatHeaderTag> tags) => factory switch
    {
        nameof(MatchExport.ForMatch) => MatchExport.ForMatch(1, "a", "b", games, tags),
        nameof(MatchExport.ForMoneySession) => MatchExport.ForMoneySession("a", "b", games, tags),
        nameof(MatchExport.ForForfeit) => MatchExport.ForForfeit(1, "a", "b", games, null, MatchSeat.One, tags),
        nameof(MatchExport.ForAbandoned) => MatchExport.ForAbandoned(1, "a", "b", games, null, "aborted", tags),
        _ => throw new ArgumentOutOfRangeException(nameof(factory), factory, "Unknown factory."),
    };

    // -----------------------------------------------------------------------
    //  MatchExport.CompletedGames
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(nameof(MatchExport.ForMatch))]
    [InlineData(nameof(MatchExport.ForMoneySession))]
    [InlineData(nameof(MatchExport.ForForfeit))]
    [InlineData(nameof(MatchExport.ForAbandoned))]
    public void CompletedGames_IsACopyOfTheCallersList(string factory)
    {
        GameRecord game = Game();
        var games = new List<GameRecord> { game };

        MatchExport export = Build(factory, games, []);
        games.Clear();

        Assert.Same(game, Assert.Single(export.CompletedGames));
        AssertNotWritable(export.CompletedGames);
    }

    [Fact]
    public void CompletedGames_CallerChangingItsList_DoesNotChangeTheValidatedExport()
    {
        // ForMatch validated that these games complete the match; emptying the
        // caller's list afterwards must not leave an export of no games.
        var games = new List<GameRecord> { Game() };
        MatchExport export = MatchExport.ForMatch(1, "a", "b", games);
        string before = MatExporter.Export(export);

        games.Clear();

        Assert.Equal(before, MatExporter.Export(export));
        Assert.Contains(" and the match", before);
    }

    // -----------------------------------------------------------------------
    //  MatchExport.Tags
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(nameof(MatchExport.ForMatch))]
    [InlineData(nameof(MatchExport.ForMoneySession))]
    [InlineData(nameof(MatchExport.ForForfeit))]
    [InlineData(nameof(MatchExport.ForAbandoned))]
    public void Tags_IsACopyOfTheCallersList(string factory)
    {
        var tag = new MatHeaderTag("Site", "here");
        var tags = new List<MatHeaderTag> { tag };

        MatchExport export = Build(factory, [Game()], tags);
        tags.Add(new MatHeaderTag("Event", "later"));

        Assert.Same(tag, Assert.Single(export.Tags));
        AssertNotWritable(export.Tags);
    }

}
