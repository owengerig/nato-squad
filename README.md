# NATO Squad

A side-scrolling jet shooter for iOS and Android, built in Unity 6. Created by **OG Software**. Version **0.1.0**.

It plays like the classic 16-bit mercenary-squadron shooters. You fly left to right, your guns fire automatically, and
you fight air and ground enemies until a boss shows up at the end of each operation. Missions pay gold, and you spend
it on upgrades between sorties. All art, names, characters and music are original. Every sprite and sound is generated
in code at startup, so the game ships no image or audio files apart from the app icon.

## Running the game
1. Open this folder in Unity **6000.6.9f1** (or newer Unity 6), with **iOS Build Support** and/or **Android Build Support**.
2. Use the **NATO Squad → Play Game** menu, or open `Assets/Scenes/Main` and press **Play**.

The scene only contains a camera. Everything else, including UI, art, audio and the store, is built from code when Play
starts (`GameManager` boots itself). Editor helpers live in the **NATO Squad** menu: test gold, unlock all missions,
delete the save, version bumps, and app icons.

### Controls
| | Mobile | Editor / desktop |
|---|---|---|
| Fly | Drag anywhere (relative, like a trackpad) | Drag with the mouse, or **WASD** / arrows |
| Guns | Automatic | Automatic |
| Special weapon | Big button, bottom-right | **Space** or **K** |
| Pause | **II** button, top-right | **Esc** |

## Gameplay
- **8 operations.** Each has its own terrain, enemy mix and boss: Border Shield (rail battery), Iron Tide (destroyer),
  Granite Pass (mountain fortress), Dust Convoy (sand crawler), Thunderhead (flying gunship), Polar Night (submarine),
  Night Canyon (enemy ace) and Black Citadel (airborne fortress). Clearing an operation unlocks the next one.
- **Enemies.**
  - Air: fighters, interceptors, bombers, attack helicopters, drone swarms and aces.
  - Ground and sea: tanks, AA guns firing flak, SAM launchers with homing missiles (you can shoot the missiles down),
    trucks, gunboats and bunkers.
- **Hull and repair.** Taking hits wears down your hull. If you avoid damage for a few seconds, the hull repairs itself.
  Below 30% the HUD shows **DANGER**.
- **Gun power (1–8).** Shooting enemies drops blue capsules that raise your gun power, and big targets always drop one.
  Green capsules repair the hull.
- **Three jets.** These are fictional designs styled after modern NATO aircraft:
  - **FX-35 Specter**: a stealth multirole jet with a spread gun. You start with it.
  - **TF-4 Tempest**: a canard interceptor with a fast stream gun. 12,000 gold.
  - **AG-10 Bulldog**: an armored attack jet that fires forward and down at 45°. 20,000 gold.
- **Gold.**
  - During a mission, enemies drop coins and a magnet pulls them in.
  - Clearing a mission adds a reward. The first clear pays in full, replays pay half, and every star adds 25%.
  - Stars depend on how much hull damage you took.
  - If you fail or abort, you keep the gold you collected.

## Upgrades (Hangar)
- **Upgrades:** 12 permanent tracks with 4 to 15 levels each and rising costs. They cover gun damage, fire rate, hull,
  repair, speed, special capacity, gold bonus, starting gun power, pickup range, crit chance, special damage and armor.
- **Modules:** gear with rarity tiers, in six slots (Gun, Engine, Armor, Avionics, Warhead, Drone).
  - Rarities: **Common, Uncommon, Rare, Epic, Legendary, Mythic**. Power per tier: ×1, ×1.6, ×2.5, ×4, ×6.5, ×10.
    Max level per tier: 5, 10, 15, 20, 25, 30.
  - Each module has a primary and a secondary stat (20 module types).
  - **Level up** a module with gold.
  - **Fuse** three spare modules of the same slot and rarity into one module of the next rarity.
  - **Salvage** a module for gold.
  - A Drone module adds a wingman drone that fires alongside you.
- **Armory:** 8 special weapons that you buy as ammo with gold, then equip one per sortie. They unlock as you clear
  operations: Cluster Bombs, Seeker Missiles, Rocket Salvo, Napalm, Rail Lance (piercing beam), Aegis Shield, Swarm
  Drones and Thermobaric Strike (hits every enemy on screen).

