using Crewmeleon.Essential;
using Crewmeleon.GameMode;
using FungleAPI.Api;
using FungleAPI.Components;
using FungleAPI.Event;
using FungleAPI.Event.Vanilla.Player;
using FungleAPI.Extensions;
using FungleAPI.Hud;
using FungleAPI.Utilities;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Crewmeleon.Components
{
    public class SpectatorBehaviour : PlayerComponent
    {
        public TextMeshPro PlayerText;
        public GameObject Changer;

        public bool lastSpectating;
        public bool Spectating;
        public int SpectatorIndex;
        public PlayerControl Target;

        public void Update()
        {
            if (!player.AmOwner) return;

            if (Spectating && (Target == null || Target != null && Target.Data.IsDead))
            {
                List<PlayerControl> spectable = GetSpectablePlayers();
                if (spectable.Count > 0)
                {
                    Target = spectable[0];
                    SpectatorIndex = 0;
                    UpdateText();
                }
                else
                {
                    Spectating = false;
                }
            }
            else if (!Spectating)
            {
                Target = player;
                if (Target != null)
                {
                    UpdateText();
                }
            }

            if (player == null || Target == null) return;

            if (HudManager.InstanceExists)
            {
                HudManager.Instance.PlayerCam.Target = Target;
                player.lightSource.transform.position = Target.transform.position;
            }

            if (lastSpectating != Spectating)
            {
                GetChanger().SetActive(Spectating);
                lastSpectating = Spectating;
            }
        }

        public void Next()
        {
            List<PlayerControl> spectable = GetSpectablePlayers();

            if (spectable.Count <= 0)
            {
                Spectating = false;
                return;
            }

            SpectatorIndex++;
            if (spectable.Count <= SpectatorIndex)
            {
                SpectatorIndex = 0;
            }
            Target = spectable[SpectatorIndex];
            UpdateText();
        }
        public void Back()
        {
            List<PlayerControl> spectable = GetSpectablePlayers();

            if (spectable.Count <= 0)
            {
                Spectating = false;
                return;
            }

            SpectatorIndex--;
            if (0 > SpectatorIndex)
            {
                SpectatorIndex = spectable.Count - 1;
            }
            Target = spectable[SpectatorIndex];
            UpdateText();
        }

        public GameObject GetChanger()
        {
            if (Changer == null)
            {
                PluginChanger pluginChanger = GameObject.Instantiate(FungleAssets.PluginChangerPrefab, HudManager.Instance.transform);
                PlayerText = pluginChanger.Text;
                pluginChanger.RightButton.SetNewAction(Next);
                pluginChanger.LeftButton.SetNewAction(Back);
                pluginChanger.RightButton.transform.localPosition = new Vector3(4, 0, -1);
                pluginChanger.LeftButton.transform.localPosition = new Vector3(-4, 0, -1);
                SpriteRenderer spriteRenderer = pluginChanger.gameObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = ChameleonAssets.SpectatorBackground;
                spriteRenderer.drawMode = SpriteDrawMode.Sliced;
                spriteRenderer.size = new Vector2(11, 3);
                Changer = pluginChanger.gameObject;
                Changer.transform.localPosition = new Vector3(0, HudHelper.BottomLeft.transform.localPosition.y, 0);
                pluginChanger.Destroy();
                UpdateText();
            }
            return Changer;
        }

        public void UpdateText()
        {
            if (PlayerText == null) return;

            PlayerText.text = $"{Target.Data.PlayerName}\n<size=40%>{Target.Data.Role.TeamColor.ToTextColor()}{Target.Data.Role.NiceName}</color>";
        }


        public List<PlayerControl> GetSpectablePlayers()
        {
            if (ChameleonModeSettings.InfectionSettings.Infection.BooleanValue && AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay)
            {
                return PlayerControl.AllPlayerControls.ToSystemList().FindAll(p => p.Data.Role.TeamType == RoleTeamTypes.Impostor);
            }

            return PlayerControl.AllPlayerControls.ToSystemList().FindAll(p => p != null && !p.Data.IsDead);
        }

        [EventRegister]
        public static void OnChangeRole(AfterSetRoleEvent afterSetRoleEvent)
        {
            afterSetRoleEvent.TargetPlayer.GetComponent<SpectatorBehaviour>().Spectating = false;
        }
    }
}
