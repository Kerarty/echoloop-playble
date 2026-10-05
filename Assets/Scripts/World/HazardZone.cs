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
                size, MaterialLibrary.Ghost(Palette.Danger, 0.16f), false);
            _renderers.AddRange(volume.GetComponentsInChildren<MeshRenderer>());

            // Полосы предупреждения на верхней грани.
            int count = Mathf.Max(2, Mathf.RoundToInt(Mathf.Max(size.x, size.z) / 0.55f));
            var stripeMat = MaterialLibrary.Glowing(Palette.Danger, 2.8f);

            for (int i = 0; i < count; i++)
            {
                float t = (i + 0.5f) / count;
                float x = Mathf.Lerp(-size.x * 0.5f + 0.14f, size.x * 0.5f - 0.14f, t);

                var stripe = Primitives.Box(transform, "Stripe",
                    new Vector3(x, size.y * 0.5f + 0.03f, 0f),
                    new Vector3(size.x / count * 0.45f, 0.06f, size.z * 0.96f), stripeMat, false);
                stripe.transform.localRotation = Quaternion.Euler(0f, 22f, 0f);
                _renderers.AddRange(stripe.GetComponentsInChildren<MeshRenderer>());
            }
        }

        public bool Contains(Vector3 center, Vector3 size)
        {
            return Zone.Intersects(new Bounds(center, size));
        }

        void Update()
        {
            _phase += Time.deltaTime;
            float pulse = 2.4f + Mathf.Sin(_phase * 5f) * 0.9f;
            var stripeMaterial = MaterialLibrary.Glowing(Palette.Danger, pulse);
            var volumeMaterial = MaterialLibrary.Ghost(Palette.Danger, 0.13f + Mathf.Sin(_phase * 5f) * 0.04f);

            for (int i = 0; i < _renderers.Count; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;
                r.sharedMaterial = r.gameObject.name == "Volume" ? volumeMaterial : stripeMaterial;
            }
        }
    }
}