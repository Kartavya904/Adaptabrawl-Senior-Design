# Project-Description — Adaptabrawl

> **Course:** CS5001 - Senior Design (Fall 2025)
> **Team Name:** Adaptabrawl
> **Repository:** [Adaptabrawl Senior Design](https://github.com/Kartavya904/Adaptabrawl-Senior-Design.git)
> **Faculty/Industry Advisor:** *TBD*

---

## Team Members

| Name           | Major            | Email   | Focus/Role (tentative)   |
| -------------- | ---------------- | ------- | ------------------------ |
| Kartavya Singh | Computer Science | **singhk6@mail.uc.edu**  | Netcode & Infrastructure |
| Saarthak Sinha | Computer Science | **sinhas6@mail.uc.edu**  | Combat/Systems & Tooling |
| Kanav Shetty   | Computer Science | **shettykv@mail.uc.edu** | UX/UI, VFX/SFX, Stages   |
| Yash Ballabh   | Computer Science | **ballabyh@mail.uc.edu** | Tools, CI/CD & QA        | 
---

## Project Topic Area

**Game Development** → **2D Multiplayer Fighting Game**, **Online Netcode & Lobbies**, **Real‑Time Systems**, **Gameplay/UX Readability**, **Adaptive Game Mechanics**.

---

## One‑Paragraph Overview

**Adaptabrawl** is a 2D fighting game that refuses to over‑index on a single style. Instead, it blends **attack**, **defense**, and **evasion**—and then adapts them live through **match conditions** (e.g., stage/weather modifiers) and **readable status effects** (e.g., poisoned, staggered/punched, heavy‑attack windup, low‑HP state). The result is a fast, legible combat loop where players switch gears fluidly rather than committing to one-dimensional play.

---

## Core Features (MVP → Beta)

- Local & **online** 1v1 (modes beyond 1v1 in stretch)
- Movement, light/heavy attacks, block/parry, dodge
- Status effects with readable UI (icons + timers)
- Adaptive conditions that modify stats/moves in fair, disclosed ways
- Controller + keyboard support

---

## Getting Started

```bash
git clone https://github.com/Kartavya904/Adaptabrawl-Senior-Design.git
# Unity: Open the cloned folder with Unity Hub (LTS recommended). Open a scene from `Adaptabrawl/Assets/Scenes/` and press Play.
# Requirements: Unity Hub + Unity LTS (matching project version), Git.
```

## Semester Plan (high level)

* **Semester 1:**

  * Core offline combat: movement, light/heavy attacks, block/parry, dodge.
  * Status effects v1 and adaptive conditions v1 (clear UI icons/timers, disclosed modifiers).
  * Online 1v1 with lobbies/room codes; host‑authoritative netcode with client prediction.
  * Deliver a vertical slice: two fighters, one stage, stable online play.
* **Semester 2:**

  * Add fighters/stages; adaptive conditions v2 with richer rule graph and UI disclosure.
  * Balance/telemetry, online hardening (QoS/regions, reconnect), training mode and usability polish.

(See `Miscellaneous/adaptabrawl_projectplan.md` for detailed milestones, KPIs, and architecture.)

---

## Problem & Motivation (draft)

Recent fighters often emphasize one dominant axis—pure evasion, pure rushdown, or heavy turtling. This narrows decision space and punishes mixed‑style play. **Adaptabrawl** makes adaptability the first‑class skill: conditions and states nudge stats/moves in fair, disclosed ways so players constantly re‑optimize tactics without losing readability or balance.

---

## Why Existing Approaches Fall Short (draft)

* **Single‑style bias**: systems reward one dimension disproportionately.
* **Opaque modifiers**: hidden or unclear status/condition effects reduce trust.
* **Netplay friction**: inconsistent latency handling and desyncs undermine fairness.

**Our angle:** adaptive but **transparent** modifiers, strict readability, and pragmatic netcode that favors predictability over unchecked rollback complexity.

---

## Technical Background (draft)

* **Engine:** Unity (LTS) *primary*; Godot 4 (GDScript) *fallback*. Finalize choice by Week 2-4.
* **Networking:** Host‑authoritative simulation; client prediction + server reconciliation; snapshot sync (20–30 Hz); lag compensation for hit validation; Relay/Lobby service for NAT traversal.
* **Data‑driven content:** ScriptableObjects (fighters, moves, statuses, conditions) for rapid iteration.

---

## Team Approach (draft)

* **Pillars:** Adaptive combat • Responsiveness (60 FPS) • Readability • Fair Netplay.
* **Process:** 1–2 week sprints; Friday playtests; Conventional Commits; PRs with short clips (maybe).
* **Risk controls:** scope gates (S1 = 1v1 only), determinism audits, perf budgets, early telemetry.

---

## To Do List

* Faculty/Industry Advisor name + contact
* Expanded abstract
* Detailed problem statement & background
* Comparative analysis of existing solutions
* Technical design deep‑dive
* Evaluation plan & results

---
