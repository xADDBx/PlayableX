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

namespace PlayableX.PlayableRanger; 
public static class RangerPatches {
    public static bool CreateRanger = false;

    [HarmonyPatch(typeof(CharGenConfig), nameof(CharGenConfig.Create))]
    internal static class CharGenConfig_Create_Patch {
        [HarmonyPrefix]
        private static void Create(CharGenConfig.CharGenMode mode) {
            if (CreateRanger) {
                CharGenContext_GetOriginPath_Patch.isMercenary = mode == CharGenConfig.CharGenMode.NewCompanion;
            }
        }
    }
    [HarmonyPatch(typeof(CharGenContext), nameof(CharGenContext.GetOriginPath))]
    internal static class CharGenContext_GetOriginPath_Patch {
        internal static bool isMercenary = false;
        [HarmonyPostfix]
        private static void GetOriginPath(ref BlueprintOriginPath __result) {
            if (CreateRanger) {
                var copy = CopyBlueprint(__result);
                try {
                    var c = copy.Components.OfType<AddFeaturesToLevelUp>().Where(c => c.Group == FeatureGroup.ChargenOccupation).First();
                    var index = copy.Components.IndexOf(c);
                    var rangerOccupation = ResourcesLibrary.BlueprintsCache.Load("33497e0597e64570bb5cf78b19a95d96") as BlueprintFeature;
                    c.m_Features = new[] { rangerOccupation.ToReference<BlueprintFeatureReference>() }.AddRangeToArray(c.m_Features);
                    copy.Components[index] = c;
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
            //THIS SHOULD PROBABLY BE DIFFERENT... BUT WHAT???
            var rangerOccupation = ResourcesLibrary.BlueprintsCache.Load("33497e0597e64570bb5cf78b19a95d96") as BlueprintFeature;
            Feature feature = unit.ToBaseUnitEntity().Facts.Get(rangerOccupation) as Feature;
            if (CreateRanger && feature != null) {
                var key = __instance.Owner.name;
                if ((key?.Contains("DarkestHour") ?? false) || (key?.Contains("MomentOfTriumph") ?? false)) {
                    __result = true;
                }
            }
        }
    }
    // Find the eldar clothes. The best I could find is ranger armour, lets hope that works
    // Also adding the eldar body KEE, I hope I understood it correctly
    // Yrliet
    private static List<string> EEIds = new() { };
    [HarmonyPatch(typeof(CharGenContextVM), nameof(CharGenContextVM.CompleteCharGen))]
    internal static class CharGenContextVM_ComplteCharGen_Patch {
        [HarmonyPrefix]
        private static void CompleteCharGen(BaseUnitEntity resultUnit) {
            //THIS SHOULD PROBABLY BE DIFFERENT... BUT WHAT???
            var rangerOccupation = ResourcesLibrary.BlueprintsCache.Load("33497e0597e64570bb5cf78b19a95d96") as BlueprintFeature;
            Feature feature = resultUnit.Facts.Get(rangerOccupation) as Feature;
            if (CreateRanger && feature != null) {
                RangerEntityPartStorage.perSave.AddClothes[resultUnit.UniqueId] = EEIds;
                RangerEntityPartStorage.SavePerSaveSettings();
                var yrlietFeatureList = ResourcesLibrary.BlueprintsCache.Load("12e72ea7bb6b491fa25f93b2771132ff") as BlueprintFeature;
                var facts = (yrlietFeatureList?.Components.Get(0, null) as AddFacts)?.Facts;
                foreach (var feat in facts) {
                    resultUnit.AddFact(feat);
                }
                var aeldariArmorFeature = ResourcesLibrary.BlueprintsCache.Load("590cb5c394ee4b5eb213121c96063dec") as BlueprintFeature;
                resultUnit.AddFact(aeldariArmorFeature);
                CreateRanger = false;
            }
        }
    }
    [HarmonyPatch(typeof(PartUnitProgression))]
    internal static class PartUnitProgression_Patch {
        [HarmonyPatch(nameof(PartUnitProgression.AddFeatureSelection))]
        [HarmonyPrefix]
        private static void AddFeatureSelection(ref BlueprintPath path) {
            if (CreateRanger && path is BlueprintOriginPath) {
                path = CharGenContext_GetOriginPath_Patch.isMercenary ? BlueprintCharGenRoot.Instance.NewCompanionCustomChargenPath : BlueprintCharGenRoot.Instance.NewGameCustomChargenPath;
            }
        }
        [HarmonyPatch(nameof(PartUnitProgression.AddPathRank))]
        [HarmonyPrefix]
        private static void AddPathRank(ref BlueprintPath path) {
            if (CreateRanger && path is BlueprintOriginPath) {
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
            //THIS SHOULD PROBABLY BE DIFFERENT... BUT WHAT???
            var rangerBackLoreFeature = ResourcesLibrary.BlueprintsCache.Load("b8431cb469fb46498b91ec35fda10681") as BlueprintFeature;
            Feature feature = unit.Facts.Get(rangerBackLoreFeature) as Feature;
            if (CreateRanger && feature != null) {
                var eels = EEIds.Select(id => new EquipmentEntityLink() { AssetId = id });
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
            if (RangerEntityPartStorage.perSave.AddClothes.TryGetValue(context.UniqueId, out var ees)) {
                Character component2 = __result.GetComponent<Character>();
                foreach (var eeId in ees) {
                    var eel = new EquipmentEntityLink() { AssetId = eeId };
                    var ee = eel.Load();
                    if (!component2.EquipmentEntities.Where(e => e.name == ee.name).Any()) {
                        component2.AddEquipmentEntity(ee, savedEquipment);
                    }
                }
                __instance.ApplyRampIndices(component2, savedEquipment);
            }
        }
    }
}
