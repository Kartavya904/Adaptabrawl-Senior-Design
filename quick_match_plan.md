# Quick Match & AI Training — Comprehensive Plan

**Document purpose:** Single source of truth for implementing Quick Match (dedicated setup scene), three difficulty tiers (**Dummy**, **Trainer**, **Extreme**), opponent selection (random by default, name preview), input mode (keyboard vs controller), lightweight map selection, and a **training + model registry** pipeline under `Assets/AI trainings/`.

**Status:** Planning / architecture — not an implementation checklist order-of-operations (those can be derived from phases below).

**Related codebase facts (as of authoring):**

- Local flow uses `LobbyContext`, `CharacterSelectData`, `ArenaSelectData`, and loads `GameScene` via `ArenaSelectUI` / Netcode host pattern.
- `LocalGameManager` spawns two fighters and configures **human** input on both via `PlayerController_Platform.ConfigureForPlayer`.
- Fighters are data-driven (`FighterDef` in `Resources/Fighters/` with ordering rules in `CharacterSelectUI`).
- **No** Unity ML-Agents, Sentis inference, or existing CPU-opponent controller is present in `Packages/manifest.json` or core gameplay scripts — any ML path is **additive**.

### Project decision — training method

**Heuristic-first is the chosen method** for Quick Match opponent behavior and for the training/evaluation pipeline in this milestone. Tuned rules, timers, and parameters (stored as JSON / ScriptableObjects per tier) are **primary**; reinforcement learning or ONNX policies remain **optional future work** and do not block shipping.

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
| **Scope for a shipping milestone**               | **Heuristic-first** (committed) delivers playable tiers quickly; full **reinforcement learning** remains optional and is a **research-scale** effort if pursued later. A **phased** plan (§6) keeps the project shippable.                                 |

---

## 2. Architecture overview

### 2.1 Runtime: three cooperating layers

1. **Match configuration (data):** ScriptableObject or static session object created when leaving Quick Match Setup — includes `QuickMatchConfig` with: difficulty enum, `HumanInputScheme`, `ArenaId` or name, `HumanFighterId`, `OpponentFighterId` (or random seed), `OpponentPolicyId` (which checkpoint slot).
2. **AI policy (inference):** For ML-based policies: a **runner** that turns game state → model input tensor → outputs (move, buttons) at fixed frequency. For heuristic tiers: a **rules brain** with tunable parameters.
3. **Input injection:** Instead of `PlayerController_Platform` reading devices for P2, a **`CpuInputAdapter`** (name illustrative) feeds the **same** logical commands the human pipeline expects (preferred) or calls a thin “command buffer” API if one exists.

**Design principle:** **One combat codebase**; **two input sources** (human device vs policy output). Avoid duplicating movement/combat logic for AI.

### 2.2 Training: committed approach and optional future path

#### Approach A — Heuristic / utility AI (**committed for this project**)

- **Dummy:** chase player, attack on cooldown, rarely defend — tuned to the **Dummy** score band (see §5.2).
- **Trainer:** add spacing, occasional block/dodge, simple reactions — tuned to the **Trainer** anchor (half of the best normalized score).
- **Extreme:** tightest timings and best move selection among heuristics — champions drawn from the **upper quartile** of normalized evaluation scores (see §5.2).

**Pros:** Fits senior-design timelines; no Python stack; easy to tune by designers.  
**Cons:** Will not generalize like a true RL policy; “Extreme” is bounded by hand-tuned quality.

#### Approach B — Unity ML-Agents (or custom RL) (**optional / later**)

- Not required for the heuristic-first milestone. If added later: **`com.unity.ml-agents`**, Python training, observations/actions, export behaviors into the same `models/{tier}/` folders.
- The **same normalized-score tier rules** in §5.2 can still govern which checkpoint occupies **Dummy** vs **Trainer** vs **Extreme** slots.

#### Practical sequencing

1. **Ship** Quick Match with **heuristics only** for all three tiers; evaluation and promotion use **§5.2** (normalized scores + percentile/anchors).
2. **Optionally later:** swap in ML policies per tier **without** changing the tier-assignment rules, if evaluation shows benefit.

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

