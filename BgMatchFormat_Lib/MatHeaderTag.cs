namespace BgMatchFormat_Lib;

/// <summary>
/// A single pass-through header tag rendered as a <c>; [Name "Value"]</c> comment
/// line at the top of a <c>.MAT</c> file.
/// </summary>
/// <remarks>
/// The exporter writes tags verbatim in the order supplied; it neither interprets
/// nor reorders them. Which tags a match carries (for example <c>Site</c>,
/// <c>Match ID</c>, <c>Event</c>) is the caller's decision — the library is a
/// dumb carrier so that new tags need no library change. There is no escaping
/// convention in the <c>.MAT</c> grammar, so a <see cref="Name"/> or
/// <see cref="Value"/> containing a quote or newline is rejected at construction
/// of the owning <see cref="MatchExport"/> rather than silently mangled.
/// </remarks>
/// <param name="Name">The tag name, e.g. <c>Site</c>. Must be non-empty.</param>
/// <param name="Value">The tag value, e.g. <c>BackgammonGalaxy</c>. May be empty.</param>
public sealed record MatHeaderTag(string Name, string Value);
