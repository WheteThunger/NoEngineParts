## Features

The main feature of this plugin is to allow modular cars to be driven without engine parts. This is appropriate for several use cases.
- **Use case #1 (Default):** Cars without parts can be driven slowly and are less fuel efficient. Engines can be incrementally improved by adding parts (a full set is not required).
- **Use case #2:** Missing engine parts are equivalent to low or medium quality parts. Engines can be improved by adding higher quality parts, but lower quality parts provide no benefit.
- **Use case #3:** Missing engine parts are equivalent to high quality parts (or higher), so engine parts provide no advantage. Can be used to make cars extremely fast or fuel efficient since the stats are configurable.

All use cases can be applied globally to all cars, and/or to cars *owned* by players with permissions. For example, you can apply use case #1 to most cars, and also make admin cars super fast.

Note: Cars still need fuel and engine modules to be driven. This plugin does not address that.

## Permissions

Each entry in the `EngineStatsRequiringPermission` configuration option automatically generates a permission of format `noengineparts.preset.<name>`. Granting one to a player will cause cars they **own** to use the corresponding engine part stats as a minimum. Granting multiple permissions to a player will cause only the last one to apply (based on the order in the config).

The following permissions come with this plugin's **default configuration**.

- `noengineparts.preset.tier1` -- Equivalent to low quality engine parts
- `noengineparts.preset.tier2` -- Equivalent to medium quality engine parts
- `noengineparts.preset.tier3` -- Equivalent to high quality engine parts
- `noengineparts.preset.tier4` -- Higher quality
- `noengineparts.preset.tier5` -- Even higher quality
- `noengineparts.preset.tier6` -- Extremely high quality

Note: Vanilla Rust does not currently assign player ownership to vehicles, so only plugins do it. Most plugins that spawn cars for specific players will already assign that player as the owner. For other cars, players can obtain ownership using the [Claim Vehicle Ownership](https://umod.org/plugins/claim-vehicle-ownership) plugin.

## Configuration

Default configuration:

```json
{
  "DefaultEngineStats": {
    "Acceleration": 0.3,
    "TopSpeed": 0.3,
    "FuelEconomy": 0.3
  },
  "EngineStatsRequiringPermission": [
    {
      "Name": "tier1",
      "Acceleration": 0.6,
      "TopSpeed": 0.6,
      "FuelEconomy": 0.6
    },
    {
      "Name": "tier2",
      "Acceleration": 0.8,
      "TopSpeed": 0.8,
      "FuelEconomy": 0.8
    },
    {
      "Name": "tier3",
      "Acceleration": 1.0,
      "TopSpeed": 1.0,
      "FuelEconomy": 1.0
    },
    {
      "Name": "tier4",
      "Acceleration": 2.0,
      "TopSpeed": 2.0,
      "FuelEconomy": 2.0
    },
    {
      "Name": "tier5",
      "Acceleration": 3.0,
      "TopSpeed": 3.0,
      "FuelEconomy": 3.0
    },
    {
      "Name": "tier6",
      "Acceleration": 4.0,
      "TopSpeed": 4.0,
      "FuelEconomy": 4.0
    }
  ]
}
```

In vanilla Rust, each engine part provides one or more stats, such as Acceleration, Top Speed and Fuel Economy. Each part provides the same value for all applicable stats, either `0.6` (low quality), `0.8` (medium quality) or `1.0` (high quality). This plugin applies the configured values as essentially a minimum per engine part slot. This means that if an engine part is missing, these values will be used in its place, for whichever stat types the part would normally provide. If an engine part is present, only the values configured here that are higher than the part's vanilla stats will be used.

For example, if you set all stats to `0.7`, an empty Carburetor slot will provide `0.7` Top Speed and `0.7` Fuel Economy. Adding a low quality Carburetor will have no effect because it will have both those stats at `0.6`. However, adding a medium quality Carburetor will upgrade to use `0.8` for both stats.

- `DefaultEngineStats` -- These stats apply to all cars, except for those owned by players with permissions to configurations in `EngineStatsRequiringPermission`.
  - `Acceleration` -- Not sure what this affects. Doesn't seem to affect acceleration very much.
  - `TopSpeed` -- Affects acceleration and top speed.
  - `FuelEconomy` -- Affects fuel consumption.
- `EngineStatsRequiringPermission` -- List of engine part stat configurations for use with permissions. A permission is automatically generated for each entry using the format `noengineparts.preset.<name>`. Granting one to a player will cause cars they **own** to use the configured stats as a minimum per engine part slot.
  - Note: These configurations have the same options as the `DefaultEngineStats` configuration option.

Note: Due to the way the game calculates engine force, increasing the engine stats beyond a certain point will actually cause engines to perform worse. For example, setting stats to `100` will cause the car to basically not move at all. The optimal stats will depend on how many engines a car has.

## Developer Hooks

#### OnEngineStatsOverride

- Called when this plugin is about to override the stats of an engine module.
- Returning `false` will prevent the engine stats from being overriden.
- Returning `null` will result in the default behavior.

```csharp
object OnEngineStatsOverride(VehicleModuleEngine engineModule)
```

**Note:** Planning to change this to `OnEngineLoadoutOverride(EngineStorage engineStorage)` in v1.0.0 pending an Oxide update.
