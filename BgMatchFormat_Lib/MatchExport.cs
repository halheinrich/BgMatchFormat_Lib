using BgGame_Lib;

namespace BgMatchFormat_Lib;

/// <summary>
/// The immutable input to <see cref="MatExporter.Export(MatchExport)"/>: a played
/// match in the shape the tournament server retains, needing no knowledge of the
/// <c>.MAT</c> format on the caller's part.
/// </summary>
/// <remarks>
/// <para>
/// A match never has to have produced a <c>MatchResult</c> — a forfeited match
/// never does. The exporter reads only ordered game transcripts
/// (<see cref="GameRecord"/> for a finished game; a bare <see cref="Transcript"/>
/// for a trailing in-flight game), the match length, two player names, and
/// pass-through header tags.
/// </para>
/// <para>
/// The three factory methods make the mutually exclusive shapes — a completed
/// match, a money session, a forfeit — the only representable states, and validate
/// their inputs eagerly so a malformed export fails at construction rather than
/// producing corrupt <c>.MAT</c> text.
/// </para>
/// </remarks>
public sealed class MatchExport
{
    private static readonly char[] ForbiddenNameChars = ['\n', '\r'];
    private static readonly char[] ForbiddenTagChars = ['"', '\n', '\r'];

    /// <summary>The match length in points; <c>0</c> denotes a money session.</summary>
    public int MatchLength { get; }

    /// <summary>Player 1's display name — owner of the left move-pair column.</summary>
    public string Player1Name { get; }

    /// <summary>Player 2's display name — owner of the right move-pair column.</summary>
    public string Player2Name { get; }

    /// <summary>Pass-through header tags, emitted verbatim in order.</summary>
    public IReadOnlyList<MatHeaderTag> Tags { get; }

    /// <summary>The finished games, in play order.</summary>
    public IReadOnlyList<GameRecord> CompletedGames { get; }

    /// <summary>
    /// The trailing in-flight game of a forfeited match, or <see langword="null"/>.
    /// An empty transcript here (forfeit before the game's first entry) is exported
    /// as no game block at all.
    /// </summary>
    public Transcript? PartialGame { get; }

    /// <summary>
    /// The seat awarded the forfeited match, or <see langword="null"/> for a
    /// normally-concluded match or money session.
    /// </summary>
    public MatchSeat? ForfeitWinner { get; }

    /// <summary>
    /// Whether the deciding game's result carries the <c>and the match</c> suffix.
    /// True only for a positive-length, non-forfeited match; money sessions and
    /// forfeits never emit a match-final line.
    /// </summary>
    internal bool AppendsMatchSuffix => MatchLength > 0 && ForfeitWinner is null;

    private MatchExport(
        int matchLength, string player1Name, string player2Name,
        IReadOnlyList<GameRecord> completedGames, Transcript? partialGame,
        MatchSeat? forfeitWinner, IReadOnlyList<MatHeaderTag> tags)
    {
        MatchLength = matchLength;
        Player1Name = player1Name;
        Player2Name = player2Name;
        CompletedGames = completedGames;
        PartialGame = partialGame;
        ForfeitWinner = forfeitWinner;
        Tags = tags;
    }

    /// <summary>
    /// A normally-concluded match of positive length. The supplied games must
    /// actually complete the match — some seat's cumulative score must reach
    /// <paramref name="matchLength"/> — otherwise the caller wanted
    /// <see cref="ForForfeit"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="matchLength"/>
    /// is not positive.</exception>
    /// <exception cref="ArgumentException">A name or tag is malformed, the games
    /// are empty, a game has no transcript, or the games do not complete the match.</exception>
    public static MatchExport ForMatch(
        int matchLength, string player1Name, string player2Name,
        IReadOnlyList<GameRecord> games, IEnumerable<MatHeaderTag>? tags = null)
    {
        if (matchLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(matchLength), matchLength,
                "A match must have a positive length; use ForMoneySession for money play.");
        ValidateName(player1Name, nameof(player1Name));
        ValidateName(player2Name, nameof(player2Name));
        ArgumentNullException.ThrowIfNull(games);
        if (games.Count == 0)
            throw new ArgumentException("A completed match needs at least one game.", nameof(games));
        IReadOnlyList<MatHeaderTag> tagList = ValidateTags(tags);
        foreach (GameRecord game in games) RequirePlayed(game, nameof(games));
        RequireCompletes(games, matchLength);

        return new MatchExport(matchLength, player1Name, player2Name, games, null, null, tagList);
    }

