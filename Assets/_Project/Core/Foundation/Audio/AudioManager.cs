using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using PawBlock.Core.Contracts;

namespace PawBlock.Core.Foundation
{
    /// <summary>
    /// Quản lý âm thanh:
    /// - Nhạc nền: 1 AudioSource lặp, đổi / bật / tắt có fade.
    /// - Hiệu ứng: pool AudioSource (sfxVoices); hết nguồn rảnh thì dùng lại nguồn phát lâu nhất.
    ///   Mỗi âm có cooldown riêng để nhiều sự kiện dồn cùng lúc không bị dội tiếng.
    /// - Cài đặt Nhạc / Âm thanh / Rung lưu trong SaveData.settings, đổi thì phát SettingsChanged (cho popup cài đặt sau này).
    /// Gọi: AudioManager.Play(SoundId.Click), AudioManager.Vibrate().
    /// </summary>
    public class AudioManager : Singleton<AudioManager>
    {
        [SerializeField] private AudioDatabase database;
        [Tooltip("Số kênh SFX (Editor tạo sẵn object con Sfx_i: menu chuột phải -> Tạo nguồn âm)")]
        [SerializeField] private int sfxVoices = 8;
        [Header("Nguồn âm (object con có sẵn)")]
        [SerializeField] private AudioSource music;
        [SerializeField] private AudioSource[] voices;
        [SerializeField] private float musicFadeDuration = 0.6f;
        [SerializeField] private bool playMusicOnStart = true;

        /// <summary>Đổi cài đặt Nhạc / Âm thanh / Rung.</summary>
        public static event Action SettingsChanged;

        private Tween _musicPitch;
        private float _musicPitchTarget = 1f;
        private readonly Dictionary<SoundId, AudioDatabase.Sound> _lookup = new Dictionary<SoundId, AudioDatabase.Sound>();
        private readonly Dictionary<SoundId, float> _lastPlayed = new Dictionary<SoundId, float>();
        private Tween _musicFade;

        private static ISaveService _save;
        private static SettingsSave _settings = new SettingsSave(); // chưa Init (Edit mode): mặc định, không ghi

        /// <summary>GameBootstrap gọi đầu tiên: section "settings" (nhạc, âm thanh, rung).</summary>
        public static void InitSettings(ISaveService save)
        {
            _save = save;
            _settings = save.Get<SettingsSave>(SettingsSave.Key);
        }

        private static void MarkSettingsDirty() => _save?.MarkDirty();

        public static bool MusicOn
        {
            get => _settings.music;
            set
            {
                if (value == MusicOn) return;
                _settings.music = value;
                MarkSettingsDirty();
                if (HasInstance) Instance.ApplyMusicSetting();
                SettingsChanged?.Invoke();
            }
        }

        public static bool SoundOn
        {
            get => _settings.sound;
            set
            {
                if (value == SoundOn) return;
                _settings.sound = value;
                MarkSettingsDirty();
                if (!value && HasInstance) Instance.StopAllSounds();
                SettingsChanged?.Invoke();
            }
        }

