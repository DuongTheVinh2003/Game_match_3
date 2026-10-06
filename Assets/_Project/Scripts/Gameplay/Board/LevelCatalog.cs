using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameMatch3.Gameplay.Board
{
    // Danh muc runtime duoc Level Editor dong bo tu dong de WebGL/build co the nap level.
    [CreateAssetMenu(fileName = "LevelCatalog", menuName = "Game Match 3/Level Catalog")]
    public sealed class LevelCatalog : ScriptableObject
    {
        [SerializeField] private List<LevelData> classicLevels = new List<LevelData>();
        [SerializeField] private List<LevelData> pveLevels = new List<LevelData>();

        public IReadOnlyList<LevelData> GetLevels(LevelMode mode)
        {
            return mode == LevelMode.Classic ? classicLevels : pveLevels;
        }

        public void Configure(IEnumerable<LevelData> levels)
        {
            classicLevels = Sort(levels.Where(level => level != null && level.Mode == LevelMode.Classic));
            pveLevels = Sort(levels.Where(level => level != null && level.Mode == LevelMode.Pve));
        }

        private static List<LevelData> Sort(IEnumerable<LevelData> levels)
        {
            return levels
                .Distinct()
                .OrderBy(level => level.LevelNumber)
                .ToList();
        }
    }
}
