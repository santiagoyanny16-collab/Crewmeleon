using BepInEx.Unity.IL2CPP.Utils.Collections;
using Crewmeleon.Components;
using Crewmeleon.Essential;
using Crewmeleon.Roles;
using FungleAPI.Base.Buttons;
using FungleAPI.Components;
using FungleAPI.Hud;
using FungleAPI.Ship;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Crewmeleon.Buttons
{
    [FungleAPI.Attributes.RegisterPriority(1)]
    public class GradientButton : RoleButton<ChameleonRole>
    {
        public override bool Active => base.Active && ChameleonHelper.PaintState != PaintState.None;
        public override ButtonLocation Location => ButtonLocation.BottomLeft;
        public override Sprite ButtonSprite => ChameleonAssets.GradientButton;
        public override string OverrideText => ChameleonTranslation.Gradient.GetString();
        public override Color32 TextOutlineColor => Color.black;
        public override float Cooldown => 1;
        public override void OnClick()
        {
            if (Minigame.Instance != null) return;

            Minigame.Instance = GameObject.Instantiate(ShipPrefabLoader.SkeldPrefab.transform.Find("TaskAddConsole").GetComponent<SystemConsole>().MinigamePrefab, Camera.main.transform);

            for (int i = 2; i < Minigame.Instance.transform.GetChildCount(); i++)
            {
                Minigame.Instance.transform.GetChild(i).gameObject.SetActive(false);
            }

            SpriteRenderer display = new GameObject("Display")
            {
                layer = 5,
                transform =
                {
                    parent = Minigame.Instance.transform,
                    localPosition = new Vector3(0, 1.7f, -1),
                    localScale = new Vector3(2.6f, 2, 1)
                }
            }.AddComponent<SpriteRenderer>();
            display.sprite = ChameleonAssets.ColorDisplay;
            display.color = ChameleonHelper.BrushColor;

            SpriteRenderer spriteRenderer = new GameObject("Gradient")
            {
                layer = 5,
                transform =
                {
                    parent = Minigame.Instance.transform,
                    localPosition = new Vector3(0, -0.6f, -1)
                }
            }.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = ChameleonAssets.Gradient;
            spriteRenderer.gameObject.SetActive(false);

            BoxCollider2D boxCollider2D = spriteRenderer.gameObject.AddComponent<BoxCollider2D>();
            boxCollider2D.isTrigger = true;
            spriteRenderer.gameObject.SetActive(true);

            Minigame.Instance.timeOpened = Time.realtimeSinceStartup;
            if (PlayerControl.LocalPlayer)
            {
                if (MapBehaviour.Instance)
                {
                    MapBehaviour.Instance.Close();
                }
                PlayerControl.LocalPlayer.MyPhysics.SetNormalizedVelocity(Vector2.zero);
            }
            Minigame.Instance.logger.Info("Opening minigame " + base.GetType().Name, null);
            Minigame.Instance.StartCoroutine(Minigame.Instance.CoAnimateOpen());

            System.Collections.IEnumerator CoStart()
            {
                while (true)
                {
                    yield return new WaitForEndOfFrame();
                    if (Input.GetMouseButton(0) && boxCollider2D.OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition)))
                    {
                        ChameleonHelper.BrushColor = ChameleonHelper.GetMousePixelColor();
                        display.color = ChameleonHelper.BrushColor;
                    }
                }
            }

            Minigame.Instance.StartCoroutine(CoStart().WrapToIl2Cpp());
        }
    }
}
