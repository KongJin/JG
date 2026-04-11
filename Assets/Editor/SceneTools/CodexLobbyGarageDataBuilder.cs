#if UNITY_EDITOR
using System;
using System.IO;
using Features.Unit.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace ProjectSD.EditorTools.SceneTools
{
    internal static class CodexLobbyGarageDataBuilder
    {
        private const string GarageDataRoot = "Assets/Data/Garage";
        private const string ModuleCatalogPath = GarageDataRoot + "/ModuleCatalog.asset";

        internal static ModuleCatalog EnsureModuleCatalog()
        {
            EnsureFolder("Assets/Data");
            EnsureFolder(GarageDataRoot);
            EnsureFolder(GarageDataRoot + "/Traits");
            EnsureFolder(GarageDataRoot + "/Frames");
            EnsureFolder(GarageDataRoot + "/Firepower");
            EnsureFolder(GarageDataRoot + "/Mobility");

            var bulwark = LoadOrCreateAsset(GarageDataRoot + "/Traits/Trait_Bulwark.asset", () => ScriptableObject.CreateInstance<PassiveTraitData>());
            SetString(bulwark, "traitId", "trait_bulwark");
            SetString(bulwark, "displayName", "Bulwark Core");
            SetEnumIndex(bulwark, "strength", 1);
            SetString(bulwark, "description", "Raises base durability for front-line frames.");

            var overclock = LoadOrCreateAsset(GarageDataRoot + "/Traits/Trait_Overclock.asset", () => ScriptableObject.CreateInstance<PassiveTraitData>());
            SetString(overclock, "traitId", "trait_overclock");
            SetString(overclock, "displayName", "Overclock");
            SetEnumIndex(overclock, "strength", 0);
            SetString(overclock, "description", "Keeps agile frames cheap to deploy.");

            var flux = LoadOrCreateAsset(GarageDataRoot + "/Traits/Trait_Flux.asset", () => ScriptableObject.CreateInstance<PassiveTraitData>());
            SetString(flux, "traitId", "trait_flux");
            SetString(flux, "displayName", "Flux Relay");
            SetEnumIndex(flux, "strength", 2);
            SetString(flux, "description", "Supports expensive hybrid builds with extra utility.");

            var bastion = LoadOrCreateAsset(GarageDataRoot + "/Frames/Frame_Bastion.asset", () => ScriptableObject.CreateInstance<UnitFrameData>());
            SetString(bastion, "frameId", "frame_bastion");
            SetString(bastion, "displayName", "Bastion");
            SetFloat(bastion, "baseHp", 640f);
            SetFloat(bastion, "baseMoveRange", 3f);
            SetFloat(bastion, "baseAttackSpeed", 0.85f);
            SetObject(bastion, "passiveTrait", bulwark);

            var striker = LoadOrCreateAsset(GarageDataRoot + "/Frames/Frame_Striker.asset", () => ScriptableObject.CreateInstance<UnitFrameData>());
            SetString(striker, "frameId", "frame_striker");
            SetString(striker, "displayName", "Striker");
            SetFloat(striker, "baseHp", 520f);
            SetFloat(striker, "baseMoveRange", 5f);
            SetFloat(striker, "baseAttackSpeed", 1.1f);
            SetObject(striker, "passiveTrait", overclock);

            var relay = LoadOrCreateAsset(GarageDataRoot + "/Frames/Frame_Relay.asset", () => ScriptableObject.CreateInstance<UnitFrameData>());
            SetString(relay, "frameId", "frame_relay");
            SetString(relay, "displayName", "Relay");
            SetFloat(relay, "baseHp", 560f);
            SetFloat(relay, "baseMoveRange", 4f);
            SetFloat(relay, "baseAttackSpeed", 1f);
            SetObject(relay, "passiveTrait", flux);

            var scatter = LoadOrCreateAsset(GarageDataRoot + "/Firepower/Firepower_Scatter.asset", () => ScriptableObject.CreateInstance<FirepowerModuleData>());
            SetString(scatter, "moduleId", "fire_scatter");
            SetString(scatter, "displayName", "Scatter Cannon");
            SetFloat(scatter, "attackDamage", 32f);
            SetFloat(scatter, "attackSpeed", 1.25f);
            SetFloat(scatter, "range", 3f);
            SetString(scatter, "description", "Short range burst weapon for melee rush builds.");

            var pulse = LoadOrCreateAsset(GarageDataRoot + "/Firepower/Firepower_Pulse.asset", () => ScriptableObject.CreateInstance<FirepowerModuleData>());
            SetString(pulse, "moduleId", "fire_pulse");
            SetString(pulse, "displayName", "Pulse Rifle");
            SetFloat(pulse, "attackDamage", 21f);
            SetFloat(pulse, "attackSpeed", 1.15f);
            SetFloat(pulse, "range", 4.5f);
            SetString(pulse, "description", "Balanced rifle for hybrid engagement ranges.");

            var rail = LoadOrCreateAsset(GarageDataRoot + "/Firepower/Firepower_Rail.asset", () => ScriptableObject.CreateInstance<FirepowerModuleData>());
            SetString(rail, "moduleId", "fire_rail");
            SetString(rail, "displayName", "Rail Lance");
            SetFloat(rail, "attackDamage", 48f);
            SetFloat(rail, "attackSpeed", 0.72f);
            SetFloat(rail, "range", 8.5f);
            SetString(rail, "description", "Long range option for backline and siege builds.");

            var treads = LoadOrCreateAsset(GarageDataRoot + "/Mobility/Mobility_Treads.asset", () => ScriptableObject.CreateInstance<MobilityModuleData>());
            SetString(treads, "moduleId", "mob_treads");
            SetString(treads, "displayName", "Siege Treads");
            SetFloat(treads, "hpBonus", 260f);
            SetFloat(treads, "moveRange", 2.5f);
            SetFloat(treads, "anchorRange", 2f);
            SetString(treads, "description", "Heavy but stable. Great for long range artillery.");

            var vector = LoadOrCreateAsset(GarageDataRoot + "/Mobility/Mobility_Vector.asset", () => ScriptableObject.CreateInstance<MobilityModuleData>());
            SetString(vector, "moduleId", "mob_vector");
            SetString(vector, "displayName", "Vector Legs");
            SetFloat(vector, "hpBonus", 160f);
            SetFloat(vector, "moveRange", 3.5f);
            SetFloat(vector, "anchorRange", 4f);
            SetString(vector, "description", "Mid-line option suited for hybrid pressure.");

            var burst = LoadOrCreateAsset(GarageDataRoot + "/Mobility/Mobility_Burst.asset", () => ScriptableObject.CreateInstance<MobilityModuleData>());
            SetString(burst, "moduleId", "mob_burst");
            SetString(burst, "displayName", "Burst Thrusters");
            SetFloat(burst, "hpBonus", 120f);
            SetFloat(burst, "moveRange", 5.5f);
            SetFloat(burst, "anchorRange", 6.5f);
            SetString(burst, "description", "Fast chassis for dive or flanking melee builds.");

            var catalog = LoadOrCreateAsset(ModuleCatalogPath, () => ScriptableObject.CreateInstance<ModuleCatalog>());
            SetObjectArray(catalog, "unitFrames", bastion, striker, relay);
            SetObjectArray(catalog, "firepowerModules", scatter, pulse, rail);
            SetObjectArray(catalog, "mobilityModules", treads, vector, burst);

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return catalog;
        }

        internal static void SetObject(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Property {propertyName} was not found on {target.GetType().Name}");

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetObjectArray(UnityEngine.Object target, string propertyName, params UnityEngine.Object[] values)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null || !property.isArray)
                throw new InvalidOperationException($"Array property {propertyName} was not found on {target.GetType().Name}");

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetString(UnityEngine.Object target, string propertyName, string value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Property {propertyName} was not found on {target.GetType().Name}");

            property.stringValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetFloat(UnityEngine.Object target, string propertyName, float value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Property {propertyName} was not found on {target.GetType().Name}");

            property.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetEnumIndex(UnityEngine.Object target, string propertyName, int index)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Property {propertyName} was not found on {target.GetType().Name}");

            property.enumValueIndex = index;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }

        private static T LoadOrCreateAsset<T>(string path, Func<T> create) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = create();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
#endif
