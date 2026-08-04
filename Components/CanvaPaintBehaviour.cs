using Crewmeleon.Essential;
using FungleAPI.Api;
using FungleAPI.Attributes;
using FungleAPI.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Crewmeleon.Components
{
    [RegisterTypeInIl2Cpp]
    public class CanvaPaintBehaviour : MonoBehaviour
    {
        public static CanvaPaintBehaviour Instance;

        public const float paintDelay = 0.017f;

        public const int cursorPreviewSortingOrderOffset = 1;
        public const float cursorPreviewAlpha = 0.6f;

        public const int minBrushSize = 1;
        public const int maxBrushSize = 10;

        public const int cursorPreviewBaseDiameter = 32;

        public CanvaBehaviour Canva;

        public Func<bool> Paint;
        public bool CanPaint => ChameleonHelper.PaintState == PaintState.Painting && Canva.player.CanMove && Paint();

        public bool isPainting;
        public Vector2Int lastPaintedPixel = new Vector2Int(-1, -1);

        public GameObject cursorPreviewObject;
        public SpriteRenderer cursorPreviewRenderer;

        public int brushSize = 5;

        private float paintTimer;

        public void Start()
        {
            CreateCursorPreview();
            ApplyCursorPreviewScale();
            cursorPreviewObject.SetActive(false);
        }
        public void Update()
        {
            if (ChameleonHelper.PaintState == PaintState.ColorPicker && Input.GetMouseButtonDown(0))
            {
                ChameleonHelper.PaintState = PaintState.Painting;
                ChameleonHelper.BrushColor = ChameleonHelper.GetMousePixelColor();
                return;
            }

            if (!CanPaint)
            {
                SetCursorPreviewActive(false);
                return;
            }

            float scroll = Input.mouseScrollDelta.y;
            if (scroll != 0f)
            {
                ChangeBrushSize(scroll > 0 ? 1 : -1);
            }

            if (Input.GetMouseButtonDown(0))
            {
                isPainting = true;
                paintTimer = paintDelay;
                lastPaintedPixel = new Vector2Int(-1, -1);
            }

            if (!Input.GetMouseButton(0))
            {
                isPainting = false;
            }

            if (isPainting)
            {
                PaintAtMousePosition();
            }

            UpdateCursorPreview();
        }
        public void ChangeBrushSize(int delta)
        {
            int newSize = Mathf.Clamp(brushSize + delta, minBrushSize, maxBrushSize);

            if (newSize != brushSize)
            {
                brushSize = newSize;
                ApplyCursorPreviewScale();
            }
        }
        private void PaintAtMousePosition()
        {
            Vector2Int? pixel = MouseToPixelCoord(out _);
            if (pixel == null)
            {
                return;
            }

            Vector2Int current = pixel.Value;

            paintTimer += Time.deltaTime;
            if (paintTimer >= paintDelay)
            {
                Color32 brushColor = ChameleonHelper.BrushColor;
                bool painted;

                if (lastPaintedPixel.x < 0)
                {
                    Canva.PaintBrush(current, brushColor, brushSize);
                    painted = true;
                }
                else
                {
                    float distance = Vector2Int.Distance(lastPaintedPixel, current);
                    int steps = Mathf.Max(1, Mathf.CeilToInt(distance));
                    painted = false;

                    for (int i = 0; i <= steps; i++)
                    {
                        float t = i / (float)steps;
                        Vector2Int stepPixel = new Vector2Int(Mathf.RoundToInt(Mathf.Lerp(lastPaintedPixel.x, current.x, t)), Mathf.RoundToInt(Mathf.Lerp(lastPaintedPixel.y, current.y, t)));

                        if (Canva.PaintBrush(stepPixel, brushColor, brushSize))
                        {
                            painted = true;
                        }
                    }
                }

                if (painted)
                {
                    Canva.Canva.sprite.texture.SetPixels32(Canva.TextureBuffer);
                    Canva.Canva.sprite.texture.Apply(false);
                }

                paintTimer = 0;
            }

            lastPaintedPixel = current;
        }
        private Vector2Int? MouseToPixelCoord(out Vector3 mouseWorld)
        {
            Sprite sprite = Canva.Canva.sprite;

            mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 localPos = transform.InverseTransformPoint(mouseWorld);

            float width = sprite.rect.width / sprite.pixelsPerUnit;
            float height = sprite.rect.height / sprite.pixelsPerUnit;

            Vector2 pivot = new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height);

            float x = (localPos.x + pivot.x * width) / width;
            float y = (localPos.y + pivot.y * height) / height;

            if (x < 0 || x > 1 || y < 0 || y > 1)
            {
                return null;
            }

            if (Canva.Canva.flipX)
            {
                x = 1f - x;
            }

            return new Vector2Int(Mathf.Clamp(Mathf.FloorToInt(x * sprite.texture.width), 0, sprite.texture.width - 1), Mathf.Clamp(Mathf.FloorToInt(y * sprite.texture.height), 0, sprite.texture.height - 1));
        }
        private void CreateCursorPreview()
        {
            cursorPreviewObject = new GameObject("BrushCursorPreview")
            {
                hideFlags = HideFlags.DontSave,
                transform = { parent = transform }
            };

            cursorPreviewRenderer = cursorPreviewObject.AddComponent<SpriteRenderer>();
            cursorPreviewRenderer.sortingLayerID = Canva.Canva.sortingLayerID;
            cursorPreviewRenderer.sortingOrder = Canva.Canva.sortingOrder + cursorPreviewSortingOrderOffset;

            int diameter = cursorPreviewBaseDiameter;
            Texture2D previewTexture = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 center = new Vector2((diameter - 1) * 0.5f, (diameter - 1) * 0.5f);
            float radius = diameter * 0.5f;
            float radiusSqr = radius * radius;

            Color[] pixels = new Color[diameter * diameter];
            for (int y = 0; y < diameter; y++)
            {
                float dySqr = (y - center.y) * (y - center.y);
                for (int x = 0; x < diameter; x++)
                {
                    float dx = x - center.x;
                    pixels[y * diameter + x] = new Color(1f, 1f, 1f, dx * dx + dySqr <= radiusSqr ? 1f : 0f);
                }
            }

            previewTexture.SetPixels(pixels);
            previewTexture.Apply();

            cursorPreviewRenderer.sprite = Sprite.Create(previewTexture, new Rect(0, 0, diameter, diameter), new Vector2(0.5f, 0.5f), Canva.Canva.sprite.pixelsPerUnit);
        }
        private void ApplyCursorPreviewScale()
        {
            float desiredDiameter = brushSize * 2 + 1;
            float scale = desiredDiameter / cursorPreviewBaseDiameter;
            cursorPreviewObject.transform.localScale = new Vector3(scale, scale, 1f);
        }
        private void UpdateCursorPreview()
        {
            Vector2Int? pixel = MouseToPixelCoord(out Vector3 mouseWorld);

            if (pixel == null)
            {
                SetCursorPreviewActive(false);
                return;
            }

            SetCursorPreviewActive(true);

            Color previewColor = ChameleonHelper.BrushColor;
            previewColor.a = cursorPreviewAlpha;
            cursorPreviewRenderer.color = previewColor;

            mouseWorld.z = transform.position.z - 0.01f;
            cursorPreviewObject.transform.position = mouseWorld;
        }
        
        private void SetCursorPreviewActive(bool active)
        {
            if (cursorPreviewObject != null && cursorPreviewObject.activeSelf != active)
            {
                cursorPreviewObject.SetActive(active);
            }
        }
    }
}
