using UnityEngine;
using HookHorizon.Domain.Fishing;
using HookHorizon.Domain.Inventory;
using HookHorizon.Unity.Data;
using HookHorizon.Unity.Economy;
using HookHorizon.Unity.Fishing;
using HookHorizon.Unity.Player;

namespace HookHorizon.Unity.View
{
    /// <summary>
    /// A lightweight immediate-mode HUD used for the prototype: gold, prompts, the bite
    /// alert, the reel-QTE bar, catch popups and the inventory panel. It is a pure *view* —
    /// it only reads state and subscribes to <see cref="FishingController"/> events, so
    /// replacing it with a proper uGUI/Canvas HUD later means writing another subscriber
    /// and deleting this file. Nothing in the gameplay layer references it.
    /// </summary>
    public sealed class PrototypeHud : MonoBehaviour
    {
        private FishingController _fishing;
        private InventoryComponent _inventory;
        private Wallet _wallet;
        private WaterSensor _water;
        private PlayerInteractor _interactor;
        private IInputSource _input;
        private RodLoadout _loadout;
        private RodShopCustomer _shop;
        private BoatShopCustomer _boatShop;
        private BoatController _boat;
        private BoatLoadout _boatLoadout;
        private FishIconResolver _icons;

        private string _message = "";
        private Color _messageColor = Color.white;
        private float _messageTimer;
        private const float MessageDuration = 3.2f;
        private Sprite _messageIcon;
        private Color _messageIconTint = Color.white;

        private bool _showInventory;
        private GUIStyle _big;
        private GUIStyle _label;

        /// <summary>Called by the bootstrap once every dependency exists.</summary>
        public void Bind(FishingController fishing, InventoryComponent inventory, Wallet wallet,
            WaterSensor water, PlayerInteractor interactor, IInputSource input,
            RodLoadout loadout, RodShopCustomer shop, FishIconResolver icons,
            BoatShopCustomer boatShop, BoatController boat, BoatLoadout boatLoadout)
        {
            Unsubscribe();

            _icons = icons;

            _fishing = fishing;
            _inventory = inventory;
            _wallet = wallet;
            _water = water;
            _interactor = interactor;
            _input = input;
            _loadout = loadout;
            _shop = shop;
            _boatShop = boatShop;
            _boat = boat;
            _boatLoadout = boatLoadout;

            _fishing.CastRejected += OnCastRejected;
            _fishing.Landed += OnLanded;
            _fishing.FishLost += OnFishLost;
            _interactor.Sold += OnSold;
            _shop.Bought += OnBought;
            _shop.Equipped += OnEquipped;
            _shop.Denied += OnDenied;
            if (_boatShop != null)
            {
                _boatShop.Bought += OnBoatBought;
                _boatShop.Activated += OnBoatActivated;
                _boatShop.Denied += OnDenied;
            }
            if (_boat != null)
            {
                _boat.Boarded += OnBoarded;
                _boat.Disembarked += OnDisembarked;
            }
        }

        private void OnDestroy() => Unsubscribe();

        private void Unsubscribe()
        {
            if (_fishing != null)
            {
                _fishing.CastRejected -= OnCastRejected;
                _fishing.Landed -= OnLanded;
                _fishing.FishLost -= OnFishLost;
            }
            if (_interactor != null) _interactor.Sold -= OnSold;
            if (_shop != null)
            {
                _shop.Bought -= OnBought;
                _shop.Equipped -= OnEquipped;
                _shop.Denied -= OnDenied;
            }
            if (_boatShop != null)
            {
                _boatShop.Bought -= OnBoatBought;
                _boatShop.Activated -= OnBoatActivated;
                _boatShop.Denied -= OnDenied;
            }
            if (_boat != null)
            {
                _boat.Boarded -= OnBoarded;
                _boat.Disembarked -= OnDisembarked;
            }
        }

        // ---- Event handlers -> transient messages ---------------------------------

        private void OnCastRejected(string reason) => SetMessage(reason, new Color(1f, 0.85f, 0.3f));

        private void OnFishLost(string reason) => SetMessage(reason, new Color(1f, 0.5f, 0.4f));

        private void OnSold(SaleReport r) =>
            SetMessage($"Sold {r.FishSold} fish for {r.GoldEarned}g", new Color(1f, 0.85f, 0.2f));

