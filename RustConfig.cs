//AMP Rust Module - See LICENCE.txt
//©2017 CubeCoders Limited - All rights reserved.

using ModuleShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RustModule
{
    public class RustConfig : SettingStore
    {
        public class RustSettings : SettingSectionStore
        {
            public string SteamCMDPath = "./rust/";
            [WebSetting("Install Oxide Mod", "Auto-installs the Oxide mod when the server is updated", "Rust Settings", false)]
            public bool InstallOxide = false;
            [ProvisionSetting("Game Port Number", CustomFieldTypes.UDPPort, 28015)]
            [WebSetting("Game Port Number", "", "HIDDEN", true, "server.port")]
            public int Port = 28015;
            [ProvisionSetting("RCON Port Number", CustomFieldTypes.TCPPort, 5678)]
            [WebSetting("RCON Port Number", "", "HIDDEN", true, "rcon.port")]
            public int RconPort = 5678;
            [ProvisionSetting("IP Binding", CustomFieldTypes.IPAddress)]
            [WebSetting("IP Binding", "The seed used to randomly generate the world", "HIDDEN", true, "server.ip")]
            public string IP = "0.0.0.0";
            [WebSetting("World Seed", "The seed used to randomly generate the world", "Rust Settings", false, "server.seed")]
            public int ServerSeed = 6738;
            [WebSetting("Server Name", "The name of the server as it appears in the server list", "Rust Settings", false, "server.hostname")]
            public string ServerName = "A Rust server";
            [WebSetting("Server Description", "A short description of your server. Multiple lines separated with \\n", "Rust Settings", false, "server.description")]
            public string ServerDescription = "My Rust server - powered by AMP";
            [WebSetting("Header Image URL", "A http(s) link to a URL for your servers banner image", "Rust Settings", false, "server.headerimage")]
            public string HeaderImageURL = "";
            [WebSetting("Server Website URL", "A web link to your servers web page for the 'Visit Website' button", "Rust Settings", false, "server.url")]
            public string ServerWebsiteURL = "";
            [ProvisionSetting("Maximum Players", null, 24)]
            [WebSetting("Max Players", "The maximum number of players that can join the server", "Rust Settings", false, "server.maxplayers")]
            public int MaxPlayers = 100;
            [ProvisionSetting("Tick Rate", null, 30)]
            public int TickRate = 30;
            [WebSetting("Automatically Update", "Whether or not to automatically update the server when updates become available", "Updates", false)]
            public bool AutoUpdate = true;
            [WebSetting("Enable AntiCheat", "Enable CheatPunch Anti-Cheat", "Security", false, "antihack.enabled")]
            public bool AntiCheat = false;
            [WebSetting("World Size", "Maximum size of the explorable world", "Gameplay", false, "server.worldsize")]
            [Range(2000, 8000)]
            public int WorldSize = 4000;
            [WebSetting("Save Interval", "How frequently in seconds the server should save changes to disk", "Rust Settings", false, "server.saveinterval")]
            public int SaveInterval = 4000;
            [WebSetting("Enable PvE", "Disallow player v.s. player combat", "Gameplay", false, "server.pve")]
            public bool EnablePvE = false;

            [ProvisionSetting("Custom startup arguments")]
            public string CustomArgs = "";
        }

        public RustSettings Rust = new RustSettings();
    }
}
