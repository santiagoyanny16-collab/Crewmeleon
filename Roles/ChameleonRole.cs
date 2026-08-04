using Crewmeleon.Buttons;
using Crewmeleon.Components;
using Crewmeleon.Essential;
using Crewmeleon.GameMode;
using Crewmeleon.RPC;
using FungleAPI.Base.Roles;
using FungleAPI.Extensions;
using FungleAPI.Networking;
using FungleAPI.Role;
using FungleAPI.Role.Utilities;
using FungleAPI.Ship;
using FungleAPI.Teams;
using FungleAPI.Translation;
using FungleAPI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Crewmeleon.Roles
{
    public class ChameleonRole : CrewmateBase, ICustomRole
    {
        public CanvaBehaviour CanvaBehaviour;
        public ModdedTeam Team { get; } = ModdedTeamManager.Crewmates;
        public StringNames RoleName { get; } = ChameleonTranslation.ChameleonName;
        public StringNames RoleBlur { get; } = ChameleonTranslation.ChameleonBlur;
        public StringNames RoleBlurMed { get; } = ChameleonTranslation.ChameleonBlur;
        public StringNames RoleBlurLong { get; } = ChameleonTranslation.ChameleonBlur;
        public Color RoleColor { get; } = Color.gray;
        public RoleConfiguration Configuration => new RoleConfiguration(this)
        {
            HideInLobby = true,
        };

        public float proximityLevel;
        public float stopedTimer;

        public float updateNearbyImpostorsTime;

        public bool wasReveled;
        public bool timeUp;

        public HorizontalGauge Gauge;

        public void Start()
        {
            if (Player == null) return;

            CanvaBehaviour = Player.GetComponent<CanvaBehaviour>();

            Gauge = GameObject.Instantiate<HorizontalGauge>(GetTaskPrefab(TaskTypes.UploadData).MinigamePrefab.TryCast<UploadDataGame>().Gauge, Player.transform);
            Gauge.transform.localPosition = new Vector3(0, -0.7f, 0);
            Gauge.transform.localScale = new Vector3(0.65f, 1, 1);

            Gauge.gameObject.SetActive(false);
        }
        public void Update()
        {
            if (Player == null) return;

            if (!timeUp)
            {
                Player.cosmetics.ToggleHat(false);
                Player.cosmetics.TogglePetVisible(false);
                Player.cosmetics.ToggleVisor(false);
                Player.cosmetics.ToggleNameVisible(ChameleonModeSettings.GeneralSettings.ShowNames.BooleanValue && !ChameleonModeSettings.InfectionSettings.Infection.BooleanValue && PlayerControl.LocalPlayer.Data.Role.TeamType != RoleTeamTypes.Impostor);
                Player.cosmetics.skin.Visible = false;

                updateNearbyImpostorsTime += Time.deltaTime;
                if (updateNearbyImpostorsTime >= 0.05f)
                {
                    if (NearbySeekers())
                    {
                        proximityLevel += Time.deltaTime;
                        if (proximityLevel > ChameleonModeSettings.ChameleonSettings.FoundBar.FloatValue)
                        {
                            proximityLevel = ChameleonModeSettings.ChameleonSettings.FoundBar.FloatValue;
                        }

                        if (Player.AmOwner)
                        {
                            Gauge.gameObject.SetActive(true);
                            Gauge.Value = proximityLevel / ChameleonModeSettings.ChameleonSettings.FoundBar.FloatValue;
                        }
                    }
                    else
                    {
                        proximityLevel = 0;

                        if (Player.AmOwner)
                        {
                            Gauge.gameObject.SetActive(false);
                        }
                    }

                    updateNearbyImpostorsTime = 0;
                }

                UpdateStopTimer();

                CanvaBehaviour.OutlineActive = stopedTimer < ChameleonModeSettings.ChameleonSettings.StopOutline.FloatValue || proximityLevel >= ChameleonModeSettings.ChameleonSettings.FoundBar.FloatValue;

                if (CanvaBehaviour.OutlineActive && !wasReveled && !SeekerRole.SafeToKill.Contains(Player.Data))
                {
                    SeekerRole.SafeToKill.Add(Player.Data);
                }
                else if (!CanvaBehaviour.OutlineActive && wasReveled && SeekerRole.SafeToKill.Contains(Player.Data))
                {
                    SeekerRole.SafeToKill.Remove(Player.Data);
                }

                wasReveled = CanvaBehaviour.OutlineActive;
            }
        }
        public void OnDestroy()
        {
            Gauge?.gameObject.Destroy();

            if (Player == null) return;

            if (Player.AmOwner)
            {
                ChameleonHelper.PaintState = PaintState.None;
                if (ZoomButton.Zoom != null)
                {
                    ZoomButton.Zoom.gameObject.SetActive(false);
                }
            }

            Player.cosmetics.ToggleHat(true);
            Player.cosmetics.TogglePetVisible(true);
            Player.cosmetics.ToggleVisor(true);
            Player.cosmetics.ToggleNameVisible(true);
            Player.cosmetics.skin.Visible = true;

            if (SeekerRole.SafeToKill.Contains(Player.Data))
            {
                SeekerRole.SafeToKill.Remove(Player.Data);
            }
        }
        public void UpdateStopTimer()
        {
            if (Player.rigidbody2D.velocity == Vector2.zero)
            {
                stopedTimer += Time.deltaTime;
            }
            else
            {
                stopedTimer = 0;
            }
        }
        public void Reveal()
        {
            Player.moveable = false;
            Player.rigidbody2D.velocity = Vector2.zero;

            CanvaBehaviour.ForceShow = true;
            Player.cosmetics.ToggleNameVisible(true);

            ArrowBehaviour arrowBehaviour = GameObject.Instantiate(GetTaskPrefab(TaskTypes.FixWiring).SafeCast<NormalPlayerTask>().Arrow);
            arrowBehaviour.gameObject.SetActive(true);
            arrowBehaviour.MaxScale = 0.5f;
            arrowBehaviour.target = Player.transform.position;

            timeUp = true;
            CanvaBehaviour.OutlineActive = true;
        }
        
        public PlayerTask GetTaskPrefab(TaskTypes type)
        {
            return ShipPrefabLoader.SkeldPrefab.GetAllTasks().FirstOrDefault(t => t.TaskType == type);
        }
        public bool NearbySeekers()
        {
            return PlayerControl.AllPlayerControls.Any(p => p.Data.Role.TeamType == RoleTeamTypes.Impostor && Vector2.Distance(p.transform.position, Player.transform.position) <= ChameleonModeSettings.ChameleonSettings.Proximity.FloatValue);
        }
    }
}