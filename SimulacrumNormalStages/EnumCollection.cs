using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static RoR2.SceneCollection;

namespace SimulacrumNormalStages
{
    public class EnumCollection
    {
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

        private static SceneEntry createSceneEntry(StageEnum stageEnum, float weight = 1.0f)
        {
            var sceneDef = SceneCatalog.allStageSceneDefs.Where(s => s.cachedName == GetStageName(stageEnum)).First();
            return createSceneEntry(sceneDef, weight);
        }

        private static SceneEntry createSceneEntry(string stageName, float weight = 1.0f)
        {
            var sceneDef = SceneCatalog.allStageSceneDefs.Where(s => s.cachedName == stageName).First();
            return createSceneEntry(sceneDef, weight);
        }

        private static SceneEntry createSceneEntry(SceneDef sceneDef, float weight = 1.0f)
        {
            var sceneEntry = new SceneEntry();
            sceneEntry.sceneDef = sceneDef;
            sceneEntry.weight = weight;
            return sceneEntry;
        }

        public static List<SceneEntry> GetNormalStagesList()
        {
            var list = new List<SceneEntry>
            {
                createSceneEntry("golemplains", 0.5f),
                createSceneEntry("golemplains2", 0.5f),
                createSceneEntry("blackbeach", 0.5f),
                createSceneEntry("blackbeach2", 0.5f),
                createSceneEntry(StageEnum.WetlandAspect),
                createSceneEntry(StageEnum.AbandonedAqueduct),
                createSceneEntry(StageEnum.RallypointDelta),
                createSceneEntry(StageEnum.ScorchedAcres),
                createSceneEntry(StageEnum.AbyssalDepths),
                createSceneEntry(StageEnum.SirensCall),
                createSceneEntry(StageEnum.SkyMeadow),
                createSceneEntry(StageEnum.SunderedGrove),
                createSceneEntry(StageEnum.AphelianSanctuary),
                createSceneEntry(StageEnum.SiphonedForest),
                createSceneEntry(StageEnum.SulfurPools),
                createSceneEntry("moon2")
            };
            return list;
        }

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
    }
}
