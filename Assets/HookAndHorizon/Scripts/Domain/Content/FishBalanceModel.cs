using System;
using System.Collections.Generic;
using System.Text;
using HookHorizon.Domain.Fishing;

namespace HookHorizon.Domain.Content
{
    /// <summary>The gameplay numbers the balance model derives for one species.</summary>
    public readonly struct FishBalance
    {
        public readonly Rarity Rarity;
        public readonly float MinWeightKg;
        public readonly float MaxWeightKg;
        public readonly float BaseValuePerKg;
        public readonly float MutationChance;

        /// <summary>Weight-band bucket the min/max came from (micro..monster). Informational only.</summary>
        public readonly string WeightBand;

        public FishBalance(Rarity rarity, float minWeightKg, float maxWeightKg,
            float baseValuePerKg, float mutationChance, string weightBand)
        {
            Rarity = rarity;
            MinWeightKg = minWeightKg;
            MaxWeightKg = maxWeightKg;
            BaseValuePerKg = baseValuePerKg;
            MutationChance = mutationChance;
            WeightBand = weightBand;
        }
    }

    /// <summary>
    /// Turns a bare fish name + Exclusive/General flag into balanced gameplay numbers:
    /// rarity, weight band (min/max kg), base gold/kg and mutation chance. Deterministic —
    /// the same name always yields the same fish — so the CSV importer can regenerate the
    /// whole 500-fish roster reproducibly. Every balance knob for content lives in this file.
    ///
    /// NOTE: the per-catch size tag (Tiny..Monster) is NOT set here. It is derived at catch
    /// time from where the rolled weight lands inside the [min,max] band (see SizeClassifier),
    /// so every species can produce a Tiny or a Monster specimen of itself.
    /// </summary>
    public static class FishBalanceModel
    {
        // ---------- RARITY -------------------------------------------------------
        // Relative tier weights [Common,Uncommon,Rare,Epic,Legendary] by fish type.
        // General fish (real species) skew common; exclusive (biome signature) fish skew rare.
        private static readonly float[] GeneralRarityDist   = { 55f, 30f, 12f, 3f, 0f };
        private static readonly float[] ExclusiveRarityDist = { 18f, 30f, 30f, 16f, 6f };

        // Name cues force a MINIMUM tier (theme beats the dice). Checked most-powerful first.
        private static readonly string[] LegendaryCues =
            { "leviathan", "dragonheart", "celestial", "ancient wyrm", "guardian gar" };
        private static readonly string[] EpicCues =
            { "dragon", "drake", "wyrm", "void", "abyss", "phantom", "wraith", "spectral",
              "ghost", "ghoul", "banshee", "soul", "cosmic", "astral", "nebula", "meteor",
              "comet", "starmetal", "starshard", "arcane", "rune", "mystic", "spell",
              "enchant", "fairy", "pixie", "wizard", "magic", "ancient", "relic", "crown",
              "inferno", "magma", "obsidian", "prism", "diamond", "amethyst", "opal",
              "geode", "glimmer", "aether" };
        private static readonly string[] RareCues =
            { "gold", "silver", "pearl", "moon", "lunar", "star", "sapphire", "emerald",
              "jade", "ruby", "crystal", "gem", "king", "giant", "titan", "champion",
              "aurora", "ember", "frost", "storm", "thunder", "lightning", "tempest",
              "dragonscale", "rainbow", "glow", "spirit", "mirage", "oasis" };

        // ---------- WEIGHT BANDS -------------------------------------------------
        // (min,max) base kg per band. The species NOUN picks the band; fantastical
        // adjectives never do, so a "Leviathan Minnow" stays minnow-sized (but Legendary).
        private static readonly Dictionary<string, float[]> Bands = new Dictionary<string, float[]>
        {
            { "micro",   new[] { 0.02f, 0.6f   } },
            { "small",   new[] { 0.1f,  2.5f   } },
            { "medium",  new[] { 0.4f,  9.0f   } },
            { "large",   new[] { 1.5f,  30.0f  } },
            { "huge",    new[] { 8.0f,  260.0f } },
            { "monster", new[] { 40.0f, 500.0f } },
        };

