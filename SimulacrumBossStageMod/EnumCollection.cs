using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SimulacrumBossStageMod
{
    public class EnumCollection
    {
        public enum ArtifactEnum
        {
            None,
            Chaos,
            Command,
            Death,
            Dissonance,
            Enigma,
            Evolution,
            Frailty,
            Glass,
            Honor,
            Kin,
            Metamorphosis,
            Sacrifice,
            Soul,
            Spite,
            Swarms,
            Vengeance
        }

        public enum StageEnum
        {
            None,
            TitanicPlains,
            DistantRoost,
            WetlandAspect,
            AbandonedAqueduct,
            RallypointDelta,
            ScorchedAcres,
            AbyssalDepths,
            SirensCall,
            GildedCoast,
            MomentFractured,
            Bazaar,
            VoidFields,
            MomentWhole,
            SkyMeadow,
            BullwarksAmbry,
            Commencement,
            SunderedGrove,
            VoidLocus,
            Planetarium,
            AphelianSanctuary,
            SimulacrumAbandonedAquaduct,
            SimulacrumAbyssalDepths,
            SimulacrumAphelianSanctuary,
            SimulacrumCommencement,
            SimulacrumRallypointDelta,
            SimulacrumSkyMeadow,
            SimulacrumTitanicPlains,
            SiphonedForest,
            SulfurPools
        }

        public static Dictionary<StageEnum, List<string>> SceneNames = new Dictionary<StageEnum, List<string>>()
        {
            { StageEnum.TitanicPlains, new List<string>(){ "golemplains", "golemplains2", "golemplains trailer" } },
            { StageEnum.DistantRoost, new List<string>(){ "blackbeach", "blackbeach2", "blackbeachTest" } },
            { StageEnum.WetlandAspect, new List<string>(){ "foggyswamp" } },
            { StageEnum.AbandonedAqueduct, new List<string>(){ "goolake" } },
            { StageEnum.RallypointDelta, new List<string>(){ "frozenwall" } },
            { StageEnum.ScorchedAcres, new List<string>(){ "wispgraveyard" } },
            { StageEnum.AbyssalDepths, new List<string>(){ "dampcavesimple" } },
            { StageEnum.SirensCall, new List<string>(){ "shipgraveyard" } },
            { StageEnum.GildedCoast, new List<string>(){ "goldshores" } },
            { StageEnum.MomentFractured, new List<string>(){ "mysteryspace" } },
            { StageEnum.Bazaar, new List<string>(){ "bazaar" } },
            { StageEnum.VoidFields, new List<string>(){ "arena" } },
            { StageEnum.MomentWhole, new List<string>(){ "limbo" } },
            { StageEnum.SkyMeadow, new List<string>(){ "skymeadow" } },
            { StageEnum.BullwarksAmbry, new List<string>(){ "artifactworld" } },
            { StageEnum.Commencement, new List<string>(){ "moon", "moon2" } },
            { StageEnum.SunderedGrove, new List<string>(){ "rootjungle" } },
            { StageEnum.VoidLocus, new List<string>(){ "voidstage" } },
            { StageEnum.Planetarium, new List<string>(){ "voidraid" } },
            { StageEnum.AphelianSanctuary, new List<string>(){ "ancientloft" } },
            { StageEnum.SimulacrumAbandonedAquaduct, new List<string>(){ "itgoolake" } },
            { StageEnum.SimulacrumAbyssalDepths, new List<string>(){ "itdampcave" } },
            { StageEnum.SimulacrumAphelianSanctuary, new List<string>(){ "itancientloft" } },
            { StageEnum.SimulacrumCommencement, new List<string>(){ "itmoon" } },
            { StageEnum.SimulacrumRallypointDelta, new List<string>(){ "itfrozenwall" } },
            { StageEnum.SimulacrumSkyMeadow, new List<string>(){ "itskymeadow" } },
            { StageEnum.SimulacrumTitanicPlains, new List<string>(){ "itgolemplains" } },
            { StageEnum.SiphonedForest, new List<string>(){ "snowyforest" } },
            { StageEnum.SulfurPools, new List<string>(){ "sulfurpools" } },
        };

        public static bool IsSimulacrumStage(String name)
        {
            if (GetStageName(StageEnum.SimulacrumAbandonedAquaduct) == name) return true;
            if (GetStageName(StageEnum.SimulacrumAbyssalDepths) == name) return true;
            if (GetStageName(StageEnum.SimulacrumAphelianSanctuary) == name) return true;
            if (GetStageName(StageEnum.SimulacrumCommencement) == name) return true;
            if (GetStageName(StageEnum.SimulacrumRallypointDelta) == name) return true;
            if (GetStageName(StageEnum.SimulacrumSkyMeadow) == name) return true;
            if (GetStageName(StageEnum.SimulacrumTitanicPlains) == name) return true;
            return false;
        }

        public static string GetStageName(StageEnum stageEnum)
        {
            return SceneNames[stageEnum].First();
        }

        public static ArtifactDef GetArtifactDef(ArtifactEnum artifacts)
        {
            switch (artifacts)
            {
                case ArtifactEnum.Chaos: return RoR2Content.Artifacts.FriendlyFire;
                case ArtifactEnum.Command: return RoR2Content.Artifacts.Command;
                case ArtifactEnum.Death: return RoR2Content.Artifacts.TeamDeath;
                case ArtifactEnum.Dissonance: return RoR2Content.Artifacts.MixEnemy;
                case ArtifactEnum.Enigma: return RoR2Content.Artifacts.Enigma;
                case ArtifactEnum.Evolution: return RoR2Content.Artifacts.MonsterTeamGainsItems;
                case ArtifactEnum.Frailty: return RoR2Content.Artifacts.WeakAssKnees;
                case ArtifactEnum.Glass: return RoR2Content.Artifacts.Glass;
                case ArtifactEnum.Honor: return RoR2Content.Artifacts.EliteOnly;
                case ArtifactEnum.Kin: return RoR2Content.Artifacts.SingleMonsterType;
                case ArtifactEnum.Metamorphosis: return RoR2Content.Artifacts.RandomSurvivorOnRespawn;
                case ArtifactEnum.Sacrifice: return RoR2Content.Artifacts.Sacrifice;
                case ArtifactEnum.Soul: return RoR2Content.Artifacts.WispOnDeath;
                case ArtifactEnum.Spite: return RoR2Content.Artifacts.Bomb;
                case ArtifactEnum.Swarms: return RoR2Content.Artifacts.Swarms;
                case ArtifactEnum.Vengeance: return RoR2Content.Artifacts.ShadowClone;
            }
            return null;
        }
    }
}
