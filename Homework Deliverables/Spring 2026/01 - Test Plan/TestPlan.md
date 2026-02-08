# Adaptabrawl — Test Plan (Assignment #1)

**Course:** Senior Design (Spring 2026)  
**Project:** Adaptabrawl — 2D Multiplayer Fighting Game  
**Repository:** [Adaptabrawl-Senior-Design](https://github.com/Kartavya904/Adaptabrawl-Senior-Design)

---

## Part I. Description of Overall Test Plan

Our testing strategy for Adaptabrawl is organized in two layers. 

**First**, we test individual systems in isolation using the Unity Test Framework (Edit Mode and Play Mode tests) and targeted manual verification. Unit tests focus on combat logic (CombatFSM, hit/hurtbox resolution, damage and knockback), movement (ground detection, jump/dash, friction), status and adaptive-condition application, and data loading from ScriptableObjects (FighterDef, MoveDef, StatusDef, ConditionDef). These tests use simulated or mock inputs and cover normal, boundary, and abnormal cases so that each module behaves correctly before integration.

**Second**, we perform integration and end-to-end testing to validate flows that span multiple systems. Integration tests cover the full match lifecycle (main menu → character select → game scene → match results → rematch), lobby create/join with room codes and ready states, settings persistence (audio, video, accessibility), and HUD updates driven by gameplay events (health, status icons, timers). Where applicable, we use blackbox tests against the requirements (e.g., “block reduces damage,” “parry grants counter window”) and whitebox tests where implementation knowledge helps achieve coverage (e.g., frame data transitions, cancel windows). Manual playtests supplement automated tests for feel, readability, and control responsiveness.

**Finally**, we include performance and stress checks to meet our targets: sustained 60 FPS on mid-range hardware, stable behavior under typical match load (two fighters, hitboxes, status effects, VFX), and—when online is enabled—acceptable behavior under constrained network conditions (e.g., ~120 ms RTT, limited packet loss). Test artifacts (scripts, playtest logs, and this plan) are version-controlled so the suite remains repeatable and can be extended as new features are added.

---

## Part II. Test Case Descriptions

### Combat & Damage

| Field | Content |
|-------|--------|
| **1. Test Case Identifier** | TC-CM01 |
| **2. Purpose of Test** | Verify that a light attack executes correctly and applies damage when hitbox overlaps hurtbox. |
| **3. Description of Test** | Trigger a light attack input for a fighter with valid MoveDef; advance frames so the move’s active window is active; ensure the hitbox is enabled and that when it overlaps the opponent’s hurtbox, damage is applied per MoveDef and the victim’s health decreases. |
| **4. Inputs** | Fighter with light attack MoveDef (startup/active/recovery frames, damage value); attack input; opponent with active hurtbox at overlap position. |
| **5. Expected Outputs/Results** | Hitbox activates during active frames; damage is applied once per hit; victim HP decreases by the move’s damage (after modifiers); hitstop/feedback occurs as designed. |
| **6. Normal/Abnormal/Boundary** | Normal |
| **7. Blackbox/Whitebox** | Blackbox |
| **8. Functional/Performance** | Functional |
| **9. Unit/Integration** | Unit |

---

| Field | Content |
|-------|--------|
| **1. Test Case Identifier** | TC-CM02 |
| **2. Purpose of Test** | Verify that blocking reduces or negates damage according to block rules. |
| **3. Description of Test** | Attacker performs a light or heavy attack; defender is in block state (holding block input) and facing the attacker; confirm hit is registered as blocked and damage is reduced/zeroed per design. |
| **4. Inputs** | Attacker attack input; defender block input and correct facing; overlapping hitbox and hurtbox during attack active frames. |
| **5. Expected Outputs/Results** | Block is detected; damage to defender is reduced or zero; blockstun is applied; block VFX/feedback plays. |
| **6. Normal/Abnormal/Boundary** | Normal |
| **7. Blackbox/Whitebox** | Blackbox |
| **8. Functional/Performance** | Functional |
| **9. Unit/Integration** | Integration |

---

| Field | Content |
|-------|--------|
| **1. Test Case Identifier** | TC-CM03 |
| **2. Purpose of Test** | Verify parry detection and counter window when defender parries at the correct timing. |
| **3. Description of Test** | Attacker performs an attack; defender inputs parry within the parry window before impact; verify parry is recognized and a counter opportunity (e.g., frame advantage or special state) is granted. |
| **4. Inputs** | Attack input; parry input at valid timing; overlapping hitbox/hurtbox. |
| **5. Expected Outputs/Results** | Parry succeeds; attacker is in recovery or vulnerable state; defender can perform a counter move within the counter window; parry VFX/feedback plays. |
| **6. Normal/Abnormal/Boundary** | Normal |
| **7. Blackbox/Whitebox** | Blackbox |
| **8. Functional/Performance** | Functional |
| **9. Unit/Integration** | Integration |

---

| Field | Content |
|-------|--------|
| **1. Test Case Identifier** | TC-CM04 |
| **2. Purpose of Test** | Verify that damage calculation respects status and condition modifiers. |
| **3. Description of Test** | Apply a status effect or adaptive condition that modifies damage (e.g., low-HP bonus, poison, or a condition that increases damage taken); perform a hit and compare actual damage to expected modified value. |
| **4. Inputs** | Fighter with active status/condition that modifies damage; standard attack with known base damage; victim with optional modifiers. |
| **5. Expected Outputs/Results** | Final damage equals the expected value after applying all modifiers; status/condition UI reflects active effects. |
| **6. Normal/Abnormal/Boundary** | Normal |
| **7. Blackbox/Whitebox** | Whitebox |
| **8. Functional/Performance** | Functional |
| **9. Unit/Integration** | Unit |

---

| Field | Content |
|-------|--------|
| **1. Test Case Identifier** | TC-CM05 |
| **2. Purpose of Test** | Verify round end when a fighter’s HP reaches zero. |
| **3. Description of Test** | In a match, deal damage to one fighter until HP reaches 0 (or the defined “KO” threshold); confirm the round is declared over and the correct winner is assigned. |
| **4. Inputs** | Two fighters in an active round; sufficient damage to reduce one fighter’s HP to zero. |
| **5. Expected Outputs/Results** | Round ends; winner is the fighter with HP > 0; GameManager/match state advances (e.g., round count, match end or next round). |
| **6. Normal/Abnormal/Boundary** | Boundary |
| **7. Blackbox/Whitebox** | Blackbox |
| **8. Functional/Performance** | Functional |
| **9. Unit/Integration** | Integration |

---

### Movement & Evasion

| Field | Content |
|-------|--------|
| **1. Test Case Identifier** | TC-MV01 |
| **2. Purpose of Test** | Verify ground movement and facing change from horizontal input. |
| **3. Description of Test** | Apply horizontal movement input to a grounded fighter; verify position changes, speed respects movement stats, and facing flips when changing direction. |
| **4. Inputs** | Horizontal axis input (left/right); fighter on ground with MovementController and FighterDef movement stats. |
| **5. Expected Outputs/Results** | Fighter moves left/right at correct speed; facing direction updates to match input; no movement when input is zero (or friction brings to stop). |
| **6. Normal/Abnormal/Boundary** | Normal |
| **7. Blackbox/Whitebox** | Blackbox |
| **8. Functional/Performance** | Functional |
| **9. Unit/Integration** | Unit |

---

| Field | Content |
|-------|--------|
| **1. Test Case Identifier** | TC-MV02 |
| **2. Purpose of Test** | Verify dodge grants invincibility and cooldown is enforced. |
| **3. Description of Test** | Input dodge; during dodge frames, trigger an attack that would hit the fighter; verify no damage is taken. Then verify a second dodge cannot be performed until cooldown has elapsed. |
| **4. Inputs** | Dodge input; attack overlapping fighter during dodge invincibility window; repeated dodge input before cooldown ends. |
| **5. Expected Outputs/Results** | No damage during invincibility frames; second dodge is ignored or delayed until cooldown expires. |
| **6. Normal/Abnormal/Boundary** | Normal |
| **7. Blackbox/Whitebox** | Blackbox |
| **8. Functional/Performance** | Functional |
| **9. Unit/Integration** | Unit |

---

| Field | Content |
|-------|--------|
| **1. Test Case Identifier** | TC-MV03 |
| **2. Purpose of Test** | Verify jump and air state: can only jump when grounded; air control is applied in air. |
| **3. Description of Test** | On ground, input jump; verify fighter leaves ground and enters air state. In air, apply horizontal input and verify reduced air control per design. Input jump again while airborne and verify it is ignored (no double jump unless designed). |
| **4. Inputs** | Jump input when grounded; horizontal input when airborne; jump input when airborne. |
| **5. Expected Outputs/Results** | Single jump when grounded; correct arc and air movement; no second jump (or double jump only if in design); ground detection correctly sets grounded vs airborne. |
| **6. Normal/Abnormal/Boundary** | Normal |
| **7. Blackbox/Whitebox** | Blackbox |
| **8. Functional/Performance** | Functional |
| **9. Unit/Integration** | Unit |

---

### Status Effects & Adaptive Conditions

| Field | Content |
|-------|--------|
| **1. Test Case Identifier** | TC-ST01 |
| **2. Purpose of Test** | Verify poison status applies damage over time and expires after duration. |
| **3. Description of Test** | Apply poison status to a fighter with defined duration and damage-per-tick; advance game time and verify HP decreases at each tick and status is removed when duration ends. |
| **4. Inputs** | Fighter with poison StatusDef (duration, tick damage); game time advancing. |
| **5. Expected Outputs/Results** | HP decreases at each tick by the defined amount; status icon/timer visible; status removed when duration reaches zero. |
| **6. Normal/Abnormal/Boundary** | Normal |
| **7. Blackbox/Whitebox** | Blackbox |
| **8. Functional/Performance** | Functional |
| **9. Unit/Integration** | Unit |

---

| Field | Content |
|-------|--------|
| **1. Test Case Identifier** | TC-ST02 |
| **2. Purpose of Test** | Verify adaptive condition applies and discloses modifiers correctly. |
| **3. Description of Test** | Activate a match or stage condition (e.g., slippery floor, blood moon) that modifies stats or move properties; perform an affected action and verify the modifier is applied; verify UI shows the condition (banner/tooltip). |
| **4. Inputs** | ConditionDef applied to match/stage; fighter performing move or stat check. |
| **5. Expected Outputs/Results** | Modifiers alter behavior as defined in ConditionDef; condition is visible in UI. |
| **6. Normal/Abnormal/Boundary** | Normal |
| **7. Blackbox/Whitebox** | Blackbox |
| **8. Functional/Performance** | Functional |
| **9. Unit/Integration** | Integration |

---

### UI & Menus

| Field | Content |
|-------|--------|
| **1. Test Case Identifier** | TC-UI01 |
| **2. Purpose of Test** | Verify main menu navigation: Play, Online, Settings, Quit. |
| **3. Description of Test** | From StartScene, select each main menu option and verify correct scene or behavior: Play opens local/online submenu or character select; Online opens lobby flow; Settings opens settings scene; Quit exits application (or does nothing in editor per implementation). |
| **4. Inputs** | User selection on main menu (Play, Online, Settings, Quit). |
| **5. Expected Outputs/Results** | Each option leads to the correct scene or submenu; no unexpected errors or freezes. |
| **6. Normal/Abnormal/Boundary** | Normal |
| **7. Blackbox/Whitebox** | Blackbox |
| **8. Functional/Performance** | Functional |
| **9. Unit/Integration** | Integration |

---

| Field | Content |
|-------|--------|
| **1. Test Case Identifier** | TC-UI02 |
| **2. Purpose of Test** | Verify character selection persists and both players can select fighters before match start. |
| **3. Description of Test** | Enter character select; Player 1 and Player 2 each choose a fighter and confirm ready; start match and verify the correct FighterDefs are used to spawn fighters in the game scene. |
| **4. Inputs** | Character select navigation and confirm for both players; match start. |
| **5. Expected Outputs/Results** | Selected fighters are stored (e.g., CharacterSelectData); game scene spawns the two selected fighters with correct definitions. |
| **6. Normal/Abnormal/Boundary** | Normal |
| **7. Blackbox/Whitebox** | Blackbox |
| **8. Functional/Performance** | Functional |
| **9. Unit/Integration** | Integration |

---

| Field | Content |
|-------|--------|
| **1. Test Case Identifier** | TC-UI03 |
| **2. Purpose of Test** | Verify HUD shows health and status for both fighters and updates in real time. |
| **3. Description of Test** | During a match, deal damage and apply status effects; verify HUD health bars decrease and status icons/timers appear and update each frame. |
| **4. Inputs** | Damage and status application during live match; HUDManager and StatusIcon components active. |
| **5. Expected Outputs/Results** | Health bars reflect current HP; status icons and timers match active effects and durations. |
| **6. Normal/Abnormal/Boundary** | Normal |
| **7. Blackbox/Whitebox** | Blackbox |
| **8. Functional/Performance** | Functional |
| **9. Unit/Integration** | Integration |

---

| Field | Content |
|-------|--------|
| **1. Test Case Identifier** | TC-UI04 |
| **2. Purpose of Test** | Verify pause menu pauses game and allows Resume, Settings, Main Menu, Quit. |
| **3. Description of Test** | During gameplay, press pause (e.g., Escape); verify time scale stops or game is paused; open Settings and return; then test Resume, Main Menu, and Quit options. |
| **4. Inputs** | Pause key; menu choices (Resume, Settings, Main Menu, Quit). |
| **5. Expected Outputs/Results** | Game pauses; all menu options work; Resume restores gameplay; Main Menu and Quit lead to correct scene or exit. |
| **6. Normal/Abnormal/Boundary** | Normal |
| **7. Blackbox/Whitebox** | Blackbox |
| **8. Functional/Performance** | Functional |
| **9. Unit/Integration** | Integration |

---

| Field | Content |
|-------|--------|
| **1. Test Case Identifier** | TC-UI05 |
| **2. Purpose of Test** | Verify match results screen shows winner, score, and rematch/main menu/character select options. |
| **3. Description of Test** | Complete a match (one fighter wins required rounds); verify transition to MatchResults scene; verify winner, round score, and options (Rematch, Main Menu, Character Select) work. |
| **4. Inputs** | Completed match with known winner and round count; user choice on results screen. |
| **5. Expected Outputs/Results** | MatchResults displays correct winner and score; Rematch returns to character select or lobby as designed; Main Menu and Character Select navigate correctly. |
| **6. Normal/Abnormal/Boundary** | Normal |
| **7. Blackbox/Whitebox** | Blackbox |
| **8. Functional/Performance** | Functional |
| **9. Unit/Integration** | Integration |

---

### Lobby & Session

| Field | Content |
|-------|--------|
| **1. Test Case Identifier** | TC-LO01 |
| **2. Purpose of Test** | Verify host can create a room and receive a 6-character room code. |
| **3. Description of Test** | From main menu, choose Online and create room; verify a room code is generated and displayed (e.g., 6 alphanumeric characters) and room state is “waiting for player.” |
| **4. Inputs** | Create-room action from LobbyUI/LobbyManager. |
| **5. Expected Outputs/Results** | Room is created; 6-character code is shown; host can wait for join. |
| **6. Normal/Abnormal/Boundary** | Normal |
| **7. Blackbox/Whitebox** | Blackbox |
| **8. Functional/Performance** | Functional |
| **9. Unit/Integration** | Integration |

---

| Field | Content |
|-------|--------|
| **1. Test Case Identifier** | TC-LO02 |
| **2. Purpose of Test** | Verify client can join with valid room code and both players can set ready to start match. |
| **3. Description of Test** | Host creates room; second client joins using displayed room code; both set ready; verify match starts (e.g., transition to character select or game scene per design). |
| **4. Inputs** | Valid 6-character room code; ready inputs from both players. |
| **5. Expected Outputs/Results** | Client joins correct room; ready states update; match starts when both are ready. |
| **6. Normal/Abnormal/Boundary** | Normal |
| **7. Blackbox/Whitebox** | Blackbox |
| **8. Functional/Performance** | Functional |
| **9. Unit/Integration** | Integration |

---

| Field | Content |
|-------|--------|
| **1. Test Case Identifier** | TC-LO03 |
| **2. Purpose of Test** | Verify invalid or expired room code is rejected with clear feedback. |
| **3. Description of Test** | Attempt to join with an invalid code (wrong length, wrong characters, or non-existent room); verify join fails and user sees an error message or clear feedback. |
| **4. Inputs** | Invalid or non-existent room code. |
| **5. Expected Outputs/Results** | Join fails; error message or state indicates invalid code; no crash or undefined behavior. |
| **6. Normal/Abnormal/Boundary** | Abnormal |
| **7. Blackbox/Whitebox** | Blackbox |
| **8. Functional/Performance** | Functional |
| **9. Unit/Integration** | Integration |

---

### Settings & Persistence

| Field | Content |
|-------|--------|
| **1. Test Case Identifier** | TC-SE01 |
| **2. Purpose of Test** | Verify audio and video settings persist after restart. |
| **3. Description of Test** | Change master/music/SFX volume and a video option (e.g., quality, VSync, target FPS); exit or restart application; reopen and verify settings match the last saved values. |
| **4. Inputs** | New values for audio and video settings; application restart. |
| **5. Expected Outputs/Results** | Settings are saved (e.g., PlayerPrefs); after restart, UI and behavior reflect saved values. |
| **6. Normal/Abnormal/Boundary** | Normal |
| **7. Blackbox/Whitebox** | Blackbox |
| **8. Functional/Performance** | Functional |
| **9. Unit/Integration** | Integration |

---

### Data & Combat FSM

| Field | Content |
|-------|--------|
| **1. Test Case Identifier** | TC-DT01 |
| **2. Purpose of Test** | Verify CombatFSM transitions through startup → active → recovery in sync with frame data. |
| **3. Description of Test** | Trigger an attack with known startup/active/recovery frame counts; advance frame-by-frame and verify state changes at the correct frame indices; verify hitbox is only active during active frames. |
| **4. Inputs** | MoveDef with defined startup, active, recovery; attack input; frame advancement. |
| **5. Expected Outputs/Results** | FSM is in startup for N startup frames, active for M active frames, recovery for R recovery frames; hitbox enabled only during active; then return to idle or next state. |
| **6. Normal/Abnormal/Boundary** | Normal |
| **7. Blackbox/Whitebox** | Whitebox |
| **8. Functional/Performance** | Functional |
| **9. Unit/Integration** | Unit |

---

| Field | Content |
|-------|--------|
| **1. Test Case Identifier** | TC-DT02 |
| **2. Purpose of Test** | Verify FighterFactory creates fighters from valid FighterDef without error. |
| **3. Description of Test** | Call FighterFactory (or equivalent) with two valid FighterDef ScriptableObject references; verify two fighter instances are created with correct components and data (movement, combat, moveset). |
| **4. Inputs** | Two valid FighterDef assets. |
| **5. Expected Outputs/Results** | Two fighter GameObjects (or entities) with correct stats and move sets; no null references or missing components. |
| **6. Normal/Abnormal/Boundary** | Normal |
| **7. Blackbox/Whitebox** | Whitebox |
| **8. Functional/Performance** | Functional |
| **9. Unit/Integration** | Unit |

---

### Performance

| Field | Content |
|-------|--------|
| **1. Test Case Identifier** | TC-PF01 |
| **2. Purpose of Test** | Verify game maintains target frame rate (e.g., 60 FPS) during typical match. |
| **3. Description of Test** | Run a match with two fighters, continuous movement and attacks, status effects and VFX enabled; measure frame rate over a sustained period (e.g., 60 seconds) on a mid-range target machine. |
| **4. Inputs** | Full match scene; two fighters with moves and status effects; target hardware profile. |
| **5. Expected Outputs/Results** | Average frame rate at or above target (e.g., 60 FPS); no sustained drops below acceptable minimum (e.g., 50 FPS) under normal play. |
| **6. Normal/Abnormal/Boundary** | Normal |
| **7. Blackbox/Whitebox** | Blackbox |
| **8. Functional/Performance** | Performance |
| **9. Unit/Integration** | Integration |

---

## Part III. Test Case Matrix

| Test Case ID | Normal/Abnormal/Boundary | Blackbox/Whitebox | Functional/Performance | Unit/Integration |
|--------------|-------------------------|-------------------|------------------------|------------------|
| TC-CM01      | Normal                  | Blackbox          | Functional             | Unit             |
| TC-CM02      | Normal                  | Blackbox          | Functional             | Integration      |
| TC-CM03      | Normal                  | Blackbox          | Functional             | Integration      |
| TC-CM04      | Normal                  | Whitebox          | Functional             | Unit             |
| TC-CM05      | Boundary                | Blackbox          | Functional             | Integration      |
| TC-MV01      | Normal                  | Blackbox          | Functional             | Unit             |
| TC-MV02      | Normal                  | Blackbox          | Functional             | Unit             |
| TC-MV03      | Normal                  | Blackbox          | Functional             | Unit             |
| TC-ST01      | Normal                  | Blackbox          | Functional             | Unit             |
| TC-ST02      | Normal                  | Blackbox          | Functional             | Integration      |
| TC-UI01      | Normal                  | Blackbox          | Functional             | Integration      |
| TC-UI02      | Normal                  | Blackbox          | Functional             | Integration      |
| TC-UI03      | Normal                  | Blackbox          | Functional             | Integration      |
| TC-UI04      | Normal                  | Blackbox          | Functional             | Integration      |
| TC-UI05      | Normal                  | Blackbox          | Functional             | Integration      |
| TC-LO01      | Normal                  | Blackbox          | Functional             | Integration      |
| TC-LO02      | Normal                  | Blackbox          | Functional             | Integration      |
| TC-LO03      | Abnormal                | Blackbox          | Functional             | Integration      |
| TC-SE01      | Normal                  | Blackbox          | Functional             | Integration      |
| TC-DT01      | Normal                  | Whitebox          | Functional             | Unit             |
| TC-DT02      | Normal                  | Whitebox          | Functional             | Unit             |
| TC-PF01      | Normal                  | Blackbox          | Performance            | Integration      |

---

## Terminology (from course notes)

- **Normal:** Testing with expected inputs in normal operating conditions.
- **Abnormal:** Testing with exceptional inputs or conditions.
- **Boundary:** Testing focused on subdivisions of an input domain (e.g., at limits).
- **Blackbox:** Testing based on the requirements specification.
- **Whitebox:** Testing based on knowledge of the implementation.
- **Functional:** Testing based on expected features.
- **Performance:** Testing based on speed, resource use, or other performance criteria.
- **Unit:** Testing of individual components or modules.
- **Integration:** Testing of interfaces between components and end-to-end flows.

---

*Document version: 1.0 | Spring 2026 — Adaptabrawl Team*
