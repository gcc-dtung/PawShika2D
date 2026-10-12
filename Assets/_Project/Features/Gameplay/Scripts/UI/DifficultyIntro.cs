using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Hiệu ứng vào level Hard / Very Hard (như bản gốc "EXTREME LEVEL"): lưới hiện xong thì nền tối dần,
    /// dải băng cảnh báo sọc và băng chữ (HARD LEVEL / EXTREME LEVEL) rơi thẳng từ trên xuống giữa (nảy quá 1 chút, hơi xoay về thẳng),
    /// mèo đầu lâu bật ra phía trên băng, giữ một lúc (đầu lâu nhịp nhẹ) rồi đầu lâu thu lại, 2 băng chùng xuống 1 chút
    /// rồi vọt ngược lên khỏi mép trên (dải sọc đi trước băng chữ, nghiêng thêm khi bay), nền sáng lại.
    /// Là 1 ILevelIntro (Order 0, chạy đầu tiên): LevelIntroSequencer chờ lưới hiện xong, khóa lưới trong lúc chạy.
    /// Sprite theo độ khó lấy từ DifficultyTheme.
    /// </summary>
    public class DifficultyIntro : MonoBehaviour, ILevelIntro
    {
        [SerializeField] private DifficultyTheme theme;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Image dim;
        [SerializeField] private RectTransform tape;
        [SerializeField] private Image banner;
        [SerializeField] private Image skull;

        [Header("Nhịp (giây)")]
        [SerializeField] private float dimAlpha = 0.65f;
        [SerializeField] private float slideIn = 0.42f;
        [SerializeField] private float hold = 1.25f;
        [SerializeField] private float slideOut = 0.42f;
        [Tooltip("Băng rơi từ / bay về độ cao này phía trên chỗ đứng (đơn vị canvas, đủ ra khỏi mép trên)")]
        [SerializeField] private float dropHeight = 1600f;
        [Tooltip("Góc xoay (độ) lúc rơi vào: dải sọc / băng chữ, rồi xoay về 0")]
        [SerializeField] private Vector2 enterTilt = new Vector2(-10f, 4f);
        [Tooltip("Góc xoay (độ) khi bay lên: dải sọc / băng chữ (băng chữ lật nghiêng xuống phải như bản gốc)")]
        [SerializeField] private Vector2 exitTilt = new Vector2(-6f, -12f);
        [Tooltip("Tỉ lệ hiện băng chữ so với kích thước ảnh (băng dài quá 2 mép màn, chữ giữ cỡ)")]
        [SerializeField] private float bannerScale = 0.8f;

        private Vector2 _tapePos, _bannerPos, _skullPos;
        private Sequence _seq;

        private void Awake()
        {
            if (tape != null) _tapePos = tape.anchoredPosition;
            if (banner != null) _bannerPos = banner.rectTransform.anchoredPosition;
            if (skull != null) _skullPos = skull.rectTransform.anchoredPosition;
            Hide();
        }

        private void OnDisable() => Stop();

        // ---------- ILevelIntro ----------

        public int Order => 0;
        public float StartDelay => 0f;

        public bool ShouldPlay(LevelLoadedEvent level) => StyleOf(level.Level) != null;

        public void Play(LevelLoadedEvent level, Action onDone)
        {
            DifficultyStyle style = StyleOf(level.Level);
            if (style == null)
            {
                onDone?.Invoke();
                return;
            }
            Play(style, onDone);
        }

        public void Cancel() => Stop();

        private DifficultyStyle StyleOf(LevelDefinition level)
        {
            DifficultyStyle style = theme != null && level != null ? theme.Get(level.difficulty) : null;
            return style != null && style.banner != null ? style : null;
        }

        /// <summary>Chạy hiệu ứng với kiểu <paramref name="style"/> (Debug / thử).</summary>
        public void Play(DifficultyStyle style, Action onDone = null)
        {
            _seq?.Kill();
            gameObject.SetActive(true);
            if (group != null)
            {
                group.alpha = 1f;
                group.blocksRaycasts = true;
            }
            banner.sprite = style.banner;
            banner.SetNativeSize();
            banner.rectTransform.sizeDelta *= bannerScale;
            if (skull != null)
            {
                skull.sprite = style.skull;
                skull.gameObject.SetActive(style.skull != null);
                skull.rectTransform.localScale = Vector3.zero;
                skull.rectTransform.anchoredPosition = _skullPos;
            }
            SetDim(0f);
            RectTransform bn = banner.rectTransform;
            Vector2 up = new Vector2(0f, dropHeight);
            tape.anchoredPosition = _tapePos + up;
            bn.anchoredPosition = _bannerPos + up;
            tape.localEulerAngles = new Vector3(0f, 0f, enterTilt.x);
            bn.localEulerAngles = new Vector3(0f, 0f, enterTilt.y);
            AudioManager.Play(SoundId.HardLevel);

            // Vào: rơi thẳng xuống, nảy quá chỗ đứng 1 chút (OutBack); dải sọc rơi trước, băng chữ theo sau
            Sequence s = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            s.Insert(0f, DOVirtual.Float(0f, dimAlpha, 0.25f, SetDim));
            s.Insert(0f, tape.DOAnchorPos(_tapePos, slideIn).SetEase(Ease.OutBack, 1.1f));
            s.Insert(0f, tape.DOLocalRotate(Vector3.zero, slideIn).SetEase(Ease.OutCubic));
            s.Insert(0.06f, bn.DOAnchorPos(_bannerPos, slideIn).SetEase(Ease.OutBack, 1.4f));
            s.Insert(0.06f, bn.DOLocalRotate(Vector3.zero, slideIn).SetEase(Ease.OutBack, 1.4f));
            if (skull != null && style.skull != null)
            {
                RectTransform sk = skull.rectTransform;
                s.Insert(slideIn * 0.8f, sk.DOScale(1f, 0.32f).SetEase(Ease.OutBack, 2.2f));
                // Đầu lâu nhịp nhẹ trong lúc giữ
                s.Insert(slideIn * 0.8f + 0.4f, sk.DOScale(1.08f, 0.3f).SetEase(Ease.InOutSine).SetLoops(2, LoopType.Yoyo));
            }

            float outAt = slideIn + hold;
            if (skull != null && style.skull != null) s.Insert(outAt, skull.rectTransform.DOScale(0f, 0.2f).SetEase(Ease.InBack));
            // Ra: chùng xuống 1 chút rồi vọt lên khỏi mép trên (InBack); dải sọc đi trước, băng chữ theo sau
            s.Insert(outAt + 0.1f, tape.DOAnchorPos(_tapePos + up, slideOut).SetEase(Ease.InBack, 1.2f));
            s.Insert(outAt + 0.1f, tape.DOLocalRotate(new Vector3(0f, 0f, exitTilt.x), slideOut).SetEase(Ease.InOutSine));
            s.Insert(outAt + 0.16f, bn.DOAnchorPos(_bannerPos + up, slideOut).SetEase(Ease.InBack, 1.2f));
            s.Insert(outAt + 0.16f, bn.DOLocalRotate(new Vector3(0f, 0f, exitTilt.y), slideOut).SetEase(Ease.InOutSine));
            s.Insert(outAt + 0.25f, DOVirtual.Float(dimAlpha, 0f, 0.3f, SetDim));
            s.OnComplete(() =>
            {
                Hide();
                tape.localEulerAngles = Vector3.zero;
                bn.localEulerAngles = Vector3.zero;
                onDone?.Invoke();
            });
            s.OnKill(() => { if (_seq == s) _seq = null; });
            _seq = s;
        }

        private void Stop()
        {
            _seq?.Kill();
            Hide();
        }

        private void Hide()
        {
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
            }
        }

        private void SetDim(float a)
        {
            if (dim == null) return;
            Color c = dim.color;
            c.a = a;
            dim.color = c;
        }

    }
}
