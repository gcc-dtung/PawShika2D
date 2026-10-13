using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PawBlock.Core.Contracts;
using PawBlock.Core.Foundation;
using PawBlock.Shared;

namespace PawBlock.Game
{
    /// <summary>
    /// Tools → PawBlock → Builders → Setup Audio:
    /// - Tạo / bổ sung AudioDatabase (Shared/Configs) với clip trong Shared/Audio (game gốc) và Shared/Audio/ShikaCat.
    ///   Chạy lại chỉ thêm âm / clip còn thiếu, không ghi đè volume / pitch / cooldown đã chỉnh trong Inspector.
    /// - Nhạc nền nhập kiểu Streaming (không nạp cả bài vào RAM).
    /// - Scene: object AudioManager (AudioManager + GameAudioEvents), UIButtonSounds trên Canvas_Menu / Canvas_HUD / Canvas_Popup.
    /// </summary>
    public static class AudioSetup
    {
        private const string DatabasePath = "Assets/_Project/Shared/Configs/AudioDatabase.asset";
        private const string A = "Assets/_Project/Shared/Audio/";
        private const string S = "Assets/_Project/Shared/Audio/ShikaCat/";
        private const string MusicPath = A + "BGM/BGM.ogg";

        private static readonly string[] Canvases = { "Canvas_Menu", "Canvas_HUD", "Canvas_Popup" };

        private struct Def
        {
            public SoundId id;
            public string[] clips;
            public float volume;
            public Vector2 pitch;
            public float cooldown;

            public Def(SoundId id, float volume, float cooldown, Vector2 pitch, params string[] clips)
            {
                this.id = id;
                this.volume = volume;
                this.cooldown = cooldown;
                this.pitch = pitch;
                this.clips = clips;
            }
        }

        private static readonly Vector2 Same = Vector2.one;
        private static readonly Vector2 Vary = new Vector2(0.95f, 1.08f);

        private static readonly Def[] Defaults =
        {
            new Def(SoundId.Click, 0.8f, 0.05f, Same, A + "UI_SFX/click.ogg"),
            new Def(SoundId.PopupOpen, 0.7f, 0.1f, Same, S + "UI_SFX/SC_SFX_UI_Popup_Open_01.ogg"),
            new Def(SoundId.PopupClose, 0.6f, 0.1f, Same, S + "UI_SFX/SC_SFX_UI_Popup_Close_01.ogg"),
            new Def(SoundId.Coin, 0.8f, 0.1f, Same, A + "UI_SFX/earn_coin.ogg"),
            new Def(SoundId.Reward, 0.9f, 0.3f, Same, A + "UI_SFX/earrn_reward.ogg"),
            new Def(SoundId.LevelStart, 0.8f, 0.3f, Same, A + "Gameplay_SFX/load_level.ogg"),
            new Def(SoundId.BlockPlace, 1f, 0.05f, Vary, A + "Gameplay_SFX/block_true.ogg"),
            new Def(SoundId.Mistake, 1f, 0.1f, Same, S + "Gameplay_SFX/SC_Gameplay_Mistake_01.ogg"),
            new Def(SoundId.Unlock, 0.9f, 0.05f, Vary, A + "Gameplay_SFX/unlock_obs2.ogg"),
            new Def(SoundId.Win, 1f, 0.5f, Same, A + "Gameplay_SFX/win.ogg"),
            new Def(SoundId.Lose, 1f, 0.5f, Same, A + "Gameplay_SFX/failed.ogg"),
            new Def(SoundId.OutOfTime, 0.9f, 0.5f, Same, A + "Gameplay_SFX/warning.ogg"),
            new Def(SoundId.HardLevel, 0.9f, 0.5f, Same, A + "Gameplay_SFX/warning.ogg"),
            new Def(SoundId.TimeWarning, 0.9f, 0.5f, Same, A + "Gameplay_SFX/warningEndTime.ogg"),
            new Def(SoundId.BlockRemove, 0.8f, 0.05f, Vary, A + "Gameplay_SFX/poof_star.ogg"),
            new Def(SoundId.Booster, 0.9f, 0.1f, Same, A + "Gameplay_SFX/booster2.ogg"),
            new Def(SoundId.FreezeOn, 0.9f, 0.1f, Same, A + "Gameplay_SFX/freeze_appear.ogg"),
            new Def(SoundId.FreezeEnd, 1f, 0.1f, Same, A + "Gameplay_SFX/freeze_start.ogg"),
            new Def(SoundId.StarLost, 0.9f, 0.1f, Same, A + "Gameplay_SFX/poof_star.ogg"),
            new Def(SoundId.StarEarn, 0.9f, 0.05f, Same, A + "Gameplay_SFX/unlock_obs2.ogg"),
            new Def(SoundId.DragTick, 0.45f, 0.03f, Same, S + "UI_SFX/SC_SFX_UI_Button_01.ogg"),
            new Def(SoundId.DragReady, 0.6f, 0.08f, Same, S + "Gameplay_SFX/SC_SFX_Gameplay_Box_Created_01.ogg"),
            new Def(SoundId.DragCancel, 0.5f, 0.1f, new Vector2(0.78f, 0.85f), S + "UI_SFX/SC_SFX_UI_Popup_Close_01.ogg"),
            new Def(SoundId.CatHappy, 0.6f, 0.4f, new Vector2(0.95f, 1.1f),
                A + "Cat_Voices/Happy.ogg", A + "Cat_Voices/Joyful.ogg", A + "Cat_Voices/Cute.ogg", A + "Cat_Voices/Cool.ogg", A + "Cat_Voices/Blink.ogg",
                A + "Cat_Voices/Sly.ogg", A + "Cat_Voices/Lying.ogg"),
            new Def(SoundId.CatSad, 0.7f, 0.4f, new Vector2(0.95f, 1.05f),
                A + "Cat_Voices/Angry1.ogg", A + "Cat_Voices/Angry2.ogg", A + "Cat_Voices/Sad.ogg", A + "Cat_Voices/Dizzy.ogg", A + "Cat_Voices/Scare.ogg",
                A + "Cat_Voices/Dumb.ogg"),
        };

