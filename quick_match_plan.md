# Quick Match & AI Training — Comprehensive Plan

**Document purpose:** Single source of truth for implementing Quick Match (dedicated setup scene), three difficulty tiers (**Dummy**, **Trainer**, **Extreme**), opponent selection (random by default, name preview), input mode (keyboard vs controller), lightweight map selection, and a **training + model registry** pipeline under `Assets/AI trainings/`.

**Status:** Planning / architecture — not an implementation checklist order-of-operations (those can be derived from phases below).

**Related codebase facts (as of authoring):**

- Local flow uses `LobbyContext`, `CharacterSelectData`, `ArenaSelectData`, and loads `GameScene` via `ArenaSelectUI` / Netcode host pattern.
- `LocalGameManager` spawns two fighters and configures **human** input on both via `PlayerController_Platform.ConfigureForPlayer`.
- Fighters are data-driven (`FighterDef` in `Resources/Fighters/` with ordering rules in `CharacterSelectUI`).
- **No** Unity ML-Agents, Sentis inference, or existing CPU-opponent controller is present in `Packages/manifest.json` or core gameplay scripts — any ML path is **additive**.

---

## 1. Executive summary

### 1.1 What you are building (product)

1. **Main menu:** A **Quick Match** entry (alongside local / online / back).
2. **Quick Match Setup Scene:** A **focused** scene (not the full `SetupScene` pipeline) containing:
   - **Difficulty row:** Three catchy tiers — **Dummy**, **Trainer**, **Extreme** (map to increasing opponent strength).
   - **Input mode:** Explicit **Switch to keyboard** / **Switch to controller** (and document behavior for “touchpad” if that means laptop trackpad mapped as mouse — usually not ideal for fighters; see §7.3).
   - **Map:** Simple text/name selection (not necessarily the polished arena UI from main setup).
   - **Character row:** Mirrors **one** side of existing character-select UX for **the human**; **opponent** shows **Random** by default with optional **cycle** to inspect which opponent name will be used (still random or “random with seed” — see §7.1).
3. **Match:** Load `GameScene` with **P1 = human**, **P2 = AI-controlled** (same combat simulation as today, different input source for P2).
4. **Training assets:** Under `Assets/AI trainings/models/{Dummy,Trainer,Extreme}/` — **model checkpoints** plus **metadata** describing quality, identity, and promotion rules.

### 1.2 Does your proposed training flow “sound good”?

**Yes, conceptually** — the pattern “train → evaluate → compare to recorded best → promote or discard” is standard (similar to checkpoint selection in RL and MLOps).

**Caveats you should treat as first-class requirements:**

| Topic                                            | Why it matters                                                                                                                                                                                                                                             |
| ------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **What “better” means**                          | A single scalar “score” is only valid if the **metric** is aligned with fun and strength. You need a **formal evaluation protocol** (§4).                                                                                                                  |
| **Three separate models vs one model + scaling** | You can train **three policies** (one per folder) **or** one policy with **action noise / reward shaping / curriculum** for lower tiers. Both are valid; three folders still make sense as **deployment slots** even if training shares code.              |
| **“Meta files” naming**                          | In Unity, `*.meta` files are **YAML sidecars for asset import** (GUIDs, import settings). **Do not** overload them for training metrics. Use **`training_metadata.json`** (or similar) **next to** the model file, or a single **registry JSON** per tier. |
| **Repository size**                              | Neural checkpoints are **large**. Use **Git LFS** or keep **default shipped models** small and store heavy experiments elsewhere.                                                                                                                          |
| **Scope for a shipping milestone**               | Full **reinforcement learning** for a fighting game is a **research-scale** effort unless observations/rewards are heavily simplified. A **phased** plan (§6) keeps the project shippable.                                                                 |

---

## 2. Architecture overview

### 2.1 Runtime: three cooperating layers

1. **Match configuration (data):** ScriptableObject or static session object created when leaving Quick Match Setup — includes `QuickMatchConfig` with: difficulty enum, `HumanInputScheme`, `ArenaId` or name, `HumanFighterId`, `OpponentFighterId` (or random seed), `OpponentPolicyId` (which checkpoint slot).
2. **AI policy (inference):** For ML-based policies: a **runner** that turns game state → model input tensor → outputs (move, buttons) at fixed frequency. For heuristic tiers: a **rules brain** with tunable parameters.
3. **Input injection:** Instead of `PlayerController_Platform` reading devices for P2, a **`CpuInputAdapter`** (name illustrative) feeds the **same** logical commands the human pipeline expects (preferred) or calls a thin “command buffer” API if one exists.

