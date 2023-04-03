## MiniEvent Plugin Documentation
# Overview
MiniEvent is a Terraria plugin that allows players to participate in a mini event by killing a target NPC. When the event is active, players who successfully kill the target NPC will receive a reward item. This plugin is designed to be customizable with a JSON configuration file. MiniEvent plugin will broadcast to all players on the server when the event starts or ends.

# Installation
To use the MiniEvent plugin, you must have TShock installed on your Terraria server. TShock is a server modification for Terraria that provides many additional features, including the ability to use plugins like MiniEvent. The following steps will guide you through the installation of MiniEvent plugin:

Download the plugin from its official source or another trusted source.
Copy the MiniEvent.dll file into your TShock ServerPlugins folder.
Restart your Terraria server to load the plugin.

# Usage
Once you have installed the plugin, you can use the following commands in-game:

/event status - for players to see the event status.
/event update <npcID> <itemID> <itemCount> - stops the mini event. Only players in the "owner" group can update the event.
