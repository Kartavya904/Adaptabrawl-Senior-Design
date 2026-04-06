# Quick Match Heuristic Models

This folder stores the shipped Quick Match CPU champions.

- `models/Dummy/` contains the weak practice profile.
- `models/Trainer/` contains the mid-tier sparring profile.
- `models/Extreme/` contains the strongest shipped profile.

Each tier exports two JSON files:

- `*_model.json` is the heuristic policy payload.
- `*_metadata.json` records the score, policy id, and promotion details.

## Rebuild / retrain

From the repo root on Windows PowerShell:

```powershell
.\build_quick_match_feature.ps1
```

That Unity batch command will:

1. Mirror the Unity project into `Adaptabrawl/Adaptabrawl_QuickMatchBuildTemp/`.
2. Build or update `QuickMatchScene` in the temp project.
3. Inject the Quick Match entry into `StartScene`.
4. Retrain the heuristic champions with a fresh seed.
5. Export the champion JSON files into this folder.
6. Sync the generated scene, catalog, models, and training log back into the real project.

## Logs

- Tracked training summary: `Assets/AI trainings/logs/quick_match_training_latest.txt`
- Raw Unity batch log: `quickmatch_unity_temp.log`

## Runtime behavior

At runtime the game copies these shipped champions into `Application.persistentDataPath/QuickMatchModels/` on first load.
The Quick Match screen can retrain the local copies without touching the tracked asset files.

To publish improved local champions back into the repo, use:

- `Tools/Adaptabrawl/Quick Match/Export Persistent Models To Assets`
