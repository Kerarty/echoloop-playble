using System.Collections.Generic;
using UnityEngine;

namespace Playble.EchoLoop
{
    /// <summary>
    /// Энергетический барьер. Открывается, только когда все связанные с ним плиты
    /// нажаты — одновременно игроком и/или эхо. На старте петли закрывается мгновенно,
    /// поэтому «залипнуть» в проёме между петлями нельзя.
    /// </summary>
    [DisallowMultipleComponent]
    public class Gate : MonoBehaviour
    {
        readonly List<MeshRenderer> _renderers = new List<MeshRenderer>();

        Vector3 _closedPosition;
        Vector3 _openPosition;
        int[] _plateIndices = new int[0];
        BoxCollider _collider;
        Transform _visual;

        float _anim;

        public bool IsOpen { get; private set; }

        public static Gate Create(Transform parent, Vector3 center, Vector3 size, Vector3 openOffset, int[] plateIndices)
        {
            var go = new GameObject("Gate");
            go.transform.SetParent(parent, false);
            go.transform.position = center;

            var gate = go.AddComponent<Gate>();
            gate._closedPosition = center;
            gate._openPosition = center + openOffset;
            gate._plateIndices = plateIndices ?? new int[0];
            gate.BuildVisual(size);
            return gate;
        }

        void BuildVisual(Vector3 size)
        {
            // Вся графика барьера живёт под одним узлом: его можно целиком сжать
            // при открытии, иначе рёбра остались бы висеть в воздухе над проёмом.
            var visual = new GameObject("Visual");
            visual.transform.SetParent(transform, false);
            _visual = visual.transform;

            var shell = Primitives.Box(_visual, "Shell", Vector3.zero, size, MaterialLibrary.Ghost(Palette.Power, 0.24f));
            _renderers.AddRange(shell.GetComponentsInChildren<MeshRenderer>());
            _collider = shell.GetComponent<BoxCollider>();

            // Рёбра: барьер должен читаться на любом фоне и с любого ракурса.
            bool vertical = size.y >= size.x && size.y >= size.z;
            int count = Mathf.Clamp(Mathf.RoundToInt(vertical ? size.z : size.x), 2, 7);

            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (float)(count - 1);
                float span = vertical ? size.z : size.x;

                Vector3 local = vertical
                    ? new Vector3(0f, 0f, Mathf.Lerp(-span * 0.5f + 0.06f, span * 0.5f - 0.06f, t))
                    : new Vector3(Mathf.Lerp(-span * 0.5f + 0.06f, span * 0.5f - 0.06f, t), 0f, 0f);

                Vector3 ribSize = vertical
                    ? new Vector3(size.x * 0.82f, size.y * 0.98f, 0.1f)
                    : new Vector3(0.1f, size.y * 0.98f, size.z * 0.82f);

                var rib = Primitives.Box(_visual, "Rib", local, ribSize, MaterialLibrary.Glowing(Palette.Power, 1.5f), false);
                _renderers.AddRange(rib.GetComponentsInChildren<MeshRenderer>());
            }
        }

        /// <summary>Пересчитать состояние по плитам. Барьер без плит всегда закрыт.</summary>
        public bool Evaluate(IReadOnlyList<PressurePlate> plates)
        {
            bool open = _plateIndices.Length > 0;

            for (int i = 0; open && i < _plateIndices.Length; i++)
            {
                int index = _plateIndices[i];
                if (index < 0 || index >= plates.Count || plates[index] == null || !plates[index].IsPressed)
                    open = false;
            }

            IsOpen = open;
            return open;
        }

        public void ResetState()
        {
            IsOpen = false;
            _anim = 0f;
            transform.position = _closedPosition;
            if (_visual != null) _visual.localScale = Vector3.one;
            if (_collider != null) _collider.enabled = true;
        }

        void Update()
        {
            float target = IsOpen ? 1f : 0f;
            _anim = Mathf.MoveTowards(_anim, target, Time.deltaTime * (IsOpen ? 5f : 14f));

            float eased = Mathf.SmoothStep(0f, 1f, _anim);
            transform.position = Vector3.Lerp(_closedPosition, _openPosition, eased);

            // Барьер уходит вверх и сжимается в ниточку, а коллайдер отключается сразу:
            // иначе рёбра остались бы висеть в воздухе над проёмом.
            if (_visual != null) _visual.localScale = Vector3.one * Mathf.Lerp(1f, 0.06f, eased);
            if (_collider != null) _collider.enabled = _anim < 0.3f;

            float q = MaterialLibrary.Quantize(_anim, 8);
            Color tint = Color.Lerp(Palette.Power, Palette.Accent, q);
            float glow = Mathf.Lerp(1.5f, 3.4f, q);

            var shellMaterial = MaterialLibrary.Ghost(tint, Mathf.Lerp(0.24f, 0f, q));
            var ribMaterial = MaterialLibrary.Glowing(tint, glow * (1f - eased));

            for (int i = 0; i < _renderers.Count; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;
                r.sharedMaterial = r.gameObject.name == "Shell" ? shellMaterial : ribMaterial;
            }
        }
    }
}