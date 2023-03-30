using System.Runtime.CompilerServices;
using System.IO.Pipes;
using TShockAPI;
using Terraria;
using Terraria.ID;
using Newtonsoft.Json;

namespace MiniEvent
{
    public class Config
    {
        public string OwnerPermission { get; set;} = "jgran";
        public double EventCooldown { get; set; } = 20; // in seconds

        public short RewardItem { get; set; } = ItemID.LifeCrystal;
        public int RewardStack { get; set; } = 3;

        public short TargetNPC { get; set;} = NPCID.TravellingMerchant;

        public static Config Read(string path)
        {
            if (!File.Exists(path))
            {
                Config.Write(path, new Config());
                TShock.Log.ConsoleInfo($"Created new config file at {path}");
            }

            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<Config>(json);
        }

        public static void Write(string path, Config config)
        {
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        public static void Reload(string path)
        {
            Config.Read(path);
            TShock.Log.ConsoleInfo($"Reloaded config file at {path}");
        }
    }
}
