# Adaptabrawl – Project Plan (Two‑Semester) PS: This is all ChatGPT after giving the Project Description

_A 2D multiplayer fighter where attack, defense, and evasion coexist—and adapt._

## 0) Summary
**Goal:** Ship a polished vertical slice (S1) and an expanded, play‑tested online build (S2) of a 2D fighter that blends attack/defense/evasion, with dynamic match conditions and readable status effects (e.g., poisoned, staggered/punched, heavy‑attack windup, low‑HP state).

**Primary engine:** Unity (LTS). **Fallback:** Godot 4 (GDScript). Choice is locked at end of Week 2.

**Players/modes (initial):** Local 1v1; Online 1v1. **Stretch:** Training mode, 2v2, FFA, “Condition Trials.”

---

## 1) Player Experience Pillars
1. **Adaptive Combat** – Conditions (stage/weather/match modifiers + opponent state) alter stats and move properties in coherent, learnable ways.
2. **Responsiveness** – 60 FPS target, tight input buffering, fast recovery out of neutral.
3. **Readability** – Telegraphs, hit/hurtbox clarity, explicit status icons/timers.
4. **Fair Netplay** – Predictable latency behavior; desync‑safe architecture; clear rematch flow.

---

## 2) Scope & Milestones
### Semester 1 (S1): Systems & Online Foundations (Weeks 1–15)
**Target outcome:** A stable online 1v1 vertical slice with 2 fighters, 1 stage, adaptive conditions v1, status effects v1, and a simple lobby/room code system.

- **W1: Project bootstrap**
  - Decide engine candidate list; pin Unity LTS version; enable Git LFS; repo scaffolding.
  - CI: build check (Editor play mode tests + lint).
  - Coding standards: C# analyzers, Conventional Commits.

- **W2: Engine finalization & architecture**
  - Spike: Unity Input System vs. Godot input; character controller prototypes.
  - LOCK engine. Create architecture sketch (see §4).

- **W3–W4: Core combat loop (offline)**
  - Movement, jump, dash/dodge, block/parry; stamina/cooldown if used.
  - Combat FSM: idle → startup → active → recovery; cancel windows.
  - Hit/hurtboxes, damage/heavy‑hit (stagger/punched) states.

- **W5: Status effects v1**
  - Poison (DoT), heavy‑attack state (armor, slow), low‑HP state (UI + modifier hooks).
  - Status UI: icons + stack/timer rendering.

- **W6: Adaptive conditions v1**
  - Stage/weather/match modifier system that maps named conditions → stat/move modifiers.
  - Rule examples: “Slippery floor” (friction ↓), “Thick fog” (projectile speed ↓), “Blood moon” (heavies armor ↑, startup ↑).

- **W7–W9: Netcode baseline + lobbies**
  - Choose stack: **Unity** → NGO/Fish‑Net/Mirror + Unity Transport + (Relay/Lobby) service.
  - Authority: host‑authoritative, client prediction + server reconciliation; lag compensation for hitscan/projectiles.
  - Implement room codes; host → invite → join; ready → start match.

- **W10: Online combat parity**
  - Sync movement, states, hit events, status effects; rollback‑like corrections limited to positions/hits (if feasible).
  - Determinism audit for gameplay‑critical paths.

- **W11: Performance & tooling**
  - 60 FPS on midrange laptop; GC spikes < 2 ms/frame; network RTT budget ≤ 120 ms.
  - In‑game netgraph overlay (RTT, jitter, loss, resim count).

- **W12–W13: Usability & polish**
  - Input remap, controller support; pause; rematch flow; basic accessibility (color‑safe UI, font size).

- **W14: Stability freeze & bug bash**
  - Crash‑free sessions ≥ 30 min; reconnect from transient loss; log capture.

- **W15: S1 Vertical Slice**
  - Deliverables: trailer GIF, gameplay doc, playtest report, build + README.

### Semester 2 (S2): Content, Balance, Online Hardening (Weeks 1–15)
**Target outcome:** 3–4 fighters, 2–3 stages, adaptive conditions v2, balance pass, better online (QoS, platform builds), basic progression.