## Store
- **Gold packs** for real money: 5k, 30k, 70k and 160k.
- **Supply crates.** Standard and Elite cost gold. The Command Crate costs real money and holds 3 modules of Rare or
  better. Drop odds are shown on every crate, as Apple and Google require. A Legendary is guaranteed at least once
  every 40 crate modules.
- **Daily Deal:** one Epic, Legendary or Mythic module for gold. It changes at 00:00 UTC.
- **Starter Pack** (real money) and **Double Gold Pay** (real money, permanent: doubles mission gold).
- **Restore Purchases** is in the Store and in Settings.

### In-app purchase setup
Purchases use **Unity IAP 5** (`com.unity.purchasing`, `Assets/Scripts/NatoSquad/Store/IAPManager.cs`). In the editor
it uses Unity's fake store, so you can test the whole flow. For the real stores, create these products in **App Store
Connect** and **Google Play Console**, using exactly these IDs:

| Product ID | Type | Grants |
|---|---|---|
| `com.ogsoftware.natosquad.gold_small` | Consumable | 5,000 gold |
| `com.ogsoftware.natosquad.gold_medium` | Consumable | 30,000 gold |
| `com.ogsoftware.natosquad.gold_large` | Consumable | 70,000 gold |
| `com.ogsoftware.natosquad.gold_huge` | Consumable | 160,000 gold |
| `com.ogsoftware.natosquad.command_crate` | Consumable | 3 modules (Rare+) |
| `com.ogsoftware.natosquad.starter_pack` | Consumable | 20,000 gold + 1 Epic module + 20 Seeker Missiles |
| `com.ogsoftware.natosquad.double_gold` | Non-consumable | Double mission gold, permanently |

Prices come from the stores. The amounts shown in the code (`$0.99` and so on) are fallbacks for when the store
can't be reached. The game saves before it confirms a purchase, and it records transaction IDs so a purchase is never
granted twice.

## Game Center / Google Play Games
- Shared OG Software code in `Assets/OGPlatform` (+ native bridges in `Assets/Plugins/iOS/OGPlatform` and
  `Assets/Plugins/Android/OGPlatform`); the game's part is `Assets/Scripts/NatoSquad/Core/GameServices.cs`.
- 13 achievements and 2 leaderboards (enemies destroyed, lifetime gold), worked out from the save after every save,
  so progress made while signed out is sent at the next sign-in.
- Cloud save: the whole save. `SaveData.revision` counts saves; on sign-in the copy saved more often wins and a fresh
  install always takes the cloud copy. A restore that arrives during a sortie waits for the main menu. Double Gold
  Pay and granted purchase IDs on the device are kept when a cloud copy is loaded.
- Xcode (GameKit, Game Center + iCloud) and Gradle (Play Games library, App ID) are set up automatically at build
  time by `Assets/OGPlatform/Editor/OGPlatformBuild.cs`.
- Store setup (IDs, names, descriptions, points): [`GAME_SERVICES.md`](GAME_SERVICES.md).

## Main menu, privacy and support
- The bottom-left of the main menu shows `Created by OG Software v0.1.0` (the same line in every OG Software app).
- The bottom-right has **ACHIEVEMENTS** and **LEADERBOARDS** (Game Center / Google Play Games; they sign in first if
  needed).
