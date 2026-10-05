using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

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
    [SerializeField] private int sfxVoices = 8;
    [SerializeField] private float musicFadeDuration = 0.6f;
    [SerializeField] private bool playMusicOnStart = true;

    /// <summary>Đổi cài đặt Nhạc / Âm thanh / Rung.</summary>
    public static event Action SettingsChanged;

    private AudioSource _music;
    private readonly List<AudioSource> _voices = new List<AudioSource>();
    private readonly Dictionary<SoundId, AudioDatabase.Sound> _lookup = new Dictionary<SoundId, AudioDatabase.Sound>();
    private readonly Dictionary<SoundId, float> _lastPlayed = new Dictionary<SoundId, float>();
    private Tween _musicFade;

    public static bool MusicOn
    {
        get => SaveSystem.Data.settings.music;
        set
        {
            if (value == MusicOn) return;
            SaveSystem.Data.settings.music = value;
            SaveSystem.MarkDirty();
            if (Instance != null) Instance.ApplyMusicSetting();
            SettingsChanged?.Invoke();
        }
    }

    public static bool SoundOn
    {
        get => SaveSystem.Data.settings.sound;
        set
        {
            if (value == SoundOn) return;
            SaveSystem.Data.settings.sound = value;
            SaveSystem.MarkDirty();
            if (!value && Instance != null) Instance.StopAllSounds();
            SettingsChanged?.Invoke();
        }
    }

    public static bool VibrateOn
    {
        get => SaveSystem.Data.settings.vibrate;
        set
        {
            if (value == VibrateOn) return;
            SaveSystem.Data.settings.vibrate = value;
            SaveSystem.MarkDirty();
            SettingsChanged?.Invoke();
        }
    }

    protected override void Awake()
    {
        base.Awake();
        _music = CreateSource("Music");
        _music.loop = true;
        for (int i = 0; i < sfxVoices; i++) _voices.Add(CreateSource("Sfx_" + i));
        BuildLookup();
    }

    private void Start()
    {
        if (playMusicOnStart && database != null && database.music != null) PlayMusic(database.music);
    }

    private void OnDestroy()
    {
        _musicFade?.Kill();
    }

    // ---------- API tĩnh ----------

    /// <summary>Phát âm (bỏ qua khi tắt Âm thanh, chưa gán clip, hoặc đang trong cooldown).</summary>
    public static void Play(SoundId id)
    {
        if (Instance != null) Instance.PlaySound(id, 1f);
    }

    /// <summary>Phát âm với xác suất <paramref name="chance"/> (0..1), vd. tiếng mèo không phải lần nào cũng kêu.</summary>
    public static void PlayChance(SoundId id, float chance)
    {
        if (UnityEngine.Random.value <= chance) Play(id);
    }

    /// <summary>Rung máy (Android / iOS) nếu bật Rung.</summary>
    public static void Vibrate()
    {
        if (!VibrateOn) return;
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }

    // ---------- Nhạc nền ----------

    /// <summary>Đổi nhạc nền (fade ra bài cũ, fade vào bài mới).</summary>
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (_music.clip == clip && _music.isPlaying)
        {
            ApplyMusicSetting();
            return;
        }

        float target = MusicTargetVolume();
        _musicFade?.Kill();
        Sequence seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
        if (_music.isPlaying) seq.Append(FadeMusic(0f, musicFadeDuration * 0.5f));
        seq.AppendCallback(() =>
        {
            _music.clip = clip;
            _music.volume = 0f;
            _music.Play();
        });
        seq.Append(FadeMusic(target, musicFadeDuration));
        _musicFade = seq;
    }

    public void StopMusic()
    {
        _musicFade?.Kill();
        _musicFade = FadeMusic(0f, musicFadeDuration * 0.5f).SetUpdate(true).SetLink(gameObject)
            .OnComplete(() => _music.Stop());
    }

    private void ApplyMusicSetting()
    {
        _musicFade?.Kill();
        _musicFade = FadeMusic(MusicTargetVolume(), musicFadeDuration * 0.5f).SetUpdate(true).SetLink(gameObject);
    }

    private float MusicTargetVolume() => MusicOn && database != null ? database.musicVolume : 0f;

    private Tween FadeMusic(float to, float duration) =>
        DOVirtual.Float(_music.volume, to, duration, v => _music.volume = v);

    // ---------- Hiệu ứng ----------

    private void PlaySound(SoundId id, float volumeScale)
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
        source.pitch = UnityEngine.Random.Range(sound.pitch.x, sound.pitch.y);
        source.Play();
    }

    private AudioSource FreeVoice()
    {
        AudioSource oldest = _voices[0];
        foreach (AudioSource s in _voices)
        {
            if (!s.isPlaying) return s;
            if (s.time > oldest.time) oldest = s;
        }
        return oldest;
    }

    private void StopAllSounds()
    {
        foreach (AudioSource s in _voices) s.Stop();
    }

    // ---------- Khởi tạo ----------

    private AudioSource CreateSource(string sourceName)
    {
        GameObject go = new GameObject(sourceName);
        go.transform.SetParent(transform, false);
        AudioSource s = go.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.spatialBlend = 0f; // âm 2D
        return s;
    }

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
