namespace BgMatchFormat_Lib.Tests;

/// <summary>
/// Unit tests for the grid-packing rules that decide row boundaries and numbering.
/// </summary>
public sealed class ColumnGridTests
{
    [Fact]
    public void LeftThenRight_ShareOneRow()
    {
        var grid = new ColumnGrid();
        grid.Place(MatColumn.Left, "L", countsForNumber: true);
        grid.Place(MatColumn.Right, "R", countsForNumber: true);

        IReadOnlyList<GridRow> rows = grid.Build();

        Assert.Single(rows);
        Assert.Equal(new GridRow("L", "R", true), rows[0]);
    }

    [Fact]
    public void RightThenLeft_SplitAcrossRows()
    {
        // Seat Two opens: the reply cannot join a row whose right cell is set.
        var grid = new ColumnGrid();
        grid.Place(MatColumn.Right, "R", countsForNumber: true);
        grid.Place(MatColumn.Left, "L", countsForNumber: true);

        IReadOnlyList<GridRow> rows = grid.Build();

        Assert.Equal(2, rows.Count);
        Assert.Equal(new GridRow(null, "R", true), rows[0]);
        Assert.Equal(new GridRow("L", null, true), rows[1]);
    }

    [Fact]
    public void TerminalResult_WhenOwnColumnFilled_StartsUnnumberedRow()
    {
        var grid = new ColumnGrid();
        grid.Place(MatColumn.Left, "L", countsForNumber: true);
        grid.Place(MatColumn.Right, "R", countsForNumber: true);
        grid.Place(MatColumn.Left, "Wins", countsForNumber: false);

        IReadOnlyList<GridRow> rows = grid.Build();

        Assert.Equal(2, rows.Count);
        Assert.Equal(new GridRow("Wins", null, false), rows[1]);
    }

    [Fact]
    public void TerminalResult_WhenOppositeColumnFree_JoinsNumberedRow()
    {
        var grid = new ColumnGrid();
        grid.Place(MatColumn.Left, "Drops", countsForNumber: true);
        grid.Place(MatColumn.Right, "Wins", countsForNumber: false);

        IReadOnlyList<GridRow> rows = grid.Build();

        Assert.Single(rows);
        Assert.Equal(new GridRow("Drops", "Wins", true), rows[0]);
    }
}
