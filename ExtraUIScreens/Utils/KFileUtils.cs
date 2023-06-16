using System.IO;
using System.Linq;
using UnityEngine;

namespace Belzont.Utils
{
    public class KFileUtils
    {
        #region File & Prefab Utils
        public static readonly string BASE_FOLDER_PATH = Application.persistentDataPath + Path.DirectorySeparatorChar + "Klyte45Mods" + Path.DirectorySeparatorChar;

        public static FileInfo EnsureFolderCreation(string folderName)
        {
            if (File.Exists(folderName) && (File.GetAttributes(folderName) & FileAttributes.Directory) != FileAttributes.Directory)
            {
                File.Delete(folderName);
            }
            if (!Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }
            return new FileInfo(folderName);
        }
        public static bool IsFileCreated(string fileName) => File.Exists(fileName);
        public static string[] GetAllFilesEmbeddedAtFolder(string packageDirectory, string extension)
        {
            var executingAssembly = KResourceLoader.RefAssemblyMod;
            string folderName = $"Klyte.{packageDirectory}";
            return executingAssembly
                .GetManifestResourceNames()
                .Where(r => r.StartsWith(folderName) && r.EndsWith(extension))
                .Select(r => r.Substring(folderName.Length + 1))
                .ToArray();
        }
        #endregion
    }
}
