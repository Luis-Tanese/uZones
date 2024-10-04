/* 
    =======================
    uZones by Tanese
    =======================
    V1.3.0
    =======================
    This is an extended zoning script that allows users to manage zones and nodes with a JSON configuration with commands. 
    More information can be found here: https://github.com/Luis-Tanese/uZones/wiki/Home.
*/

configFilePath = "uZonesConfig.json";
zoneDataFilePath = "uZonesData.json";

zones = [];
zoneConfig = {};
predefinedFlags = [];
flagPacks = {};

event onLoad() {
    loadZoneConfig();
    loadZonesFromData();
    logger.log("uZones by Tanese V2.0 successfully loaded!");
}

// Functions

function loadZoneConfig() {
    jsonData = file.read(configFilePath);
    if (jsonData == "") {
        zoneConfig = {
            "predefinedFlags": ["nodamage", "nobuild", "nocraft", "novehicledamage"],
            "flagPacks": {
                "safezone": ["nodamage", "nobuild", "novehicledamage"]
            },
            "customFlags": {}
        };
        saveZoneConfig();
    } else {
        zoneConfig = deserialize(jsonData);
        predefinedFlags = zoneConfig["predefinedFlags"];
        flagPacks = zoneConfig["flagPacks"];
    }
}

function saveZoneConfig() {
    jsonData = zoneConfig.serialize();
    file.writeAll(configFilePath, jsonData);
}

function loadZonesFromData() {
    jsonData = file.read(zoneDataFilePath);
    if (jsonData == "") {
        zones = [];
    } else {
        zones = deserialize(jsonData);
        foreach (zone in zones) {
            if (zone["nodes"].count >= 3) {
                zone["isReady"] = true;
            }
        }
        zones = filterZonesByReady(zones);
    }
}

function saveZonesToData() {
    jsonData = zones.serialize();
    file.writeAll(zoneDataFilePath, jsonData);
}

function filterZonesByReady(zones) {
    readyZones = [];
    foreach (zone in zones) {
        if (zone["isReady"] == true) {
            readyZones.add(zone);
        }
    }
    return readyZones;
}

function addPlayerToZone(zone, playerId) {
    if (!zone.containsKey("players")) {
        zone["players"] = [];
    }
    if (!zone["players"].contains(playerId)) {
        zone["players"].add(playerId);
    }
}

function removePlayerFromZone(zone, playerId) {
    if (zone.containsKey("players")) {
        zone["players"].remove(playerId);
    }
}

function filterEnteredZones(currentZones, previousZones) {
    result = [];
    foreach (zone in currentZones) {
        if (!previousZones.contains(zone)) {
            result.add(zone);
        }
    }
    return result;
}

function filterExitedZones(previousZones, currentZones) {
    result = [];
    foreach (zone in previousZones) {
        if (!currentZones.contains(zone)) {
            result.add(zone);
        }
    }
    return result;
}

function checkPlayerZone(playerPos) {
    insideZones = [];
    foreach (zone in zones) {
        if (zone["isReady"] and isPointInZone(playerPos, zone["nodes"])) {
            insideZones.add(zone);
        }
    }
    return insideZones;
}

