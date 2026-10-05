using NUnit.Framework;
using Playble.EchoLoop;

namespace Playble.Tests
{
    /// <summary>
    /// Тесты «всё по правилам», а не «как задумано автором». Именно они ловят
    /// самые частые ошибки в данных уровня: плиту в стене, выход внутри блока,
    /// обход двери сбоку и нехватку времени на петлю.
    /// </summary>
    [TestFixture]
    public class LevelValidationTests
    {
        const float ActorBody = 0.4f;   // половина тела для проверки «внутри блока»
        const float GroundY = 0f;

        static IEnumerable<LevelDefinition> Levels()
        {
            for (int i = 0; i < LevelDatabase.Count; i++) yield return LevelDatabase.Get(i);
        }

        // ---------------------------------------------------------------
        // Базовые проверки
        // ---------------------------------------------------------------

        [Test]
        public void Database_IsNotEmpty()
        {
            Assert.That(LevelDatabase.Count, Is.GreaterThanOrEqualTo(8), "в базе должно быть минимум 8 уровней");
        }

        [Test]
        public void EveryLevel_HasNameHintAndShards()
        {
            foreach (var level in Levels())
            {
                Assert.That(level.Name, Is.Not.Empty, $"{level.Name}: пустое имя");
                Assert.That(level.Hint, Is.Not.Empty, $"{level.Name}: нет подсказки");
                Assert.That(level.ShardCount, Is.GreaterThanOrEqualTo(1), $"{level.Name}: нет осколков");
                Assert.That(level.LoopSeconds, Is.InRange(4f, 20f), $"{level.Name}: странная длительность петли");
                Assert.That(level.MaxEchoes, Is.InRange(0, 6), $"{level.Name}: странное число эхо");
            }
        }

        [Test]
        public void EveryLevel_EchoesAreEnoughForParLoops()
        {
            foreach (var level in Levels())
            {
                Assert.That(level.ParLoops, Is.GreaterThanOrEqualTo(1), $"{level.Name}: par меньше 1");
                // Число эхо не может превышать общее число «инструкций» уровня:
                // иначе лишние тела просто дублируются.
                int setupPoints = level.Plates.Count + 1;
                Assert.That(level.MaxEchoes, Is.LessThanOrEqualTo(setupPoints),
                    $"{level.Name}: эхо больше, чем есть точек для установки");
            }
        }

        // ---------------------------------------------------------------
        // Геометрия: ничто не должно быть «внутри стены»
        // ---------------------------------------------------------------

        [Test]
        public void EveryLevel_PlayerStartStandsOnSolidGround()
        {
            foreach (var level in Levels())
            {
                Vector3 start = level.PlayerStart;

                Assert.That(InsideSolid(level, start + Vector3.up * ActorBody), Is.False,
                    $"{level.Name}: игрок стартует внутри блока");

                Assert.That(InsideHazard(level, start + Vector3.up * ActorBody), Is.False,
                    $"{level.Name}: игрок стартует в опасной зоне");

                float surface = HighestSurface(level, start.x, start.z, start.y + 0.1f);
                Assert.That(surface, Is.EqualTo(start.y).Within(1.5f),
                    $"{level.Name}: под стартом игрока нет площадки (поверхность на {surface}, игрок на {start.y})");
            }
        }

        [Test]
        public void EveryLevel_PlatesAreFreeAndSupported()
        {
            foreach (var level in Levels())
            {
                for (int i = 0; i < level.Plates.Count; i++)
                {
                    var plate = level.Plates[i];
                    Vector3 c = plate.Center;

                    Assert.That(InsideSolid(level, c), Is.False,
                        $"{level.Name}: плита {i} внутри блока");

                    Assert.That(InsideHazard(level, c), Is.False,
                        $"{level.Name}: плита {i} в опасной зоне");

                    float surface = HighestSurface(level, c.x, c.z, c.y + 0.05f);
                    Assert.That(surface, Is.EqualTo(c.y - 0.25f).Within(0.45f),
                        $"{level.Name}: под плитой {i} нет пола (пол на {surface}, плита на {c.y})");
                }
            }
        }

        [Test]
        public void EveryLevel_ShardsAreReachableAndNotBuried()
        {
            foreach (var level in Levels())
            {
                for (int i = 0; i < level.Shards.Count; i++)
                {
                    Vector3 p = level.Shards[i].Position;

                    Assert.That(InsideSolid(level, p), Is.False,
                        $"{level.Name}: осколок {i} внутри блока");

                    Assert.That(InsideHazard(level, p), Is.False,
                        $"{level.Name}: осколок {i} в опасной зоне");

                    // Осколок должен висеть над опорой: если под ним ничего,
                    // значит он висит в пустоте и взять его нечем.
                    float surface = HighestSurface(level, p.x, p.z, p.y);
                    Assert.That(surface, Is.Not.EqualTo(float.NegativeInfinity).And.Not.EqualTo(float.PositiveInfinity),
                        $"{level.Name}: под осколком {i} вообще нет поверхности");

                    float height = p.y - surface;
                    Assert.That(height, Is.InRange(0.5f, 1.2f),
                        $"{level.Name}: осколок {i} висит на высоте {height} от опоры — до него не дотянуться");
                }
            }
        }

