using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using Newtonsoft.Json.Linq;
using ShopAPI;

namespace ShopDefuser
{
    public class ShopDefuser : BasePlugin
    {
        public override string ModuleName => "[SHOP] Defuser";
        public override string ModuleDescription => "";
        public override string ModuleAuthor => "E!N";
        public override string ModuleVersion => "v1.0.1";

        private IShopApi? SHOP_API;
        private const string CategoryName = "Defuser";
        public static JObject? JsonDefuser { get; private set; }
        private readonly PlayerDefuser[] playerDefusers = new PlayerDefuser[65];

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            SHOP_API = IShopApi.Capability.Get();
            if (SHOP_API == null) return;

            LoadConfig();
            InitializeShopItems();
            SetupTimersAndListeners();
        }

        private void LoadConfig()
        {
            string configPath = Path.Combine(ModuleDirectory, "../../configs/plugins/Shop/Defuser.json");
            if (File.Exists(configPath))
            {
                JsonDefuser = JObject.Parse(File.ReadAllText(configPath));
            }
        }

        private void InitializeShopItems()
        {
            if (JsonDefuser == null || SHOP_API == null) return;

            SHOP_API.CreateCategory(CategoryName, "Дефузер");

            foreach (var item in JsonDefuser.Properties().Where(p => p.Value is JObject))
            {
                Task.Run(async () =>
                {
                    int itemId = await SHOP_API.AddItem(
                        item.Name,
                        (string)item.Value["name"]!,
                        CategoryName,
                        (int)item.Value["price"]!,
                        (int)item.Value["sellprice"]!,
                        (int)item.Value["duration"]!
                    );
                    SHOP_API.SetItemCallbacks(itemId, OnClientBuyItem, OnClientSellItem, OnClientToggleItem);
                }).Wait();
            }
        }

        private void SetupTimersAndListeners()
        {
            RegisterListener<Listeners.OnClientDisconnect>(playerSlot => playerDefusers[playerSlot] = null!);
        }

        public HookResult OnClientBuyItem(CCSPlayerController player, int itemId, string categoryName, string uniqueName, int buyPrice, int sellPrice, int duration, int count)
        {
            playerDefusers[player.Slot] = new PlayerDefuser(itemId);
            return HookResult.Continue;
        }

        public HookResult OnClientToggleItem(CCSPlayerController player, int itemId, string uniqueName, int state)
        {
            if (state == 1)
            {
                playerDefusers[player.Slot] = new PlayerDefuser(itemId);
            }
            else if (state == 0)
            {
                OnClientSellItem(player, itemId, uniqueName, 0);
            }
            return HookResult.Continue;
        }

        public HookResult OnClientSellItem(CCSPlayerController player, int itemId, string uniqueName, int sellPrice)
        {
            playerDefusers[player.Slot] = null!;
            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player != null && !player.IsBot && playerDefusers[player.Slot] != null)
            {
                GiveDefuser(player);
            }
            return HookResult.Continue;
        }

        private static bool HasWeapon(CCSPlayerController player, string weaponName)
        {
            if (!player.IsValid || !player.PawnIsAlive)
                return false;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || pawn.WeaponServices == null)
                return false;

            foreach (var weapon in pawn.WeaponServices.MyWeapons)
            {
                if (weapon?.Value?.IsValid == true && weapon.Value.DesignerName?.Contains(weaponName) == true)
                {
                    return true;
                }
            }
            return false;
        }

        private static void GiveDefuser(CCSPlayerController player)
        {
            if (player == null) return;

            var playerPawn = player.PlayerPawn.Value;
            if (playerPawn == null) return;

            if (player.TeamNum == 3 && !HasWeapon(player, "item_defuser"))
            {
                if (playerPawn != null && playerPawn.ItemServices != null)
                {
                    var itemServices = new CCSPlayer_ItemServices(playerPawn.ItemServices.Handle)
                    {
                        HasDefuser = true
                    };
                }
            }
        }

        public record class PlayerDefuser(int ItemID);
    }
}