- **W1–W2:** Adaptive v2 – richer rule graph; UI disclosure (why stats changed).
- **W3–W5:** Fighters 3 & 4; unique adaptation hooks; introductions/tutorial moveset trials.
- **W6–W7:** Stages 2 & 3; Condition Trials single‑player mode.
- **W8–W9:** Online hardening – NAT traversal via Relay; region selection; QoS buckets.
- **W10–W11:** Balance & data – telemetry for move usage/win rates; tuning pipeline.
- **W12–W13:** Cosmetics & UX – profile names, simple unlocks (non‑monetized), sound/VFX polish.
- **W14–W15:** Release candidate; showcase build; documentation.

---

## 3) Success Criteria & KPIs
- **Frame rate:** 60 FPS sustained in match on target hardware.
- **Netplay:** Median RTT ≤ 80 ms (campus LAN/Wi‑Fi), acceptable up to 150 ms with prediction; disconnect rate < 3%/match.
- **Stability:** 0 crashes in 50 consecutive online matches during test day.
- **Readability:** 90% of testers correctly identify current opponent state (poisoned/stagger/heavy/low‑HP) in 3‑sec quiz after play.
- **Balance proxy:** No single move > 35% usage across all rounds at similar skill bracket.

---

## 4) Technical Architecture (Unity‑first)
### 4.1 High‑Level Modules
- **Gameplay:** CharacterController2D, CombatFSM, Hitbox/Hurtbox, DamageResolver, StatusSystem, ConditionSystem, CameraRig.
- **Networking:** NetSession, NetPlayer, SnapshotSync, LagCompensator, Lobby/Relay Client.
- **UI:** HUD (HP bars, status icons/timers), Lobby UI, Netgraph, Settings.
- **Content:** ScriptableObjects for Fighters, Moves, Conditions, Statuses.
- **Tooling:** Debug overlay, Replay capture (ring buffer), Data logging.

### 4.2 Authority & Sync Model
- **Host‑authoritative** server (can be a player‑host). Clients send inputs; server simulates and owns truth for positions/states.
- **Client prediction** for movement; **server reconciliation** to correct drift.
- **Lag compensation**: record past collider states on server for hit validation using RTT timestamp.
- **Snapshots**: 20–30 Hz state replication; client interpolates/extrapolates with small buffers.

### 4.3 Data & Extensibility
- **Fighters:** `FighterDef.asset` (speed, weight, base stats, move set, adaptation hooks).
- **Moves:** `MoveDef.asset` (startup/active/recovery, damage, knockback, armor frames, cancel rules).
- **Statuses:** `StatusDef.asset` (type, stacks, tick rate, visuals, onApply/onTick/onRemove).
- **Conditions:** `ConditionDef.asset` with `Modifier[]` (property path + op + value) and optional triggers.

### 4.4 State Machines
- Combat FSM per fighter; nested sub‑states for hurt/poisoned/heavy; exit conditions; network‑safe triggers.
- Event bus to broadcast state changes to UI/VFX and networking.

### 4.5 Input & Controls
- Unity Input System; device‑agnostic mappings; input buffering window (e.g., 3–5 frames).

---

## 5) Networking Stack Options
**Unity preferred:**
- **NGO (Netcode for GameObjects) + Unity Transport + Relay/Lobby** – tight editor integration, adequate docs.
- **Fish‑Networking** – performant alternative with rollback‑friendly patterns.
- **Mirror** – mature, large community; self‑host friendly.

**Godot fallback:**
- **ENet‑based high‑level multiplayer**; custom prediction/reconciliation.

**Recommendation:** Start with NGO + Relay/Lobby for S1; switch only if blockers appear by W8.

---

## 6) Adaptive Conditions – Design Notes
- **Source types:** Stage environment, timed events, round state (e.g., low‑HP), mutual stance challenges.
- **Modifier examples:**
  - _Slippery Ground:_ friction −20%; dodge invuln window −2 frames; landing lag +1 frame.
  - _Thick Fog:_ projectile speed −25%; melee hitstop +1 frame (clearer reads).
  - _Heavy Gravity:_ jump height −15%; heavy attacks gain 1 armor point.
