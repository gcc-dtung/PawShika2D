using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PawBlock.Game
{
    /// <summary>
    /// Tự điền tham chiếu của GameBootstrap (mọi field [SerializeField] kiểu Component / Component[]) bằng component
    /// cùng loại trong scene của nó, kể cả object đang tắt. Chạy khi lưu scene và trước khi vào Play mode, nên thêm / đổi
    /// prefab xong thì lần lưu / Play kế tiếp tham chiếu đã đúng. Chỉ chạy trong Editor (guide 8.3).
    /// Tay: Tools/PawBlock/Game/Collect Bootstrap References.
    /// </summary>
    [InitializeOnLoad]
    public static class GameBootstrapCollector
    {
        static GameBootstrapCollector()
        {
            EditorSceneManager.sceneSaving += (scene, path) => CollectIn(scene);
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.ExitingEditMode)
                {
                    return;
                }
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    CollectIn(SceneManager.GetSceneAt(i));
                }
            };
        }

        [MenuItem("Tools/PawBlock/Game/Collect Bootstrap References", priority = 1)]
        public static void CollectAll()
        {
            int changed = 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                changed += CollectIn(SceneManager.GetSceneAt(i));
            }
            Debug.Log($"[GameBootstrap] Đã điền tham chiếu ({changed} field đổi)");
        }

        /// <summary>Điền cho mọi GameBootstrap trong scene; trả về số field đã đổi.</summary>
        public static int CollectIn(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return 0;
            }
            int changed = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (GameBootstrap bootstrap in root.GetComponentsInChildren<GameBootstrap>(true))
                {
                    changed += Collect(bootstrap, scene);
                }
            }
            return changed;
        }

        private static int Collect(GameBootstrap bootstrap, Scene scene)
        {
            var so = new SerializedObject(bootstrap);
            int changed = 0;
            foreach (FieldInfo field in typeof(GameBootstrap).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (field.GetCustomAttribute<SerializeField>() == null && !field.IsPublic)
                {
                    continue;
                }
                SerializedProperty prop = so.FindProperty(field.Name);
                if (prop == null)
                {
                    continue;
                }
                Type type = field.FieldType;
                if (type.IsArray && typeof(Component).IsAssignableFrom(type.GetElementType()))
                {
                    List<Component> found = FindInScene(scene, type.GetElementType());
                    found.Sort((a, b) => string.CompareOrdinal(Path(a.transform), Path(b.transform))); // thứ tự ổn định, diff scene gọn
                    bool same = prop.arraySize == found.Count;
                    for (int i = 0; same && i < found.Count; i++)
                    {
                        same = prop.GetArrayElementAtIndex(i).objectReferenceValue == found[i];
                    }
                    if (same)
                    {
                        continue;
                    }
                    prop.arraySize = found.Count;
                    for (int i = 0; i < found.Count; i++)
                    {
                        prop.GetArrayElementAtIndex(i).objectReferenceValue = found[i];
                    }
                    changed++;
                }
                else if (typeof(Component).IsAssignableFrom(type))
                {
                    List<Component> found = FindInScene(scene, type);
                    if (found.Count > 1)
                    {
                        Debug.LogWarning($"[GameBootstrap] Có {found.Count} {type.Name} trong scene, dùng cái đầu ({Path(found[0].transform)})", found[0]);
                    }
                    Component value = found.Count > 0 ? found[0] : null;
                    if (prop.objectReferenceValue == value)
                    {
                        continue;
                    }
                    prop.objectReferenceValue = value;
                    changed++;
                }
            }
            if (changed > 0)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
            }
            return changed;
        }

        private static List<Component> FindInScene(Scene scene, Type type)
        {
            var list = new List<Component>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                list.AddRange(root.GetComponentsInChildren(type, true));
            }
            return list;
        }

        private static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
    }
}
