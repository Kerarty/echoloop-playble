using System.Collections.Generic;
using UnityEngine;

namespace Playble.EchoLoop
{
    /// <summary>
    /// Все уровни игры. Уровень — это данные, поэтому здесь нет ни одного
    /// GameObject: только координаты опор, плит, врат и осколков.
    ///
    /// Правила проектирования уровней, на которых держатся все головоломки:
    ///   * точка, где должен встать эхо, всегда достижима обычным ходом
    ///     (иначе получился бы замкнутый круг «нужно эхо, чтобы поставить эхо»);
    ///   * то, что эхо открывает, недостижимо без него;
    ///   * высота прыжка — 2.4 м, высота эхо — 1.8 м, вместе это 4.2 м.
    /// </summary>
    public static class LevelDatabase
    {
        static readonly List<LevelDefinition> _levels = BuildAll();

        public static IReadOnlyList<LevelDefinition> All => _levels;
        public static int Count => _levels.Count;

        public static LevelDefinition Get(int index)
        {
            if (_levels.Count == 0) return null;
            return _levels[MathfClamp(index, 0, _levels.Count - 1)];
        }

        static int MathfClamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);

        static List<LevelDefinition> BuildAll()
        {
            return new List<LevelDefinition>
            {
                Level01_Awakening(),
                Level02_Echo(),
                Level03_Footing(),
                Level04_TwoDoors(),
                Level05_Rift(),
                Level06_Staircase(),
                Level07_Mechanism(),
                Level08_Heart(),
            };
        }

        // ==================================================================
        // 1. Обучение: движение, прыжок, осколки переживают петлю
        // ==================================================================
        static LevelDefinition Level01_Awakening()
        {
            var l = new LevelDefinition(
                "Пробуждение",
                "цикл 01",
                "Осколки не исчезают после перезапуска. Собери все и коснись выхода.");

            l.LoopSeconds = 8f;
            l.MaxEchoes = 2;
            l.ParLoops = 1;

            l.PlayerStart = new Vector3(-7f, 0.1f, -4f);
            l.CameraFocus = new Vector3(0f, 0.5f, 1f);
            l.ExitPosition = new Vector3(7f, 0f, -1f);
            l.ExitRadius = 2.6f;

            l.Pedestal(0f, 1f, 22f, 14f, 0f);          // земля
            l.Pedestal(-4f, 2f, 4f, 4f, 1.0f, 3f);    // первая ступень
            l.Pedestal(2f, 4.5f, 4f, 4f, 1.8f, 4f);   // вторая ступень
            l.Block(7f, 1.5f, 4.5f, 3f, 3f, 3f, BlockTint.StructureDark);

            l.Shard(-4f, 1.9f, 2f);
            l.Shard(2f, 2.7f, 4.5f);

            return l;
        }

        // ==================================================================
        // 2. Эхо удерживает плиту, игрок проходит в открытые врата
        // ==================================================================
        static LevelDefinition Level02_Echo()
        {
            var l = new LevelDefinition(
                "Эхо",
                "цикл 02",
                "Встань на плиту и дождись конца цикла. В следующем проходе её удержит твоё эхо.");

            l.LoopSeconds = 8f;
            l.MaxEchoes = 2;
            l.ParLoops = 2;

            l.PlayerStart = new Vector3(-9f, 0.1f, -3f);
            l.CameraFocus = new Vector3(0f, 0.5f, 0f);
            l.ExitPosition = new Vector3(9f, 0f, -3f);
            l.ExitRadius = 2.6f;

            l.Pedestal(0f, 0f, 24f, 14f, 0f);

            l.Plate(-2f, 0.1f, 0f);
            l.Gate(4f, 2.2f, 0f, 0.7f, 4.4f, 8f, new Vector3(0f, 5.2f, 0f), 0);
            l.Block(4f, 2f, -5.8f, 1f, 4f, 2.6f);
            l.Block(4f, 2f, 5.8f, 1f, 4f, 2.6f);

            l.Pedestal(8f, 3f, 4f, 4f, 1.0f, 3f);
            l.Shard(8f, 1.9f, 3f);

            return l;
        }

        // ==================================================================
        // 3. Эхо как платформа: на него можно встать
        // ==================================================================
        static LevelDefinition Level03_Footing()
        {
            var l = new LevelDefinition(
                "Опора",
                "цикл 03",
                "Эхо — твёрдое тело. Оттолкнись от своей головы, стоящей внизу.");

            l.LoopSeconds = 8f;
            l.MaxEchoes = 2;
            l.ParLoops = 2;

            l.PlayerStart = new Vector3(-7f, 0.1f, 0f);
            l.CameraFocus = new Vector3(0f, 1.5f, 1f);
            l.ExitPosition = new Vector3(8f, 0f, -3.5f);
            l.ExitRadius = 2.6f;

            l.Pedestal(0f, 0f, 20f, 12f, 0f);
            l.Pedestal(4f, 2f, 6f, 6f, 3.2f, 3.2f);   // уступ, недостижимый с земли
            l.Block(4f, 1.5f, 5.6f, 6f, 3f, 1.2f, BlockTint.StructureDark);

            l.Shard(4f, 4.1f, 2f);

            return l;
        }

