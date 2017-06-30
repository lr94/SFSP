using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace SfspTests
{
    abstract internal class PathUtils
    {
        private const int FILE_ATTRIBUTE_DIRECTORY = 0x10;
        private const int FILE_ATTRIBUTE_NORMAL = 0x80;

        [DllImport("shlwapi.dll", SetLastError = true)]
        private static extern int PathRelativePathTo(StringBuilder pszPath, string pszFrom, int dwAttrFrom, string pszTo, int dwAttrTo);

        static internal string GetRelativePath(string basePath, string targetPath)
        {
            int attribute = 0;
            if (System.IO.File.Exists(targetPath))
                attribute = FILE_ATTRIBUTE_NORMAL;
            else if (System.IO.Directory.Exists(targetPath))
                attribute = FILE_ATTRIBUTE_DIRECTORY;

            StringBuilder sb = new StringBuilder(260);
            if (PathRelativePathTo(sb, basePath, FILE_ATTRIBUTE_DIRECTORY, targetPath, attribute) == 0)
                throw new Exception();

            return sb.ToString();
        }

        static internal string GetRelativePath2(string basePath, string targetPath)
        {
            if (!basePath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                basePath += Path.DirectorySeparatorChar.ToString();
            Uri baseUri = new Uri(basePath);
            Uri targetUri = new Uri(targetPath);

            Uri relativeUri = baseUri.MakeRelativeUri(targetUri);
            String result = Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', System.IO.Path.DirectorySeparatorChar);

            return result;
        }
    }
}
