using System;
using UnityEngine;

namespace Playble.EchoLoop
{
    /// <summary>
    /// Единственный источник времени в игре. Все системы (запись ввода, воспроизведение
    /// эхо, сброс мира) привязаны к целочисленному такту, а не к плавающему времени кадра.
    /// Это делает петлю воспроизводимой и позволяет обойтись без PhysX-детерминизма.
    /// </summary>
    [DisallowMultipleComponent]
    public class LoopClock : MonoBehaviour
    {
        public const float TicksPerSecond = 60f;

        [SerializeField, Range(3f, 20f)] float _loopSeconds = 8f;

        int _tick;
        int _tickCount;
        int _loopIndex;
        bool _running;

        /// <summary>Тик: (номер тика).</summary>
        public event Action<int> Ticked;

        /// <summary>Петля завершилась естественным образом (таймер вышел).</summary>
        public event Action LoopExpired;

        /// <summary>Петля начинается заново: (номер петли).</summary>
        public event Action<int> LoopRestarting;

        public int Tick => _tick;
        public int TickCount => _tickCount;
        public int LoopIndex => _loopIndex;
        public float LoopSeconds => _loopSeconds;
        public bool Running => _running;

        public float Normalized => _tickCount > 0 ? Mathf.Clamp01((float)_tick / _tickCount) : 0f;
        public float RemainingSeconds => (_tickCount - _tick) / TicksPerSecond;

        void Awake()
        {
            Time.fixedDeltaTime = 1f / TicksPerSecond;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
        }

        public void Configure(float loopSeconds)
        {
            _loopSeconds = Mathf.Clamp(loopSeconds, 3f, 20f);
            _tickCount = Mathf.RoundToInt(_loopSeconds * TicksPerSecond);
        }

        public void StartFirstLoop()
        {
            _loopIndex = 0;
            _tick = 0;
            _running = true;
            LoopRestarting?.Invoke(_loopIndex);
        }

        /// <summary>Экстренный перезапуск: смерть игрока или ручной сброс.</summary>
        public void Restart()
        {
            _running = false;
            LoopExpired?.Invoke();
        }

        /// <summary>Освободить таймер, чтобы обновить состояние мира без следующего тика.</summary>
        public void Halt() => _running = false;

        public void Resume() => _running = true;

        /// <summary>Следующая петля. Вызывается один раз после обработки LoopExpired.</summary>
        public void AdvanceLoop()
        {
            _loopIndex++;
            _tick = 0;
            _running = true;
            LoopRestarting?.Invoke(_loopIndex);
        }

        void FixedUpdate()
        {
            if (!_running) return;

            Ticked?.Invoke(_tick);
            _tick++;

            if (_tick >= _tickCount) Restart();
        }
    }
}