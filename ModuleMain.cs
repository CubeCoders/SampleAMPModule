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
        internal ILogger log;                  //Provides logging, you must use this - never do Console.WriteLine directly.
        internal IRunningTasksManager taskmgr; //Allows you to display the status of running tasks in the UI, such as updates
        internal RustConfig settings;          //Defined in RustConfig.cs - information on what settings you want.
        internal IConfigSerializer config;     //This lets you save/load your config. This is handled automatically for you by default.
        internal SupportedOS os;               //The OS you're running on, if you need to do different things on different OSs (Win/Linux) then you check this.
        internal steamcmdhelper steamcmd;      //Provided by the SteamCMD Plugin that ships with AMP to let you download/update games from Steam.
        internal RustApp app;                  //Defined in RustApp.cs, this is where you implement your wrapping logic (start/stop/console/players/etc)
        internal IFeatureManager features;     //Used to access features exposed by other plugins, such as the steamcmdhelper from the SteamCMD plugin


        //ModuleMain is the entry point for a module, it's the first thing that gets called when AMP loads the module.
        public ModuleMain(ILogger log, IConfigSerializer config, SupportedOS currentPlatform, IRunningTasksManager taskManager, IFeatureManager features)
        {
            this.log = log;
            this.taskmgr = taskManager;
            this.config = config;
            this.os = currentPlatform;
            this.features = features;
            this.settings = config.Load<RustConfig>(); //This will also automatically save your settings for you when the user changes them. You don't need to do anything
        }

        //Init is called after all of the plugins/modules have been loaded. At this point you need to provide a reference to your application wrapper and any 
        //extra methods you want to expose to the API.
        public override void Init(out IApplicationWrapper Application, out WebMethodsBase APIMethods)
        {
            app = new RustApp(this);
            Application = app;
            APIMethods = new WebMethods(this);
        }

        public override bool HasFrontendContent => true;

        //PostInit is called after all of the plugins have been initialized. At this point you can request features from other plugins.
        public override void PostInit()
        {
            steamcmd = features.RequestFeature<steamcmdhelper>();   //Get a copy of a steamcmd helper.
            steamcmd.CurrentApp = steamcmd.GetAppInfo(258550);      //Rust dedicated server
            steamcmd.WorkingDirectory = settings.Rust.SteamCMDPath;
            
            app.PostInit();
        }

        //Lets AMP know where are settings are being stored. You can have multiple setting stores but this generally isn't used.
        public override IEnumerable<SettingStore> SettingStores => new List<SettingStore>() { settings };
    }
}