function handleAdd(player, type, zoneName, flag) {
    zone = getZoneByName(zoneName);
    if (type == "zone") {
        if (zone != null) {
            player.message("Zone already exists!", "red");
            return;
        }
        zones.add({ "zoneName": zoneName, "nodes": [], "isReady": false });
        saveZonesToData();
        player.message("Zone {0} added. Add atleast 3 nodes for the zone to be ready.".format(zoneName), "green");
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
        if (zone["nodes"].count >= 3 and zone["isReady"] == false) {
            zone["isReady"] = true;
            player.message("Zone {0} is now ready.".format(zoneName), "green");
        }
        saveZonesToData();
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
                saveZonesToData();
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
            saveZonesToData();
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
        saveZonesToData();
        player.message("Zone {0} removed.".format(zoneName), "green");
    } else if (type == "node" and nodeIndex != null) {
        if (zone == null or nodeIndex.toNumber() >= zone["nodes"].count) {
            player.message("Invalid zone or node number.", "red");
            return;
        }
        zone["nodes"].removeAt(nodeIndex.toNumber());
        saveZonesToData();
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
        saveZonesToData();
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
        saveZonesToData();
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
        saveZonesToData();
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
    zonesPlayerIsIn = player.getData("zone");
    if (zonesPlayerisIn == null) {
        zonesPlayerIsIn  = [];
    }
    womp = false;
    foreach (zoneName in zonesPlayerIsIn) {
        zone = getZoneByName(zoneName);
        if (zone != null and zone.containsKey("flags") and zone["flags"].contains(flag)) {
            womp = true;
            break;
        }
    }
    return womp;
}

function filterArray(array, predicate) {
    result = [];
    foreach (item in array) {
        if (predicate(item)) {
            result.add(item);
        }
    }
    return result;
}

function hasOverridePermission(player, flag) {
    return player.hasPermission("uZones.override." + flag);
}

function isObjectInZone(position) {
    insideZones = [];
    foreach (zone in zones) {
        if (zone["isReady"] == true and isPointInZone(position, zone["nodes"])) {
            insideZones.add(zone["zoneName"]);
        }
    }
    return insideZones;
}

function zoneHasFlagForZone(zoneName, flag) {
    zone = getZoneByName(zoneName);
    if (zone != null and zone.containsKey("flags") and zone["flags"].contains(flag)) {
        return true;
    }
    return false;
}

// Events

event onPlayerPositionUpdated(player) {
    playerPos = player.position;
    insideZones = checkPlayerZone(playerPos);
    previousZones = player.getData("zone");
    if (previousZones == null) {
        previousZones = [];
    }
    enteredZones = filterEnteredZones(currentZones, previousZones);
    exitedZones = filterExitedZones(previousZones, currentZones);
    foreach (zone in enteredZones) {
        addPlayerToZone(zone, player.id);
    }
    foreach (zone in exitedZones) {
        removePlayerFromZone(zone, player.id);
    }
    player.setData("zone", insideZones);
    saveZonesToData();
}

event onBarricadeBuild(player, id, position, cancel) {
    if (zoneHasFlag(player, "nobuild") and !hasOverridePermission(player, "nobuild")) {
        cancel = true;
    }
}

event onPlayerCrafted(player, itemId, cancel) {
    if (zoneHasFlag(player, "nocraft") and !hasOverridePermission(player, "nocraft")) {
        cancel = true;
    }
}

event onPlayerDamaged(victim, killer, cancel, damage, cause, limb) {
    if (zoneHasFlag(victim, "nodamage") and !hasOverridePermission(victim, "nodamage")) {
        cancel = true;
    }
}

event onStructureBuild(player, id, position, cancel) {
    if (zoneHasFlag(player, "nobuild") and !hasOverridePermission(player, "nobuild")) {
        cancel = true;
    }
}

event onPlayerTakingItem(player, itemId, cancel) {
    if (zoneHasFlag(player, "noitemtake") and !hasOverridePermission(player, "noitemtake")) {
        cancel = true;
    }
}

event onVehicleCarjack(player, vehicle, force, torque, cancel) {
    if (zoneHasFlag(player, "nocarjack") and !hasOverridePermission(player, "nocarjack")) {
        cancel = true;
    }
}

event onSiphonVehicleRequest(player, vehicle, amount, cancel) {
    if (zoneHasFlag(player, "nosiphon") and !hasOverridePermission(player, "nosiphon")) {
        cancel = true;
    }
}

event onVehicleHook(player, vehicle, vehicleHooked, cancel) {
    if (zoneHasFlag(player, "nohook") and !hasOverridePermission(player, "nohook")) {
        cancel = true;
    }
}

event onVehicleLockpick(player, vehicle, cancel) {
    if (zoneHasFlag(player, "nolockpick") and !hasOverridePermission(player, "nolockpick")) {
        cancel = true;
    }
}

event onVehicleTireDamaged(player, vehicle, cause, cancel) {
    if (zoneHasFlag(player, "novehicledamage") and !hasOverridePermission(player, "novehicledamage")) {
        cancel = true;
    }
}

event onBarricadeDamaged(player, barricade, damage, cause, cancel) {
    if (player != null) {
        if (zoneHasFlag(player, "noraid") and !hasOverridePermission(player, "noraid")) {
            cancel = true;
        }
    } else {
        objectZones = isObjectInZone(barricade.position);
        foreach (zoneName in objectZones) {
            if (zoneHasFlagForZone(zoneName, "noraid")) {
                cancel = true;
                break;
            }
        }
    }
}

event onStructureDamaged(player, structure, damage, cause, cancel) {
    if (player != null) {
        if (zoneHasFlag(player, "noraid") and !hasOverridePermission(player, "noraid")) {
            cancel = true;
        }
    } else {
        objectZones = isObjectInZone(structure.position);
        foreach (zoneName in objectZones) {
            if (zoneHasFlagForZone(zoneName, "noraid")) {
                cancel = true;
                break;
            }
        }
    }
}

event onVehicleDamaged(vehicle, player, cause, damage, cancel) {
    if (player != null) {
        if (zoneHasFlag(player, "novehicledamage") and !hasOverridePermission(player, "novehicledamage")) {
            cancel = true;
        }
    } else {
        objectZones = isObjectInZone(vehicle.position);
        foreach (zoneName in objectZones) {
            if (zoneHasFlagForZone(zoneName, "novehicledamage")) {
                cancel = true;
                break;
            }
        }
    }
}

// Command

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
        } else if (action == "json") {
            if (arguments[1] != "refresh") {
                player.message("Error: Missing refresh argument.", "red");
                return;
            }
            loadZoneConfig();
        } else {
            player.message("Error: Unknown action.", "red");
        }
    }
}
