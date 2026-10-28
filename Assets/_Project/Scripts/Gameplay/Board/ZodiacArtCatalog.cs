using System;
using UnityEngine;

namespace GameMatch3.Gameplay.Board
{
    // Centralizes the stable TileTypeId -> Vietnamese zodiac art mapping.
    // Assets live under a Resources folder so existing scenes and level assets do not
    // need fragile per-sprite references serialized into every pool entry.
    public static class ZodiacArtCatalog
    {
        public const int ZodiacTypeCount = 12;

        private static readonly string[] AnimalNames =
        {
            string.Empty,
            "Rat",
            "Ox",
            "Tiger",
            "Cat",
            "Dragon",
            "Snake",
            "Horse",
            "Goat",
            "Monkey",
            "Rooster",
            "Dog",
            "Pig"
        };

        private static readonly string[] VietnameseNames =
        {
            string.Empty,
            "Tý - Chuột",
            "Sửu - Trâu",
            "Dần - Hổ",
            "Mão - Mèo",
            "Thìn - Rồng",
            "Tỵ - Rắn",
            "Ngọ - Ngựa",
            "Mùi - Dê",
            "Thân - Khỉ",
            "Dậu - Gà",
            "Tuất - Chó",
            "Hợi - Heo"
        };

        private static readonly Sprite[] NormalSprites = new Sprite[ZodiacTypeCount + 1];
        private static readonly Sprite[] BlinkSprites = new Sprite[ZodiacTypeCount + 1];
        private static Sprite horizontalMatch4Overlay;
        private static Sprite verticalMatch4Overlay;
        private static Sprite match5ArmorOverlay;
        private static Sprite wrappedFireworksOverlay;

        public static string GetDisplayName(TileTypeId typeId)
        {
            int index = (int)typeId;
            return index >= 1 && index <= ZodiacTypeCount
                ? VietnameseNames[index]
                : typeId.ToString();
        }

        public static Sprite GetNormalSprite(TileTypeId typeId)
        {
            return LoadAnimalSprite(typeId, "Normal", NormalSprites);
        }

        public static Sprite GetBlinkSprite(TileTypeId typeId)
        {
            return LoadAnimalSprite(typeId, "IdleBlink", BlinkSprites);
        }

        public static Sprite GetSpecialOverlay(
            SpecialObjectType specialType,
            StripedDirection stripedDirection)
        {
            switch (specialType)
            {
                case SpecialObjectType.Striped:
                    if (stripedDirection == StripedDirection.Vertical)
                    {
                        return verticalMatch4Overlay != null
                            ? verticalMatch4Overlay
                            : verticalMatch4Overlay = Resources.Load<Sprite>(
                                "Zodiac/SpecialOverlays/Match4_Vertical_Overlay");
                    }

                    return horizontalMatch4Overlay != null
                        ? horizontalMatch4Overlay
                        : horizontalMatch4Overlay = Resources.Load<Sprite>(
                            "Zodiac/SpecialOverlays/Match4_Horizontal_Overlay");

                case SpecialObjectType.Wrapped:
                    return wrappedFireworksOverlay != null
                        ? wrappedFireworksOverlay
                        : wrappedFireworksOverlay = Resources.Load<Sprite>(
                            "Zodiac/SpecialOverlays/TL_Fireworks_Overlay");

                case SpecialObjectType.ColorBomb:
                    return match5ArmorOverlay != null
                        ? match5ArmorOverlay
                        : match5ArmorOverlay = Resources.Load<Sprite>(
                            "Zodiac/SpecialOverlays/Match5_Armor_Overlay");

                default:
                    return null;
            }
        }

        private static Sprite LoadAnimalSprite(
            TileTypeId typeId,
            string suffix,
            Sprite[] cache)
        {
            int index = (int)typeId;
            if (index < 1 || index > ZodiacTypeCount)
            {
                return null;
            }

            if (cache[index] == null)
            {
                string animalName = AnimalNames[index];
                cache[index] = Resources.Load<Sprite>(
                    $"Zodiac/{animalName}/{animalName}_{suffix}");
            }

            return cache[index];
        }
    }
}
