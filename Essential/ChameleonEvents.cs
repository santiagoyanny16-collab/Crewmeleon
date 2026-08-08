using Crewmeleon.GameMode;
using Crewmeleon.Roles;
using FungleAPI.Event;
using FungleAPI.Event.Vanilla;
using FungleAPI.GameModes;
using FungleAPI.Ship;
using FungleAPI.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Crewmeleon.Essential
{
    public static class ChameleonEvents
    {
        [EventRegister]
        public static void OnIntroEnd(IntroEndEvent introEndEvent)
        {
            if (!GameManager.Instance.IsHideAndSeek() && GameModeManager.GetCurrentGameMode() is ChameleonGameMode chameleonGameMode)
            {
                chameleonGameMode.CanCount = true;
                foreach (PlayerControl playerControl in PlayerControl.AllPlayerControls)
                {
                    if (playerControl.Data.Role.Is(out SeekerRole seekerRole))
                    {
                        seekerRole.StartStun();
                    }
                }
            }
        }
        [EventRegister]
        public static void OnShip(ShipStartEvent shipStartEvent)
        {
            ChameleonHelper.PaintState = PaintState.None;
            if (!GameManager.Instance.IsHideAndSeek() && GameModeManager.GetCurrentGameMode() is ChameleonGameMode)
            {
                ShipStatus.Instance?.BreakEmergencyButton();
                if (ShipStatus.Instance.GetShipType() == ShipType.MiraHQ)
                {
                    GameObject.Find("SurvLogConsole")?.SetActive(false);
                }
            }
        }
    }
}
