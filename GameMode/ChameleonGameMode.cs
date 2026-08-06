using AmongUs.GameOptions;
using Crewmeleon.Essential;
using Crewmeleon.Roles;
using Crewmeleon.RPC;
using FungleAPI.Api;
using FungleAPI.Components;
using FungleAPI.Extensions;
using FungleAPI.GameModes;
using FungleAPI.Networking;
using FungleAPI.PluginLoading;
using FungleAPI.Role;
using FungleAPI.Role.Utilities;
using FungleAPI.Translation;
using FungleAPI.Utilities;
using Il2CppSystem.Xml.Schema;
using Innersloth.Assets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Crewmeleon.GameMode
{
    public class ChameleonGameMode : NormalGameMode
    {
        public TextMeshPro TitleText;
        public ProgressTracker ProgressTracker;
        public float Time;
        public float HideTime;
        public float RevelationTime;
        public bool CanCount;
        public override GameModeOptions ModeOptions { get; } = new ChameleonModeSettings();
        public override StringNames GameModeName => ChameleonTranslation.ChameleonsText;
        public override int RequiredPlayerToStart() => 2;
        public override float CalculateLightRadius(NetworkedPlayerInfo player, bool airship)
        {
            if (player.Role.TeamType == RoleTeamTypes.Impostor && HideTime > 0)
            {
                return 0;
            }
            return base.CalculateLightRadius(player, airship);
        }
        public override void OnGameStart()
        {
            ProgressTracker = HudManager.Instance.TaskPanel.transform.parent.GetChild(1).GetComponent<ProgressTracker>();
            TitleText = ProgressTracker.transform.GetChild(2).GetComponent<TextMeshPro>();

            Time = ChameleonModeSettings.GeneralSettings.Time.FloatValue;
            HideTime = ChameleonModeSettings.GeneralSettings.HideTime.FloatValue;
            RevelationTime = 15;

            Manager.gameObject.AddComponent<Updater>().update = delegate
            {
                if (CanCount)
                {
                    if (HideTime > 0)
                    {
                        HideTime -= UnityEngine.Time.deltaTime;
                        ProgressTracker.curValue = HideTime / ChameleonModeSettings.GeneralSettings.HideTime.FloatValue;
                        TitleText.text = string.Format(ChameleonTranslation.HideTimerText.GetString(), (int)HideTime);
                    }
                    else if (Time > 0)
                    {
                        Time -= UnityEngine.Time.deltaTime;
                        ProgressTracker.curValue = Time / ChameleonModeSettings.GeneralSettings.Time.FloatValue;
                        TitleText.text = string.Format(ChameleonTranslation.RemainTimeText.GetString(), (int)Time);
                        if (Time <= 0)
                        {
                            AudioClip audioClip = RoleManager.Instance.GetRole(RoleTypes.Noisemaker).SafeCast<NoisemakerRole>().deathSound;

                            SoundManager.Instance.PlaySound(audioClip, false, 1.5f);

                            foreach (PlayerControl playerControl in PlayerControl.AllPlayerControls)
                            {
                                if (playerControl.Data.Role.Is(out ChameleonRole chameleonRole))
                                {
                                    chameleonRole.Reveal();
                                }
                            }
                            SeekerRole.SafeToKill.Clear();
                        }
                    }
                    else
                    {
                        RevelationTime -= UnityEngine.Time.deltaTime;
                        ProgressTracker.curValue = RevelationTime / 15;
                        TitleText.text = string.Format(ChameleonTranslation.RevelationText.GetString(), (int)RevelationTime);
                    }
                    ProgressTracker.enabled = false;
                    ProgressTracker.TileParent.enabled = true;
                    ProgressTracker.TileParent.material.SetFloat("_Buckets", 1);
                    ProgressTracker.TileParent.material.SetFloat("_FullBuckets", ProgressTracker.curValue);
                }
            };
        }
        public override void SetTaskPanelText(HudManager hudManager)
        {
            foreach (PlayerControl playerControl in PlayerControl.AllPlayerControls)
            {
                string roleText = playerControl.Data.Role.NiceName;
                if (playerControl.Data.IsDead)
                {
                    roleText = StringNames.DeadLabel.GetString();
                }
                hudManager.tasksString.AppendLine($"{playerControl.Data.PlayerName}: {playerControl.Data.Role.TeamColor.ToTextColor()}{roleText}</color>");
            }
        }
        public override float CanUseMapConsole(MapConsole mapConsole, NetworkedPlayerInfo pc, out bool canUse, out bool couldUse)
        {
            canUse = false;
            couldUse = false;
            return 0;
        }
        public override void OnGameEnd()
        {
            Manager.GetComponent<Updater>()?.Destroy();
        }
        public override bool GetChatInGame()
        {
            return ChameleonModeSettings.GeneralSettings.ChatDuringGame.BooleanValue;
        }
        public override float GetKillCooldown()
        {
            return 0.01f;
        }
        public override float GetPlayerSpeedMod(PlayerControl pc)
        {
            return pc.Data.Role.TeamType == RoleTeamTypes.Impostor ? ChameleonModeSettings.GeneralSettings.SeekerSpeed.FloatValue : ChameleonModeSettings.GeneralSettings.ChameleonSpeed.FloatValue;
        }
        public override bool CanReportBodies()
        {
            return false;
        }
        public override void OnPlayerDeath(PlayerControl player, bool assignGhostRole)
        {
            if (AmongUsClient.Instance.AmHost && assignGhostRole)
            {
                player.RpcSetRole(RoleTypes.CrewmateGhost, false);
            }
        }
        public override MapOptions GetMapOptions()
        {
            return new MapOptions
            {
                Mode = MapOptions.Modes.Normal
            };
        }
        public override TaskBarMode GetTaskBarMode()
        {
            return TaskBarMode.Normal;
        }
        public override void SelectRoles(RoleManager roleManager)
        {
            List<PlayerControl> playerControls = PlayerControl.AllPlayerControls.ToSystemList();
            EnumerableExtensions.Shuffle(playerControls);

            if (playerControls.Count <= 0) return;

            int players = ChameleonModeSettings.GeneralSettings.SeekersCount.IntValue;

            if (HostImpCommand.HostImpostorEnabled && playerControls.Contains(PlayerControl.LocalPlayer))
            {
                players--;

                playerControls.Remove(PlayerControl.LocalPlayer);

                PlayerControl.LocalPlayer.RpcSetRole(CustomRoleManager.GetRoleType<SeekerRole>());
            }

            for (int i = 0; i < players; i++)
            {
                if (playerControls.Count <= 0) break;

                PlayerControl playerControl = playerControls[0];
                playerControls.RemoveAt(0);

                playerControl.RpcSetRole(CustomRoleManager.GetRoleType<SeekerRole>());
            }
            foreach (PlayerControl playerControl in playerControls)
            {
                playerControl.RpcSetRole(CustomRoleManager.GetRoleType<ChameleonRole>());
            }
        }
        public override void CheckEndCriteria()
        {
            if (RevelationTime <= 0)
            {
                Manager.RpcEndGame(GameOverReason.CrewmatesByTask, false);
                CanCount = false;
                return;
            }

            int aliveCrewmates = 0;
            int aliveImpostors = 0;
            foreach (PlayerControl playerControl in PlayerControl.AllPlayerControls)
            {
                if (!playerControl.Data.IsDead)
                {
                    if (playerControl.Data.Role.TeamType == RoleTeamTypes.Impostor)
                    {
                        aliveImpostors++;
                        continue;
                    }
                    aliveCrewmates++;
                }
            }
            if (aliveCrewmates <= 0)
            {
                Manager.RpcEndGame(GameOverReason.ImpostorsByKill, false);
                CanCount = false;
                return;
            }
            if (aliveImpostors <= 0)
            {
                Manager.RpcEndGame(GameOverReason.CrewmatesByTask, false);
                CanCount = false;
            }
        }
    }
}
