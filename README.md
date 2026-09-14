# Mars Vanguard

Mars Vanguard is a 3D space-combat game built with Unity. Pilot a combat ship through a space environment, engage AI-controlled enemies, manage ship equipment, and progress through the game's menu, hangar, gameplay, and credits scenes.

## Features

- Space-combat gameplay with player and AI-controlled ships
- Cannon and missile weapon systems
- Enemy difficulty, targeting, rewards, and equipment configuration
- Ship upgrades for weapons, engines, and pilots
- Radar and compass-style target UI
- 3D pathfinding and streamed environment/collider scenes
- Touch joystick and gyroscope controller support for mobile devices
- Unity Ads and rewarded-ad integration
- Universal Render Pipeline with mobile-focused Unity packages

## Requirements

- Unity Hub
- Unity Editor `6000.6.0f1`
- A platform supported by the project's configured Unity modules

The editor version is recorded in `ProjectSettings/ProjectVersion.txt`. Opening the project with another Unity version may trigger an upgrade and could change serialized project files.

## Getting Started

1. Clone the repository:

	 ```bash
	 git clone https://github.com/Shammas-satti-00/Mars-Vanguard-Unity-3D.git
	 ```

2. Open the cloned folder in Unity Hub.
3. Let Unity import the project and regenerate the local `Library` folder.
4. Open `Assets/Scenes/Veeivs.unity` and press Play. This scene loads the main menu automatically.

The scenes configured for the player-facing flow are:

1. `Veeivs.unity` - initial scene and transition to the main menu
2. `MainMenu.unity` - main menu
3. `Hanger.unity` - ship and equipment area
4. `GamePlay.unity` - space-combat gameplay
5. `Credits.unity` - credits

## Controls

The project includes configurable Unity Input System, touch joystick, and gyroscope controllers. The active controls can vary by scene and target platform. For a desktop test, inspect the input actions and controller components in the active scene; for mobile, use the on-screen joystick and device gyro controls where enabled.

## Building

1. Open **File > Build Profiles** in the Unity Editor.
2. Select the target platform.
3. Confirm the enabled scenes from `ProjectSettings/EditorBuildSettings.asset`.
4. Choose **Build** or **Build and Run**.

Build outputs such as APK files are intentionally excluded from this repository. Create a local build when needed.

## Project Structure

```text
Assets/
	Scenes/          Main menu, hangar, gameplay, streamed world, and credits scenes
	Scripts/         Gameplay systems, ship controllers, AI, UI, ads, and utilities
	Prefabs/         Ships, weapons, effects, UI, and reusable game objects
	3D Models/       Environment and ship models
	Materials/       Materials and render assets
	SFX/             Sound effects and music
	Textures/        Textures and UI art
Packages/          Unity package manifest and lock file
ProjectSettings/   Unity project, build, and editor settings
```

## Repository Notes

Unity-generated folders (`Library`, `Temp`, `obj`, `Logs`, and `UserSettings`), IDE files, and local build artifacts are excluded through `.gitignore`. Unity `.meta` files are kept because they preserve asset references between machines.

## Status

This repository contains the Unity project source and assets. No released build is included; use the Unity Editor build process above to create one.
