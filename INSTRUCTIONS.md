# BgMatchFormat_Lib

> Collaboration contract: [`../AGENTS.md`](../AGENTS.md)
> Umbrella status & dependency graph: [`../INSTRUCTIONS.md`](../INSTRUCTIONS.md)
> Mission & principles: [`../VISION.md`](../VISION.md)

## Stack

C# / .NET 10 / Class Library / xUnit.

Match interchange formats over the `BgGame_Lib` substrate.

## Solution

`D:\Users\Hal\Documents\Visual Studio 2026\Projects\backgammon\BgMatchFormat_Lib\BgMatchFormat_Lib.slnx`

## Repo

https://github.com/halheinrich/BgMatchFormat_Lib — branch `main`.

## Depends on

* **BgGame_Lib** — the substrate. The exporter reads `GameRecord`, `Transcript`
  and its entries (`PlayTranscriptEntry`, `CubeTranscriptEntry`,
  `GameEndedTranscriptEntry`), `GameSnapshot` / `MatchSnapshot`, `GameResult`,
  and `MatchSeat`.
* **BgDataTypes_Lib** — `Move` (the sign-encoded from/to primitive) and `Play`
  (the per-turn move sequence), plus `CubeAction`.
* **BgMoveGen** — *test project only*, for the seeded real-wire golden's play
  agent (`MoveGenerator`). The library itself does not depend on BgMoveGen.

## Directory tree

```
BgMatchFormat_Lib.slnx
Directory.Packages.props
.gitattributes                       # *.mat text eol=lf — pins golden line endings
README.md
BgMatchFormat_Lib/
  BgMatchFormat_Lib.csproj
  MatchExport.cs                     # public input + validating factories
  MatHeaderTag.cs                    # public pass-through header tag
  MatExporter.cs                     # public: Export(MatchExport) -> string
  MatLayout.cs                       # column geometry + line assembly (SSOT)
  GameFormatter.cs                   # transcript -> rows; .MAT action vocabulary
  ColumnGrid.cs                      # the two-column grid-packing engine
  MoveFormatter.cs                   # Move / Play -> numeric .MAT notation
BgMatchFormat_Lib.Tests/
  BgMatchFormat_Lib.Tests.csproj
  GameBuilder.cs                     # fluent transcript builder for goldens
  GoldenFile.cs                      # golden read/compare (+ regeneration mode)
  MatExportGoldenTests.cs            # one golden per required edge case
  SeededMatchGoldenTests.cs          # end-to-end golden over a real MatchRunner match
  MoveFormatterTests.cs
  ColumnGridTests.cs
  MatchExportValidationTests.cs
  Goldens/*.mat                      # committed byte-exact fixtures
```

## Architecture

### The `.MAT` dialect

Output targets the Jellyfish `.MAT` text format as emitted by BackgammonGalaxy
(the dialect BgTournament matches). It imports cleanly into GNU Backgammon and
eXtreme Gammon. The layout was verified byte-for-byte against real Galaxy exports;
those exports are the format oracle (the golden fixtures' column geometry is
checked against them), not a from-memory reconstruction. "Galaxy" is layout
evidence, not the format's name — keep it out of code and public docs.

Format essentials:

* **Encoding:** UTF-8, `\n` line endings, one trailing newline, no trailing
  whitespace on any line.
* **Header:** `; [Name "Value"]` comment lines, then a blank line (only if any
  tags), then `N point match` (`0` = money session).
* **Per game:** ` Game n`, then a ` P1 : s1{pad}P2 : s2` entering-score line, then
  numbered move-pair rows.
* **Moves:** numeric only — bar = point 25, bear-off = `X/0`, one hop per move
  (no `(n)` counts, no `bar`/`off` literals), a hit is a trailing `*`. Dice are
  printed in raw roll order (`Die1Die2:`), not sorted.
