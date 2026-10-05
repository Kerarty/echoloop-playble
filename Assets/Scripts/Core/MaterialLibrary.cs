using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Playble.EchoLoop
{
    /// <summary>
    /// Ленивый кэш материалов. Весь визуальный язык игры описан кодом,
    /// поэтому в репозитории нет ни одного бинарного ассета с текстурами.
    /// </summary>
    public static class MaterialLibrary
    {
        static readonly Dictionary<int, Material> _lit = new Dictionary<int, Material>();
        static readonly Dictionary<int, Material> _unlit = new Dictionary<int, Material>();

        public static Material Lit(Color color, float smoothness = 0.35f, float metallic = 0f, float emission = 0f)
        {
            int key = Key(color, smoothness, metallic, emission);
            if (_lit.TryGetValue(key, out var cached)) return cached;

            var mat = new Material(FindShader("Universal Render Pipeline/Lit", "Standard"));
            mat.name = "PB_Lit_" + key.ToString("X");
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Glossiness", smoothness);
            mat.SetFloat("_Metallic", metallic);

            if (emission > 0f)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * emission);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            _lit[key] = mat;
            return mat;
        }

        public static Material Glowing(Color color, float intensity = 2.2f, float smoothness = 0.6f)
        {
            return Lit(color, smoothness, 0f, intensity);
        }

        /// <summary>Плоский, неосвещаемый, полупрозрачный цвет — материал эхо.</summary>
        public static Material Ghost(Color color, float alpha)
        {
            int key = Key(color, alpha, 0f, 0f);
            if (_unlit.TryGetValue(key, out var cached)) return cached;

            var mat = new Material(FindShader("Universal Render Pipeline/Unlit", "Unlit/Color", "Sprites/Default"));
            mat.name = "PB_Ghost_" + key.ToString("X");

            Color c = color;
            c.a = alpha;
            mat.SetColor("_BaseColor", c);
            mat.SetColor("_Color", c);

            ConfigureTransparent(mat);

            _unlit[key] = mat;
            return mat;
        }

        /// <summary>
        /// Переводит URP-материал в прозрачный режим. Набор ключей и очередь
        /// прозрачности у URP-шейдеров задаются именно этими свойствами.
        /// </summary>
        public static void ConfigureTransparent(Material mat)
        {
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_AlphaClip", 0f);

            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        /// <summary>
        /// Округляет анимируемое значение до конечного числа ступеней.
        /// Материалы кэшируются по параметрам, поэтому без этого пульсация
        /// пересоздавала бы материал каждый кадр.
        /// </summary>
        public static float Quantize(float value, int steps = 8)
        {
            steps = Mathf.Max(1, steps);
            return Mathf.Round(value * steps) / steps;
        }

        static Shader FindShader(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                var shader = Shader.Find(names[i]);
                if (shader != null) return shader;
            }

            // Шейкер, который гарантированно есть в любой сборке.
            return Shader.Find("Sprites/Default");
        }

        static int Key(Color c, float a, float b, float d)
        {
            int rgba = ((Mathf.RoundToInt(c.r * 255) & 0xFF) << 24) |
                       ((Mathf.RoundToInt(c.g * 255) & 0xFF) << 16) |
                       ((Mathf.RoundToInt(c.b * 255) & 0xFF) << 8) |
                       (Mathf.RoundToInt(c.a * 255) & 0xFF);

            return unchecked(rgba ^ (Mathf.RoundToInt(a * 1000) * 397) ^ (Mathf.RoundToInt(b * 1000) * 7) ^ Mathf.RoundToInt(d * 100));
        }
    }
}