## 4. Training methodology (ML path) — optional future detail

**Primary development uses heuristics** (§2.2, §5.2). This section is retained for a **possible later** ML-Agents track; heuristic tuning does not require it.

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

### 5.2 Metrics and tier assignment (**heuristic-first**, normalized scores)

This project uses **batch evaluation** over many test runs (same protocol each time: e.g. **N** episodes per candidate, fixed maps/opponents where applicable). Each run produces a **raw score** (e.g. composite of damage ratio, win rate, or a single agreed primary metric — lock this in implementation).

#### Step 1 — Normalize

After a round of testing, **normalize** all candidate scores to a common scale (e.g. min–max to \([0, 1]\) so the **best** run in that batch maps to the top of the range, or z-score + squash — **pick one normalization and document it** in `dummy_metadata.json` / registry). The formulas below assume you can refer to:

- **`S_max`** = **maximum normalized score** in the batch (the “top scorer” after normalization).
- Individual run scores **`s`** in the same normalized space.

#### Step 2 — Map tiers to score bands (project rules)

| Tier        | Rule                          | Meaning                                                                                                                                                                                                                                                                                                             |
| ----------- | ----------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Extreme** | **75th percentile and above** | Among normalized scores, the **strongest quartile** (top ~25% of runs). **Extreme** champions are chosen from this band — e.g. the **best** candidate at or above the **75th percentile** threshold, subject to guardrails.                                                                                         |
| **Trainer** | **½ × `S_max`**               | The **medium** tier is anchored at **half of the top (highest) normalized score** — not the literal minimum of the dataset. Implementation: assign **Trainer** to the candidate whose score is **closest** to **`0.5 * S_max`**, or treat **`0.5 * S_max`** as the **target strength** when hand-tuning heuristics. |
| **Dummy**   | **⅛ × `S_max`**               | **Half of a quarter** of the top normalized score: **`(1/2) × (1/4) × S_max = S_max / 8`**. This is **not** “pick the lowest raw scorer” unless it coincides with this anchor; it deliberately keeps Dummy **weak but non-zero** relative to the best in the batch.                                                 |

**Example:** If normalization maps the batch so **`S_max = 1`**, then **Trainer** anchor = **0.5**, **Dummy** anchor = **0.125**. **Extreme** still selects from scores **≥ P75** in that batch.

#### Step 3 — Guardrails

Keep lightweight checks so bad runs do not promote (e.g. infinite stalling, illegal inputs): win rate floor, max match length, or manual disqualification flags — unchanged from earlier recommendations.

#### Relationship to “promote if better”

- **Within a tier folder**, you still **replace** the champion only if the new candidate **beats** the previous champion **and** lands in the correct **band** (or is the best eligible in the Extreme band).
- If a training run does not beat the stored champion, **keep** the pre-trained / previous heuristic parameters.

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

### 5.4 Ordering sanity check (optional)

The rules in §5.2 should usually imply **Dummy < Trainer < Extreme** in mean strength. If not (e.g. small **N**, noisy scores), add:

- A **ladder eval** (each tier beats the previous **X%** of the time), and/or
- Pairwise logs in `evaluation/ladder_results.json`.

---

## 6. Phased delivery plan (recommended)

### Phase 0 — Design locks (short)

- Lock **input semantics** for Quick Match Setup (keyboard vs controller vs what “touchpad” means).
- Lock **random opponent** behavior (pure random vs seeded random for rematch).
- **AI:** **Heuristic-first** (§2.2); lock **normalization** and **tier rules** in §5.2 for evaluation tooling.

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
2. Defining **evaluation protocols**, **normalization**, and **tier bands** (§5.2: **Extreme** from **≥ 75th percentile**, **Trainer** at **½ × top normalized score**, **Dummy** at **⅛ × top normalized score**).
3. Committing to **heuristic-first** training (§2.2) so gameplay ships on schedule; ML remains optional.

---

_End of plan._
