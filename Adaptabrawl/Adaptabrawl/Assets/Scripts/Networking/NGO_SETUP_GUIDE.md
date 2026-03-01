# Unity Netcode (NGO) - Local Host Setup Guide

The `LobbyManager.cs` script has been updated to use REAL local networking instead of a simulation! This means you can boot up the game twice on your computer and actually play against yourself!

However, because I used official Netcode functions, **Unity currently has a compiler error in your console.** 

To fix the error and test the real network, follow these exact 2 steps:

---

## 1. Install Netcode for GameObjects
Unity requires us to explicitly download the networking package.
1. Open Unity.
2. In the very top toolbar, click **Window** -> **Package Manager**.
3. In the top-left corner of the Package Manager window, change the dropdown from `Packages: In Project` to `Packages: Unity Registry`.
4. In the search bar on the right, type: **Netcode for GameObjects**
5. Select it, and click the **Install** button in the bottom right corner.
*(Wait for Unity to fully finish compiling. Once it's done, your console errors will vanish!)*

---

## 2. Set Up the NetworkManager
NGO requires a single "Master GameObject" to exist in the scene to handle all connections.

1. Open your `StartScene` (Main Menu) in Unity.
2. Right-click your Hierarchy, create an **Empty GameObject**, and name it exactly `NetworkManager`.
3. In the Inspector, click Add Component and search for `NetworkManager`. Attach it.
4. Inside the `NetworkManager` script component in the Inspector:
   - Check the box that says **"Don't Destroy"** (so it carries over into the Lobby and Gameplay scenes).
   - Check the box that says **"Enable Scene Management"** (crucial so the Host can teleport both players to SetupScene).
   - Look for the **"Network Transport"** slot. Click the "Select Transport" button underneath it, and click `UnityTransport`.

---

## 3. How to Test Local Multiplayer!

This requires two instances of the game running side-by-side to test.

1. Open **File** > **Build and Run** (Windows/Mac). This will compile your game and open a standalone window of Adaptabrawl. Let this be **Player 1**.
2. Go back into the Unity Editor, open `StartScene`, and hit **Play**. Let this be **Player 2**.
3. **Player 1 (Host):** Click "Play Online" -> Click "Create Room". (You literally become the server).
4. **Player 2 (Client):** Click "Play Online" -> Click "Join Room" (Since we are local, you don't even need to type a code, the script automatically hooks into your localhost 127.0.0.1).
5. Both players click **Ready**.
6. Boom! Both screens instantly jump into `SetupScene` in perfect sync. You are officially doing real multiplayer!
