using UnityEngine;

namespace Playble.EchoLoop
{
    /// <summary>
    /// Выход с уровня. Активируется навсегда, как только собраны все осколки,
    /// и остаётся активным во всех следующих петлях — последний шанс один,
    /// и это делает финальный заход напряжением, а не формальностью.
    /// </summary>
    [DisallowMultipleComponent]
    public class ExitCore : MonoBehaviour
    {
        MeshRenderer[] _renderers;
        Transform _beam;
        float _phase;

        public bool IsArmed { get; private set; }

        public static ExitCore Create(Transform parent, Vector3 position, float radius)
        {
            var go = new GameObject("ExitCore");
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var core = go.AddComponent<ExitCore>();
            core.BuildVisual(radius);
            return core;
        }

        void BuildVisual(float radius)
        {
            float r = Mathf.Max(radius, 1.2f);
            float bar = 0.28f;
            float h = 0.16f;

            var ringMat = MaterialLibrary.Lit(Palette.StructureDark, 0.3f, 0.35f);
            var glowMat = MaterialLibrary.Glowing(Palette.Accent, 2.5f);

            // Квадратная рамка-приёмник.
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f;
                var offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0f, r - bar * 0.5f);

                var rib = Primitives.Box(transform, "Rib", offset + new Vector3(0f, 0.05f, 0f),
                    new Vector3(r * 2f, h, bar), ringMat, false);
                rib.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
            }

            // Световой столб — главный визуальный якорь уровня.
            var beam = Primitives.Box(transform, "Beam", new Vector3(0f, 3.2f, 0f),
                new Vector3(r * 0.92f, 6.4f, r * 0.92f), MaterialLibrary.Ghost(Palette.Accent, 0.1f), false);
            _beam = beam.transform;

            Primitives.Box(transform, "Pad", new Vector3(0f, 0.1f, 0f),
                new Vector3(r * 0.8f, 0.12f, r * 0.8f), glowMat, false);

            _renderers = GetComponentsInChildren<MeshRenderer>(true);
        }

        public void SetArmed(bool armed) => IsArmed = armed;

        public bool Contains(Vector3 point)
        {
            if (!IsArmed) return false;

            Vector3 d = point - transform.position;
            d.y = 0f;
            return d.sqrMagnitude <= 1.6f * 1.6f;
        }

        void Update()
        {
            _phase += Time.deltaTime;

            Color tint = IsArmed ? Palette.Accent : Palette.Dim(Palette.StructureLight, 0.3f);
            float pulse = IsArmed ? MaterialLibrary.Quantize(2.6f + Mathf.Sin(_phase * 4f) * 1.1f, 8) : 0.7f;
            var beamMaterial = MaterialLibrary.Ghost(tint, IsArmed ? 0.13f : 0.04f);
            var ribMaterial = MaterialLibrary.Glowing(tint, pulse);

            for (int i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;

                r.sharedMaterial = r.gameObject.name == "Beam" ? beamMaterial : ribMaterial;
            }
        }
    }
}