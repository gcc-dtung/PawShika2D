using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;
using PawBlock.Features.Gameplay;

namespace PawBlock.Game
{
    /// <summary>
    /// Nút PLAY trên Home: "Level n" theo level đang chơi, thở nhẹ để mời bấm; bấm -> ScreenFlow.Play.
    /// Level Hard / Very Hard (DifficultyTheme): khung viền kem có tab ghi mức khó (hardFrame) thay khung thường,
    /// mặt nút đổi màu (đỏ / tím), viền chữ "Level n" theo mức khó (mặt chữ vẫn trắng).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class PlayButtonView : MonoBehaviour
    {
        private LevelManager _levels;
        private ScreenFlow _screens;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(ScreenFlow screens, LevelManager levels)
        {
            _screens = screens;
            _levels = levels;
        }

        [SerializeField] private TMP_Text levelText;
        [SerializeField] private float breathScale = 1.04f;
        [SerializeField] private float breathDuration = 0.9f;

        [Header("Độ khó")]
        [SerializeField] private DifficultyTheme theme;
        [SerializeField] private Image frame;        // khung thường (ảnh của nút)
        [SerializeField] private Image fill;         // mặt nút
        [SerializeField] private GameObject hardFrame; // khung kem có tab (khung_home_hard)
        [SerializeField] private TMP_Text tabText;
        [SerializeField] private Vector2 hardFillSize = new Vector2(490f, 190f);
        [SerializeField] private Button button;

        private Sprite _normalFill;
        private Vector2 _normalFillSize;
        private Color _normalFrameColor;
        private Sprite _normalFrameSprite;
        private Vector2 _normalRootSize;
        private Material _normalLevelMaterial;
        private bool _cached;

        private Tween _breath;

        private void Awake()
        {
            button.onClick.AddListener(OnClick);
            CacheNormal();
        }

        private void CacheNormal()
        {
            if (_cached) return;
            _cached = true;
            if (fill != null)
            {
                _normalFill = fill.sprite;
                _normalFillSize = fill.rectTransform.sizeDelta;
            }
            if (levelText != null) _normalLevelMaterial = levelText.fontSharedMaterial;
            if (frame != null)
            {
                _normalFrameColor = frame.color;
                _normalFrameSprite = frame.sprite;
            }
            _normalRootSize = ((RectTransform)transform).sizeDelta;
        }

        private void OnEnable()
        {
            EventBus<LevelLoadedEvent>.Subscribe(OnLevelLoaded);
            Refresh();

            transform.localScale = Vector3.one;
            _breath = transform.DOScale(breathScale, breathDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);
        }

        private void OnDisable()
        {
            EventBus<LevelLoadedEvent>.Unsubscribe(OnLevelLoaded);
            _breath?.Kill();
            _breath = null;
            transform.localScale = Vector3.one;
        }

        private void OnClick()
        {
            if (_screens != null) _screens.Play();
        }

        private void Refresh()
        {
            if (_levels != null) SetLevel(_levels.CurrentLevelNumber);
        }

        private void OnLevelLoaded(LevelLoadedEvent e) => SetLevel(e.Number);

        private void SetLevel(int number)
        {
            if (levelText != null) levelText.SetText("Level {0}", number);
            ApplyDifficulty();
        }

        private void ApplyDifficulty()
        {
            CacheNormal();
            LevelDefinition level = _levels != null ? _levels.CurrentLevel : null;
            DifficultyStyle style = theme != null && level != null ? theme.Get(level.difficulty) : null;
            bool hard = style != null && style.playFace != null;

            // Image gốc luôn bật (vùng nhận chạm của Button). Level khó: đổi sang nền tối của mức khó – khi nhấn mặt nút lún vào
            // thì lộ nền tối trong khung kem (như nút thường lộ btn_green_4); chưa cấu hình nền thì để trong suốt
            if (frame != null)
            {
                frame.enabled = true;
                Color c = _normalFrameColor;
                if (hard && style.playBacker == null) c.a = 0f;
                frame.color = c;
                frame.sprite = hard && style.playBacker != null ? style.playBacker : _normalFrameSprite;
            }
            // Nền tối có viền kem riêng: thu về đúng bề rộng khung có tab để viền không lòi ra 2 bên
            RectTransform root = (RectTransform)transform;
            Vector2 size = _normalRootSize;
            if (hard && hardFrame != null) size.x = ((RectTransform)hardFrame.transform).sizeDelta.x;
            root.sizeDelta = size;
            if (hardFrame != null) hardFrame.SetActive(hard);
            if (fill != null)
            {
                fill.sprite = hard ? style.playFace : _normalFill;
                fill.rectTransform.sizeDelta = hard ? hardFillSize : _normalFillSize;
            }
            if (levelText != null) levelText.fontSharedMaterial = hard && style.playLevelOutline != null ? style.playLevelOutline : _normalLevelMaterial;
            if (tabText != null && hard)
            {
                tabText.SetText(style.label);
                tabText.color = style.playTabTextColor;
            }
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
