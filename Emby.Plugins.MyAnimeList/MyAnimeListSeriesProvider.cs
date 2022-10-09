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
using Emby.Anime;

namespace Emby.Plugins.MyAnimeList
{
    public class MyAnimeListSeriesProvider : IRemoteMetadataProvider<Series, SeriesInfo>, IHasOrder
    {
        private readonly ILogger _log;
        private readonly IHttpClient _httpClient;
        private readonly IConfigurationManager _config;
        private readonly Api _api;
        public int Order => 5;
        public static string StaticName = "MyAnimeList";
        public string Name => StaticName;

        public MyAnimeListSeriesProvider(IConfigurationManager config, IHttpClient httpClient, ILogManager logManager)
        {
            _log = logManager.GetLogger(StaticName);
            _api = new Api(_log, httpClient);
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Series>();

            var aid = info.GetProviderId(Name);
            if (string.IsNullOrEmpty(aid))
            {
                aid = await _api.FindSeries(info.Name, _config.GetMyAnimeListOptions(), cancellationToken).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(aid))
            {
                string WebContent = await _api.WebRequestAPI(_api.anime_link + aid, cancellationToken).ConfigureAwait(false);
                result.Item = new Series();
                result.HasMetadata = true;

                result.Item.SetProviderId(Name, aid);
                result.Item.Name = _api.SelectName(WebContent, info.MetadataLanguage);
                result.Item.Overview = _api.Get_OverviewAsync(WebContent);
                result.ResultLanguage = "eng";
                try
                {
                    result.Item.CommunityRating = float.Parse(_api.Get_RatingAsync(WebContent), System.Globalization.CultureInfo.InvariantCulture);
                }
                catch (Exception) { }
                foreach (var genre in _api.Get_Genre(WebContent))
                {
                    if (!string.IsNullOrEmpty(genre))
                    {
                        result.Item.AddGenre(genre);
                    }
                }
                GenreHelper.CleanupGenres(result.Item);
                //StoreImageUrl(aid, _api.Get_ImageUrlAsync(WebContent), "image");
            }
            return result;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo searchInfo, CancellationToken cancellationToken)
        {
            var aid = searchInfo.GetProviderId(Name);
            if (!string.IsNullOrEmpty(aid))
            {
                var metadata = await GetMetadata(searchInfo, cancellationToken).ConfigureAwait(false);

                if (metadata.HasMetadata)
                {
                    return new List<RemoteSearchResult>
                    {
                        metadata.ToRemoteSearchResult(Name)
                    };
                }
            }

            if (!string.IsNullOrEmpty(searchInfo.Name))
            {
                var results = new List<RemoteSearchResult>();

                List<string> ids = await _api.Search_GetSeries_list(searchInfo.Name, _config.GetMyAnimeListOptions(), cancellationToken).ConfigureAwait(false);
                foreach (string a in ids)
                {
                    var subSearchInfo = new SeriesInfo
                    {
                        DisplayOrder = searchInfo.DisplayOrder,
                        EnableAdultMetadata = searchInfo.EnableAdultMetadata,
                        EpisodeAirDate = searchInfo.EpisodeAirDate,
                        IndexNumber = searchInfo.IndexNumber,
                        IsAutomated = searchInfo.IsAutomated,
                        MetadataCountryCode = searchInfo.MetadataCountryCode,
                        MetadataLanguage = searchInfo.MetadataLanguage,
                        ParentIndexNumber = searchInfo.ParentIndexNumber,
                        PremiereDate = searchInfo.PremiereDate,
                        ProviderIds = new ProviderIdDictionary()
                    };
                    subSearchInfo.SetProviderId(Name, a);

                    results.Add(await _api.GetAnime(a, searchInfo.MetadataLanguage, cancellationToken).ConfigureAwait(false));
                    
                    //var metadata = await GetMetadata(subSearchInfo, cancellationToken).ConfigureAwait(false);

                    //if (metadata.HasMetadata)
                    //{
                    //    results.Add(metadata.ToRemoteSearchResult(Name));
                    //}
                }

                return results;
            }

            return new List<RemoteSearchResult>();
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

    public class MyAnimeListSeriesImageProvider : IRemoteImageProvider
    {
        private readonly IHttpClient _httpClient;
        private readonly IApplicationPaths _appPaths;
        private readonly Api _api;

        public MyAnimeListSeriesImageProvider(IHttpClient httpClient, IApplicationPaths appPaths, ILogManager logManager)
        {
            var logger = logManager.GetLogger("MyAnimeList");
            _api = new Api(logger, httpClient);
            _httpClient = httpClient;
            _appPaths = appPaths;
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
            var list = new List<RemoteImageInfo>();

            if (!string.IsNullOrEmpty(aid))
            {
                var primary = _api.Get_ImageUrlAsync(await _api.WebRequestAPI(_api.anime_link + aid, cancellationToken).ConfigureAwait(false));
                list.Add(new RemoteImageInfo
                {
                    ProviderName = Name,
                    Type = ImageType.Primary,
                    Url = primary
                });
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