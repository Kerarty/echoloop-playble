using UnityEngine;

namespace Playble.EchoLoop
{
    /// <summary>
    /// Прогресс игрока: лучшее число петель на уровень и число открытых уровней.
    /// Хранится в PlayerPrefs — этого достаточно, чтобы «пройдёмость» пережила
    /// перезапуск игры, и при этом не тянуть за собой отдельный слой сериализации.
    /// </summary>
    [DisallowMultipleComponent]
    public class ProgressStore : MonoBehaviour
    {
        const string KeyPrefix = "echoloop.";
        const string KeyUnlocked = KeyPrefix + "unlocked";

        public int LevelsTotal { get; private set; }
        public int Unlocked { get; private set; }

        public void Initialize(int levelsTotal)
        {
            LevelsTotal = Mathf.Max(1, levelsTotal);
            Unlocked = Mathf.Clamp(PlayerPrefs.GetInt(KeyUnlocked, 1), 1, LevelsTotal);
        }

        public bool IsUnlocked(int levelIndex) => levelIndex < Unlocked;

        public void Unlock(int levelIndex)
        {
            int next = Mathf.Clamp(levelIndex + 1, 1, LevelsTotal);
            if (next <= Unlocked) return;

            Unlocked = next;
            PlayerPrefs.SetInt(KeyUnlocked, Unlocked);
            PlayerPrefs.Save();
        }

        public int BestLoops(int levelIndex)
        {
            return PlayerPrefs.GetInt(KeyPrefix + "best." + levelIndex, 0);
        }

        /// <summary>Записать результат. Возвращает true, если это новый рекорд.</summary>
        public bool SubmitResult(int levelIndex, int loops)
        {
            int best = BestLoops(levelIndex);
            if (best > 0 && loops >= best) return false;

            PlayerPrefs.SetInt(KeyPrefix + "best." + levelIndex, loops);
            PlayerPrefs.Save();
            return true;
        }

        public void ResetAll()
        {
            PlayerPrefs.DeleteKey(KeyUnlocked);
            for (int i = 0; i < LevelsTotal; i++)
                PlayerPrefs.DeleteKey(KeyPrefix + "best." + i);
            PlayerPrefs.Save();
            Unlocked = 1;
        }
    }
}