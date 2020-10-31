using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rust.Modular;

namespace Oxide.Plugins
{
    [Info("No Engine Parts", "WhiteThunder", "0.1.0")]
    [Description("Allows modular cars to be driven without engine parts.")]
    internal class NoEngineParts : CovalencePlugin
    {
        #region Fields

        private const string PermissionPresetPrefix = "noengineparts.preset";

        private Configuration pluginConfig;

        #endregion

        #region Hooks

        private void Init()
        {
            foreach (var preset in pluginConfig.engineStatsRequiringPermission)
                if (!string.IsNullOrWhiteSpace(preset.name))
                    permission.RegisterPermission(GetPresetPermission(preset.name), this);
        }

        private void OnServerInitialized(bool initialBoot)
        {
            if (!initialBoot)
            {
                foreach (var entity in BaseNetworkable.serverEntities)
                {
                    var engineModule = entity as VehicleModuleEngine;
                    if (engineModule != null)
                        OnEngineStatsRefresh(engineModule);
                }
            }
        }

        // TODO: Use more precise hook only for EngineStorage when available
        // Note: That hook won't be called on engine startup so this plugin should use the
        // OnEngineStarted hook to update the loadout to avoid needing to hook permission/group changes
        private object OnEngineStatsRefresh(VehicleModuleEngine engineModule)
        {
            var engineStats = DetermineEngineStatsForModule(engineModule);
            if (engineStats == null)
                return null;

            if (OverrideStatsWasBlocked(engineModule))
                return null;

            // Delay this because EngineStorage containers aren't created immediately after spawn
            NextTick(() => TryApplyEngineStats(engineModule, engineStats));
            return false;
        }

        #endregion

        #region Helper Methods

        private bool OverrideStatsWasBlocked(VehicleModuleEngine engineModule)
        {
            object hookResult = Interface.CallHook("OnEngineStatsOverride", engineModule);
            return hookResult is bool && (bool)hookResult == false;
        }

        private string GetPresetPermission(string presetName) => $"{PermissionPresetPrefix}.{presetName}";

        private void TryApplyEngineStats(VehicleModuleEngine engineModule, EngineStats engineStats)
        {
            var engineStorage = engineModule.GetContainer() as EngineStorage;
            if (engineStorage == null)
                return;

            RefreshEngineLoadout(engineStorage, engineStats);

            engineModule.IsUsable = true;
            engineModule.PerformanceFractionAcceleration = GetPerformanceFraction(engineModule, engineStorage.accelerationBoostPercent);
            engineModule.PerformanceFractionTopSpeed = GetPerformanceFraction(engineModule, engineStorage.topSpeedBoostPercent);
            engineModule.PerformanceFractionFuelEconomy = GetPerformanceFraction(engineModule, engineStorage.fuelEconomyBoostPercent);
            engineModule.OverallPerformanceFraction = (engineModule.PerformanceFractionAcceleration +
                engineModule.PerformanceFractionTopSpeed + engineModule.PerformanceFractionFuelEconomy) / 3f;
        }

        private void RefreshEngineLoadout(EngineStorage engineStorage, EngineStats engineStats)
        {
            var acceleration = 0f;
            var topSpeed = 0f;
            var fuelEconomy = 0f;

            // TODO: Replace with corresponding fields of `EngineStorage` when they are made public
            var accelerationSlots = 0f;
            var topSpeedSlots = 0f;
            var fuelEconomySlots = 0f;

            for (var slot = 0; slot < engineStorage.inventory.capacity; slot++)
            {
                var engineItemType = engineStorage.slotTypes[slot];

                var item = engineStorage.inventory.GetSlot(slot);
                var itemValue = 0f;
                if (item != null && !item.isBroken)
                {
                    var component = item.info.GetComponent<ItemModEngineItem>();
                    if (component != null)
                        itemValue = item.amount * GetTierValue(component.tier);
                }

                if (engineItemType.BoostsAcceleration())
                {
                    accelerationSlots++;
                    acceleration += Math.Max(itemValue, engineStats.acceleration);
                }

                if (engineItemType.BoostsFuelEconomy())
                {
                    fuelEconomySlots++;
                    fuelEconomy += Math.Max(itemValue, engineStats.fuelEconomy);
                }
                
                if (engineItemType.BoostsTopSpeed())
                {
                    topSpeedSlots++;
                    topSpeed += Math.Max(itemValue, engineStats.topSpeed);
                }
            }

            engineStorage.isUsable = acceleration > 0 && topSpeed > 0 && fuelEconomy > 0;
            engineStorage.accelerationBoostPercent = acceleration / accelerationSlots;
            engineStorage.fuelEconomyBoostPercent = fuelEconomy / fuelEconomySlots;
            engineStorage.topSpeedBoostPercent = topSpeed / topSpeedSlots;
            engineStorage.SendNetworkUpdate();
        }

        // TODO: Replace with the `EngineStorage.GetTierValue(int)` method when it is made public
        private float GetTierValue(int tier) =>
            tier == 1 ? 0.6f :
            tier == 2 ? 0.8f :
            1;

        // TODO: Replace with `EngineStorage.GetPerformanceFraction()` when it is made public
        // Or remove when the more specific hook is being used
        private float GetPerformanceFraction(VehicleModuleEngine engineModule, float statBoostPercent)
        {
            var healthFraction = engineModule.healthFraction;
            return Mathf.Lerp(0, 0.25f, healthFraction) + (healthFraction != 0 ? statBoostPercent * 0.75f : 0);
        }

