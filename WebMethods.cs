//AMP Rust Module - See LICENCE.txt
//©2017 CubeCoders Limited - All rights reserved.

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

        [UserAction("Kick")]
        [JSONMethod]
        private void Kick(string ID)
        {
            module.app.KickPlayer(module.app.GetPlayer(ID));
        }

        [UserAction("Ban")]
        [JSONMethod]
        private void Ban(string ID)
        {
            module.app.BanPlayer(module.app.GetPlayer(ID));
        }
    }
}
