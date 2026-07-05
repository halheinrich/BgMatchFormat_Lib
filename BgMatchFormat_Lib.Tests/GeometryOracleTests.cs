using System.Text.RegularExpressions;
using BgGame_Lib;

namespace BgMatchFormat_Lib.Tests;

/// <summary>
/// The anti-tautology guard: the exporter's column geometry is pinned against the
/// real BackgammonGalaxy exports, not against itself. Those exports are committed,
/// append-only reference fixtures under
/// <c>TestData/FixtureFiles/Mat/match_*.mat</c>; this test derives the canonical
/// column of each line kind from them and asserts the exporter lands on the same
/// columns. If the fixtures are missing the test fails loudly rather than skipping
/// — a silent skip would let the oracle rot.
/// </summary>
public sealed class GeometryOracleTests
{
    /// <summary>The observed second-column / result origins in a body of .MAT text.</summary>
    private sealed record Geometry(
        SortedSet<int> SecondColumnDice,        // right dice half or score line (col 33)
        SortedSet<int> SecondColumnCubeResult,  // right cube/result half (col 34)
        SortedSet<int> StandaloneLeftResult);   // unnumbered left result (col 6)

    [Fact]
    public void ExporterGeometry_MatchesRealGalaxyExports()
    {
        string[] fixtures = Directory.GetFiles(FixtureDir(), "match_*.mat");
        Assert.NotEmpty(fixtures);   // fail loudly if the reference corpus is absent

        Geometry oracle = Extract(fixtures.SelectMany(f => File.ReadAllLines(f)));

        // The real exports agree on one column per kind.
        Assert.Equal([33], oracle.SecondColumnDice);
        Assert.Equal([34], oracle.SecondColumnCubeResult);
        Assert.Equal([6], oracle.StandaloneLeftResult);

        Geometry mine = Extract(ExhaustiveExport().Split('\n'));

        Assert.Equal(oracle.SecondColumnDice, mine.SecondColumnDice);
        Assert.Equal(oracle.SecondColumnCubeResult, mine.SecondColumnCubeResult);
        Assert.Equal(oracle.StandaloneLeftResult, mine.StandaloneLeftResult);
    }

    /// <summary>
    /// A single money session that exercises every geometry-bearing construct:
    /// openings by each seat, both dice halves, a cube offer answered by a take, a
    /// cube drop (same-row result), and standalone results in each column.
    /// </summary>
    private static string ExhaustiveExport()
    {
        // Seat One opens and wins on its own move -> standalone LEFT result.
        GameRecord standaloneLeft = new GameBuilder(matchLength: 0, 0, 0)
            .Play(MatchSeat.One, 6, 5, "24/18", "13/8")
            .Play(MatchSeat.Two, 3, 1, "8/5", "6/5")
            .Play(MatchSeat.One, 5, 2, "6/1", "6/4")
            .EndGame(MatchSeat.One)
            .AsGameRecord();

        // Seat Two opens (blank left); a double in the right column; Two wins on its
        // own move -> standalone RIGHT result.
        GameRecord openTwoAndCube = new GameBuilder(matchLength: 0, 0, 0)
            .Play(MatchSeat.Two, 6, 5, "24/18", "18/13")
            .Play(MatchSeat.One, 3, 1, "8/5", "6/5")
            .Double(MatchSeat.Two)
            .Take()
            .Play(MatchSeat.Two, 4, 2, "13/9", "13/11")
            .Play(MatchSeat.One, 5, 3, "6/1", "6/3")
            .Play(MatchSeat.Two, 6, 1, "13/7", "6/5")
            .EndGame(MatchSeat.Two)
            .AsGameRecord();

        // A drop puts the winner's result on the same row's right half.
        GameRecord drop = new GameBuilder(matchLength: 0, 0, 0)
            .Play(MatchSeat.One, 6, 5, "8/2", "8/3")
            .Double(MatchSeat.Two)
            .Drop()
            .EndGame(MatchSeat.Two)
            .AsGameRecord();

        return MatExporter.Export(MatchExport.ForMoneySession(
            "gobetzu", "torsten", [standaloneLeft, openTwoAndCube, drop],
            [new MatHeaderTag("Site", "BackgammonGalaxy")]));
    }

    private static Geometry Extract(IEnumerable<string> lines)
    {
        var dice = new SortedSet<int>();
        var cubeResult = new SortedSet<int>();
        var standaloneLeft = new SortedSet<int>();

        foreach (string line in lines)
        {
            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith(" Game ")
                || Regex.IsMatch(line, @"^\d+ point match$"))
                continue;

            // Resignations use a Losses/offset-Wins grammar the substrate can't
            // produce; they are out of scope, so they don't inform the oracle.
            if (line.Contains("Losses"))
                continue;

            Match standalone = Regex.Match(line, @"^( +)Wins ");
            if (standalone.Success)
            {
                int col = standalone.Groups[1].Length;
                (col < 20 ? standaloneLeft : cubeResult).Add(col);
                continue;
            }

            Match score = Regex.Match(line, @"^ .+? : \d+ {2,}(\S).* : \d+$");
            if (score.Success && !line.Contains(')'))
            {
                dice.Add(score.Groups[1].Index);
                continue;
            }

            // The right half is the content after a 2+ space gap in the second-column
            // region; the 2-space indent of a left cube word (near column 6) is not it.
            foreach (Match gap in Regex.Matches(line, @"\s{2,}(\S)"))
            {
                int col = gap.Groups[1].Index;
                if (col < 30) continue;
                (char.IsDigit(line[col]) ? dice : cubeResult).Add(col);
            }
        }

        return new Geometry(dice, cubeResult, standaloneLeft);
    }

    private static string FixtureDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null &&
               !Directory.Exists(Path.Combine(dir.FullName, "TestData", "FixtureFiles", "Mat")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
            throw new InvalidOperationException(
                "Could not locate TestData/FixtureFiles/Mat/ — the .MAT reference corpus is required.");

        return Path.Combine(dir.FullName, "TestData", "FixtureFiles", "Mat");
    }
}
