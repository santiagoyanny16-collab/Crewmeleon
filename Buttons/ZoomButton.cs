using Crewmeleon.Components;
using Crewmeleon.Essential;
using Crewmeleon.Roles;
using FungleAPI.Api;
using FungleAPI.Base.Buttons;
using FungleAPI.Components;
using FungleAPI.Extensions;
using FungleAPI.Hud;
using FungleAPI.Ship;
using FungleAPI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Crewmeleon.Buttons
{
    public class ZoomButton : RoleButton<ChameleonRole>
    {
        public static SpriteRenderer Zoom;
        public float Size = 3;
        public override bool Active => base.Active && ChameleonHelper.PaintState != PaintState.None;
        public override ButtonLocation Location => ButtonLocation.BottomLeft;
        public override Sprite ButtonSprite => ChameleonAssets.Zoom1;
        public override string OverrideText => ChameleonTranslation.Zoom.GetString();
        public override Color32 TextOutlineColor => Color.black;
        public override float Cooldown => 1;
        public override void OnClick()
        {
            GameObject zoom = GetZoom().gameObject;
            zoom.SetActive(!zoom.activeSelf);
            Button.graphic.sprite = zoom.activeSelf ? ChameleonAssets.Zoom2 : ChameleonAssets.Zoom1;
        }
        public SpriteRenderer GetZoom()
        {
            if (Zoom == null)
            {
                Zoom = new GameObject("Zoom")
                {
                    layer = 5,
                    transform =
                {
                    parent = Camera.main.transform,
                    localPosition = Vector3.zero,
                    localScale = Vector3.one * 1.05f
                }
                }.AddComponent<SpriteRenderer>();
                Zoom.sprite = ChameleonAssets.ZoomBackground;
                Zoom.drawMode = SpriteDrawMode.Sliced;
                Zoom.size = Vector3.one * 5.8f;

                PluginChanger zoomAd = GameObject.Instantiate(FungleAssets.PluginChangerPrefab, Zoom.transform);
                zoomAd.RightButton.SetNewAction(delegate
                {
                    Size += 0.25f;
                    if (Size > 5)
                    {
                        Size = 5;
                    }
                });
                zoomAd.LeftButton.SetNewAction(delegate
                {
                    Size -= 0.25f;
                    if (Size < 1.5f)
                    {
                        Size = 1.5f;
                    }
                });
                zoomAd.RightButton.transform.localPosition = new Vector3(1.1f, 0, -1);
                zoomAd.LeftButton.transform.localPosition = new Vector3(-1.1f, 0, -1);
                SpriteRenderer zoomB = zoomAd.gameObject.AddComponent<SpriteRenderer>();
                zoomB.sprite = ChameleonAssets.SpectatorBackground;
                zoomB.drawMode = SpriteDrawMode.Sliced;
                zoomB.size = new Vector2(5.5f, 3.2f);
                zoomAd.transform.localPosition = new Vector3(-3.3f, 0, -1);
                zoomAd.transform.rotation = new Quaternion(0, 0, 0.7071f, 0.7071f);
                zoomAd.Text.Destroy();
                zoomAd.Destroy();

                SpriteRenderer Player = new GameObject("Player")
                {
                    layer = 5,
                    transform =
                {
                    parent = Zoom.transform,
                    localPosition = new Vector3(0, 0, -1),
                }
                }.AddComponent<SpriteRenderer>();
                CanvaPaintBehaviour canvaPaintBehaviour = Player.gameObject.AddComponent<CanvaPaintBehaviour>();
                canvaPaintBehaviour.Canva = CanvaPaintBehaviour.Instance.Canva;
                canvaPaintBehaviour.Paint = () => true;

                GameObject quad = new GameObject("Quad")
                {
                    layer = 5,
                    transform =
                {
                    parent = Zoom.transform,
                    localPosition = new Vector3(0, 0, -1),
                    localScale = new Vector3(5, 5, 5)
                }
                };

                MeshFilter meshFilter = quad.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = quad.AddComponent<MeshRenderer>();

                Mesh quadMesh = new Mesh
                {
                    name = "Quad"
                };

                quadMesh.vertices = new Vector3[]
                {
                new Vector3(-0.5f, -0.5f, 0),
                new Vector3( 0.5f, -0.5f, 0),
                new Vector3(-0.5f,  0.5f, 0),
                new Vector3( 0.5f,  0.5f, 0)
                };

                quadMesh.uv = new Vector2[]
                {
                new Vector2(0,0),
                new Vector2(1,0),
                new Vector2(0,1),
                new Vector2(1,1)
                };

                quadMesh.triangles = new int[]
                {
                0,2,1,
                2,3,1
                };

                quadMesh.RecalculateNormals();
                quadMesh.RecalculateBounds();

                meshFilter.sharedMesh = quadMesh;

                Camera camera = GameObject.Instantiate(ShipPrefabLoader.SkeldPrefab.transform.GetChild(21).GetChild(0).GetChild(1).GetChild(0).GetComponent<SystemConsole>().MinigamePrefab.SafeCast<SurveillanceMinigame>().CameraPrefab, Zoom.transform);
                camera.clearFlags = CameraClearFlags.SolidColor;

                RenderTexture rt = new RenderTexture(512, 512, 32);
                rt.Create();

                camera.targetTexture = rt;

                Material mat = new Material(Shader.Find("Unlit/Texture"));
                mat.mainTexture = rt;

                meshRenderer.sharedMaterial = mat;

                Zoom.gameObject.AddComponent<Updater>().update = delegate
                {
                    Player.flipX = CanvaPaintBehaviour.Instance.Canva.Canva.flipX;
                    Player.sprite = CanvaPaintBehaviour.Instance.Canva.Canva.sprite;
                    camera.transform.position = PlayerControl.LocalPlayer.transform.position;
                    camera.orthographicSize = 1.745f / Size;
                    Player.transform.localScale = Vector3.one * Size;
                    if (canvaPaintBehaviour.cursorPreviewObject != null)
                    {
                        canvaPaintBehaviour.cursorPreviewObject.layer = 5;
                    }
                };
                Zoom.gameObject.SetActive(false);
            }
            return Zoom;
        }
    }
}