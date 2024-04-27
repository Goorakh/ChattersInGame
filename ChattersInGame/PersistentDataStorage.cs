using System.IO;
using UnityEngine;

namespace ChattersInGame
{
    internal static class PersistentDataStorage
    {
        public static readonly string DirectoryPath = Path.Combine(Application.persistentDataPath, Main.PluginName);

        static PersistentDataStorage()
        {
            if (!Directory.Exists(DirectoryPath))
            {
                Directory.CreateDirectory(DirectoryPath);
            }
        }

        public static string GetAbsoluteSavePath(string relativePath)
        {
            return Path.Combine(DirectoryPath, relativePath);
        }
    }
}
