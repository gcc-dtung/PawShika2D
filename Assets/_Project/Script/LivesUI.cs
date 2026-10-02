using System.Collections;
using System.Collections.Generic;
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

    private void OnEnable()
    {
        if (LivesManager.Instance == null) return;
        LivesManager.Instance.onLivesChanged.AddListener(Refresh);
        LivesManager.Instance.onLifeLost.AddListener(PlayLoseFX);
        Refresh(LivesManager.Instance.CurrentLives, LivesManager.Instance.MaxLives);
    }

    private void OnDisable()
    {
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
        StartCoroutine(Punch(_hearts[index].rectTransform));
    }

    private IEnumerator Punch(RectTransform t)
    {
        float elapsed = 0f;
        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / punchDuration);
            t.localScale = Vector3.one * Mathf.Lerp(1f, punchScale, Mathf.Sin(p * Mathf.PI));
            t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(p * Mathf.PI * 4f) * 12f * (1f - p));
            yield return null;
        }
        t.localScale = Vector3.one;
        t.localRotation = Quaternion.identity;
    }
}
