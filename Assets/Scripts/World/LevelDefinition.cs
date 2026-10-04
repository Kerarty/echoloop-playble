using System;
using System.Collections.Generic;
using UnityEngine;

namespace Playble.EchoLoop
{
    public enum BlockTint
    {
        Structure = 0,
        StructureLight = 1,
        StructureDark = 2,
        Accent = 3,
        Danger = 4,
        Power = 5,
    }

    [Serializable]
    public struct BoxDef
    {
        public Vector3 Center;
        public Vector3 Size;
        public BlockTint Tint;
    }

    [Serializable]
    public struct PlateDef
    {
        public Vector3 Center;
        public Vector3 Size;
    }

    [Serializable]
    public struct GateDef
    {
        public Vector3 Center;
        public Vector3 Size;
        /// <summary>Смещение, на которое gate отъезжает в открытом состоянии.</summary>
        public Vector3 OpenOffset;
        /// <summary>Индексы плит, которые должны быть нажаты одновременно.</summary>
        public int[] Plates;
    }

    [Serializable]
    public struct ShardDef
    {
        public Vector3 Position;
    }

    [Serializable]
    public struct HazardDef
    {
        public Vector3 Center;
        public Vector3 Size;
    }

    /// <summary>
    /// Описание уровня как чистые данные. Сцена не хранится вручную: она
    /// собирается из этого описания в рантайме, поэтому уровень добавляется
    /// одним блоком кода, а не редактированием .unity.
    /// </summary>
    [Serializable]
    public class LevelDefinition
    {
        public string Name = "Уровень";
        public string Subtitle = "";
        public string Hint = "";

        public float LoopSeconds = 8f;
        public int MaxEchoes = 3;
        public int ParLoops = 1;

        public Vector3 PlayerStart = new Vector3(0f, 0.1f, 0f);
        public Vector3 CameraFocus;

        public List<BoxDef> Blocks = new List<BoxDef>();
        public List<PlateDef> Plates = new List<PlateDef>();
        public List<GateDef> Gates = new List<GateDef>();
        public List<ShardDef> Shards = new List<ShardDef>();
        public List<HazardDef> Hazards = new List<HazardDef>();

        public Vector3 ExitPosition;
        public float ExitRadius = 2.4f;

        /// <summary>Мировая Y, ниже которой считается падение в пустоту.</summary>
        public float KillPlaneY = -14f;

        public int ShardCount => Shards == null ? 0 : Shards.Count;

        public LevelDefinition(string name, string subtitle, string hint)
        {
            Name = name;
            Subtitle = subtitle;
            Hint = hint;
        }

        public LevelDefinition Block(float cx, float cy, float cz, float sx, float sy, float sz, BlockTint tint = BlockTint.Structure)
        {
            Blocks.Add(new BoxDef { Center = new Vector3(cx, cy, cz), Size = new Vector3(sx, sy, sz), Tint = tint });
            return this;
        }

        /// <summary>Блок, верхняя грань которого поднята на <paramref name="height"/> — удобная опора для прыжка.</summary>
        public LevelDefinition Pedestal(float cx, float cz, float sx, float sz, float topY, float height = 1.2f)
        {
            return Block(cx, topY - height * 0.5f, cz, sx, height, sz, BlockTint.Structure);
        }

        public LevelDefinition Plate(float cx, float cy, float cz, float sx = 2.4f, float sz = 2.4f)
        {
            Plates.Add(new PlateDef { Center = new Vector3(cx, cy, cz), Size = new Vector3(sx, 0.5f, sz) });
            return this;
        }

        public LevelDefinition Gate(float cx, float cy, float cz, float sx, float sy, float sz, Vector3 openOffset, params int[] plates)
        {
            Gates.Add(new GateDef
            {
                Center = new Vector3(cx, cy, cz),
                Size = new Vector3(sx, sy, sz),
                OpenOffset = openOffset,
                Plates = plates ?? Array.Empty<int>(),
            });
            return this;
        }

        public LevelDefinition Shard(float cx, float cy, float cz)
        {
            Shards.Add(new ShardDef { Position = new Vector3(cx, cy, cz) });
            return this;
        }

        public LevelDefinition Hazard(float cx, float cy, float cz, float sx, float sy, float sz)
        {
            Hazards.Add(new HazardDef { Center = new Vector3(cx, cy, cz), Size = new Vector3(sx, sy, sz) });
            return this;
        }
    }
}