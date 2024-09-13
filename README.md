# uZones

**Version:** 1.1.14  
**Author:** Tanese  

## Overview

**uZones** is an advanced zoning script that allows users to define and manage zones and nodes through a JSON configuration. Users can create, remove, and interact with zones and nodes using in-game commands. Additionally, uZones supports visualizing nodes, teleporting to specific nodes, and saving the state in a JSON file, making it easy to persist changes across game sessions.

## Features
- **Dynamic Zone & Node Management:** Add, remove, and edit zones and nodes with ease.
- **Node Visualization:** Visualize nodes with barricades for clear representation.
- **Teleportation Support:** Teleport to specific nodes within a zone.
- **Easy Configuration:** All settings and zones are stored in a user-defined JSON configuration file.
- **Command-Based Control:** Simple commands for managing zones and nodes.

## Requirements

Before using **uZones**, ensure you have the following module installed:

- [uScriptExtended v1.6.8.0](https://github.com/MolyiEZ/uScriptExtended/releases/tag/v1.6.8.0) by MolyiEZ

## Setup Instructions

1. **Create a Configuration File:**
   - Create a file named `uZonesConfig.json` in the `data` folder of your server directory:
   - ```Path: .../servers/yourserver/uScript/data/uZonesConfig.json```


2. **Config Structure:**  
Zones and nodes will be automatically managed by the script and saved in this JSON file. No manual setup inside the file is necessary.

## How It Works

### Zone Detection

uZones tracks player positions in real-time and determines which zone a player is in based on pre-defined nodes in the configuration file. You need at least **3 nodes** to define a zone, though you can have more, but too many may lead to performance issues.

# Commands

## Zone and Node Management
`/uzones add <zone/node> <zoneName>`  
Adds a new zone or node (node is added at the player's current position).  
Permission: uZones.manage

`/uzones remove <zone/node> <zoneName> [nodeNumber]`  
Removes a zone or a node. If removing a node, specify the node number.  
Permission: uZones.manage

`/uzones replace <zone/node> <zoneName> [newZoneName/nodeNumber]`  
Replaces a zone name or node.  
Permission: uZones.manage

`/uzones list <zones/nodes/zone> <zoneName>`  
Lists all zones or details of a specific zone/nodes.  
Permission: uZones.view

## Visualization
`/uzonesvisualize <nodes> <zoneName> <on/off>`  
Turns node visualization on or off for the specified zone.  
Permission: uZones.visualize

## Teleportation
`/uzonestp <zoneName> <nodeNumber>`  
Teleports the player to the specified node in the zone.  
Permission: uZones.teleport

## Miscellaneous
`/uzonesgetpos`  
Retrieves the player’s current position coordinates.  
Permission: uZones.getpos

# Implementation
Just use the following event to get the desired outcome:
```c#
event onPlayerPositionUpdated(player) {
 if (player.getData("zone") == zoneName) { // zoneName can be whatever zone you want to get
     // Replace with your custom logic here.
 }
}
```

# JSON Configuration Example:
The zones and nodes are stored in a JSON file. Here’s an example of how a zone with nodes might look:
```json
[
    {
      "zoneName": "ExampleZone",
      "nodes": [
        {
          "x": 211.57762145996094,
          "y": 32.950801849365234,
          "z": -787.4661865234375
        },
        {
          "x": 182.3287353515625,
          "y": 32.928306579589844,
          "z": -787.20147705078125
        },
        {
          "x": 182.09104919433594,
          "y": 32.3869514465332,
          "z": -818.11444091796875
        }
      ]
    },
    {
        "zoneName": "ExampleZone2",
        "nodes": [
          {
            "x": 211.57762145996094,
            "y": 32.950801849365234,
            "z": -787.4661865234375
          },
          {
            "x": 182.3287353515625,
            "y": 32.928306579589844,
            "z": -787.20147705078125
          },
          {
            "x": 182.09104919433594,
            "y": 32.3869514465332,
            "z": -818.11444091796875
          },
          {
            "x": 214.70668029785156,
            "y": 32.381565093994141,
            "z": -817.6444091796875
          },
          {
            "x": 217.86195373535156,
            "y": 32.533638000488281,
            "z": -801.5567626953125
          },
          {
            "x": 222.14466857910156,
            "y": 34.259078979492188,
            "z": -787.88555908203125
          },
          {
            "x": 219.20257568359375,
            "y": 34.588790893554688,
            "z": -778.1904296875
          }
        ]
      }
  ]
```

# Extras
For any questions or issues, feel free to raise a bug report or suggestion on this GitHub repository or in discord @ tanese.
Thank you to benjaminmaigua for helping me test this.
