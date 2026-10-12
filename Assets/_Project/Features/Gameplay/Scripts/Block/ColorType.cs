using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PawBlock.Features.Gameplay
{
    public enum ColorType
    {
        Blue,
        Brown,
        Cyan,
        Gray,
        Green,
        Orange,
        Pink,
        Red,
        Purple,
        Yellow,
    }

    public static class ColorTypeExtensions
    {
        /// <summary>
        /// Chuyển đổi enum ColorType thành Color RGBA tương ứng với alpha tùy chọn.
        /// </summary>
        public static Color ToColor(this ColorType colorType, float alpha = 1f)
        {
            Color color = colorType switch
            {
                ColorType.Blue   => new Color(0.18431373f, 0.7372549f, 0.9019608f),
                ColorType.Brown  => new Color(0.83137256f, 0.5254902f, 0.19607843f),
                ColorType.Cyan   => new Color(0.18039216f, 0.83137256f, 0.7921569f),
                ColorType.Gray   => new Color(0.8980392f, 0.81960785f, 0.61960787f),
                ColorType.Green  => new Color(0.35686275f, 0.8352941f, 0.24313726f),
                ColorType.Orange => new Color(0.99607843f, 0.41960785f, 0.13725491f),
                ColorType.Pink   => new Color(0.972549f, 0.36078432f, 0.8666667f),
                ColorType.Red    => new Color(0.9882353f, 0.22745098f, 0.23529412f),
                ColorType.Purple => new Color(0.72156864f, 0.36078432f, 0.9490196f),
                ColorType.Yellow => new Color(0.9764706f, 0.83137256f, 0.2509804f),
                _                => Color.white
            };
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        /// <summary>
        /// Lấy ngẫu nhiên 1 ColorType.
        /// </summary>
        public static ColorType GetRandomColorType()
        {
            Array values = Enum.GetValues(typeof(ColorType));
            return (ColorType)values.GetValue(Random.Range(0, values.Length));
        }

        /// <summary>
        /// Lấy ngẫu nhiên 1 Color tương ứng theo ColorType với alpha tùy chọn.
        /// </summary>
        public static Color GetRandomColor(float alpha = 1f)
        {
            return GetRandomColorType().ToColor(alpha);
        }
    }
}
