using Crewmeleon.Components;
using Crewmeleon.Essential;
using Crewmeleon.Roles;
using FungleAPI.Base.Buttons;
using FungleAPI.Hud;
using FungleAPI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Crewmeleon.Buttons
{
    [FungleAPI.Attributes.RegisterPriority(0)]
    public class DrawButton : RoleButton<ChameleonRole>
    {
        public SpriteRenderer PlayerSprite;
        public override ButtonLocation Location => ButtonLocation.BottomLeft;
        public override Sprite ButtonSprite => null;
        public override string OverrideText => ChameleonTranslation.Draw.GetString();
        public override Color32 TextOutlineColor => Color.black;
        public override float Cooldown => 1;
        public override void CreateButton()
        {
            base.CreateButton();
            PlayerSprite = new GameObject("Player")
            {
                layer = Button.gameObject.layer,
                transform =
                 {
                    parent = Button.graphic.transform,
                    localScale = Vector3.one,
                    localPosition = new Vector3(0, 0, 1)
                 }
            }.AddComponent<SpriteRenderer>();
        }
        public override void OnClick()
        {
            if (ChameleonHelper.PaintState == PaintState.None)
            {
                ChameleonHelper.PaintState = PaintState.Painting;
                SpectatorBehaviour spectatorBehaviour = Player.GetComponent<SpectatorBehaviour>();

                spectatorBehaviour.Spectating = false;
                spectatorBehaviour.Update();

                CustomButton<SpectateButton>.Instance.Button.graphic.sprite = ChameleonAssets.Spectate1;
            }
            else
            {
                ChameleonHelper.PaintState = PaintState.None;
                ZoomButton.Zoom?.gameObject.SetActive(false);
                CustomButton<ZoomButton>.Instance.Button.graphic.sprite = ChameleonAssets.Zoom1;
            }
            HudHelper.UpdateActiveState();
        }
        public override void Update()
        {
            base.Update();
            Button.buttonLabelText.text = ChameleonHelper.PaintState == PaintState.None ? ChameleonTranslation.Draw.GetString() : ChameleonTranslation.Stop.GetString();

            if (CanvaPaintBehaviour.Instance != null)
            {
                Button.graphic.sprite = CanvaPaintBehaviour.Instance.Canva.Canva.sprite;
                PlayerSprite.sprite = CanvaPaintBehaviour.Instance.Canva.PlayerSprite.sprite;
                PlayerSprite.material = CanvaPaintBehaviour.Instance.Canva.PlayerSprite.material;
            }
        }
    }
}