**Design principle:** **One combat codebase**; **two input sources** (human device vs policy output). Avoid duplicating movement/combat logic for AI.

### 2.2 Training: two viable approaches (choose explicitly)

#### Approach A — Heuristic / utility AI (fastest path to “playable”)

- **Dummy:** chase player, attack on cooldown, rarely defend.
- **Trainer:** add spacing, occasional block/dodge, simple reactions.
- **Extreme:** aggressive frame-chasing is **hard** without ML; “Extreme” may still be heuristic with **tighter timings** and **better move selection**, not necessarily a larger network.

**Pros:** Fits senior-design timelines; no Python stack; easy to tune by designers.  
**Cons:** Will not generalize like a true RL policy; “Extreme” may feel artificial.

#### Approach B — Unity ML-Agents (or custom RL) (strongest long-term)

- Add **`com.unity.ml-agents`** (training) and typically **`com.unity.ml-agents.extensions`** as needed; training uses **Python** outside the editor or via ML-Agents CLI.
- Implement `Agent` with **observations** (positions, velocities, cooldowns, discrete combat phase if available) and **actions** (discrete or continuous stick + discrete buttons).
- Export trained **behavior** → place under `Assets/AI trainings/models/{tier}/`.

**Pros:** Can discover non-obvious policies; scalable with compute.  
**Cons:** High setup cost; reward design is tricky; long iteration loops; needs **fast headless simulation** for throughput.

#### Recommended **hybrid** strategy for this project

1. **Ship v1** with **heuristics** for all three tiers (or Dummy heuristic + Trainer/Extreme heuristic) so Quick Match is **feature-complete**.
2. **Parallel track:** Build the **observation/action bridge** and **evaluation harness** so ML-Agents can **replace** the policy for Trainer/Extreme later **without** rewriting combat.
3. **Folder layout** stays the same: `Dummy/` might hold **JSON params** only; `Extreme/` might hold **`.onnx`** + metadata when ML lands.

---

## 3. Asset and folder layout

### 3.1 Proposed Unity asset tree

```
Assets/
  AI trainings/
    README.md                    # How to train, evaluate, promote (pointer to this doc)
    models/
      Dummy/
        policy_placeholder.txt   # Optional: document if using heuristics only
        dummy_metadata.json      # Registry for heuristic params + “score”
        (optional) dummy.onnx  # If ML later
      Trainer/
        trainer_metadata.json
        trainer.onnx             # When using ML-Agents exported policy
      Extreme/
        extreme_metadata.json
        extreme.onnx
    Evaluation/                  # Optional: scenes or configs used only for batch eval
      QuickMatchEval.unity
```

**Naming note:** Unity will generate **`*.meta`** for every asset — those are **import sidecars**, not your training metrics. Your **training registry** should be explicit JSON (see §5).

### 3.2 What each tier folder contains

| Tier        | Intended behavior                       | Typical artifact                                                |
| ----------- | --------------------------------------- | --------------------------------------------------------------- |
| **Dummy**   | Clearly weak; teaches basics            | Low reaction speed; predictable patterns; heuristic params JSON |
| **Trainer** | Intermediate; punishes obvious mistakes | Medium timings; maybe first ML checkpoint                       |
| **Extreme** | Demanding; still fair                   | Best available policy; tightest heuristic or strongest ML       |

---

## 4. Training methodology (ML path) — end-to-end flow

This section assumes **Approach B** for training; if you only ship heuristics, skip to §5 for metadata that tracks **tuned constants** instead of neural weights.

### 4.1 Environment (simulation) requirements

1. **Deterministic or seeded runs** when comparing models (same opponent policy, same map, same RNG for item spawns if any).
2. **Fast-forward:** optional **fixed timestep** headless build for batch evaluation.
3. **Episode definition:** e.g. one **round** or **full match**; truncation on timeout.

### 4.2 Observations (vectorized state)