        private void OnBought(RodData rod) =>
            SetMessage($"Bought & equipped {rod.DisplayName}!", new Color(0.5f, 1f, 0.6f));

        private void OnEquipped(RodData rod) =>
            SetMessage($"Equipped {rod.DisplayName}", new Color(0.7f, 0.9f, 1f));

        private void OnDenied(string reason) => SetMessage(reason, new Color(1f, 0.7f, 0.3f));

        private void OnBoatBought(BoatData boat) =>
            SetMessage($"Bought the {boat.DisplayName}!", new Color(0.5f, 1f, 0.6f));

        private void OnBoatActivated(BoatData boat) =>
            SetMessage($"Now sailing the {boat.DisplayName}", new Color(0.7f, 0.9f, 1f));

        private void OnBoarded(BoatData boat) =>
            SetMessage($"Boarded the {boat.DisplayName} — [E] to disembark at shore", new Color(0.6f, 0.9f, 1f));

        private void OnDisembarked() =>
            SetMessage("Back on dry land", new Color(0.8f, 0.9f, 1f));

        private void OnLanded(CaughtFish fish, AddResult add)
        {
            Sprite icon = _icons?.Resolve(fish.SpeciesId);
            Color tint = _icons != null ? _icons.MutationTint(fish.SpeciesId, fish.MutationId) : Color.white;
            if (add == AddResult.Added)
                SetMessage($"Landed {fish.DisplayName}!  {fish.WeightKg:0.0}kg  ·  worth {fish.Value}g",
                    new Color(0.5f, 1f, 0.6f), icon, tint);
            else
                SetMessage($"Bag full — released {fish.DisplayName}", new Color(1f, 0.7f, 0.3f), icon, tint);
        }

        private void SetMessage(string text, Color color, Sprite icon = null, Color? iconTint = null)
        {
            _message = text;
            _messageColor = color;
            _messageIcon = icon;
            _messageIconTint = iconTint ?? Color.white;
            _messageTimer = MessageDuration;
        }

        // ---- Update ---------------------------------------------------------------

        private void Update()
        {
            if (_messageTimer > 0f) _messageTimer -= Time.deltaTime;
            if (_input != null && _input.ToggleInventoryDown && !PauseMenuUI.IsOpen) _showInventory = !_showInventory;
        }

        // ---- Rendering ------------------------------------------------------------

        private void OnGUI()
        {
            if (_fishing == null) return;
            EnsureStyles();

            DrawTopBar();
            DrawPrompt();
            if (_fishing.Phase == FishingPhase.Bite) DrawBiteAlert();
            if (_fishing.Phase == FishingPhase.Reeling) DrawReelQte();
            if (_shop != null && _shop.ShopInRange != null) DrawShop();
            if (_boatShop != null && _boatShop.ShopInRange != null) DrawBoatShop();
            if (_messageTimer > 0f) DrawMessage();
            if (_showInventory) DrawInventory();
        }

        private void EnsureStyles()
        {
            if (_big != null) return;
            _big = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _label = new GUIStyle(GUI.skin.label) { fontSize = 15 };
        }

        private void DrawTopBar()
        {
            GUI.color = Color.white;
            var style = new GUIStyle(_label) { fontSize = 18, fontStyle = FontStyle.Bold };
            GUI.Label(new Rect(16, 12, 400, 28), $"Gold: {_wallet.Gold}g", style);
            string rodName = _loadout != null && _loadout.Equipped != null ? _loadout.Equipped.DisplayName : "—";
            string boatName = _boatLoadout != null && _boatLoadout.ActiveBoat != null ? _boatLoadout.ActiveBoat.DisplayName : "—";
            string sailing = _boat != null && _boat.IsSailing ? " (sailing)" : "";
            GUI.Label(new Rect(16, 40, 760, 24),
                $"Rod: {rodName}    Boat: {boatName}{sailing}    Bag: {_inventory.Inventory.Count}/{_inventory.Inventory.MaxSlots}  ({_inventory.Inventory.TotalWeightKg:0.0}/{_inventory.Inventory.MaxWeightKg:0}kg)",
                _label);
        }

