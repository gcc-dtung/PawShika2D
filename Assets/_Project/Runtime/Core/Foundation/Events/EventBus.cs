using System;
using System.Collections.Generic;
using UnityEngine;
using PawBlock.Core.Contracts;

namespace PawBlock.Core.Foundation
{
    /// <summary>
    /// Kênh sự kiện toàn game theo kiểu (Observer / Publish-Subscribe): bên phát không cần biết ai nghe, bên nghe không cần
    /// .Instance của bên phát. Mỗi kiểu sự kiện T là 1 kênh riêng.
    ///   EventBus&lt;LevelLoadedEvent&gt;.Subscribe(OnLevelLoaded);   // OnEnable / Start
    ///   EventBus&lt;LevelLoadedEvent&gt;.Unsubscribe(OnLevelLoaded); // OnDisable / OnDestroy (nhớ gỡ, giống UnityEvent)
    ///   EventBus&lt;LevelLoadedEvent&gt;.Raise(new LevelLoadedEvent(...));
    /// - Raise chạy trên bản chụp danh sách nghe: nghe / gỡ nghe trong lúc đang phát không làm hỏng vòng lặp.
    /// - 1 listener lỗi chỉ log, các listener còn lại vẫn nhận.
    /// - Dùng UnityEvent khi cần nối trong Inspector; dùng EventBus cho sự kiện nhiều nơi nghe.
    /// </summary>
    public static class EventBus<T> where T : struct, IGameEvent
    {
        private static readonly List<Action<T>> Handlers = new List<Action<T>>();
        private static readonly List<Action<T>> Snapshot = new List<Action<T>>();
        private static int _raising; // Raise lồng nhau (listener phát lại cùng kênh): không dùng chung Snapshot

        static EventBus()
        {
            EventBusRegistry.Register(Clear);
        }

        public static int ListenerCount => Handlers.Count;

        public static void Subscribe(Action<T> handler)
        {
            if (handler == null || Handlers.Contains(handler)) return;
            Handlers.Add(handler);
        }

        public static void Unsubscribe(Action<T> handler)
        {
            if (handler != null) Handlers.Remove(handler);
        }

        public static void Raise(T evt)
        {
            if (Handlers.Count == 0) return;

            List<Action<T>> list = _raising == 0 ? Snapshot : new List<Action<T>>(Handlers.Count);
            list.Clear();
            list.AddRange(Handlers);
            _raising++;
            try
            {
                foreach (Action<T> handler in list)
                {
                    try
                    {
                        handler(evt);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            finally
            {
                _raising--;
                if (_raising == 0) Snapshot.Clear();
            }
        }

        public static void Clear()
        {
            Handlers.Clear();
            Snapshot.Clear();
            _raising = 0;
        }
    }

    /// <summary>Xóa mọi kênh khi vào Play mode (tắt Domain Reload thì static không tự reset).</summary>
    public static class EventBusRegistry
    {
        private static readonly List<Action> ClearActions = new List<Action>();

        internal static void Register(Action clear) => ClearActions.Add(clear);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearAll()
        {
            foreach (Action clear in ClearActions) clear();
        }
    }
}
