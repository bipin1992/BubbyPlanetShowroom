namespace BubbyPlanetShowroom
{
    /// <summary>
    /// Hardcoded software version date. Change <see cref="ReleaseDate"/> when you ship a new build.
    /// </summary>
    internal static class AppVersion
    {
        public const string ReleaseDate = "06-Oct-2026";

        public static string Display => "Version " + ReleaseDate;
    }
}
