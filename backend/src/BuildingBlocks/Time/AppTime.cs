namespace BuildingBlocks.Time;

/// <summary>
/// A „nap” és a „nap vége” fogalma a felhasználók időzónájában értendő (pl. jövőbeli dátum
/// tiltása, kihagyott edzés). Egyelőre egyetlen, rögzített időzónával dolgozunk.
/// </summary>
public static class AppTime
{
    public static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Budapest");

    public static DateOnly GetLocalToday(this TimeProvider timeProvider) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), Zone).DateTime);
}