        // Species noun -> band. Registered monster->micro so later (smaller) entries win ties,
        // matching the reference model. Multi-word nouns handled by SizePhrases first.
        private static readonly Dictionary<string, string> SizeNoun = new Dictionary<string, string>();
        private static readonly (string phrase, string band)[] SizePhrases =
        {
            ("sand lance", "small"), ("flying fish", "small"), ("john dory", "medium"),
            ("stone loach", "micro"), ("wolf eel", "large"), ("electric eel", "large"),
        };

        // ---------- VALUE & MUTATION (indexed by rarity tier 0..4) ---------------
        private static readonly float[] BasePerKg = { 3.5f, 6f, 9f, 13f, 20f };
        private static readonly float[] MutChance = { 0.05f, 0.08f, 0.11f, 0.16f, 0.25f };

        static FishBalanceModel()
        {
            Reg("monster", "shark", "whale", "manta", "megalodon", "kraken");
            Reg("huge", "tuna", "marlin", "sailfish", "swordfish", "sturgeon", "arapaima",
                "halibut", "wahoo", "cobia", "paddlefish", "opah", "tarpon", "oarfish",
                "wels", "sunfish", "mola");
            Reg("large", "pike", "salmon", "catfish", "cod", "gar", "eel", "carp", "barracuda",
                "amberjack", "trevally", "snook", "bowfin", "bonito", "ling", "burbot",
                "snakehead", "crocodilefish", "drum", "cusk", "hake", "pollock", "walleye",
                "muskie", "mackerel", "kawakawa", "tunny", "seriola", "jack", "grouper",
                "lampfish", "lanternfish");
            Reg("medium", "trout", "char", "bass", "snapper", "bream", "perch", "wrasse",
                "parrotfish", "koi", "goldfish", "haddock", "whiting", "grunt", "porgy",
                "mullet", "hogfish", "tang", "surgeonfish", "triggerfish", "pufferfish",
                "puffer", "scorpionfish", "lionfish", "rockfish", "lingcod", "flounder",
                "sole", "plaice", "turbot", "brill", "skate", "ray", "chub", "ide",
                "barbel", "tench", "oscar", "discus", "arowana", "piranha", "pacu",
                "tigerfish", "payara", "knifefish", "bichir", "monkfish", "dory",
                "batfish", "frogfish", "angler", "anglerfish", "cardinalfish", "cod",
                "squirrelfish", "soldierfish", "boxfish", "cowfish", "gurnard", "sculpin",
                "filefish", "porcupinefish", "croaker", "weakfish", "spot", "cobia");
            Reg("small", "rudd", "roach", "crappie", "bluegill", "pumpkinseed", "tetra",
                "gourami", "betta", "angelfish", "seahorse", "pipefish", "goby",
                "sardine", "herring", "sprat", "smelt", "menhaden", "pilchard",
                "silverside", "topminnow", "pupfish", "killifish", "shrimpfish",
                "damselfish", "clownfish", "loach", "octopus", "cuttlefish", "squid",
                "nautilus", "bobtail", "flyingfish", "sunbeam", "moonbeam");
            Reg("micro", "minnow", "dace", "bleak", "gudgeon", "bullhead", "ruffe", "anchovy",
                "needlefish", "mudfish", "mudskipper", "starfish");
        }

        private static void Reg(string band, params string[] nouns)
        {
            foreach (var n in nouns) SizeNoun[n] = band;
        }

