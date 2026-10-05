using UnityEngine;

namespace Playble.EchoLoop
{
    /// <summary>
    /// Базовый класс для всего, что занимает место в пространстве петли и должно
    /// «ощущаться» нажатыми плитами и опасностями. Габариты считаются вручную,
    /// чтобы не зависеть от Physics.autoSyncTransforms — весь игровой логический слой
    /// работает на обычных вычислениях и детерминирован.
    /// </summary>
    public abstract class LoopActor : MonoBehaviour
    {
        public const float ActorHeight = 1.8f;
        public const float ActorRadius = 0.35f;

        /// <summary>
        /// Высота, до которой допрыгивает игрок, встав на эхо (1.8 м) и подпрыгнув.
        /// Ровно это число дизайнер уровней держит как предел для «эхо-платформ».
        /// </summary>
        public const float EchoStepHeight = ActorHeight + PlayerController.JumpApex;

        static readonly Vector3 ActorSize = new Vector3(ActorRadius * 2f, ActorHeight, ActorRadius * 2f);

        /// <summary>Центр объёма тела в мировых координатах.</summary>
        public virtual Vector3 Center => transform.position + Vector3.up * (ActorHeight * 0.5f);

        public virtual Vector3 Size => ActorSize;

        public Bounds Bounds => new Bounds(Center, Size);

        public bool Overlaps(Bounds other) => Bounds.Intersects(other);
    }
}