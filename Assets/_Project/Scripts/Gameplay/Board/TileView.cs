using System;
using UnityEngine;

namespace GameMatch3.Gameplay.Board
{
    [Serializable]
    public sealed class TileAnimationConfig
    {
        [SerializeField, Range(0f, 0.08f)] private float idleBounceAmplitude = 0.025f;
        [SerializeField, Min(0.1f)] private float idleBounceSpeed = 2.2f;
        [SerializeField, Min(0.2f)] private float blinkIntervalMin = 2.2f;
        [SerializeField, Min(0.2f)] private float blinkIntervalMax = 4.5f;
        [SerializeField, Range(0.04f, 0.4f)] private float blinkDuration = 0.11f;
        [SerializeField, Range(0.08f, 0.6f)] private float selectedDuration = 0.24f;
        [SerializeField, Range(0f, 0.3f)] private float selectedBounce = 0.14f;

        public float IdleBounceAmplitude => Mathf.Clamp(idleBounceAmplitude, 0f, 0.08f);
        public float IdleBounceSpeed => Mathf.Max(0.1f, idleBounceSpeed);
        public float BlinkIntervalMin => Mathf.Max(0.2f, blinkIntervalMin);
        public float BlinkIntervalMax => Mathf.Max(BlinkIntervalMin, blinkIntervalMax);
        public float BlinkDuration => Mathf.Clamp(blinkDuration, 0.04f, 0.4f);
        public float SelectedDuration => Mathf.Clamp(selectedDuration, 0.08f, 0.6f);
        public float SelectedBounce => Mathf.Clamp(selectedBounce, 0f, 0.3f);
    }

    // Displays a static 2D zodiac tile. Board movement remains in BoardController,
    // while character idle/blink/VFX animation is intentionally disabled.
    [ExecuteAlways, RequireComponent(typeof(SpriteRenderer))]
    public sealed class TileView : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer overlayRenderer;
        private Transform visualRoot;
        private Sprite normalFallbackSprite;
        private Sprite specialFallbackSprite;
        private Sprite normalSprite;
        private TilePoolEntry currentDefinition;
        private TileAnimationConfig animationConfig;
        private float selectedEndTime;
        private float visualFitScale = 1f;
        private bool destroying;

        public Tile Tile { get; private set; }
        public int InstanceId => Tile.InstanceId;
        public TileTypeId TypeId => Tile.TypeId;
        public Vector2Int Coordinate => Tile.Coordinate;
        public SpecialObjectType SpecialType => Tile.SpecialType;
        public StripedDirection StripedDirection => Tile.StripedDirection;
        public bool CanMatchByType => Tile.CanMatchByType;

        public void Initialize(
            Tile tile,
            TilePoolEntry definition,
            Sprite fallbackSprite,
            TileAnimationConfig config = null)
        {
            Tile = tile;
            animationConfig = config ?? new TileAnimationConfig();
            normalFallbackSprite = fallbackSprite;
            currentDefinition = definition;
            CreateVisualHierarchy();
            ApplyDefinition(definition, fallbackSprite);
            UpdateObjectName();
        }

        public void SetType(TileTypeId typeId, TilePoolEntry definition, Sprite fallbackSprite)
        {
            Tile.SetTypeId(typeId);
            normalFallbackSprite = fallbackSprite;
            currentDefinition = definition;
            destroying = false;
            ApplyCurrentVisual();
            UpdateObjectName();
        }

        public void SetSpecialObject(
            SpecialObjectType specialType,
            StripedDirection stripedDirection,
            Sprite modelSprite)
        {
            Tile.SetSpecialObject(specialType, stripedDirection);
            specialFallbackSprite = modelSprite;
            ApplyCurrentVisual();
            UpdateObjectName();
        }

        public void PlaySelected()
        {
            if (!Application.isPlaying || destroying)
            {
                return;
            }

            selectedEndTime = Time.time + animationConfig.SelectedDuration;
            spriteRenderer.sprite = normalSprite;
        }

        public void SetDestroyProgress(float normalizedProgress)
        {
            if (visualRoot == null)
            {
                return;
            }

            destroying = true;
            float progress = Mathf.Clamp01(normalizedProgress);
            visualRoot.localScale = Vector3.one * visualFitScale;
            Color color = new Color(1f, 1f, 1f, 1f - progress);
            spriteRenderer.color = color;
            if (overlayRenderer != null)
            {
                overlayRenderer.color = new Color(1f, 1f, 1f, 1f - progress);
            }
        }