    /// <summary>
    /// A money session (no match length). Emits a <c>0 point match</c> header and
    /// per-game results only — never a match-final line.
    /// </summary>
    /// <exception cref="ArgumentException">A name or tag is malformed, or a game
    /// has no transcript.</exception>
    public static MatchExport ForMoneySession(
        string player1Name, string player2Name,
        IReadOnlyList<GameRecord> games, IEnumerable<MatHeaderTag>? tags = null)
    {
        ValidateName(player1Name, nameof(player1Name));
        ValidateName(player2Name, nameof(player2Name));
        ArgumentNullException.ThrowIfNull(games);
        IReadOnlyList<MatHeaderTag> tagList = ValidateTags(tags);
        foreach (GameRecord game in games) RequirePlayed(game, nameof(games));

        return new MatchExport(0, player1Name, player2Name, games, null, null, tagList);
    }

    /// <summary>
    /// A forfeited match: the completed games export normally, the optional
    /// in-flight <paramref name="partialGame"/> exports its moves with no result
    /// line, and a comment names <paramref name="forfeitWinner"/>. No match-final
    /// line is ever emitted. A money session can be forfeited too — pass
    /// <paramref name="matchLength"/> <c>0</c> for money semantics.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="matchLength"/>
    /// is negative, or <paramref name="forfeitWinner"/> is not a defined seat.</exception>
    /// <exception cref="ArgumentException">A name or tag is malformed, or a completed
    /// game has no transcript.</exception>
    public static MatchExport ForForfeit(
        int matchLength, string player1Name, string player2Name,
        IReadOnlyList<GameRecord> completedGames, Transcript? partialGame,
        MatchSeat forfeitWinner, IEnumerable<MatHeaderTag>? tags = null)
    {
        if (matchLength < 0)
            throw new ArgumentOutOfRangeException(nameof(matchLength), matchLength,
                "Match length cannot be negative (0 = money session).");
        ValidateName(player1Name, nameof(player1Name));
        ValidateName(player2Name, nameof(player2Name));
        ArgumentNullException.ThrowIfNull(completedGames);
        if (!Enum.IsDefined(forfeitWinner))
            throw new ArgumentOutOfRangeException(nameof(forfeitWinner), forfeitWinner,
                "Forfeit winner must be a defined match seat.");
        IReadOnlyList<MatHeaderTag> tagList = ValidateTags(tags);
        foreach (GameRecord game in completedGames) RequirePlayed(game, nameof(completedGames));

        return new MatchExport(
            matchLength, player1Name, player2Name, completedGames, partialGame, forfeitWinner, tagList);
    }

    private static void ValidateName(string name, string parameterName)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, parameterName);
        if (name.IndexOfAny(ForbiddenNameChars) >= 0)
            throw new ArgumentException("Player names cannot contain newline characters.", parameterName);
    }

    private static IReadOnlyList<MatHeaderTag> ValidateTags(IEnumerable<MatHeaderTag>? tags)
    {
        if (tags is null) return [];

        var list = new List<MatHeaderTag>();
        foreach (MatHeaderTag tag in tags)
        {
            ArgumentNullException.ThrowIfNull(tag, nameof(tags));
            ArgumentException.ThrowIfNullOrEmpty(tag.Name, nameof(tags));
            ArgumentNullException.ThrowIfNull(tag.Value, nameof(tags));
            // No escaping convention exists in .MAT, so a quote or newline would
            // corrupt the grammar — reject rather than mangle.
            if (tag.Name.IndexOfAny(ForbiddenTagChars) >= 0)
                throw new ArgumentException(
                    $"Header tag name \"{tag.Name}\" contains a quote or newline; .MAT has no escaping convention.",
                    nameof(tags));
            if (tag.Value.IndexOfAny(ForbiddenTagChars) >= 0)
                throw new ArgumentException(
                    $"Header tag value for \"{tag.Name}\" contains a quote or newline; .MAT has no escaping convention.",
                    nameof(tags));
            list.Add(tag);
        }

        return list;
    }

    private static void RequirePlayed(GameRecord game, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(game, parameterName);
        if (game.Transcript.Entries.Count == 0)
            throw new ArgumentException("A completed game must have a non-empty transcript.", parameterName);
    }

    private static void RequireCompletes(IReadOnlyList<GameRecord> games, int matchLength)
    {
        GameRecord last = games[games.Count - 1];
        (int player1, int player2) = GameFormatter.EnteringScores(last.Transcript.Entries[0]);
        int player1Final = player1 + (last.Winner == MatchSeat.One ? last.Result.Points : 0);
        int player2Final = player2 + (last.Winner == MatchSeat.Two ? last.Result.Points : 0);

        if (player1Final < matchLength && player2Final < matchLength)
            throw new ArgumentException(
                $"The supplied games do not complete a {matchLength}-point match " +
                $"(final score {player1Final}-{player2Final}); use ForForfeit for an unfinished match.",
                nameof(games));
    }
}
