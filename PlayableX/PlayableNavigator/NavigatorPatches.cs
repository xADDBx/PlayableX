using HarmonyLib;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Blueprints.Root.Strings;
using Kingmaker.Blueprints.Root;
using Kingmaker.Blueprints;
using Kingmaker.Code.UI.MVVM.VM.GroupChanger;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Mechanics.Entities;
using Kingmaker.ResourceLinks;
using Kingmaker.UI.MVVM.VM.CharGen.Phases.Appearance.Pages;
using Kingmaker.UI.MVVM.VM.CharGen.Phases.Appearance;
using Kingmaker.UI.MVVM.VM.CharGen;
using Kingmaker.UnitLogic.Levelup.Selections.Doll;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.UnitLogic.Progression.Paths;
using Kingmaker.UnitLogic.Progression.Prerequisites;
using Kingmaker.UnitLogic;
using Kingmaker.View;
using Kingmaker.Visual.CharacterSystem;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Code.GameCore.ElementsSystem;
using Kingmaker.PubSubSystem.Core;

namespace PlayableX.PlayableNavigator; 
public static class NavigatorPatches {
    public static bool CreateNavigator = false;
    [HarmonyPatch(typeof(CharGenConfig), nameof(CharGenConfig.Create))]
    internal static class CharGenConfig_Create_Patch {
        [HarmonyPrefix]
        private static void Create(CharGenConfig.CharGenMode mode, ref CharGenConfig.CharGenCompanionType companionType) {
            if (CreateNavigator) {
                companionType = CharGenConfig.CharGenCompanionType.Navigator;
                CharGenContext_GetOriginPath_Patch.needCustomBP = mode == CharGenConfig.CharGenMode.NewGame;
            }
        }
    }
    [HarmonyPatch(typeof(CharGenContext), nameof(CharGenContext.GetOriginPath))]
    internal static class CharGenContext_GetOriginPath_Patch {
        internal static bool needCustomBP = false;
        [HarmonyPostfix]
        private static void GetOriginPath(ref BlueprintOriginPath __result) {
            if (CreateNavigator && needCustomBP) {
                var navi = ResourcesLibrary.BlueprintsCache.Load("bf7b6a4da7fe4b69accac3506f0dd561") as BlueprintOriginPath;
                var norm = BlueprintCharGenRoot.Instance.NewGameCustomChargenPath;
                var copy = CopyBlueprint(norm);
                copy.Components = new BlueprintComponent[8] { navi.Components[0], navi.Components[1], navi.Components[2],
                                                              navi.Components[3], navi.Components[4], navi.Components[5],
                                                              navi.Components[6], navi.Components[7] };
                // Homeworld            Occupation           Navigator
                copy.RankEntries = new BlueprintPath.RankEntry[16] { norm.RankEntries[0], norm.RankEntries[1], navi.RankEntries[2],
                                                                  // ImperialWorld        ForgeWorld           SanctionedPsyker
                                                                     norm.RankEntries[2], norm.RankEntries[3], norm.RankEntries[4], 
                                                                  // DarkestHour          MomentOfTriumph      CareerPath
                                                                     norm.RankEntries[5], norm.RankEntries[6], norm.RankEntries[7], 
                                                                  // StatAdvancement      CharacterName        SelectionDoll   
                                                                     norm.RankEntries[8], norm.RankEntries[9], norm.RankEntries[10], 
                                                                  // SelectionPortrait     SelectionShip         SelectionVoice
                                                                     norm.RankEntries[11], norm.RankEntries[12], norm.RankEntries[13], 
                                                                  // Selection Gender
                                                                     norm.RankEntries[14]};
                copy.Ranks = 16;
                __result = copy;
            }
        }
        private static T CopyBlueprint<T>(T bp) where T : SimpleBlueprint {
            var writer = new StringWriter();
            var serializer = JsonSerializer.Create(Json.Settings);
            serializer.Serialize(writer, new BlueprintJsonWrapper(bp));
            return serializer.Deserialize<BlueprintJsonWrapper>(new JsonTextReader(new StringReader(writer.ToString()))).Data as T;
        }
    }
    [HarmonyPatch(typeof(CharGenAppearanceComponentAppearancePhaseVM), nameof(CharGenAppearanceComponentAppearancePhaseVM.IsPageEnabled))]
    internal static class CharGenAppearanceComponentAppearancePhaseVM_IsPageEnabled_Patch {
        [HarmonyPostfix]
        private static void IsPageEnabled(ref bool __result, CharGenAppearancePageType pageType) {
            if (CreateNavigator && pageType == CharGenAppearancePageType.NavigatorMutations) __result = true;
        }
    }
    [HarmonyPatch(typeof(Prerequisite), nameof(Prerequisite.Meet))]//, [typeof(ElementsList), typeof(IBaseUnitEntity)])]
    internal static class Prerequisite_Meet_Patch {
        [HarmonyPostfix]
        private static void Meet(ref bool __result, Prerequisite __instance) {
            if (CreateNavigator && CharGenContext_GetOriginPath_Patch.needCustomBP) {
                var key = __instance.Owner.name;
                if ((key?.Contains("DarkestHour") ?? false) || (key?.Contains("MomentOfTriumph") ?? false)) {
                    __result = true;
                }
            }
        }
    }
    [HarmonyPatch(typeof(CharGenContextVM), nameof(CharGenContextVM.CompleteCharGen))]
    internal static class CharGenContextVM_ComplteCharGen_Patch {
        [HarmonyPrefix]
        private static void CompleteCharGen(BaseUnitEntity resultUnit) {
            if (CreateNavigator) {
                if (CharGenContext_GetOriginPath_Patch.needCustomBP) {
                    NavigatorEntityPartStorage.perSave.AddClothes[resultUnit.UniqueId] = resultUnit.Gender == Kingmaker.Blueprints.Base.Gender.Male ? "0211d3356ed961f478207c6d181d5e70" : "29b483e81833c25479bb295189afef4a";
                    NavigatorEntityPartStorage.SavePerSaveSettings();
                }
                CreateNavigator = false;
            }
        }
    }
    [HarmonyPatch(typeof(PartUnitProgression))]
    internal static class PartUnitProgression_Patch {
        [HarmonyPatch(nameof(PartUnitProgression.AddFeatureSelection))]
        [HarmonyPrefix]
        private static void AddFeatureSelection(ref BlueprintPath path) {
            if (CreateNavigator && CharGenContext_GetOriginPath_Patch.needCustomBP && path is BlueprintOriginPath) {
                path = BlueprintCharGenRoot.Instance.NewGameCustomChargenPath;
            }
        }
        [HarmonyPatch(nameof(PartUnitProgression.AddPathRank))]
        [HarmonyPrefix]
        private static void AddPathRank(ref BlueprintPath path) {
            if (CreateNavigator && CharGenContext_GetOriginPath_Patch.needCustomBP && path is BlueprintOriginPath) {
                path = BlueprintCharGenRoot.Instance.NewGameCustomChargenPath;
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
        [HarmonyPrefix]
        private static void CollectMechanicEntitities(DollState __instance) {
            if (CreateNavigator && CharGenContext_GetOriginPath_Patch.needCustomBP) {
                __instance.m_EquipmentEntities.Add(ResourcesLibrary.BlueprintsCache.Load("3afcd2d9ccb24e85844857ba852c1d88") as KingmakerEquipmentEntity);
            }
        }
    }
    [HarmonyPatch(typeof(DollData), nameof(DollData.CreateUnitView))]
    internal static class DollData_CreateUnitView_Patch {
        internal static AbstractUnitEntity context = null;
        [HarmonyPostfix]
        private static void CreateUnitView(DollData __instance, ref UnitEntityView __result, bool savedEquipment) {
            if (NavigatorEntityPartStorage.perSave.AddClothes.TryGetValue(context.UniqueId, out var id)) {
                Character component2 = __result.GetComponent<Character>();
                var eel = new EquipmentEntityLink() { AssetId = id };
                var ee = eel.Load();
                component2.AddEquipmentEntity(ee, savedEquipment);
                __instance.ApplyRampIndices(component2, savedEquipment);
            }
        }
    }
    [HarmonyPatch(typeof(GroupChangerVM), nameof(GroupChangerVM.CanMoveCharacterFromRemoteToParty))]
    internal static class GroupChangerVM_CanMoveCharacterFromRemoteToParty_Patch {
        [HarmonyPrefix]
        private static bool CanMoveCharacterFromRemoteToParty(GroupChangerVM __instance, ref string __result) {
            if (Main.settings.enableMoreThanOneNavigatorInParty) {
                if (__instance.m_PartyCharacter.Count == 6) {
                    __result = UIStrings.Instance.GroupChangerTexts.MaxGroupCountWarning;
                    return false;
                }
                __result = null;
                return false;
            }
            return true;
        }
    }
}
