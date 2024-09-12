/* 
    =======================
    uZones by Tanese
    =======================
    V1.1.13
    =======================
    This is an extended zoning script that allows users to manage zones and nodes with a JSON configuration with commands. 
    It supports adding/removing zones and nodes, visualizing nodes and borders, and teleporting to specific nodes. 
    The commands are intuitive and easy to use, and the state is saved in a JSON file.
    You need atleast 3 nodes in a zones to make it work. You can have as many nodes as you want, tho I don't recommend since it can be laggy

    Commands:
    =======================
    - /uzones add <node/zone> <zoneName> : Add a new zone or node (node is added at player's position).
      Permission: uZones.manage
    
    - /uzones remove <zone/node> <zoneName> [nodeNumber] : Remove a zone or a node (node number is required for node removal).
      Permission: uZones.manage

    - /uzones replace <zone/node> <zoneName> [newZoneName/nodeNumber] : Replace zone name or node.
      Permission: uZones.manage

    - /uzones list <zones/nodes/zone> <zoneName> : List all zones or details of a specific zone/nodes.
      Permission: uZones.view

    - /uzonesvisualize <nodes> <zoneName> <on/off> : Visualize nodes using barricades.
      Permission: uZones.visualize

    - /uzonesgetpos : Get the player's current position.
      Permission: uZones.getpos

    - /uzonestp <zoneName> <nodeNumber> : Teleport to a specific node in a zone.
      Permission: uZones.teleport
*/

configFilePath = "uZonesConfig.json";

zones = [];

function loadZonesFromConfig() {
    jsonData = file.read(configFilePath);
    if (jsonData == "") {
        zones = [];
    } else {
        zones = deserialize(jsonData);
        convertNodesToVector3();
    }
}

function saveZonesToConfig() {
    jsonData = zones.serialize();
    file.writeAll(configFilePath, jsonData);
}

function isPointInZone(playerPos, zoneNodes) {
    crossings = 0;
    for (i = 0; i < zoneNodes.count; i++) {
        count = i + 1;
        nextIndex = count % zoneNodes.count;
        node1 = zoneNodes[i];
        node2 = zoneNodes[nextIndex];
        isNode1Above = node1["z"] > playerPos.z;
        isNode2Above = node2["z"] > playerPos.z;
        if (isNode1Above != isNode2Above) {
            zDiffPlayerNode1 = playerPos.z - node1["z"];
            xDiffNode2Node1 = node2["x"] - node1["x"];
            zDiffNode2Node1 = node2["z"] - node1["z"];
            intersectXNumerator = zDiffPlayerNode1 * xDiffNode2Node1;
            intersectXDenominator = zDiffNode2Node1;
            if (math.abs(intersectXDenominator) > 0) {
                intersectXIndex = intersectXNumerator / intersectXDenominator;
                intersectX = node1["x"] + intersectXIndex;
                if (playerPos.x < intersectX) {
                    crossings += 1;
                }
            }
        }
    }
    womp = false;
    wompity = crossings % 2;
    if (wompity == 1) {
        womp = true;
    }
    return womp;
}

function checkPlayerZone(player) {
    playerPos = player.position;
    foreach (zone in zones) {
        if (isPointInZone(playerPos, zone["nodes"])) {
            return zone["zoneName"];
        }
    }
    return null;
}

event onPlayerPositionUpdated(player) {
    currentZone = checkPlayerZone(player);
    previousZone = player.getData("zone");
    if (currentZone != null and currentZone != previousZone) {
        logger.log("{0} has entered zone: {1}".format(player.name, currentZone));
        player.setData("zone", currentZone);
        player.message("You entered {0}".format(currentZone), "green");
    } else if (currentZone == null and previousZone != null) {
        logger.log("{0} has left zone: {1}".format(player.name, previousZone));
        player.setData("zone", null);
        player.message("You left {0}".format(previousZone), "red");
    }
}

function convertNodesToVector3() {
    foreach (zone in zones) {
        foreach (node in zone["nodes"]) {
            node["x"] = node["x"];
            node["y"] = node["y"];
            node["z"] = node["z"];
        }
    }
}