        // ==================================================================
        // 4. Две двери — нужны два эхо
        // ==================================================================
        static LevelDefinition Level04_TwoDoors()
        {
            var l = new LevelDefinition(
                "Две двери",
                "цикл 04",
                "Каждая плита — отдельное эхо. Сначала собери «дирижёра», потом исполни.");

            l.LoopSeconds = 9f;
            l.MaxEchoes = 3;
            l.ParLoops = 3;

            l.PlayerStart = new Vector3(-10f, 0.1f, 0f);
            l.CameraFocus = new Vector3(0f, 0.5f, 0f);
            l.ExitPosition = new Vector3(9f, 0f, 0f);
            l.ExitRadius = 2.6f;

            l.Pedestal(0f, 0f, 26f, 14f, 0f);

            l.Plate(-5f, 0.1f, -3f);
            l.Plate(-5f, 0.1f, 3f);

            l.Gate(0f, 2.2f, -3.2f, 0.7f, 4.4f, 5.4f, new Vector3(0f, 5.2f, 0f), 0);
            l.Gate(0f, 2.2f, 3.2f, 0.7f, 4.4f, 5.4f, new Vector3(0f, 5.2f, 0f), 1);
            l.Block(0f, 2f, 0f, 1f, 4f, 1f);
            l.Block(0f, 2f, -6.4f, 1f, 4f, 2.2f);
            l.Block(0f, 2f, 6.4f, 1f, 4f, 2.2f);

            l.Pedestal(4f, -3f, 3.5f, 3.5f, 1.0f, 3f);
            l.Pedestal(4f, 3f, 3.5f, 3.5f, 1.0f, 3f);
            l.Shard(4f, 1.9f, -3f);
            l.Shard(4f, 1.9f, 3f);

            return l;
        }

        // ==================================================================
        // 5. Разлом: опасность в пропасти, эхо — трамплин
        // ==================================================================
        static LevelDefinition Level05_Rift()
        {
            var l = new LevelDefinition(
                "Разлом",
                "цикл 05",
                "Красное убивает мгновенно. Эхо встанет на ступень и останется там навсегда.");

            l.LoopSeconds = 9f;
            l.MaxEchoes = 2;
            l.ParLoops = 2;

            l.PlayerStart = new Vector3(-12f, 0.1f, 0f);
            l.CameraFocus = new Vector3(-1f, 1.5f, 0f);
            l.ExitPosition = new Vector3(10f, 0f, 0f);
            l.ExitRadius = 2.6f;

            l.Pedestal(-9f, 0f, 12f, 12f, 0f);
            l.Pedestal(9f, 0f, 12f, 12f, 0f);
            l.Hazard(0f, 0.6f, 0f, 6f, 1.6f, 12f);

            l.Pedestal(-1.8f, 0f, 2.4f, 2.4f, 1.4f, 1.4f);   // ступень: сюда можно встать
            l.Pedestal(1.4f, 0f, 2.2f, 2.2f, 4.6f, 4.6f);    // колонна: только с эхо
            l.Shard(1.4f, 5.5f, 0f);

            return l;
        }

        // ==================================================================
        // 6. Две плиты и опасность внизу — композиция приёмов
        // ==================================================================
        static LevelDefinition Level06_Staircase()
        {
            var l = new LevelDefinition(
                "Лестница",
                "цикл 06",
                "Плиты на разной высоте. Эхо ставятся по порядку — у самой дальней первой.");

            l.LoopSeconds = 9f;
            l.MaxEchoes = 3;
            l.ParLoops = 3;

            l.PlayerStart = new Vector3(-12f, 0.1f, 0f);
            l.CameraFocus = new Vector3(0f, 0.5f, 0f);
            l.ExitPosition = new Vector3(12f, 0f, 3f);
            l.ExitRadius = 2.6f;

            l.Pedestal(0f, 0f, 30f, 14f, 0f);

            l.Plate(-5f, 0.1f, -2f);
            l.Gate(-1f, 2.2f, 0f, 0.7f, 4.4f, 9f, new Vector3(0f, 5.2f, 0f), 0);
            l.Block(-1f, 2f, -5.8f, 1f, 4f, 2.6f);
            l.Block(-1f, 2f, 5.8f, 1f, 4f, 2.6f);

            l.Pedestal(2f, -3f, 3.2f, 3.2f, 1.0f, 3f);
            l.Shard(2f, 1.9f, -3f);

            l.Plate(6f, 0.1f, -5f);
            l.Gate(9f, 2.2f, -3f, 0.7f, 4.4f, 5.4f, new Vector3(0f, 5.2f, 0f), 1);
            l.Block(9f, 2f, 0f, 1f, 4f, 1.2f);
            l.Block(9f, 2f, -6.5f, 1f, 4f, 1.6f);

            l.Pedestal(12f, -3f, 3.2f, 3.2f, 1.0f, 3f);
            l.Shard(12f, 1.9f, -3f);

            return l;
        }

