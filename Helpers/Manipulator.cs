using System;
using System.Collections.Generic;
using System.IO;

namespace Mp3TagReader.Helpers
{
    public class Manipulator
    {
        private static readonly string[] audioExtensions = { ".mp3", ".wma", ".flac", ".m4a" };
        private static readonly string[] videoExtensions = { ".mp4", ".mpg", ".flv", ".wmv", ".avi" };
        private static readonly string[] imageExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };

        public static string ArrayToString(string[] strArray, string strDelimeter)
        {
            if (strArray == null) return string.Empty;
            return string.Join(strDelimeter, strArray);
        }

        // reverse of ArrayToString: "A, B" -> { "A", "B" }, empty text -> empty array
        public static string[] StringToArray(string text, char delimeter)
        {
            List<string> values = new List<string>();
            if (!string.IsNullOrEmpty(text))
            {
                foreach (string value in text.Split(delimeter))
                {
                    string trimmed = value.Trim();
                    if (trimmed.Length > 0)
                    {
                        values.Add(trimmed);
                    }
                }
            }
            return values.ToArray();
        }

        public static bool IsAudioFile(string fi)
        {
            return HasExtension(fi, audioExtensions);
        }

        public static bool IsVideoFile(string fi)
        {
            return HasExtension(fi, videoExtensions);
        }

        public static bool IsImageFile(string fi)
        {
            return HasExtension(fi, imageExtensions);
        }

        public static bool IsFolder(string selectedPath)
        {
            return !string.IsNullOrEmpty(selectedPath) && Directory.Exists(selectedPath);
        }

        public static string GetImageMimeType(string imagePath)
        {
            string ext = GetExtension(imagePath);
            switch (ext)
            {
                case ".png": return "image/png";
                case ".bmp": return "image/bmp";
                case ".gif": return System.Net.Mime.MediaTypeNames.Image.Gif;
                default: return System.Net.Mime.MediaTypeNames.Image.Jpeg;
            }
        }

        private static bool HasExtension(string path, string[] extensions)
        {
            string ext = GetExtension(path);
            return ext.Length > 0 && Array.IndexOf(extensions, ext) >= 0;
        }

        private static string GetExtension(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            try
            {
                return Path.GetExtension(path).ToLowerInvariant();
            }
            catch (ArgumentException)
            {
                return string.Empty;
            }
        }
    }
}
