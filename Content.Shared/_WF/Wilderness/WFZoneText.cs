namespace Content.Shared._WF.Wilderness;

/// <summary>A zone's title and summary as shown, given the war level and who may fight.</summary>
public static class WFZoneText
{
    public static string Title(WFZonePrototype zone, bool hot)
    {
        return Loc.GetString(hot && zone.HotTitle is { } title ? title : zone.Title);
    }

    public static string Summary(WFZonePrototype zone, bool hot, bool openToAll)
    {
        var key = hot && zone.HotSummary is { } summary ? summary : zone.Summary;
        return Loc.GetString(key, ("open", openToAll ? "yes" : "no"));
    }

    /// <summary>"9:42" for a span, never negative.</summary>
    public static string Clock(TimeSpan span)
    {
        if (span < TimeSpan.Zero)
            span = TimeSpan.Zero;

        return $"{(int) span.TotalMinutes}:{span.Seconds:00}";
    }
}