        // ==================================================================
        // 7. Экзамен: эхо-платформа плюс две двери
        // ==================================================================
        static LevelDefinition Level07_Mechanism()
        {
            var l = new LevelDefinition(
                "Механизм",
                "цикл 07",
                "Три эхо, два замка и один осколок наверху. У тебя ровно три петли на подготовку.");

            l.LoopSeconds = 11f;
            l.MaxEchoes = 3;
            l.ParLoops = 4;

            l.PlayerStart = new Vector3(-14f, 0.1f, 0f);
            l.CameraFocus = new Vector3(0f, 1.2f, 0f);
            l.ExitPosition = new Vector3(15f, 0f, 0f);
            l.ExitRadius = 2.6f;

            l.Pedestal(0f, 0f, 32f, 16f, 0f);

            l.Plate(-10f, 0.1f, -3f);
            l.Gate(-6f, 2.2f, -3f, 0.7f, 4.4f, 5.4f, new Vector3(0f, 5.2f, 0f), 0);
            l.Block(-6f, 2f, 0.6f, 1f, 4f, 1.8f);
            l.Block(-6f, 2f, -6.6f, 1f, 4f, 1.8f);

            // Колонна, на которую нельзя запрыгнуть с земли (верх 4.0 м при прыжке 2.4 м).
            l.Pedestal(-1f, 4f, 4f, 4f, 4.0f, 4.0f);
            l.Shard(-1f, 4.9f, 4f);

            l.Plate(4f, 0.1f, 4f);
            l.Gate(8f, 2.2f, 4f, 0.7f, 4.4f, 5.4f, new Vector3(0f, 5.2f, 0f), 1);
            l.Block(8f, 2f, 0.2f, 1f, 4f, 2.2f);
            l.Block(8f, 2f, 7.6f, 1f, 4f, 1.8f);

            l.Pedestal(12f, 4f, 3.2f, 3.2f, 1.0f, 3f);
            l.Shard(12f, 1.9f, 4f);

            l.Hazard(14f, 0.6f, -5f, 3f, 1.6f, 6f);

            return l;
        }

        // ==================================================================
        // 8. Финал: всё сразу, длинная петля
        // ==================================================================
        static LevelDefinition Level08_Heart()
        {
            var l = new LevelDefinition(
                "Сердце цикла",
                "финал",
                "Три плиты, пропасть и три осколка. У тебя ровно три петли, чтобы всё расставить.");

            l.LoopSeconds = 12f;
            l.MaxEchoes = 3;
            l.ParLoops = 4;

            l.PlayerStart = new Vector3(-15f, 0.1f, 0f);
            l.CameraFocus = new Vector3(0f, 1.5f, 0f);
            l.ExitPosition = new Vector3(16f, 0f, 0f);
            l.ExitRadius = 2.8f;

            l.Pedestal(-13f, 0f, 14f, 16f, 0f);          // западная площадка
            l.Pedestal(9f, 0f, 18f, 16f, 0f);           // восточная площадка
            l.Hazard(-2f, 0.6f, 0f, 10f, 1.6f, 16f);   // разлом между ними

            l.Pedestal(-5.5f, 0f, 2.4f, 2.4f, 1.4f, 1.4f);   // ступень в разломе
            l.Pedestal(-1.4f, 0f, 2.2f, 2.2f, 4.4f, 4.4f);   // колонна с осколком

            l.Plate(-11f, 0.1f, -5f);
            l.Gate(-7f, 2.2f, -5f, 0.7f, 4.4f, 5.4f, new Vector3(0f, 5.2f, 0f), 0);
            l.Block(-7f, 2f, -1.6f, 1f, 4f, 1.8f);
            l.Block(-7f, 2f, -7.4f, 1f, 4f, 1.8f);

            l.Plate(-5.5f, 0.1f, 5f);
            l.Gate(-7f, 2.2f, 5f, 0.7f, 4.4f, 5.4f, new Vector3(0f, 5.2f, 0f), 1);
            l.Block(-7f, 2f, 2.4f, 1f, 4f, 1.8f);
            l.Block(-7f, 2f, 7.4f, 1f, 4f, 1.8f);

            l.Shard(-1.4f, 5.3f, 0f);

            l.Pedestal(3f, 5f, 3.2f, 3.2f, 1.0f, 3f);
            l.Shard(3f, 1.9f, 5f);

            l.Plate(9f, 0.1f, -4f);
            l.Gate(13f, 2.2f, -4f, 0.7f, 4.4f, 5.4f, new Vector3(0f, 5.2f, 0f), 2);
            l.Block(13f, 2f, -0.6f, 1f, 4f, 1.8f);
            l.Block(13f, 2f, -6.4f, 1f, 4f, 1.8f);

            l.Pedestal(16f, -4f, 3.2f, 3.2f, 1.0f, 3f);
            l.Shard(16f, 1.9f, -4f);

            return l;
        }
    }
}