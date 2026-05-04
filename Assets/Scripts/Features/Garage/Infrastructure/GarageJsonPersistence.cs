using System.IO;
using Features.Garage.Application.Ports;
using Features.Garage.Domain;
using UnityEngine;

namespace Features.Garage.Infrastructure
{
    /// <summary>
    /// JSON 기반 편성 데이터 저장/불러오기 구현.
    /// Application.persistentDataPath에 저장.
    /// </summary>
    public sealed class GarageJsonPersistence : IGaragePersistencePort
    {
        private const string FileName = "garage_roster.json";

        public void Save(GarageRoster roster)
        {
            try
            {
                var wrapper = new GarageRosterWrapper { roster = roster };
                string json = JsonUtility.ToJson(wrapper, true);
                string path = Path.Combine(UnityEngine.Application.persistentDataPath, FileName);
                File.WriteAllText(path, json);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"[GarageJsonPersistence] Save failed: {e.Message}");
            }
        }

        public GarageRoster Load()
        {
            try
            {
                string path = Path.Combine(UnityEngine.Application.persistentDataPath, FileName);
                if (!File.Exists(path))
                    return new GarageRoster();

                string json = File.ReadAllText(path);
                var wrapper = JsonUtility.FromJson<GarageRosterWrapper>(json);
                var roster = wrapper?.roster ?? new GarageRoster();
                roster.Normalize();
                return roster;
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"[GarageJsonPersistence] Load failed: {e.Message}");
                return new GarageRoster();
            }
        }

        public void Delete()
        {
            try
            {
                string path = Path.Combine(UnityEngine.Application.persistentDataPath, FileName);
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"[GarageJsonPersistence] Delete failed: {e.Message}");
            }
        }

        [System.Serializable]
        private class GarageRosterWrapper
        {
            public GarageRoster roster;
        }
    }

    public sealed class GarageRosterLegacyIdMigrator : IRosterMigrationPort
    {
        public GarageRoster Migrate(GarageRoster roster)
        {
            if (roster == null)
                return new GarageRoster();

            roster.Normalize();
            bool hasChanges = false;
            for (int i = 0; i < GarageRoster.MaxSlots; i++)
            {
                var slot = roster.GetSlot(i);
                if (slot == null)
                    continue;

                hasChanges |= slot.frameId != GarageLegacyPartIdMap.ResolveCurrentPartId(slot.frameId);
                hasChanges |= slot.firepowerModuleId != GarageLegacyPartIdMap.ResolveCurrentPartId(slot.firepowerModuleId);
                hasChanges |= slot.mobilityModuleId != GarageLegacyPartIdMap.ResolveCurrentPartId(slot.mobilityModuleId);
            }

            if (!hasChanges)
                return roster;

            var migrated = roster.Clone();
            for (int i = 0; i < GarageRoster.MaxSlots; i++)
            {
                var slot = migrated.GetSlot(i);
                migrated.SetSlot(i, new GarageRoster.UnitLoadout(
                    GarageLegacyPartIdMap.ResolveCurrentPartId(slot.frameId),
                    GarageLegacyPartIdMap.ResolveCurrentPartId(slot.firepowerModuleId),
                    GarageLegacyPartIdMap.ResolveCurrentPartId(slot.mobilityModuleId)));
            }

            return migrated;
        }
    }
}
