using TShockAPI;
using Terraria;
using TerrariaApi.Server;
using Microsoft.Xna.Framework;
using TShockAPI.Hooks;
using Terraria.Net;
using Terraria.Localization;

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
            ServerApi.Hooks.GameUpdate.Register(this, OnStatusUpdate);
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

                TShock.Utils.Broadcast($"{_owner.Name} the owner status is active. A mini event has started!", Color.LightCyan);
                TShock.Utils.Broadcast("A mini event has started!", Color.LightCyan);
                _isActive = true;
            }

            // string statusActive = $"Event Status: Active \nTarget NPC: {TShock.Utils.GetNPCById(config.TargetNPC).TypeName} \nReward: {config.RewardStack} x {TShock.Utils.GetItemById(config.RewardItem).Name}";

            // if (_isActive)
            // {
            //     NetMessage.SendData((int)PacketTypes.Status, -1, -1, NetworkText.FromLiteral(statusActive), 255, 100f, 100f, 0);
            // }
            // else
            // {
            //     EventStatus("Event Status: Inactive");
            // }
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

        private void OnStatusUpdate(EventArgs args)
        {
            var player = TSPlayer.All;

            if (player != null && player.Group.Name == "owner")
            {
                _isActive = true;
            }

            string statusActive = $"\n \n \n \n \n \n \n \n \n \n \n \n \n \n \nEvent Status: Active \nTarget NPC: {TShock.Utils.GetNPCById(config.TargetNPC).TypeName} \nReward: {config.RewardStack} x {TShock.Utils.GetItemById(config.RewardItem).Name}";

            if (_isActive)
            {
                NetMessage.SendData((int)PacketTypes.Status, -1, -1, NetworkText.FromLiteral(statusActive), 255, (BitsByte)0x1);
            }
            else
            {
                EventStatus("Event Status: Inactive");
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
                triggerElapse = TimeSpan.FromMinutes(600);
                if (DateTime.UtcNow - _lastEventTriggeredTime <= triggerElapse)
                {
                    var timeLeft = DateTime.UtcNow - _lastEventTriggeredTime;
                    player.SendInfoMessage($"Next npc hunt will be available in {timeLeft.Minutes} minute/s.");
                    return;
                }
                player.GiveItem(rewardItem, rewardStack);
                TShock.Utils.Broadcast($"{player.Name} won {rewardStack} x {TShock.Utils.GetItemById(rewardItem).Name} from the event!", Color.LightGreen);
                TShock.Utils.Broadcast($"From killing {TShock.Utils.GetNPCById(config.TargetNPC).TypeName}!", Color.LightGreen);
                _lastEventTriggeredTime = DateTime.Now;
            }

            // var data = args.MsgId;
            // string statusActive = "Event Status: Active";
            // string statusTarget = $"Target NPC: {TShock.Utils.GetNPCById(config.TargetNPC).TypeName}";
            // string statusReward = $"Reward: {config.RewardStack} x {TShock.Utils.GetItemById(config.RewardItem).Name}";

            // if(data == PacketTypes.Status)
            // {
            //     NetMessage.SendData((int)data, -1, -1, NetworkText.FromLiteral(statusActive));
            //     NetMessage.SendData((int)data, -1, -1, NetworkText.FromLiteral(statusTarget));
            //     NetMessage.SendData((int)data, -1, -1, NetworkText.FromLiteral(statusReward));
            // }
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
                ServerApi.Hooks.GameUpdate.Deregister(this, OnStatusUpdate);

            }
            base.Dispose(disposing);
        }

        public void EventStatus(string statusText)
        {
            NetMessage.SendData(9, -1, -1, NetworkText.FromLiteral(statusText));
        }
    }
}