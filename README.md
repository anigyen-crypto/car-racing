# Unity 3D Car Racing Game (Starter Kit)

A complete, playable arcade car racing game for Unity. Cars use real
`WheelCollider` physics, there are AI opponents, a 3-2-1-GO countdown,
lap counting with best-lap timing, a chase camera and a HUD.

**Best part:** one menu click builds the entire scene for you — ground,
road, walls, checkpoints, 3 cars, camera, HUD. No manual scene assembly.

---

## Quick Start (2 minutes)

1. **Create a project**
   Open Unity Hub → *New Project* → **3D (Built-in)** or **3D (URP)** template.

2. **Copy the scripts in**
   Copy the `Assets` folder from this kit into your project (so you end up
   with `Assets/Scripts/CarController.cs` etc.). Unity will compile them.

3. **Open any scene** (e.g. `Scenes/SampleScene`).

4. **Build the track**
   Menu bar → **Tools → Racing Kit → Build Demo Track**.
   This generates everything: ground, road, walls, checkpoints, player car,
   2 AI cars, race manager, camera and HUD.

5. **Press Play** 🏁

---

## Controls

| Input | Action |
|---|---|
| `W` / `↑` | Accelerate |
| `S` / `↓` | Brake / Reverse |
| `A` `D` / `←` `→` | Steer |
| `Space` | Handbrake |
| `R` | Reset car to last checkpoint (if you flip/get stuck) |

---

## What's included

| File | Purpose |
|---|---|
| `Scripts/CarController.cs` | WheelCollider-based car physics (drive, brake, reverse, handbrake, speed-sensitive steering, downforce) |
| `Scripts/PlayerInput.cs` | Keyboard input → CarController |
| `Scripts/AICarController.cs` | Waypoint-following AI driver (eases off for corners, brakes when needed) |
| `Scripts/CameraFollow.cs` | Smooth chase camera with look-ahead |
| `Scripts/RaceManager.cs` | Countdown, ordered checkpoint lap counting, lap/total/best times, finish detection, `R` respawn |
| `Scripts/Checkpoint.cs` | Invisible trigger reports the player passing |
| `Scripts/HUD.cs` | Speed, lap, timer, best lap, countdown / GO! / FINISH messages |
| `Scripts/Editor/TrackBuilder.cs` | **The one-click scene generator** (menu item lives here) |

---

## Customizing

### Your own track shape
Open `Assets/Scripts/Editor/TrackBuilder.cs` and edit `MakeWaypoints()`:

```csharp
static Vector3[] MakeWaypoints()
{
    // Your own loop of points (last connects back to first)
    return new Vector3[]
    {
        new Vector3(0, 0, 0),    new Vector3(60, 0, 10),
        new Vector3(120, 0, 0),  new Vector3(140, 0, -60),
        // ...as many points as you like
    };
}
```

Then run the Build menu again. Road, walls, checkpoints and AI lines all
regenerate automatically.

### Number of laps
Select the `RaceManager` object in the hierarchy and change **Total Laps**.

### AI speed / difficulty
Select an `AICar_*` object → `AICarController` component:
- **Cruise Speed** – how fast it tries to drive (km/h)
- **Waypoint Reach Distance** – how close to the racing line it stays
- **Max Steer Lookahead** – how sharply it reacts to corners

### Car tuning (select a car → CarController)

| Field | Effect |
|---|---|
| `Motor Torque` | Acceleration strength (higher = faster launch) |
| `Max Speed km/h` | Top speed limiter |
| `Brake Torque` | Stopping power |
| `Down Force` | More = better high-speed grip, less sliding |
| `Max Steer Angle` | Sharper cornering (reduce if wheelspin/spinouts) |
| `Center Of Mass` | Lower Y = less body roll, harder to flip |

Wheel grip lives in the 4 `Wheel*` child objects (`Sideways Friction →
Stiffness`: raise to ~1.5 for grippier, lower to ~0.8 for drifty).

### Car colors
Edit the `Color(...)` values in `TrackBuilder.Build()` (PlayerCar, AI cars)
and rebuild.

---

## Troubleshooting

**Materials are pink/magenta** — you're on URP and the renderer wasn't found.
The builder auto-detects URP; if it still happens, just reassign materials on
the road glyph / car body cubes.

