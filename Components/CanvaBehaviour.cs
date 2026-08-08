using AmongUs.Data;
using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Crewmeleon.Buttons;
using Crewmeleon.Essential;
using Crewmeleon.GameMode;
using Crewmeleon.Roles;
using Crewmeleon.RPC;
using FungleAPI.Assets;
using FungleAPI.Components;
using FungleAPI.Hud;
using FungleAPI.Networking;
using FungleAPI.Role;
using Il2CppSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheOldUs;
using UnityEngine;

namespace Crewmeleon.Components
{
    public class CanvaBehaviour : PlayerComponent
    {
        public Dictionary<Vector2Int, PaintStroke> Strokes = new Dictionary<Vector2Int, PaintStroke>();

        public bool[] Paintable;
        public Color32[] TextureBuffer;
        public SpriteRenderer Canva;
        public SpriteRenderer PlayerSprite;

        public Color32 VisorColor = Color.gray;
        public Color32 BodyColor = Color.white;
        public Color32 BackColor = Color.gray;

        public bool OutlineActive;
        public bool ForceShow;

        public RoleTypes ChameleonRole = CustomRoleManager.GetRoleType<ChameleonRole>();

        public float SendTimer;
        public void Start()
        {
            Canva = new GameObject("Canva")
            {
                layer = player.gameObject.layer,
                transform =
                {
                    parent = player.transform,
                    localPosition = Vector3.zero,
                    localScale = Vector3.one
                }
            }.AddComponent<SpriteRenderer>();

            Texture2D texture = new Texture2D(165, 211, TextureFormat.RGBA32, false);

            TextureBuffer = new Color32[165 * 211];
            for (int i = 0; i < TextureBuffer.Count(); i++)
            {
                TextureBuffer[i] = new Color32(0, 0, 0, 0);
            }

            texture.SetPixels32(TextureBuffer);
            texture.Apply();

            Canva.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 200);

            PlayerSprite = new GameObject("PlayerSprite")
            {
                layer = player.gameObject.layer,
                transform =
                {
                    parent = Canva.transform,
                    localPosition = new Vector3(0, 0, 0.00001f),
                    localScale = Vector3.one
                }
            }.AddComponent<SpriteRenderer>();

            PlayerSprite.sprite = AssetLoader.LoadSprite(CrewmeleonPlugin.Plugin.ModAssembly, "Crewmeleon.Assets.DefaultIdle.png", 200, true, false);

            Color[] pixels = PlayerSprite.sprite.texture.GetPixels();
            Paintable = new bool[pixels.Length];

            for (int i = 0; i < pixels.Length; i++)
            {
                Paintable[i] = pixels[i].a > 0;
            }

            Canva.gameObject.SetActive(false);
        }
        public void Update()
        {
            Canva.gameObject.SetActive(IsCanvaActive());

            player.MyPhysics.Animations.group.SpriteAnimator.transform.parent.localScale = player.Data.RoleType == ChameleonRole ? Vector3.zero : Vector3.one;

            if (!Canva.gameObject.activeSelf) return;

            PlayerSprite.material = player.MyPhysics.Animations.group.SpriteAnimator.m_nodes.m_spriteRenderer.material;
            PlayerSprite.flipX = player.cosmetics.FlipX;
            Canva.flipX = player.cosmetics.FlipX;

            SendTimer += Time.deltaTime;
            if (SendTimer >= 0.2f)
            {
                if (Strokes.Count > 0)
                {
                    Rpc<RpcSyncBody>.Instance.Send(player);
                }
                SendTimer = 0;
            }

            if (Canva.gameObject.activeSelf)
            {
                PlayerSprite.material.SetColor("_OutlineColor", Color.white);
                PlayerSprite.material.SetFloat("_Outline", OutlineActive ? 1 : 0);
            }

            if (CanvaPaintBehaviour.Instance == null && player.AmOwner)
            {
                CanvaPaintBehaviour.Instance = Canva.gameObject.AddComponent<CanvaPaintBehaviour>();
                CanvaPaintBehaviour.Instance.Canva = this;
                CanvaPaintBehaviour.Instance.Paint = () => ZoomButton.Zoom == null || !ZoomButton.Zoom.gameObject.activeSelf;
            }
        }
        public bool IsCanvaActive()
        {
            if (ForceShow)
            {
                return true;
            }

            if (player.Data.RoleType != ChameleonRole)
            {
                return false;
            }

            if (PlayerControl.LocalPlayer.Data.Role.TeamType == RoleTeamTypes.Impostor)
            {
                return true;
            }

            if (!player.AmOwner && PlayerControl.LocalPlayer.Data.RoleType == ChameleonRole && ChameleonModeSettings.InfectionSettings.Infection.BooleanValue && ChameleonModeSettings.InfectionSettings.HidePlayers)
            {
                return false;
            }
            return true;
        }
        public bool PaintBrush(Vector2Int center, Color32 color, int brushSize)
        {
            int radiusSqr = brushSize * brushSize;

            int minX = Mathf.Max(0, center.x - brushSize);
            int maxX = Mathf.Min(Canva.sprite.texture.width - 1, center.x + brushSize);
            int minY = Mathf.Max(0, center.y - brushSize);
            int maxY = Mathf.Min(Canva.sprite.texture.height - 1, center.y + brushSize);

            bool paintedAny = false;

            for (int y = minY; y <= maxY; y++)
            {
                int dySqr = (y - center.y) * (y - center.y);
                int rowOffset = y * Canva.sprite.texture.width;

                for (int x = minX; x <= maxX; x++)
                {
                    int dx = x - center.x;
                    if (dx * dx + dySqr > radiusSqr)
                    {
                        continue;
                    }

                    int index = rowOffset + x;
                    if (!Paintable[index])
                    {
                        continue;
                    }

                    TextureBuffer[index] = color;
                    paintedAny = true;
                }
            }

            if (player.AmOwner)
            {
                Strokes[center] = new PaintStroke(color, (byte)brushSize);
            }

            return paintedAny;
        }
    }
}
