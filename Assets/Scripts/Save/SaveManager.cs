using System;
using System.IO;
using UnityEngine;

namespace PlanetScrappers.Save
{
    /// <summary>
    /// Local JSON save file with a backup of the previous save. Writes go to a temporary file
    /// first and are then swapped in, so a crash mid-write cannot corrupt the save.
    /// Location: %USERPROFILE%\AppData\LocalLow\Planet Scrappers\Planet Scrappers\
    /// </summary>
    public static class SaveManager
    {
        static string Folder => Application.persistentDataPath;
        public static string SavePath => Path.Combine(Folder, "save.json");
        public static string BackupPath => Path.Combine(Folder, "save.bak.json");

        public static bool HasSave => File.Exists(SavePath) || File.Exists(BackupPath);

        public static bool Write(SaveData data)
        {
            try
            {
                Directory.CreateDirectory(Folder);
                data.version = SaveData.CurrentVersion;
                data.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                string json = JsonUtility.ToJson(data, true);
                string temp = SavePath + ".tmp";
                File.WriteAllText(temp, json);
                if (File.Exists(SavePath)) File.Copy(SavePath, BackupPath, true);
                if (File.Exists(SavePath)) File.Delete(SavePath);
                File.Move(temp, SavePath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[Save] Could not write save: " + e.Message);
                return false;
            }
        }

        /// <summary>Reads the save, falling back to the backup if the main file is missing or damaged.</summary>
        public static SaveData Read()
        {
            return TryRead(SavePath) ?? TryRead(BackupPath);
        }

        static SaveData TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (data == null || data.version < 1 || data.version > SaveData.CurrentVersion || data.position == null || data.position.Length != 3)
                {
                    Debug.LogWarning("[Save] Ignoring invalid save at " + path);
                    return null;
                }
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] Could not read " + path + ": " + e.Message);
                return null;
            }
        }

        public static void DeleteAll()
        {
            try
            {
                if (File.Exists(SavePath)) File.Delete(SavePath);
                if (File.Exists(BackupPath)) File.Delete(BackupPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] Could not delete save: " + e.Message);
            }
        }
    }
}
