using System.Collections.Generic;
using UnityEngine;

namespace Playble.EchoLoop
{
    /// <summary>
    /// Хранит архив записей и управляет «толпой» эхо. На каждой петле прошлое
    /// переносится в архив, а новое тело снова пишется с нуля — так память
    /// автоматически ограничена несколькими последними петлями.
    /// </summary>
    [DisallowMultipleComponent]
    public class EchoDirector : MonoBehaviour
    {
        [SerializeField, Range(0, 6)] int _maxEchoes = 3;

        readonly List<LoopTape> _history = new List<LoopTape>();
        readonly List<EchoClone> _clones = new List<EchoClone>();

        LoopTape _recording;
        Transform _actorRoot;
        Vector3 _spawnPoint;
        Quaternion _spawnRotation;
        int _recordedCount;

        public int MaxEchoes => _maxEchoes;
        public int LiveEchoes => _clones.Count;
        public LoopTape Current => _recording;

        public void Configure(int maxEchoes, Vector3 spawnPoint, Quaternion spawnRotation)
        {
            _maxEchoes = Mathf.Clamp(maxEchoes, 0, 6);
            _spawnPoint = spawnPoint;
            _spawnRotation = spawnRotation;
        }

        public void SetActorRoot(Transform root) => _actorRoot = root;

        /// <summary>Начать писать новую петлю с чистой ленты.</summary>
        public void BeginRecording(int tickCount)
        {
            _recording = new LoopTape(tickCount);
            _recordedCount = 0;
        }

        public void Capture(int tick, Vector3 position, Quaternion rotation)
        {
            if (_recording == null) return;
            _recording.Capture(tick, position, rotation);
            _recordedCount = Mathf.Max(_recordedCount, tick + 1);
        }

        /// <summary>
        /// Отправить записанную петлю в архив. Хвост дописывается последней позой,
        /// чтобы прерванная смертью петля тоже была корректной записью.
        /// </summary>
        public void CommitRecording(Vector3 tailPosition, Quaternion tailRotation)
        {
            if (_recording == null) return;

            _recording.FillTailFrom(_recordedCount, tailPosition, tailRotation);

            _history.Insert(0, _recording);
            while (_history.Count > _maxEchoes) _history.RemoveAt(_history.Count - 1);
            _recording = null;
        }

        /// <summary>Пересобрать эхо под текущую петлю: архив проигрывается с нулевого тика.</summary>
        public void RebuildClones()
        {
            DespawnClones();

            if (_actorRoot == null) return;

            int count = Mathf.Min(_history.Count, _maxEchoes);
            for (int i = 0; i < count; i++)
            {
                _clones.Add(EchoClone.Create(_actorRoot, _history[i], i, _spawnPoint, _spawnRotation));
            }
        }

        public void PlayTick(int tick)
        {
            for (int i = 0; i < _clones.Count; i++)
            {
                _clones[i].Place(tick);
            }
        }

        /// <summary>Добавить всех живых эхо в список тел петли (для плит и опасностей).</summary>
        public void CollectActors(List<LoopActor> into)
        {
            for (int i = 0; i < _clones.Count; i++)
            {
                if (_clones[i] != null) into.Add(_clones[i]);
            }
        }

        public void DespawnClones()
        {
            for (int i = 0; i < _clones.Count; i++)
            {
                if (_clones[i] == null) continue;
                if (Application.isPlaying) Destroy(_clones[i].gameObject);
                else DestroyImmediate(_clones[i].gameObject);
            }
            _clones.Clear();
        }

        /// <summary>Полный сброс: вместе со всеми воспоминаниями. Используется при рестарте уровня.</summary>
        public void Wipe()
        {
            DespawnClones();
            _history.Clear();
            _recording = null;
        }
    }
}