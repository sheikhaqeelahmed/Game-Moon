namespace PlanetScrappers.Core
{
    /// <summary>
    /// Single source of truth for the game version. The build script copies it into
    /// PlayerSettings.bundleVersion and uses it to name the QA zip.
    /// </summary>
    public static class GameVersion
    {
        public const string Version = "0.2.0";
        public const string Milestone = "Phase 2: Iron District";
        public const string ProductName = "Planet Scrappers";

        public static string Label => $"{ProductName} v{Version} · {Milestone}";
    }
}
