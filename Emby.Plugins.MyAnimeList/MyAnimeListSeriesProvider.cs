using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Serialization;

namespace Emby.Plugins.MyAnimeList
{
    public class MyAnimeListSeriesProvider : IRemoteMetadataProvider<Series, SeriesInfo>, IHasOrder, IHasSupportedExternalIdentifiers, IHasMetadataFeatures
    {
        private readonly ILogger _log;
        private readonly IHttpClient _httpClient;
        private readonly IConfigurationManager _config;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly Api _api;
        public int Order => 5;
        public static string StaticName = "MyAnimeList";
        public string Name => StaticName;

        public MyAnimeListSeriesProvider(IConfigurationManager config, IHttpClient httpClient, IJsonSerializer jsonSerializer, ILogManager logManager)
        {
            _log = logManager.GetLogger(StaticName);
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _config = config;
            _api = new Api(_log, _httpClient, _jsonSerializer);
        }

        public string[] GetSupportedExternalIdentifiers()
        {
            return new[] {

                Name
            };
        }

        public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo searchInfo, CancellationToken cancellationToken)
        {
            _api.clientID = _config.GetMyAnimeListOptions().ClientID;
            var result = new MetadataResult<Series>();

            var aid = searchInfo.GetProviderId(Name);
            if (string.IsNullOrEmpty(aid))
            {
                aid = await _api.FindSeries(searchInfo.Name, searchInfo.EnableAdultMetadata, cancellationToken).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(aid))
            {
                result = await _api.GetMetadata(aid, searchInfo.MetadataLanguage, searchInfo.EnableAdultMetadata, cancellationToken);
            }
            return result;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo searchInfo, CancellationToken cancellationToken)
        {
            _api.clientID = _config.GetMyAnimeListOptions().ClientID;
            var results = new Dictionary<string, RemoteSearchResult>();
            var aid = searchInfo.GetProviderId(Name);
            if (!string.IsNullOrEmpty(aid))
            {
                results.Add(aid, await _api.GetAnime(aid, searchInfo.MetadataLanguage, searchInfo.EnableAdultMetadata, cancellationToken));
            }

            if (!string.IsNullOrEmpty(searchInfo.Name))
            {
                List<string> ids = await _api.Search_GetSeries_list(searchInfo.Name, searchInfo.EnableAdultMetadata, cancellationToken).ConfigureAwait(false);
                foreach (string a in ids)
                {
                    results.Add(a, await _api.GetAnime(a, searchInfo.MetadataLanguage, searchInfo.EnableAdultMetadata, cancellationToken));
                }
            }

            return results.Values;
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url
            });
        }

        public MetadataFeatures[] Features => new[] { MetadataFeatures.Adult };
    }

    public class MyAnimeListSeriesImageProvider : IRemoteImageProvider
    {
        private readonly ILogger _log;
        private readonly IHttpClient _httpClient;
        private readonly IApplicationPaths _appPaths;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IConfigurationManager _config;
        private readonly Api _api;

        public MyAnimeListSeriesImageProvider(IHttpClient httpClient, IJsonSerializer jsonSerializer, IApplicationPaths appPaths, ILogManager logManager, IConfigurationManager config)
        {
            _log = logManager.GetLogger(MyAnimeListSeriesProvider.StaticName);
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _appPaths = appPaths;
            _config = config;
            _api = new Api(_log, _httpClient, _jsonSerializer);
        }

        public string Name => MyAnimeListSeriesProvider.StaticName;

        public bool Supports(BaseItem item) => item is Series || item is Season;

        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            return new[] { ImageType.Primary };
        }

        public Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, LibraryOptions libraryOptions, CancellationToken cancellationToken)
        {
            var seriesId = item.GetProviderId(MyAnimeListSeriesProvider.StaticName);
            return GetImages(seriesId, cancellationToken);
        }

        public async Task<IEnumerable<RemoteImageInfo>> GetImages(string aid, CancellationToken cancellationToken)
        {
            _api.clientID = _config.GetMyAnimeListOptions().ClientID;
            var list = new List<RemoteImageInfo>();
            if (!string.IsNullOrEmpty(aid))
            {
                list = await _api.Get_RemoteImageListAsync(aid, cancellationToken).ConfigureAwait(false);
            }
            return list;
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url
            });
        }
    }
}