## Features

The main feature of this plugin is to allow modular car engines to function without engine parts. This is appropriate for several use cases.
- **Use case #1 (Default):** Cars without engine parts can be driven, but they are slower and less fuel efficient. Engines can be incrementally improved by adding parts (a full set is not required).
- **Use case #2:** Missing engine parts are equivalent to low or medium quality parts. Engines can be improved by adding higher quality parts, but lower quality parts provide no benefit.
- **Use case #3:** Missing engine parts are equivalent to high quality parts (or higher), so engine parts provide no advantage. This not only saves time getting a car going, but also makes it easier to change a car's module configuration since you don't have to worry about removing and adding back engine parts. This can also be used to make cars extremely fast or fuel efficient since the stats are configurable.

All use cases can be applied globally to all cars, and/or to cars *owned* by players with permissions. For example, you can make unowned cars slow, owned cars equivalent to low quality engine parts, VIP cars equivalent to high quality engine parts, and give admin cars super speed.

Note: Cars still need fuel and engine *modules* to be driven. Changing that is out of scope for this plugin.

## Alternative plugin

The main criticism of this plugin has been that players will not necessarily realize that a car can be driven if the engine modules appear empty. To address this, the [Auto Engine Parts](https://umod.org/plugins/auto-engine-parts) plugin automatically fills engine modules with parts and also prevents players from removing them.

## Permissions

Each entry in the `PresetsRequiringPermission` configuration option automatically generates a permission of format `noengineparts.preset.<name>`. Granting one to a player will cause cars they **own** to use the corresponding engine part stats as a minimum. Granting multiple permissions to a player will cause only the last one to apply (based on the order in the config).

The following permissions come with this plugin's **default configuration**.

- `noengineparts.preset.tier1` -- Equivalent to low quality engine parts
- `noengineparts.preset.tier2` -- Equivalent to medium quality engine parts
- `noengineparts.preset.tier3` -- Equivalent to high quality engine parts
- `noengineparts.preset.tier4` -- Higher quality
- `noengineparts.preset.tier5` -- Even higher quality
- `noengineparts.preset.tier6` -- Extremely high quality

### How ownership works

**There is no such thing as car ownership in the vanilla Rust.** You will need another plugin to assign ownership to cars in order for the permissions in this plugin to be effective.

Car ownership is determined by the `OwnerID` property of the car, which is usually a player's Steam ID, or `0` for no owner. Most plugins that spawn cars for a player (such as [Craft Car Chassis](https://umod.org/plugins/craft-car-chassis) and [Spawn Modular Car](https://umod.org/plugins/spawn-modular-car)) will assign that player as the owner. For cars spawned by the vanilla game, it's recommended to use [Claim Vehicle](https://umod.org/plugins/claim-vehicle) to allow players to claim them with a command on cooldown.

## Configuration

Default configuration:

```json
{
  "DefaultPreset": {
    "Acceleration": 0.3,
    "TopSpeed": 0.3,
    "FuelEconomy": 0.3
  },
  "PresetsRequiringPermission": [
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

In vanilla Rust, each engine part provides one or more stats, such as Acceleration, Top Speed and Fuel Economy. Each part provides the same value for all applicable stats, either `0.6` (low quality), `0.8` (medium quality) or `1.0` (high quality). This plugin applies the configured stats as essentially a minimum per engine part slot. This means that if an engine part is missing, these values will be used in its place, for whichever stat types the part would normally provide. If an engine part is present, either the part's vanilla stats or the configured stats will be used, whichever are higher (can be a combination of both).

For example, if you set all stats to `0.7`, an empty Carburetor slot will provide `0.7` Top Speed and `0.7` Fuel Economy. Adding a low quality Carburetor will have no effect because it only has those stats at `0.6`. However, adding a medium quality Carburetor will be an upgrade since it provides `0.8` for both stats.

- `DefaultPreset` -- These stats apply to all cars, except for those owned by players with permissions to any entries in `PresetsRequiringPermission`.
  - `Acceleration` -- Not sure exactly what this affects. Doesn't seem to affect acceleration very much.
  - `TopSpeed` -- Affects acceleration and top speed. Higher means faster.
  - `FuelEconomy` -- Affects fuel consumption. Higher means less fuel is consumed (more efficient).
- `PresetsRequiringPermission` -- List of engine part stat configurations for use with permissions. A permission is automatically generated for each entry using the format `noengineparts.preset.<name>`. Granting one to a player will cause cars they **own** to use the configured stats as a minimum per engine part slot.
  - Note: These configurations have the same options as the `DefaultPreset` configuration option.

Note: Due to the way the game calculates engine power, increasing the engine stats beyond a certain point will actually cause engines to perform worse. For example, setting stats to `100` will cause the car to basically not move at all. The optimal stats will depend on how many engines a car has.

## Developer Hooks

#### OnEngineLoadoutOverride

- Called when this plugin is about to override the the stats of an engine module.
- Returning `false` will prevent the engine stats from being overriden.
- Returning `null` will result in the default behavior.

```csharp
object OnEngineLoadoutOverride(EngineStorage engineStorage)
```
