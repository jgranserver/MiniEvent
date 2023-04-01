using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic.CompilerServices;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Xml.Xsl;
using System.IO;
using System;
using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using Microsoft.Xna.Framework;
using TShockAPI.Hooks;
using Terraria.ID;
using System.Timers;

namespace MiniEvent
{
    [ApiVersion(2, 1)]
    public class MiniEvent : TerrariaPlugin
    {
        private bool _isActive;
        private TSPlayer _owner;
        private DateTime _lastEventTriggeredTime = DateTime.MinValue;
        private TimeSpan triggerElapse;
        static string configPath = Path.Combine(TShock.SavePath, "minieventconfig.json");
        Config config = Config.Read(configPath);

        public override string Name => "MiniEvent";
        public override string Author => "jgranserver";
        public override string Description => "A mini event plugin. Killing the Traveling Merchant!";
        public override Version Version => new Version(1, 0, 0);

        public MiniEvent(Main game) : base(game)
        {
        }

        public override void Initialize()
        {
            Config.Read(configPath);
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            ServerApi.Hooks.NetSendData.Register(this, NpcKill);
            TShockAPI.Hooks.PlayerHooks.PlayerPostLogin += OnServerJoin;
            TShockAPI.Hooks.PlayerHooks.PlayerLogout += OnServerLeave;

        }

        private void OnInitialize(EventArgs args)
        {
            Commands.ChatCommands.Add(new Command("minievent", MiniEventCommand, "event"));
        }

        private void OnServerJoin(PlayerPostLoginEventArgs args)
        {
            var player = args.Player;
            if (args == null)
            {
                return;
            }

            if (player != null && player.Group.Name == "owner")
            {
                _owner = player;

                triggerElapse = TimeSpan.FromSeconds(600);
                TShock.Utils.Broadcast($"{_owner.Name} the owner status is active. A mini event has started!", Color.LightCyan);
                TShock.Utils.Broadcast("A mini event has started!", Color.LightCyan);
                _isActive = true;
            }
            else
            {
                return;
            }
        }

        private void OnServerLeave(PlayerLogoutEventArgs args)
        {
            var player = args.Player;

            // If the leaving player is the owner, end the event.
            if (_isActive && player != null && player.Group.Name == "owner")
            {
                _owner = player;

                TShock.Utils.Broadcast($"Owner {_owner.Name} leaves the server and mini event has ended!", Color.LightCyan);
                _isActive = false;
                _owner = null;
            }
            else
            {
                TShock.Utils.Broadcast($"See you {player.Name} and lets play again together!", Color.LightCyan);
            }
        }

        private void NpcKill(SendDataEventArgs args)
        {
            if (args.MsgId != PacketTypes.NpcStrike)
            {
                return;
            }

            var npc = Main.npc[args.number];

            if (args.ignoreClient == -1)
            {
                return;
            }

            var player = TSPlayer.FindByNameOrID(args.ignoreClient.ToString())[0];

            if (!(npc.life <= 0))
            {
                return;
            }

            var rewardItem = config.RewardItem;
            var rewardStack = config.RewardStack;
            var eventNPC = config.TargetNPC;

            if (npc.netID == eventNPC)
            {
                if (DateTime.UtcNow - _lastEventTriggeredTime <= triggerElapse)
                {
                    var timeLeft = DateTime.UtcNow.Subtract(_lastEventTriggeredTime);
                    player.SendInfoMessage($"Next npc hunt will be available in {timeLeft.Minutes} minute/s.");
                    return;
                }
                player.GiveItem(rewardItem, rewardStack);
                TShock.Utils.Broadcast($"{player.Name} won {rewardStack} x {TShock.Utils.GetItemById(rewardItem).Name} from the event!", Color.LightGreen);
                TShock.Utils.Broadcast($"From killing {config.TargetNPC}!", Color.LightGreen);
                _lastEventTriggeredTime = DateTime.Now;
            }
        }

        public void MiniEventCommand(CommandArgs args)
        {
            var player = args.Player;
            var cmd = args.Parameters;

            if (!(player != null || player.IsLoggedIn))
            {
                return;
            }

            if (cmd.Count <= 0)
            {
                if (player.Group.HasPermission("minievent.update"))
                {
                    player.SendErrorMessage("To update event configuration: /event update <targetnpc> <rewarditem> <reward amount>");
                    player.SendErrorMessage("To reload configuration: /event reload");
                }
                player.SendErrorMessage("Invalid command: /event status");
                return;
            }

            if (cmd.Count >= 1 || cmd.Count <= 4)
            {
                switch (cmd[0])
                {
                    case "status":

                        var npcName = TShock.Utils.GetNPCById((int)config.TargetNPC);
                        var itemName = TShock.Utils.GetItemById((int)config.RewardItem);
                        if (_owner.Active && _isActive)
                        {
                            player.SendInfoMessage("Event status: Active");
                            player.SendInfoMessage($"Target NPC: {npcName.TypeName}");
                            player.SendInfoMessage($"Reward: {config.RewardStack} x {itemName.Name}");
                        }
                        else
                        {
                            player.SendInfoMessage("Event status: No event Active");
                        }
                        break;

                    case "update":

                        if (cmd[0].Length == 0)
                        {
                            args.Player.SendErrorMessage("Invalid command.");
                            return;
                        }

                        var npcs = TShock.Utils.GetNPCByIdOrName(cmd[1]);

                        if (npcs.Count == 0)
                        {
                            args.Player.SendErrorMessage("Invalid mob type!");
                        }
                        else if (npcs.Count > 1)
                        {
                            args.Player.SendMultipleMatchError(npcs.Select(n => $"{n.FullName}({n.type})"));
                        }
                        else
                        {
                            var npc = npcs[0];

                            if (npc.type >= 1 && npc.type < Terraria.ID.NPCID.Count)
                            {
                                config.TargetNPC = (short)npc.netID;
                            }
                        }

                        Item item;
                        List<Item> matchedItems = TShock.Utils.GetItemByIdOrName(cmd[2]);

                        if (matchedItems.Count == 0)
                        {
                            player.SendErrorMessage("Invalid item type!");
                            return;
                        }
                        else if (matchedItems.Count > 1)
                        {
                            player.SendMultipleMatchError(matchedItems.Select(i => $"{i.Name}({i.netID})"));
                            return;
                        }
                        else
                        {
                            item = matchedItems[0];
                            config.RewardItem = (short)item.netID;
                        }

                        if (!(cmd[2].Length == 0))
                        {
                            config.RewardStack = int.Parse(cmd[3]);
                        }
                        else
                        {
                            player.SendErrorMessage("Specify the amount for the reward int the 3rd parameter.");
                        }

                        Config.Write(configPath, config);

                        break;

                    case "reload":
                        Config.Reload(configPath, ref config);
                        break;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.NetSendData.Deregister(this, NpcKill);
                TShockAPI.Hooks.PlayerHooks.PlayerPostLogin -= OnServerJoin;
                TShockAPI.Hooks.PlayerHooks.PlayerLogout -= OnServerLeave;
            }
            base.Dispose(disposing);
        }
    }
}