* **Results:** singular `Wins N point` per game — verbatim to the reference
  exports, which write `Wins 2 point` / `Wins 4 point` (never `points`); the
  match-deciding game adds ` and the match`. Money sessions and forfeits never
  emit a match-final line.

### Column geometry — `MatLayout`

The single home for widths. Columns are 0-indexed: the move-number field is
`%3d)` (4 chars) + one separator space, so the first half-cell begins at column 5
and the second at column 33 (score lines share the column-33 origin). Cube and
result half-cells carry one baked-in leading space, shifting them to columns 6 and
34 — that one-space offset is the whole difference between a dice half and a
cube/result half. If a first half is long enough to reach column 33 the second
falls back to a single separating space.

### The grid-packing model — `ColumnGrid`

This is the subtle part. A game's chronological actions are packed into two
columns: each action lands in **its owner's** column (seat One = left, Two =
right). A new row opens when the target column is already filled, or when a left
cell would follow an already-placed right cell (the left half is always the
earlier action of a pair). This one rule reproduces every observed layout:
player-2-opens leaves the left of line 1 blank; a double answered by a take pushes
the take to the next row; a drop puts the winner's `Wins` on the same row when its
column is free but on a fresh unnumbered row when it is not. A row is numbered iff
it holds a real action; a row holding only the terminal `Wins` is unnumbered.

### Action mapping — `GameFormatter`

