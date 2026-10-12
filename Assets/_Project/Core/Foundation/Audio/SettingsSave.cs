using System;

namespace PawBlock.Core.Foundation
{
    /// <summary>Section "settings" của save v2: nhạc, âm thanh, rung (AudioManager).</summary>
    [Serializable]
    public class SettingsSave
    {
        public const string Key = "settings";

        public bool music = true;
        public bool sound = true;
        public bool vibrate = true;
    }
}
