using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bảng âm thanh: mỗi SoundId -> 1 hoặc nhiều clip (phát ngẫu nhiên), âm lượng, khoảng cao độ ngẫu nhiên,
/// khoảng cách tối thiểu giữa 2 lần phát (chống dồn tiếng). Thêm nhạc nền.
/// Tạo / điền sẵn bằng Tools → PawBlock → Builders → Setup Audio; chỉnh tiếp trong Inspector.
/// </summary>
[CreateAssetMenu(fileName = "AudioDatabase", menuName = "ScriptableObjects/AudioDatabase")]
public class AudioDatabase : ScriptableObject
{
    [Serializable]
    public class Sound
    {
        public SoundId id;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Cao độ ngẫu nhiên trong khoảng (x, y); (1, 1) = giữ nguyên")]
        public Vector2 pitch = Vector2.one;
        [Tooltip("Không phát lại âm này trong khoảng (giây)")]
        public float cooldown = 0.05f;
    }

    [Header("Nhạc nền")]
    public AudioClip music;
    [Range(0f, 1f)] public float musicVolume = 0.5f;

    [Header("Âm thanh")]
    public List<Sound> sounds = new List<Sound>();

    public Sound Find(SoundId id) => sounds.Find(s => s.id == id);
}
