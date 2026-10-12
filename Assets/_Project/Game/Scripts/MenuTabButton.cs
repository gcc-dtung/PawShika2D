using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PawBlock.Game
{
    /// <summary>
    /// 1 tab của thanh điều hướng dưới Main Menu. Đang chọn: icon to ra, nhô lên, hiện chữ.
    /// Vị trí / độ rộng của tab do MainMenuNavigator sắp xếp; script này chỉ lo icon, chữ, vạch ngăn.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MenuTabButton : MonoBehaviour
    {
        [SerializeField] private RectTransform icon;
        [SerializeField] private TMP_Text label;
        [SerializeField] private GameObject divider; // vạch ngăn ở mép phải tab

        [Header("Đang chọn")]
        [SerializeField] private float selectedIconScale = 1.3f;
        [SerializeField] private float selectedIconY = 175f;
        [SerializeField] private float normalIconY = 100f;

        [Header("Chuyển tab")]
        [Tooltip("Icon tab vừa chọn: phóng to + nhô lên, vượt nhẹ rồi về (như bản gốc)")]
        [SerializeField] private float selectOvershoot = 2f;
        [Tooltip("Icon tab bỏ chọn thu về nhanh hơn tab mới (tỉ lệ thời gian)")]
        [SerializeField, Range(0.2f, 1f)] private float deselectRatio = 0.6f;

        [Header("Vạch ngăn")]
        [Tooltip("Vạch biến mất trong tỉ lệ này của thời gian chuyển tab (nhanh, trước khi nền tab chọn lướt qua)")]
        [SerializeField, Range(0.1f, 1f)] private float dividerHideRatio = 0.35f;

        private Sequence _anim;
        private Tween _dividerFade;
        private Graphic _dividerGraphic;
        private float _dividerAlpha = 1f;
        private bool _selected;
        private bool _dividerVisible;

        private void Awake()
        {
            if (divider == null) return;
            _dividerGraphic = divider.GetComponent<Graphic>();
            if (_dividerGraphic != null) _dividerAlpha = _dividerGraphic.color.a;
            _dividerVisible = divider.activeSelf;
        }

        public RectTransform Rect => (RectTransform)transform;
        public Button Button => GetComponent<Button>();

        private void OnDisable()
        {
            KillAnim();
            Apply(_selected ? 1f : 0f); // về đúng trạng thái cuối, không kẹt giữa chừng
            if (_dividerFade != null) SetDividerVisible(_dividerVisible);
        }

        /// <summary>Chọn: icon nảy lên (OutBack); bỏ chọn: thu về nhanh (OutCubic, deselectRatio thời gian).</summary>
        public void SetSelected(bool selected, float duration)
        {
            _selected = selected;
            KillAnim();
            if (duration <= 0f || !isActiveAndEnabled)
            {
                Apply(selected ? 1f : 0f);
                return;
            }

            float from = CurrentProgress();
            float to = selected ? 1f : 0f;
            Sequence seq = DOTween.Sequence().SetLink(gameObject);
            Tween t = selected
                ? DOVirtual.Float(from, to, duration, Apply).SetEase(Ease.OutBack, selectOvershoot)
                : DOVirtual.Float(from, to, duration * deselectRatio, Apply).SetEase(Ease.OutCubic);
            seq.Append(t);
            seq.OnKill(() => { if (_anim == seq) _anim = null; });
            _anim = seq;
        }

        /// <summary>Hiện / ẩn vạch ngăn; có <paramref name="duration"/> thì hiện dần trong cả quãng, ẩn nhanh (dividerHideRatio).</summary>
        public void SetDividerVisible(bool visible, float duration = 0f)
        {
            if (divider == null) return;
            _dividerVisible = visible;
            _dividerFade?.Kill();
            _dividerFade = null;

            if (duration <= 0f || _dividerGraphic == null || !isActiveAndEnabled || !Application.isPlaying)
            {
                divider.SetActive(visible);
                SetDividerAlpha(_dividerAlpha);
                return;
            }

            if (visible)
            {
                if (!divider.activeSelf)
                {
                    SetDividerAlpha(0f);
                    divider.SetActive(true);
                }
                _dividerFade = _dividerGraphic.DOFade(_dividerAlpha, duration).SetEase(Ease.OutQuad);
            }
            else
            {
                if (!divider.activeSelf) return;
                _dividerFade = _dividerGraphic.DOFade(0f, duration * dividerHideRatio).SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        divider.SetActive(false);
                        SetDividerAlpha(_dividerAlpha);
                    });
            }
            Tween fade = _dividerFade.SetLink(gameObject);
            fade.OnKill(() => { if (_dividerFade == fade) _dividerFade = null; });
        }

        private void SetDividerAlpha(float a)
        {
            if (_dividerGraphic == null) return;
            Color c = _dividerGraphic.color;
            c.a = a;
            _dividerGraphic.color = c;
        }

        /// <summary>0 = bình thường, 1 = đang chọn.</summary>
        private void Apply(float p)
        {
            if (icon != null)
            {
                icon.localScale = Vector3.one * Mathf.LerpUnclamped(1f, selectedIconScale, p);
                Vector2 pos = icon.anchoredPosition;
                pos.y = Mathf.LerpUnclamped(normalIconY, selectedIconY, p);
                icon.anchoredPosition = pos;
            }
            if (label != null) label.alpha = Mathf.Clamp01(p * 1.5f - 0.5f); // chữ hiện ở nửa sau
        }

        private float CurrentProgress()
        {
            if (icon == null || Mathf.Approximately(selectedIconY, normalIconY)) return _selected ? 0f : 1f;
            return Mathf.InverseLerp(normalIconY, selectedIconY, icon.anchoredPosition.y);
        }

        private void KillAnim()
        {
            _anim?.Kill();
            _anim = null;
        }
    }
}
