using Crewmeleon.Essential;
using FungleAPI.Api;
using FungleAPI.Attributes;
using FungleAPI.Components;
using FungleAPI.Extensions;
using FungleAPI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
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

        public SpriteRenderer cursorPreviewCopy;
        public SpriteRenderer cursorPreviewRenderer;

        public GameObject Changer;

        public int brushSize = 5;

        private float paintTimer;

        public void Start()
        {
            if (cursorPreviewRenderer != null) return;

            CreateCursorPreview();
            cursorPreviewRenderer.gameObject.SetActive(false);

            PluginChanger zoomAd = GameObject.Instantiate(FungleAssets.PluginChangerPrefab, transform);
            zoomAd.RightButton.SetNewAction(delegate
            {
                ChangeBrushSize(1);
            });
            zoomAd.LeftButton.SetNewAction(delegate
            {
                ChangeBrushSize(-1);
            });
            zoomAd.RightButton.transform.localPosition = new Vector3(1.3f, 0, -1);
            zoomAd.LeftButton.transform.localPosition = new Vector3(-1.3f, 0, -1);
            SpriteRenderer zoomB = zoomAd.gameObject.AddComponent<SpriteRenderer>();
            zoomB.sprite = ChameleonAssets.SpectatorBackground;
            zoomB.drawMode = SpriteDrawMode.Sliced;
            zoomB.size = new Vector2(5.5f, 3.2f);
            zoomAd.transform.localPosition = new Vector3(1.5f, 0, -1);
            zoomAd.transform.rotation = new Quaternion(0, 0, 0.7071f, 0.7071f);
            zoomAd.Text.Destroy();

            Changer = zoomAd.gameObject;

            zoomAd.Destroy();

            cursorPreviewCopy = new GameObject("BrushCursorPreview")
            {
                layer = 5,
                transform =
                {
                    parent = transform,
                    localPosition = new Vector3(0, 0, -1)
                }
            }.AddComponent<SpriteRenderer>();
            cursorPreviewCopy.sprite = cursorPreviewRenderer.sprite;

            ApplyCursorPreviewScale();
        }
        public void Update()
        {
            if (ChameleonHelper.PaintState == PaintState.ColorPicker && Input.GetMouseButtonDown(0))
            {
                ChameleonHelper.PaintState = PaintState.Painting;
                ChameleonHelper.BrushColor = ChameleonHelper.GetMousePixelColor();
                return;
            }

            bool ac = CanPaint;
            Changer.SetActive(ac);
            cursorPreviewCopy.gameObject.SetActive(ac);
            if (!ac)
            {
                cursorPreviewRenderer.gameObject.SetActive(false);
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
            cursorPreviewRenderer = new GameObject("BrushCursorPreview")
            {
                layer = 5,
                transform = { parent = transform }
            }.AddComponent<SpriteRenderer>();

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
            cursorPreviewRenderer.transform.localScale = new Vector3(scale, scale, 1f);
            cursorPreviewCopy.transform.localScale = cursorPreviewRenderer.transform.localScale;
        }
        private void UpdateCursorPreview()
        {
            Vector3 vector3 = Changer.transform.position;
            vector3.z += -1;
            cursorPreviewCopy.transform.position = vector3;

            Vector2Int? pixel = MouseToPixelCoord(out Vector3 mouseWorld);

            if (pixel == null)
            {
                cursorPreviewRenderer.gameObject.SetActive(false);
                return;
            }

            cursorPreviewRenderer.gameObject.SetActive(true);

            Color previewColor = ChameleonHelper.BrushColor;
            previewColor.a = cursorPreviewAlpha;
            cursorPreviewRenderer.color = previewColor;
            cursorPreviewCopy.color = previewColor;

            mouseWorld.z = transform.position.z - 0.01f;
            cursorPreviewRenderer.transform.position = mouseWorld;
        }
    }
}
