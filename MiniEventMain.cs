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

        string configPath = Path.Combine(TShock.SavePath, "minieventconfig.json");

        private Config config = new Config();
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

            if (npc.netID == NPCID.TravellingMerchant)
            {
                if (DateTime.UtcNow - _lastEventTriggeredTime <= triggerElapse)
                {
                    var timeLeft = DateTime.UtcNow.Subtract(_lastEventTriggeredTime);
                    player.SendInfoMessage($"Next npc hunt will be available in {timeLeft.Minutes} minute/s.");
                    return;
                }
                else
                {
                    _isActive = true;
                }
                player.GiveItem(ItemID.LifeCrystal, 3);
                player.SendInfoMessage($"You have won from the event!");
                TShock.Utils.Broadcast($"{player.Name} won the event!", Color.LightGreen);
                _lastEventTriggeredTime = DateTime.Now;
                _isActive = false;
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