        private void DrawPrompt()
        {
            string prompt = BuildPrompt();
            if (string.IsNullOrEmpty(prompt)) return;

            GUI.color = new Color(1f, 1f, 1f, 0.95f);
            GUI.Label(new Rect(Screen.width * 0.5f - 300, Screen.height - 60, 600, 30), prompt,
                new GUIStyle(_label) { alignment = TextAnchor.MiddleCenter, fontSize = 17 });
        }

        private string BuildPrompt()
        {
            switch (_fishing.Phase)
            {
                case FishingPhase.Idle:
                    if (_interactor != null && _interactor.MerchantInRange != null && _inventory.Inventory.Count > 0)
                        return $"[B] Sell {_inventory.Inventory.Count} fish at {_interactor.MerchantInRange.DisplayName}    ·    [I] Inventory";
                    if (_shop != null && _shop.ShopInRange != null)
                        return $"At {_shop.ShopInRange.DisplayName} — press [1]-[{_shop.ShopInRange.RodsForSale.Count}] to buy or equip a rod";
                    if (_boatShop != null && _boatShop.ShopInRange != null)
                        return $"At {_boatShop.ShopInRange.DisplayName} — press [1]-[{_boatShop.ShopInRange.BoatsForSale.Count}] to buy or switch boat";
                    if (_boat != null && _boat.IsSailing)
                    {
                        if (_boat.AtDepthLimit())
                            return "The water ahead is too deep for this boat — buy a deeper one at the dock.";
                        string sail = _water.IsWaterInRange
                            ? $"[Space] Cast into {(_water.CurrentBiome != null ? _water.CurrentBiome.DisplayName : "the water")}"
                            : "Sailing the open water";
                        return _boat.CanToggleHere ? $"{sail}    ·    [E] Disembark" : $"{sail}    ·    [I] Inventory";
                    }
                    if (_boat != null && _boat.CanToggleHere)
                    {
                        string boatName = _boatLoadout != null && _boatLoadout.ActiveBoat != null ? _boatLoadout.ActiveBoat.DisplayName : "boat";
                        string cast = _water.IsWaterInRange
                            ? $"    ·    [Space] Cast into {(_water.CurrentBiome != null ? _water.CurrentBiome.DisplayName : "the water")}"
                            : "";
                        return $"[E] Board the {boatName}{cast}";
                    }
                    if (_water.IsWaterInRange)
                        return $"[Space] Cast into {(_water.CurrentBiome != null ? _water.CurrentBiome.DisplayName : "the water")}    ·    [I] Inventory";
                    return "Walk to the water's edge with WASD, then press Space to cast.";
                case FishingPhase.Casting: return "Casting…";
                case FishingPhase.WaitingForBite: return "Waiting for a bite…  ([Space] to reel in early)";
                case FishingPhase.Reeling: return "Hold [Space] to reel — keep the fish inside the green zone!";
                default: return "";
            }
        }

        private void DrawBiteAlert()
        {
            GUI.color = Color.Lerp(new Color(1f, 0.9f, 0.2f), Color.red, 1f - _fishing.BiteWindow01);
            GUI.Label(new Rect(Screen.width * 0.5f - 200, Screen.height * 0.5f - 120, 400, 40),
                "! BITE !  Press SPACE", _big);

            // Reaction time bar
            var barRect = new Rect(Screen.width * 0.5f - 150, Screen.height * 0.5f - 78, 300, 14);
            DrawRect(barRect, new Color(0, 0, 0, 0.5f));
            DrawRect(new Rect(barRect.x, barRect.y, barRect.width * _fishing.BiteWindow01, barRect.height),
                new Color(1f, 0.85f, 0.2f));
            GUI.color = Color.white;
        }

