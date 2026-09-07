using System.Collections.Generic;

namespace HookHorizon.Domain.Content
{
    /// <summary>
    /// Classifies a general (non-exclusive) fish into habitat tags from its name, so the
    /// content importer can drop it into biomes whose water matches (a trout into freshwater
    /// biomes, a tuna into open-sea biomes, a parrotfish onto reefs). Pure and tunable —
    /// add a keyword to move a species. Exclusive fish skip this; they belong to one biome.
    /// </summary>
    public static class FishHabitat
    {
        // Tag vocabulary: FRESH, FRESH_COLD, FRESH_TROP, RIVER, CAVE, SWAMP, BRACKISH,
        // REEF, MARINE, MARINE_COLD, PELAGIC, DEEP. Biomes list which tags they accept.
        private static readonly (string[] keywords, string[] tags)[] Rules =
        {
            (new[] { "arowana", "arapaima", "piranha", "pacu", "tigerfish", "payara",
                     "knifefish", "oscar", "discus", "angelfish", "gourami", "betta",
                     "bichir", "snakehead" }, new[] { "FRESH_TROP", "FRESH", "RIVER" }),

            (new[] { "arctic char", "grayling", "lake trout", "brook trout", "burbot" },
                new[] { "FRESH_COLD", "FRESH" }),

            (new[] { "carp", "tench", "rudd", "roach", "bleak", "dace", "chub", " ide",
                     "barbel", "gudgeon", "minnow", "loach", "bullhead", "ruffe", "perch",
                     "crappie", "bluegill", "pumpkinseed", "walleye", "muskie", "pike",
                     "bass", "catfish", "trout", "salmon", "char", "koi", "goldfish",
                     "bowfin", "gar", "paddlefish", "sturgeon", "sunfish", "killifish",
                     "topminnow", "silverside", "pupfish", "mudfish", "drum", "steelhead",
                     "huchen" }, new[] { "FRESH" }),

            (new[] { "mangrove", "mudskipper", "brackish", "tarpon", "bonefish", "permit",
                     "snook", "mullet" }, new[] { "BRACKISH", "MARINE" }),

            (new[] { "parrotfish", "wrasse", "hogfish", "damsel", "clownfish", "surgeon",
                     "tang", "triggerfish", "filefish", "puffer", "porcupine", "boxfish",
                     "cowfish", "lionfish", "scorpion", "stonefish", "butterfly", "squirrel",
                     "soldier", "cardinal", "goatfish", "grunt", "moray", "garden eel",
                     "ribbon eel", "seahorse", "pipefish", "shrimpfish", "reef", "coral" },
                new[] { "REEF", "MARINE" }),

            (new[] { "tuna", "marlin", "sailfish", "swordfish", "spearfish", "wahoo",
                     "dolphinfish", "mahi", "barracuda", "bonito", "kawakawa", "tunny",
                     "albacore", "skipjack", "opah", "ocean sunfish", "manta", "flying fish",
                     "needlefish" }, new[] { "PELAGIC", "MARINE" }),

            (new[] { "cod", "haddock", "pollock", "whiting", "hake", "ling", "saithe",
                     "cusk", "capelin", "herring", "halibut", "plaice", "turbot", "brill",
                     "sole", "flounder", "wolf eel" }, new[] { "MARINE_COLD" }),

            (new[] { "angler", "frogfish", "gulper", "monkfish", "john dory", "batfish",
                     "lantern", "oarfish" }, new[] { "DEEP" }),

            (new[] { "mackerel", "sea bass", "sea bream", "bream", "porgy", "croaker",
                     "weakfish", "spot", "cobia", "amberjack", "trevally", "seriola",
                     "jack", "gurnard", "sculpin", "rockfish", "lingcod", "skate",
                     "stingray", "ray", "sardine", "anchovy", "sprat", "smelt", "menhaden",
                     "pilchard", "sand lance", "snapper", "grouper", "octopus", "cuttlefish",
                     "squid", "nautilus", "bobtail" }, new[] { "MARINE" }),
        };

        /// <summary>Habitat tags for a fish name. Never empty (unmatched fish are ubiquitous).</summary>
        public static HashSet<string> Tags(string displayName)
        {
            string low = " " + displayName.ToLowerInvariant() + " ";
            var tags = new HashSet<string>();
            foreach (var (keywords, ruleTags) in Rules)
            {
                foreach (string k in keywords)
                {
                    if (low.Contains(k))
                    {
                        foreach (string t in ruleTags) tags.Add(t);
                        break;
                    }
                }
            }
            if (tags.Count == 0) { tags.Add("MARINE"); tags.Add("FRESH"); }
            return tags;
        }
    }
}
