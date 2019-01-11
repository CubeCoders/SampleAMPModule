//AMP Rust Module - See LICENCE.txt
//©2017-2019 CubeCoders Limited - All rights reserved.

using Ionic.Zip;
using ModuleShared;
using RCONPlugin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RustModule
{
    public class RustApp : AppServerBase, IApplicationWrapper, IHasReadableConsole, IHasWriteableConsole, IHasSimpleUserList
    {
        private ModuleMain module;

        private bool IgnoreStatus = false;
        private bool IssueUpdateEvent = true;

        public RustApp(ModuleMain module)
        {
            this.module = module;
            this.Users = new List<SimpleUser>();
        }

        [MessageHandler(@"^New version detected!$")]
        private bool UpdateAvailableHandler(Match match)
        {
            if (IssueUpdateEvent)
            {
                UpdateAvailable.InvokeSafe(this, EventArgs.Empty);
                IssueUpdateEvent = false;

                if (module.settings.Rust.AutoUpdate)
                {
                    Update();
                }
            }

            return false;
        }

        Regex statusLineRegex = new Regex(@"^(\d+?)\s+""(.+?)""\s+(\d+?)\s+(.+?)\s+(.+?):(\d+?)\s+(.+?)$");

        [MessageHandler(@"^hostname.+$", Options: RegexOptions.Singleline)]
        private bool StatusResponse(Match match)
        {
            if (IgnoreStatus)
            {
                IgnoreStatus = false;

                var lines = match.Value.Split('\n');

                foreach (var l in lines)
                {
                    var lineMatch = statusLineRegex.Match(l);
                    if (lineMatch.Success)
                    {
                        StatusUserLine(lineMatch);
                    }
                }

                return true;
            }
            return false;
        }

        private bool StatusUserLine(Match match)
        {
            string steamId = match.Groups[1].Value;
            string playerName = match.Groups[2].Value.Replace(@"\""", "\"");

            IPAddress.TryParse(match.Groups[5].Value, out IPAddress address);
            int.TryParse(match.Groups[6].Value, out int port);

            var user = Users.Where(u => u.Name == playerName).FirstOrDefault();
            var endpoint = new IPEndPoint(address, port);

            if (user != null)
            {
                if (!string.IsNullOrEmpty(user.Id))
                {
                    return false;
                }

                user.RemoteEndpoint = endpoint;
                user.Id = steamId;
            }
            else
            {
                var newUser = new SimpleUser()
                {
                    Name = playerName,
                    JoinTime = DateTime.Now,
                    RemoteEndpoint = endpoint,
                    Id = steamId,
                };

                Users.Add(newUser);
                UserJoins.InvokeSafe(this, new UserEventArgs(newUser));
            }

            return false;
        }

        [MessageHandler(@"^Unsupported encoding: 'utf8'$")]
        [MessageHandler(@"^Reporting Performance Data system/server$")]
        private bool IgnoreLine(Match match)
        {
            return true;
        }

        private void InternalQueryStatus()
        {
            IgnoreStatus = true;
            PostMessage("status");
        }

        [MessageHandler(@"^(.+?) was killed by (.+?)$")]
        private bool PlayerKilled(Match match)
        {
            var playerName = match.Groups[1].Value;
            var deathMethod = match.Groups[2].Value;

            var victim = Users.Where(u => u.Name == playerName).FirstOrDefault();
            var attacker = Users.Where(u => u.Name == deathMethod).FirstOrDefault();

            if (victim == null) { return false; } //How'd that happen?

            if (attacker != null)
            {
                PlayerKilledByPlayer.InvokeSafe(this, new PlayerKilledEventArgs(victim, attacker));
            }
            else
            {
                PlayerKilledByEnvironment.InvokeSafe(this, new PlayerKilledByEnvironmentEventArgs(victim, deathMethod));
            }

            Users.Remove(victim);

            return false;
        }

        [MessageHandler(@"^([\d\.]+):(\d+)\/(\d+)\/(.+?) disconnecting: (.+?)$")]
        private bool PlayerLeave(Match match)
        {
            var playerId = match.Groups[3].Value;
            var playerName = match.Groups[4].Value;
            var reason = match.Groups[5].Value;

            var user = Users.Where(u => u.Name == playerName).FirstOrDefault();

            if (user == null) { return false; } //How'd that happen?

            UserLeaves.InvokeSafe(this, new UserEventArgs(user));

            Users.Remove(user);

            return false;
        }

        [MessageHandler(@"^\[RCON\]\[(.+?)\] (.+)$")]
        private bool RconCommand(Match match)
        {
            var address = match.Groups[1].Value;
            var message = match.Groups[2].Value;

            if (IgnoreStatus && message == "status")
            {
                return true;
            }

            var newEntry = new ConsoleEntry()
            {
                Contents = message,
                Source = "RCON/" + address,
                SourceId = "RCON/" + address,
                Type = "Console",
                Timestamp = DateTime.Now
            };

            AddConsoleEntry(newEntry);
            module.log.ConsoleOutput(match.Value);

            return true;
        }

        [MessageHandler(@"^\[CHAT\] (.+?): (.+)$")]
        private bool PlayerChat(Match match)
        {
            var playerName = match.Groups[1].Value;
            var message = match.Groups[2].Value;

            var user = Users.Where(u => u.Name == playerName).FirstOrDefault();
            if (user == null) { return false; } //How'd that happen?

            var newEntry = new ConsoleEntry()
            {
                Contents = message,
                Source = user.Name,
                SourceId = user.Id,
                Type = "Chat",
                Timestamp = DateTime.Now
            };

            AddConsoleEntry(newEntry);
            module.log.Chat(playerName, message);
            PlayerChats.InvokeSafe(this, new UserChatEventArgs(user, message));

            return true;
        }

        [MessageHandler(@"^([\d\.]+):(\d+)\/(\d+)\/(.+?) joined \[(.+?)\/(\d+)\]$")]
        private bool PlayerJoined(Match match)
        {
            InternalQueryStatus();
            return false;
        }

        internal void PostInit()
        {
            module.steamcmd.UpdateProgressChange += Steamcmd_UpdateProgressChange;
        }

        private steamcmdplugin.Steamcmdhelper.AppInfo CurrentApp => module.steamcmd.CurrentApp;

        private readonly Dictionary<SupportedOS, string> SRCDSAppPath = new Dictionary<SupportedOS, string>()
        {
            { SupportedOS.Windows, "RustDedicated.exe" },
            { SupportedOS.Linux, "RustDedicated" }
        };

        private string WorkingDir => Path.Combine(module.settings.Rust.SteamCMDPath, CurrentApp.ID.ToString());
        private string ServerFile => Path.Combine(WorkingDir, SRCDSAppPath[this.module.os]);

        public bool IsGameServerInstalled() => File.Exists(ServerFile);

        /// <summary>
        /// Generates a random password for RCON and creates a new ApplicationProcess StartInfo
        /// </summary>
        private void SetupProcess()
        {
            ApplicationProcess = new AMPProcess()
            {
                Win32RequiresConsoleAssistant = false,
            };
            var id = CurrentApp.ID;

            RandomRCONPassword = GenerateRandomPassword();

            //GetTaggedValues() returns information about all of the settings according to their tag, which is the final parameter of the WebSetting attribute.
            var settingArgs = string.Join(" ", module.settings.Rust.GetTaggedValues().Select(kvp => $"+{kvp.Key} \"{kvp.Value.ToString()}\""));

            string args = $"-batchmode -nographics -logFile +rcon.password \"{RandomRCONPassword}\" {settingArgs} +rcon.web \"False\" {module.settings.Rust.CustomArgs}";

            ApplicationProcess.StartInfo = new ProcessStartInfo()
            {
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                ErrorDialog = false,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = WorkingDir,
                FileName = ServerFile,
                Arguments = args,
            };

            //Rust requires some extra environment variables on Linux to tell it where its own plugins are.
            if (module.os == SupportedOS.Linux)
            {
                string LD_LIBRARY_PATH = Environment.GetEnvironmentVariable(nameof(LD_LIBRARY_PATH));
                var AddPath = Path.Combine(WorkingDir, "RustDedicated_Data", "Plugins", "x86_64");
                LD_LIBRARY_PATH = LD_LIBRARY_PATH + $":{AddPath}";
                ApplicationProcess.StartInfo.AddEnvirionmentVariable(nameof(LD_LIBRARY_PATH),LD_LIBRARY_PATH);
            }

            ApplicationProcess.EnableRaisingEvents = true;
            ApplicationProcess.Exited += ApplicationProcess_Exited;
            ApplicationProcess.OutputDataReceived += ApplicationProcess_OutputDataReceived;
            ApplicationProcess.ErrorDataReceived += ApplicationProcess_OutputDataReceived;
        }

        private void ApplicationProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            module.log.Debug(e.Data);
        }

        private void ApplicationProcess_Exited(object sender, EventArgs e)
        {
            var unexpectedStop = (this.State != ApplicationState.Stopping && this.State != ApplicationState.Restarting);
            var needsRestart = (this.State == ApplicationState.Restarting);

            this.State = ApplicationState.Stopped;

            if (unexpectedStop)
            {
                this.module.log.Warning("The application stopped unexpectedly.");
                UnexpectedStop.InvokeSafe(this, new ServerStoppedEventArgs(DateTime.Now));
            }
            else
            {
                NormalStop.InvokeSafe(this, new ServerStoppedEventArgs(DateTime.Now));
            }

            this.Users.Clear();

            if (needsRestart)
            {
                Start();
            }
        }

        public void StartGameServer()
        {
            if (!IsGameServerInstalled()) // If our Game Server directory does not exist with the srcds executable in
            {
                try
                {
                    Update();
                }
                catch
                {
                    this.State = ApplicationState.Failed;
                    return;
                }
            }

            this.State = ApplicationState.PreStart;
            IssueUpdateEvent = true;

            if (!IsGameServerInstalled())
            {
                this.State = ApplicationState.Failed;
                module.log.Warning("Rust is not installed!");
                return;
            }

            SetupProcess();
            ApplicationProcess.Start();
            SetupRCON();
            this.State = ApplicationState.Starting;
        }

        private SourceRconClient rcon;

        private async void SetupRCON()
        {
            await Task.Delay(10000);

            if (this.State != ApplicationState.Starting)
            {
                module.log.Warning("Tried to start RCON but server is not running!");
                return;
            }

            try
            {
                rcon = new SourceRconClient();
                rcon.DataRecieved += Rcon_DataRecieved;

                bool connectResult = false;
                int rconRetry = 0;

                //Keep trying to connect to RCON over and over until we either succeed, get a permission denied, or the server stops.
                do
                {
                    string rconTargetIP = "";
                    if (IPAddress.TryParse(module.settings.Rust.IP, out var BindIP))
                    {
                        rconTargetIP = IPAddress.Equals(BindIP, IPAddress.Any) ? IPAddress.Loopback.ToString() : BindIP.ToString();
                    }

                    connectResult = await rcon.Connect(rconTargetIP, module.settings.Rust.RconPort);

                    if (connectResult == false)
                    {
                        await Task.Delay(10000);
                        rconRetry++;
                    }
                } while (connectResult == false && this.State == ApplicationState.Starting);

                if (!connectResult)
                {
                    throw new Exception("Unable to connect.");
                }

                var authResult = await rcon.Login(RandomRCONPassword);

                //Once we're connected, the application can transition to the Ready state - even if auth failed (but this stops the console being usable)
                if (connectResult && authResult)
                {
                    PostMessage("chat.serverlog true");
                    module.log.Info("RCON connection successful.");
                    State = ApplicationState.Ready;
                }
                else
                {
                    module.log.Warning("RCON connection failed, console write unavailable.");
                    State = ApplicationState.Ready;
                }
            }
            catch
            {
                module.log.Warning("RCON connection failed, console write unavailable.");
                State = ApplicationState.Ready;
            }
        }

        void Rcon_DataRecieved(object sender, SourceRconPacket e)
        {
            if (e.ID != 0) { return; }
            ProcessMessage(e.Body);
        }

        void ProcessMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) { return; }

            var newEntry = new ConsoleEntry()
            {
                Contents = message,
                Source = "Console",
                Type = "Console",
                Timestamp = DateTime.Now
            };

            if (!ProcessOutput(message))
            {
                AddConsoleEntry(newEntry);
                this.module.log.ConsoleOutput(message);
            }

        }

        internal SimpleUser GetPlayer(string Id)
        {
            return Users.Where(u => u.UID == Id).FirstOrDefault();
        }

        [ScheduleableTask("Kick a player")]
        public void KickPlayer(SimpleUser Player) => WriteLine($"kick {Player.Name}");

        [ScheduleableTask("Ban a player")]
        public void BanPlayer(SimpleUser Player) => WriteLine($"ban {Player.Name}");

        [ScheduleableTask("Save the map and player inventories")]
        public void SaveMap() => WriteLine("save.all");

        [ScheduleableTask("Send a chat message to everyone")]
        public void ChatMessage(string Message) => WriteLine($"say {Message}");

        [ScheduleableTask("Send a popup message to everyone")]
        public void PopupMessage(string Message) => WriteLine($"notice.popupall {Message}");

        [ScheduleableTask("Perform a console command")]
        public void ConsoleCommand(string Command) => PostMessage(Command);

        [ScheduleableTask("Start the Rust server")]
        public ActionResult Start()
        {
            StartGameServer();
            return ActionResult.Success;
        }

        [ScheduleableTask("Stop the Rust server")]
        public void Stop()
        {
            StopApplication(false);
        }

        public ActionResult Sleep()
        {
            throw new NotSupportedException();
        }

        [ScheduleableTask("Restart the Rust server")]
        public void Restart()
        {
            StopApplication(true);
        }

        public void StopApplication(bool andRestart = false)
        {
            if (this.State != ApplicationState.Stopped)
            {
                this.State = (andRestart) ? ApplicationState.Restarting : ApplicationState.Stopping;
                WriteLine("quit");
            }
        }

        public void Kill()
        {
            if (this.State != ApplicationState.Stopped)
            {
                ApplicationProcess.Kill();
            }
        }

        /// <summary>
        /// Called when the update progress changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Steamcmd_UpdateProgressChange(object sender, steamcmdplugin.Steamcmdhelper.UpdateProgressChangedEventArgs e)
        {
            if (e.Finished)
            {
                this.State = ApplicationState.Stopped;
            }
            else if (e.Error)
            {
                this.State = ApplicationState.Failed;
            }
        }

        public ActionResult Update()
        {
            if (this.State != ApplicationState.Stopped)
            {
                NormalStop += RustApp_AutoUpdate;
                this.Stop();
            }
            else
            {
                this.State = ApplicationState.Installing;
                module.steamcmd.UpdateAndInstallInBackground(this.ApplicationName);
                module.steamcmd.UpdateProgressChange += (s, e) => { if (e.Finished) { InstallOxide(); } };
            }

            return ActionResult.Success;
        }

        private async void InstallOxide()
        {
            if (!module.settings.Rust.InstallOxide) { return; }

            var outFile = Path.Combine(WorkingDir, "Oxide-Rust.zip");

            var task = module.taskmgr.CreateTask("Updating UMod");

            if (await Utilities.DownloadFileWithProgressAsync(module.settings.Rust.OxideModDownloadURL, outFile, task) == false)
            {
                return;
            }

            if (module.os == SupportedOS.Windows)
            {
                using (var zip = new ZipFile(outFile))
                {
                    await zip.ExtractAllAsync(WorkingDir, ExtractExistingFileAction.OverwriteSilently);
                }
            }
            else
            {
                Utilities.UnzipArchive_Linux(outFile, WorkingDir);
            }
        }

        void RustApp_AutoUpdate(object sender, ServerStoppedEventArgs e)
        {
            NormalStop -= RustApp_AutoUpdate;
            Update();
        }

        public int MaxRAMUsage => 0;

        public int MaxUsers => module.settings.Rust.MaxPlayers;

        public bool SupportsSleep => false;

        public string ApplicationName => "Rust Dedicated Server";

        public string ModuleName => "RustModule";

        public string ModuleAuthor => "CubeCoders Limited";

        public SupportedOS SupportedOperatingSystems => SupportedOS.Windows | SupportedOS.Linux;

        public bool CanRunVirtualized => true;

        public bool CanUpdateApplication => true;

        private string RandomRCONPassword = "";

