using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Mp3TagReader.Models;

namespace Mp3TagReader.Services
{
    public class SubtitleService
    {
        public List<SubdlSearchResult> SearchSubtitles(string keywordQuery, string videoPath, string selectedLang)
        {
            List<SubdlSearchResult> autoResults = new List<SubdlSearchResult>();
            List<SubdlSearchResult> keywordResults = new List<SubdlSearchResult>();

            // 1. Search by exact filename if video is loaded
            if (!string.IsNullOrEmpty(videoPath) && File.Exists(videoPath))
            {
                string fileQuery = Path.GetFileNameWithoutExtension(videoPath);
                if (!string.IsNullOrEmpty(fileQuery))
                {
                    autoResults = ScrapeSubtitleCat(fileQuery, selectedLang);
                }
            }

            // 2. Search by textbox keyword query
            if (!string.IsNullOrEmpty(keywordQuery))
            {
                keywordResults = ScrapeSubtitleCat(keywordQuery, selectedLang);
            }

            // 3. Combine results with priority (autoResults first) and deduplicate
            List<SubdlSearchResult> combined = new List<SubdlSearchResult>();
            HashSet<string> seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in autoResults)
            {
                if (seenUrls.Add(item.DownloadPageUrl))
                {
                    item.Name = "[Auto-Match] " + item.Name;
                    combined.Add(item);
                }
            }

            foreach (var item in keywordResults)
            {
                if (seenUrls.Add(item.DownloadPageUrl))
                {
                    combined.Add(item);
                }
            }

            return combined;
        }

        public bool DownloadSubtitle(SubdlSearchResult result, string savePath)
        {
            // Scenario 1: Hash matched contains direct text content
            if (!string.IsNullOrEmpty(result.DirectContent))
            {
                File.WriteAllText(savePath, result.DirectContent, Encoding.UTF8);
                return false;
            }

            // Scenario 2: Keyword search requires fetching subtitlecat page
            // 1. Visit subtitle details page
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(result.DownloadPageUrl);
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
            request.Timeout = 10000;

            string html = string.Empty;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                html = reader.ReadToEnd();
            }

            // 2. Map language to subtitlecat language code
            string langCode = "en";
            if (result.Language.Equals("Vietnamese", StringComparison.OrdinalIgnoreCase))
            {
                langCode = "vi";
            }

            // 3. Find download link or translate fallback
            string downloadUrl = string.Empty;
            bool isOriginalFallback = false;

            // Regex to match: id="download_{lang}" href="downloadUrl"
            var downloadMatch = Regex.Match(html, @"id=""download_" + langCode + @"""[^>]*href=""([^""]+)""", RegexOptions.Singleline);
            if (downloadMatch.Success)
            {
                downloadUrl = "https://www.subtitlecat.com" + downloadMatch.Groups[1].Value;
            }
            else
            {
                // Fallback: Check if translation button is available, download the original instead
                var translateMatch = Regex.Match(html, @"id=""" + langCode + @"""[^>]*onclick=""translate_from_server_folder\('" + langCode + @"',\s*'([^']*)',\s*'([^']*)'\)""", RegexOptions.Singleline);
                if (translateMatch.Success)
                {
                    string origFile = translateMatch.Groups[1].Value;
                    string folder = translateMatch.Groups[2].Value;
                    downloadUrl = "https://www.subtitlecat.com" + (folder.EndsWith("/") ? folder : folder + "/") + origFile;
                    isOriginalFallback = true;
                }
            }

            if (string.IsNullOrEmpty(downloadUrl))
            {
                throw new Exception("Unable to find download or translation link on details page.");
            }

            // 4. Download SRT file content directly
            byte[] fileBytes;
            using (WebClient wc = new WebClient())
            {
                wc.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                fileBytes = wc.DownloadData(downloadUrl);
            }

            File.WriteAllBytes(savePath, fileBytes);
            return isOriginalFallback;
        }

        private List<SubdlSearchResult> ScrapeSubtitleCat(string query, string selectedLang)
        {
            List<SubdlSearchResult> list = new List<SubdlSearchResult>();
            try
            {
                string searchUrl = "https://www.subtitlecat.com/index.php?search=" + Uri.EscapeDataString(query);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(searchUrl);
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
                request.Timeout = 10000;

                string html = string.Empty;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    html = reader.ReadToEnd();
                }

                var subMatches = Regex.Matches(html, @"href=""(subs/[^""]+\.html)""[^>]*>(.*?)</a>", RegexOptions.Singleline);
                foreach (Match m in subMatches)
                {
                    string subUrl = "https://www.subtitlecat.com/" + m.Groups[1].Value;
                    string nameText = Regex.Replace(m.Groups[2].Value, "<.*?>", "").Trim();
                    nameText = System.Net.WebUtility.HtmlDecode(nameText);
                    if (string.IsNullOrEmpty(nameText)) nameText = selectedLang + " Subtitle Link";

                    list.Add(new SubdlSearchResult
                    {
                        Name = nameText,
                        DownloadPageUrl = subUrl,
                        Language = selectedLang
                    });
                }
            }
            catch
            {
                // Ignore search exceptions so that the other search can still complete successfully
            }
            return list;
        }
    }
}
