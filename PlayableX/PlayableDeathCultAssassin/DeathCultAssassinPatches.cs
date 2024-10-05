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

namespace PlayableX.DeathCultAssassin;
public static class DeathCultAssassinPatches {
    public static bool CreateDeathCultAssassin = false;
    [HarmonyPatch(typeof(CharGenConfig), nameof(CharGenConfig.Create))]
    internal static class CharGenConfig_Create_Patch {
        [HarmonyPrefix]
        private static void Create(CharGenConfig.CharGenMode mode) {
            if (CreateDeathCultAssassin) {
                CharGenContext_GetOriginPath_Patch.isMercenary = mode == CharGenConfig.CharGenMode.NewCompanion;
            }
        }
    }
    [HarmonyPatch(typeof(CharGenContext), nameof(CharGenContext.GetOriginPath))]
    internal static class CharGenContext_GetOriginPath_Patch {
        internal static bool isMercenary = false;
        [HarmonyPostfix]
        private static void GetOriginPath(ref BlueprintOriginPath __result) {
            if (CreateDeathCultAssassin) {
                var copy = CopyBlueprint(__result);
                try {
                    var c = copy.Components.OfType<AddFeaturesToLevelUp>().Where(c => c.Group == FeatureGroup.ChargenOccupation).First();
                    var deathCultAssassinOccupation = ResourcesLibrary.BlueprintsCache.Load("9b090810169e4a42b22afd5995d3720d") as BlueprintFeature;
                    c.m_Features = new[] { deathCultAssassinOccupation.ToReference<BlueprintFeatureReference>() }.AddRangeToArray(c.m_Features);
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
            var deathCultAssassinOccupation = ResourcesLibrary.BlueprintsCache.Load("9b090810169e4a42b22afd5995d3720d") as BlueprintFeature;
            Feature feature = unit.ToBaseUnitEntity().Facts.Get(deathCultAssassinOccupation) as Feature;
            if (CreateDeathCultAssassin && feature != null) {
                var key = __instance.Owner.name;
                if ((key?.Contains("DarkestHour") ?? false) || (key?.Contains("MomentOfTriumph") ?? false)) {
                    __result = true;
                }
            }
        }
    }
    private static HashSet<string> femaleEEIds = new() { "cdcdd7b841efd4f41b5109f427af116d", "5533540f6a72c05468a0106c725ff75f", "3ffcd55f4434ea34daafc7f51dc6bc5d", "83d8b9c3568250f4fb0bdb63661d80f6", "08e1d6fc4f34ed44790b8db8c49a2cbc", "befba159eb4bafa44a9070f1f5a7870d" };
    private static HashSet<string> maleEEIds = new() { "bddf86de1e2148d4a9e46225bc01c7f7", "01f3aa7170fd9d444944f05c48b6dae5", "84cac5fc855afcd49829f7ee1c2e01f4", "3b123bf06519e3947926597d68d4de63", "f884dd1554a87cc478694927f7ab2829", "8d4aa7416d1a9384d8dda41fd00d73e4" };
    [HarmonyPatch(typeof(CharGenContextVM), nameof(CharGenContextVM.CompleteCharGen))]
    internal static class CharGenContextVM_ComplteCharGen_Patch {
        [HarmonyPrefix]
        private static void CompleteCharGen(BaseUnitEntity resultUnit) {
            var deathCultAssassinOccupation = ResourcesLibrary.BlueprintsCache.Load("9b090810169e4a42b22afd5995d3720d") as BlueprintFeature;
            var feature = resultUnit.Facts.Get(deathCultAssassinOccupation) as Feature;
            if (CreateDeathCultAssassin && feature != null) {
                DeathCultAssassinEntityPartStorage.perSave.AddClothes[resultUnit.UniqueId] = (resultUnit.Gender == Kingmaker.Blueprints.Base.Gender.Male ? maleEEIds : femaleEEIds).ToList();
                DeathCultAssassinEntityPartStorage.SavePerSaveSettings();
                CreateDeathCultAssassin = false;
            }
        }
    }
    [HarmonyPatch(typeof(PartUnitProgression))]
    internal static class PartUnitProgression_Patch {
        [HarmonyPatch(nameof(PartUnitProgression.AddFeatureSelection))]
        [HarmonyPrefix]
        private static void AddFeatureSelection(ref BlueprintPath path) {
            if (CreateDeathCultAssassin && path is BlueprintOriginPath) {
                path = CharGenContext_GetOriginPath_Patch.isMercenary ? BlueprintCharGenRoot.Instance.NewCompanionCustomChargenPath : BlueprintCharGenRoot.Instance.NewGameCustomChargenPath;
            }
        }
        [HarmonyPatch(nameof(PartUnitProgression.AddPathRank))]
        [HarmonyPrefix]
        private static void AddPathRank(ref BlueprintPath path) {
            if (CreateDeathCultAssassin && path is BlueprintOriginPath) {
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
            var deathCultAssassinOccupation = ResourcesLibrary.BlueprintsCache.Load("9b090810169e4a42b22afd5995d3720d") as BlueprintFeature;
            Feature feature = unit.Facts.Get(deathCultAssassinOccupation) as Feature;
            if (CreateDeathCultAssassin && feature != null) {
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
            if (DeathCultAssassinEntityPartStorage.perSave.AddClothes.TryGetValue(context.UniqueId, out var ees)) {
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