        [MenuItem("Tools/PawBlock/Builders/Setup Audio", priority = 112)]
        public static void Setup()
        {
            AudioDatabase db = BuildDatabase();
            SetMusicImport();

            // Scene: AudioManager
            GameObject go = FindRoot("AudioManager");
            if (go == null)
            {
                go = new GameObject("AudioManager");
                Undo.RegisterCreatedObjectUndo(go, "Setup Audio");
            }
            AudioManager manager = go.GetComponent<AudioManager>();
            if (manager == null) manager = Undo.AddComponent<AudioManager>(go);
            if (go.GetComponent<GameAudioEvents>() == null) Undo.AddComponent<GameAudioEvents>(go);
            AssignDatabase(manager, db);

            int canvases = 0;
            foreach (string name in Canvases)
            {
                GameObject canvas = FindRoot(name);
                if (canvas == null) continue;
                if (canvas.GetComponent<UIButtonSounds>() == null) Undo.AddComponent<UIButtonSounds>(canvas);
                canvases++;
            }

            EditorSceneManager.MarkSceneDirty(go.scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[AudioSetup] AudioDatabase: {db.sounds.Count} âm + nhạc nền; AudioManager trong scene; âm nút trên {canvases} canvas.");
        }

        private static GameObject FindRoot(string name)
        {
            foreach (GameObject root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == name) return root;
            }
            return null;
        }

        private static void AssignDatabase(AudioManager manager, AudioDatabase db)
        {
            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("database").objectReferenceValue = db;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static AudioDatabase BuildDatabase()
        {
            AudioDatabase db = AssetDatabase.LoadAssetAtPath<AudioDatabase>(DatabasePath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<AudioDatabase>();
                AssetDatabase.CreateAsset(db, DatabasePath);
            }

            if (db.music == null)
            {
                db.music = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);
                db.musicVolume = 0.45f;
            }

            foreach (Def d in Defaults)
            {
                List<AudioClip> clips = LoadClips(d.clips);
                AudioDatabase.Sound existing = db.Find(d.id);
                if (existing != null)
                {
                    // Đã có (có thể đã chỉnh tay): giữ volume / pitch / cooldown, chỉ bổ sung clip mới thêm vào Defaults
                    List<AudioClip> merged = new List<AudioClip>(existing.clips ?? new AudioClip[0]);
                    foreach (AudioClip clip in clips) if (!merged.Contains(clip)) merged.Add(clip);
                    existing.clips = merged.ToArray();
                    continue;
                }
                db.sounds.Add(new AudioDatabase.Sound
                {
                    id = d.id,
                    clips = clips.ToArray(),
                    volume = d.volume,
                    pitch = d.pitch,
                    cooldown = d.cooldown
                });
            }
            db.sounds.Sort((a, b) => a.id.CompareTo(b.id));
            EditorUtility.SetDirty(db);
            return db;
        }

        private static List<AudioClip> LoadClips(string[] paths)
        {
            List<AudioClip> clips = new List<AudioClip>();
            foreach (string path in paths)
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null) clips.Add(clip);
                else Debug.LogWarning($"[AudioSetup] Không tìm thấy clip: {path}");
            }
            return clips;
        }

        private static void SetMusicImport()
        {
            AudioImporter importer = AssetImporter.GetAtPath(MusicPath) as AudioImporter;
            if (importer == null) return;
            AudioImporterSampleSettings s = importer.defaultSampleSettings;
            if (s.loadType == AudioClipLoadType.Streaming) return;
            s.loadType = AudioClipLoadType.Streaming;
            importer.defaultSampleSettings = s;
            importer.SaveAndReimport();
        }
    }
}
