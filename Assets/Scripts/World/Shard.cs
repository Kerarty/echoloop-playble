using UnityEngine;

namespace Playble.EchoLoop
{
    /// <summary>
    /// Осколок данных. В отличие от плит и врат, осколки переживают петлю:
    /// их достаточно собрать однажды за все попытки. Именно это и делает
    /// многоходовку осмысленной — сначала разведка, потом чистое исполнение.
    /// </summary>
    [DisallowMultipleComponent]
    public class Shard : MonoBehaviour
    {
        public const float PickupRadius = 1.25f;

        Transform _crystal;
        Transform _halo;
        MeshRenderer[] _crystalRenderers;
        float _phase;

        public bool IsCollected { get; private set; }

        public static Shard Create(Transform parent, Vector3 position, int index)
        {
            var go = new GameObject("Shard_" + index);
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var shard = go.AddComponent<Shard>();
            shard._phase = index * 1.3f;
            shard.BuildVisual();
            return shard;
        }

        void BuildVisual()
        {
            var crystalMat = MaterialLibrary.Glowing(Palette.AccentWarm, 3.2f, 0.85f);
            var coreMat = MaterialLibrary.Glowing(Color.Lerp(Palette.AccentWarm, Color.white, 0.55f), 5f);

            // Ромб из повёрнутого куба — читается как кристалл без единого ассета.
            var crystal = Primitives.Box(transform, "Crystal", Vector3.zero,
                new Vector3(0.5f, 0.5f, 0.5f), crystalMat, false);
            crystal.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            _crystal = crystal.transform;
            _crystalRenderers = crystal.GetComponentsInChildren<MeshRenderer>();

            var halo = Primitives.Sphere(transform, "Halo", Vector3.zero, 1.5f, MaterialLibrary.Ghost(Palette.AccentWarm, 0.12f));
            _halo = halo.transform;
        }

        public bool TryCollect(Vector3 actorCenter)
        {
            if (IsCollected) return false;
            if ((actorCenter - transform.position).sqrMagnitude > PickupRadius * PickupRadius) return false;

            IsCollected = true;
            gameObject.SetActive(false);
            return true;
        }

        /// <summary>Вернуть осколок в игру. Используется только при полном рестарте уровня.</summary>
        public void ResetState()
        {
            IsCollected = false;
            if (_crystal != null) _crystal.localScale = Vector3.one;
            gameObject.SetActive(true);
        }

        void Update()
        {
            float t = Time.time + _phase;
            if (_crystal != null)
            {
                _crystal.localRotation = Quaternion.Euler(0f, t * 110f, 45f);
                _crystal.localPosition = new Vector3(0f, Mathf.Sin(t * 1.9f) * 0.14f, 0f);
            }

            if (_halo != null)
            {
                float pulse = 1f + Mathf.Sin(t * 2.4f) * 0.12f;
                _halo.localScale = new Vector3(pulse, pulse, pulse);
            }

            if (_crystalRenderers != null)
            {
                float glow = 2.8f + Mathf.Sin(t * 3f) * 0.6f;
                var mat = MaterialLibrary.Glowing(Palette.AccentWarm, glow, 0.85f);
                for (int i = 0; i < _crystalRenderers.Length; i++)
                    if (_crystalRenderers[i] != null) _crystalRenderers[i].sharedMaterial = mat;
            }
        }
    }
}