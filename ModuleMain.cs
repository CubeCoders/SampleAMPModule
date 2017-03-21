//AMP Rust Module - See LICENCE.txt
//©2017 CubeCoders Limited - All rights reserved.

using ModuleShared;
using steamcmdplugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RustModule
{
    [AMPDependency(nameof(steamcmdplugin), nameof(RCONClientPlugin))]
    public class ModuleMain : AppModule
    {
        internal ILogger log;
        internal IRunningTasksManager taskmgr;
        internal RustConfig settings;
        internal IConfigSerializer config;
        internal SupportedOS os;
        internal steamcmdhelper steamcmd;
        internal RustApp app;
        internal IFeatureManager features;

        public ModuleMain(ILogger log, IConfigSerializer config, SupportedOS currentPlatform, IRunningTasksManager taskManager, IFeatureManager features)
        {
            this.log = log;
            this.taskmgr = taskManager;
            this.config = config;
            this.os = currentPlatform;
            this.features = features;
            this.settings = config.Load<RustConfig>();
        }

        public override void Init(out IApplicationWrapper Application, out WebMethodsBase APIMethods)
        {
            app = new RustApp(this);
            Application = app;
            APIMethods = new WebMethods(this);
        }

        public override bool HasFrontendContent => true;

        public override void PostInit()
        {
            steamcmd = features.RequestFeature<steamcmdhelper>();
            steamcmd.CurrentApp = steamcmd.GetAppInfo(258550); //Rust dedicated server
            steamcmd.WorkingDirectory = settings.Rust.SteamCMDPath;
            
            app.PostInit();
        }

        public override IEnumerable<SettingStore> SettingStores => new List<SettingStore>() { settings };
    }
}
