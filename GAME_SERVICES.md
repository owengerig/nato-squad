# NATO Squad: Game Center and Google Play Games setup

NATO Squad uses Apple Game Center (iOS) and Google Play Games Services (Android) for sign-in, achievements,
leaderboards and a cloud copy of the player's profile, through OG Software's shared `OGPlatform` code. The code
is done; these are the one-time steps in each store. Until they're done the game works normally and keeps
achievement progress and best scores on the device, then sends them once everything exists.

Bundle ID / package name: `com.ogsoftware.natosquad`. Game code: `Assets/Scripts/NatoSquad/Core/GameServices.cs`.

## Apple (App Store Connect + developer portal)

1. **Developer portal → Identifiers → `com.ogsoftware.natosquad`:** tick **Game Center** and **iCloud** (iCloud is where Game
   Center keeps saved games; the default container is fine). Regenerate the provisioning profile.
2. **App Store Connect → the app → Services → Game Center:** turn Game Center on for the app version, then add
   each achievement and leaderboard below. Use the **IDs exactly as written** (the code uses them).
3. Add the achievements and leaderboards to the app version ("Game Center" section of the version page) before
   submitting. Test on a device with a sandbox account: Settings → Game Center.
4. The Xcode project gets GameKit and the Game Center / iCloud capabilities automatically
   (`Assets/OGPlatform/Editor/OGPlatformBuild.cs`, or the native project for non-Unity apps).

## Google (Play Console)

1. **Play Console → the app → Grow users → Play Games Services → Setup and management → Configuration:**
   create a Play Games Services project for the app.
2. **Credentials:** add an Android credential with an OAuth client for the package name and the SHA-1 of
   **both** the upload key and the Play app-signing key (Setup → App signing). Debug builds need the debug key's
   SHA-1 too.
3. **Saved games:** turn it on (Configuration → Properties).
4. Create each achievement and leaderboard below. Progress achievements (steps > 1) must be **Incremental**
   with the same number of steps. Copy each generated ID (looks like `CgkI...EAIQAQ`) into the code file above,
   replacing its `TODO`, and the **Project ID** (the long number at the top of Configuration) into
   `AndroidAppId`.
5. **Testers:** add your Google account under Testers, then **Publish** the Play Games configuration.

## Achievements (13, 600 Game Center points)

| iOS ID | Name | Description | Points | Steps |
|---|---|---|---|---|
| `com.ogsoftware.natosquad.first_clear` | Wheels Up | Clear your first operation. | 10 | - |
| `com.ogsoftware.natosquad.ops_4` | Halfway Home | Clear 4 operations. | 30 | 4 |
| `com.ogsoftware.natosquad.ops_all` | Mission Accomplished | Clear all 8 operations. | 100 | 8 |
| `com.ogsoftware.natosquad.perfect` | Flawless | Earn 3 stars on an operation. | 20 | - |
| `com.ogsoftware.natosquad.all_stars` | Ace of Aces | Earn 3 stars on every operation. | 100 | 24 |
| `com.ogsoftware.natosquad.kills_500` | Hunter | Destroy 500 enemies. | 20 | 500 |
| `com.ogsoftware.natosquad.kills_5000` | Exterminator | Destroy 5,000 enemies. | 60 | 5000 |
| `com.ogsoftware.natosquad.sorties_50` | Veteran Pilot | Fly 50 sorties. | 30 | 50 |
| `com.ogsoftware.natosquad.full_squad` | Full Squadron | Own every jet. | 50 | - |
| `com.ogsoftware.natosquad.legendary` | Legendary Gear | Own a Legendary module. | 30 | - |
| `com.ogsoftware.natosquad.mythic` | Mythic Gear | Own a Mythic module. | 60 | - |
| `com.ogsoftware.natosquad.crates_25` | Quartermaster | Open 25 crates. | 20 | 25 |
| `com.ogsoftware.natosquad.millionaire` | Millionaire | Earn 1,000,000 gold in total. | 70 | - |

All achievements are visible (not hidden). Icons: 512×512 for both stores; the app icon on a coloured
background is fine to start with.

## Leaderboards (2)

| iOS ID | Name | Score | Order |
|---|---|---|---|
| `com.ogsoftware.natosquad.total_kills` | Enemies Destroyed | Total enemies destroyed | High to low |
| `com.ogsoftware.natosquad.lifetime_gold` | Gold Earned | Total gold earned | High to low |

All are "all-time", classic (not recurring) leaderboards. On Google Play set the same order and an integer
format.

## Cloud save

- What's saved: the player's whole profile, as one saved game called `profile`.
- When: a few seconds after each local save, and when the app goes to the background.
- On a new device or after a reinstall the cloud copy is loaded automatically after sign-in. When both have
  data, the copy that was saved more often wins (each save counts up a revision number); the replaced copy is
  kept on the device (`og.cloud.backup`).
- Nothing is uploaded until the cloud copy has been read, so a fresh install can't wipe a good backup.
