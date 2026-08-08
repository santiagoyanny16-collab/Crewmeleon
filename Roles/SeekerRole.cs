using BepInEx.Unity.IL2CPP.Utils.Collections;
using Crewmeleon.Components;
using Crewmeleon.Essential;
using Crewmeleon.GameMode;
using Crewmeleon.RPC;
using FungleAPI.Base.Roles;
using FungleAPI.Extensions;
using FungleAPI.GameModes;
using FungleAPI.Networking;
using FungleAPI.Role;
using FungleAPI.Role.Utilities;
using FungleAPI.Teams;
using FungleAPI.Translation;
using FungleAPI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Crewmeleon.Roles
{
    public class SeekerRole : ImpostorBase, ICustomRole
    {
        public static List<byte> SafeToKill = new List<byte>();

        public ModdedTeam Team { get; } = ModdedTeamManager.Impostors;
        public StringNames RoleName { get; } = ChameleonTranslation.SeekerName;
        public StringNames RoleBlur { get; } = ChameleonTranslation.SeekerBlur;
        public StringNames RoleBlurMed { get; } = ChameleonTranslation.SeekerBlur;
        public StringNames RoleBlurLong { get; } = ChameleonTranslation.SeekerBlur;
        public Color RoleColor { get; } = Color.red;
        public bool Kicked;
        public RoleConfiguration Configuration => new RoleConfiguration(this)
        {
            CanUseVent = false,
            CanSabotage = false,
            HideInLobby = true,
            HideInFreeplay = true
        };
        public KillButtonConfig KillConfig { get; } = new KillButtonConfig(out KillButtonConfig self)
        {
            SetTarget = delegate (PlayerControl target)
            {
                self.Button.currentTarget = target;
            },
            DoClick = delegate
            {
                if (self.CanUse())
                {
                    if (ChameleonModeSettings.InfectionSettings.Infection.BooleanValue)
                    {
                        Rpc<RpcSetSeeker>.Instance.Send(self.Button.currentTarget, PlayerControl.LocalPlayer);
                        PlayerControl.LocalPlayer.SetKillTimer(GameModeManager.GetCurrentGameMode().GetKillCooldown());
                    }
                    else
                    {
                        PlayerControl.LocalPlayer.CmdCheckMurder(self.Button.currentTarget);
                    }
                    self.SetTarget(null);
                }
            }
        };
        public override PlayerControl FindClosestTarget()
        {
            PlayerControl closest = null;
            float distance = 1.5f;
            foreach (PlayerControl playerControl in PlayerControl.AllPlayerControls.FindAll(p => SafeToKill.Contains(p.PlayerId) && !p.Data.IsDead && p.Data.Role.TeamType != RoleTeamTypes.Impostor))
            {
                float dis = Vector2.Distance(playerControl.transform.position, Player.transform.position);
                if (dis < distance)
                {
                    distance = dis;
                    closest = playerControl;
                }
            }
            return closest;
        }
        public override bool CanUse(IUsable usable)
        {
            return usable.Is(out Ladder _) || usable.Is(out ZiplineConsole _);
        }
        public void Update()
        {
            if (Player == null) return;

            if (GameModeManager.GetCurrentGameMode() is ChameleonGameMode chameleonGameMode && chameleonGameMode.HideTime > 5 && !Kicked)
            {
                if (Player.rigidbody2D.velocity != Vector2.zero)
                {
                    AntiCheatManager.KickWithReason(Player.OwnerId, "{0} was caught walking during hide time.");
                    Kicked = true;
                }
                else if (Player.isKilling)
                {
                    AntiCheatManager.KickWithReason(Player.OwnerId, "{0} was caught killing during hide time.");
                    Kicked = true;
                }
            }
        }
        public void StartStun()
        {
            StartCoroutine(CoStun().WrapToIl2Cpp());
        }
        public System.Collections.IEnumerator CoStun()
        {
            Player.moveable = false;
            Player.rigidbody2D.velocity = Vector2.zero;
            Player.cosmetics.SetBodyCosmeticsVisible(false);
            yield return Player.MyPhysics.CoAnimateCustom(HudManager.Instance.IntroPrefab.HnSSeekerSpawnAnim);
            Player.cosmetics.ToggleHat(true);
            Player.MyPhysics.SetBodyType(PlayerBodyTypes.Seeker);
            Player.moveable = true;
        }
    }
}
