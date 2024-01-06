using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Emby.Plugins.MyAnimeList
{
    public class AlternativeTitles
    {
        public List<string> synonyms { get; set; }
        public string en { get; set; }
        public string ja { get; set; }
    }

    public class Picture
    {
        public string medium { get; set; }
        public string large { get; set; }
    }

    public class SerchNode
    {
        public int id { get; set; }
        public string title { get; set; }
        public Picture main_picture { get; set; }
        public AlternativeTitles alternative_titles { get; set; }
    }

    public class SearchData
    {
        public SerchNode node { get; set; }
    }

    public class SearchObject
    {
        public List<SearchData> data { get; set; }
    }
    public class Genre
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    public class Studio
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    public class AnimeObject
    {
        public int id { get; set; }
        public string title { get; set; }
        public Picture main_picture { get; set; }
        public AlternativeTitles alternative_titles { get; set; }
        public string start_date { get; set; }
        public string end_date { get; set; }
        public string synopsis { get; set; }
        public float mean { get; set; }
        public string status { get; set; }
        public List<Genre> genres { get; set; }
        public string source { get; set; }
        public List<Picture> pictures { get; set; }
        public string background { get; set; }
        public List<Studio> studios { get; set; }
    }
}
