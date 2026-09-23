using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameMatch3.Gameplay.Board
{
    [Serializable]
    public sealed class TilePoolEntry
    {
        [SerializeField] private TileTypeId typeId;
        [SerializeField] private bool enabledForLevel;
        [SerializeField, Min(0.01f)] private float spawnWeight = 1f;
        [SerializeField] private Color prototypeColor = Color.white;
        [SerializeField] private Sprite sprite = null;

        public TileTypeId TypeId => typeId;
        public bool EnabledForLevel => enabledForLevel;
        public float SpawnWeight => Mathf.Max(0.01f, spawnWeight);
        public Color PrototypeColor => prototypeColor;
        public Sprite Sprite => sprite;

        public TilePoolEntry(
            TileTypeId typeId,
            Color prototypeColor,
            bool enabledForLevel,
            float spawnWeight = 1f)
        {
            this.typeId = typeId;
            this.prototypeColor = prototypeColor;
            this.enabledForLevel = enabledForLevel;
            this.spawnWeight = spawnWeight;
        }
    }

    // Danh muc 10 loai tile va cau hinh pool dang dung cho level hien tai.
    [Serializable]
    public sealed class TilePool
    {
        [SerializeField] private List<TilePoolEntry> entries = CreateDefaultEntries();

        public IReadOnlyList<TilePoolEntry> Entries => entries;

        public void EnsureCompleteCatalog()
        {
            if (HasCompleteCatalog())
            {
                return;
            }

            Dictionary<TileTypeId, TilePoolEntry> currentEntries =
                new Dictionary<TileTypeId, TilePoolEntry>();

            if (entries != null)
            {
                foreach (TilePoolEntry entry in entries)
                {
                    if (entry != null && !currentEntries.ContainsKey(entry.TypeId))
                    {
                        currentEntries.Add(entry.TypeId, entry);
                    }
                }
            }

            List<TilePoolEntry> normalizedEntries = new List<TilePoolEntry>(10);
            foreach (TileTypeId typeId in Enum.GetValues(typeof(TileTypeId)))
            {
                normalizedEntries.Add(currentEntries.TryGetValue(typeId, out TilePoolEntry entry)
                    ? entry
                    : CreateDefaultEntry(typeId));
            }

            entries = normalizedEntries;
        }

        private bool HasCompleteCatalog()
        {
            if (entries == null || entries.Count != 10)
            {
                return false;
            }

            HashSet<TileTypeId> typeIds = new HashSet<TileTypeId>();
            foreach (TilePoolEntry entry in entries)
            {
                if (entry == null
                    || !Enum.IsDefined(typeof(TileTypeId), entry.TypeId)
                    || !typeIds.Add(entry.TypeId))
                {
                    return false;
                }
            }

            return typeIds.Count == 10;
        }

        public void GetActiveEntries(List<TilePoolEntry> results)
        {
            results.Clear();
            EnsureCompleteCatalog();

            foreach (TilePoolEntry entry in entries)
            {
                if (entry.EnabledForLevel && entry.SpawnWeight > 0f)
                {
                    results.Add(entry);
                }
            }

            // Cau hinh level khong hop le van can mot loai de board co the duoc tao va sua trong Inspector.
            if (results.Count == 0)
            {
                results.Add(entries[0]);
            }
        }

        public TilePoolEntry GetEntry(TileTypeId typeId)
        {
            EnsureCompleteCatalog();
            foreach (TilePoolEntry entry in entries)
            {
                if (entry.TypeId == typeId)
                {
                    return entry;
                }
            }

            throw new InvalidOperationException($"Tile type {typeId} is missing from the catalog.");
        }

        public static TilePoolEntry PickRandom(
            System.Random random,
            IReadOnlyList<TilePoolEntry> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                throw new InvalidOperationException("Tile pool must contain at least one active tile type.");
            }

            double totalWeight = 0d;
            for (int i = 0; i < candidates.Count; i++)
            {
                totalWeight += candidates[i].SpawnWeight;
            }

            double selection = random.NextDouble() * totalWeight;
            for (int i = 0; i < candidates.Count; i++)
            {
                selection -= candidates[i].SpawnWeight;
                if (selection <= 0d)
                {
                    return candidates[i];
                }
            }

            return candidates[candidates.Count - 1];
        }

        private static List<TilePoolEntry> CreateDefaultEntries()
        {
            List<TilePoolEntry> defaults = new List<TilePoolEntry>(10);
            foreach (TileTypeId typeId in Enum.GetValues(typeof(TileTypeId)))
            {
                defaults.Add(CreateDefaultEntry(typeId));
            }

            return defaults;
        }

        private static TilePoolEntry CreateDefaultEntry(TileTypeId typeId)
        {
            bool enabledByDefault = (int)typeId <= 4;
            switch (typeId)
            {
                case TileTypeId.Tile01:
                    return new TilePoolEntry(typeId, new Color(0.91f, 0.20f, 0.22f), enabledByDefault);
                case TileTypeId.Tile02:
                    return new TilePoolEntry(typeId, new Color(1.00f, 0.78f, 0.12f), enabledByDefault);
                case TileTypeId.Tile03:
                    return new TilePoolEntry(typeId, new Color(0.20f, 0.72f, 0.32f), enabledByDefault);
                case TileTypeId.Tile04:
                    return new TilePoolEntry(typeId, new Color(0.16f, 0.48f, 0.90f), enabledByDefault);
                case TileTypeId.Tile05:
                    return new TilePoolEntry(typeId, new Color(0.57f, 0.28f, 0.84f), enabledByDefault);
                case TileTypeId.Tile06:
                    return new TilePoolEntry(typeId, new Color(1.00f, 0.47f, 0.10f), enabledByDefault);
                case TileTypeId.Tile07:
                    return new TilePoolEntry(typeId, new Color(0.10f, 0.82f, 0.84f), enabledByDefault);
                case TileTypeId.Tile08:
                    return new TilePoolEntry(typeId, new Color(0.96f, 0.31f, 0.62f), enabledByDefault);
                case TileTypeId.Tile09:
                    return new TilePoolEntry(typeId, new Color(0.60f, 0.82f, 0.13f), enabledByDefault);
                default:
                    return new TilePoolEntry(typeId, new Color(0.55f, 0.34f, 0.20f), enabledByDefault);
            }
        }
    }
}
