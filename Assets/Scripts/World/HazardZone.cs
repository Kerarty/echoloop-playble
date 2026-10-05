using System.Collections.Generic;
using UnityEngine;

namespace Playble.EchoLoop
{
    /// <summary>
    /// Зона опасности. Убивает только игрока: эхо — это запись, оно обязано
    /// честно повторять прошлый промах, иначе петли перестанут быть предсказуемыми.
    /// </summary>
    [DisallowMultipleComponent]
    public class HazardZone : MonoBehaviour
    {
        readonly List<MeshRenderer> _renderers = new List<MeshRenderer>();

        Vector3 _center;
        Vector3 _size;
        float _phase;

        public Bounds Zone => new Bounds(_center, _size);

        public static HazardZone Create(Transform parent, Vector3 center, Vector3 size)
        {
            var go = new GameObject("Hazard");
            go.transform.SetParent(parent, false);
            go.transform.position = center;

            var hazard = go.AddComponent<HazardZone>();
            hazard._center = center;
            hazard._size = size;
            hazard.BuildVisual(size);
            return hazard;
        }

        void BuildVisual(Vector3 size)
        {
            var volume = Primitives.Box(transform, "Volume", Vector3.zero,
                size, MaterialLibrary.Ghost(Palette.Danger, 0.11f), false);
            _renderers.AddRange(volume.GetComponentsInChildren<MeshRenderer>());

            var barMat = MaterialLibrary.Glowing(Palette.Danger, 2.4f);
            var rimMat = MaterialLibrary.Glowing(Palette.Danger, 3.4f);

            // Светящаяся рамка по периметру: опасность должна читаться как граница,
            // а не как пёстрая заливка.
            float top = size.y * 0.5f + 0.04f;
            float rim = 0.16f;

            AddRim(new Vector3(0f, top, size.z * 0.5f - rim * 0.5f), new Vector3(size.x, rim, rim));
            AddRim(new Vector3(0f, top, -size.z * 0.5f + rim * 0.5f), new Vector3(size.x, rim, rim));
            AddRim(new Vector3(size.x * 0.5f - rim * 0.5f, top, 0f), new Vector3(rim, rim, size.z));
            AddRim(new Vector3(-size.x * 0.5f + rim * 0.5f, top, 0f), new Vector3(rim, rim, size.z));

            // Редкие поперечины — задают масштаб и читаются как «решётка» над пропастью.
            float span = Mathf.Max(size.x, size.z);
            int bars = Mathf.Clamp(Mathf.RoundToInt(span / 2.6f), 2, 7);

            for (int i = 0; i < bars; i++)
            {
                float t = (i + 0.5f) / bars;
                bool alongX = size.x >= size.z;

                Vector3 position = alongX
                    ? new Vector3(Mathf.Lerp(-size.x * 0.5f, size.x * 0.5f, t), top, 0f)
                    : new Vector3(0f, top, Mathf.Lerp(-size.z * 0.5f, size.z * 0.5f, t));

                Vector3 barSize = alongX
                    ? new Vector3(0.26f, rim, size.z * 0.94f)
                    : new Vector3(size.x * 0.94f, rim, 0.26f);

                var bar = Primitives.Box(transform, "Bar", position, barSize, barMat, false);
                _renderers.AddRange(bar.GetComponentsInChildren<MeshRenderer>());
            }
        }

        void AddRim(Vector3 position, Vector3 rimSize)
        {
            var piece = Primitives.Box(transform, "Rim", position, rimSize,
                MaterialLibrary.Glowing(Palette.Danger, 3.4f), false);
            _renderers.AddRange(piece.GetComponentsInChildren<MeshRenderer>());
        }

        public bool Contains(Vector3 center, Vector3 size)
        {
            return Zone.Intersects(new Bounds(center, size));
        }

        void Update()
        {
            _phase += Time.deltaTime;

            float pulse = MaterialLibrary.Quantize(2.2f + Mathf.Sin(_phase * 4f) * 0.7f, 8);
            float alpha = MaterialLibrary.Quantize(0.09f + Mathf.Sin(_phase * 4f) * 0.03f, 8);

            var barMaterial = MaterialLibrary.Glowing(Palette.Danger, pulse);
            var rimMaterial = MaterialLibrary.Glowing(Palette.Danger, pulse + 1.2f);
            var volumeMaterial = MaterialLibrary.Ghost(Palette.Danger, alpha);

            for (int i = 0; i < _renderers.Count; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;

                switch (r.gameObject.name)
                {
                    case "Volume": r.sharedMaterial = volumeMaterial; break;
                    case "Rim": r.sharedMaterial = rimMaterial; break;
                    default: r.sharedMaterial = barMaterial; break;
                }
            }
        }
    }
}