        private void DrawReelQte()
        {
            QteMinigame qte = _fishing.CurrentQte;
            if (qte == null) return;

            const float trackW = 46f, trackH = 320f;
            float x = Screen.width * 0.5f - trackW * 0.5f;
            float y = Screen.height * 0.5f - trackH * 0.5f;
            var track = new Rect(x, y, trackW, trackH);

            DrawRect(track, new Color(0f, 0f, 0f, 0.55f));

            // Green catch zone (track 0 = bottom, so invert for screen space)
            float zoneH = qte.Config.BarSize * trackH;
            float zoneY = y + trackH - (qte.BarPosition * trackH) - zoneH;
            DrawRect(new Rect(x, zoneY, trackW, zoneH),
                qte.IsFishInZone ? new Color(0.4f, 1f, 0.5f, 0.85f) : new Color(0.4f, 0.8f, 0.5f, 0.45f));

            // Fish marker
            float fishY = y + trackH - (qte.FishPosition * trackH) - 8f;
            DrawRect(new Rect(x + 6f, fishY, trackW - 12f, 16f), new Color(0.15f, 0.35f, 0.8f));

            // Progress bar to the right of the track
            var prog = new Rect(x + trackW + 12f, y, 18f, trackH);
            DrawRect(prog, new Color(0f, 0f, 0f, 0.55f));
            float fillH = qte.Progress * trackH;
            DrawRect(new Rect(prog.x, prog.y + trackH - fillH, prog.width, fillH), new Color(1f, 0.85f, 0.25f));

            GUI.color = Color.white;
        }

        private void DrawMessage()
        {
            float alpha = Mathf.Clamp01(_messageTimer / 0.8f);
            if (_messageIcon != null)
            {
                const float sz = 56f;
                Color tint = _messageIconTint; tint.a *= alpha;
                FishIconResolver.Draw(
                    new Rect(Screen.width * 0.5f - sz * 0.5f, Screen.height * 0.5f + 150 - sz - 4f, sz, sz),
                    _messageIcon, tint);
            }
            GUI.color = new Color(_messageColor.r, _messageColor.g, _messageColor.b, alpha);
            GUI.Label(new Rect(Screen.width * 0.5f - 350, Screen.height * 0.5f + 150, 700, 34),
                _message, new GUIStyle(_big) { fontSize = 20 });
            GUI.color = Color.white;
        }

        private void DrawInventory()
        {
            var bag = _inventory.Inventory;
            var panel = new Rect(Screen.width - 340, 80, 320, 360);
            DrawRect(panel, new Color(0f, 0f, 0f, 0.75f));

            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 12, panel.y + 8, panel.width - 20, 24),
                $"Bag  ·  {bag.Count}/{bag.MaxSlots}  ·  {bag.TotalWeightKg:0.0}/{bag.MaxWeightKg:0}kg",
                new GUIStyle(_label) { fontStyle = FontStyle.Bold, fontSize = 16 });

            float rowY = panel.y + 40;
            foreach (CaughtFish fish in bag.Items)
            {
                GUI.Label(new Rect(panel.x + 12, rowY, panel.width - 24, 20),
                    $"{fish.DisplayName}  ·  {fish.WeightKg:0.0}kg  ·  {fish.Value}g", _label);
                rowY += 20f;
                if (rowY > panel.yMax - 20f) break;
            }