- **Settings** has links to the **Privacy Policy** (https://sites.google.com/view/og-software/privacy-policy) and
  **Support** (https://sites.google.com/view/og-software/support). The same URLs are in `Core/GameVersion.cs` (`Links`).
- The game collects no personal data. Progress is saved on the device and, when signed in to Game Center / Play
  Games, backed up to its saved games. See [`PRIVACY.md`](PRIVACY.md). The iOS
  privacy manifest is `Assets/Plugins/iOS/PrivacyInfo.xcprivacy`.

## Render pipeline (URP 2D)
- The game uses the Universal Render Pipeline with the 2D Renderer, the same setup as 2D Clash. Unity 6.5+
  deprecates the Built-in Render Pipeline (the warning Unity Hub showed).
- Everything is drawn with SpriteRenderers and UI, with no custom shaders, lights or post-processing, so nothing else
  needed converting. Sprites without their own material get URP's default sprite material automatically.
- Settings: `Assets/Settings/Rendering/RenderPipeline.asset` (uses `Renderer2D.asset`).
  `Assets/Settings/Rendering/Editor/URPSetup.cs` assigns it in Graphics settings and every Quality level when the
  project opens (and installs URP if the package is missing). Check with **OG Software > Render Pipeline > Check
  URP Setup**. Commit the changed `ProjectSettings/` files after the first open.

## Versioning
The app is at **0.1.0, build 2** (reset to 0.1.0 with the other OG Software apps; the build number kept rising). Use **NATO Squad → Version → Bump Patch / Minor / Major / Build Only**. These
commands keep Player Settings, the Android versionCode, the iOS build number and `GameVersion.Build` in step. Commit
`ProjectSettings/ProjectSettings.asset` and `GameVersion.cs` afterwards.

- Bundle ID / package name: `com.ogsoftware.natosquad`
- Orientation: landscape only
- Minimum versions: iOS 15, Android 8 (API 26)

## Cloud builds (GitHub Actions)
Both workflows run manually. They skip the build when nothing in `Assets/`, `Packages/` or `ProjectSettings/` changed
since the latest release, unless you tick **Build even if nothing changed**. They publish to **Releases** as
`NATO Squad v<version> (build <n>)`.

### Android: APK / AAB
Go to **Actions → Build Android APK → Run workflow** (`.github/workflows/build-android.yml`, GameCI).
- **apk:** download it on the phone and install it.
- **aab:** upload it to Google Play Console. This needs the signing secrets.

### iOS: IPA
Go to **Actions → Build iOS IPA → Run workflow** (`.github/workflows/build-ios.yml`). It runs in two steps:
1. Linux and GameCI export the Xcode project.
2. macOS and Xcode archive it and export the `.ipa`.

What you get depends on the secrets:
- **With the signing secrets:** a signed `.ipa`. The export type is App Store Connect or Release Testing (Ad Hoc).
  Tick **Upload to TestFlight** to send it straight to TestFlight.
- **Without them:** an **unsigned** `.ipa`, which sideloading tools can re-sign. This is useful before you have an Apple
  Developer account.

### Secrets
Add these under Settings → Secrets and variables → Actions:

| Secret | Used by | Notes |
|---|---|---|
| `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` | both | Same as 2D Clash. |
| `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASS`, `ANDROID_KEYALIAS_NAME`, `ANDROID_KEYALIAS_PASS` | Android | Optional for APK, required for AAB. |
| `APPLE_TEAM_ID` | iOS | 10-character team ID. |
| `IOS_CERTIFICATE_P12_BASE64`, `IOS_CERTIFICATE_PASSWORD` | iOS | Apple Distribution certificate and key (.p12), base64. |
| `IOS_PROVISIONING_PROFILE_BASE64` | iOS | App Store or Ad Hoc profile for `com.ogsoftware.natosquad`, base64. |
| `APP_STORE_CONNECT_KEY_ID`, `APP_STORE_CONNECT_ISSUER_ID`, `APP_STORE_CONNECT_KEY_P8` | iOS | Only for the TestFlight upload. |

Signing: create a keystore once, for example
`keytool -genkeypair -v -keystore nato-squad.keystore -alias natosquad -keyalg RSA -keysize 2048 -validity 10000`.
Keep it safe, because Google Play needs the same key for every update. Then store it base64-encoded in
`ANDROID_KEYSTORE_BASE64`. The `.gitignore` keeps keystores, certificates and profiles out of git.

## Code map (`Assets/Scripts/NatoSquad`)
| Folder | What's in it |
|---|---|
| `Core/` | `GameManager` (boot, screens, dialogs, toasts), `Audio` (synthesized SFX and music), `GameVersion` + `Links` |
| `Data/` | `SaveData` (JSON save + settings), `Upgrades` (stats, rarities, modules, tracks, jets, specials, crates, daily deal), `Missions` |
| `Art/` | `Painter` (anti-aliased vector rasterizer) and `SpriteFactory` (every sprite, drawn at startup) |
| `Battle/` | `Battle` (the sortie: input, guns, enemies, bosses, specials, pickups, results), `Entities` (pools, shots, effects), `Backdrop` (parallax) |
| `Screens/` | Main menu, operation select, battle HUD and pause, results, hangar, store, settings |
| `Store/` | `IAPManager` (Unity IAP 5) |
| `Editor/` | Version tools, app icons, editor menu, iOS Info.plist post-process, CI iOS signing |