        #endregion

        #region Configuration

        private EngineStats DetermineEngineStatsForModule(VehicleModuleEngine engineModule)
        {
            var car = engineModule.Vehicle;
            if (car == null)
                return pluginConfig.defaultEngineStats;

            return DetermineEngineStatsForOwner(car.OwnerID);
        }

        private EngineStats DetermineEngineStatsForOwner(ulong ownerId)
        {
            if (ownerId == 0 || pluginConfig.engineStatsRequiringPermission == null)
                return pluginConfig.defaultEngineStats;

            var ownerIdString = ownerId.ToString();
            for (var i = pluginConfig.engineStatsRequiringPermission.Length - 1; i >= 0; i--)
            {
                var preset = pluginConfig.engineStatsRequiringPermission[i];
                if (!string.IsNullOrWhiteSpace(preset.name) &&
                    permission.UserHasPermission(ownerIdString, GetPresetPermission(preset.name)))
                {
                    return preset;
                }
            }

            return pluginConfig.defaultEngineStats;
        }

        private Configuration GetDefaultConfig() => new Configuration();

        internal class Configuration : SerializableConfiguration
        {
            [JsonProperty("DefaultEngineStats")]
            public EngineStats defaultEngineStats = new EngineStats()
            {
                acceleration = 0.3f,
                topSpeed = 0.3f,
                fuelEconomy = 0.3f,
            };

            [JsonProperty("EngineStatsRequiringPermission")]
            public EngineStats[] engineStatsRequiringPermission = new EngineStats[]
            {
                new EngineStats ()
                {
                    name = "tier1",
                    acceleration = 0.6f,
                    topSpeed = 0.6f,
                    fuelEconomy = 0.6f,
                },
                new EngineStats ()
                {
                    name = "tier2",
                    acceleration = 0.8f,
                    topSpeed = 0.8f,
                    fuelEconomy = 0.8f,
                },
                new EngineStats ()
                {
                    name = "tier3",
                    acceleration = 1,
                    topSpeed = 1,
                    fuelEconomy = 1,
                },
                new EngineStats ()
                {
                    name = "tier4",
                    acceleration = 2,
                    topSpeed = 2,
                    fuelEconomy = 2,
                },
                new EngineStats()
                {
                    name = "tier5",
                    acceleration = 3,
                    topSpeed = 3,
                    fuelEconomy = 3,
                },
                new EngineStats()
                {
                    name = "tier6",
                    acceleration = 4,
                    topSpeed = 4,
                    fuelEconomy = 4,
                },
            };
        }

        internal class EngineStats
        {
            [JsonProperty("Name", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public string name;

            [JsonProperty("Acceleration")]
            public float acceleration;

            [JsonProperty("TopSpeed")]
            public float topSpeed;

            [JsonProperty("FuelEconomy")]
            public float fuelEconomy;
        }

        #endregion

        #region Configuration Boilerplate

        internal class SerializableConfiguration
        {
            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonHelper.Deserialize(ToJson()) as Dictionary<string, object>;
        }

        internal static class JsonHelper
        {
            public static object Deserialize(string json) => ToObject(JToken.Parse(json));

            private static object ToObject(JToken token)
            {
                switch (token.Type)
                {
                    case JTokenType.Object:
                        return token.Children<JProperty>()
                                    .ToDictionary(prop => prop.Name,
                                                  prop => ToObject(prop.Value));

                    case JTokenType.Array:
                        return token.Select(ToObject).ToList();

                    default:
                        return ((JValue)token).Value;
                }
            }
        }

        private bool MaybeUpdateConfig(SerializableConfiguration config)
        {
            var currentWithDefaults = config.ToDictionary();
            var currentRaw = Config.ToDictionary(x => x.Key, x => x.Value);
            return MaybeUpdateConfigDict(currentWithDefaults, currentRaw);
        }

        private bool MaybeUpdateConfigDict(Dictionary<string, object> currentWithDefaults, Dictionary<string, object> currentRaw)
        {
            bool changed = false;

            foreach (var key in currentWithDefaults.Keys)
            {
                object currentRawValue;
                if (currentRaw.TryGetValue(key, out currentRawValue))
                {
                    var defaultDictValue = currentWithDefaults[key] as Dictionary<string, object>;
                    var currentDictValue = currentRawValue as Dictionary<string, object>;

                    if (defaultDictValue != null)
                    {
                        if (currentDictValue == null)
                        {
                            currentRaw[key] = currentWithDefaults[key];
                            changed = true;
                        }
                        else if (MaybeUpdateConfigDict(defaultDictValue, currentDictValue))
                            changed = true;
                    }
                }
                else
                {
                    currentRaw[key] = currentWithDefaults[key];
                    changed = true;
                }
            }

            return changed;
        }

        protected override void LoadDefaultConfig() => pluginConfig = GetDefaultConfig();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                pluginConfig = Config.ReadObject<Configuration>();
                if (pluginConfig == null)
                {
                    throw new JsonException();
                }

                if (MaybeUpdateConfig(pluginConfig))
                {
                    LogWarning("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch
            {
                LogWarning($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Log($"Configuration changes saved to {Name}.json");
            Config.WriteObject(pluginConfig, true);
        }

        #endregion
    }
}
