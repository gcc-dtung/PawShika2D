using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hộp quà 1 mốc trên thanh tiến trình Daily Challenge (GiftSlot gốc: Glow tia_sang, Icon hop_qua, Tick).
/// Đủ ngày chưa nhận: tia sáng xoay sau hộp (RotateFX gốc) + hộp nhún; đã nhận: dấu tick, hộp mờ.
/// </summary>
public class DailyGiftSlot : MonoBehaviour
{
    [SerializeField] private int milestone = 7;
    [SerializeField] private Button button;
    [SerializeField] private RectTransform glow;
    [SerializeField] private Image icon;
    [SerializeField] private GameObject tick;
    [SerializeField] private float glowSpeed = 60f; // độ / giây

    private Tween _spin;
    private Tween _bob;

    public int Milestone => milestone;
    public RectTransform Rect => (RectTransform)transform;

    public event Action<DailyGiftSlot> Clicked;

    private void Awake()
    {
        if (button != null) button.onClick.AddListener(() => Clicked?.Invoke(this));
    }

    private void OnDisable()
    {
        _spin?.Kill();
        _bob?.Kill();
        _spin = _bob = null;
    }

    public void SetState(DailyGiftState state)
    {
        bool ready = state == DailyGiftState.Ready;
        if (glow != null)
        {
            glow.gameObject.SetActive(ready);
            if (ready && _spin == null)
                _spin = glow.DOLocalRotate(new Vector3(0f, 0f, -360f), 360f / glowSpeed, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear).SetLoops(-1).SetLink(glow.gameObject);
            else if (!ready)
            {
                _spin?.Kill();
                _spin = null;
            }
        }
        if (icon != null)
        {
            RectTransform rt = icon.rectTransform;
            if (ready && _bob == null)
                _bob = rt.DOScale(1.12f, 0.45f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(icon.gameObject);
            else if (!ready)
            {
                _bob?.Kill();
                _bob = null;
                rt.localScale = Vector3.one;
            }
            icon.color = state == DailyGiftState.Claimed ? new Color(1f, 1f, 1f, 0.55f) : Color.white;
        }
        if (tick != null) tick.SetActive(state == DailyGiftState.Claimed);
    }

    /// <summary>Vừa nhận: hộp nảy.</summary>
    public void Pop()
    {
        if (icon == null) return;
        icon.rectTransform.DOPunchScale(Vector3.one * 0.3f, 0.35f, 6, 0.6f).SetLink(icon.gameObject);
    }
}