        [Test]
        public void EveryLevel_ExitIsFree()
        {
            foreach (var level in Levels())
            {
                Vector3 p = level.ExitPosition;

                Assert.That(InsideSolid(level, p + Vector3.up * 0.4f), Is.False,
                    $"{level.Name}: выход внутри блока");

                Assert.That(InsideHazard(level, p + Vector3.up * 0.4f), Is.False,
                    $"{level.Name}: выход в опасной зоне");

                float surface = HighestSurface(level, p.x, p.z, p.y + 0.1f);
                Assert.That(surface, Is.EqualTo(p.y).Within(1.5f),
                    $"{level.Name}: выход висит в воздухе");
            }
        }

        // ---------------------------------------------------------------
        // Главная проверка дизайна: дверь нельзя обойти
        // ---------------------------------------------------------------

        [Test]
        public void EveryLevel_GateCannotBeWalkedAround()
        {
            foreach (var level in Levels())
            {
                for (int i = 0; i < level.Gates.Count; i++)
                {
                    var gate = level.Gates[i];

                    float[] span = GroundSpan(level);
                    Assert.That(IsSealed(level, gate, span[0], span[1]), Is.True,
                        $"{level.Name}: врата {i} можно обойти сбоку — стена не закрывает проём");
                }
            }
        }

        // ---------------------------------------------------------------
        // Тайминг: должно хватать времени на заход
        // ---------------------------------------------------------------

        [Test]
        public void EveryLevel_LoopIsLongEnoughForFinalRun()
        {
            foreach (var level in Levels())
            {
                float distance = PathLength(level, level.PlayerStart, ShardPositions(level), level.ExitPosition);
                float budget = PlayerController.MoveSpeed * level.LoopSeconds * 0.55f;

                Assert.That(distance, Is.LessThan(budget),
                    $"{level.Name}: финальный заход {distance:0.0} м не влезает в {level.LoopSeconds:0.#} с (нужно < {budget:0.0} м)");
            }
        }

        [Test]
        public void EveryLevel_LoopIsLongEnoughToPlaceEchoes()
        {
            foreach (var level in Levels())
            {
                float distance = PathLength(level, level.PlayerStart, PlatePositions(level), level.PlayerStart);
                float budget = PlayerController.MoveSpeed * level.LoopSeconds * 0.55f;

                Assert.That(distance, Is.LessThan(budget),
                    $"{level.Name}: расстановка эхо занимает {distance:0.0} м, а петля вмещает {budget:0.0} м");
            }
        }

        // ---------------------------------------------------------------
        // Опасности
        // ---------------------------------------------------------------

        [Test]
        public void EveryLevel_HazardIsVisibleAndNotFloating()
        {
            foreach (var level in Levels())
            {
                for (int i = 0; i < level.Hazards.Count; i++)
                {
                    var h = level.Hazards[i];
                    Vector3 top = h.Center + Vector3.up * (h.Size.y * 0.5f);

                    float surface = HighestSurface(level, h.Center.x, h.Center.z, top.y);
                    Assert.That(top.y, Is.GreaterThanOrEqualTo(surface - 0.7f),
                        $"{level.Name}: опасность {i} утоплена в полу и её не видно");
                }
            }
        }

        // ===============================================================
        // Вспомогательная геометрия
        // ===============================================================

        internal static bool InsideSolid(LevelDefinition level, Vector3 p, float margin = 0f)
        {
            foreach (var b in level.Blocks)
            {
                if (Mathf.Abs(p.x - b.Center.x) > b.Size.x * 0.5f + margin) continue;
                if (Mathf.Abs(p.y - b.Center.y) > b.Size.y * 0.5f + margin) continue;
                if (Mathf.Abs(p.z - b.Center.z) > b.Size.z * 0.5f + margin) continue;
                return true;
            }
            return false;
        }

        internal static bool InsideHazard(LevelDefinition level, Vector3 p, float margin = 0f)
        {
            foreach (var h in level.Hazards)
            {
                if (Mathf.Abs(p.x - h.Center.x) > h.Size.x * 0.5f + margin) continue;
                if (Mathf.Abs(p.y - h.Center.y) > h.Size.y * 0.5f + margin) continue;
                if (Mathf.Abs(p.z - h.Center.z) > h.Size.z * 0.5f + margin) continue;
                return true;
            }
            return false;
        }

