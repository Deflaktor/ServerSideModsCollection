using R2API;
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
        public static T DecrementEnumValue<T>(T value) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }
            // Get the total number of enum values
            int enumCount = Enum.GetValues(typeof(T)).Length;

            // Calculate the previous enum value and wrap around if necessary
            int previousValue = ((int)(object)value - 1 + enumCount) % enumCount;

            // Cast the integer back to enum type and return
            return (T)(object)previousValue;
        }

        public static T IncrementEnumValue<T>(T value) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }

            // Get the total number of enum values
            int enumCount = Enum.GetValues(typeof(T)).Length;

            // Calculate the next enum value and wrap around if necessary
            int nextValue = ((int)(object)value + 1) % enumCount;

            // Cast the integer back to enum type and return
            return (T)(object)nextValue;
        }


        public enum BossEnum
        {
            None,
            AlloyVulture,
            AlloyWorshipUnit,
            AlphaConstruct,
            Aurelionite,
            Beetle,
            BeetleGuard,
            BeetleQueen,
            BighornBison,
            BlindPest,
            BlindVermin,
            BrassContraption,
            ClayApothecary,
            ClayDunestrider,
            ClayTemplar,
            ElderLemurian,
            Geep,
            Gip,
            Grandparent,
            GreaterWisp,
            Grovetender,
            Gup,
            HermitCrab,
            Imp,
            ImpOverlord,
            Jellyfish,
            Larva,
            Lemurian,
            LesserWisp,
            LunarChimeraExploder,
            LunarChimeraGolem,
            LunarChimeraWisp,
            MagmaWorm,
            MiniMushrum,
            MithrixPhase1,
    //        MithrixPhase3,
            MithrixPhase4,
            MithrixSimulacrum,
            OverloadingWorm,
            Parent,
            Scavenger,
            SolusControlUnit,
            SolusProbe,
            StoneGolem,
            StoneTitan,
            TwistedScavenger,
            VoidBarnacle,
            VoidDevastator,
            VoidJailer,
            VoidReaver,
            Voidling,
            VoidlingPhase1,
            VoidlingPhase2,
            VoidlingPhase3,
            WanderingVagrant,
            XiConstruct,
            //MajorConstruct,
        }

        public static Dictionary<BossEnum, string> BossNames = new Dictionary<BossEnum, string>()
        {
            { BossEnum.AlloyVulture, "RoR2/Base/Vulture/cscVulture.asset" },
            { BossEnum.AlloyWorshipUnit, "RoR2/Base/RoboBallBoss/cscSuperRoboBallBoss.asset" },
            { BossEnum.AlphaConstruct, "RoR2/DLC1/MajorAndMinorConstruct/cscMinorConstruct.asset" },
            { BossEnum.Aurelionite, "RoR2/Base/Titan/cscTitanGold.asset" },
            { BossEnum.Beetle, "RoR2/Base/Beetle/cscBeetle.asset" },
            { BossEnum.BeetleGuard, "RoR2/Base/Beetle/cscBeetleGuard.asset" },
            { BossEnum.BeetleQueen, "RoR2/Base/Beetle/cscBeetleQueen.asset" },
            { BossEnum.BighornBison, "RoR2/Base/Bison/cscBison.asset" },
            { BossEnum.BlindPest, "RoR2/DLC1/FlyingVermin/cscFlyingVermin.asset" },
            { BossEnum.BlindVermin, "RoR2/DLC1/Vermin/cscVermin.asset" },
            { BossEnum.BrassContraption, "RoR2/Base/Bell/cscBell.asset" },
            { BossEnum.ClayApothecary, "RoR2/DLC1/ClayGrenadier/cscClayGrenadier.asset" },
            { BossEnum.ClayDunestrider, "RoR2/Base/ClayBoss/cscClayBoss.asset" },
            { BossEnum.ClayTemplar, "RoR2/Base/ClayBruiser/cscClayBruiser.asset" },
            { BossEnum.ElderLemurian, "RoR2/Base/LemurianBruiser/cscLemurianBruiser.asset" },
            { BossEnum.Geep, "RoR2/DLC1/Gup/cscGeepBody.asset" },
            { BossEnum.Gip, "RoR2/DLC1/Gup/cscGipBody.asset" },
            { BossEnum.Grandparent, "RoR2/Base/Grandparent/cscGrandparent.asset" },
            { BossEnum.GreaterWisp, "RoR2/Base/GreaterWisp/cscGreaterWisp.asset" },
            { BossEnum.Grovetender, "RoR2/Base/Gravekeeper/cscGravekeeper.asset" },
            { BossEnum.Gup, "RoR2/DLC1/Gup/cscGupBody.asset" },
            { BossEnum.HermitCrab, "RoR2/Base/HermitCrab/cscHermitCrab.asset" },
            { BossEnum.Imp, "RoR2/Base/Imp/cscImp.asset" },
            { BossEnum.ImpOverlord, "RoR2/Base/ImpBoss/cscImpBoss.asset" },
            { BossEnum.Jellyfish, "RoR2/Base/Jellyfish/cscJellyfish.asset" },
            { BossEnum.Larva, "RoR2/DLC1/AcidLarva/cscAcidLarva.asset" },
            { BossEnum.Lemurian, "RoR2/Base/Lemurian/cscLemurian.asset" },
            { BossEnum.LesserWisp, "RoR2/Base/Wisp/cscLesserWisp.asset" },
            { BossEnum.LunarChimeraExploder, "RoR2/Base/LunarExploder/cscLunarExploder.asset" },
            { BossEnum.LunarChimeraGolem, "RoR2/Base/LunarGolem/cscLunarGolem.asset" },
            { BossEnum.LunarChimeraWisp, "RoR2/Base/LunarWisp/cscLunarWisp.asset" },
            { BossEnum.MagmaWorm, "RoR2/Base/MagmaWorm/cscMagmaWorm.asset" },
            { BossEnum.MiniMushrum, "RoR2/Base/MiniMushroom/cscMiniMushroom.asset" },
            { BossEnum.MithrixPhase1, "RoR2/Base/Brother/cscBrother.asset" },
//            { BossEnum.MithrixPhase3, "RoR2/Base/Brother/cscBrotherGlass.asset" }, // doesnt work
            { BossEnum.MithrixPhase4, "RoR2/Base/Brother/cscBrotherHurt.asset" },
            { BossEnum.MithrixSimulacrum, "RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/cscBrotherIT.asset" },
            { BossEnum.OverloadingWorm, "RoR2/Base/ElectricWorm/cscElectricWorm.asset" },
            { BossEnum.Parent, "RoR2/Base/Parent/cscParent.asset" },
            { BossEnum.Scavenger, "RoR2/Base/Scav/cscScav.asset" },
            { BossEnum.SolusControlUnit, "RoR2/Base/RoboBallBoss/cscRoboBallBoss.asset" },
            { BossEnum.SolusProbe, "RoR2/Base/RoboBallBoss/cscRoboBallMini.asset" },
            { BossEnum.StoneGolem, "RoR2/Base/Golem/cscGolem.asset" },
            { BossEnum.StoneTitan, "RoR2/Base/Titan/cscTitanGolemPlains.asset" },
            { BossEnum.TwistedScavenger, "RoR2/Base/Scav/cscScavBoss.asset" },
            { BossEnum.VoidBarnacle, "RoR2/DLC1/VoidBarnacle/cscVoidBarnacle.asset" },
            { BossEnum.VoidDevastator, "RoR2/DLC1/VoidMegaCrab/cscVoidMegaCrab.asset" },
            { BossEnum.VoidJailer, "RoR2/DLC1/VoidJailer/cscVoidJailer.asset" },
            { BossEnum.VoidReaver, "RoR2/Base/Nullifier/cscNullifier.asset" },
            { BossEnum.Voidling, "RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabBase.asset" },
            { BossEnum.VoidlingPhase1, "RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase1.asset" },
            { BossEnum.VoidlingPhase2, "RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase2.asset" },
            { BossEnum.VoidlingPhase3, "RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase3.asset" },
            { BossEnum.WanderingVagrant, "RoR2/Base/Vagrant/cscVagrant.asset" },
            { BossEnum.XiConstruct, "RoR2/DLC1/MajorAndMinorConstruct/cscMegaConstruct.asset" },
            //{ BossEnum.MajorConstruct, "RoR2/DLC1/MajorAndMinorConstruct/cscMajorConstruct.asset" }, some kind of tower enemy
        };

        public enum EliteEnum
        {
            None,
            Blazing,
            Overloading,
            Glacial,
            Mending,
            Malachite,
            Celestine,
            Perfected,
            Voidtouched
        }

        public static Dictionary<EliteEnum, EliteDef> EliteDefs = new Dictionary<EliteEnum, EliteDef>()
        {
            { EliteEnum.Blazing, EliteCatalog.eliteDefs.FirstOrDefault(obj => obj.name == "edFire") },
            { EliteEnum.Overloading, EliteCatalog.eliteDefs.FirstOrDefault(obj => obj.name == "edLightning") },
            { EliteEnum.Glacial, EliteCatalog.eliteDefs.FirstOrDefault(obj => obj.name == "edIce") },
            { EliteEnum.Mending, EliteCatalog.eliteDefs.FirstOrDefault(obj => obj.name == "edEarth") },
            { EliteEnum.Malachite, EliteCatalog.eliteDefs.FirstOrDefault(obj => obj.name == "edPoison") },
            { EliteEnum.Celestine, EliteCatalog.eliteDefs.FirstOrDefault(obj => obj.name == "edHaunted") },
            { EliteEnum.Perfected, EliteCatalog.eliteDefs.FirstOrDefault(obj => obj.name == "edLunar") },
            { EliteEnum.Voidtouched, EliteCatalog.eliteDefs.FirstOrDefault(obj => obj.name == "edVoid") }
            // edFire
            // edFireHonor
            // edHaunted
            // edIce
            // edIceHonor
            // edLightning
            // edLightningHonor
            // edLunar
            // edPoison
            // edGold
            // edEarth
            // edEarthHonor
            // edSecretSpeed
            // edVoid
        };

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
