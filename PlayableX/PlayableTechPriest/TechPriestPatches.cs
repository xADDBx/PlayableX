using HarmonyLib;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Root;
using Kingmaker.Blueprints;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Mechanics.Entities;
using Kingmaker.PubSubSystem.Core;
using Kingmaker.ResourceLinks;
using Kingmaker.UI.MVVM.VM.CharGen;
using Kingmaker.UnitLogic.Levelup.Selections.Doll;
using Kingmaker.UnitLogic.Levelup.Selections;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.UnitLogic.Progression.Features;
using Kingmaker.UnitLogic.Progression.Paths;
using Kingmaker.UnitLogic.Progression.Prerequisites;
using Kingmaker.UnitLogic;
using Kingmaker.View;
using Kingmaker;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingmaker.Visual.CharacterSystem;
using Code.GameCore.ElementsSystem;
using Owlcat.Runtime.Core;
using Kingmaker.UnitLogic.FactLogic;

namespace PlayableX.PlayableTechPriest;
public static class TechPriestPatches {
    public static bool CreateTechPriest = false;
    [HarmonyPatch(typeof(CharGenConfig), nameof(CharGenConfig.Create))]
    internal static class CharGenConfig_Create_Patch {
        [HarmonyPrefix]
        private static void Create(CharGenConfig.CharGenMode mode) {
            if (CreateTechPriest) {
                CharGenContext_GetOriginPath_Patch.isMercenary = mode == CharGenConfig.CharGenMode.NewCompanion;
            }
        }
    }
    [HarmonyPatch(typeof(CharGenContext), nameof(CharGenContext.GetOriginPath))]
    internal static class CharGenContext_GetOriginPath_Patch {
        internal static bool isMercenary = false;
        [HarmonyPostfix]
        private static void GetOriginPath(ref BlueprintOriginPath __result) {
            if (CreateTechPriest) {
                var copy = CopyBlueprint(__result);
                try {
                    var c = copy.Components.OfType<AddFeaturesToLevelUp>().Where(c => c.Group == FeatureGroup.ChargenOccupation).First();
                    var techPriestOccupation = ResourcesLibrary.BlueprintsCache.Load("777d9f9c570443b59120e78f2d9dd515") as BlueprintFeature;
                    c.m_Features = new[] { techPriestOccupation.ToReference<BlueprintFeatureReference>() }.AddRangeToArray(c.m_Features);
                    copy.Components[1] = c;
                    __result = copy;
                } catch (Exception e) {
                    Main.log.Log(e.ToString());
                }
            }
        }
        private static T CopyBlueprint<T>(T bp) where T : SimpleBlueprint {
            var writer = new StringWriter();
            var serializer = JsonSerializer.Create(Json.Settings);
            serializer.Serialize(writer, new BlueprintJsonWrapper(bp));
            return serializer.Deserialize<BlueprintJsonWrapper>(new JsonTextReader(new StringReader(writer.ToString()))).Data as T;
        }
    }
    [HarmonyPatch(typeof(Prerequisite), nameof(Prerequisite.Meet), [typeof(ElementsList), typeof(IBaseUnitEntity)])]
    internal static class Prerequisite_Meet_Patch {
        [HarmonyPostfix]
        private static void Meet(ref bool __result, Prerequisite __instance, IBaseUnitEntity unit) {
            var techPriestOccupation = ResourcesLibrary.BlueprintsCache.Load("777d9f9c570443b59120e78f2d9dd515") as BlueprintFeature;
            Feature feature = unit.ToBaseUnitEntity().Facts.Get(techPriestOccupation) as Feature;
            if (CreateTechPriest && feature != null) {
                var key = __instance.Owner.name;
                if ((key?.Contains("DarkestHour") ?? false) || (key?.Contains("MomentOfTriumph") ?? false)) {
                    __result = true;
                }
            }
        }
    }
    private static HashSet<string> femaleEEIds = new() { "95b61f949fe46bc43a4107a77fa10a97", "c256cd40ee105e44b8b67edd6f71784f", "d317d7fb22ff9824ab497a029b8e1c3b", "9991c5802950b6241991e51d61c45ed1", "fb7dbf6f0935fc1458c1a86488ce5de7" };
    private static HashSet<string> maleEEIds = new() { "26055e6f510e74442a881f0707b45f98", "f38850c1ac1b5cb4ebcb462198ebfed7", "9f77631259fb68344851d55c62536071", "fb7dbf6f0935fc1458c1a86488ce5de7", "fb7dbf6f0935fc1458c1a86488ce5de7" };
    [HarmonyPatch(typeof(CharGenContextVM), nameof(CharGenContextVM.CompleteCharGen))]
    internal static class CharGenContextVM_ComplteCharGen_Patch {
        [HarmonyPrefix]
        private static void CompleteCharGen(BaseUnitEntity resultUnit) {
            var techPriestOccupation = ResourcesLibrary.BlueprintsCache.Load("777d9f9c570443b59120e78f2d9dd515") as BlueprintFeature;
            var feature = resultUnit.Facts.Get(techPriestOccupation) as Feature;
            if (CreateTechPriest && feature != null) {
                TechPriestEntityPartStorage.perSave.AddClothes[resultUnit.UniqueId] = (resultUnit.Gender == Kingmaker.Blueprints.Base.Gender.Male ? maleEEIds : femaleEEIds).ToList();
                TechPriestEntityPartStorage.SavePerSaveSettings();
                var techPriestOccupationFeature = ResourcesLibrary.BlueprintsCache.Load("31d25d8b646c454a8fbc17bc8f775c2c") as BlueprintFeature;
                resultUnit.AddFact(techPriestOccupationFeature);
                var pascalFeatureList_Chapter1_Start = ResourcesLibrary.BlueprintsCache.Load("9c13849997a84548bd6825bfaf752816") as BlueprintFeature;
                var facts = (pascalFeatureList_Chapter1_Start?.Components.Get(0, null) as AddFacts)?.Facts;
                foreach (var feat in facts) {
                    resultUnit.AddFact(feat);
                }
                CreateTechPriest = false;
            }
        }
    }
    [HarmonyPatch(typeof(PartUnitProgression))]
    internal static class PartUnitProgression_Patch {
        [HarmonyPatch(nameof(PartUnitProgression.AddFeatureSelection))]
        [HarmonyPrefix]
        private static void AddFeatureSelection(ref BlueprintPath path) {
            if (CreateTechPriest && path is BlueprintOriginPath) {
                path = CharGenContext_GetOriginPath_Patch.isMercenary ? BlueprintCharGenRoot.Instance.NewCompanionCustomChargenPath : BlueprintCharGenRoot.Instance.NewGameCustomChargenPath;
            }
        }
        [HarmonyPatch(nameof(PartUnitProgression.AddPathRank))]
        [HarmonyPrefix]
        private static void AddPathRank(ref BlueprintPath path) {
            if (CreateTechPriest && path is BlueprintOriginPath) {
                path = CharGenContext_GetOriginPath_Patch.isMercenary ? BlueprintCharGenRoot.Instance.NewCompanionCustomChargenPath : BlueprintCharGenRoot.Instance.NewGameCustomChargenPath;
            }
        }
    }
    [HarmonyPatch(typeof(PartUnitViewSettings), nameof(PartUnitViewSettings.Instantiate))]
    internal static class PartUnitViewSettings_Instantiate_Patch {
        [HarmonyPrefix]
        private static void Instant_Pre(PartUnitViewSettings __instance) {
            DollData_CreateUnitView_Patch.context = __instance.Owner;
        }
        [HarmonyPostfix]
        private static void Instant_Post() {
            DollData_CreateUnitView_Patch.context = null;
        }
    }
    [HarmonyPatch(typeof(DollState), nameof(DollState.CollectMechanicEntities))]
    internal static class DollState_CollectMechanicEntities_Patch {
        [HarmonyPostfix]
        private static void CollectMechanicEntitities(DollState __instance, ref IEnumerable<EquipmentEntityLink> __result, BaseUnitEntity unit) {
            var techPriestOccupation = ResourcesLibrary.BlueprintsCache.Load("777d9f9c570443b59120e78f2d9dd515") as BlueprintFeature;
            Feature feature = unit.Facts.Get(techPriestOccupation) as Feature;
            if (CreateTechPriest && feature != null) {
                var ids = unit.Gender == Kingmaker.Blueprints.Base.Gender.Male ? maleEEIds : femaleEEIds;
                var eels = ids.Select(id => new EquipmentEntityLink() { AssetId = id });
                var res = __result.ToList();
                res.AddRange(eels);
                __result = res.AsEnumerable();
            }
        }
    }
    [HarmonyPatch(typeof(DollData), nameof(DollData.CreateUnitView))]
    internal static class DollData_CreateUnitView_Patch {
        internal static AbstractUnitEntity context = null;
        [HarmonyPostfix]
        private static void CreateUnitView(DollData __instance, ref UnitEntityView __result, bool savedEquipment) {
            if (TechPriestEntityPartStorage.perSave.AddClothes.TryGetValue(context.UniqueId, out var ees)) {
                Character component2 = __result.GetComponent<Character>();
                foreach (var eeId in ees) {
                    var eel = new EquipmentEntityLink() { AssetId = eeId };
                    var ee = eel.Load();
                    component2.AddEquipmentEntity(ee, savedEquipment);
                }
                __instance.ApplyRampIndices(component2, savedEquipment);
            }
        }
    }
}