        /// <summary>Наивысшая поверхность блока под точкой, не выше потолка.</summary>
        internal static float HighestSurface(LevelDefinition level, float x, float z, float ceilingY)
        {
            float best = float.NegativeInfinity;

            foreach (var b in level.Blocks)
            {
                if (Mathf.Abs(x - b.Center.x) > b.Size.x * 0.5f) continue;
                if (Mathf.Abs(z - b.Center.z) > b.Size.z * 0.5f) continue;

                float top = b.Center.y + b.Size.y * 0.5f;
                if (top <= ceilingY && top > best) best = top;
            }

            return best;
        }

        /// <summary>Границы основной площадки уровня по оси Z.</summary>
        static float[] GroundSpan(LevelDefinition level)
        {
            float minZ = float.PositiveInfinity;
            float maxZ = float.NegativeInfinity;

            foreach (var b in level.Blocks)
            {
                float top = b.Center.y + b.Size.y * 0.5f;
                if (Mathf.Abs(top - GroundY) > 0.2f) continue;

                minZ = Mathf.Min(minZ, b.Center.z - b.Size.z * 0.5f);
                maxZ = Mathf.Max(maxZ, b.Center.z + b.Size.z * 0.5f);
            }

            return float.IsInfinity(minZ) ? new[] { -4f, 4f } : new[] { minZ, maxZ };
        }

        /// <summary>
        /// Считает, что стена вокруг врата непрерывна: сами врата плюс статические
        /// блоки в той же плоскости должны перекрывать всю ширину площадки.
        /// </summary>
        static bool IsSealed(LevelDefinition level, GateDef gate, float minZ, float maxZ)
        {
            var spans = new System.Collections.Generic.List<Vector2>();
            spans.Add(new Vector2(gate.Center.z - gate.Size.z * 0.5f, gate.Center.z + gate.Size.z * 0.5f));

            foreach (var b in level.Blocks)
            {
                // Блок считается частью стены, если он в той же плоскости по X и Y.
                if (Mathf.Abs(b.Center.x - gate.Center.x) > 1.6f) continue;
                if (Mathf.Abs(b.Center.y - gate.Center.y) > b.Size.y * 0.5f + 2.4f) continue;

                spans.Add(new Vector2(b.Center.z - b.Size.z * 0.5f, b.Center.z + b.Size.z * 0.5f));
            }

            spans.Sort((a, b) => a.x.CompareTo(b.x));

            float coveredTo = spans[0].y;
            if (spans[0].x > minZ + 0.2f) return false;   // есть щель слева

            for (int i = 1; i < spans.Count; i++)
            {
                if (spans[i].x > coveredTo + 0.2f) return false;   // дыра в стене
                coveredTo = Mathf.Max(coveredTo, spans[i].y);
            }

            return coveredTo >= maxZ - 0.2f;
        }

        static Vector3[] ShardPositions(LevelDefinition level)
        {
            var result = new Vector3[level.Shards.Count];
            for (int i = 0; i < result.Length; i++) result[i] = level.Shards[i].Position;
            return result;
        }

        static Vector3[] PlatePositions(LevelDefinition level)
        {
            var result = new Vector3[level.Plates.Count];
            for (int i = 0; i < result.Length; i++) result[i] = level.Plates[i].Center;
            return result;
        }

        /// <summary>Длина маршрута в порядке: старт → точки → финиш (жадный порядок).</summary>
        static float PathLength(LevelDefinition level, Vector3 start, Vector3[] waypoints, Vector3 finish)
        {
            var remaining = new System.Collections.Generic.List<Vector3>(waypoints);
            float total = 0f;
            var current = start;

            while (remaining.Count > 0)
            {
                int best = 0;
                float bestDistance = float.PositiveInfinity;

                for (int i = 0; i < remaining.Count; i++)
                {
                    // Расстояние по земле: вертикальная составляющая не бесплатная.
                    float d = Distance(current, remaining[i]);
                    if (d >= bestDistance) continue;
                    bestDistance = d;
                    best = i;
                }

                total += bestDistance;
                current = remaining[best];
                remaining.RemoveAt(best);
            }

            return total + Distance(current, finish);
        }

        static float Distance(Vector3 a, Vector3 b)
        {
            Vector2 flat = new Vector2(a.x - b.x, a.z - b.z);
            float rise = Mathf.Abs(a.y - b.y);
            return flat.magnitude + rise * 1.6f;   // подъём стоит примерно в 1.6 раза дороже
        }
    }
}