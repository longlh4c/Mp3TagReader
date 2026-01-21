using System.Text;

namespace Mp3TagReader
{
    public class Manipulator
    {
        public static string ArrayToString(string[] strArray, string strDelimeter)
        {
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

        public static bool isAudioFile(string fi)
        {
            if (fi.EndsWith("mp3") || fi.EndsWith("wma") || fi.EndsWith("flac") || fi.EndsWith("m4a"))
            {
                return true;
            }
            else
                return false;
        }

        public static bool isVideoFile(string fi)
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

        public static bool isFolder(string selectedPath)
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