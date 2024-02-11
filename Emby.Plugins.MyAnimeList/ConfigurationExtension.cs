using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Providers;

namespace Emby.Plugins.MyAnimeList
{
    public static class ConfigurationExtension
    {
        public static MyAnimeListOptions GetMyAnimeListOptions(this IConfigurationManager manager)
        {
            return manager.GetConfiguration<MyAnimeListOptions>("myanimelist");
        }
    }

    public class OpenSubtitleConfigurationFactory : IConfigurationFactory
    {
        public IEnumerable<ConfigurationStore> GetConfigurations()
        {
            return new ConfigurationStore[]
            {
                new ConfigurationStore
                {
                    Key = "myanimelist",
                    ConfigurationType = typeof (MyAnimeListOptions)
                }
            };
        }
    }

    public class MyAnimeListOptions
    {
        public string ClientID { get; set; }

    }
}