        /// <summary>Derive the balanced numbers for a species from its name and type.</summary>
        public static FishBalance Derive(string displayName, bool isExclusive)
        {
            string id = Slug(displayName);
            float r0 = Roll(id, 0), r1 = Roll(id, 1), r2 = Roll(id, 2), r3 = Roll(id, 3);

            Rarity rarity = RarityFor(displayName, isExclusive, r0);
            int tier = (int)rarity;

            string band = WeightBandFor(displayName);
            if (tier >= (int)Rarity.Epic && band == "micro") band = "small"; // an Epic+ minnow isn't micro
            float[] range = Bands[band];

            float minW = Round(range[0] * (0.85f + r1 * 0.35f), 2);
            float maxW = Round(range[1] * (0.85f + r2 * 0.35f), 1);
            maxW = Round(maxW * (1f + 0.10f * tier), 1);           // rarer fish run a bit bigger
            if (maxW <= minW) maxW = Round(minW * 3f, 1);

            float baseValue = BasePerKg[tier];
            if (isExclusive) baseValue *= 1.15f;
            baseValue = Round(baseValue * (0.9f + r3 * 0.2f), 1);

            float mutation = MutChance[tier];
            if (isExclusive) mutation = Math.Min(0.35f, Round(mutation * 1.1f, 3));
            mutation = Round(mutation, 3);

            return new FishBalance(rarity, minW, maxW, baseValue, mutation, band);
        }

        private static Rarity RarityFor(string name, bool isExclusive, float roll)
        {
            int tier = PickTier(isExclusive ? ExclusiveRarityDist : GeneralRarityDist, roll);
            string low = name.ToLowerInvariant();
            if (ContainsAny(low, LegendaryCues)) tier = (int)Rarity.Legendary;
            else if (ContainsAny(low, EpicCues)) tier = Math.Max(tier, (int)Rarity.Epic);
            else if (ContainsAny(low, RareCues)) tier = Math.Max(tier, (int)Rarity.Rare);
            return (Rarity)tier;
        }

        private static int PickTier(float[] dist, float roll)
        {
            float total = 0f;
            foreach (float w in dist) total += w;
            float x = roll * total;
            for (int i = 0; i < dist.Length; i++)
            {
                x -= dist[i];
                if (x <= 0f) return i;
            }
            return dist.Length - 1;
        }

        private static string WeightBandFor(string name)
        {
            string low = name.ToLowerInvariant().Replace("'", "");
            foreach (var (phrase, band) in SizePhrases)
                if (low.Contains(phrase)) return band;

            string[] tokens = Tokenize(low);
            for (int i = tokens.Length - 1; i >= 0; i--)     // the noun is almost always the last word
                if (SizeNoun.TryGetValue(tokens[i], out string tail)) return tail;
            for (int i = 0; i < tokens.Length; i++)          // then any earlier recognised noun
                if (SizeNoun.TryGetValue(tokens[i], out string any)) return any;
            return "medium";                                 // sensible default
        }

        private static string[] Tokenize(string low)
        {
            var sb = new StringBuilder(low.Length);
            foreach (char c in low) sb.Append(c >= 'a' && c <= 'z' ? c : ' ');
            return sb.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool ContainsAny(string haystack, string[] needles)
        {
            foreach (string n in needles)
                if (haystack.Contains(n)) return true;
            return false;
        }

        /// <summary>Lowercase, apostrophe-free, underscore-joined id (e.g. "Dragon's Basin" -> "dragons_basin").</summary>
        public static string Slug(string name)
        {
            var sb = new StringBuilder(name.Length);
            bool pendingUnderscore = false;
            foreach (char c in name.ToLowerInvariant())
            {
                if (c == '\'') continue;
                bool alnum = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');
                if (alnum)
                {
                    if (pendingUnderscore && sb.Length > 0) sb.Append('_');
                    pendingUnderscore = false;
                    sb.Append(c);
                }
                else
                {
                    pendingUnderscore = true;
                }
            }
            return sb.ToString();
        }

        /// <summary>Stable 32-bit hash (FNV-1a) — used for deterministic ordering in the importer.</summary>
        public static uint StableHash(string s)
        {
            uint h = 2166136261u;
            for (int i = 0; i < s.Length; i++)
            {
                h ^= (byte)s[i];
                h *= 16777619u;
            }
            return h;
        }

        private static float Roll(string id, int salt)
            => (float)((double)StableHash(id + "#" + salt.ToString()) / 4294967295.0);

        private static float Round(float value, int digits)
            => (float)Math.Round(value, digits, MidpointRounding.AwayFromZero);
    }
}
