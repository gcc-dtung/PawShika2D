using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>1 ô thống kê trong popup hồ sơ: icon, nhãn, số.</summary>
public class ProfileStatTile : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_Text value;

    public void Set(Sprite sprite, string labelText, string valueText)
    {
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }
        if (label != null) label.SetText(labelText);
        if (value != null) value.SetText(valueText);
        gameObject.SetActive(true);
    }

    public void Hide() => gameObject.SetActive(false);
}
