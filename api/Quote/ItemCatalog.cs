using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;

namespace api.Quote;

internal sealed record InventoryItem(
    string Key,
    string Category,
    string DisplayName,
    double CubicFeet,
    bool IsBulky = false
);

internal sealed record ItemCategory(string Key, string DisplayName);

/// <summary>
/// Cubic-feet-per-item table ported verbatim from the legacy <c>instaquote.js</c>.
/// Keys are normalized to snake_case; legacy field names (e.g. <c>LR_Sofa</c>) are documented
/// in <c>docs/legacy-migration-notes.md</c> if historical DB migration is ever needed.
/// </summary>
internal static class ItemCatalog {
    public static readonly ItemCategory[] Categories = [
        new("lr",   "Living Room"),
        new("dr",   "Dining Room"),
        new("kit",  "Kitchen"),
        new("br",   "Bedroom"),
        new("ofc",  "Office"),
        new("grg",  "Garage"),
        new("apl",  "Appliances"),
        new("blky", "Bulky / Specialty"),
        new("pto",  "Patio"),
        new("misc", "Miscellaneous")
    ];

    public static readonly InventoryItem[] Items = [
        // Living Room
        new("lr_bench",        "lr", "Bench",                 5),
        new("lr_bookcase_lg",  "lr", "Bookcase (large)",     20),
        new("lr_bookcase_sm",  "lr", "Bookcase (small)",      5),
        new("lr_music_cabinet","lr", "Music cabinet",        10),
        new("lr_chair_arm",    "lr", "Arm chair",            12),
        new("lr_chair_occ",    "lr", "Occasional chair",     15),
        new("lr_chair_over",   "lr", "Oversized chair",      25),
        new("lr_clock_grand",  "lr", "Grandfather clock",    20),
        new("lr_desk_sect",    "lr", "Secretary desk",       35),
        new("lr_desk_sm",      "lr", "Small desk",           22),
        new("lr_ent_center",   "lr", "Entertainment center", 20),
        new("lr_footstool",    "lr", "Footstool",             2),
        new("lr_lamp",         "lr", "Lamp",                  3),
        new("lr_mirror",       "lr", "Mirror",                5),
        new("lr_picture_sm",   "lr", "Picture (small)",       2),
        new("lr_picture_lg",   "lr", "Picture (large)",       5),
        new("lr_plant_stand",  "lr", "Plant stand",           5),
        new("lr_sofa_sect",    "lr", "Sectional sofa (per piece)", 30),
        new("lr_loveseat",     "lr", "Loveseat",             35),
        new("lr_sofa",         "lr", "Sofa",                 50),
        new("lr_table_sm",     "lr", "Small table",           5),
        new("lr_table_sofa",   "lr", "Sofa table",           12),
        new("lr_tube_tv",      "lr", "TV (tube)",            10),
        new("lr_flat_tv",      "lr", "TV (flat panel)",       7),
        new("lr_misc_sm",      "lr", "Miscellaneous (small)", 2),
        new("lr_misc",         "lr", "Miscellaneous",         5),

        // Dining Room
        new("dr_buffet_btm",   "dr", "Buffet (bottom)",      30),
        new("dr_buffet_top",   "dr", "Buffet (top / hutch)", 20),
        new("dr_cabinet_crnr", "dr", "Corner cabinet",       25),
        new("dr_chair",        "dr", "Dining chair",          5),
        new("dr_server",       "dr", "Server",               15),
        new("dr_table",        "dr", "Dining table",         30),
        new("dr_tea_cart",     "dr", "Tea cart",             10),

        // Kitchen
        new("kit_bakers_rack", "kit", "Baker's rack",         20),
        new("kit_chair",       "kit", "Kitchen chair",         5),
        new("kit_table",       "kit", "Breakfast table",      10),
        new("kit_high_chair",  "kit", "High chair",            5),
        new("kit_micro",       "kit", "Microwave",             5),
        new("kit_stool",       "kit", "Stool",                 3),
        new("kit_table_sm",    "kit", "Small table",           5),
        new("kit_util_cart",   "kit", "Utility cart",         10),

        // Bedroom
        new("br_armoire",      "br", "Armoire",               40),
        new("br_armoire_lg",   "br", "Armoire (large)",       60),
        new("br_bed_bunk",     "br", "Bunk bed",              70),
        new("br_bed_dbl",      "br", "Bed (double / full)",   60),
        new("br_bed_kq",       "br", "Bed (king / queen)",    70),
        new("br_bed_sngl",     "br", "Bed (single / twin)",   40),
        new("br_chest",        "br", "Chest",                 15),
        new("br_chair_bdr",    "br", "Bedroom chair",         10),
        new("br_chair",        "br", "Chair",                  5),
        new("br_chest_drs",    "br", "Chest of drawers",      25),
        new("br_hamper",       "br", "Hamper",                 5),
        new("br_dresser_dbl",  "br", "Dresser (double)",      50),
        new("br_xbike",        "br", "Exercise bike",         10),
        new("br_mirror",       "br", "Mirror",                 5),
        new("br_nightstand",   "br", "Nightstand",             5),
        new("br_treadmill",    "br", "Treadmill",             10),
        new("br_vanity",       "br", "Vanity",                20),
        new("br_misc_sm",      "br", "Miscellaneous (small)",  2),
        new("br_misc",         "br", "Miscellaneous",          5),

        // Office
        new("ofc_bookcase_lg", "ofc", "Bookcase (large)",     20),
        new("ofc_bookcase_sm", "ofc", "Bookcase (small)",      5),
        new("ofc_desk_sm",     "ofc", "Desk (small)",         22),
        new("ofc_desk_lg",     "ofc", "Desk (large / exec)",  35),
        new("ofc_file_cab_lg", "ofc", "File cabinet (4-drawer)", 20),
        new("ofc_file_cab_sm", "ofc", "File cabinet (2-drawer)", 10),
        new("ofc_machine",     "ofc", "Office machine (copier, etc.)", 5),
        new("ofc_sofa",        "ofc", "Office sofa",          50),
        new("ofc_table_conf",  "ofc", "Conference table",     40),
        new("ofc_table_fold",  "ofc", "Folding table",        10),
        new("ofc_table_sm",    "ofc", "Small table",           5),
        new("ofc_util_cart",   "ofc", "Utility cart",         10),

        // Garage
        new("grg_ladder_ext",  "grg", "Extension ladder",     10),
        new("grg_ladder",      "grg", "Step ladder",           5),
        new("grg_mower",       "grg", "Lawn mower",           15),
        new("grg_bike",        "grg", "Bicycle",              10),
        new("grg_cabinet_stg", "grg", "Storage cabinet",      15),
        new("grg_chair_fold",  "grg", "Folding chair",         1),
        new("grg_footlocker",  "grg", "Footlocker",            5),
        new("grg_shelves_mtl", "grg", "Metal shelves",         5),
        new("grg_power_tools", "grg", "Power tools (group)",  20),
        new("grg_table_util",  "grg", "Utility table",         5),
        new("grg_tool_chest",  "grg", "Tool chest",           10),
        new("grg_trash_can",   "grg", "Trash can",             7),
        new("grg_trunk",       "grg", "Trunk",                10),
        new("grg_bench_work",  "grg", "Workbench",            20),
        new("grg_misc",        "grg", "Miscellaneous",         2),

        // Appliances
        new("apl_fridge_top",  "apl", "Refrigerator (top/bottom)", 45),
        new("apl_fridge_sxs",  "apl", "Refrigerator (side-by-side)", 65),
        new("apl_freezer",     "apl", "Freezer",              45),
        new("apl_washer",      "apl", "Washer",               25),
        new("apl_dryer",       "apl", "Dryer",                25),
        new("apl_dishwasher",  "apl", "Dishwasher",           15),
        new("apl_stove",       "apl", "Stove / range",        30),

        // Bulky / Specialty (each forces a 3-person crew minimum)
        new("blky_piano_baby_grand", "blky", "Piano — baby grand", 70, IsBulky: true),
        new("blky_piano_spinet",     "blky", "Piano — spinet",     60, IsBulky: true),
        new("blky_piano_upright",    "blky", "Piano — upright",    70, IsBulky: true),
        new("blky_big_screen_tv",    "blky", "Big-screen TV (60\"+)",    40, IsBulky: true),
        new("blky_pool_table",       "blky", "Pool table",                40, IsBulky: true),
        new("blky_pinball",          "blky", "Pinball / arcade",          40, IsBulky: true),

        // Patio
        new("pto_bbq",         "pto", "BBQ grill",            10),
        new("pto_chair_lawn",  "pto", "Lawn chair",            5),
        new("pto_chair_lng",   "pto", "Lounge chair",         10),
        new("pto_hose_tools",  "pto", "Hose / yard tools",    10),
        new("pto_glider",      "pto", "Glider",               20),
        new("pto_picnic_tbl",  "pto", "Picnic table",         20),
        new("pto_picnic_bch",  "pto", "Picnic bench",          5),
        new("pto_table_patio", "pto", "Patio table",          10),
        new("pto_umbrella",    "pto", "Umbrella",              5),
        new("pto_misc",        "pto", "Miscellaneous",         5),

        // Miscellaneous
        new("misc_fan",        "misc", "Fan / heater",         5),
        new("misc_iron_board", "misc", "Ironing board",        2),
        new("misc_plant_lg",   "misc", "Plant (large)",        5),
        new("misc_plant_sm",   "misc", "Plant (small)",        2),
        new("misc_rug_sm",     "misc", "Rug (small)",          3),
        new("misc_rug_lg",     "misc", "Rug (large)",         10),
        new("misc_suitcase",   "misc", "Suitcase",             5),
        new("misc_misc",       "misc", "Miscellaneous",        5)
    ];

    public static readonly FrozenDictionary<string, InventoryItem> ByKey =
        Items.ToFrozenDictionary(i => i.Key, StringComparer.OrdinalIgnoreCase);

    public static readonly FrozenDictionary<string, ItemCategory> CategoryByKey =
        Categories.ToFrozenDictionary(c => c.Key, StringComparer.OrdinalIgnoreCase);

    public static IEnumerable<InventoryItem> ForCategory(string categoryKey) =>
        Items.Where(i => string.Equals(i.Category, categoryKey, StringComparison.OrdinalIgnoreCase));

    // Boxes have their own pricing model (different cubic feet + man-hours rates) so they are
    // not part of the catalog. These are the literal cubic-feet-per-box values from instaquote.js.
    public const double SmallBoxCubicFeet     = 1.5;
    public const double MediumBoxCubicFeet    = 3.0;
    public const double LargeBoxCubicFeet     = 4.5;
    public const double WardrobeBoxCubicFeet  = 10.0;
}
