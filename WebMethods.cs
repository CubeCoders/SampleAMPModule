using ModuleShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RustModule
{
    class WebMethods : WebMethodsBase
    {
        private ModuleMain module;
        public WebMethods(ModuleMain module)
        {
            this.module = module;
        }

        public enum InGameActionPermissions
        {
            KickPlayer,
            BanPlayer
        }

        [UserAction("Kick")]
        [JSONMethod]
        [RequiresPermissions(InGameActionPermissions.KickPlayer)]
        private void Kick(string ID)
        {
            module.app.KickPlayer(module.app.GetPlayer(ID));
        }

        [UserAction("Ban")]
        [JSONMethod]
        [RequiresPermissions(InGameActionPermissions.BanPlayer)]
        private void Ban(string ID)
        {
            module.app.BanPlayer(module.app.GetPlayer(ID));
        }
    }
}
