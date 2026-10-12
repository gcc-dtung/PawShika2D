using System.IO;
using UnityEditor;
using UnityEngine;

namespace PawBlock.Core.Foundation
{
    /// <summary>Tools → PawBlock → Save Data: xem, mở thư mục, xóa file save (save.json trong persistentDataPath).</summary>
    public static class SaveDataMenu
    {
        [MenuItem("Tools/PawBlock/Save Data/Log Save", priority = 21)]
        public static void LogSave()
        {
            string path = SaveSystem.FilePath;
            if (!File.Exists(path))
            {
                Debug.Log($"[SaveData] Chưa có file save: {path}");
                return;
            }
            Debug.Log($"[SaveData] {path}\n{File.ReadAllText(path)}");
        }

        [MenuItem("Tools/PawBlock/Save Data/Open Save Folder", priority = 22)]
        public static void OpenFolder()
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            EditorUtility.RevealInFinder(SaveSystem.FilePath);
        }

        [MenuItem("Tools/PawBlock/Save Data/Delete Save", priority = 23)]
        public static void DeleteSave()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[SaveData] Thoát Play mode rồi mới xóa save (đang chơi sẽ ghi đè lại).");
                return;
            }
            if (!EditorUtility.DisplayDialog("Xóa save", "Xóa toàn bộ dữ liệu người chơi (level, coin, booster, năng lượng, cài đặt)?\n" + SaveSystem.FilePath, "Xóa", "Hủy"))
                return;
            SaveSystem.Delete();
            Debug.Log("[SaveData] Đã xóa save. Lần chơi sau bắt đầu từ level 1, 0 coin.");
        }
    }
}