`GameFormatter.BuildRows` walks `Transcript.Entries` (a completed game's transcript
ends with a `GameEndedTranscriptEntry`; a forfeited partial game's does not) and
maps each entry to a grid placement. `CubeTranscriptEntry.State.CubeSize` is the
pre-double value (the offer is snapshotted before the cube is applied), so
`Doubles => N` uses `N = 2 * CubeSize`. Entering scores come from the game's first
entry via `EnteringScores`, resolving the on-roll-relative snapshot against
`OnRollSeat` — the single source, no extra parameter.

### Input shape — `MatchExport`

Immutable, built through four validating factories that make the mutually
exclusive shapes the only representable states:

* `ForMatch` — a completed positive-length match; validates that the games
  actually finish it (else the caller wanted `ForForfeit`/`ForAbandoned`). The
  deciding game gets the ` and the match` suffix.
* `ForMoneySession` — length 0; per-game results only.
* `ForForfeit` — a terminated match **with a winner**; completed games export
  normally; the optional in-flight partial game exports its moves with no result
  line; a `; <winner> wins by forfeit` comment keeps the body strictly standard.
  **A money session can be forfeited too** (pass length 0) — an engine can
  disconnect mid-session.
* `ForAbandoned` — a terminated match **with no winner** (a tournament server's
  aborted/faulted match). Same body semantics as `ForForfeit`, but instead of a
  seat it takes a caller-supplied, single-line reason rendered verbatim as the
  trailing `; <reason>` comment. The library owns only the comment framing (the
  leading `; `) and stays taxonomy-blind; length 0 is supported for an abandoned
  session.

`ForForfeit` **deliberately keeps its winner required**: a forfeit is a *resolved*
outcome — one seat is awarded the match — so the library owns the whole comment
sentence and there is nothing for the caller to phrase. `ForAbandoned` is the
winner-less counterpart, where no seat is awarded and the caller owns the reason
text. The kind of termination is decided once, positively, at the factory — an
internal `TerminationKind` (`Completed` / `Forfeit` / `Abandoned`) is the single
source for both the match-final line (`AppendsMatchSuffix` is `Completed && length
> 0`) and the trailing comment branch. `ForfeitWinner` and `TerminationReason` are
per-kind payloads whose presence the discriminator makes invariant, not an
emergent property of nullable-field combinations; a future terminal shape adds a
`TerminationKind` case rather than another exclusion clause. `TerminationKind` is
orthogonal to stakes — money (length 0) is a stakes shape and can end in any kind.

No `MatchResult` is ever required — neither a forfeited nor an abandoned match
produces one.

## Public API

```csharp
namespace BgMatchFormat_Lib;

public sealed record MatHeaderTag(string Name, string Value);

public sealed class MatchExport
{
    public int MatchLength { get; }                         // 0 = money session
    public string Player1Name { get; }                      // left column
    public string Player2Name { get; }                      // right column
    public IReadOnlyList<MatHeaderTag> Tags { get; }
    public IReadOnlyList<GameRecord> CompletedGames { get; }
    public Transcript? PartialGame { get; }
    public MatchSeat? ForfeitWinner { get; }                // set by ForForfeit only
    public string? TerminationReason { get; }               // set by ForAbandoned only

    public static MatchExport ForMatch(int matchLength, string player1Name, string player2Name,
        IReadOnlyList<GameRecord> games, IEnumerable<MatHeaderTag>? tags = null);
    public static MatchExport ForMoneySession(string player1Name, string player2Name,
        IReadOnlyList<GameRecord> games, IEnumerable<MatHeaderTag>? tags = null);
    public static MatchExport ForForfeit(int matchLength, string player1Name, string player2Name,
        IReadOnlyList<GameRecord> completedGames, Transcript? partialGame,
        MatchSeat forfeitWinner, IEnumerable<MatHeaderTag>? tags = null);
    public static MatchExport ForAbandoned(int matchLength, string player1Name, string player2Name,
        IReadOnlyList<GameRecord> completedGames, Transcript? partialGame,
        string terminationReason, IEnumerable<MatHeaderTag>? tags = null);
}

public static class MatExporter
{
    public static string Export(MatchExport match);         // -> .MAT text
}
```

Factories reject inputs that would corrupt the grammar: empty or newline-bearing
player names, header tag names/values containing a quote or newline (rejected, not
escaped — the format has no escaping convention), and an empty or multi-line
`ForAbandoned` termination reason. Tag values may be empty.

## Pitfalls

* **Never `StringBuilder.AppendLine`.** Its platform newline emits `\r\n` on
  Windows; the format is LF-only. The exporter appends `'\n'` explicitly. The
  `.gitattributes` `*.mat text eol=lf` keeps committed goldens from being mangled
  by autocrlf.
* **`Wins N point` is singular always** — matching the reference exports (`Wins 2
  point`, `Wins 4 point`). Do not "fix" it to `points`; the match-final line is
  the only place ` and the match` is appended.
* **`CubeAction.NoDouble` emits nothing.** It is an implicit "declined to double,
  rolled on" with no `.MAT` line. A transcript reader must not assume every entry
  renders — `GameFormatter` skips it deliberately.
* **`Doubles => N` uses twice the snapshot cube.** The offer is snapshotted before
  the cube is applied, so `State.CubeSize` is the pre-double value; the printed `N`
  is `2 * CubeSize`. A drop ends the game at the pre-double value
  (`GameResult.Points`), a take settles at the doubled value.
* **The grid packs by owner column, not by naive pairing.** Do not reorder actions
  or "pair up" left/right by index; the `ColumnGrid` rule (own column, new row on
  conflict or left-after-right) is what makes opening rolls, cube exchanges, and
  terminal results land where the reference exports put them.
* **The seeded real-wire golden is coupled to substrate internals.** It depends on
  `SeededDiceSource`'s RNG and `MoveGenerator`'s play ordering; a substrate change
  can shift it. Regenerate with `BGMATCHFORMAT_UPDATE_GOLDENS=1` and re-verify the
  column geometry against the reference exports before committing.
* **Resignations are out of scope.** Real Galaxy exports emit `Losses N point` /
  offset `Wins` lines for resignations; the substrate has no resign action, so the
  exporter never produces them. Do not add that grammar without a substrate source.

## Subproject-internal next steps

None. Cross-cutting arc items live in the umbrella `INSTRUCTIONS.md` (Arc 7:
BgTournament export endpoint, BgArena download affordance).
