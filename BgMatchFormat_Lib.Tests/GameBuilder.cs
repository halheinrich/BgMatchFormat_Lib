using BgDataTypes_Lib;
using BgGame_Lib;

namespace BgMatchFormat_Lib.Tests;

/// <summary>
/// Builds a single game's <see cref="Transcript"/> from a compact, readable
/// script, for golden-file tests. The transcripts are deterministic and need not
/// be legal backgammon — the exporter renders the entry stream it is given, so a
/// short synthetic game exercises the same rendering paths as a real one. A dummy
/// board is used because the exporter never reads it (moves come from the
/// <see cref="Play"/>, not the position).
/// </summary>
/// <remarks>
/// Cube tracking mirrors the substrate: a <see cref="Double"/> offer's snapshot
/// carries the pre-double cube value (the exporter raises it to twice that for
/// <c>Doubles =&gt; N</c>); the cube only actually doubles once the offer is taken.
/// </remarks>
internal sealed class GameBuilder
{
    private static readonly IReadOnlyList<int> DummyBoard = new int[26];

    private readonly int _matchLength;
    private readonly int _player1Entering;
    private readonly int _player2Entering;
    private readonly bool _crawford;
    private readonly Transcript _transcript = new();

    private int _cube = 1;
    private MatchSeat? _pendingOfferer;
    private MatchSeat _winner;
    private GameResult? _result;

    public GameBuilder(int matchLength, int player1Entering, int player2Entering, bool crawford = false)
    {
        _matchLength = matchLength;
        _player1Entering = player1Entering;
        _player2Entering = player2Entering;
        _crawford = crawford;
    }

    /// <summary>A played turn: dice <paramref name="die1"/><paramref name="die2"/>
    /// and zero or more moves in <c>.MAT</c> notation (e.g. <c>"8/5"</c>,
    /// <c>"13/7*"</c> for a hit, <c>"6/0"</c> for a bear-off). No moves = a dance.</summary>
    public GameBuilder Play(MatchSeat mover, int die1, int die2, params string[] moves)
    {
        var play = new Play();
        foreach (string move in moves) play.Add(ParseMove(move));
        _transcript.Append(new PlayTranscriptEntry(Snapshot(mover), mover, die1, die2, play));
        return this;
    }

    /// <summary>A dance (no legal moves) — an empty play on dice
    /// <paramref name="die1"/><paramref name="die2"/>.</summary>
    public GameBuilder Dance(MatchSeat mover, int die1, int die2) => Play(mover, die1, die2);

    /// <summary>A cube offer by <paramref name="offerer"/> (the on-roll seat). The
    /// matching <see cref="Take"/>/<see cref="Drop"/> takes no seat — the responder
    /// is derived — so the two can't be mis-paired.</summary>
    public GameBuilder Double(MatchSeat offerer)
    {
        _transcript.Append(new CubeTranscriptEntry(Snapshot(offerer), offerer, CubeAction.Double));
        _pendingOfferer = offerer;
        return this;
    }

    /// <summary>The responder takes the pending offer; the live cube doubles.</summary>
    public GameBuilder Take()
    {
        MatchSeat offerer = PendingOfferer();
        _transcript.Append(new CubeTranscriptEntry(Snapshot(offerer), offerer, CubeAction.Take));
        _cube *= 2;
        _pendingOfferer = null;
        return this;
    }

    /// <summary>The responder drops the pending offer; the game ends at the
    /// pre-double value.</summary>
    public GameBuilder Drop()
    {
        MatchSeat offerer = PendingOfferer();
        _transcript.Append(new CubeTranscriptEntry(Snapshot(offerer), offerer, CubeAction.Pass));
        _pendingOfferer = null;
        return this;
    }

    private MatchSeat PendingOfferer() =>
        _pendingOfferer ?? throw new InvalidOperationException("A cube response needs a pending Double.");

    /// <summary>Ends the game with <paramref name="winner"/> taking
    /// <paramref name="kind"/> at the current cube value.</summary>
    public GameBuilder EndGame(MatchSeat winner, GameResultKind kind = GameResultKind.WinSingle)
    {
        _winner = winner;
        _result = new GameResult(kind, OnRollWon: true, CubeSize: _cube);
        _transcript.Append(new GameEndedTranscriptEntry(Snapshot(winner), winner, _result));
        return this;
    }

    /// <summary>The finished game as a <see cref="GameRecord"/>.
    /// <see cref="EndGame"/> must have been called.</summary>
    public GameRecord AsGameRecord()
    {
        if (_result is null) throw new InvalidOperationException("EndGame was not called.");
        return new GameRecord(_winner, _result, _transcript);
    }

    /// <summary>The in-flight game as a bare <see cref="Transcript"/> (no result).</summary>
    public Transcript AsPartialTranscript() => _transcript;

    private GameSnapshot Snapshot(MatchSeat onRoll)
    {
        int onRollScore = onRoll == MatchSeat.One ? _player1Entering : _player2Entering;
        int opponentScore = onRoll == MatchSeat.One ? _player2Entering : _player1Entering;
        var match = new MatchSnapshot(_matchLength, onRollScore, opponentScore, _crawford);
        return new GameSnapshot(DummyBoard, _cube, CubeOwner.Centered, match);
    }

    private static Move ParseMove(string notation)
    {
        int slash = notation.IndexOf('/');
        int from = int.Parse(notation[..slash]);
        ReadOnlySpan<char> rest = notation.AsSpan(slash + 1);
        bool hit = rest.EndsWith("*");
        if (hit) rest = rest[..^1];
        int to = int.Parse(rest);
        return new Move(from, hit ? -to : to);
    }
}
