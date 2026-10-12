using Coffee.UIExtensions;
using DG.Tweening;
using Spine;
using Spine.Unity;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Hiệu ứng booster Đóng băng như bản gốc (cấu trúc FreezeOverlayFX của game gốc):
    /// - Bấm Freeze: Spine "HandStartFreeze" (Freeze.skel) chạy animation "Start": đồng hồ mèo hiện ra, bàn tay bấm nút.
    ///   Thao tác bị khóa tới sự kiện Spine "Press" (lúc ngón tay bấm xuống).
    /// - Sự kiện "Press": nổ hạt Booster_FreezeFX (Twinkle, Spark_Blue, snowflake, Circle_Blue) tại đồng hồ;
    ///   đúng lúc này đồng hồ đóng băng (BoosterManager.freezeEffectDelay = thời điểm sự kiện) -> viền hiện.
    /// - Trong lúc đóng băng: FreezeBorders (ảnh ice_effect phủ toàn màn + hạt tuyết FreezeSnow) mờ dần vào 0.4 s;
    ///   hết đóng băng thì mờ dần ra 0.4 s rồi tắt.
    /// Spine chạy theo thời gian thực để khớp mốc với BoosterManager. Mọi phần không chắn bấm.
    /// </summary>
    public class FreezeOverlayFX : MonoBehaviour
    {
        private BoosterManager _boosterManager;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(BoosterManager boosterManager, LevelTimer timer)
        {
            _boosterManager = boosterManager;
            _timer = timer;
        }

        [Header("Đồng hồ + bàn tay (Spine)")]
        [SerializeField] private SkeletonGraphic handStartFreeze;
        [SerializeField, SpineAnimation(dataField = nameof(handStartFreeze))] private string startAnimation = "Start";
        [SerializeField, SpineEvent(dataField = nameof(handStartFreeze))] private string pressEvent = "Press";

        [Header("Nổ hạt lúc bấm")]
        [SerializeField] private UIParticle burstFX;

        [Header("Trong lúc đóng băng: viền ice_effect + tuyết")]
        [SerializeField] private CanvasGroup borders;
        [SerializeField] private float borderFade = 0.4f;

        private BoosterManager _boosters;
        private LevelTimer _timer;
        private Tween _borderTween;
        private bool _locking;

        private void Awake()
        {
            if (handStartFreeze != null)
            {
                handStartFreeze.raycastTarget = false; // "Unscaled Time" bật sẵn trong Inspector (spine-unity 4.1 không mở cho code)
                handStartFreeze.gameObject.SetActive(false);
            }
            if (burstFX != null)
            {
                burstFX.raycastTarget = false; // UIParticle là Graphic: không được chặn chạm lên lưới
                burstFX.gameObject.SetActive(false);
            }
            if (borders != null)
            {
                borders.alpha = 0f;
                borders.blocksRaycasts = false;
                borders.interactable = false;
                foreach (UIParticle p in borders.GetComponentsInChildren<UIParticle>(true)) p.raycastTarget = false;
                borders.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            _boosters = _boosterManager;
            if (_boosters != null) _boosters.onBoosterUsed.AddListener(OnBoosterUsed);
        }

        private void OnEnable()
        {
            if (_timer == null) return;
            _timer.onFreezeChanged.AddListener(OnFreezeChanged);
            SetBorders(_timer.IsFrozen, true);
        }

        private void OnDisable()
        {
            if (_timer != null) _timer.onFreezeChanged.RemoveListener(OnFreezeChanged);
            _borderTween?.Kill();
            _borderTween = null;
            Unlock();
            if (handStartFreeze != null) handStartFreeze.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_boosters != null) _boosters.onBoosterUsed.RemoveListener(OnBoosterUsed);
        }

        private void OnBoosterUsed(BoosterType type)
        {
            if (type == BoosterType.Freeze) PlayStart();
        }

        // ---------- Đồng hồ + bàn tay ----------

        private void PlayStart()
        {
            if (handStartFreeze == null) return;
            GameObject go = handStartFreeze.gameObject;
            go.SetActive(true);
            go.transform.SetAsLastSibling();

            Spine.AnimationState state = handStartFreeze.AnimationState;
            state.Event -= OnSpineEvent;
            state.Complete -= OnSpineComplete;
            state.ClearTracks();
            handStartFreeze.Skeleton.SetToSetupPose();
            state.SetAnimation(0, startAnimation, false);
            state.Event += OnSpineEvent;
            state.Complete += OnSpineComplete;
            handStartFreeze.Update(0f);

            _locking = true;
            InputGate.Lock(this);
        }

        private void OnSpineEvent(TrackEntry entry, Spine.Event e)
        {
            if (e.Data.Name != pressEvent) return;
            Unlock();
            AudioManager.Vibrate(HapticType.Rise);
            if (burstFX == null) return;
            burstFX.gameObject.SetActive(true);
            burstFX.Clear();
            burstFX.Play();
        }

        private void OnSpineComplete(TrackEntry entry)
        {
            Unlock(); // phòng khi animation không có sự kiện Press
            if (handStartFreeze != null) handStartFreeze.gameObject.SetActive(false);
        }

        private void Unlock()
        {
            if (!_locking) return;
            _locking = false;
            InputGate.Unlock(this);
        }

        // ---------- Viền + tuyết ----------

        private void OnFreezeChanged(bool frozen) => SetBorders(frozen, false);

        private void SetBorders(bool on, bool instant)
        {
            if (borders == null) return;
            _borderTween?.Kill();
            _borderTween = null;
            if (on) borders.gameObject.SetActive(true);
            else if (!borders.gameObject.activeSelf) return;

            float to = on ? 1f : 0f;
            if (instant || borderFade <= 0f)
            {
                borders.alpha = to;
                if (!on) borders.gameObject.SetActive(false);
                return;
            }
            _borderTween = borders.DOFade(to, borderFade)
                .SetEase(Ease.Linear)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    _borderTween = null;
                    if (!on) borders.gameObject.SetActive(false);
                });
        }
    }
}
