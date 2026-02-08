# Frequently Asked Questions (FAQ)

Common questions and concerns about installing, playing, and troubleshooting Adaptabrawl.

---

## Table of contents

1. [Installation and setup](#installation-and-setup)
2. [Running the game](#running-the-game)
3. [Controls and input](#controls-and-input)
4. [Local and online play](#local-and-online-play)
5. [Gameplay and mechanics](#gameplay-and-mechanics)
6. [Settings and performance](#settings-and-performance)
7. [Errors and troubleshooting](#errors-and-troubleshooting)

---

## Installation and setup

### What do I need to run Adaptabrawl?

You need **Unity** to run the project (either in the Unity Editor or via a built executable).  
Required: **Unity Hub**, **Unity LTS 6000.2.6f2** (or compatible LTS), and **Git** to clone the repository.  
See [Getting Started → System requirements](Getting_Started.md#system-requirements) for details.

### Can I play without installing Unity?

Only if someone provides you a **built game** (e.g. a Windows `.exe` or Mac app). The repository contains a Unity project, so to run it from source you need the Unity Editor. For a standalone build, use Unity’s **File → Build Settings** to create an executable.

### The project won’t open in Unity Hub. What should I do?

- Open the **Adaptabrawl** folder that is **inside** the cloned repo (the one that contains `Assets`, `Packages`, and `ProjectSettings`). Do not select the root `Adaptabrawl-Senior-Design` folder.
- Ensure your Unity version matches or is compatible with the project (LTS 6000.2.6f2 or as stated in the project).
- If Unity asks to upgrade/downgrade the project, follow the prompt or install the exact version the project was made with.

### Where do I get Unity and Unity Hub?

Download Unity Hub from [unity.com/download](https://unity.com/download). Use Hub to install the Unity Editor version required by the project (e.g. 6000.2.6f2 LTS).

---

## Running the game

### How do I start the game?

**In the Unity Editor:** Open **StartScene** (Assets → Scenes → StartScene), then press the **Play** button at the top.  
**From a build:** Run the built executable; the main menu should appear.

### Why does the first load take so long?

Unity imports and compiles assets the first time you open a project or scene. This can take several minutes. Later loads are usually faster.

---

## Controls and input

### Can I use a gamepad?

Yes. Adaptabrawl supports gamepad; the exact button layout depends on the build. Check in-game prompts or Settings for the mapping. Keyboard (and optionally numpad for Player 2) is also supported.

### Can I change key bindings?

If **input remapping** is implemented, it will be in **Settings**. Otherwise, default bindings are as in the [User Manual → Controls](User_Manual.md#controls). Future versions may add full remapping.

### Player 2’s keys don’t work. What do I do?

- Ensure Player 2 is using the **numpad** (or the keys configured for Player 2).
- If using a gamepad, ensure the game recognizes the second device.
- On some systems, numpad must be enabled (e.g. Num Lock on Windows).

### My controller isn’t detected.

- Plug in the controller before starting the game.
- On Windows, try different USB ports or ensure drivers are installed.
- Unity’s Input System must be configured for your gamepad; if you’re building from source, check the project’s input settings.

---

## Local and online play

### How many players can play locally?

Local mode is **2 players** on one computer (1v1).

### How does online play work?

One player **creates a room** and gets a **6-character room code**. The other player chooses **Join Room** and enters that code. No account or friends list is required. When both are in the lobby and both set **Ready**, the match starts (after character select).

### I can’t join my friend’s room.

- Verify the **exact 6-character code** (no spaces; watch for similar-looking characters like 0/O, 1/I).
- Ensure the host’s game is still in the lobby and the room is open.
- Check that both of you have a working internet connection and that firewalls/NAT don’t block the game’s networking (Mirror/Relay may be used).

### The room code doesn’t work.

- Codes are usually **case-sensitive** and alphanumeric; type carefully.
- Confirm the host hasn’t closed the room or started without you.
- If the game uses a relay service, temporary server issues can prevent joining; try again later.

### Can I play online with random players (matchmaking)?

The current design uses **room codes** for friend vs friend play. There is no public matchmaking; you need to share a code with your opponent.

---

## Gameplay and mechanics

### What’s the difference between light and heavy attack?

**Light attacks** are faster and lower damage, good for combos and pressure. **Heavy attacks** are slower, deal more damage, and may have **armor** (you don’t flinch during part of the move) but are easier to punish if missed.

### How do I block?

Hold the **block** button and **face your opponent**. Block reduces or negates damage but may apply **blockstun**. You cannot block in the back; positioning matters.

### What is parry and when do I use it?

**Parry** is a timing-based counter: you input parry just before an attack hits. On success, the attack is countered and you get a **counter window** to punish. It’s riskier than blocking but rewards good timing.

### What is dodge and why can’t I dodge again?

**Dodge** gives a short burst of movement with **invincibility frames**. It has a **cooldown**; you must wait a few seconds before you can dodge again. The HUD or character state may indicate when dodge is available again.

### What are the icons above the health bar?

They are **status effects** (e.g. Poison, Stagger, Low HP state). Icons and timers show what’s active and for how long. The game is designed so these are readable so you can adapt your play.

### What are “adaptive conditions” or stage/weather effects?

These are **match or stage modifiers** (e.g. slippery floor, fog, “blood moon”) that change stats or rules in a **disclosed** way. They appear as banners or tooltips so you know what is in effect. They are meant to be fair and transparent.

### How do I win a round? A match?

- **Round:** Reduce the opponent’s health to zero before time runs out (or meet the round win condition).
- **Match:** Win the required number of rounds (e.g. 2 out of 3). The match results screen shows the winner and score.

---

## Settings and performance

### Where do I change volume or graphics?

Use **Settings** from the main menu or the **pause menu** during a match. Options include **Audio** (master, music, SFX), **Video** (quality, resolution, VSync, target FPS), and **Accessibility** (UI scale, color blind mode, hitbox display).

### Do my settings save?

Yes. Settings are saved (e.g. via PlayerPrefs) and persist between sessions.

### The game is laggy or stuttering. What can I do?

- Lower **Quality** and **Resolution** in Settings.
- Set **Target FPS** to 60 or 30.
- Enable **VSync** if you see screen tearing; disable it if you want higher FPS and have a high-refresh monitor.
- Close other heavy applications.
- If you’re in the Unity Editor, performance can be lower than in a built executable; try a build to test.

### Can I run the game at 60 FPS?

The game targets 60 FPS on mid-range hardware. Use **Settings → Video** (Target FPS, Quality, Resolution) to adjust. Very low-end hardware may need lower settings.

---

## Errors and troubleshooting

### The game window is black or nothing appears.

- Ensure **StartScene** (or the correct first scene) is in Build Settings at index 0 and that the camera and UI are set up.
- Check the Unity Console (or the build’s log) for errors.
- Update graphics drivers.

### I get an error about “scene not found” or “scene not in build.”

All scenes used in the flow (StartScene, CharacterSelect, LobbyScene, SettingsScene, GameScene, MatchResults) must be added in **File → Build Settings**. Add each scene and ensure the order matches the project’s design (StartScene first).

### Characters don’t appear in character select.

The list of fighters is configured in the Character Select scene (e.g. FighterDef assets). If no fighters appear, the build may not have any fighter definitions assigned. This is a content/setup issue; developers need to assign at least two FighterDefs to the character select UI.

### After the match, the results screen doesn’t show or is wrong.

The game writes match results into a shared data object and then loads the Match Results scene. If results don’t show, the transition or data may be failing. Ensure the Match Results scene is in Build Settings and that the game is updated to the latest version. If you’re a developer, check GameManager and MatchResultsData usage.

### “Quit” in the menu doesn’t exit the game.

In the **Unity Editor**, Quit often only stops Play mode. In a **built executable**, Quit should close the application. If it doesn’t, it may be a platform or build configuration issue.

### Where can I report bugs or get help?

- **Repository:** [Adaptabrawl-Senior-Design](https://github.com/Kartavya904/Adaptabrawl-Senior-Design)
- **Contact:** singhk6@mail.uc.edu

Include what you were doing, what you expected, and any error messages or screenshots.

---

## Quick reference

| Topic        | Link                                                                        |
| ------------ | --------------------------------------------------------------------------- |
| Installation | [Getting Started](Getting_Started.md#installation)                          |
| Controls     | [User Manual → Controls](User_Manual.md#controls)                           |
| Local play   | [User Manual → Playing a local match](User_Manual.md#playing-a-local-match) |
| Online play  | [User Manual → Playing online](User_Manual.md#playing-online)               |
| Settings     | [User Manual → Settings](User_Manual.md#settings)                           |
| Full manual  | [User Manual](User_Manual.md)                                               |

---

[← Back to User Documentation](User_Docs.md)