Minimum viable (illustrative — must match your actual `FighterController` / `PlayerController_Platform` capabilities):

- Relative position / distance, facing, grounded, velocity.
- Normalized timers: cooldowns for dash, attack, block, hitstun.
- Health difference, round timer.
- Optional: last action one-hot (for stability).

**Avoid** raw pixels for v1 (much harder).

### 4.3 Actions

Prefer **discrete** buckets for stick direction + discrete buttons (light/heavy/jump/block/special) to reduce exploration space — aligned with how fighting games are played on digital inputs.

### 4.4 Reward shaping (critical)

Poor rewards → degenerate policies (infinite backoff, spam one move). Typical components:

- **Damage dealt** positive, **damage taken** negative.
- Small **living** penalty per step to encourage engagement.
- **Stall** penalty if players are too far for too long.
- Optional: **style** terms (discouraged until basics work).

### 4.5 Training loop (offline)

1. Start ML-Agents **training** with a **curriculum** (optional): first vs Dummy heuristic, then vs self-play snapshot.
2. Periodically run **evaluation** (no exploration): **N** episodes vs fixed opponents (scripted or previous best).
3. Compute **metrics** (§5.2).
4. **Promote** if better than registry threshold; else discard run.

### 4.6 Relationship between the three tiers and training jobs

**Option 1 — Three independent training runs**

- Train `Dummy` with restricted action speed or noisy actions.
- Train `Trainer` with standard rewards.
- Train `Extreme` longer / vs stronger opponents / higher learning rate (risky).

**Option 2 — One training run + distilled tiers**

- Train one strong policy; create **behavior clones** with noise / action masking for lower tiers (advanced).

For clarity and your folder structure, **Option 1** is easier to explain in documentation and debug.

---

## 5. Metadata, scoring, and promotion

### 5.1 File format: `*_metadata.json` (per tier)

Illustrative schema (adjust field names to implementation):

```json
{
  "schemaVersion": 1,
  "tier": "Trainer",
  "policyType": "Heuristic | MLAgents | ONNX",
  "assetPath": "Assets/AI trainings/models/Trainer/trainer.onnx",
  "policyId": "trainer-2026-04-06T12-00-00Z-run-042",
  "createdUtc": "2026-04-06T12:00:00Z",
  "training": {
    "gitCommit": "abc123",
    "mlAgentsConfig": "trainer_config.yaml",
    "seed": 12345,
    "steps": 2000000
  },
  "evaluation": {
    "protocol": "vs_heuristic_dummy_100ep",
    "primaryMetric": "mean_damage_ratio",
    "value": 1.12,
    "winRate": 0.61,
    "confidence": "95% CI [0.52, 0.70]"
  },
  "promotion": {
    "status": "champion | candidate | rejected",
    "replacesPolicyId": "trainer-previous-id",
    "notes": "Improved spacing; fewer illegal inputs."
  }
}
```

**Why not Unity `.meta`?** Unity `.meta` files are **import pipelines**; hand-editing for metrics is fragile and merges badly. Keep metrics in **JSON** (or ScriptableObject assets if you prefer inspector-driven workflow).

### 5.2 Metrics (“score threshold”)

Define **one primary metric** plus **guardrails**:

| Metric                              | Meaning                    | Failure modes                             |
| ----------------------------------- | -------------------------- | ----------------------------------------- |
| **Win rate** vs fixed baseline      | Simple                     | Can encourage timeouts if rounds are long |
| **Damage ratio** (dealt / taken)    | Often stable               | Ignores win condition                     |
| **ELO / TrueSkill** across policies | Good for relative strength | Needs many matches                        |

**Recommendation:** **Primary:** average **damage ratio** over **N** episodes; **Guardrail:** win rate must exceed floor (e.g. not always losing by timer stall).

### 5.3 Promotion algorithm (pseudocode)

```
new_score = Evaluate(candidate_policy)
old_score = ReadRegistry(champion_for_tier)

if new_score > old_score + margin_delta AND passes_guardrails(new_score):
    BackupPreviousChampion()
    InstallCandidateAsChampion()
    UpdateRegistryJSON()
else:
    DiscardCandidate()
```

**`margin_delta`** avoids churn from noise (e.g. require **statistically significant** improvement or **+2%** absolute on win rate).

