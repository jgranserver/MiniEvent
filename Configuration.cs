using TShockAPI;
using Terraria.ID;
using Newtonsoft.Json;

namespace MiniEvent
{
    public class Config
    {
        public double EventCooldown { get; set; }
        public short RewardItem { get; set; }
        public int RewardStack { get; set; }
        public short TargetNPC { get; set;}

        public List<Config> EventSettings = new List<Config>();

        public static Config Read(string path)
        {
            if (!File.Exists(path))
            {
                Config config = new Config();

                config.EventSettings.Add(new Config()
                {
                    EventCooldown = 600,
                    RewardItem = ItemID.LifeCrystal,
                    RewardStack = 3,
                    TargetNPC = NPCID.TravellingMerchant
                });
            }
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<Config>(json);
        }

        public static void Write(string path, Config config)
        {
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        public static void Reload(string path, ref Config config)
        {
            config = Config.Read(path);
            TShock.Log.ConsoleInfo($"Reloaded config file at {path}");
        }
    }
}
