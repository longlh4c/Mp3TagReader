using System.Text;

namespace Mp3TagReader.Helpers
{
    public class Manipulator
    {
        public static string ArrayToString(string[] strArray, string strDelimeter)
        {
            if (strArray == null) return string.Empty;
            StringBuilder builder = new StringBuilder();
            foreach (string value in strArray)
            {
                builder.Append(value);
                if (strArray.Length > 1)
                {
                    builder.Append(strDelimeter);
                }
            }
            return builder.ToString();
        }

        public static bool IsAudioFile(string fi)
        {
            if (fi.EndsWith("mp3") || fi.EndsWith("wma") || fi.EndsWith("flac") || fi.EndsWith("m4a"))
            {
                return true;
            }
            else
                return false;
        }

        public static bool IsVideoFile(string fi)
        {
            if (fi.EndsWith("mp4") || fi.EndsWith("mpg") ||
                    fi.EndsWith("flv") || fi.EndsWith("wmv") ||
                    fi.EndsWith("avi") || fi.EndsWith("flac"))
            {
                return true;
            }
            else
                return false;
        }

        public static bool IsFolder(string selectedPath)
        {
            if (!selectedPath.EndsWith("mp3") && !selectedPath.EndsWith("wma") && !selectedPath.EndsWith("m4a")
                && !selectedPath.EndsWith("mp4") && !selectedPath.EndsWith("avi")
                && !selectedPath.EndsWith("flv") && !selectedPath.EndsWith("wmv")
                && !selectedPath.EndsWith("mpg") && !selectedPath.EndsWith("flac"))
            {
                return true;
            }
            else
                return false;
        }
    }
}
