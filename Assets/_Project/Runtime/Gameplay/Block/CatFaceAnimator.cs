using System.Collections.Generic;
using UnityEngine;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Chạy chuỗi khung hình mặt mèo (cắt từ atlas Spine ingame_cat_face_03) trên SpriteRenderer.
    /// Không có Update riêng: mọi mặt mèo đang bật nằm trong 1 danh sách, CatFaceTicker chạy chung 1 vòng mỗi frame.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class CatFaceAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer faceRenderer;

        private static readonly List<CatFaceAnimator> Running = new List<CatFaceAnimator>();

        private CatFaceAnimation _animation;
        private float _fps = 24f;
        private float _phase;
        private int _frame = -1;
        private int _index = -1; // vị trí trong Running, -1: chưa đăng ký

        public void Play(CatFaceAnimation animation, float fps, float width)
        {
            _animation = animation;
            _frame = -1;
            _fps = Mathf.Max(1f, fps);
            _phase = Random.Range(0f, animation.frames.Count / _fps) - Time.time; // lệch pha để các mèo không chớp mắt cùng lúc

            float k = width / Mathf.Max(0.0001f, animation.frames[0].bounds.size.x);
            transform.localScale = new Vector3(k, k, 1f);

            ShowFrame(0);
            if (isActiveAndEnabled) Register();
        }

        public void SetColor(Color color)
        {
            faceRenderer.color = color;
        }

        private void OnEnable()
        {
            if (_animation != null) Register();
        }

        private void OnDisable() => Unregister();

        private void Register()
        {
            if (_index >= 0) return;
            _index = Running.Count;
            Running.Add(this);
        }

        private void Unregister()
        {
            if (_index < 0) return;
            // Lấy phần tử cuối lấp chỗ trống: gỡ O(1)
            int last = Running.Count - 1;
            CatFaceAnimator moved = Running[last];
            Running[_index] = moved;
            moved._index = _index;
            Running.RemoveAt(last);
            _index = -1;
        }

        /// <summary>CatFaceTicker gọi mỗi frame.</summary>
        internal static void TickAll(float time)
        {
            for (int i = 0; i < Running.Count; i++) Running[i].Tick(time);
        }

        private void Tick(float time)
        {
            int count = _animation.frames.Count;
            if (count == 0) return;
            int frame = (int)((time + _phase) * _fps) % count;
            if (frame != _frame) ShowFrame(frame);
        }

        private void ShowFrame(int index)
        {
            _frame = index;
            faceRenderer.sprite = _animation.frames[index];
        }

#if UNITY_EDITOR
        // Editor tự điền tham chiếu (guide 8.3: lúc chạy không GetComponent)
        private void OnValidate()
        {
            if (faceRenderer == null) faceRenderer = GetComponent<SpriteRenderer>();
        }
#endif
    }
}
