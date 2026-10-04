using UnityEngine;

namespace Playble.EchoLoop
{
    /// <summary>
    /// Запись одной петли: поза игрока на каждом такте.
    /// Хранится плотным массивом фиксированного размера — на 8-секундную петлю это
    /// меньше 20 КБ, поэтому переписывать буфер не нужно: на каждой петли
    /// аллокаций нет вообще.
    /// </summary>
    public sealed class LoopTape
    {
        public readonly Vector3[] Positions;
        public readonly Quaternion[] Rotations;
        public readonly int Capacity;

        public LoopTape(int capacity)
        {
            Capacity = Mathf.Max(1, capacity);
            Positions = new Vector3[Capacity];
            Rotations = new Quaternion[Capacity];

            // Пока петля не записана, эхо стоит на месте старта.
            var start = Quaternion.identity;
            for (int i = 0; i < Capacity; i++) Rotations[i] = start;
        }

        public void Capture(int tick, Vector3 position, Quaternion rotation)
        {
            if (tick < 0 || tick >= Capacity) return;
            Positions[tick] = position;
            Rotations[tick] = rotation;
        }

        public Vector3 PositionAt(int tick) =>
            Positions[Mathf.Clamp(tick, 0, Capacity - 1)];

        public Quaternion RotationAt(int tick) =>
            Rotations[Mathf.Clamp(tick, 0, Capacity - 1)];
    }
}