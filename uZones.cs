/* 
    =======================
    uZones by Tanese
    =======================
    V1.2.0
    =======================
    This is an extended zoning script that allows users to manage zones and nodes with a JSON configuration with commands. 
    More information can be found here: https://github.com/Luis-Tanese/uZones/wiki/Home.
*/

event onLoad() {
    loadZonesFromConfig();
    logger.log("uZones by Tanese V1.2.0 successfully loaded!");
}

configFilePath = "uZonesConfig.json";

zones = [];

predefinedFlags = ["nodamage", "nobuild", "nocraft", "novehicledamage", "noitemtake", "nocarjack", "nosiphon", "nohook", "nolockpick", "noraid"];

flagPacks = {
    "safezone": ["nodamage", "nobuild", "novehicledamage", "nocarjack", "nosiphon", "nolockpick", "nohook", "noraid"],
    "nopvpzone": ["nodamage", "novehicledamage"]
};

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

command uzones() {
    permission = "uZones.manage";
    execute() {
        if (arguments.count < 1) {
            player.message("Error: Missing action argument.", "red");
            return;
        }
        action = arguments[0];
        if (action == "add") {
            if (arguments.count < 3) {
                player.message("Error: Missing type or zoneName argument.", "red");
                return;
            }
            handleAdd(player, arguments[1], arguments[2], arguments[3]);
        } else if (action == "remove") {
            if (arguments.count < 3) {
                player.message("Error: Missing type or zoneName argument.", "red");
                return;
            }
            handleRemove(player, arguments[1], arguments[2], arguments[3]);
        } else if (action == "replace") {
            if (arguments.count < 4) {
                player.message("Error: Missing type, zoneName, or new value argument.", "red");
                return;
            }
            handleReplace(player, arguments[1], arguments[2], arguments[3]);
        } else if (action == "list") {
            if (arguments.count < 2) {
                player.message("Error: Missing type argument.", "red");
                return;
            }
            handleList(player, arguments[1], arguments[2]);
        } else if (action == "tp") {
            if (arguments.count < 3) {
                player.message("Error: Missing zoneName or nodeNumber argument.", "red");
                return;
            }
            handleTeleport(player, arguments[1], arguments[2]);
        } else if (action == "getpos") {
            handleGetPos(player);
        } else if (action == "visualize") {
            if (arguments.count < 3) {
                player.message("Error: Missing type or zoneName argument.", "red");
                return;
            }
            handleVisualize(player, arguments[1], arguments[2], arguments[3]);
        } else {
            player.message("Error: Unknown action.", "red");
        }
    }
}

function handleAdd(player, type, zoneName, flag) {
    zone = getZoneByName(zoneName);
    if (type == "zone") {
        if (zone != null) {
            player.message("Zone already exists!", "red");
            return;
        }
        zones.add({ "zoneName": zoneName, "nodes": [] });
        saveZonesToConfig();
        player.message("Zone {0} added.".format(zoneName), "green");
    } else if (type == "node") {
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
    } else if (type == "flag") {
        if (zone == null) {
            player.message("Zone not found!", "red");
            return;
        }
        if (!isValidFlag(flag)) {
            player.message("Flag {0} doesn't exist.".format(flag), "red");
            return;
        }
        if (!zone.containsKey("flags")) {
            zone["flags"] = [];
        }

        if (predefinedFlags.contains(flag)) {
            if (!zone["flags"].contains(flag)) {
                zone["flags"].add(flag);
                saveZonesToConfig();
                player.message("Flag {0} added to zone {1}.".format(flag, zoneName), "green");
            } else {
                player.message("Flag {0} already exists in zone {1}.".format(flag, zoneName), "yellow");
            }
        } else if (flagPacks.containsKey(flag)) {
            foreach (flagInPack in flagPacks[flag]) {
                if (!zone["flags"].contains(flagInPack)) {
                    zone["flags"].add(flagInPack);
                }
            }
            saveZonesToConfig();
            player.message("Flag pack {0} added to zone {1}.".format(flag, zoneName), "green");
        }
    }
}

function handleRemove(player, type, zoneName, nodeIndex) {
    zone = getZoneByName(zoneName);
    if (type == "zone") {
        if (zone == null) {
            player.message("Zone not found!", "red");
            return;
        }
        zones.remove(zone);
        saveZonesToConfig();
        player.message("Zone {0} removed.".format(zoneName), "green");
    } else if (type == "node" and nodeIndex != null) {
        if (zone == null or nodeIndex.toNumber() >= zone["nodes"].count) {
            player.message("Invalid zone or node number.", "red");
            return;
        }
        zone["nodes"].removeAt(nodeIndex.toNumber());
        saveZonesToConfig();
        player.message("Node {0} removed from zone {1}.".format(nodeIndex, zoneName), "green");
    } else if (type == "flag" and nodeIndex != null) {
        if (zone == null) {
            player.message("Zone not found!", "red");
            return;
        }
        if (!zone.containsKey("flags")) {
            player.message("No flags are set for this zone.", "yellow");
            return;
        }
        if (!zone["flags"].contains(nodeIndex)) {
            player.message("Flag {0} does not exist in zone {1}.".format(nodeIndex, zoneName), "red");
            return;
        }
        zone["flags"].remove(nodeIndex);
        saveZonesToConfig();
        player.message("Flag {0} removed from zone {1}.".format(nodeIndex, zoneName), "green");
    } else {
        player.message("Error: Missing node number or flag for removal.", "red");
    }
}

