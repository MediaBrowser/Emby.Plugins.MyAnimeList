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
using Emby.Anime;
using System.Linq;
using System.Globalization;

namespace Emby.Plugins.MyAnimeList
{
    /// <summary>
    /// This API use the WebContent of MyAnimelist and the API of MyAnimelist
    /// </summary>
    public class Api
    {
        private static ILogger _logger;
        //Use API too search
        public string SearchLink = "https://myanimelist.net/api/anime/search.xml?q={0}";
        //Web Fallback search
        public string FallbackSearchLink = "https://myanimelist.net/search/all?q={0}";
        //No API funktion exist too get anime
        public string anime_link = "https://myanimelist.net/anime/";

        private IHttpClient _httpClient;

        /// <summary>
        /// WebContent API call to get a anime with id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Api(ILogger logger, IHttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }
        public async Task<RemoteSearchResult> GetAnime(string id, string preferredMetadataLanguage, CancellationToken cancellationToken)
        {
            var WebContent = await WebRequestAPI(anime_link + id, cancellationToken).ConfigureAwait(false);

            var result = new RemoteSearchResult
            {
                Name = SelectName(WebContent, preferredMetadataLanguage)
            };

            result.SearchProviderName = MyAnimeListSeriesProvider.StaticName;
            result.ImageUrl = Get_ImageUrlAsync(WebContent);
            result.SetProviderId(MyAnimeListSeriesProvider.StaticName, id);
            result.Overview = Get_OverviewAsync(WebContent);

            return result;
        }

        /// <summary>
        ///  WebContent API call to select a prefence title
        /// </summary>
        /// <param name="WebContent"></param>
        /// <param name="preference"></param>
        /// <param name="language"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public string SelectName(string WebContent, string preferredMetadataLanguage)
        {   
            var title;
            if (string.IsNullOrEmpty(preferredMetadataLanguage) || preferredMetadataLanguage.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            {
                title = Get_title("en", WebContent);
                if (string.IsNullOrWhiteSpace(title))
                {
                    title = Get_title("en_alt", WebContent);
                }
            }
            if (string.Equals(preferredMetadataLanguage, "ja", StringComparison.OrdinalIgnoreCase))
            {
                title = Get_title("jap", WebContent);
            }
            //Default to romanji title
            if (string.IsNullOrWhiteSpace(title))
            {
                title = Get_title("jap_r", WebContent);
            }

            return title;
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

                case "en_alt":
                    return WebUtility.HtmlDecode(One_line_regex(new Regex(@">([\S\s]*?)<"), One_line_regex(new Regex(@"English:<\/span>(?s)(.*?)<"), WebContent)));

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
        public async Task<List<string>> Search_GetSeries_list(string title, MyAnimeListOptions config, CancellationToken cancellationToken)
        {
            List<string> result = new List<string>();
            string result_text = null;
            //API
            if (!string.IsNullOrEmpty(config.Username) && !string.IsNullOrEmpty(config.Password))
            {
                string WebContent = await WebRequestAPI(string.Format(SearchLink, Uri.EscapeUriString(title)), cancellationToken, config.Username, config.Password).ConfigureAwait(false);
                int x = 0;
                while (result_text != "" && x < 50)
                {
                    result_text = One_line_regex(new Regex(@"<entry>(.*?)<\/entry>"), WebContent, 1, x);
                    if (result_text != "")
                    {
                        //get id
                        string id = One_line_regex(new Regex(@"<id>(.*?)<\/id>"), result_text);
                        string a_name = One_line_regex(new Regex(@"<title>(.*?)<\/title>"), result_text);
                        string b_name = One_line_regex(new Regex(@"<english>(.*?)<\/english>"), result_text);
                        string c_name = One_line_regex(new Regex(@"<synonyms>(.*?)<\/synonyms>"), result_text);

                        if (Equals_check.Compare_strings(a_name, title))
                        {
                            result.Add(id);
                            return result;
                        }
                        if (Equals_check.Compare_strings(b_name, title))
                        {
                            result.Add(id);
                            return result;
                        }
                        foreach (string d_name in c_name.Split(';'))
                        {
                            if (Equals_check.Compare_strings(d_name, title))
                            {
                                result.Add(id);
                                return result;
                            }
                        }
                        if (Int32.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
                        {
                            result.Add(id);
                        }
                    }
                    x++;
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
        public async Task<string> FindSeries(string title, MyAnimeListOptions config, CancellationToken cancellationToken)
        {
            var aid = (await Search_GetSeries_list(title, config, cancellationToken).ConfigureAwait(false)).FirstOrDefault();
            if (!string.IsNullOrEmpty(aid))
            {
                return aid;
            }

            var cleanedTitle = Equals_check.Clear_name(title);
            if (!string.Equals(cleanedTitle, title, StringComparison.OrdinalIgnoreCase))
            {
                aid = (await Search_GetSeries_list(cleanedTitle, config, cancellationToken).ConfigureAwait(false)).FirstOrDefault();
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
        public async Task<string> WebRequestAPI(string link, CancellationToken cancellationToken, string name = null, string pw = null)
        {
            try
            {
                var options = new HttpRequestOptions
                {
                    CancellationToken = cancellationToken,
                    Url = link
                };

                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(pw))
                {
                    var encoded = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(name + ":" + pw));

                    options.RequestHeaders["Authorization"] = "Basic " + encoded;
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
