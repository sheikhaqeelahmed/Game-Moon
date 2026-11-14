# Planet Scrappers

*Nothing lasts. Everything has value.*

An original first-person salvage sim. You dismantle the rusting industry left on the airless planet Kessler-9,
collect the scrap, sell it, buy better tools and grow a salvage operation.

- Approved concept: [docs/concept/planet-scrappers-concept.html](docs/concept/planet-scrappers-concept.html)
- Changelog: [CHANGELOG.md](CHANGELOG.md)
- Third-party licences: [docs/third-party-licences.md](docs/third-party-licences.md)

## Requirements

| Tool | Version | Notes |
|---|---|---|
| Unity | Unity 6 LTS (6000.x) | Install through Unity Hub with **Windows Build Support (Mono)**. A free Unity Personal licence is enough. |
| Render pipeline | URP | Comes with the Universal 3D template. |
| Packages | Input System | Required by the player controller. |
| IDE | Visual Studio Community 2022 | Optional, for editing C#. |
| Git + Git LFS | Any recent | Binary art and audio are stored with LFS. |

## Opening the project

1. Clone the repository and run `git lfs pull`.
2. In Unity Hub choose **Add > Add project from disk** and select this folder.
3. Open the project. The first import takes a few minutes.
4. Run **Planet Scrappers > Build Rust Valley Greybox** from the menu bar. This generates the materials,
   textures and `Assets/Scenes/RustValley.unity`.
5. Open `Assets/Scenes/RustValley.unity` and press Play.

## Controls (Module 1)

| Action | Keyboard / mouse | Gamepad |
|---|---|---|
| Move | WASD or arrow keys | Left stick |
| Look | Mouse | Right stick |
| Sprint | Left Shift | Left stick press |
| Jump | Space | A / Cross |
| Release mouse | Esc | — |
| Resume | Left click | — |

## Making a QA build

Menu: **Planet Scrappers > Build Windows QA Zip**.

Command line (from the project folder):

```bash
"C:/Program Files/Unity/Hub/Editor/<version>/Editor/Unity.exe" -batchmode -quit -projectPath . -executeMethod PlanetScrappers.EditorTools.BuildScript.BuildWindowsQA -logFile Logs/build.log
```

Output:

- `Builds/PlanetScrappers_v<version>_QA/` — the game folder (`PlanetScrappers.exe`, data, `CHANGELOG.txt`, `VERSION.txt`)
- `Builds/PlanetScrappers_v<version>_QA.zip` — send this to testers. They extract it and run `PlanetScrappers.exe`; Unity is not needed.

The version comes from `Assets/Scripts/Core/GameVersion.cs`. Bump it and add a `CHANGELOG.md` entry for every QA release.
`Builds/` is ignored by Git and never committed.

## Folder layout

```
Assets/
  Art/            Characters, Environments, Props, Materials, Textures
  Audio/
  Animations/
  Prefabs/        Characters, Environment, Objects, UI
  Scenes/
  Scripts/        Core, Player, Companion, Environment, Interaction, Destruction,
                  Inventory, Economy, Equipment, Missions, Save, UI, Editor
  ScriptableObjects/
  Settings/
  UI/
docs/             Concept, licences, QA material
```

## Milestone status

| Module | Scope | Status |
|---|---|---|
| 1 | Project setup, first-person movement, Rust Valley grey-box | In progress |
| 2 | Wrench, destruction, scrap collection | Not started |
| 3 | Inventory, selling, credits, upgrades | Not started |
| 4 | Missions, UI, save/load, art pass, QA package | Not started |