#pragma warning disable 0162 //Ignore 'cannot be reached' because it's conditional on DEBUG
        private string GenerateRandomPassword()
        {
#if DEBUG
            return "testingpassword123";
#endif
            return Guid.NewGuid().ToString("N");
        }
#pragma warning restore 0162

        public void WriteLine(string message)
        {
            if (State != ApplicationState.Stopped && State != ApplicationState.Sleeping)
            {
                PostMessage(message);
            }
        }

        private void PostMessage(string Message)
        {
            if (rcon.Connected)
            {
                var result = rcon.SendMessage(new SourceRconPacket() { Body = Message, Type = SourceRconPacket.PacketType.ExecCommandOrAuthResponse }, true).GetAwaiter().GetResult();
            }
        }

        public List<SimpleUser> Users { get; private set; }

        [ScheduleableEvent("An update for the Rust server is available")]
        public event EventHandler<EventArgs> UpdateAvailable;

        [ScheduleableEvent("A player joins the Rust server")]
        public event EventHandler<UserEventArgs> UserJoins;

        [ScheduleableEvent("A player leaves the Rust server")]
        public event EventHandler<UserEventArgs> UserLeaves;

        [ScheduleableEvent("A player sends a chat message")]
        public event EventHandler<UserChatEventArgs> PlayerChats;

        public class PlayerKilledEventArgs : EventArgs
        {
            public SimpleUser Victim { get; private set; }
            public SimpleUser Attacker { get; private set; }

            public PlayerKilledEventArgs(SimpleUser Victim, SimpleUser Attacker)
            {
                this.Victim = Victim;
                this.Attacker = Attacker;
            }
        }

        public class PlayerKilledByEnvironmentEventArgs : EventArgs
        {
            public SimpleUser Victim { get; private set; }
            public string Method { get; private set; }

            public PlayerKilledByEnvironmentEventArgs(SimpleUser Victim, string Method)
            {
                this.Victim = Victim;
                this.Method = Method;
            }
        }

        public class ServerStoppedEventArgs : EventArgs
        {
            public DateTime Time { get; private set; }

            public ServerStoppedEventArgs(DateTime time)
            {
                this.Time = time;
            }
        }

        [ScheduleableEvent("The Rust Server stops unexpectedly")]
        public event EventHandler<ServerStoppedEventArgs> UnexpectedStop;

        [ScheduleableEvent("The Rust Server stops normally")]
        public event EventHandler<ServerStoppedEventArgs> NormalStop;

        [ScheduleableEvent("A player is killed by another player")]
        public event EventHandler<PlayerKilledEventArgs> PlayerKilledByPlayer;

        [ScheduleableEvent("A player is killed by the environment")]
        public event EventHandler<PlayerKilledByEnvironmentEventArgs> PlayerKilledByEnvironment;

        public string BaseDirectory
        {
            get => module.settings.Rust.SteamCMDPath;
            set => module.settings.Rust.SteamCMDPath = value;
        }

    }
}
