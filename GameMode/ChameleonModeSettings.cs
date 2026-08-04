using Crewmeleon.Essential;
using FungleAPI.GameModes;
using FungleAPI.GameOptions;
using FungleAPI.GameOptions.Options;
using FungleAPI.Translation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crewmeleon.GameMode
{
    public class ChameleonModeSettings : GameModeOptions
    {
        public class GeneralSettings : SettingsGroup
        {
            public override StringNames GroupName => StringNames.SettingsGeneral;
            public static ModdedNumberOption ChameleonSpeed = new ModdedNumberOption(ChameleonTranslation.ChameleonSpeed, 1, 0.5f, 3, 0.25f, null, false, NumberSuffixes.Multiplier);
            public static ModdedNumberOption SeekerSpeed = new ModdedNumberOption(ChameleonTranslation.SeekerSpeed, 1.5f, 0.5f, 3, 0.25f, null, false, NumberSuffixes.Multiplier);
            public static ModdedNumberOption SeekersCount = new ModdedNumberOption(ChameleonTranslation.SeekerCount, 2, 1, 10, 1, null, false, NumberSuffixes.None);
            public static ModdedNumberOption Time = new ModdedNumberOption(ChameleonTranslation.GameDuration, 150, 10, 800, 10);
            public static ModdedNumberOption HideTime = new ModdedNumberOption(ChameleonTranslation.HideTime, 15, 0, 550, 5);
            public static ModdedToggleOption ShowNames = new ModdedToggleOption(ChameleonTranslation.ShowNames, true);
            public static ModdedToggleOption ChatDuringGame = new ModdedToggleOption(ChameleonTranslation.ChatActive, true);
        }
        public class InfectionSettings : SettingsGroup
        {
            public override StringNames GroupName => ChameleonTranslation.InfectionText;
            public static ModdedToggleOption Infection = new ModdedToggleOption(ChameleonTranslation.InfectionActive, false);
            public static ModdedToggleOption HidePlayers = new ModdedToggleOption(ChameleonTranslation.HidePlayers, true);
        }
        public class ChameleonSettings : SettingsGroup
        {
            public override StringNames GroupName => ChameleonTranslation.ChameleonsText;
            public static ModdedNumberOption StopOutline = new ModdedNumberOption(ChameleonTranslation.CamouflageTime, 2.5f, 1, 10, 0.5f);
            public static ModdedNumberOption Proximity = new ModdedNumberOption(ChameleonTranslation.MaxProximity, 1.5f, 0.2f, 3, 0.1f, null, false, NumberSuffixes.None);
            public static ModdedNumberOption FoundBar = new ModdedNumberOption(ChameleonTranslation.DangerBarLevel, 0.8f, 0.2f, 2, 0.1f, null, false, NumberSuffixes.Multiplier);
        }
    }
}
