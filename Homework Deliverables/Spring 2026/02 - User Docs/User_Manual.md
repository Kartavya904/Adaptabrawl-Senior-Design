# Adaptabrawl — User Manual

This manual explains how to use Adaptabrawl: menus, controls, gameplay, and settings.

---

## Table of contents

1. [Main menu and navigation](#main-menu-and-navigation)
2. [Playing a local match](#playing-a-local-match)
3. [Playing online](#playing-online)
4. [Controls](#controls)
5. [Gameplay basics](#gameplay-basics)
6. [In-match HUD and status](#in-match-hud-and-status)
7. [Settings](#settings)
8. [Match results and rematch](#match-results-and-rematch)

---

## Main menu and navigation

When you start the game, you see the **main menu**.

| Option       | What it does                                                                          |
| ------------ | ------------------------------------------------------------------------------------- |
| **Play**     | Opens play options: **Local** (same computer) or **Online** (over the internet).      |
| **Online**   | Goes directly to the online lobby (create or join a room).                            |
| **Settings** | Opens audio, video, and accessibility options.                                        |
| **Quit**     | Exits the game (in a built version). In the Unity Editor, it may only stop Play mode. |

**Local** takes you to **Character Select** so both players can pick fighters on one machine.  
**Online** takes you to the **Lobby** so you can create a room or join with a room code.

---

## Playing a local match

Local play is for two players on the same computer (couch versus).

1. From the main menu, choose **Play** → **Local**.
2. You enter **Character Select**.
3. **Player 1** and **Player 2** each:
   - Use the controls shown on screen to move **left/right** through the fighter list.
   - **Confirm** to select a fighter, then set yourself as **Ready**.
4. When **both** players are ready, the **Start** (or equivalent) option becomes available; use it to begin the match.
5. The **Game** scene loads. Fight until the match is over.
6. On the **Match Results** screen you can **Rematch**, go to **Character Select**, or **Main Menu**.

---

## Playing online

Online play uses **room codes**: one player creates a room and gets a 6-character code; the other types it in to join.

### Creating a room (host)

1. From the main menu, choose **Online** (or **Play** → **Online**).
2. Select **Create Room**.
3. A **6-character room code** appears on screen.
4. Share this code with your friend (e.g. by voice, chat, or message).
5. Wait for the other player to join.
6. When both players are in the lobby, each selects **Ready**.
7. When **both** are ready, the match starts (you will go to Character Select, then to the game).

### Joining a room (client)

1. From the main menu, choose **Online** (or **Play** → **Online**).
2. Select **Join Room**.
3. Enter the **exact 6-character code** your friend gave you (check for typos and similar-looking characters).
4. Confirm join. If the code is valid, you enter the same lobby.
5. Both players set **Ready**; when both are ready, the match starts.

**Tips:**

- Codes are usually letters and numbers; enter them exactly.
- If join fails, confirm the code and that the host’s room is still open.

---

## Controls

Adaptabrawl supports **keyboard** and **gamepad**. Default layouts are below.  
(If your build uses different bindings, the in-game menus or options may show the actual keys.)

### Player 1 (keyboard)

| Action                            | Key         |
| --------------------------------- | ----------- |
| Move left                         | Left Arrow  |
| Move right                        | Right Arrow |
| Jump / Up                         | Up Arrow    |
| Crouch / Down                     | Down Arrow  |
| Light attack                      | **F**       |
| Heavy attack                      | **G**       |
| Block                             | **R**       |
| Dodge / Parry (context-dependent) | **T**       |

### Player 2 (keyboard – numpad)

| Action        | Key                      |
| ------------- | ------------------------ |
| Move left     | Num 4                    |
| Move right    | Num 6                    |
| Jump / Up     | Num 8                    |
| Crouch / Down | Num 2                    |
| Light attack  | Num 7 (or as configured) |
| Heavy attack  | Num 9 (or as configured) |
| Block         | Num 1 (or as configured) |
| Dodge / Parry | Num 3 (or as configured) |

### Gamepad

The game supports **gamepad** for one or both players. Button layout may follow a standard (e.g. face buttons for attack, shoulder for block). Check the **Settings** or in-game prompts for your build’s mapping.

### Menu controls

- **Confirm / Select:** Enter, Space, or the usual confirm button.
- **Back / Cancel:** Escape or back button.
- **Pause (during match):** Escape (or the assigned pause key).

---

## Gameplay basics

### Goal

Reduce your opponent’s **health** to zero to win a **round**. Win enough rounds (e.g. 2 out of 3) to win the **match**.

### Core actions

| Term             | Meaning                                                                                                                   |
| ---------------- | ------------------------------------------------------------------------------------------------------------------------- |
| **Light attack** | Faster, lower damage; good for combos and pressure.                                                                       |
| **Heavy attack** | Slower, higher damage; can have armor (take a hit without flinching) during part of the move.                             |
| **Block**        | Hold block (and face the attacker) to reduce or negate damage; you may receive **blockstun** (brief lock).                |
| **Parry**        | A well-timed input that counters an incoming attack and can give you a **counter window** to punish.                      |
| **Dodge**        | Short movement with **invincibility frames** so you avoid attacks; usually has a **cooldown** before you can dodge again. |

### Movement

- **Walk** left/right on the ground.
- **Jump** for vertical and air movement; air control is often more limited than on the ground.
- **Dash** (if available) for a quick horizontal move.

### Status effects and conditions

During a match you may see:

- **Status effects** (e.g. **Poison** — damage over time; **Stagger**; **Low HP** state) shown as **icons and timers** on or near the health bar.
- **Adaptive conditions** (e.g. stage or “weather” modifiers) that change rules or stats in a **disclosed** way (e.g. banners or tooltips).

The game is designed so these effects are **readable**: you can see what is active and for how long.

### Fair play and readability

- Modifiers that affect damage or movement are intended to be visible (e.g. UI banners, tooltips).
- Use the HUD and status icons to plan around effects and conditions.

---

## In-match HUD and status

During a fight the **HUD** (heads-up display) shows:

| Element              | Meaning                                                                  |
| -------------------- | ------------------------------------------------------------------------ |
| **Health bars**      | Current health for Player 1 and Player 2.                                |
| **Status icons**     | Active status effects (e.g. poison, stagger) with timers or stack count. |
| **Round timer**      | Time left in the round (if rounds are timed).                            |
| **Condition banner** | When an adaptive condition is active (e.g. stage/weather effect).        |

### Pause menu

Press **Escape** (or the configured pause key) during a match to open the **pause menu**:

- **Resume** — Continue the match.
- **Settings** — Adjust audio, video, and accessibility; you return to the paused match when you exit settings.
- **Main Menu** — Quit the match and go to the main menu.
- **Quit** — Exit the game (in a build).

---

## Settings

Access **Settings** from the main menu or from the pause menu during a match.  
Settings are **saved** and persist between sessions.

### Audio

| Option            | Description                             |
| ----------------- | --------------------------------------- |
| **Master volume** | Overall volume.                         |
| **Music volume**  | Background music level.                 |
| **SFX volume**    | Sound effects (hits, blocks, UI, etc.). |

### Video

| Option         | Description                                                         |
| -------------- | ------------------------------------------------------------------- |
| **Quality**    | Overall graphics quality (e.g. Low / Medium / High).                |
| **Resolution** | Screen resolution.                                                  |
| **VSync**      | Syncs frame rate to monitor refresh; can reduce tearing or cap FPS. |
| **Target FPS** | Cap for frame rate (e.g. 30, 60, 120, 144, or Unlimited).           |

---

## Match results and rematch

When a match ends, the **Match Results** screen shows:

- **Winner** of the match.
- **Round score** (e.g. 2–1).

### After the match

| Button / Option      | Action                                                                                                                         |
| -------------------- | ------------------------------------------------------------------------------------------------------------------------------ |
| **Rematch**          | Start another match (same mode: local or online). You return to Character Select (or Lobby for online) to pick fighters again. |
| **Character Select** | Go back to character selection without going to the main menu.                                                                 |
| **Main Menu**        | Return to the main menu.                                                                                                       |

For **online** matches, **Rematch** typically returns you to the lobby so you can ready up again with the same opponent.

---

## Summary

- **Main menu:** Play (Local/Online), Online, Settings, Quit.
- **Local:** Play → Local → Character Select → both ready → fight → results → Rematch / Character Select / Main Menu.
- **Online:** Create or join room with 6-character code → both ready → Character Select → fight → results.
- **Fight:** Move, light/heavy attack, block, parry, dodge; use the HUD for health, status, and conditions.
- **Settings:** Audio, video, and accessibility; saved automatically.
- **Pause:** Escape for Resume, Settings, Main Menu, Quit.

For installation and first run, see [Getting Started](Getting_Started.md). For common issues, see the [FAQ](FAQ.md).

---

[← Back to User Documentation](User_Docs.md)
