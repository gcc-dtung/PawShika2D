using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PawBlock.Shared
{
    /// <summary>
    /// Chuẩn bị UI dùng chung trong Editor để lúc chạy không phải tìm / gắn component (guide 8.3):
    /// UIButtonPress gắn ButtonPress cho Button con chưa có; ButtonPress, UIButtonSounds, UIGrayscaleWhenDisabled thu lại
    /// danh sách con. Chạy khi lưu scene và trước khi vào Play mode (builder dựng lại UI xong vẫn đúng).
    /// Tay: Tools/PawBlock/UI/Setup Buttons.
    /// </summary>
    [InitializeOnLoad]
    public static class UIAutoSetup
    {
        static UIAutoSetup()
        {
            EditorSceneManager.sceneSaving += (scene, path) => SetupIn(scene);
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.ExitingEditMode)
                {
                    return;
                }
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    SetupIn(SceneManager.GetSceneAt(i));
                }
            };
        }

        [MenuItem("Tools/PawBlock/UI/Setup Buttons")]
        public static void SetupAll()
        {
            int changed = 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                changed += SetupIn(SceneManager.GetSceneAt(i));
            }
            Debug.Log($"[UIAutoSetup] Đã chuẩn bị UI ({changed} thay đổi)");
        }

        /// <summary>Trả về số component vừa gắn / đổi.</summary>
        public static int SetupIn(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return 0;
            }
            int changed = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            // Gắn ButtonPress trước, rồi mới thu danh sách
            foreach (GameObject root in roots)
            {
                foreach (UIButtonPress p in root.GetComponentsInChildren<UIButtonPress>(true))
                {
                    changed += p.EditorAttach();
                }
            }
            foreach (GameObject root in roots)
            {
                foreach (ButtonPress b in root.GetComponentsInChildren<ButtonPress>(true))
                {
                    changed += Collect(b, b.EditorCollect());
                }
                foreach (UIButtonSounds s in root.GetComponentsInChildren<UIButtonSounds>(true))
                {
                    changed += Collect(s, s.EditorCollect());
                }
                foreach (UIGrayscaleWhenDisabled g in root.GetComponentsInChildren<UIGrayscaleWhenDisabled>(true))
                {
                    changed += Collect(g, g.EditorCollect());
                }
            }
            if (changed > 0 && !Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
            return changed;
        }

        private static int Collect(Object target, bool changed)
        {
            if (!changed)
            {
                return 0;
            }
            EditorUtility.SetDirty(target);
            return 1;
        }
    }
}