command uzones(action, type, zoneName, optionalArg) {
    permission = "uZones.manage";
    execute() {
        if (action == "add" and type == "zone") {
            if (getZoneByName(zoneName) != null) {
                player.message("Zone already exists!", "red");
                return;
            }
            zones.add({ "zoneName": zoneName, "nodes": [] });
            saveZonesToConfig();
            player.message("Zone {0} added.".format(zoneName), "green");
        } else if (action == "add" and type == "node") {
            zone = getZoneByName(zoneName);
            if (zone == null) {
                player.message("Zone not found!", "red");
                return;
            }
            position = player.position;
            zone["nodes"].add({ 
                "x": position.x, 
                "y": position.y, 
                "z": position.z 
            });
            saveZonesToConfig();
            player.message("Node added to zone {0}.".format(zoneName), "green");
        } else if (action == "remove" and type == "zone") {
            zone = getZoneByName(zoneName);
            if (zone == null) {
                player.message("Zone not found!", "red");
                return;
            }
            zones.remove(zone);
            saveZonesToConfig();
            player.message("Zone {0} removed.".format(zoneName), "green");
        } else if (action == "remove" and type == "node") {
            zone = getZoneByName(zoneName);
            if (zone == null or optionalArg == null or optionalArg.toNumber() >= zone["nodes"].count) {
                player.message("Invalid zone or node number.", "red");
                return;
            }
            zone["nodes"].removeAt(optionalArg.toNumber());
            saveZonesToConfig();
            player.message("Node {0} removed from zone {1}.".format(optionalArg, zoneName), "green");
        } else if (action == "replace" and type == "zone") {
            zone = getZoneByName(zoneName);
            if (zone == null or optionalArg == null) {
                player.message("Invalid zone or new name.", "red");
                return;
            }
            zone["zoneName"] = optionalArg;
            saveZonesToConfig();
            player.message("Zone {0} renamed to {1}.".format(zoneName, optionalArg), "green");
        } else if (action == "replace" and type == "node") {
            zone = getZoneByName(zoneName);
            if (zone == null or optionalArg == null or optionalArg.toNumber() >= zone["nodes"].count) {
                player.message("Invalid zone or node number.", "red");
                return;
            }
            node = zone["nodes"][optionalArg.toNumber()];
            node["x"] = player.position.x.toString();
            node["y"] = player.position.y.toString();
            node["z"] = player.position.z.toString();
            saveZonesToConfig();
            player.message("Node {0} in zone {1} replaced.".format(optionalArg, zoneName), "green");
        } else if (action == "list" and type == "zones") {
            foreach (zone in zones) {
                player.message(zone["zoneName"]);
                logger.log(zone["zoneName"]);
            }
        } else if (action == "list" and type == "zone") {
            zone = getZoneByName(zoneName);
            if (zone == null) {
                player.message("Zone not found!", "red");
                return;
            }
            player.message("{0} has {1} nodes.".format(zone["zoneName"], zone["nodes"].count));
        } else if (action == "list" and type == "nodes") {
            zone = getZoneByName(zoneName);
            if (zone == null) {
                player.message("Zone not found!", "red");
                return;
            }
            foreach (node in zone["nodes"]) {
                player.message("Node: x={0}, y={1}, z={2}".format(node["x"], node["y"], node["z"]));
            }
        }
    }
}

function getZoneByName(zoneName) {
    foreach (zone in zones) {
        if (zone["zoneName"] == zoneName) {
            return zone;
        }
    }
    return null;
}

command uzonesgetpos() {
    permission = "uZones.getpos";
    execute() {
        player.message("Position: vector3(x: {0}, y: {1}, z: {2})".format(player.position.x, player.position.y, player.position.z));
        logger.log("Position: vector3(x: {0}, y: {1}, z: {2})".format(player.position.x, player.position.y, player.position.z));
    }
}

command uzonestp(zoneName, nodeNumber) {
    permission = "uZones.teleport";
    execute() {
        zone = getZoneByName(zoneName);
        if (zone == null or nodeNumber.toNumber() >= zone["nodes"].count) {
            player.message("Zone or node not found.", "red");
            return;
        }
        node = zone["nodes"][nodeNumber.toNumber()];
        player.teleport(vector3(node["x"], node["y"], node["z"]));
        player.message("Teleported to node {0} in zone {1}.".format(nodeNumber, zoneName), "green");
    }
}

command uzonesvisualize(type, zoneName, state) {
    permission = "uZones.visualize";
    execute() {
        zone = getZoneByName(zoneName);
        if (zone == null) {
            player.message("Zone not found!", "red");
            return;
        }
        if (type == "nodes") {
            if (state == "on") {
                visualizeNodes(zone);
                player.message("Node visualization for zone {0} is now ON.".format(zoneName), "green");
            } else if (state == "off") {
                removeVisualizedNodes(zone);
                player.message("Node visualization for zone {0} is now OFF.".format(zoneName), "red");
            }
        }
    }
}

function visualizeNodes(zone) {
    foreach (node in zone["nodes"]) {
        barricade = spawner.spawnBarricade(1098, vector3(node["x"], node["y"], node["z"]));
        node["visualizedBarricadeId"] = barricade.instanceId;
    }
}

function removeVisualizedNodes(zone) {
    foreach (node in zone["nodes"]) {
        if (node.containsKey("visualizedBarricadeId")) {
            barricade = server.findBarricade(node["visualizedBarricadeId"]);
            if (barricade != null) {
                barricade.destroy();
            }
            node.remove("visualizedBarricadeId");
        }
    }
}

event onLoad() {
    loadZonesFromConfig();
    logger.log("uZones by Tanese V1.1.13 successfully loaded!");
}
