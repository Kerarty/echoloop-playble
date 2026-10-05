using System;
using UnityEngine;

namespace Playble.EchoLoop
{
    /// <summary>
    /// Персонаж. Плоское управление в мировых координатах (камера не вращается),
    /// CharacterController для надёжных коллизий и предсказуемое поведение:
    /// coyote-time, буфер прыжка, регулируемая воздушная управляемость.
    /// Движение вызывается строго из тика петли, а не из FixedUpdate.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public class PlayerController : LoopActor
    {
        [Header("Движение")]
        [SerializeField] float _moveSpeed = 7.2f;
        [SerializeField] float _airControl = 0.7f;
        [SerializeField] float _acceleration = 70f;

        [Header("Прыжок")]
        [SerializeField] float _jumpApex = 2.4f;
        [SerializeField] float _gravity = 32f;
        [SerializeField] float _lowJumpGravityMultiplier = 1.9f;
        [SerializeField] float _coyoteTime = 0.1f;
        [SerializeField] float _jumpBufferTime = 0.12f;
        [SerializeField] float _fastFallSpeed = 34f;

        [Header("Оформление")]
        [SerializeField] float _turnSpeed = 16f;
        [SerializeField] float _killPlaneY = -14f;

        CharacterController _cc;
        Vector3 _velocity;
        Vector3 _facing = Vector3.forward;

        float _jumpVelocity;
        float _lastGroundedTime = -99f;
        float _jumpRequestedTime = -99f;
        float _simulationTime;
        float _squash = 1f;

        Transform _visual;
        Transform _visor;
        MeshRenderer _visorRenderer;

        public event Action Died;
        public event Action Jumped;
        public event Action Landed;

        public bool IsGrounded => _cc != null && _cc.isGrounded;
        public Vector3 Velocity => _velocity;
        public float SpeedNormalized => _moveSpeed <= 0f ? 0f : Mathf.Clamp01(new Vector2(_velocity.x, _velocity.z).magnitude / _moveSpeed);
        public bool IsAlive { get; private set; } = true;

        public static PlayerController Create(Transform parent, Vector3 position)
        {
            var go = new GameObject("Player");
            go.transform.SetParent(parent, false);
            go.transform.position = position;

            var cc = go.AddComponent<CharacterController>();
            cc.height = ActorHeight;
            cc.radius = ActorRadius;
            cc.center = new Vector3(0f, ActorHeight * 0.5f, 0f);
            cc.slopeLimit = 55f;
            cc.stepOffset = 0.4f;
            cc.skinWidth = 0.03f;
            cc.minMoveDistance = 0f;

            var player = go.AddComponent<PlayerController>();
            player.Cache();
            player.BuildVisual();
            player.Teleport(position, Quaternion.identity);
            return player;
        }

        void Cache()
        {
            _cc = GetComponent<CharacterController>();
            _jumpVelocity = Mathf.Sqrt(2f * _gravity * _jumpApex);
        }

        void BuildVisual()
        {
            var bodyMat = MaterialLibrary.Lit(Palette.Player, 0.5f, 0f, 0.35f);
            var accentMat = MaterialLibrary.Glowing(Palette.Accent, 2.6f);

            var root = new GameObject("Visual");
            root.transform.SetParent(transform, false);
            _visual = root.transform;

            Primitives.Capsule(_visual, "Body", new Vector3(0f, ActorHeight * 0.5f, 0f),
                ActorHeight, ActorRadius, bodyMat, false);

            Primitives.Capsule(_visual, "Crown", new Vector3(0f, ActorHeight * 0.97f, 0f),
                0.3f, ActorRadius * 0.95f, accentMat, false);

            var visor = Primitives.Box(_visual, "Visor",
                new Vector3(0f, ActorHeight * 0.72f, ActorRadius * 0.95f),
                new Vector3(0.34f, 0.12f, 0.08f), accentMat, false);
            _visor = visor.transform;
            _visorRenderer = visor.GetComponent<MeshRenderer>();

            Primitives.Box(_visual, "Backpack",
                new Vector3(0f, ActorHeight * 0.55f, -ActorRadius * 1.05f),
                new Vector3(0.3f, 0.34f, 0.14f), MaterialLibrary.Lit(Palette.Dim(Palette.Accent, 0.25f), 0.4f), false);
        }

        // ------------------------------------------------------------------

        /// <summary>Один шаг симуляции. Вызывается из тика петли.</summary>
        public void SimulateStep(float dt, Vector2 moveInput, bool jumpPressed, bool jumpHeld)
        {
            if (!IsAlive) return;

            _simulationTime += dt;

            if (jumpPressed) _jumpRequestedTime = _simulationTime;

            Vector2 move = moveInput;
            float control = IsGrounded ? 1f : _airControl;

            Vector3 desired = new Vector3(move.x, 0f, move.y) * _moveSpeed;
            Vector3 planar = new Vector3(_velocity.x, 0f, _velocity.z);
            planar = Vector3.MoveTowards(planar, desired, _acceleration * control * dt);

            // Гравитация: удержание кнопки даёт «длинный» прыжок, отпускание — короткий.
            float g = _gravity;
            if (_velocity.y > 0f && !jumpHeld) g *= _lowJumpGravityMultiplier;

            _velocity.x = planar.x;
            _velocity.z = planar.z;
            _velocity.y = Mathf.Max(_velocity.y - g * dt, -_fastFallSpeed);

            bool wasGrounded = IsGrounded;

            if (wasGrounded && _velocity.y <= 0f)
            {
                if (_velocity.y < -6f) _squash = 0.72f;
                _lastGroundedTime = _simulationTime;
                _velocity.y = -2f;
            }

            bool coyoteValid = (_simulationTime - _lastGroundedTime) <= _coyoteTime;
            bool bufferValid = (_simulationTime - _jumpRequestedTime) <= _jumpBufferTime;

            if (coyoteValid && bufferValid)
            {
                _velocity.y = _jumpVelocity;
                _lastGroundedTime = -99f;
                _jumpRequestedTime = -99f;
                _squash = 1.28f;
                Jumped?.Invoke();
            }

            _cc.Move(_velocity * dt);

            if (move.sqrMagnitude > 0.02f) _facing = new Vector3(move.x, 0f, move.y).normalized;

            UpdateVisual(dt);

            if (transform.position.y < _killPlaneY) Kill();
        }

        void UpdateVisual(float dt)
        {
            Quaternion target = Quaternion.LookRotation(_facing, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, 1f - Mathf.Exp(-_turnSpeed * dt));

            _squash = Mathf.Lerp(_squash, 1f, 1f - Mathf.Exp(-9f * dt));
            float stretchY = _squash;
            float stretchXZ = 1f / Mathf.Sqrt(Mathf.Max(_squash, 0.05f));
            _visual.localScale = new Vector3(stretchXZ, stretchY, stretchXZ);

            if (_visor != null)
            {
                float glow = MaterialLibrary.Quantize(1.6f + (IsGrounded ? 0f : 1.4f) + SpeedNormalized * 0.8f, 8);
                _visorRenderer.sharedMaterial = MaterialLibrary.Glowing(Palette.Accent, glow);
            }
        }

        /// <summary>Мгновенное перемещение. Гасит инерцию, чтобы петля начиналась одинаково.</summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            bool wasEnabled = _cc.enabled;
            _cc.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            if (wasEnabled) _cc.enabled = true;

            _velocity = Vector3.zero;
            _facing = rotation * Vector3.forward;
            _simulationTime = 0f;
            _lastGroundedTime = 0f;
            _jumpRequestedTime = -99f;
            _squash = 1f;
            IsAlive = true;
        }

        public void Revive(Vector3 position, Quaternion rotation)
        {
            Teleport(position, rotation);
            IsAlive = true;
            if (_visual != null) _visual.gameObject.SetActive(true);
            if (_cc != null) _cc.enabled = true;
        }

        public void Kill()
        {
            if (!IsAlive) return;

            IsAlive = false;
            if (_cc != null) _cc.enabled = false;
            _velocity = Vector3.zero;

            Died?.Invoke();
        }
    }
}