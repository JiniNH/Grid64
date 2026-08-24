# 🧩 Block Puzzle 2D (Unity Prototype)

![Unity](https://img.shields.io/badge/Unity-100000?style=for-the-badge&logo=unity&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![Status](https://img.shields.io/badge/Status-Prototype_v1.0-brightgreen?style=for-the-badge)


A hyper-casual infinite block puzzle built with Unity and C#. Fit blocks into a 8×8 grid, chain combos, trigger Fever Time, and push your limits.

**Status: Prototype v2.0 — Core loop complete & playtested**

## 🛠️ Tech Stack

* **Engine**: Unity 6.3 LTS (6000.3.18f1)
* **Scripting**: C#
* **Rendering**: URP 2D Renderer
* **UI**: TextMeshPro (UGUI)
* **Platform Target**: Android / iOS (Portrait)

## ✨ Features

### Core Loop
* **8×8 Grid System** — 2D array data structure with world/grid coordinate conversion
* **Drag & Drop** — Smooth interaction with automatic grid snapping and placement validation
* **Line Clear** — Row/column detection with particle VFX
* **Deadlock Detection** — Simulates every remaining block against all 64 cells to trigger game over accurately

### Scoring & Technical Play
* **Multi-Clear Scaling** — Clearing multiple lines at once raises the per-cell value (100 / 150 / 200)
* **Combo System** — Consecutive clears build a multiplier (×1.0 → ×1.1 → ×1.2 …)
* **Perfect Cross Clear** — Clearing a row and column simultaneously grants a 1.5× bonus
* **Fever Time** — Technical clears charge a gauge; at 100% all points double for 10 seconds

### Content & Balance
* **42 Block Types** — I/O/L/J/T/S/Z tetrominoes plus pentomino variants and a cross-shaped block
* **Tier-Weighted Spawning** — Each block carries a tier (T1–T4) and spawn weight via `BlockInfo`, tuned through playtesting

### Polish
* **Auto-Generated Drop Shadows** — Runtime shadow creation per block cell
* **Deck Scaling** — Blocks shrink in the deck and restore to full size when picked up
* **Procedural SFX** — `RetroAudio` synthesizes pickup/drop/clear sounds in code, pitch rising with combo
* **Lo-Fi BGM** — Looping background music that survives scene reloads

## 🎮 How to Play

1. Drag a block from the bottom deck onto the 8×8 grid.
2. Fill an entire row or column to clear it and score.
3. Chain clears back-to-back to build combos and charge the Fever gauge.
4. Clear a row and column at once for a Perfect bonus.
5. Use all 3 blocks to spawn a new set — the game ends when nothing fits.

## 📁 Architecture

| Script | Role |
|---|---|
| `BoardManager.cs` | Grid data, coordinate conversion, line clearing, scoring, combo/fever logic, game over |
| `DeckManager.cs` | Deck spawning with weighted randomness, refill, deck scaling |
| `BlockDrag.cs` | Drag & drop, snap validation, shadow generation, sorting order |
| `BlockInfo.cs` | Per-block tier and spawn weight data |
| `RetroAudio.cs` | Procedural sound effect synthesis |
| `BGMPlayer.cs` | Looping background music with scene persistence |

## 🚀 Roadmap

* **Mobile Build Testing** — Verify touch controls on actual devices
* **UI Overhaul** — Fever gauge polish, layout, color scheme
* **Dynamic Obstacles** — Score-triggered blocker tiles with HP scaling
* **DDA** — Adjust spawn weights based on remaining board space
* **SDK Integration** — AdMob, Firebase Analytics
* **Store Release** — Google Play & App Store

## 📜 Credits & Licenses

All third-party assets are CC0 or OFL licensed. See project documentation for the full asset ledger.

---

*"A journey of a thousand miles begins with a single block."* 🚀
