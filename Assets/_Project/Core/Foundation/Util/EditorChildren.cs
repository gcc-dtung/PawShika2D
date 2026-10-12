#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PawBlock.Core.Foundation
{
    /// <summary>
    /// Chỉ trong Editor: dựng sẵn object con cho component (thay cho tạo / AddComponent lúc chạy, guide 8.3).
    /// Có rồi (cùng tên) thì dùng lại, chưa có thì tạo kèm Undo. Gọi từ hàm [ContextMenu] EditorBuild của component.
    /// </summary>
    public static class EditorChildren
    {
        /// <summary>Object con <paramref name="name"/> có component <typeparamref name="T"/> (RectTransform nếu cha là UI).</summary>
        public static T Child<T>(Transform parent, string name, bool active = true) where T : Component
        {
            Transform t = parent.Find(name);
            if (t == null)
            {
                GameObject go = parent is RectTransform ? new GameObject(name, typeof(RectTransform)) : new GameObject(name);
                Undo.RegisterCreatedObjectUndo(go, "Dựng sẵn " + name);
                go.layer = parent.gameObject.layer;
                go.transform.SetParent(parent, false);
                t = go.transform;
            }
            t.gameObject.SetActive(active);
            T c = t.GetComponent<T>();
            if (c == null) c = Undo.AddComponent<T>(t.gameObject);
            return c;
        }

        /// <summary>Component <typeparamref name="T"/> trên chính <paramref name="go"/> (thêm nếu chưa có).</summary>
        public static T On<T>(GameObject go) where T : Component
        {
            T c = go.GetComponent<T>();
            return c != null ? c : Undo.AddComponent<T>(go);
        }

        public static RectTransform Rect(Transform parent, string name, bool active = true) => Child<RectTransform>(parent, name, active);
    }
}
#endif
