using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace Sfsp
{
    internal static class PathUtils
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

        /// <summary>
        /// Sostituisce in un percorso il carattere di separazione delle directory della piattaforma con "\",
        /// usato nel protocollo SFSP. Su Windows non fa niente.
        /// </summary>
        /// <param name="originalPath">Percorso (rappresentazione del sistema operativo corrente)</param>
        /// <returns></returns>
        public static string ConvertOSPathToSFSP(string originalPath)
        {
            return originalPath.Replace(Path.DirectorySeparatorChar, '\\');
        }

        /// <summary>
        /// Sostituisce in un percorso il carattere "\" usato nel protocollo SFSP col carattere di separazione
        /// delle directory utilizzato dalla piattaforma corrente.
        /// </summary>
        /// <param name="sfspPath">Percorso (rappresentazione SFSP)</param>
        /// <returns></returns>
        public static string ConvertSFSPPathToOS(string sfspPath)
        {
            return sfspPath.Replace('\\', Path.DirectorySeparatorChar);
        }
    }
}
