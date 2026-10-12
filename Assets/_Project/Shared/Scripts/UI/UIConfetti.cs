using Coffee.UIExtensions;
using UnityEngine;

namespace PawBlock.Shared
{
    /// <summary>
    /// Confetti bằng ParticleSystem vẽ lên uGUI qua UIParticle (package com.coffee.ui-particle):
    /// mô phỏng chạy trong engine (native, đa luồng), cả lớp là 1 mesh / 1 material (atlas confetti_sheet 2x2 + Texture Sheet Animation)
    /// -> nhẹ hơn nhiều so với cập nhật từng Image bằng C#.
    /// - Burst(): 2 ParticleSystem bắn chéo lên từ mép trái / phải.
    /// - StartRain() / StopRain(): ParticleSystem rơi từ mép trên (dừng phát, mảnh đang rơi rơi nốt).
    /// Đặt trên RectTransform phủ màn hình có UIParticle; vị trí phát tính theo kích thước rect lúc gọi (màn cao / thấp đều đúng mép).
    /// 1 đơn vị particle = UIParticle.scale đơn vị canvas.
    /// </summary>
    [RequireComponent(typeof(UIParticle))]
    public class UIConfetti : MonoBehaviour
    {
        [SerializeField] private ParticleSystem burstLeft;
        [SerializeField] private ParticleSystem burstRight;
        [SerializeField] private ParticleSystem rain;
        [Tooltip("Chùm bắn đặt thấp hơn tâm màn hình bao nhiêu (tỉ lệ chiều cao)")]
        [SerializeField, Range(0f, 0.5f)] private float burstHeight = 0.05f;

        private UIParticle _ui;
        private RectTransform _rect;

        public int AliveCount =>
            Count(burstLeft) + Count(burstRight) + Count(rain);

        private void Awake()
        {
            _ui = GetComponent<UIParticle>();
            _rect = (RectTransform)transform;
        }

        private void OnDisable()
        {
            if (_ui != null)
            {
                _ui.Stop();
                _ui.Clear();
            }
        }

        public void Burst()
        {
            Unpause();
            Rect r = _rect.rect;
            float unit = Unit();
            PlaceShape(burstLeft, new Vector3(r.xMin / unit, -r.height * burstHeight / unit, 0f));
            PlaceShape(burstRight, new Vector3(r.xMax / unit, -r.height * burstHeight / unit, 0f));
            Play(burstLeft);
            Play(burstRight);
        }

        public void StartRain()
        {
            if (rain == null) return;
            Unpause();
            Rect r = _rect.rect;
            float unit = Unit();
            ParticleSystem.ShapeModule shape = rain.shape;
            shape.position = new Vector3(0f, r.yMax / unit + 0.4f, 0f);
            shape.scale = new Vector3(r.width / unit, 0.1f, 0.1f);
            Play(rain);
        }

        public void StopRain()
        {
            if (rain != null) rain.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        /// <summary>
        /// UIParticle.Stop() / Clear() (gọi ở OnDisable khi popup đóng) bật cờ isPaused: renderer ngừng mô phỏng.
        /// ParticleSystem.Play() không tắt cờ này -> lần thắng thứ 2 hạt sinh ra ở mép rồi đứng im. Phải Resume trước khi phát.
        /// </summary>
        private void Unpause()
        {
            if (_ui != null && _ui.isPaused) _ui.Resume();
        }

        private float Unit() => _ui != null && _ui.scale > 0f ? _ui.scale : 100f;

        private static void PlaceShape(ParticleSystem ps, Vector3 pos)
        {
            if (ps == null) return;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.position = pos;
        }

        private static void Play(ParticleSystem ps)
        {
            if (ps == null) return;
            ps.Clear(true);
            ps.Play(true);
        }

        private static int Count(ParticleSystem ps) => ps != null ? ps.particleCount : 0;
    }
}
