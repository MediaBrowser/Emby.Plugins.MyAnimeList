using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Emby.Plugins.MyAnimeList
{
    internal static class Equals_check
    {
        /// <summary>
        /// Clear name
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static string Clear_name(string a)
        {
            try
            {
                a = a.Trim().Replace(One_line_regex(new Regex(@"(?s) \(.*?\)"), a.Trim(), 0), "");
            }
            catch (Exception)
            { }
            a = a.Replace(".", " ");
            a = a.Replace("-", " ");
            a = a.Replace("`", "");
            a = a.Replace("'", "");
            a = a.Replace("&", "and");
            a = a.Replace("(", "");
            a = a.Replace(")", "");
            try
            {
                a = a.Replace(One_line_regex(new Regex(@"(?s)(S[0-9]+)"), a.Trim()), One_line_regex(new Regex(@"(?s)S([0-9]+)"), a.Trim()));
            }
            catch (Exception)
            {
            }
            return a;
        }

        /// <summary>
        /// Clear name heavy.
        /// Example: Text & Text to Text and Text
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        private static string Clear_name_step2(string a)
        {
            if (a.Contains("Gekijyouban"))
                a = (a.Replace("Gekijyouban", "") + " Movie").Trim();
            if (a.Contains("gekijyouban"))
                a = (a.Replace("gekijyouban", "") + " Movie").Trim();
            try
            {
                a = a.Trim().Replace(One_line_regex(new Regex(@"(?s) \(.*?\)"), a.Trim(), 0), "");
            }
            catch (Exception)
            { }
            a = a.Replace(".", " ");
            a = a.Replace("-", " ");
            a = a.Replace("`", "");
            a = a.Replace("'", "");
            a = a.Replace("&", "and");
            a = a.Replace(":", "");
            a = a.Replace("␣", "");
            a = a.Replace("2wei", "zwei");
            a = a.Replace("3rei", "drei");
            a = a.Replace("4ier", "vier");
            return a;
        }

        /// <summary>
        /// If a and b match it return true
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool Compare_strings(string a, string b)
        {
            if (!string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b))
            {
                if (Simple_compare(a, b))
                    return true;

                return false;
            }
            return false;
        }

        /// <summary>
        /// simple regex
        /// </summary>
        /// <param name="regex"></param>
        /// <param name="match"></param>
        /// <param name="group"></param>
        /// <param name="match_int"></param>
        /// <returns></returns>
        private static string One_line_regex(Regex regex, string match, int group = 1, int match_int = 0)
        {
            int x = 0;
            foreach (Match _match in regex.Matches(match))
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
        /// Compare 2 Strings, and it just works
        /// SeriesA S2 == SeriesA Second Season | True;
        /// </summary>
        private static bool Simple_compare(string a, string b, bool fastmode = false)
        {
            if (fastmode)
            {
                if (a[0] == b[0])
                {
                }
                else
                {
                    return false;
                }
            }

            if (Core_compare(a, b))
                return true;
            if (Core_compare(b, a))
                return true;

            return false;
        }

        /// <summary>
        /// Compare 2 Strings, and it just works
        /// </summary>
        private static bool Core_compare(string a, string b)
        {
            if (a == b)
                return true;

            a = a.ToLower().Replace(" ", "").Trim().Replace(".", "");
            b = b.ToLower().Replace(" ", "").Trim().Replace(".", "");

            if (Clear_name(a) == Clear_name(b))
                return true;
            if (Clear_name_step2(a) == Clear_name_step2(b))
                return true;
            if (a.Replace("-", " ") == b.Replace("-", " "))
                return true;
            if (a.Replace(" 2", ":secondseason") == b.Replace(" 2", ":secondseason"))
                return true;
            if (a.Replace("2", "secondseason") == b.Replace("2", "secondseason"))
                return true;
            if (Convert_symbols_too_numbers(a, "I") == Convert_symbols_too_numbers(b, "I"))
                return true;
            if (Convert_symbols_too_numbers(a, "!") == Convert_symbols_too_numbers(b, "!"))
                return true;
            if (a.Replace("ndseason", "") == b.Replace("ndseason", ""))
                return true;
            if (a.Replace("ndseason", "") == b)
                return true;
            if (One_line_regex(new Regex(@"((.*)s([0 - 9]))"), a, 2) + One_line_regex(new Regex(@"((.*)s([0 - 9]))"), a, 3) == One_line_regex(new Regex(@"((.*)s([0 - 9]))"), b, 2) + One_line_regex(new Regex(@"((.*)s([0 - 9]))"), b, 3))
                if (!string.IsNullOrEmpty(One_line_regex(new Regex(@"((.*)s([0 - 9]))"), a, 2) + One_line_regex(new Regex(@"((.*)s([0 - 9]))"), a, 3)))
                    return true;
            if (One_line_regex(new Regex(@"((.*)s([0 - 9]))"), a, 2) + One_line_regex(new Regex(@"((.*)s([0 - 9]))"), a, 3) == b)
                if (!string.IsNullOrEmpty(One_line_regex(new Regex(@"((.*)s([0 - 9]))"), a, 2) + One_line_regex(new Regex(@"((.*)s([0 - 9]))"), a, 3)))
                    return true;
            if (a.Replace("rdseason", "") == b.Replace("rdseason", ""))
                return true;
            if (a.Replace("rdseason", "") == b)
                return true;
            try
            {
                if (a.Replace("2", "secondseason").Replace(One_line_regex(new Regex(@"(?s)\(.*?\)"), a, 0), "") == b.Replace("2", "secondseason").Replace(One_line_regex(new Regex(@"(?s)\(.*?\)"), b, 0), ""))
                    return true;
            }
            catch (Exception)
            {
            }
            try
            {
                if (a.Replace("2", "secondseason").Replace(One_line_regex(new Regex(@"(?s)\(.*?\)"), a, 0), "") == b)
                    return true;
            }
            catch (Exception)
            {
            }
            try
            {
                if (a.Replace(" 2", ":secondseason").Replace(One_line_regex(new Regex(@"(?s)\(.*?\)"), a, 0), "") == b.Replace(" 2", ":secondseason").Replace(One_line_regex(new Regex(@"(?s)\(.*?\)"), b, 0), ""))
                    return true;
            }
            catch (Exception)
            {
            }
            try
            {
                if (a.Replace(" 2", ":secondseason").Replace(One_line_regex(new Regex(@"(?s)\(.*?\)"), a, 0), "") == b)
                    return true;
            }
            catch (Exception)
            {
            }
            try
            {
                if (a.Replace(One_line_regex(new Regex(@"(?s)\(.*?\)"), a, 0), "") == b.Replace(One_line_regex(new Regex(@"(?s)\(.*?\)"), b, 0), ""))
                    return true;
            }
            catch (Exception)
            {
            }
            try
            {
                if (a.Replace(One_line_regex(new Regex(@"(?s)\(.*?\)"), a, 0), "") == b)
                    return true;
            }
            catch (Exception)
            {
            }
            try
            {
                if (b.Replace(One_line_regex(new Regex(@"(?s)\(.*?\)"), b, 0), "").Replace("  2", ": second Season") == a)
                    return true;
            }
            catch (Exception)
            {
            }
            try
            {
                if (a.Replace(" 2ndseason", ":secondseason") + " vs " + b == a)
                    return true;
            }
            catch (Exception)
            {
            }
            try
            {
                if (a.Replace(One_line_regex(new Regex(@"(?s)\(.*?\)"), a, 0), "").Replace("  2", ":secondseason") == b)
                    return true;
            }
            catch (Exception)
            {
            }
            return false;
        }

        /// <summary>
        /// Example: Convert II to 2
        /// </summary>
        /// <param name="input"></param>
        /// <param name="symbol"></param>
        /// <returns></returns>
        private static string Convert_symbols_too_numbers(string input, string symbol)
        {
            try
            {
                string regex_c = "_";
                int x = 0;
                int highest_number = 0;
                while (!string.IsNullOrEmpty(regex_c))
                {
                    regex_c = (One_line_regex(new Regex(@"(" + symbol + @"+)"), input.ToLower().Trim(), 1, x)).Trim();
                    if (highest_number < regex_c.Count())
                        highest_number = regex_c.Count();
                    x++;
                }
                x = 0;
                string output = "";
                while (x != highest_number)
                {
                    output = output + symbol;
                    x++;
                }
                output = input.Replace(output, highest_number.ToString());
                if (string.IsNullOrEmpty(output))
                {
                    output = input;
                }
                return output;
            }
            catch (Exception)
            {
                return input;
            }
        }
    }
}