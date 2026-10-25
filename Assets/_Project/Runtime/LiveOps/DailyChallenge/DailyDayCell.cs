using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PawBlock.Features.DailyChallenge
{
    /// <summary>1 ô ngày trên lịch Daily Challenge (DailyDayCell gốc): khung tròn theo trạng thái + số ngày. Ô trống đầu tháng thì ẩn hết.</summary>
    public class DailyDayCell : MonoBehaviour
    {
        [SerializeField] private Image frame;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Button button;

        public long Day { get; private set; } = -1;
        public DailyDayState State { get; private set; }

        public event Action<DailyDayCell> Clicked;

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(() => Clicked?.Invoke(this));
        }

        public void Setup(long day, int dayOfMonth, DailyDayState state, Sprite frameSprite, Color labelColor, bool interactable)
        {
            Day = day;
            State = state;
            transform.DOKill(true);
            transform.localScale = Vector3.one;
            if (frame != null)
            {
                frame.sprite = frameSprite;
                frame.enabled = frameSprite != null;
            }
            if (label != null)
            {
                label.enabled = true;
                label.SetText("{0}", dayOfMonth);
                label.color = labelColor;
            }
            if (button != null) button.interactable = interactable;
        }

        /// <summary>Ô trống trước ngày 1 của tháng (giữ chỗ trong lưới 7 cột).</summary>
        public void SetEmpty()
        {
            Day = -1;
            if (frame != null) frame.enabled = false;
            if (label != null) label.enabled = false;
            if (button != null) button.interactable = false;
        }

        /// <summary>Ngày vừa thắng: ô nảy lên.</summary>
        public void Pop()
        {
            transform.DOKill(true);
            transform.DOPunchScale(Vector3.one * 0.35f, 0.4f, 6, 0.6f).SetLink(gameObject);
        }
    }
}
