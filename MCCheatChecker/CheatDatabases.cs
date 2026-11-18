using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MCCheatChecker
{
	public static class CheatDatabases
	{
		public sealed class BanRule
		{
			public string Id;

			public string Title;

			public string[] Keys;

			public CheckSeverity Severity;

			public BanRule(string id, string title, CheckSeverity sev, params string[] keys)
			{
				Id = id;
				Title = title;
				Severity = sev;
				Keys = keys;
			}
		}

		// Разрешенные визуальные модификации (косметика, графика, HUD, анимации, миникарты, шейдеры)
		public static readonly string[] AllowedVisuals = new string[]
		{
			// Оптимизация, рендеринг, шейдеры и освещение
			"optifine", "sodium", "lithium", "iris", "indium", "ferritecore", "entityculling", "immediatelyfast",
			"embeddium", "modernfix", "krypton", "starlight", "lazydfu", "fastsuite", "bobby", "distanthorizons",
			"cullleaves", "dynamiclights", "lambdynamiclights", "lucent", "continuity", "citresewn", "enhancedblockentities",
			"ebe", "animatica", "colormatic", "fabricskyboxes", "customfog", "simplefogcontrol", "clearskies", "nofog",
			"sodiumextra", "reesessodiumoptions", "canvas", "vulkanmod", "nvidium", "smoothboot", "borderlessmining",
			"controlling", "searchables", "yacl", "yetanotherconfiglib", "architectury", "fabricapi", "forgeapi",

			// Популярные шейдерпаки
			"bsl", "complementary", "complementaryreimagined", "astralex", "sildurs", "sildursvibrant", "chocapic",
			"seus", "continuum", "vanillaplus", "makeupsultra", "solas", "superdupervanilla", "tea", "rethinkingvoxels", "photon",

			// Разрешенные ресурспаки и текстуры
			"faithless", "faithful", "barebones", "sphax", "bdcraft", "purebdcraft", "staytrue", "mizunos",
			"johnsmith", "dandelion", "defaultdarkmode", "darkmode", "animateditems", "lowshield", "shortswords",
			"clearwater", "clear_water", "betterleaves", "roundtrees",

			// Разрешенные миникарты (Пункт 5.05 регламента)
			"journeymap", "voxelmap", "xaero", "xaerominimap", "xaeroworldmap", "antiqueatlas", "mapwriter",

			// Разрешенные CPVP оптимизаторы (Пункт 5.02 регламента)
			"marlow", "marlowcrystaloptimizer", "marlowcrystal", "marlow-crystal",
			"haramcrystaloptimizer", "haramcrystal", "haram-crystal",
			"endcrystaloptimizer", "end-crystal-optimizer",
			"walksy", "walksyanchor", "walksy-anchor", "anchoroptimizer",
			"herosanchor", "heros-anchor", "heroanchor",
			"cutebow", "cutebowanchor", "cutebow-anchor",

			// Косметические моды, плащи и анимации
			"waveycapes", "wavey-capes", "3dskinlayers", "3d-skin-layers", "notenoughanimations", "not-enough-animations",
			"eatinganimation", "eating-animation", "firstpersonmodel", "first-person-model", "betteranimations",
			"betteranimationscollection", "physicsmod", "physics-mod", "visuality", "showmeyourskin", "skinswapper",
			"capes", "customhitcolor", "rainbowhit", "hurttint", "nohurtcam", "cameraoverhaul", "motionblur",
			"viewmodel", "viewmodel-changer", "customcrosshair", "crosshairmod", "dynamiccrosshair", "crosshaircustomizer",
			"cpsdisplay", "simplekeystrokes", "keystrokes", "chatheads", "betterchat", "compactchat", "detailarmorbar",
			"appleskin", "betterf3", "customhud", "statuseffectbars", "shulkerboxtooltip", "cleardespawn", "essential",
			"essentialmod", "replaymod", "flashback", "cloth-config", "modmenu", "raised", "bedrockify", "hudtweaks",
			"armorchroma", "vanillaenhanced", "inventoryhud", "inventoryhudplus",

			// Зум-моды (Zoom)
			"zoomify", "logicalzoom", "okzoom", "wizoom", "zoomerboomer", "simplezoom",

			// Популярные визуальные паки серверов СНГ (ReallyWorld, FunTime, HolyWorld, Spooky, Star, Topka)
			"pulsevisuals", "topkavisuals", "plasmaclient", "selectvisuals", "paradoxvisuals", "warpvisuals",
			"customblockoverlay", "darknessvisuals", "primevisuals", "zetavisuals", "agatevisuals", "anomatweaks",
			"siriusvisuals", "soupapi", "spectravisuals", "soupbetter", "flashvisual", "adaptivevisuals",
			"enoughvisuals", "thecartel", "phantomvisuals", "rainvisuals", "strangevisuals", "mytheria",
			"impulsevisuals", "notvisuals", "wantedvisuals", "starvisuals", "aivisuals", "auramod",
			"burstvisuals", "icevisuals", "kastrixvisuals", "eclipsevisuals", "lolyvisuals", "rivalvisuals",
			"flakevisuals", "funvisuals", "fentanylvisuals", "shinyvisuals", "onyxvisuals", "zerovisuals",
			"vegavisuals", "prizrakvisuals", "rebornlight", "atomvisuals", "destravisuals", "reallyvisuals",
			"spacevisuals", "aquavisuals", "frostvisuals", "solarvisuals", "lunarvisuals", "neonvisuals",
			"cosmovisuals", "magicvisuals", "luxvisuals", "hypervisuals", "stormvisuals", "shadowvisuals",
			"pixelvisuals", "quantumvisuals", "apexvisuals", "focusvisuals", "flowvisuals", "breezevisuals",
			"zenithvisuals", "chillvisuals", "hazevisuals", "glowvisuals", "auravisual", "blurmod",
			"customparticles", "blockoutline", "itemphysic", "soundphysics", "presencefootsteps",
			"ambientenvironment", "chunksfadein", "berdinskiybear", "antighost", "topkatags",
			"itemlocks", "durabilityviewer", "minihud", "exitlag", "customfov", "connectedglass",
			"tweakeroo", "malilib", "litematica", "waila", "hwyla", "jade", "theoneprobe", "rei", "roughlyenoughitems",
			"jei", "justenoughitems", "emi", "betterpingdisplay", "pingdisplay", "itemmodelcondenser", "modelgapfix",
			"animfix", "itemphysiclite", "particlecore", "particlecustomizer", "betterparticles", "extrapolations",
			"lowshieldplus", "armorstatus", "statuseffects", "simplearmorstatus", "simpledurability", "bountifulhud",
			"betteractionbar", "actionbartweaks", "titletweaks", "fallingleaves", "betterclouds", "chunky",
			"distantgenerations", "visuality-reforged", "iris-flw-compat", "memoryleakfix", "c2me", "noisium",
			"wthit", "fastopen", "fastload", "smoothscroll", "badlionclient", "feathermc", "salwyrrclient", "solarcache",
			"spookyvisuals", "holyvisuals", "reallyworldvisuals", "funtimevisuals", "auroravisuals", "nordvisuals",
			"astralvisuals", "hydravisuals", "zenvisuals", "crystalvisuals",
			"bloodvisuals", "voidvisuals", "abyss_visuals", "frostyvisuals", "cybervisuals", "darkvisuals",
			"craftingtweaks", "vanillatweaks", "redstonehud", "blockmeter", "xaeroplus", "totemcounter",
			"bedrock_helper", "crystal_helper", "pearl_helper", "elytra_helper", "customcrosshairs", "crosshair_mod",
			"bow_fov_fix", "dynamic_fps", "entity_model_features", "entity_texture_features", "cubes_without_borders",
			"plasmovoice", "simplevoicechat", "voicechat", "statusbars", "statuseffecttimer", "smoothswapping", "customnameplates"
		};

		public static readonly HashSet<string> AmbiguousNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"rise", "flex", "wave", "ghost", "rogue", "drip", "remix", "future", "atom", "flux",
			"prism", "nova", "raven", "sora", "pulse", "reflex", "titan", "omega", "cipher", "nexus",
			"abyss", "axiom", "epoch", "nebula", "spectrum", "specter", "phantom", "ozone",
			"haste", "aqua", "ruin", "eclipse", "orion", "stronk", "tenacity", "hypnotic", "neura", "entropy",
			"vertex", "hyphen", "sable", "kage", "dusk", "haru", "blat", "vortex", "amethyst", "garnet",
			"haven", "wex", "akr", "jelly", "fatal", "hider", "decision", "caesar", "bleach", "venus",
			"cortex", "metro", "ares", "zeus", "squad", "wave", "invisible", "winner", "external", "rage",
			"simply", "excellent", "infinity", "energy", "haven", "ponos", "stardust", "darklight", "darkproject", "catalyst",
			"paragon", "haruka", "verist", "evaware", "britva", "aurus", "nodus", "matix", "pivo",
			"editme", "r3d", "destroy", "wwe", "wurst", "hwid", "bypass", "spoofed", "skidded", "cracked",
			"nulled", "leaked", "obfuscated", "criticals", "velocity", "strafe", "scaffold", "triggerbot", "bhop", "nuker",
			"antibot", "antiafk", "autofish", "autosprint", "highjump", "longjump", "boatfly", "hax", "hacks", "search",
			"chams", "norender", "dimka",
			"kolyan", "petyan", "vanyan", "sashka", "misha", "chernila", "krasniy", "bychara", "boroda", "celka",
			"nurik", "bolshoy", "banjo", "jessica", "konas", "predator", "seraph", "acryl", "flarial", "ambrosial",
			"killerware", "novoline", "huzuni", "altdentifier", "minced", "keaz", "kion", "mercu", "niobium", "kamiblue",
			"pyro", "wexside", "avalone", "blackberry", "celestial", "deadcode", "norules", "bebraware", "rynware", "neverware",
			"aristois", "baritone", "skillclient", "jigsaw", "salhack", "wolfram", "weepcraft", "kalkon", "advancedx", "neverclide",
			"prerik", "cabaletta", "gishcode", "rusherhack", "eternity", "richpremium", "mc100", "archware", "boberware", "emortalityclient",
			"dreampool", "clowdy", "rasty", "kyprak", "wintware", "panickattack", "hackbrand", "undetectable", "interium", "energyclient",
			"cclient", "quickclient", "chclient", "tofficlient", "troxill", "recode", "exosware", "viamcp", "waterclient", "extendopvp",
			"hitware", "harambaritone", "flauncher", "clownclient", "euphoriaclient", "vertclient", "wortexclient", "404client", "aoba", "mioclient",
			"vegaline", "wissend", "jexclient", "fdp", "trollhack", "catlavan", "lunaclient", "exloader", "dummie", "dummi",
			"dummyclient", "novaclient", "iceclient", "nova2", "nova3", "blatware", "chipware", "vorona", "rusohack",
			"atomware", "quantumware", "neonware", "pixelware", "slavehack", "slavware", "babahack", "killauraplus", "multiaura", "tpaura",
			"silentaura", "silentswing", "silentmine", "fastplace", "fastbreak", "aimassist", "antikb", "liquidloader", "ambientloader", "hackloader",
			"bootject", "xenos", "coremod", "coremods", "mchack", "mccheat", "hackedclient", "cheatclient", "hackforge", "hackedforge",
			"mixinhack", "mixin-hack", "bytebuddy-hack", "nodus2", "lolclient", "mc-hack", "horion", "zephyr", "horionupdated", "oseris",
			"solaris", "onix", "nohopefully", "dragonfly", "bedrockhax", "bedrockcheat", "advancedxray", "xrayultimate", "x-rayultimate",
			"brightnight", "cavefinder", "chunkborders", "oreesp", "wallhack", "wireframe", "blockesp", "itemesp",
			"mobesp", "chestesp", "storageesp", "playeresp", "nametags", "trajectories", "aurora", "astral", "cosmic",
			"stellar", "polar", "zenith", "horizon", "summit", "pinnacle", "dominion", "conquer", "victory", "empire",
			"reign", "sovereign", "monarch", "dynasty", "legacy", "prestige", "xenon", "phobos", "cinder", "sunlit",
			"deceptive", "salem", "march", "olympus", "kratos", "hydra", "vapor", "vendetta", "scythe", "valkyrie",
			"revelation", "terminator", "permafrost", "nightcore", "prism2", "infinity2", "timer", "blink", "reach", "autoTotem",
			"nofall", "aura", "killspeed", "itemtag", "nicker", "locker"
		};

		public static readonly string[] WhitelistPathPatterns = new string[]
		{
			"c:\\windows\\", "c:\\program files\\windows\\", "\\windowsapps\\", "microsoft\\windows\\", "winstore\\", "dotnet\\", "microsoft.net\\", "visual studio", "msbuild", "nuget\\packages",
			"java-runtime", "\\runtime\\jre", "eclipse\\adoptium", "\\jdk-", "\\jre-\\d", "microsoft\\jdk", "\\openjdk", "zulu", "corretto", "temurin",
			"tlauncher", "legacy launcher", "minecraft-launcher", "minecraftlauncher", "\\.lunarclient", "\\.badlion", "feather client", "\\curseforge\\", "\\.curseforge", "atlauncher",
			"\\arialdown\\", "skillpack", "\\.mceclipse", "polymc", "multimc", "prism launcher", "optifine", "sodium", "lithium", "iris",
			"fabric", "forge", "neoforge", "quilt", "vialoadingbase", "viaversion", "journeymap", "xaero", "voxelmap", "spark",
			"axiom", "appleskin", "cloth-config", "modmenu", "replaymod", "fullbright", "gamma", "connectedglass", "continuity",
			"indium", "ferritecore", "lambdynamiclights", "entityculling", "krypton", "lazydfu", "starlight", "pulsevisuals", "topkavisuals", "plasmaclient",
			"selectvisuals", "paradoxvisuals", "warpvisuals", "customblockoverlay", "darknessvisuals", "primevisuals", "viewmodel-changer", "do a barrel roll", "zetavisuals",
			"agatevisuals", "anomatweaks", "siriusvisuals", "soupapi", "spectravisuals", "soupbetter", "flashvisual", "adaptivevisuals", "enoughvisuals", "thecartel",
			"phantomvisuals", "rainvisuals", "strangevisuals", "mytheria", "impulsevisuals", "notvisuals", "wantedvisuals", "starvisuals", "aivisuals", "auramod",
			"burstvisuals", "icevisuals", "kastrixvisuals", "eclipsevisuals", "lolyvisuals", "rivalvisuals", "flakevisuals", "funvisuals", "fentanylvisuals", "shinyvisuals",
			"onyxvisuals", "zerovisuals", "vegavisuals", "prizrakvisuals", "rebornlight", "atomvisuals", "destravisuals", "reallyvisuals", "spacevisuals", "rct",
			"prime parts", "chunks fade in", "berdinskiybear", "antighost", "topkatags", "itemlocks", "durability viewer", "durabilityviewer", "minihud",
			"topkahealth", "exitlag", "custom fov", "show yourself", "jade", "visualratio", "axolotlclient",
			"nohurtcam", "lavaclearwater", "rockstar games", "riot games", "epic games", "battle.net", "steam\\steamapps", "gog galaxy", "warthunder", "wargaming",
			"hyperswitch", "razer", "logitech", "steelseries", "corsair", "\\hyperx\\", "\\thrustmaster\\", "sparkprofiler", "\\spark-/", "haste bin",
			"hastebin", "\\.atom\\", "atomcode", "electron", "node_modules", "crystalreports", "crystal manager", "aqua", "orion hash", "nebula graph",
			"phantomjs", "obs-studio", "streamlabs", "7-zip", "winrar", "\\.venv", "appdata\\local\\temp\\vs", "microsoft\\edge", "google\\chrome", "mozilla",
			"braveSoftware",

			// Разрешенные CPVP оптимизаторы (5.02)
			"marlow", "marlowcrystaloptimizer", "marlowcrystal", "haramcrystaloptimizer", "haramcrystal",
			"endcrystaloptimizer", "anchoroptimizer", "walksy", "walksyanchor", "herosanchor", "heroanchor", "cutebow", "cutebowanchor",

			// Разрешенные визуальные моды
			"waveycapes", "3dskinlayers", "notenoughanimations", "eatinganimation", "firstpersonmodel",
			"betteranimations", "physicsmod", "visuality", "showmeyourskin", "skinswapper", "capes",
			"customhitcolor", "rainbowhit", "hurttint", "cameraoverhaul", "motionblur", "viewmodel",
			"customcrosshair", "crosshairmod", "cpsdisplay", "simplekeystrokes", "keystrokes", "chatheads",
			"betterchat", "compactchat", "detailarmorbar", "betterf3", "customhud", "statuseffectbars",
			"shulkerboxtooltip", "cleardespawn", "zoomify", "logicalzoom", "okzoom", "wizoom", "zoomerboomer",
			"simplezoom", "essential", "essentialmod", "flashback", "sodiumextra", "reesessodiumoptions",
			"canvas", "vulkanmod", "nvidium", "aquavisuals", "frostvisuals", "solarvisuals", "lunarvisuals",
			"neonvisuals", "cosmovisuals", "magicvisuals", "luxvisuals", "hypervisuals", "stormvisuals",
			"shadowvisuals", "pixelvisuals", "quantumvisuals", "apexvisuals", "focusvisuals", "flowvisuals",
			"breezevisuals", "zenithvisuals", "chillvisuals", "hazevisuals", "glowvisuals", "auravisual",
			"blurmod", "customparticles", "blockoutline", "itemphysic", "soundphysics", "presencefootsteps",
			"ambientenvironment", "chunksfadein", "smoothboot", "borderlessmining", "controlling", "searchables",
			"raised", "bedrockify", "hudtweaks", "armorchroma", "vanillaenhanced", "inventoryhud"
		};

		public static readonly string[] WhitelistProcessNames = new string[]
		{
			"lunarclient", "feather", "badlionclient", "tlauncher", "arialdown", "mceclipse", "polymc", "multimc", "prismlauncher", "atlauncher",
			"curseforge", "opticfine", "vanilla", "minecraft.Windows", "spark", "haste", "obs64", "streamlabs", "discord", "telegram",
			"chrome", "msedge", "firefox", "opera", "brave", "vivaldi"
		};

		public static readonly string[] ExtraCheatClients = new string[]
		{
			// СНГ чит-клиенты и модификации
			"expensive", "expensiverecode", "expensive3", "expensive4", "expensiveclient",
			"nursultan", "nursultanrecode", "nurik", ".nur", "nursultanclient",
			"celestial", "celestialrecode", "calestial", "celka", ".celka", "celestialclient",
			"wexside", "wex", ".wex", "wexsideclient", "wexsiderecode",
			"akrien", "akrienrecode", "akrienantileek", "akr", ".akr", "akrienclient",
			"minced", "mincedrecode", "minced2", "mincedclient",
			"deadcode", "deadcode5", "deadcodeclient", "d3adcode", "d3adc0de",
			"rich", "richpremium", "richrecode", "richclient", "richpremiun",
			"catlavan", "catlavanclient", "catlean", "catleanclient",
			"delta", "deltaclient", "deltac", "deltacclient",
			"doomsday", "doomsdayclient", "doomsdayfucker",
			"flint", "flintclient", "kura", "kuraclient",
			"exloader", "ex-loader", "systemdlc", "system-dlc",
			"thunderhack", "thunderhackrecoded", "thunderhackclient",
			"wintware", "rockstar", "rockstarclient", "archware", "boberware",
			"cellent", "flavour", "fluger", "flugerclient", "neverhook",
			"pandora", "gamesense", "zenware", "vegaline", "clowdy",
			"rasty", "rastyclient", "rastylegit", "dreampool", "dreampoolhack",
			"kyprak", "bebraware", "rynware", "neverware", "auroraclient",
			"pivoclient", "troxil", "interium", "extremeclient", "babahack",
			"slavehack", "rusohack", "blatware", "chipware", "zamorozka",
			"zamorochka", "v3teran", "veteranclient", "dauntiblyat", "norules",

			// Зарубежные и анархические чит-клиенты
			"vape", "vapev4", "vapelite", "vapeclient", "bape",
			"drip", "dripclient", "driplite", "dripware",
			"meteor", "meteorclient", "meteorpenit",
			"wurst", "wurstclient", "wurds",
			"liquidbounce", "liquidbounceplus", "liquidbouncenext", "fdp", "fdpclient",
			"boze", "bozeclient", "slinky", "slinkycrack", "slinkyclient",
			"future", "futureclient", "rusherhack", "rusher",
			"pyro", "pyroclient", "kami", "kamiblue", "lambda", "lambdaclient",
			"aristois", "aristoisclient", "baritone", "fabritone", "cabaletta",
			"impact", "impactclient", "sigma", "sigmaclient", "sigmanew",
			"astolfo", "novoline", "rise", "tenacity", "augustus",
			"raven", "ravenb4", "ravenbplus", "ravenbs", "ravencheat",
			"koid", "itami", "whiteout", "slap", "exhibition",
			"coffeeclient", "bleachhack", "farcaclient", "aresclient", "ares",
			"jigsaw", "salhack", "wolfram", "weepcraft", "nodus", "matix",

			// Автокликеры и макросы (Пункт 5.03)
			"speedautoclicker", "opautoclicker", "opauto", "fastclicker",
			"autoclickerpro", "m7clicker", "gsautoclicker", "easyautoclicker",
			"keyran", "autohotkey", "jsmacros", "macrorecorder", "tinytask",
			"jitbit", "botmek", "clickerhero", "mgyclicker", "betterclicker",

			// Инжекторы и хакерские библиотеки
			"rename_me_please.dll", "editme.dll", "mc100.dll", "dauntiblyat.dll",
			"kpvp", "kpvpclient", "extremeclient", "fabriccheat", "quiltcheat",
			"forgecheat", "mixininject", "asminject", "agentinject", "javagenthack",
			"injectorchest", "cheatloader", "clientloader", "patchloader",
			"seedcracker", "seedcrackerx", "seedmapper",
			"richter", "richterclient", "kwish", "kwishclient", "avolon", "deko", "dekoclient",
			"celestialrecode", "expensiverecode", "nursultanrecode", "wexsiderecode", "mincedrecode", "boberwarerecode", "archwarerecode",
			"expensive3", "nursultan_beta", "celestial_v2", "delta_v2", "doomsday_v2", "rich_v2", "wexside_v2"
		};

		public static readonly string[] KnownCleanerSignatures = new string[]
		{
			"ccleaner", "bleachbit", "privazer", "eraser", "sdelete", "wevtutil", "fsutil", "cleanmgr",
			"journaltrace", "prefetchcleaner", "logcleaner", "winsweeper", "krestik", "bypass-cleaner",
			"bcleaner", "systemcleaner", "tracecleaner", "usncleaner", "regwipe", "eventcleaner",
			"batcleaner", "cleanpre", "cleanall", "wiper", "cleaner", "destroyer",
			"sl-cleaner", "slcleaner", "koid", "clean-detecting", "cleandetecting", "usndelete", "tracekiller",
			"vanish", "destruct", "selfdestruct", "historycleaner", "regwiper", "ghostcleaner", "bypasscleaner",
			"clearall", "clear-all", "usn_clean", "delete_traces", "clean_pc", "procexp", "processhacker", "systeminformer"
		};

		public static readonly string[] ExtraInjectorsAndLoaders = new string[]
		{
			"extremeinjector", "x64injector", "manualmap", "modulestorer", "processinjection", "CreateRemoteThread", "dllloader", "cheatloader", "d3dexloader", "d3dx9x4free",
			"d3d8to9", "detourshook", "minhook", "mhook", "easyhook", "frida", "frida-server", "ghidra", "ilspy", "dnspyex",
			"harmonylib", "bytekit", "reloaded", "relibreaker", "assetStudio", "uTinyRipper", "unityplayer", "dnfile", "de4dot", "confuserex",
			"dotconfuser", "reactordotnet", "harmony", "byte-buddy-agent", "asm-debugging"
		};

		public static readonly string[] ExtraCheatDomains = new string[]
		{
			"vape.gg", "vape.ac", "vape.pw", "vape.so", "vape.how", "expensive.de", "expensive.lol", "expensive.moe", "expensive-client.ru", "expensiveclient.store",
			"nursultan.pw", "nursultan.zip", "nursultan.store", "nurik.store", "celestial.ac", "celestial.pub", "calestial.store", "wexside.pw", "wexside.xyz",
			"akrien.co", "akrien.gg", "minced.pw", "deadcode.pw", "deadcode.gay", "richpremium.ru", "richclient.ru", "catlavan.ru", "catlavan.com",
			"delta-client.ru", "deltaclient.fun", "doomsdayclient.com", "doomsday.lol", "thunderhack.net", "slinky.gg", "boze.dev", "drip.gg",
			"drip.lol", "drip.moe", "wurstclient.net", "wurst.im", "liquidbounce.net", "meteorclient.com", "meteorclient.me", "futureclient.net",
			"future-client.ru", "rusherhack.com", "rusher.gg", "aristois.net", "tenacity.gg", "novoline.lol", "riseclient.com", "exloader.net",
			"systemdlc.ru", "rockstarclient.ru", "wintware.ru", "boberware.ru", "bebraware.fun", "zamorozka.fun", "unbanmc.ru", "hwidspoofer.ru",
			"spoofer.services", "spoofer.cc", "cheatsupply.ru", "cheatstore.ru", "mchack.store", "cheat-mc.ru", "kamiblue.me", "pyroclient.me",
			"impactclient.me", "salhack.io", "neura.click", "neura.pw", "entropy.gay", "entropy.ac", "flux.ac", "flux.gg", "prism.ac",
			"raven.ac", "sora.ac", "hypnotic.ac", "huzuni.ac", "stronk.ac", "killerware.gg", "ambrosial.ac", "reflex.ac", "pulse.ac",
			"abyss.ac", "titan.ac", "omega.ac", "cipher.ac", "predator.ac", "kage.ac", "vertex.ac", "hyphen.ac",
			"expensive.su", "expensiveclient.ru", "expensive.store", "nursultan.fun", "nursultan.recode", "nurik.fun", "celestial.fun", "celestial.store",
			"wexside.net", "wexside.fun", "flintclient.ru", "kura.fun", "catlavan.fun", "catlavan.club", "delta.fun", "deltaclient.top",
			"doomsdayclient.ru", "doomsday.fun", "thunderhack.fun", "slinky.fun", "vape.rip",
			"expensive-recode.ru", "expensiveclient.top", "nursultanclient.ru", "nurikclient.ru", "wexsideclient.ru",
			"celestialclient.ru", "doomsdayclient.me", "catlavan.top", "richterclient.ru", "deltaclient.ru", "exloader.ru",
			"cheatmine.ru", "funtimecheats.ru", "reallyworldcheats.ru", "holyworldcheats.ru"
		};

		public static readonly string[] ExtraVisualKeywords = new string[]
		{
			"xrayvision", "xray-pack", "x_ray", "x-ray-pack", "fullbright-pack", "brightercaves", "seethrough", "see-through",
			"transparentblocks", "glassblocks", "cavemapper", "cave-mapper", "mineralmapper", "mineral-mapper", "strongholdtracker", "endereyetracker", "spawnerscan", "structurefinder",
			"structure-search", "treasuremap", "playeroutline", "player-glow", "mobhighlight", "itemhighlight", "loothighlight", "lootfinder", "loot-scan",
			"shulkeresp", "shulker-highlight", "enderchestesp", "chunkoverlay", "hitboxoverlay", "hitbox-visualizer", "hitboxdisplay", "showhitboxes", "show-hitz",
			"raycastvisualizer", "trajectoravisual", "trajectory-view", "projectile-tracker", "fishing-tracker", "rod-tracker", "radarhack", "radar-hack", "wallhack-pack",
			"wall-hack", "noclip", "camera-clip", "xrayresourcepack", "xray-shader", "shader-xray", "iris-xray"
		};

		private static readonly string[] CheatContextMarkers = new string[]
		{
			".minecraft", "\\mods\\", "\\versions\\", "\\versions", "\\resourcepacks", "\\shaderpacks", "\\libraries\\", "\\coremods", "\\mixins", "\\launchers",
			"\\clients\\", "\\cheats\\", "\\hacks\\", "\\loads\\", "\\injectors\\", "\\spoofer", "\\hwid", "\\config\\", "\\premium\\", "\\clients",
			"\\downloads\\", "\\desktop\\", "\\appdata\\roaming\\", "\\temp\\", "javaagent", "bootclasspath", "mixin", "asm", "transformer", "client.jar",
			"cheat", "hack", "inject", "spu", "spoofer", "bypass", "autoclick", "macro", "esp", "xray",
			"x-ray", "aura", "aimbot", "trigger", "scaffold", "fly", "killaura"
		};

		public static readonly BanRule[] BannedByRegulation = new BanRule[]
		{
			// 5.01. Чит-клиенты (30d / perm)
			new BanRule("5.01", "Catlavan Client", CheckSeverity.Critical, "catlavan", "catlavanclient"),
			new BanRule("5.01", "Celestial Client", CheckSeverity.Critical, "celestial", "celka", ".celka", "calestial", "celestialrecode"),
			new BanRule("5.01", "Delta Client", CheckSeverity.Critical, "deltaclient", "delta", "deltac", "deltacclient"),
			new BanRule("5.01", "DoomsDay Client", CheckSeverity.Critical, "doomsday", "doomsdayfucker", "doomsdayclient"),
			new BanRule("5.01", "Drip Client", CheckSeverity.Critical, "drip", "dripware", "dripclient", "driplite"),
			new BanRule("5.01", "ExLoader", CheckSeverity.Critical, "exloader", "ex-loader"),
			new BanRule("5.01", "Expensive Client", CheckSeverity.Critical, "expensive", "expensiverecode", "expensive3", "expensive4"),
			new BanRule("5.01", "Catlean Client", CheckSeverity.Critical, "catlean", "catleanclient"),
			new BanRule("5.01", "Impact Client", CheckSeverity.Critical, "impact", "impactclient"),
			new BanRule("5.01", "LiquidBounce", CheckSeverity.Critical, "liquidbounce", "liquidbounceplus", "liquidbouncenext", "fdpclient"),
			new BanRule("5.01", "Meteor Client", CheckSeverity.Critical, "meteorclient", "meteor", "meteorpenit"),
			new BanRule("5.01", "Nursultan Client", CheckSeverity.Critical, "nursultan", "nurik", ".nur", "nursultanrecode"),
			new BanRule("5.01", "Sigma Client", CheckSeverity.Critical, "sigma", "sigmaclient", "sigmanew"),
			new BanRule("5.01", "SystemDLC", CheckSeverity.Critical, "systemdlc", "system-dlc"),
			new BanRule("5.01", "ThunderHack", CheckSeverity.Critical, "thunderhack", "thunderhackrecoded"),
			new BanRule("5.01", "Vape Client", CheckSeverity.Critical, "vape", "vapev4", "vapeclient", "vapelite", "bape"),
			new BanRule("5.01", "Wexside Client", CheckSeverity.Critical, "wexside", "wex", ".wex", "wexsideclient"),
			new BanRule("5.01", "Wurst Client", CheckSeverity.Critical, "wurst", "wurstclient"),

			// 5.02. Преимущество в бою (30d)
			new BanRule("5.02", "AimBot", CheckSeverity.Critical, "aimbot", "aimassist", "aimlock", "aimbot360", "silentaim"),
			new BanRule("5.02", "AttackThroughGrass", CheckSeverity.Critical, "attackthroughgrass", "throughgrass", "grassthrough", "notargetblock", "cleancut"),
			new BanRule("5.02", "AutoCart", CheckSeverity.Critical, "autocart", "carttntauto", "autocarttnt"),
			new BanRule("5.02", "AutoCrystal (Запрещённый CPVP)", CheckSeverity.Critical, "autocrystal", "crystalaura", "crystalauto"),
			new BanRule("5.02", "AutoShield", CheckSeverity.Critical, "autoshield", "shieldassist", "autoblockshield"),
			new BanRule("5.02", "Bow Boost", CheckSeverity.Critical, "bowboost", "bow_boost", "boostbow"),
			new BanRule("5.02", "Criticals", CheckSeverity.Critical, "criticals", "minicrit", "jumpcrit"),
			new BanRule("5.02", "Хитбоксы (мод на увеличение)", CheckSeverity.Critical, "hitbox", "hitboxes", "minihitboxes", "hitboxbaritone", "customhitbox"),
			new BanRule("5.02", "KillAura", CheckSeverity.Critical, "killaura", "silentaura", "attackaura", "multiaura"),
			new BanRule("5.02", "NoDelay", CheckSeverity.Critical, "nodelay", "nodelayoptimizer", "inputlagremover", "delayremover"),
			new BanRule("5.02", "Pearl Trajectories", CheckSeverity.Critical, "pearltrajectories", "pearlmarker", "trajectoryguard"),
			new BanRule("5.02", "QuickTurn", CheckSeverity.Critical, "quickturn", "fastturn", "instantturn"),
			new BanRule("5.02", "Reach", CheckSeverity.Critical, "reachmod", "reachdisplay", "reachhack", "extendedreach"),
			new BanRule("5.02", "ShieldBreaker", CheckSeverity.Critical, "shieldbreaker", "breakshield", "axeshieldbreaker"),
			new BanRule("5.02", "TriggerBot", CheckSeverity.Critical, "triggerbot", "autotrigger", "triggerclicker"),
			new BanRule("5.02", "Velocity", CheckSeverity.Critical, "velocity", "antikb", "antiknockback", "noknockback"),

			// 5.03. Автоматизация действий (15-30d)
			new BanRule("5.03", "AutoArmor", CheckSeverity.Critical, "autoarmor", "armorhotswap", "invtotem"),
			new BanRule("5.03", "AutoEat", CheckSeverity.Critical, "autoeat", "foodslot", "autofood"),
			new BanRule("5.03", "AutoTotem", CheckSeverity.Critical, "autototem", "totemreplacer", "inventorytotem"),
			new BanRule("5.03", "ChestStealer", CheckSeverity.Critical, "cheststealer", "autosteal", "stealchest"),
			new BanRule("5.03", "Inventory Profiles Next", CheckSeverity.NeedsCheck, "inventoryprofilesnext", "invprofilesnext", "inventoryprofiles"),
			new BanRule("5.03", "ItemScroller", CheckSeverity.NeedsCheck, "itemscroller", "itemscroll"),
			new BanRule("5.03", "Mouse Tweaks", CheckSeverity.NeedsCheck, "mousetweaks", "mousewheelie"),
			new BanRule("5.03", "QuickSwap", CheckSeverity.Critical, "quickswap", "itemswap", "swaper"),
			new BanRule("5.03", "JSMacros / AutoHotkey", CheckSeverity.Critical, "jsmacros", "autohotkey", "keyran", "macroclient", "botmek"),

			// 5.04. Передвижение и работа с блоками (30d)
			new BanRule("5.04", "Baritone", CheckSeverity.Critical, "baritone", "fabritone", "cabaletta"),
			new BanRule("5.04", "BedrockBreaker", CheckSeverity.Critical, "bedrockbreaker", "breakbedrock"),
			new BanRule("5.04", "ElytraFly", CheckSeverity.Critical, "elytrafly", "elytraspeed", "fireworkhelper"),
			new BanRule("5.04", "FastPlace", CheckSeverity.Critical, "fastplace", "fasterblockplacement"),
			new BanRule("5.04", "Fly / GhostBlock", CheckSeverity.Critical, "flymod", "ghostblock", "noclipflymod"),
			new BanRule("5.04", "InvMove", CheckSeverity.Critical, "invmove", "inventorymove", "movementingui"),
			new BanRule("5.04", "Jesus (WaterWalk)", CheckSeverity.Critical, "jesus", "waterwalk"),
			new BanRule("5.04", "NoFall / NoSlowdown", CheckSeverity.Critical, "nofall", "noslowdown", "noslow"),
			new BanRule("5.04", "Nuker", CheckSeverity.Critical, "nuker", "fastbreak"),
			new BanRule("5.04", "Scaffold", CheckSeverity.Critical, "scaffold", "autobridge"),
			new BanRule("5.04", "Speed / Spider / Timer", CheckSeverity.Critical, "speedhack", "spider", "timermod"),
			new BanRule("5.04", "Tweakeroo", CheckSeverity.NeedsCheck, "tweakeroo", "tweaksclient"),

			// 5.05. Лишняя информация и обзор (15d)
			new BanRule("5.05", "ArmorIndicator", CheckSeverity.NeedsCheck, "armorindicator", "armorhud"),
			new BanRule("5.05", "Chams / ESP", CheckSeverity.Critical, "chams", "espmod", "boxesp", "tracers"),
			new BanRule("5.05", "Freecam", CheckSeverity.NeedsCheck, "freecam", "camerafreecam"),
			new BanRule("5.05", "FriendHighlighter", CheckSeverity.NeedsCheck, "friendhighlighter", "playerhighlighter"),
			new BanRule("5.05", "HealthIndicator", CheckSeverity.NeedsCheck, "healthindicator", "mobhealthbar", "healthbarplus", "torohealth", "damageindicator"),
			new BanRule("5.05", "NBT Tooltip", CheckSeverity.NeedsCheck, "nbttooltip", "nbtviewer", "nbtpeek"),
			new BanRule("5.05", "NearByAlert / PlayerRadar", CheckSeverity.NeedsCheck, "nearbyalert", "playerradar", "playeralert"),
			new BanRule("5.05", "PieChartRadar (Shift+F3)", CheckSeverity.NeedsCheck, "piechartradar", "shiftf3radar"),
			new BanRule("5.05", "TNTCartAlert", CheckSeverity.NeedsCheck, "tntcartalert", "tntalert"),

			// 5.06. Поиск баз, руды и сида (30d)
			new BanRule("5.06", "AdvancedXRay / X-Ray", CheckSeverity.Critical, "advancedxray", "xray", "x-ray", "xraypack", "xrayultimate"),
			new BanRule("5.06", "CaveFinder / ChestLocator", CheckSeverity.Critical, "cavefinder", "chestlocator", "spawnerlocator"),
			new BanRule("5.06", "DiamondScanner", CheckSeverity.Critical, "diamondscanner", "diamondsim", "diamondgen"),
			new BanRule("5.06", "Farsight", CheckSeverity.NeedsCheck, "farsight"),
			new BanRule("5.06", "Item Tracker", CheckSeverity.Critical, "itemtracker", "containersearcher"),
			new BanRule("5.06", "SeedCrackerX / SeedMapper", CheckSeverity.Critical, "seedcrackerx", "seedcracker", "seedmapper"),
			new BanRule("5.06", "Replay Mod / FlashBack (Базы)", CheckSeverity.NeedsCheck, "replaymod", "flashback"),

			// 5.07. Невидимые блоки и эффекты (15-30d)
			new BanRule("5.07", "DarknessBegone / NoDarkness", CheckSeverity.Critical, "darknessbegone", "nodarkness", "darknessremover"),
			new BanRule("5.07", "ICU / VisibleBarriers", CheckSeverity.Critical, "icumod", "visiblebarriers", "barrierhighlight"),
			new BanRule("5.07", "NoBlindness", CheckSeverity.Critical, "noblindness", "removeblindness"),
			new BanRule("5.07", "RemoveWardenEffect", CheckSeverity.Critical, "removewardeneffect", "removewarden", "nowarden"),
			new BanRule("5.07", "True Sight", CheckSeverity.Critical, "truesight", "antiinvis"),

			// 5.08. Важно (15-30d / perm)
			new BanRule("5.08", "Аналоги читов и запрещённого ПО", CheckSeverity.Critical, "customcheat", "hackclient", "privatclient", "selfdestruct", "bypassinject")
		};

		public static readonly HashSet<long> KnownCheatJarSizes = new HashSet<long>
		{
			2263L, 5266L, 6515L, 6770L, 6778L, 7016L, 7218L, 7803L, 7891L, 9327L,
			10283L, 10605L, 10958L, 11554L, 16541L, 17308L, 17339L, 18180L, 18527L, 18587L,
			18734L, 19266L, 20578L, 20583L, 20639L, 20883L, 21161L, 21234L, 21664L, 22036L,
			22861L, 26247L, 27546L, 27809L, 28084L, 28439L, 29304L, 29567L, 30279L, 31549L,
			31607L, 34449L, 34669L, 35971L, 35993L, 38149L, 39017L, 39321L, 40142L, 42782L,
			47159L, 48242L, 50828L, 51212L, 52426L, 54088L, 59381L, 62782L, 65316L, 65486L,
			65765L, 66659L, 67491L, 68794L, 69757L, 72334L, 74105L, 80751L, 88896L, 95530L,
			98811L, 100523L, 100799L, 101297L, 101571L, 101703L, 102297L, 102733L, 103761L, 104954L,
			105623L, 105672L, 112386L, 120640L, 138417L, 143006L, 143597L, 143600L, 147329L, 147873L,
			151762L, 153937L, 156722L, 156779L, 166677L, 169718L, 173698L, 183634L, 183651L, 192156L,
			202720L, 257482L, 263070L, 267746L, 274865L, 300286L, 334588L, 343169L, 350629L, 409616L,
			410358L, 517248L, 519731L, 532826L, 539151L, 556494L, 597406L, 636621L, 640838L, 878781L,
			925493L, 1077149L, 1165063L, 1181556L, 1444714L, 1471429L, 1569093L, 1822841L, 3113569L, 3425801L,
			3541075L, 3541138L, 3642292L, 3684385L, 4642998L, 5630483L, 7052171L, 7059952L, 22258750L, 25704986L,
			26179274L, 26691896L
		};

		public static readonly string[] CheatChatDotCommands = new string[]
		{
			".bind", ".vape", ".panic", ".selfdestruct", ".friend", ".config", ".irc", ".cabaletta", ".set", ".toggle",
			".t ", ".killaura", ".aura", ".esp", ".fly", ".step", ".speed", ".reach", ".hitbox", ".aim",
			".noclip", ".vclip", ".hclip", ".celka", ".akr", ".wex", ".nur", ".rich", ".load", ".save",
			".spammer", ".fakelag", ".target", ".macro", ".autoclicker", ".drop", ".inventory",
			".thunder", ".slinky", ".b4", ".boze", ".seed", ".xray"
		};

		public static readonly string[] PrefetchExternalCheatKeywords = new string[]
		{
			"EXPENSIVE", "NURSULTAN", "CELESTIAL", "CALESTIAL", "AKRIEN", "MINCED", "WEXSIDE", "DEADCODE", "RICHPREMIUM", "EXLOADER",
			"FLAUNCHER", "KOID", "VAPE", "METEOR", "WURST", "LIQUIDBOUNCE", "ZAMOROZKA", "BEBRAWARE", "ARISTOIS", "PROCESSHACKER",
			"SYSTEMINFORMER", "JOURNALTRACE", "USNJRNL", "RENAME_ME_PLEASE", "EDITME", "MC100", "DAUNTIBLYAT", "DMC",
			"THUNDERHACK", "SLINKY", "SEEDCRACKER", "SEEDCRACKERX", "DOOMSDAY", "BOZE", "RAVEN", "VULCAN", "AUTOCLICKER", "TARGETSTRAFE",
			"WEVTUTIL", "CIPHER", "SDELETE"
		};

		public static readonly string[] CheatKeybindPatterns = new string[]
		{
			"key_key.vape", "key_key.clickgui", "key_key.killaura", "key_key.aura", "key_key.reach", "key_key.fly", "key_key.speed", "key_key.step", "key_key.noclip", "key_key.esp",
			"key_key.aimbot", "key_key.triggerbot", "key_key.autototem", "key_key.hud", "key_key.celka", "key_key.wex", "key_key.akr", "key_key.nur", "key_gui:344", "key_key.meteor",
			"key_key.future", "key_key.rusher", "key_key.bleach", "key_key.liquid"
		};

		public static readonly string EverythingNamesQuery = "kami | Impact | Wurds | inertia | LiquidBounce | Sigma | NeverHook | Zamorozka | Minced | Wexside | Shit | Expensive | Wild | Fluger | keazclient | xray | x-ray | meteor | BlackBerry | Nursultan | Avalone | Rockstar | Akrien | Rich | Celestial | Calestial | DeadCode | nova-dev | BebraWare | RynWare | Neverware | Aristois | FreeCam | Baritone | Jigsaw | Salhack | Ares | Meteorclient | Meteor Client | CheatClientJex | Jex | Wolfram | WWE | BleachHack | Matix | pivoclient | cabaletta | Gish | GishCode | Eternity | RichPremiun | Rich | R3D | boberware | nurik | .celka | celka | .akr | .wex | .nur | Ponos | EmortalityClient | Dreampool | Clowdy | Rasty | DiamondSim | AutoAttack | Jessica | InvMove | TopkaAutoBuy | AutoBuy | ElytraSwap | ArmorHotSwap | GoProne | Autofish | Excellent | kami | kamiblue | PYRO | Impact | Wurst | inertia | LiquidBounce | NeverHook | Zamorozka | Minced | Wexside | Shit | ExpensiveRecode | Expensive | Wild | Fluger | FlugerClient | NoRender | keaz | keazclient | xray | x-ray | meteor | meteorclient | BlackBerry | Nursultan | Avalone | Rockstar | MERCU | NIOBIUM | kion | rich | celestial | deadcode | external | norules | rage | BebraWare | RynWare | Neverware | Aristois | FreeCam | Baritone | Simply | SimplyHax | SkillClient | Jigsaw | Zeus | Salhack | Ares | Meteorclient | CheatClientJex | Cheat | Wolfram | Wolfram | Metro | WWE | WeepCraft | Кalkon | Кalkonclient | BleachHack | AIM | AdvancedX | Matix | Nodus | Aurus | Neverclide | Wave | Prerik | Werwedc | SquadClient | Squad | rename_me_please.dll | Future | RusherHack | Eternity | RichPremium | EdItMe.dll | mc100.dll | R3D | Destroy | ArchWare | ArchWare | boberware | bolshoy | MeteorPenit | stardust | .wex | Haven | Ponos | EmortalityClient | Dreampool | Invisible | InvisibleClient | Winner | Clowdy | RastyLegit | Rasty | RastyLegit | DreampoolHack | DiamondSim | Auto | Potions | AutoAttack | Kyprak | Flauncher | SALHACK | Infinity | WintWare | Konas | Jessic | akson | carbonara | meriada | leontab | winnerfree | exelent | rockstar | fatal | crack | avolon | xray | nodus | huzuni | aristois | skillclient | impact | inertia-iternia | matixwurst | advanced | xrayxrayultimate | baritone | fabritone | mobhealthbar | healthbarplus | chunkanimator | inventoryprofilesnext | inventorytotem | topkaautobuy | xorekautobuy | topkaautosell | xorekautomyst | topkacasino | autosell | diamondgen | truesight | elytraswap | autoattack | featherlauncher | freecam | heststealer | itemscroller | mousetweaks | invmove | mousewheelie | worldedit | autofish | cabaletta | jigsaw | liquidbounce | wurst | gishcode | inertia | rename_meplease.dll | future | rusherhack | pyro | zamorozka | konas | wintware | nursultan | norules | akrien | sigma | deadcode | eternity | wexside | rich | richpremium | editme.dll | mc100.dll | matix | r3d | celestial | wild | destroy | archware | nightmare | boberware | flauncher | exloader | celka | celestialbaritone | x-ray | doomsday | autochest | cheststealer | betterclicker | cortex | troxil | .akr | .wex | norender | akrienantileek | vape | pivo | dauntiblyat.dll | meteor | ares | bleachhack | hits | hitboxbaritone | salhack | wwe | сelestial | flux | neverhook | loader | zenware | recode | minced | nurik | expensive | calestial | github | excellent | via | topka | kwish | fuze | VegaLine | selfdistruct | dekoclient2 | thunderhack | boze | slinky | dreamclient | doomsday | ravenb4 | expensive3 | nursultanrecode | seedcrackerx | flint | catlavan | cellent | boberware | bape | whiteout | itami | coffeeclient | kuraclient | thunderhackrecoded | pandora | norules | archware | minihitboxes";

		public static readonly string EverythingSizesQuery = "size:2263|size:5266|size:6515|size:6770|size:6778|size:7016|size:7218|size:7803|size:7891|size:9327|size:10283|size:10605|size:10958|size:11554|size:16541|size:17308|size:17339|size:18180|size:18527|size:18587|size:18734|size:19266|size:20578|size:20583|size:20639|size:20883|size:21161|size:21234|size:21664|size:22036|size:22861|size:26247|size:27546|size:27809|size:28084|size:28439|size:29304|size:29567|size:30279|size:31549|size:31607|size:34449|size:34669|size:35971|size:35993|size:38149|size:39017|size:39321|size:40142|size:42782|size:47159|size:48242|size:50828|size:51212|size:52426|size:54088|size:59381|size:62782|size:65316|size:65486|size:65765|size:66659|size:67491|size:68794|size:69757|size:72334|size:74105|size:80751|size:88896|size:95530|size:98811|size:100523|size:100799|size:101297|size:101571|size:101703|size:102297|size:102733|size:103761|size:104954|size:105623|size:105672|size:112386|size:120640|size:138417|size:143006|size:143597|size:143600|size:147329|size:147873|size:151762|size:153937|size:156722|size:156779|size:166677|size:169718|size:173698|size:183634|size:183651|size:192156|size:202720|size:257482|size:263070|size:267746|size:274865|size:300286|size:334588|size:343169|size:350629|size:409616|size:410358|size:517248|size:519731|size:532826|size:539151|size:556494|size:597406|size:636621|size:640838|size:878781|size:925493|size:1077149|size:1165063|size:1181556|size:1444714|size:1471429|size:1569093|size:1822841|size:3113569|size:3425801|size:3541075|size:3541138|size:3642292|size:3684385|size:4642998|size:5630483|size:7052171|size:7059952|size:22258750|size:25704986|size:26179274|size:26691896 *.jar";

		public static bool IsAllowedVisualMod(string nameOrPath)
		{
			if (string.IsNullOrEmpty(nameOrPath))
			{
				return false;
			}
			string text = nameOrPath.ToLowerInvariant();
			string[] allowedVisuals = AllowedVisuals;
			foreach (string text2 in allowedVisuals)
			{
				if (text.IndexOf(text2, StringComparison.Ordinal) >= 0)
				{
					return true;
				}
			}
			return false;
		}

		public static bool IsAllowedCpvpOptimizer(string nameOrPath)
		{
			if (string.IsNullOrEmpty(nameOrPath))
			{
				return false;
			}
			string text = nameOrPath.ToLowerInvariant();
			return text.Contains("marlow") ||
			       text.Contains("haramcrystal") ||
			       text.Contains("endcrystal") ||
			       text.Contains("walksy") ||
			       text.Contains("herosanchor") ||
			       text.Contains("cutebow");
		}

		public static bool IsWhitelistedPath(string path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return false;
			}
			string text = path.Replace('/', '\\').ToLowerInvariant();

			if (IsAllowedVisualMod(text) || IsAllowedCpvpOptimizer(text))
			{
				return true;
			}

			string[] whitelistPathPatterns = WhitelistPathPatterns;
			foreach (string text2 in whitelistPathPatterns)
			{
				if (text.IndexOf(text2.ToLowerInvariant(), StringComparison.Ordinal) >= 0)
				{
					return true;
				}
			}

			string userName = Environment.UserName;
			if (!string.IsNullOrEmpty(userName) && userName.Length >= 3 && (text.IndexOf("\\users\\" + userName.ToLowerInvariant() + "\\", StringComparison.Ordinal) == 0 || text.Contains("\\users\\" + userName.ToLowerInvariant() + "\\")))
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text);
				if (string.Equals(fileNameWithoutExtension, userName, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			string machineName = Environment.MachineName;
			if (!string.IsNullOrEmpty(machineName) && machineName.Length >= 3)
			{
				string fileNameWithoutExtension2 = Path.GetFileNameWithoutExtension(text);
				if (string.Equals(fileNameWithoutExtension2, machineName, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}

		public static bool IsWhitelistedProcess(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return false;
			}
			string text = name.ToLowerInvariant();
			string[] whitelistProcessNames = WhitelistProcessNames;
			foreach (string text2 in whitelistProcessNames)
			{
				if (text.IndexOf(text2.ToLowerInvariant(), StringComparison.Ordinal) >= 0)
				{
					return true;
				}
			}
			return false;
		}

		public static bool IsAmbiguous(string signature)
		{
			if (!string.IsNullOrEmpty(signature))
			{
				return AmbiguousNames.Contains(signature);
			}
			return false;
		}

		public static bool HasCheatContext(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}
			string text2 = text.ToLowerInvariant();
			string[] cheatContextMarkers = CheatContextMarkers;
			foreach (string value in cheatContextMarkers)
			{
				if (text2.IndexOf(value, StringComparison.Ordinal) >= 0)
				{
					return true;
				}
			}
			return false;
		}

		public static bool Accept(string pathOrText, string signature, out bool severityDowngrade)
		{
			severityDowngrade = false;
			if (string.IsNullOrEmpty(signature))
			{
				return false;
			}
			if (IsWhitelistedPath(pathOrText))
			{
				return false;
			}
			if (IsAmbiguous(signature))
			{
				if (!HasCheatContext(pathOrText))
				{
					severityDowngrade = true;
					return false;
				}
				severityDowngrade = true;
			}
			return true;
		}

		public static string DescribeDatabases()
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("Амбигуальных сигнатур (требуют контекста): " + AmbiguousNames.Count);
			stringBuilder.AppendLine("Белый список путей: " + WhitelistPathPatterns.Length);
			stringBuilder.AppendLine("Разрешённых визуалов / паков / оптимизаторов: " + AllowedVisuals.Length);
			stringBuilder.AppendLine("Белый список процессов: " + WhitelistProcessNames.Length);
			stringBuilder.AppendLine("Доп. чит-клиентов: " + ExtraCheatClients.Length);
			stringBuilder.AppendLine("Доп. инжекторов/тулз: " + ExtraInjectorsAndLoaders.Length);
			stringBuilder.AppendLine("Доп. доменов: " + ExtraCheatDomains.Length);
			stringBuilder.AppendLine("Доп. визуал-ключей: " + ExtraVisualKeywords.Length);
			stringBuilder.AppendLine("Правил регламента 5.01–5.08: " + BannedByRegulation.Length);
			return stringBuilder.ToString();
		}

		public static string NormalizeKey(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return string.Empty;
			}
			StringBuilder stringBuilder = new StringBuilder(text.Length);
			string text2 = text.ToLowerInvariant();
			foreach (char c in text2)
			{
				if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
				{
					stringBuilder.Append(c);
				}
			}
			return stringBuilder.ToString();
		}

		public static BanRule[] MatchRegulationKeys(string normalizedContent)
		{
			if (string.IsNullOrEmpty(normalizedContent))
			{
				return Array.Empty<BanRule>();
			}
			List<BanRule> list = new List<BanRule>(3);
			BanRule[] bannedByRegulation = BannedByRegulation;
			foreach (BanRule banRule in bannedByRegulation)
			{
				string[] keys = banRule.Keys;
				foreach (string text in keys)
				{
					string value = NormalizeKey(text);
					if (normalizedContent.Contains(value))
					{
						list.Add(banRule);
						break;
					}
				}
				if (list.Count >= 2 && list.All((BanRule r) => r.Severity == CheckSeverity.Critical))
				{
					break;
				}
			}
			return list.ToArray();
		}

		public static bool HasAnyRegulationKey(string normalizedContent)
		{
			if (string.IsNullOrEmpty(normalizedContent))
			{
				return false;
			}
			BanRule[] bannedByRegulation = BannedByRegulation;
			foreach (BanRule banRule in bannedByRegulation)
			{
				string[] keys = banRule.Keys;
				foreach (string text in keys)
				{
					if (normalizedContent.Contains(NormalizeKey(text)))
					{
						return true;
					}
				}
			}
			return false;
		}
	}
}