        public void SetCoordinate(Vector2Int coordinate)
        {
            Tile.SetCoordinate(coordinate);
            UpdateObjectName();
        }

        private void Update()
        {
            if (visualRoot == null)
            {
                return;
            }

            RefreshArtIfAvailable();

            if (!Application.isPlaying || destroying)
            {
                return;
            }

            if (Time.time < selectedEndTime)
            {
                float remaining = selectedEndTime - Time.time;
                float progress = 1f - remaining / animationConfig.SelectedDuration;
                spriteRenderer.color = Color.Lerp(
                    Color.white,
                    new Color(1f, 0.93f, 0.45f, 1f),
                    Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI));
            }
            else
            {
                spriteRenderer.color = Color.white;
            }

            visualRoot.localScale = Vector3.one * visualFitScale;
        }

        private void RefreshArtIfAvailable()
        {
            if (currentDefinition == null)
            {
                return;
            }

            Sprite resolvedSprite = currentDefinition.Sprite;
            if (resolvedSprite == null)
            {
                return;
            }

            float resolvedFitScale = CalculateFitScale(resolvedSprite);
            bool spriteChanged = resolvedSprite != normalSprite;
            bool scaleChanged = !Mathf.Approximately(resolvedFitScale, visualFitScale);
            if (!spriteChanged && !scaleChanged)
            {
                return;
            }

            // Asset imports can finish after an ExecuteAlways board preview was built.
            // Rebind/recalculate in place so previews cannot retain stale sprite bounds.
            if (spriteChanged)
            {
                ApplyCurrentVisual();
                return;
            }

            visualFitScale = resolvedFitScale;
            if (!destroying)
            {
                visualRoot.localScale = Vector3.one * visualFitScale;
            }
        }

        private void CreateVisualHierarchy()
        {
            SpriteRenderer rootRenderer = GetComponent<SpriteRenderer>();
            rootRenderer.enabled = false;

            GameObject visualObject = new GameObject("Visual");
            visualObject.hideFlags = gameObject.hideFlags;
            visualObject.transform.SetParent(transform, false);
            visualRoot = visualObject.transform;

            spriteRenderer = visualObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 1;

            GameObject overlayObject = new GameObject("SpecialOverlay");
            overlayObject.hideFlags = gameObject.hideFlags;
            overlayObject.transform.SetParent(visualRoot, false);
            overlayRenderer = overlayObject.AddComponent<SpriteRenderer>();
            overlayRenderer.sortingOrder = 2;
            overlayRenderer.enabled = false;
        }

        private void ApplyCurrentVisual()
        {
            ApplyDefinition(currentDefinition, normalFallbackSprite);
            transform.localRotation = Quaternion.identity;

            if (SpecialType == SpecialObjectType.None)
            {
                overlayRenderer.enabled = false;
                overlayRenderer.sprite = null;
                return;
            }

            Sprite overlay = ZodiacArtCatalog.GetSpecialOverlay(SpecialType, StripedDirection);
            overlayRenderer.sprite = overlay != null ? overlay : specialFallbackSprite;
            overlayRenderer.color = Color.white;
            overlayRenderer.enabled = overlayRenderer.sprite != null;
        }

        private void ApplyDefinition(TilePoolEntry definition, Sprite fallbackSprite)
        {
            normalSprite = definition.Sprite != null ? definition.Sprite : fallbackSprite;
            bool usesPrototypeVisual = definition.Sprite == null;
            spriteRenderer.sprite = normalSprite;
            spriteRenderer.color = usesPrototypeVisual ? definition.PrototypeColor : Color.white;

            visualFitScale = CalculateFitScale(normalSprite);

            if (visualRoot != null && !destroying)
            {
                visualRoot.localScale = Vector3.one * visualFitScale;
            }
        }

        private static float CalculateFitScale(Sprite sprite)
        {
            if (sprite == null)
            {
                return 1f;
            }

            Vector2 spriteSize = sprite.bounds.size;
            float largestSide = Mathf.Max(spriteSize.x, spriteSize.y);
            return largestSide > 0.0001f ? 1f / largestSide : 1f;
        }

        private void UpdateObjectName()
        {
            string typeName = SpecialType == SpecialObjectType.ColorBomb
                ? SpecialType.ToString()
                : $"{ZodiacArtCatalog.GetDisplayName(TypeId)}_{SpecialType}";
            transform.name = $"Tile_{typeName}_I{InstanceId:000000}_{Coordinate.x}_{Coordinate.y}";
        }
    }
}