### 5.4 “Dynamic thresholds” between Dummy / Trainer / Extreme

If you want **ordering constraints** (Dummy < Trainer < Extreme in measured strength):

- Run a **ladder eval**: each tier must beat the previous tier at least **X%** of the time.
- Store **pairwise** results in `evaluation/ladder_results.json`.

---

## 6. Phased delivery plan (recommended)

### Phase 0 — Design locks (short)

- Lock **input semantics** for Quick Match Setup (keyboard vs controller vs what “touchpad” means).
- Lock **random opponent** behavior (pure random vs seeded random for rematch).
- Choose **Approach A**, **B**, or **hybrid** for AI.

### Phase 1 — Quick Match Setup Scene (no ML)

- New scene + UI: difficulty, input toggle, map name list, character + opponent display.
- Persist into `LobbyContext` / new `QuickMatchSession` + existing `CharacterSelectData` patterns.
- Navigate to `GameScene`.

### Phase 2 — CPU input path

- **Disable human input** on P2; add **CPU adapter** feeding commands.
- Implement **three heuristic profiles** (Dummy / Trainer / Extreme) backed by tunable ScriptableObjects or JSON.

### Phase 3 — Model folders + registry tooling

- Create `Assets/AI trainings/models/**` structure.
- Editor script: **Evaluate** heuristic params, write `*_metadata.json`, **promote** champion.
- Unit tests for JSON read/write (optional but valuable).

### Phase 4 — ML-Agents integration (optional)

- Add packages; training scene; observations/actions.
- Export ONNX / ML-Agents model; inference in player loop for Trainer/Extreme.
- Wire promotion pipeline to compare ML candidates vs heuristic champion.

### Phase 5 — Polish

- Telemetry (optional): track player win rates per tier.
- UX: loading tips, failure reasons if model missing.

---

## 7. Open design questions (to resolve before implementation)

### 7.1 Opponent selection: random vs locked

- **Pure random each time** you open the screen vs **random until you press “reroll”** vs **seeded** for reproducible debug.

### 7.2 Map selection data source

- Reuse **arena list** from existing `ArenaSelectUI` data vs a **reduced list** for Quick Match only.

### 7.3 “Touchpad”

- Clarify if this is **laptop trackpad** (mouse-like) — fighters usually expect **digital** movement; may need a **dedicated profile** or treat as keyboard subset.

### 7.4 Netcode and Quick Match

- Strong recommendation: **Quick Match = offline / local-only** `GameScene` without Netcode host to reduce complexity — unless you explicitly need parity with `StartHost` flow.

### 7.5 Single-player fairness

- Should **adaptive mid-match systems** (`ClassificationSwitcher`, etc.) behave identically to versus-human local matches?

---

## 8. Risks and mitigations

| Risk                                 | Mitigation                                   |
| ------------------------------------ | -------------------------------------------- |
| RL never converges in time           | Ship heuristics first; ML as upgrade         |
| Large binaries in Git                | Git LFS; ignore experimental runs            |
| Reward hacking (undesired play)      | Human playtests + guardrail metrics          |
| Input injection bugs (illegal moves) | Centralize command validation same as humans |
| Performance                          | Throttle AI decisions to 10–30 Hz            |

---

## 9. Testing strategy

1. **Deterministic smoke:** Load Quick Match → start → P2 receives no human input events.
2. **Tier separation:** Telemetry or batch eval shows **ordered** difficulty on average.
3. **Regression:** Champion model loads; missing file falls back to heuristic with **logged warning**.

---

## 10. Documentation and handoff

- Keep **`Assets/AI trainings/README.md`** with: how to run training, evaluation command, promotion rules, and **where** champions live.
- This **`quick_match_plan.md`** remains the **high-level** contract; implementation can split into ADRs if the team grows.

---

## 11. Summary answer to “is this flow good?”

**Yes** — your idea of **tier folders**, **tracked scores**, and **promote-if-better** is sound and maps cleanly to MLOps-style checkpoint management.

**Strengthen it by:**

1. Separating **Unity asset `.meta`** files from **training metadata JSON**.
2. Defining **evaluation protocols** and **one primary metric** with **guardrails**.
3. Planning a **heuristic-first** milestone so gameplay ships even if ML runs long.

---

_End of plan._
