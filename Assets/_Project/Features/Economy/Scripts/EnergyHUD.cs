using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Contracts;

namespace PawBlock.Features.Economy
{
    /// <summary>
    /// Năng lượng (tim) ở góc trên: số hiện có đè lên tim, đồng hồ tới lượt hồi kế tiếp ("FULL" khi đầy).
    /// Vô hạn: đổi sang tim có dấu ∞ (infiniteSprite) và ẩn số, đồng hồ đếm thời gian vô hạn còn lại.
    /// Bấm khi chưa đầy -> popup nạp năng lượng. Số đổi thì icon nảy nhẹ.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class EnergyHUD : MonoBehaviour
    {
        private IMenuPopups _popups;
        private EnergyManager _energy;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(EnergyManager energy, IMenuPopups popups)
        {
            _energy = energy;
            _popups = popups;
        }

        [SerializeField] private TMP_Text countText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private RectTransform icon;
        [SerializeField] private Image iconImage;
        [SerializeField] private Sprite normalSprite;   // icon_hear_2
        [SerializeField] private Sprite infiniteSprite; // icon_hear_infinity2

        private Tween _punch;
        private int _lastCount = -1;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        private void OnEnable()
        {
            EnergyManager em = _energy;
            if (em == null) return;
            em.onEnergyChanged.AddListener(OnEnergyChanged);
            em.onTick.AddListener(Refresh);
            Refresh();
        }

        private void OnDisable()
        {
            EnergyManager em = _energy;
            if (em != null)
            {
                em.onEnergyChanged.RemoveListener(OnEnergyChanged);
                em.onTick.RemoveListener(Refresh);
            }
            _punch?.Kill();
            _punch = null;
        }

        private void OnClick()
        {
            EnergyManager em = _energy;
            if (em == null || em.IsFull || em.IsInfinite) return;
            _popups?.OpenEnergyRefill();
        }

        private void OnEnergyChanged(int current, int max)
        {
            if (_lastCount >= 0 && current != _lastCount) Punch();
            Refresh();
        }

        private void Refresh()
        {
            EnergyManager em = _energy;
            if (em == null) return;

            bool infinite = em.IsInfinite;
            _lastCount = em.Current;
            bool swapIcon = iconImage != null && infiniteSprite != null;
            if (swapIcon) iconImage.sprite = infinite ? infiniteSprite : normalSprite;
            if (countText != null)
            {
                countText.enabled = !(infinite && swapIcon); // tim ∞ đã có dấu vô hạn
                if (infinite) countText.SetText("∞");
                else countText.SetText("{0}", em.Current);
            }
            if (timerText != null)
            {
                if (infinite) timerText.SetText(EnergyManager.FormatTime(em.InfiniteSecondsLeft));
                else if (em.IsFull) timerText.SetText("FULL");
                else timerText.SetText(EnergyManager.FormatTime(em.SecondsToNext));
            }
        }

        private void Punch()
        {
            if (icon == null) return;
            _punch?.Kill();
            icon.localScale = Vector3.one;
            _punch = icon.DOPunchScale(Vector3.one * 0.25f, 0.35f, 6, 0.6f)
                .SetLink(gameObject)
                .OnKill(() => { if (icon != null) icon.localScale = Vector3.one; });
        }
    }
}
