using ModuleShared;
using System.Collections.Generic;

namespace RustModule
{
    public class RustConfig : SettingStore
    {
        public class RustSettings : SettingSectionStore
        {
            public string SteamCMDPath = "./rust/";
            public string OxideModDownloadURL = "https://umod.org/games/rust/download";

            [WebSetting("Install UMod", "Auto-installs UMod (Formerly Oxide mod) when the server is updated", false)]
            public bool InstallOxide = false;

            [ProvisionSetting("Game Port Number", CustomFieldTypes.UDPPort, 28015)]
            [WebSetting("Game Port Number", "", true, "server.port", CustomFieldTypes.UDPPort)]
            public int Port = 28015;

            [ProvisionSetting("RCON Port Number", CustomFieldTypes.TCPPort, 5678)]
            [WebSetting("RCON Port Number", "", true, "rcon.port", CustomFieldTypes.TCPPort)]
            public int RconPort = 5678;

            [ProvisionSetting("IP Binding", CustomFieldTypes.IPAddress)]
            [WebSetting("IP Binding", "The seed used to randomly generate the world", true, "server.ip", CustomFieldTypes.IPAddress)]
            public string IP = "0.0.0.0";

            [WebSetting("World Seed", "The seed used to randomly generate the world", false, "server.seed")]
            public int ServerSeed = 6738;

            [WebSetting("Server Name", "The name of the server as it appears in the server list", false, "server.hostname")]
            public string ServerName = "A Rust server";

            public static List<string> MapList(object context, IApplicationWrapper app) => new List<string>() { "Procedural Map", "Barren", "HapisIsland", "SavasIsland", "SavasIsland_koth" };

            [WebSetting("Level Name", "Which level to start the server with", false, "server.level")]
            [StringSelectionSource(typeof(RustSettings), nameof(MapList))]
            public string LevelName = "Procedural Map";

            [WebSetting("Server Description", "A short description of your server. Multiple lines separated with \\n", false, "server.description")]
            public string ServerDescription = "My Rust server - powered by AMP";

            [WebSetting("Header Image URL", "A http(s) link to a URL for your servers banner image", false, "server.headerimage")]
            public string HeaderImageURL = "";

            [WebSetting("Server Website URL", "A web link to your servers web page for the 'Visit Website' button", false, "server.url")]
            public string ServerWebsiteURL = "";

            [ProvisionSetting("Maximum Players", null, 24)]
            [WebSetting("Max Players", "The maximum number of players that can join the server", false, "server.maxplayers")]
            public int MaxPlayers = 100;

            [ProvisionSetting("Tick Rate", null, 30)]
            public int TickRate = 30;

            [WebSetting("Automatically Update", "Whether or not to automatically update the server when updates become available", false)]
            public bool AutoUpdate = true;

            [WebSetting("Enable AntiCheat", "Enable CheatPunch Anti-Cheat", false, "antihack.enabled")]
            public bool AntiCheat = false;

            [WebSetting("World Size", "Maximum size of the explorable world", false, "server.worldsize")]
            [Range(2000, 8000)]
            public int WorldSize = 4000;

            [WebSetting("Save Interval", "How frequently in seconds the server should save changes to disk", false, "server.saveinterval")]
            public int SaveInterval = 4000;

            [WebSetting("Enable PvE", "Disallow player v.s. player combat", false, "server.pve")]
            public bool EnablePvE = false;

            [ProvisionSetting("Custom startup arguments")]
            public string CustomArgs = "";
        }

        public RustSettings Rust = new RustSettings();
    }
}
