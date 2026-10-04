using System;
using System.Collections.Generic;
using UnityEngine;

namespace Playble.EchoLoop
{
    /// <summary>
    /// Собранный уровень: геометрия плюс игровая логика петли.
    /// Разделяет «что переживает петлю» и «что сбрасывается»:
    /// сбрасываются плиты, врата и состояние выхода, осколки — нет.
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelRuntime : MonoBehaviour
    {
        readonly List<PressurePlate> _plates = new List<PressurePlate>();
        readonly List<Gate> _gates = new List<Gate>();
        readonly List<Shard> _shards = new List<Shard>();
        readonly List<HazardZone> _hazards = new List<HazardZone>();

        ExitCore _exit;

        public event Action<Shard> ShardCollected;
        public event Action PlayerDied;
        public event Action Completed;

        public LevelDefinition Definition { get; private set; }
        public IReadOnlyList<PressurePlate> Plates => _plates;
        public int ShardsTotal => _shards.Count;
        public int ShardsCollected { get; private set; }
        public bool AllShardsCollected => ShardsTotal > 0 && ShardsCollected >= ShardsTotal;
        public bool IsComplete { get; private set; }

        // ---------------------------------------------------------------

        public void Build(LevelDefinition definition, Transform root)
        {
            Definition = definition;

            var geometry = new GameObject("Geometry").transform;
            geometry.SetParent(root, false);

            for (int i = 0; i < definition.Blocks.Count; i++)
            {
                BuildBlock(geometry, definition.Blocks[i]);
            }

            var interactives = new GameObject("Interactives").transform;
            interactives.SetParent(root, false);

            for (int i = 0; i < definition.Plates.Count; i++)
            {
                _plates.Add(PressurePlate.Create(interactives, i, definition.Plates[i].Center, definition.Plates[i].Size));
            }

            for (int i = 0; i < definition.Gates.Count; i++)
            {
                var g = definition.Gates[i];
                _gates.Add(Gate.Create(interactives, g.Center, g.Size, g.OpenOffset, g.Plates));
            }

            for (int i = 0; i < definition.Shards.Count; i++)
            {
                _shards.Add(Shard.Create(interactives, definition.Shards[i].Position, i));
            }

            for (int i = 0; i < definition.Hazards.Count; i++)
            {
                var h = definition.Hazards[i];
                _hazards.Add(HazardZone.Create(interactives, h.Center, h.Size));
            }

            _exit = ExitCore.Create(interactives, definition.ExitPosition, definition.ExitRadius);

            ShardsCollected = 0;
            IsComplete = false;
            _exit.SetArmed(ShardsTotal == 0);
        }

        static void BuildBlock(Transform root, BoxDef box)
        {
            Primitives.Box(root, "Block", box.Center, box.Size, MaterialFor(box.Tint));

            // Тонкая кромка по верхней грани: даёт «объём» и помогает оценивать высоту.
            if (box.Tint == BlockTint.Structure && box.Size.y > 0.3f)
            {
                Primitives.Box(root, "Trim",
                    box.Center + new Vector3(0f, box.Size.y * 0.5f + 0.03f, 0f),
                    new Vector3(box.Size.x * 0.96f, 0.06f, box.Size.z * 0.96f),
                    MaterialLibrary.Lit(Palette.StructureLight, 0.45f, 0.1f), false);
            }
        }

        public static Material MaterialFor(BlockTint tint)
        {
            switch (tint)
            {
                case BlockTint.StructureLight: return MaterialLibrary.Lit(Palette.StructureLight, 0.4f);
                case BlockTint.StructureDark: return MaterialLibrary.Lit(Palette.StructureDark, 0.25f, 0.2f);
                case BlockTint.Accent: return MaterialLibrary.Glowing(Palette.Accent, 1.7f);
                case BlockTint.Danger: return MaterialLibrary.Glowing(Palette.Danger, 1.7f);
                case BlockTint.Power: return MaterialLibrary.Glowing(Palette.Power, 1.7f);
                default: return MaterialLibrary.Lit(Palette.Structure, 0.35f);
            }
        }

        // ---------------------------------------------------------------

        /// <summary>Один шаг логики петли. Вызывается строго один раз на тик.</summary>
        public void Tick(IReadOnlyList<LoopActor> actors, PlayerController player, float deltaTime)
        {
            if (IsComplete || player == null || !player.IsAlive) return;

            for (int i = 0; i < _plates.Count; i++)
                _plates[i].Evaluate(actors);

            for (int i = 0; i < _gates.Count; i++)
                _gates[i].Evaluate(_plates);

            Vector3 playerCenter = player.Center;

            for (int i = 0; i < _shards.Count; i++)
            {
                if (_shards[i].IsCollected) continue;
                if (!_shards[i].TryCollect(playerCenter)) continue;

                ShardsCollected++;
                ShardCollected?.Invoke(_shards[i]);

                if (AllShardsCollected && _exit != null) _exit.SetArmed(true);
            }

            for (int i = 0; i < _hazards.Count; i++)
            {
                if (_hazards[i].Contains(playerCenter, player.Size))
                {
                    PlayerDied?.Invoke();
                    return;
                }
            }

            if (_exit != null && _exit.Contains(playerCenter))
            {
                IsComplete = true;
                Completed?.Invoke();
            }
        }

        /// <summary>Сброс всего, что не должно жить дольше одной петли.</summary>
        public void ResetTransient()
        {
            for (int i = 0; i < _plates.Count; i++) _plates[i].ResetState();
            for (int i = 0; i < _gates.Count; i++) _gates[i].ResetState();
        }

        /// <summary>Полный сброс уровня, включая собранные осколки.</summary>
        public void ResetEverything()
        {
            ResetTransient();
            for (int i = 0; i < _shards.Count; i++) _shards[i].ResetState();

            ShardsCollected = 0;
            IsComplete = false;
            if (_exit != null) _exit.SetArmed(ShardsTotal == 0);
        }
    }
}