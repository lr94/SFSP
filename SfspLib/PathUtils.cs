using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace Sfsp
{
    internal abstract class PathUtils
    {
        /// <summary>
        /// Restituisce il percorso relativo di un file/directory a partire da un altro percorso
        /// </summary>
        /// <param name="basePath">Base di partenza</param>
        /// <param name="targetPath">Percorso di arrivo</param>
        /// <returns></returns>
        public static string GetRelativePath(string basePath, string targetPath)
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