**Physics feel weird after I changed Unity version** — WheelCollider stiffness
values are version-sensitive. If the car floats or bounces, tweak the
`Wheel*` children: **Suspension Spring** (≈ 40,000) and **Damper** (≈ 4,000).

**HUD text is empty** — a font resource may not exist on that Unity version.
The builder tries `LegacyRuntime.ttf` then `Arial.ttf`; assign any font to the
Text components manually if needed.

**Car spins out on corners** — lower `Max Steer Angle`, raise `Down Force`, or
reduce speed entering corners. Handbrake is intentional drift-inducing.

---

## Ideas for extending it

- Engine sound: add an `AudioSource` with a looping engine clip, pitch it by
  `car.SpeedKmh` in `CarController.FixedUpdate`
- Drift marks / smoke particles on handbrake
- Race results table comparing player vs AI
- Minimap: render the waypoint loop into a `RawImage`
- Multiple tracks: save waypoint presets as ScriptableObjects
- Mobile controls: replace `PlayerInput`'s `Input.GetAxis` calls with
  on-screen buttons/`Input.SystemUI` touch axes
- Car selection screen with different tuning presets

## Building the Android APK 📱

The project is Android-ready: touch controls (hold left/right half of the
screen to steer, hold the bottom-middle zone to brake — it auto-accelerates),
landscape orientation, IL2CPP + ARM64, and touch steering already wired into
`PlayerInput`. Either build it locally in Unity, or let a free GitHub
Actions cloud machine build it for you (no Android SDK or Unity install
needed on your PC).

### Option A — Cloud build (recommended, no installs)

This folder is already a valid Unity project root (ProjectSettings +
Packages included) with a CI workflow at `.github/workflows/android.yml`.

1. Put this folder in a GitHub repository and push it:
   ```bash
   cd UnityCarRacing
   git init && git add . && git commit -m "Racing game"
   git remote add origin https://github.com/YOUR_NAME/car-racing.git
   git push -u origin main
   ```
2. Connect your Unity account to the CI (one-time, ~5 minutes):
   - Create the repo secrets `UNITY_EMAIL` and `UNITY_PASSWORD` with your
     Unity account credentials (Repo → Settings → Secrets and variables →
     Actions). The workflow tries those first.
   - If Unity has 2FA or activation fails, use the license-file route:
     run the workflow once, download the `.alf` file GameCI produces, upload
     it at https://license.unity3d.com/manual, pick "Personal", download the
     resulting `.ulf`, open it in a text editor, copy its whole contents into
     a secret called `UNITY_LICENSE`.
3. Run the workflow (Actions tab → **Build Android APK** → Run).
4. When it finishes, download the `CarRacing-APK` artifact — that's your
   signed installable APK. Enable "install unknown apps" on your phone and
   install it.

Build time: roughly 15–25 minutes. Every push to `main` triggers a fresh
build automatically.

### Option B — Local build

1. Install Unity Hub, sign in with a (free) Unity Personal account.
2. Installs → Install Editor → **2022.3.55f1** (or the version in
   `ProjectSettings/ProjectVersion.txt`) and tick:
   - **Android Build Support**
     - Android SDK & NDK Tools
     - OpenJDK
3. Open the `UnityCarRacing` folder as a project (Assets are already in
   place), wait for compilation.
4. Menu: **Tools → Racing Kit → Build Android APK** → APK appears at
   `build/CarRacing.apk`.

> The game auto-generates the demo scene during the build (`BuildScript.cs`
> calls the TrackBuilder), so you don't need to run the scene builder by
> hand — but running **Tools → Racing Kit → Build Demo Track** first lets
> you play-test the scene in the editor before building.

### Signing
CI generates a throwaway keystore every run (`racing.keystore`,
password `Racing2026`, alias `racing`) so the APK installs directly on any
phone. For **Google Play publishing**, create a permanent keystore yourself
and fill in the keystore settings in the workflow/build script instead.

## Requirements

- Unity 2019.4 LTS or newer (tested logic works through Unity 6)
- Built-in or URP render pipeline
- No external assets or plugins required
- APK builds: Android Build Support module (local) or GitHub Actions (cloud)
