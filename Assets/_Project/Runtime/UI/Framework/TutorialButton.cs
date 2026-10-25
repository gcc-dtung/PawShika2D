using UnityEngine;
using UnityEngine.UI;

namespace PawBlock.Shared
{
    /// <summary>Nút "i" của sự kiện / liveops: bấm mở popup hướng dẫn (<see cref="TutorialPopup"/>) của sự kiện đó.</summary>
    [RequireComponent(typeof(Button))]
    public class TutorialButton : MonoBehaviour
    {
        [Tooltip("Gán từ ngoài: instance popup trong Canvas_Popup (override trên instance MainMenu)")]
        [SerializeField] private TutorialPopup popup;
        [SerializeField] private Button button;

        private void Awake()
        {
            button.onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            if (popup != null && !popup.IsOpen) popup.Show();
        }

#if UNITY_EDITOR
        // Editor tự điền tham chiếu (guide 8.3: lúc chạy không GetComponent)
        private void OnValidate()
        {
            if (button == null) button = GetComponent<Button>();
        }
#endif
    }
}
