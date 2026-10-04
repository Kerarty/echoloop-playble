using UnityEngine;

namespace Playble.EchoLoop
{
    /// <summary>
    /// Призрак прошлой петли. Проигрывает записанные позы напрямую, а не
    /// пересимулирует физику: так эхо гарантированно воспроизводит ровно то,
    /// что сделал игрок, и не «расползается» из-за накопления ошибок.
    /// При этом остаётся твёрдым — на него можно встать, а плиты под ним работают.
    /// </summary>
    [DisallowMultipleComponent]
    public class EchoClone : LoopActor
    {
        public LoopTape Tape { get; private set; }
        public int Age { get; private set; }
        public int LastTick { get; private set; } = -1;

        Transform _shell;
        Transform _core;
        MeshRenderer[] _renderers;
        float _bobPhase;

        public static EchoClone Create(Transform parent, LoopTape tape, int age, Vector3 startPosition, Quaternion startRotation)
        {
            var go = new GameObject("Echo_" + age);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(startPosition, startRotation);

            // Габариты совпадают с габаритами игрока: на эхо можно встать,
            // а расчёт высоты прыжка в дизайне уровней не разъезжается.
            var cc = go.AddComponent<CapsuleCollider>();
            cc.height = ActorHeight;
            cc.radius = ActorRadius;
            cc.center = new Vector3(0f, ActorHeight * 0.5f, 0f);
            cc.direction = 1;

            var echo = go.AddComponent<EchoClone>();
            echo.Tape = tape;
            echo.Age = age;
            echo.BuildVisual();

            echo.Place(0);
            return echo;
        }

        void BuildVisual()
        {
            // С возрастом эхо тускнеют: ближайшая петля — самая яркая подсказка.
            Color tint = Palette.Lerp(Palette.Accent, Palette.Dim(Palette.Accent, 0.55f), Mathf.Clamp01(age / 3f));
            float alpha = Mathf.Lerp(0.42f, 0.16f, Mathf.Clamp01(age / 3f));

            _shell = Primitives.Capsule(transform, "Shell", new Vector3(0f, ActorHeight * 0.5f, 0f),
                ActorHeight, ActorRadius, MaterialLibrary.Ghost(tint, alpha)).transform;

            _core = Primitives.Capsule(transform, "Core", new Vector3(0f, ActorHeight * 0.5f, 0f),
                ActorHeight * 0.42f, ActorRadius * 0.42f, MaterialLibrary.Ghost(tint, Mathf.Min(0.85f, alpha + 0.4f))).transform;

            _renderers = GetComponentsInChildren<MeshRenderer>(true);
            _bobPhase = age * 1.7f;
        }

        /// <summary>Поставить эхо в позу, записанную на такте <paramref name="tick"/>.</summary>
        public void Place(int tick)
        {
            if (Tape == null) return;
            LastTick = tick;
            transform.SetPositionAndRotation(Tape.PositionAt(tick), Tape.RotationAt(tick));
        }

        void LateUpdate()
        {
            if (_shell == null) return;

            // Лёгкое «дыхание» корпуса, чтобы призрак не выглядел статичной болванкой.
            float w = Mathf.Sin((Time.time + _bobPhase) * 3.1f);
            _shell.localScale = new Vector3(
                ActorRadius * 2f * (1f + w * 0.05f),
                ActorHeight * (1f + w * 0.02f),
                ActorRadius * 2f * (1f + w * 0.05f));
            _core.localRotation = Quaternion.Euler(0f, Time.time * 120f, 0f);
        }
    }
}