        public static bool VibrateOn
        {
            get => _settings.vibrate;
            set
            {
                if (value == VibrateOn) return;
                _settings.vibrate = value;
                MarkSettingsDirty();
                SettingsChanged?.Invoke();
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (music == null || voices == null || voices.Length == 0)
            {
                Debug.LogError("[AudioManager] Thiếu nguồn âm con (chuột phải component -> Tạo nguồn âm)", this);
            }
            BuildLookup();
        }

        private void Start()
        {
            if (playMusicOnStart && database != null && database.music != null) PlayMusic(database.music);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _musicFade?.Kill();
            _musicPitch?.Kill();
        }

        // ---------- API tĩnh ----------

        /// <summary>Phát âm (bỏ qua khi tắt Âm thanh, chưa gán clip, hoặc đang trong cooldown).</summary>
        public static void Play(SoundId id)
        {
            if (HasInstance) Instance.PlaySound(id, 1f);
        }

        /// <summary>Phát âm với cao độ nhân thêm <paramref name="pitch"/> (1 = giữ nguyên), vd. tick kéo cao dần.</summary>
        public static void PlayPitched(SoundId id, float pitch)
        {
            if (HasInstance) Instance.PlaySound(id, 1f, pitch);
        }

        /// <summary>Phát âm với xác suất <paramref name="chance"/> (0..1), vd. tiếng mèo không phải lần nào cũng kêu.</summary>
        public static void PlayChance(SoundId id, float chance)
        {
            if (UnityEngine.Random.value <= chance) Play(id);
        }

        /// <summary>Rung máy (Android / iOS) nếu bật Rung. Mặc định rung vừa; xem HapticType.</summary>
        public static void Vibrate(HapticType type = HapticType.Medium)
        {
            if (!VibrateOn) return;
            Haptics.Play(type);
        }

        // ---------- Nhạc nền ----------

        /// <summary>Đổi nhạc nền (fade ra bài cũ, fade vào bài mới).</summary>
        public void PlayMusic(AudioClip clip)
        {
            if (clip == null) return;
            if (music.clip == clip && music.isPlaying)
            {
                ApplyMusicSetting();
                return;
            }

            float target = MusicTargetVolume();
            _musicFade?.Kill();
            Sequence seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            if (music.isPlaying) seq.Append(FadeMusic(0f, musicFadeDuration * 0.5f));
            seq.AppendCallback(() =>
            {
                music.clip = clip;
                music.volume = 0f;
                music.Play();
            });
            seq.Append(FadeMusic(target, musicFadeDuration));
            _musicFade = seq;
        }

        /// <summary>Đổi tốc độ / cao độ nhạc nền mượt (vd. sắp hết giờ nhạc nhanh lên). 1 = bình thường.</summary>
        public void SetMusicPitch(float pitch, float duration = 0.4f)
        {
            if (music == null || Mathf.Approximately(_musicPitchTarget, pitch)) return; // gọi lặp lại cùng đích thì để tween chạy tiếp
            _musicPitchTarget = pitch;
            _musicPitch?.Kill();
            _musicPitch = DOVirtual.Float(music.pitch, pitch, duration, v => music.pitch = v)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        public void StopMusic()
        {
            _musicFade?.Kill();
            _musicFade = FadeMusic(0f, musicFadeDuration * 0.5f).SetUpdate(true).SetLink(gameObject)
                .OnComplete(() => music.Stop());
        }

        private void ApplyMusicSetting()
        {
            _musicFade?.Kill();
            _musicFade = FadeMusic(MusicTargetVolume(), musicFadeDuration * 0.5f).SetUpdate(true).SetLink(gameObject);
        }

        private float MusicTargetVolume() => MusicOn && database != null ? database.musicVolume : 0f;

        private Tween FadeMusic(float to, float duration) =>
            DOVirtual.Float(music.volume, to, duration, v => music.volume = v);

        // ---------- Hiệu ứng ----------

        private void PlaySound(SoundId id, float volumeScale, float pitchScale = 1f)
        {
            if (id == SoundId.None || !SoundOn) return;
            if (!_lookup.TryGetValue(id, out AudioDatabase.Sound sound) || sound.clips == null || sound.clips.Length == 0) return;

            float now = Time.unscaledTime;
            if (_lastPlayed.TryGetValue(id, out float last) && now - last < sound.cooldown) return;
            _lastPlayed[id] = now;

            AudioClip clip = sound.clips[UnityEngine.Random.Range(0, sound.clips.Length)];
            if (clip == null) return;

            AudioSource source = FreeVoice();
            source.clip = clip;
            source.volume = sound.volume * volumeScale;
            source.pitch = UnityEngine.Random.Range(sound.pitch.x, sound.pitch.y) * pitchScale;
            source.Play();
        }

        private AudioSource FreeVoice()
        {
            AudioSource oldest = voices[0];
            foreach (AudioSource s in voices)
            {
                if (!s.isPlaying) return s;
                if (s.time > oldest.time) oldest = s;
            }
            return oldest;
        }

        private void StopAllSounds()
        {
            foreach (AudioSource s in voices) s.Stop();
        }

        // ---------- Khởi tạo ----------

#if UNITY_EDITOR
        /// <summary>Tạo object con Music + Sfx_0..n-1 (AudioSource 2D, không tự phát) và gán vào field. Chỉ trong Editor.</summary>
        [ContextMenu("Tạo nguồn âm")]
        public void EditorCreateSources()
        {
            music = EditorSource("Music");
            music.loop = true;
            voices = new AudioSource[sfxVoices];
            for (int i = 0; i < sfxVoices; i++) voices[i] = EditorSource("Sfx_" + i);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private AudioSource EditorSource(string sourceName)
        {
            Transform t = transform.Find(sourceName);
            if (t == null)
            {
                GameObject go = new GameObject(sourceName);
                UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Tạo nguồn âm");
                go.transform.SetParent(transform, false);
                t = go.transform;
            }
            AudioSource s = t.GetComponent<AudioSource>();
            if (s == null) s = UnityEditor.Undo.AddComponent<AudioSource>(t.gameObject);
            s.playOnAwake = false;
            s.spatialBlend = 0f; // âm 2D
            return s;
        }
#endif

        private void BuildLookup()
        {
            _lookup.Clear();
            if (database == null)
            {
                Debug.LogWarning("[AudioManager] Chưa gán AudioDatabase (chạy Tools/PawBlock/Builders/Setup Audio).");
                return;
            }
            foreach (AudioDatabase.Sound s in database.sounds)
            {
                if (s != null && s.id != SoundId.None) _lookup[s.id] = s;
            }
        }
    }
}