            if (bag.Count == 0)
                GUI.Label(new Rect(panel.x + 12, rowY, panel.width - 24, 20), "(empty — go catch something!)", _label);
        }

        private void DrawShop()
        {
            var shop = _shop.ShopInRange;
            var rods = shop.RodsForSale;

            float height = 52f + rods.Count * 46f;
            var panel = new Rect(24, Screen.height * 0.5f - height * 0.5f, 380, height);
            DrawRect(panel, new Color(0f, 0f, 0f, 0.8f));

            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 12, panel.y + 8, panel.width - 24, 24),
                $"{shop.DisplayName}   ·   You have {_wallet.Gold}g",
                new GUIStyle(_label) { fontStyle = FontStyle.Bold, fontSize = 16 });

            float rowY = panel.y + 40;
            for (int i = 0; i < rods.Count; i++)
            {
                RodData rod = rods[i];
                bool owned = _loadout != null && _loadout.IsOwned(rod);
                bool equipped = _loadout != null && _loadout.Equipped == rod;
                bool canAfford = _wallet.Gold >= rod.Price;

                string status = equipped ? "EQUIPPED"
                    : owned ? "owned — press to equip"
                    : canAfford ? $"{rod.Price}g — press to buy"
                    : $"{rod.Price}g (not enough gold)";

                Color rowColor = equipped ? new Color(0.6f, 1f, 0.7f)
                    : owned ? new Color(0.8f, 0.9f, 1f)
                    : canAfford ? Color.white
                    : new Color(1f, 0.6f, 0.5f);

                GUI.color = rowColor;
                GUI.Label(new Rect(panel.x + 12, rowY, panel.width - 24, 20),
                    $"[{i + 1}] {rod.DisplayName}  —  {status}",
                    new GUIStyle(_label) { fontStyle = FontStyle.Bold });
                GUI.color = new Color(0.82f, 0.82f, 0.82f);
                GUI.Label(new Rect(panel.x + 26, rowY + 20, panel.width - 34, 18),
                    RodBonusSummary(rod), new GUIStyle(_label) { fontSize = 12 });
                rowY += 46f;
            }
            GUI.color = Color.white;
        }

        private static string RodBonusSummary(RodData r)
        {
            string s = "";
            if (r.LureSpeedBonus != 0f) s += $"{r.LureSpeedBonus:+0%;-0%} lure  ";
            if (r.LuckBonus != 0f) s += $"{r.LuckBonus:+0%;-0%} luck  ";
            if (r.ReactionWindowBonus != 0f) s += $"{r.ReactionWindowBonus:+0%;-0%} reaction  ";
            if (r.QteForgiveness != 0f) s += $"{r.QteForgiveness:+0%;-0%} control  ";
            return s.Length == 0 ? "no bonuses" : s.TrimEnd();
        }

        private void DrawBoatShop()
        {
            var shop = _boatShop.ShopInRange;
            var boats = shop.BoatsForSale;

            float height = 52f + boats.Count * 46f;
            var panel = new Rect(24, Screen.height * 0.5f - height * 0.5f, 380, height);
            DrawRect(panel, new Color(0f, 0f, 0f, 0.8f));

            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 12, panel.y + 8, panel.width - 24, 24),
                $"{shop.DisplayName}   ·   You have {_wallet.Gold}g",
                new GUIStyle(_label) { fontStyle = FontStyle.Bold, fontSize = 16 });

            float rowY = panel.y + 40;
            for (int i = 0; i < boats.Count; i++)
            {
                BoatData boat = boats[i];
                bool owned = _boatLoadout != null && _boatLoadout.IsOwned(boat);
                bool active = _boatLoadout != null && _boatLoadout.ActiveBoat == boat;
                bool canAfford = _wallet.Gold >= boat.Price;

                string status = active ? "ACTIVE"
                    : owned ? "owned — press to use"
                    : canAfford ? $"{boat.Price}g — press to buy"
                    : $"{boat.Price}g (not enough gold)";

                Color rowColor = active ? new Color(0.6f, 1f, 0.7f)
                    : owned ? new Color(0.8f, 0.9f, 1f)
                    : canAfford ? Color.white
                    : new Color(1f, 0.6f, 0.5f);

                GUI.color = rowColor;
                GUI.Label(new Rect(panel.x + 12, rowY, panel.width - 24, 20),
                    $"[{i + 1}] {boat.DisplayName}  —  {status}",
                    new GUIStyle(_label) { fontStyle = FontStyle.Bold });
                GUI.color = new Color(0.82f, 0.82f, 0.82f);
                GUI.Label(new Rect(panel.x + 26, rowY + 20, panel.width - 34, 18),
                    BoatSummary(boat), new GUIStyle(_label) { fontSize = 12 });
                rowY += 46f;
            }
            GUI.color = Color.white;
        }

        private static string BoatSummary(BoatData b)
        {
            string s = $"speed {b.MoveSpeed:0.#}  ·  depth tier {b.MaxDepthTier}";
            string bonus = BoatBonusSummary(b);
            return bonus.Length == 0 ? s : $"{s}  ·  {bonus}";
        }

        private static string BoatBonusSummary(BoatData b)
        {
            string s = "";
            if (b.LureSpeedBonus != 0f) s += $"{b.LureSpeedBonus:+0%;-0%} lure  ";
            if (b.LuckBonus != 0f) s += $"{b.LuckBonus:+0%;-0%} luck  ";
            if (b.ReactionWindowBonus != 0f) s += $"{b.ReactionWindowBonus:+0%;-0%} reaction  ";
            if (b.QteForgiveness != 0f) s += $"{b.QteForgiveness:+0%;-0%} control  ";
            return s.TrimEnd();
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Color prev = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = prev;
        }
    }
}