function handleReplace(player, type, zoneName, newValue) {
    if (type == "zone") {
        zone = getZoneByName(zoneName);
        if (zone == null) {
            player.message("Zone not found!", "red");
            return;
        }
        zone["zoneName"] = newValue;
        saveZonesToConfig();
        player.message("Zone {0} renamed to {1}.".format(zoneName, newValue), "green");
    } else if (type == "node") {
        zone = getZoneByName(zoneName);
        nodeIndex = newValue.toNumber();
        if (zone == null or nodeIndex >= zone["nodes"].count) {
            player.message("Invalid zone or node number.", "red");
            return;
        }
        node = zone["nodes"][nodeIndex];
        node["x"] = player.position.x;
        node["y"] = player.position.y;
        node["z"] = player.position.z;
        saveZonesToConfig();
        player.message("Node {0} in zone {1} replaced.".format(nodeIndex, zoneName), "green");
    }
}

function handleList(player, type, zoneName) {
    if (type == "zones") {
        foreach (zone in zones) {
            player.message(zone["zoneName"]);
            logger.log(zone["zoneName"]);
        }
    } else if (type == "zone") {
        zone = getZoneByName(zoneName);
        if (zone == null) {
            player.message("Zone not found!", "red");
            return;
        }
        player.message("{0} has {1} nodes.".format(zone["zoneName"], zone["nodes"].count));
    } else if (type == "nodes") {
        zone = getZoneByName(zoneName);
        if (zone == null) {
            player.message("Zone not found!", "red");
            return;
        }
        foreach (node in zone["nodes"]) {
            player.message("Node: x={0}, y={1}, z={2}".format(node["x"], node["y"], node["z"]));
        }
    } else if (type == "flags") {
        zone = getZoneByName(zoneName);
        if (zone == null) {
            player.message("Zone not found!", "red");
            return;
        }
        if (zone.containsKey("flags") and zone["flags"].count > 0) {
            player.message("Flags for {0}: {1}".format(zone["zoneName"], zone["flags"].join(", ")));
        } else {
            player.message("No flags set for {0}.".format(zone["zoneName"]));
        }
    }
}

function handleTeleport(player, zoneName, nodeIndex) {
    zone = getZoneByName(zoneName);
    if (zone == null or nodeIndex.toNumber() >= zone["nodes"].count) {
        player.message("Zone or node not found.", "red");
        return;
    }
    node = zone["nodes"][nodeIndex.toNumber()];
    player.teleport(vector3(node["x"], node["y"], node["z"]));
    player.message("Teleported to node {0} in zone {1}.".format(nodeIndex, zoneName), "green");
}

function handleGetPos(player) {
    player.message("Position: vector3(x: {0}, y: {1}, z: {2})".format(player.position.x, player.position.y, player.position.z));
    logger.log("Position: vector3(x: {0}, y: {1}, z: {2})".format(player.position.x, player.position.y, player.position.z));
}

function handleVisualize(player, type, zoneName, state) {
    zone = getZoneByName(zoneName);
    if (zone == null) {
        player.message("Zone not found!", "red");
        return;
    }
    if (type == "nodes" and state == "on") {
        visualizeNodes(zone);
        player.message("Node visualization for zone {0} is now ON.".format(zoneName), "green");
    } else if (type == "nodes" and state == "off") {
        removeVisualizedNodes(zone);
        player.message("Node visualization for zone {0} is now OFF.".format(zoneName), "red");
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

function isValidFlag(flag) {
    womp = false;
    if (predefinedFlags.contains(flag) or flagPacks.containsKey(flag)) {
        womp = true;
    }
    return womp;
}

function zoneHasFlag(player, flag) {
    zoneName = player.getData("zone");
    zone = getZoneByName(zoneName);
    womp = false;
    if (zone != null and zone.containsKey("flags") and zone["flags"].contains(flag)) {
        womp = true;
    }
    return womp;
}

event onBarricadeBuild(player, id, position, cancel) {
    if (zoneHasFlag(player, "nobuild")) {
        cancel = true;
    }
}

event onBarricadeDamaged(player, barricade, damage, cause, cancel) {
    if (player != null) {
        if (zoneHasFlag(player, "noraid")) {
            cancel = true;
        }
    }
}

event onPlayerCrafted(player, itemId, cancel) {
    if (zoneHasFlag(player, "nocraft")) {
        cancel = true;
    }
}
event onPlayerDamaged(victim, killer, cancel, damage, cause, limb) {
    if (killer != null) {
        if (zoneHasFlag(victim, "nodamage")) {
            cancel = true;
        }
    }
}
event onStructureBuild(player, id, position, cancel) {
    if (zoneHasFlag(player, "nobuild")) {
        cancel = true;
    }
}
event onStructureDamaged(player, structure, damage, cause, cancel) {
    if (player != null) {
        if (zoneHasFlag(player, "noraid")) {
            cancel = true;
        }
    }
}
event onVehicleDamaged(vehicle, player, cause, damage, cancel){
    if (player != null) {
        if (zoneHasFlag(player, "novehicledamage")) {
            cancel = true;
        }
    }
}
event onPlayerTakingItem(player, itemId, cancel){
    if (zoneHasFlag(player, "noitemtake")) {
        cancel = true;
    }
}
event onVehicleCarjack(player, vehicle, force, torque, cancel){
    if (zoneHasFlag(player, "nocarjack")) {
        cancel = true;
    }
}
event onSiphonVehicleRequest(player, vehicle, amount, cancel){
    if (zoneHasFlag(player, "nosiphon")) {
        cancel = true;
    }
}
event onVehicleHook(player, vehicle, vehicleHooked, cancel){
    if (zoneHasFlag(player, "nohook")) {
        cancel = true;
    }
}
event onVehicleLockpick(player, vehicle, cancel){
    if (zoneHasFlag(player, "nolockpick")) {
        cancel = true;
    }
}
event onVehicleTireDamaged(player, vehicle, cause, cancel){
    if (zoneHasFlag(player, "novehicledamage")) {
        cancel = true;
    }
}
