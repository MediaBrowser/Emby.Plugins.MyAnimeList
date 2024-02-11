using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using System.IO;
using MediaBrowser.Model.Net;
using System.Linq;
using System.Globalization;
using MediaBrowser.Model.Serialization;
using static MediaBrowser.Common.Updates.GithubUpdater;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace Emby.Plugins.MyAnimeList
{
    /// <summary>
    /// This API use the WebContent of MyAnimelist and the API of MyAnimelist
    /// </summary>
    public class Api
    {
        private static ILogger _logger;
        private static IJsonSerializer _jsonSerializer;
        //API v2 
        public string SearchLink = "https://api.myanimelist.net/v2/anime?q={0}&fields=alternative_titles&limit=10";
        public string AnimeLink = "https://api.myanimelist.net/v2/anime/{0}?fields=id,title,main_picture,alternative_titles,start_date,end_date,synopsis,mean,status,genres,broadcast,source,pictures,background,studios";
        //Web Fallback
        public string FallbackSearchLink = "https://myanimelist.net/search/all?q={0}";
        public string FallbackAnimeLink = "https://myanimelist.net/anime/";

        private IHttpClient _httpClient;

        public string clientID { get; set; }
        public string preferredMetadataLanguage { get; set; }

        /// <summary>
        /// WebContent API call to get a anime with id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Api(ILogger logger, IHttpClient httpClient, IJsonSerializer jsonSerializer, string clientID = "", string preferredMetadataLanguage = "eng")
        {
            _logger = logger;
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            this.clientID = clientID;
            this.preferredMetadataLanguage = preferredMetadataLanguage;
        }

        public async Task<MetadataResult<Series>> GetMetadata(string id, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Series>();
            //API
            if (!string.IsNullOrEmpty(clientID))
            {
                string json = await WebRequestAPI(string.Format(AnimeLink, id), cancellationToken, clientID).ConfigureAwait(false);
                AnimeObject anime = _jsonSerializer.DeserializeFromString<AnimeObject>(json);
                result.Item = new Series();
                result.HasMetadata = true;

                result.Item.SetProviderId(MyAnimeListSeriesProvider.StaticName, id);
                result.Item.Name = SelectName(anime);
                result.Item.OriginalTitle = anime.title;
                result.Item.Overview = anime.synopsis;
                if (!string.IsNullOrEmpty(anime.background))
                {
                    result.Item.Overview += "\n\nBackground: " + anime.background;
                }
                result.ResultLanguage = "eng";
                result.Item.CommunityRating = anime.mean;

                foreach (var studio in anime.studios)
                {
                    if (!string.IsNullOrEmpty(studio.name))
                    {
                        result.Item.AddStudio(studio.name);
                    }
                }

                foreach (var genre in anime.genres)
                {
                    if (!string.IsNullOrEmpty(genre.name))
                    {
                        result.Item.AddGenre(genre.name);
                    }
                }

                GenreHelper.CleanupGenres(result.Item);
            }
            else
            {
                //Fallback to Web
                string WebContent = await WebRequestAPI(FallbackAnimeLink + id, cancellationToken).ConfigureAwait(false);
                result.Item = new Series();
                result.HasMetadata = true;

                result.Item.SetProviderId(MyAnimeListSeriesProvider.StaticName, id);
                result.Item.Name = SelectNameFallback(WebContent);
                result.Item.Overview = Get_OverviewAsync(WebContent);
                result.ResultLanguage = "eng";
                try
                {
                    result.Item.CommunityRating = float.Parse(Get_RatingAsync(WebContent), System.Globalization.CultureInfo.InvariantCulture);
                }
                catch (Exception) { }
                foreach (var genre in Get_Genre(WebContent))
                {
                    if (!string.IsNullOrEmpty(genre))
                    {
                        result.Item.AddGenre(genre);
                    }
                }
                GenreHelper.CleanupGenres(result.Item);
            }

            return result;
        }

        public async Task<RemoteSearchResult> GetAnime(string id, CancellationToken cancellationToken)
        {
            var result = new RemoteSearchResult();
            //API
            if (!string.IsNullOrEmpty(clientID))
            {
                string json = await WebRequestAPI(string.Format(AnimeLink, id), cancellationToken, clientID).ConfigureAwait(false);
                AnimeObject anime = _jsonSerializer.DeserializeFromString<AnimeObject>(json);
                result.Name = SelectName(anime);
                result.SearchProviderName = MyAnimeListSeriesProvider.StaticName;
                result.ImageUrl = anime.main_picture.large;
                result.SetProviderId(MyAnimeListSeriesProvider.StaticName, id);
                result.Overview = anime.synopsis;
            }
            else
            {
                //Fallback to Web
                var WebContent = await WebRequestAPI(FallbackAnimeLink + id, cancellationToken).ConfigureAwait(false);
                result.Name = SelectNameFallback(WebContent);
                result.SearchProviderName = MyAnimeListSeriesProvider.StaticName;
                result.ImageUrl = Get_ImageUrlAsync(WebContent);
                result.SetProviderId(MyAnimeListSeriesProvider.StaticName, id);
                result.Overview = Get_OverviewAsync(WebContent);

            }

            return result;
        }

        /// <summary>
        ///  API call to select a prefence title
        /// </summary>
        /// <param name="AnimeObject"></param>
        /// <param name="preference"></param>
        /// <param name="language"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public string SelectName(AnimeObject animeObject)
        {
            if (string.Equals(preferredMetadataLanguage, "ja", StringComparison.OrdinalIgnoreCase))
            {
                var title = animeObject.alternative_titles.ja;
                if (!string.IsNullOrWhiteSpace(title))
                {
                    return title;
                }
            }
            if (string.IsNullOrEmpty(preferredMetadataLanguage) || preferredMetadataLanguage.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            {
                var title = animeObject.alternative_titles.en;
                if (!string.IsNullOrWhiteSpace(title))
                {
                    return title;
                }
            }

            return animeObject.title;
        }

        /// <summary>
        ///  WebContent call to select a prefence title
        /// </summary>
        /// <param name="WebContent"></param>
        /// <param name="preference"></param>
        /// <param name="language"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public string SelectNameFallback(string WebContent)
        {
            if (string.Equals(preferredMetadataLanguage, "ja", StringComparison.OrdinalIgnoreCase))
            {
                var title = Get_title("jap", WebContent);
                if (!string.IsNullOrWhiteSpace(title))
                {
                    return title;
                }
            }
            if (string.IsNullOrEmpty(preferredMetadataLanguage) || preferredMetadataLanguage.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            {
                var title = Get_title("en", WebContent);
                if (!string.IsNullOrWhiteSpace(title))
                {
                    return title;
                }
            }

            return Get_title("jap_r", WebContent);
        }

        /// <summary>
        /// WebContent API call get a specific title
        /// </summary>
        /// <param name="lang"></param>
        /// <param name="WebContent"></param>
        /// <returns></returns>
        public string Get_title(string lang, string WebContent)
        {
            switch (lang)
            {
                case "en":
                    return WebUtility.HtmlDecode(One_line_regex(new Regex("<p class=\"title-english title-inherit\">" + @"(.*?)<"), WebContent));
                case "jap":
                    return WebUtility.HtmlDecode(One_line_regex(new Regex(@">([\S\s]*?)<"), One_line_regex(new Regex(@"Japanese:<\/span>(?s)(.*?)<"), WebContent)));
                //Default is jap_r
                default:
                    return WebUtility.HtmlDecode(One_line_regex(new Regex("<h1 class=\"title-name h1_bold_none\"><strong>" + @"(.*?)<"), WebContent));
            }
        }

        /// <summary>
        /// WebContent API call get genre
        /// </summary>
        /// <param name="WebContent"></param>
        /// <returns></returns>
        public List<string> Get_Genre(string WebContent)
        {
            List<string> result = new List<string>();
            try
            {
                var regex = new Regex("<a href=\"/anime/genre/.+?\" title=\".+?\">(.+?)</a>");
                var genres = regex.Matches(WebContent);
                foreach (Match match in genres)
                {
                    var genre = match.Groups[1].Value.ToString();
                    if (!string.IsNullOrEmpty(genre))
                    {
                        result.Add(genre);
                    }
                }
                return result;
            }
            catch (Exception)
            {
                result.Add("");
                return result;
            }
        }

        /// <summary>
        /// WebContent API call get rating
        /// </summary>
        /// <param name="WebContent"></param>
        public string Get_RatingAsync(string WebContent)
        {
            return One_line_regex(new Regex("<span itemprop=\"ratingValue\" class=\"score-label score-.*?\">" + @"(.*?)<"), WebContent);
        }

        /// <summary>
        /// WebContent API call to get the imgurl
        /// </summary>
        /// <param name="WebContent"></param>
        /// <returns></returns>
        public string Get_ImageUrlAsync(string WebContent)
        {
            return One_line_regex(new Regex("src=\"(?s)(.*?)\""), One_line_regex(new Regex("<div style=\"text-align: center;\">(?s)(.+?)alt="), WebContent));
        }

        /// <summary>
        /// WebContent API call to get the RemoteImageList
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<List<RemoteImageInfo>> Get_RemoteImageListAsync(string id, CancellationToken cancellationToken)
        {
            List<RemoteImageInfo> result = new List<RemoteImageInfo>();
            //API
            if (!string.IsNullOrEmpty(clientID))
            {
                string json = await WebRequestAPI(string.Format(AnimeLink, id), cancellationToken, clientID).ConfigureAwait(false);
                AnimeObject anime = _jsonSerializer.DeserializeFromString<AnimeObject>(json);
                foreach (Picture picture in anime.pictures)
                {
                    result.Add(new RemoteImageInfo
                    {
                        ProviderName = MyAnimeListSeriesProvider.StaticName,
                        Type = ImageType.Primary,
                        Url = picture.large
                    });
                }
            }
            else
            {
                //Fallback to Web
                var WebContent = await WebRequestAPI(FallbackAnimeLink + id, cancellationToken).ConfigureAwait(false);
                result.Add(new RemoteImageInfo
                {
                    ProviderName = MyAnimeListSeriesProvider.StaticName,
                    Type = ImageType.Primary,
                    Url = One_line_regex(new Regex("src=\"(?s)(.*?)\""), One_line_regex(new Regex("<div style=\"text-align: center;\">(?s)(.+?)alt="), WebContent))
                });

            }
            return result;
        }


        /// <summary>
        /// WebContent API call to get the description
        /// </summary>
        /// <param name="WebContent"></param>
        /// <returns></returns>
        public string Get_OverviewAsync(string WebContent)
        {
            return WebUtility.HtmlDecode(One_line_regex(new Regex("<p itemprop=\"description\">(?s)(.+?)</p>"), WebContent));
        }

        /// <summary>
        /// MyAnimeListAPI call to search the series and return a list
        /// </summary>
        /// <param name="title"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<List<string>> Search_GetSeries_list(string title, CancellationToken cancellationToken)
        {
            List<string> result = new List<string>();
            //API
            if (!string.IsNullOrEmpty(clientID))
            {
                string json = await WebRequestAPI(string.Format(SearchLink, Uri.EscapeUriString(title)), cancellationToken, clientID).ConfigureAwait(false);
                SearchObject search = _jsonSerializer.DeserializeFromString<SearchObject>(json);
                foreach (SearchData data in search.data)
                {
                    //get id

                    try
                    {
                        if (Equals_check.Compare_strings(data.node.title, title))
                        {
                            result.Add(data.node.id.ToString());
                        }
                        if (Equals_check.Compare_strings(data.node.alternative_titles.en, title))
                        {
                            result.Add(data.node.id.ToString());
                        }
                        if (Equals_check.Compare_strings(data.node.alternative_titles.ja, title))
                        {
                            result.Add(data.node.id.ToString());
                        }
                    }
                    catch (Exception) { }
                }
            }
            else
            {
                //Fallback to Web
                string WebContent = await WebRequestAPI(string.Format(FallbackSearchLink, Uri.EscapeUriString(title)), cancellationToken).ConfigureAwait(false);
                string regex_id = "-";
                int x = 0;
                while (!string.IsNullOrEmpty(regex_id) && x < 50)
                {
                    regex_id = "";
                    regex_id = One_line_regex(new Regex(@"(#revInfo(.*?)" + '"' + "(>(.*?)<))"), WebContent, 2, x);
                    if (!string.IsNullOrEmpty(regex_id))
                    {
                        if (int.TryParse(regex_id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedId))
                        {
                            if (Equals_check.Compare_strings(One_line_regex(new Regex(@"(#revInfo(.*?)" + '"' + "(>(.*?)<))"), WebContent, 4, x), title))
                            {
                                result.Add(regex_id);
                                return result;
                            }
                        }

                    }
                    x++;
                }

            }
            return result;
        }

        /// <summary>
        /// MyAnimeListAPI call to find a series
        /// </summary>
        /// <param name="title"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<string> FindSeries(string title, CancellationToken cancellationToken)
        {
            var aid = (await Search_GetSeries_list(title, cancellationToken).ConfigureAwait(false)).FirstOrDefault();
            if (!string.IsNullOrEmpty(aid))
            {
                return aid;
            }

            var cleanedTitle = Equals_check.Clear_name(title);
            if (!string.Equals(cleanedTitle, title, StringComparison.OrdinalIgnoreCase))
            {
                aid = (await Search_GetSeries_list(cleanedTitle, cancellationToken).ConfigureAwait(false)).FirstOrDefault();
                if (!string.IsNullOrEmpty(aid))
                {
                    return aid;
                }
            }

            return null;
        }

        /// <summary>
        /// simple regex
        /// </summary>
        /// <param name="regex"></param>
        /// <param name="match"></param>
        /// <param name="group"></param>
        /// <param name="match_int"></param>
        /// <returns></returns>
        public string One_line_regex(Regex regex, string match, int group = 1, int match_int = 0)
        {
            int x = 0;
            MatchCollection matches = regex.Matches(match);
            foreach (Match _match in matches)
            {
                if (x == match_int)
                {
                    return _match.Groups[group].Value.ToString();
                }
                x++;
            }
            return "";
        }

        /// <summary>
        /// A WebRequestAPI too handle the Webcontent and the API of MyAnimeList
        /// </summary>
        /// <param name="link"></param>
        /// <param name="name"></param>
        /// <param name="pw"></param>
        /// <returns></returns>
        public async Task<string> WebRequestAPI(string link, CancellationToken cancellationToken, string clientId = null)
        {
            try
            {
                var options = new HttpRequestOptions
                {
                    CancellationToken = cancellationToken,
                    Url = link
                };

                if (!string.IsNullOrEmpty(clientId))
                {
                    options.RequestHeaders["X-MAL-CLIENT-ID"] = clientId;
                }

                using (var stream = await _httpClient.Get(options).ConfigureAwait(false))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        return await reader.ReadToEndAsync().ConfigureAwait(false);
                    }
                }
            }
            catch (HttpException ex)
            {
                _logger.ErrorException("Error from {0}", ex, link);

                if (ex.IsTimedOut)
                {
                    throw;
                }

                return string.Empty;
            }
        }
    }
}