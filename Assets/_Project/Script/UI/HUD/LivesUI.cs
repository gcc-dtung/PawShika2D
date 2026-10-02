using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hàng tim trên HUD (UI Canvas), thay cho đồng hồ đếm ngược của bản gốc.
/// Gắn vào 1 RectTransform; tim được sinh thành con, xếp ngang từ giữa.
/// </summary>
public class LivesUI : MonoBehaviour
{
    [SerializeField] private Sprite fullHeartSprite;   // heart_1
    [SerializeField] private Sprite brokenHeartSprite; // heart_2
    [SerializeField] private float heartSize = 80f;
    [SerializeField] private float spacing = 90f;
    [SerializeField] private float punchScale = 1.4f;
    [SerializeField] private float punchDuration = 0.3f;

    private readonly List<Image> _hearts = new List<Image>();
    private readonly Dictionary<RectTransform, Tween> _punches = new Dictionary<RectTransform, Tween>();

    private void OnEnable()
    {
        if (LivesManager.Instance == null) return;
        LivesManager.Instance.onLivesChanged.AddListener(Refresh);
        LivesManager.Instance.onLifeLost.AddListener(PlayLoseFX);
        Refresh(LivesManager.Instance.CurrentLives, LivesManager.Instance.MaxLives);
    }

    private void OnDisable()
    {
        KillPunches(); // hủy tween, tim về dáng gốc
        if (LivesManager.Instance == null) return;
        LivesManager.Instance.onLivesChanged.RemoveListener(Refresh);
        LivesManager.Instance.onLifeLost.RemoveListener(PlayLoseFX);
    }



    private void Refresh(int current, int max)
    {
        EnsureHeartCount(max);

        for (int i = 0; i < _hearts.Count; i++)
        {
            // Tim mất dần từ phải sang trái
            bool full = i < current;
            _hearts[i].sprite = full ? fullHeartSprite : brokenHeartSprite;
            _hearts[i].color = full ? Color.white : new Color(1f, 1f, 1f, 0.45f);
        }
    }

    private void EnsureHeartCount(int count)
    {
        while (_hearts.Count < count)
        {
            GameObject go = new GameObject($"Heart_{_hearts.Count}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            Image img = go.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            _hearts.Add(img);
        }

        for (int i = 0; i < _hearts.Count; i++)
        {
            bool active = i < count;
            _hearts[i].gameObject.SetActive(active);
            if (!active) continue;

            RectTransform rt = _hearts[i].rectTransform;
            rt.sizeDelta = new Vector2(heartSize, heartSize);
            rt.anchoredPosition = new Vector2((i - (count - 1) / 2f) * spacing, 0f);
            rt.localScale = Vector3.one;
        }
    }

    private void PlayLoseFX(int currentAfterLose)
    {
        int index = currentAfterLose; // tim vừa vỡ
        if (index < 0 || index >= _hearts.Count) return;
        Punch(_hearts[index].rectTransform);
    }

    /// <summary>Phồng to rồi về, lắc trái phải tắt dần.</summary>
    private void Punch(RectTransform t)
    {
        if (_punches.TryGetValue(t, out Tween old)) old.Kill();

        Tween tween = null;
        tween = DOVirtual.Float(0f, 1f, punchDuration, p =>
            {
                t.localScale = Vector3.one * Mathf.Lerp(1f, punchScale, Mathf.Sin(p * Mathf.PI));
                t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(p * Mathf.PI * 4f) * 12f * (1f - p));
            })
            .SetEase(Ease.Linear)
            .SetLink(t.gameObject)
            .OnKill(() =>
            {
                if (t != null)
                {
                    t.localScale = Vector3.one;
                    t.localRotation = Quaternion.identity;
                }
                if (_punches.TryGetValue(t, out Tween cur) && cur == tween) _punches.Remove(t);
            });
        _punches[t] = tween;
    }

    private void KillPunches()
    {
        foreach (Tween tween in new List<Tween>(_punches.Values)) tween.Kill();
        _punches.Clear();
    }
}
