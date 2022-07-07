using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using MediaBrowser.Model.Drawing;
using System.IO;

namespace Emby.Plugins.MyAnimeList
{
    public class Plugin : BasePlugin, IHasWebPages, IHasThumbImage
    {
        public override string Name
        {
            get { return "MyAnimeList"; }
        }

        public static Plugin Instance { get; private set; }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "myanimelist",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.myanimelist.html",
                    EnableInMainMenu = true,
                    MenuSection = "server",
                    MenuIcon = "closed_caption"
                },
                new PluginPageInfo
                {
                    Name = "myanimelistjs",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.myanimelist.js"
                }
            };
        }

        private Guid _id = new Guid("30FD75D0-866D-44FC-8E2F-7D05184FE195");

        public override Guid Id
        {
            get { return _id; }
        }

        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".thumb.png");
        }

        public ImageFormat ThumbImageFormat
        {
            get
            {
                return ImageFormat.Png;
            }
        }
    }
}