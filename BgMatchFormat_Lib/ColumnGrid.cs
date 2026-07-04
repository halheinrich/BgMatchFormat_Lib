namespace BgMatchFormat_Lib;

/// <summary>Which of the two move-pair columns a half-cell occupies.</summary>
internal enum MatColumn
{
    /// <summary>Player 1's column (the left half). Seat <c>One</c> owns it.</summary>
    Left,

    /// <summary>Player 2's column (the right half). Seat <c>Two</c> owns it.</summary>
    Right,
}

/// <summary>One packed move-pair row: the two half-cells (either may be absent)
/// and whether the row carries a number.</summary>
internal readonly record struct GridRow(string? Left, string? Right, bool Numbered);

/// <summary>
/// Packs a game's chronological action stream into the two-column
/// <c>.MAT</c> move-pair grid.
/// </summary>
/// <remarks>
/// <para>
/// Each action lands in <em>its owner's</em> column. A new row starts when the
/// target column in the current row is already filled, or when a left cell would
/// be placed after the current row's right cell is already set — a left action
/// can never follow a right one within a row, because the left half is always the
/// earlier action of a pair. This single rule reproduces every layout the
/// reference exports show:
/// </para>
/// <list type="bullet">
/// <item>Player 2 winning the opening roll leaves the left half of line 1 blank
/// (row 1 has only a right cell; player 1's reply is forced onto row 2).</item>
/// <item>A double answered by a take pushes the taker's response onto the next
/// row.</item>
/// <item>A drop puts the winner's terminal <c>Wins</c> in the opposite column of
/// the <em>same</em> row when that column is free, but onto a fresh unnumbered
/// row when it is not.</item>
/// </list>
/// <para>
/// A row is numbered iff it contains at least one real action (a move or cube
/// action). A row holding only the terminal <c>Wins</c> pseudo-action is
/// unnumbered — that is what the <c>countsForNumber</c> flag distinguishes.
/// </para>
/// </remarks>
internal sealed class ColumnGrid
{
    private sealed class Cells
    {
        public string? Left;
        public string? Right;
        public bool Numbered;
    }

    private readonly List<Cells> _rows = [];
    private Cells? _current;

    /// <summary>
    /// Places <paramref name="text"/> in <paramref name="column"/>, opening a new
    /// row first if the current row cannot accept it.
    /// </summary>
    /// <param name="column">The column the action's owner occupies.</param>
    /// <param name="text">The rendered half-cell text.</param>
    /// <param name="countsForNumber"><see langword="true"/> for a real move or cube
    /// action (makes its row numbered); <see langword="false"/> for the terminal
    /// result pseudo-action.</param>
    public void Place(MatColumn column, string text, bool countsForNumber)
    {
        bool left = column == MatColumn.Left;
        bool occupied = _current is not null &&
            (left ? _current.Left is not null || _current.Right is not null
                  : _current.Right is not null);

        if (_current is null || occupied)
        {
            if (_current is not null) _rows.Add(_current);
            _current = new Cells();
        }

        if (left) _current.Left = text; else _current.Right = text;
        if (countsForNumber) _current.Numbered = true;
    }

    /// <summary>Materializes the packed rows in order.</summary>
    public IReadOnlyList<GridRow> Build()
    {
        var rows = new List<GridRow>(_rows.Count + 1);
        foreach (var r in _rows) rows.Add(new GridRow(r.Left, r.Right, r.Numbered));
        if (_current is not null) rows.Add(new GridRow(_current.Left, _current.Right, _current.Numbered));
        return rows;
    }
}
