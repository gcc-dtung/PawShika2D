using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;

namespace PawBlock.Features.Gameplay
{
    /// <summary>
    /// Hàng đợi intro sau khi nạp level (Chain of Responsibility đơn giản): băng HARD -> popup cơ chế -> tutorial booster...
    /// - Nạp level: hỏi mọi ILevelIntro trong scene (ShouldPlay), có ít nhất 1 thì khóa lưới (InputGate) ngay.
    /// - Mỗi intro chờ: ván Playing, lưới hiện xong, không đang kéo, không trong tutorial lần đầu; rồi chờ thêm StartDelay.
    /// - Intro xong (onDone) thì mở khóa ngay trong cùng frame (vd bấm nút booster của tutorial dùng được luôn), sang intro kế.
    /// Các intro không cần biết nhau: thứ tự chỉ do Order.
    /// </summary>
    public class LevelIntroSequencer : MonoBehaviour
    {
        private LevelManager _levels;

        /// <summary>GameBootstrap gọi trước Awake / OnEnable / Start.</summary>
        public void Init(LevelManager levels)
        {
            _levels = levels;
        }

        [SerializeField] private GridSpawner gridSpawner;

        private readonly List<ILevelIntro> _intros = new List<ILevelIntro>();
        private Coroutine _run;
        private ILevelIntro _current;

        /// <summary>Đang có intro chạy hoặc chờ chạy.</summary>
        public bool IsRunning => _run != null;

        private void Awake()
        {
            if (gridSpawner == null) gridSpawner = FindFirstObjectByType<GridSpawner>();
            CollectIntros();
        }

        private void OnEnable() => EventBus<LevelLoadedEvent>.Subscribe(OnLevelLoaded);

        private void OnDisable()
        {
            EventBus<LevelLoadedEvent>.Unsubscribe(OnLevelLoaded);
            Stop();
        }

        private void CollectIntros()
        {
            _intros.Clear();
            foreach (MonoBehaviour mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb is ILevelIntro intro) _intros.Add(intro);
            }
            _intros.Sort((a, b) => a.Order.CompareTo(b.Order));
        }

        private void OnLevelLoaded(LevelLoadedEvent loaded)
        {
            Stop();
            // LevelManager dựng lưới (GridSpawner.Spawn) trước khi phát event nên LevelData là level vừa nạp
            LevelIntroContext e = new LevelIntroContext(loaded.Number, gridSpawner != null ? gridSpawner.LevelData : null, loaded.IsDaily);
            List<ILevelIntro> pending = new List<ILevelIntro>();
            foreach (ILevelIntro intro in _intros)
            {
                if (IsAlive(intro) && intro.ShouldPlay(e)) pending.Add(intro);
            }
            if (pending.Count == 0) return;

            InputGate.Lock(this); // khóa luôn từ lúc lưới còn đang hiện
            _run = StartCoroutine(Run(pending, e));
        }

        private IEnumerator Run(List<ILevelIntro> pending, LevelIntroContext level)
        {
            foreach (ILevelIntro intro in pending)
            {
                InputGate.Lock(this);
                yield return WaitReady(intro.StartDelay);
                if (!IsAlive(intro) || !intro.ShouldPlay(level)) continue;

                bool done = false;
                _current = intro;
                intro.Play(level, () =>
                {
                    if (done) return;
                    done = true;
                    InputGate.Unlock(this);
                });
                while (!done) yield return null;
                _current = null;
            }
            _run = null;
            InputGate.Unlock(this);
        }

        // Chỉ chạy khi đủ điều kiện liên tục trong delay giây (đang kéo / mở popup thì đếm lại)
        private IEnumerator WaitReady(float delay)
        {
            float readyTime = -1f;
            while (true)
            {
                if (!CanStartNow()) readyTime = -1f;
                else
                {
                    if (readyTime < 0f) readyTime = Time.time + delay;
                    if (Time.time >= readyTime) yield break;
                }
                yield return null;
            }
        }

        private bool CanStartNow()
        {
            LevelManager lm = _levels;
            GridManager gm = GridManager.Instance;
            if (lm == null || gm == null || gm.Board == null || !lm.IsPlaying || gm.IsSelecting) return false;
            if (TutorialController.Active) return false;
            if (gridSpawner != null && gridSpawner.IsIntroPlaying) return false;
            return true;
        }

        private void Stop()
        {
            if (_run != null)
            {
                StopCoroutine(_run);
                _run = null;
            }
            if (_current != null && IsAlive(_current)) _current.Cancel();
            _current = null;
            InputGate.Unlock(this);
        }

        private static bool IsAlive(ILevelIntro intro) => !(intro is UnityEngine.Object o) || o != null;
    }
}