- **Disclosure:** On‑screen banner at start/change; HUD tooltip; icon next to move list.
- **Fairness:** No hidden modifiers; counters exist for each condition.

---

## 7) Content Plan
- **Fighters (S1):**
  - **Striker:** pressure, frame traps; heavy‑attack stance.
  - **Elusive:** mobility/dodge cancels; counter windows.
- **Fighters (S2 add):** Zoner (space control), Bruiser (armor/slow).
- **Stages:** Training Room (S1), Industrial Rooftops / Misty Forest (S2) with condition triggers.

---

## 8) Build, QA, and Telemetry
- **CI:** GitHub Actions – build on PR; run play mode tests.
- **Bundles:** Win64, macOS (Apple Silicon + Intel if feasible).
- **Testing:**
  - Unit tests for combat math and status timers.
  - Determinism tests (seeded RNG; fixed timestep).
  - Net sim: jitter/loss/latency via transport settings.
- **Telemetry (opt‑in):** Match length, RTT, move usage, crash logs.

---

## 9) Team & Process
- **Roles (rotating every 2–3 weeks):**
  - **Net/Infra Lead:** lobbies, sync, tools, CI.
  - **Combat/Systems Lead:** FSM, moves, statuses, conditions.
  - **UX/Content Lead:** HUD, input, controllers, VFX/SFX, stage layout.
- **Workflow:** Issue‑driven sprints (1–2 weeks); PR reviews; playtest every Friday.
- **Branching:** `main` (stable) ← `dev` ← `feature/*`.
- **Definition of Done:** Feature demo in match, tests passing, docs updated, one external playtest.

---

## 10) Risks & Mitigations
- **Netcode complexity** → Start with host‑auth + prediction; keep state minimal; instrument early.
- **Scope creep** → Hard gate: S1 focuses on 1v1 only; S2 may add 2v2/FFA if KPIs green.
- **Art/content debt** → Use prototype sprites and hitbox gizmos; defer polish until S2.
- **Determinism bugs** → Centralize RNG; fixed delta time; avoid physics nondeterminism for hit logic.
- **Hardware variance** → Performance budgets; scalable VFX; profiling on mid‑tier laptops.

---

## 11) Deliverables Checklist
- **S1 Vertical Slice:**
  - Online 1v1, 2 fighters, 1 stage, adaptive v1, status v1, lobby with room codes.
  - README, quickstart, controls, netgraph overlay.
  - Playtest report + metrics.
- **S2 Showcase:**
  - 3–4 fighters, 2–3 stages, adaptive v2, balance metrics, QoS selection, training mode, basic cosmetics.
  - Release notes, trailer, itch.io/Steam demo page (if desired).

---

## 12) Engine Switch Contingency (Unity → Godot)
- Trigger: unsolved netcode blocker by W8 or licensing constraint.
- Migration plan: export gameplay data to JSON; re‑create FSM and status/condition systems; reuse design docs and assets; maintain tests as specs.

---

## 13) Repository Layout (Unity)
```
Adaptabrawl/
  Assets/
    Scripts/
      Gameplay/ (FSM, Status, Condition, Damage)
      Netcode/ (Session, Sync, LagComp)
      UI/
    Art/Audio/Prefabs/
  Packages/
  ProjectSettings/
  Docs/
    projectplan.md
    design-notes/
```

---

## 14) Versioning & Licensing
- **Versioning:** `YY.MM.minor` (e.g., 25.10.0 for S1 cut).
- **License:** MIT if open‑source; otherwise proprietary (private repo until showcase).

---

## 15) Appendix: Acceptance Tests (Samples)
- **Hit validation under latency:** At 120 ms RTT, heavy attack connects within 1 frame of local hit indicator on both clients.
- **Status timing:** Poison ticks exactly every 0.5 s (±1 frame) for 5 s; removing the status stops ticks immediately.
- **Reconnect:** Client drops for 3 s → rejoins same room → state restored within 2 s.

