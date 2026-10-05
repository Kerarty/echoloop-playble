using UnityEngine;

namespace Playble.EchoLoop
{
    /// <summary>
    /// Камера с фиксированным ракурсом и мягким следованием. Ротор не вращается:
    /// это делает управление предсказуемым (W — всегда «вверх по экрану») и
    /// упрощает чтение пространства в пазле, где важнее геометрия, чем обзор.
    /// </summary>
    [DisallowMultipleComponent]
    public class CameraRig : MonoBehaviour
    {
        [SerializeField] Vector3 _offset = new Vector3(0f, 18f, -16f);
        [SerializeField] float _smoothTime = 0.16f;
        [SerializeField] Vector3 _focusHeight = new Vector3(0f, 1.2f, 1.2f);
        [SerializeField] float _shakeDecay = 5f;

        Transform _target;
        Vector3 _velocity;
        Vector3 _bias;
        float _biasWeight = 0.28f;
        float _shakeAmount;

        public void SetTarget(Transform target) => _target = target;

        /// <summary>
        /// Точка, к которой камера стремится вместе с игроком. Лёгкое смещение
        /// к центру уровня не даёт игроку «уезжать» в угол кадра на широких картах.
        /// </summary>
        public void SetBias(Vector3 point) => _bias = point;

        public void SetBiasWeight(float weight) => _biasWeight = Mathf.Clamp01(weight);

        Vector3 Focus => (_target == null ? _bias : Vector3.Lerp(_target.position, _bias, _biasWeight)) + _focusHeight;

        public void Snap()
        {
            if (_target == null) return;

            _velocity = Vector3.zero;
            Vector3 focus = Focus;
            transform.position = focus + _offset;
            transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
        }

        public void Shake(float amount) => _shakeAmount = Mathf.Max(_shakeAmount, amount);

        void LateUpdate()
        {
            if (_target == null) return;

            Vector3 focus = Focus;
            Vector3 desired = focus + _offset;

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _velocity, _smoothTime);
            transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);

            if (_shakeAmount > 0.001f)
            {
                _shakeAmount = Mathf.Lerp(_shakeAmount, 0f, Time.deltaTime * _shakeDecay);
                float s = _shakeAmount;
                transform.position += new Vector3(
                    (Mathf.PerlinNoise(Time.time * 34f, 0f) - 0.5f) * s,
                    (Mathf.PerlinNoise(0f, Time.time * 34f) - 0.5f) * s,
                    0f);
            }
        }
    }
}