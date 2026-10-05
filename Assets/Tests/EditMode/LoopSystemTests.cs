using System.Collections.Generic;
using NUnit.Framework;
using Playble.EchoLoop;
using UnityEngine;

namespace Playble.Tests
{
    /// <summary>Тестовое тело петли: позволяет проверять плиты и врата без физики.</summary>
    public class TestActor : LoopActor
    {
        public void Move(Vector3 position) => transform.position = position;
    }

    /// <summary>
    /// Проверки самой механики: запись петли, архив эхо и правило «плите всё равно,
    /// кто на ней стоит». Это ядро геймплея, поэтому проверяется точечно.
    /// </summary>
    [TestFixture]
    public class LoopSystemTests
    {
        readonly List<GameObject> _spawned = new List<GameObject>();

        GameObject NewObject(string name)
        {
            var go = new GameObject(name);
            _spawned.Add(go);
            return go;
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null) Object.DestroyImmediate(_spawned[i]);
            }
            _spawned.Clear();
        }

        // ---------------------------------------------------------------

        [Test]
        public void LoopTape_StoresPosePerTick()
        {
            var tape = new LoopTape(10);

            tape.Capture(0, new Vector3(1f, 0f, 2f), Quaternion.identity);
            tape.Capture(5, new Vector3(7f, 0f, 9f), Quaternion.Euler(0f, 90f, 0f));

            Assert.That(tape.PositionAt(0), Is.EqualTo(new Vector3(1f, 0f, 2f)));
            Assert.That(tape.PositionAt(5), Is.EqualTo(new Vector3(7f, 0f, 9f)));
        }

        [Test]
        public void LoopTape_ClampsOutOfRangeTicks()
        {
            var tape = new LoopTape(4);
            tape.Capture(0, Vector3.one, Quaternion.identity);
            tape.Capture(99, Vector3.zero, Quaternion.identity);

            Assert.That(tape.Capacity, Is.EqualTo(4));
            Assert.That(tape.PositionAt(-5), Is.EqualTo(Vector3.one), "недопустимый тик должен читаться как нулевой");
            Assert.That(tape.PositionAt(100), Is.EqualTo(Vector3.zero), "тик за пределами ленты игнорируется");
        }

        [Test]
        public void LoopTape_FillsTailFromRecordedCount()
        {
            var tape = new LoopTape(8);
            tape.Capture(0, new Vector3(3f, 0f, 0f), Quaternion.identity);
            tape.Capture(1, new Vector3(6f, 0f, 0f), Quaternion.identity);

            // Смерть на третьем тике: хвост должен остаться в точке гибели,
            // иначе эхо телепортируется в начало координат.
            tape.FillTailFrom(2, new Vector3(6f, 0f, 0f), Quaternion.identity);

            for (int tick = 2; tick < 8; tick++)
            {
                Assert.That(tape.PositionAt(tick), Is.EqualTo(new Vector3(6f, 0f, 0f)),
                    $"тик {tick}: хвост ленты не дописан");
            }
        }

        [Test]
        public void EchoDirector_KeepsOnlyTheNewestLoops()
        {
            var root = NewObject("Root").transform;
            var director = root.gameObject.AddComponent<EchoDirector>();
            director.Configure(2, Vector3.zero, Quaternion.identity);
            director.SetActorRoot(root);

            for (int loop = 0; loop < 5; loop++)
            {
                director.BeginRecording(4);
                director.Capture(0, new Vector3(loop, 0f, 0f), Quaternion.identity);
                director.CommitRecording(Vector3.zero, Quaternion.identity);
            }

            director.RebuildClones();

            Assert.That(director.LiveEchoes, Is.EqualTo(2), "архив должен быть ограничен лимитом эхо");
        }

        [Test]
        public void EchoDirector_EchoReplaysRecordedPose()
        {
            var root = NewObject("Root").transform;
            var director = root.gameObject.AddComponent<EchoDirector>();
            director.Configure(3, new Vector3(-2f, 0f, 0f), Quaternion.identity);
            director.SetActorRoot(root);

            director.BeginRecording(6);
            director.Capture(0, new Vector3(-2f, 0f, 0f), Quaternion.identity);
            director.Capture(3, new Vector3(5f, 0f, 1f), Quaternion.identity);
            director.CommitRecording(new Vector3(5f, 0f, 1f), Quaternion.identity);

            director.RebuildClones();
            Assert.That(director.LiveEchoes, Is.EqualTo(1));

            var actors = new List<LoopActor>();
            director.CollectActors(actors);
            Assert.That(actors.Count, Is.EqualTo(1), "эхо должно участвовать в расчётах петли");

            director.PlayTick(0);
            Assert.That(actors[0].transform.position.x, Is.EqualTo(-2f).Within(0.001f));

            director.PlayTick(3);
            Assert.That(actors[0].transform.position.x, Is.EqualTo(5f).Within(0.001f));
            Assert.That(actors[0].transform.position.z, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void EchoDirector_WipeRemovesMemory()
        {
            var root = NewObject("Root").transform;
            var director = root.gameObject.AddComponent<EchoDirector>();
            director.Configure(3, Vector3.zero, Quaternion.identity);
            director.SetActorRoot(root);

            director.BeginRecording(4);
            director.CommitRecording(Vector3.zero, Quaternion.identity);
            director.RebuildClones();
            Assert.That(director.LiveEchoes, Is.EqualTo(1));

            director.Wipe();
            Assert.That(director.LiveEchoes, Is.EqualTo(0), "рестарт уровня должен стирать всю память");
        }

        // ---------------------------------------------------------------

        [Test]
        public void PressurePlate_IsPressedByAnyActor()
        {
            var root = NewObject("Root").transform;
            var plate = PressurePlate.Create(root, 0, new Vector3(0f, 0.1f, 0f), new Vector3(2.4f, 0.5f, 2.4f));

            var player = NewObject("Player").AddComponent<TestActor>();
            player.Move(new Vector3(9f, 0f, 0f));

            var actors = new List<LoopActor> { player };
            Assert.That(plate.Evaluate(actors), Is.False, "пустая плита не должна считаться нажатой");

            player.Move(new Vector3(0.5f, 0f, 0.2f));
            Assert.That(plate.Evaluate(actors), Is.True);

            player.Move(new Vector3(9f, 0f, 0f));
            Assert.That(plate.Evaluate(actors), Is.False);
        }

        [Test]
        public void Gate_OpensOnlyWhenEveryLinkedPlateIsPressed()
        {
            var root = NewObject("Root").transform;

            var plates = new List<PressurePlate>
            {
                PressurePlate.Create(root, 0, new Vector3(-8f, 0.1f, 0f), new Vector3(2.4f, 0.5f, 2.4f)),
                PressurePlate.Create(root, 1, new Vector3(-8f, 0.1f, 6f), new Vector3(2.4f, 0.5f, 2.4f)),
            };

            var gate = Gate.Create(root, new Vector3(0f, 2.2f, 0f), new Vector3(0.7f, 4.4f, 5.4f),
                new Vector3(0f, 5.2f, 0f), new[] { 0, 1 });

            var first = NewObject("Echo1").AddComponent<TestActor>();
            var second = NewObject("Echo2").AddComponent<TestActor>();
            var actors = new List<LoopActor> { first, second };

            first.Move(new Vector3(-8f, 0f, 0f));
            second.Move(new Vector3(-8f, 0f, 6f));

            plates[0].Evaluate(actors);
            plates[1].Evaluate(actors);
            Assert.That(gate.Evaluate(plates), Is.True, "обе плиты нажаты — врата открыты");

            second.Move(new Vector3(-8f, 0f, 9f));
            plates[0].Evaluate(actors);
            plates[1].Evaluate(actors);
            Assert.That(gate.Evaluate(plates), Is.False, "одна плита из двух — врата держатся");

            // Эхо, ушедшее с петли, больше не держит плиту.
            first.Move(new Vector3(-8f, 0f, 9f));
            plates[0].Evaluate(actors);
            plates[1].Evaluate(actors);
            Assert.That(gate.Evaluate(plates), Is.False);
        }

        [Test]
        public void Gate_WithoutPlatesStaysClosed()
        {
            var root = NewObject("Root").transform;
            var gate = Gate.Create(root, Vector3.zero, Vector3.one, Vector3.up, null);

            Assert.That(gate.Evaluate(new List<PressurePlate>()), Is.False,
                "врата без плит не должны открываться «сами»");
        }

        // ---------------------------------------------------------------

        [Test]
        public void Shard_CanBeCollectedOnceAndResetsWithLevel()
        {
            var root = NewObject("Root").transform;
            var shard = Shard.Create(root, new Vector3(0f, 1f, 0f), 0);

            Assert.That(shard.TryCollect(new Vector3(0.4f, 1f, 0f)), Is.True);
            Assert.That(shard.IsCollected, Is.True);

            Assert.That(shard.TryCollect(new Vector3(0.4f, 1f, 0f)), Is.False, "второй раз осколок не берётся");

            shard.ResetState();
            Assert.That(shard.IsCollected, Is.False, "рестарт уровня возвращает осколок");
            Assert.That(shard.TryCollect(new Vector3(0.4f, 1f, 0f)), Is.True);
        }

        [Test]
        public void Shard_IgnoresActorsOutsidePickupRadius()
        {
            var root = NewObject("Root").transform;
            var shard = Shard.Create(root, Vector3.zero, 0);

            Assert.That(shard.TryCollect(new Vector3(Shard.PickupRadius * 1.6f, 0f, 0f)), Is.False);
            Assert.That(shard.IsCollected, Is.False);
        }

        // ---------------------------------------------------------------

        [Test]
        public void ExitCore_IsLockedUntilArmed()
        {
            var root = NewObject("Root").transform;
            var exit = ExitCore.Create(root, Vector3.zero, 2.4f);

            Assert.That(exit.Contains(Vector3.zero), Is.False, "выход заперт по умолчанию");

            exit.SetArmed(true);
            Assert.That(exit.Contains(Vector3.zero), Is.True);
            Assert.That(exit.Contains(new Vector3(40f, 0f, 0f)), Is.False, "слишком далеко — не выход");
        }

        [Test]
        public void HazardZone_DetectsActorsOnlyInside()
        {
            var root = NewObject("Root").transform;
            var hazard = HazardZone.Create(root, new Vector3(0f, 0.6f, 0f), new Vector3(6f, 1.6f, 12f));

            var actor = NewObject("Actor").AddComponent<TestActor>();

            actor.Move(new Vector3(0f, 0f, 0f));
            Assert.That(hazard.Contains(actor.Center, actor.Size), Is.True);

            actor.Move(new Vector3(9f, 0f, 0f));
            Assert.That(hazard.Contains(actor.Center, actor.Size), Is.False);
        }

        // ---------------------------------------------------------------

        [Test]
        public void LevelRuntime_BuildsEveryPieceItWasGiven()
        {
            var root = NewObject("Root").transform;
            var level = root.gameObject.AddComponent<LevelRuntime>();
            var definition = LevelDatabase.Get(1);

            level.Build(definition, root);

            Assert.That(level.ShardsTotal, Is.EqualTo(definition.ShardCount));
            Assert.That(level.Plates.Count, Is.EqualTo(definition.Plates.Count));
            Assert.That(level.AllShardsCollected, Is.False, "выход не должен быть открыт сразу");

            level.ResetEverything();
            Assert.That(level.ShardsCollected, Is.Zero);
        }
    }
}