# Getting Started with Adaptabrawl

This guide walks you through installing Adaptabrawl and playing your first match.

---

## Table of contents

1. [System requirements](#system-requirements)
2. [Installation](#installation)
3. [Your first match](#your-first-match)
4. [Next steps](#next-steps)

---

## System requirements

Before installing, make sure your system meets the following.

### Supported platforms

- **Windows** (10 or later, 64-bit)
- **macOS** (Intel or Apple Silicon, recent OS version)

### Required software

| Requirement              | Purpose                                                                                           |
| ------------------------ | ------------------------------------------------------------------------------------------------- |
| **Unity Hub**            | Launches and manages the Unity Editor and project. Download from [unity.com](https://unity.com/). |
| **Unity LTS 6000.2.6f2** | The game engine version this project uses. Install via Unity Hub.                                 |
| **Git**                  | Used to download (clone) the project. Install from [git-scm.com](https://git-scm.com/).           |

### Recommended hardware

- **CPU:** Mid-range or better (multi-core)
- **RAM:** 8 GB minimum; 16 GB recommended
- **Graphics:** Integrated or dedicated GPU capable of 60 FPS at 1080p
- **Storage:** ~2 GB free for the project

---

## Installation

Follow these steps to get the project on your computer and open it in Unity.

### Step 1: Install Unity Hub and Unity

1. Download **Unity Hub** from [unity.com/download](https://unity.com/download).
2. Install and open Unity Hub.
3. In Unity Hub, go to **Installs** and add **Unity 6000.2.6f2** (or the LTS version that matches the project).
   - If this exact version is unavailable, use the closest LTS version and the project may prompt to upgrade.
4. Include **Windows Build Support** or **Mac Build Support** if you plan to build a standalone game.

> **Tip:** The first time you open a Unity project, the Editor may take a few minutes to import assets. This is normal.

### Step 2: Get the project (clone the repository)

1. Open a terminal (Command Prompt, PowerShell, or Terminal.app).
2. Go to the folder where you want the project (e.g. `Desktop` or `Documents`).
3. Run:

   ```bash
   git clone https://github.com/Kartavya904/Adaptabrawl-Senior-Design.git
   ```

4. After the clone finishes, you will have a folder named `Adaptabrawl-Senior-Design`.

### Step 3: Open the project in Unity

1. Open **Unity Hub**.
2. Click **Open** (or **Add**).
3. Browse to the cloned folder and select the **Adaptabrawl** folder inside it  
   (the one that contains `Assets`, `Packages`, and `ProjectSettings`).
4. Click **Select Folder**. Unity will load the project (first load can take several minutes).

### Step 4: Run the game in the Editor

1. In Unity, open the **Project** window and go to **Assets → Scenes**.
2. Double-click **StartScene** to open the main menu scene.
3. Click the **Play** button at the top of the Unity Editor (or press **Ctrl+P** / **Cmd+P**).
4. The game will run inside the Editor. You should see the main menu.

To stop the game, click the Play button again.

---

## Your first match

Once the game is running (in the Editor or in a built executable), you can play a local 1v1 match like this.

1. **Main menu** — Choose **Play**, then **Local** (or **Play Local**).
2. **Character select** — Each player picks a fighter using the on-screen prompts:
   - Move left/right to change character.
   - Confirm your choice and press **Ready**.
   - When both players are ready, start the match.
3. **Fight** — Use the [controls](User_Manual.md#controls) to move, attack, block, and dodge.
4. **Round end** — Rounds end when one fighter’s health reaches zero or time runs out.
5. **Match end** — After the required number of round wins, the match ends and the results screen appears. From there you can **Rematch**, go to **Character Select**, or **Main Menu**.

For full control layouts and gameplay details, see the [User Manual](User_Manual.md).

---

## Next steps

| Goal                                  | Where to look                                                 |
| ------------------------------------- | ------------------------------------------------------------- |
| Learn all controls and moves          | [User Manual → Controls & Gameplay](User_Manual.md#controls)  |
| Play online with a friend             | [User Manual → Playing online](User_Manual.md#playing-online) |
| Change audio, video, or accessibility | [User Manual → Settings](User_Manual.md#settings)             |
| Fix issues or find answers            | [FAQ](FAQ.md)                                                 |

---

[← Back to User Documentation](User_Docs.md)
