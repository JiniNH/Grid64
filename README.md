# 🧩 Block Puzzle 2D (Unity Prototype)

![Unity](https://img.shields.io/badge/Unity-100000?style=for-the-badge&logo=unity&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![Status](https://img.shields.io/badge/Status-Prototype_v1.0-brightgreen?style=for-the-badge)


A hyper-casual block puzzle built with Unity and C#. Fit blocks into an 8×8 grid, chain combos, trigger Fever Time, and race the clock in Time Attack.

Status: v1.1.0 — in closed testing on Google Play

🛠️ Tech Stack
Engine: Unity 6.3 LTS (6000.3.18f1)
Scripting: C#
Rendering: URP 2D Renderer
UI: TextMeshPro (UGUI)
Monetization: Google Mobile Ads (AdMob) — adaptive banner
Platform Target: Android (Portrait), iOS planned
✨ Features
Game Modes
Classic — No time limit. Play until no block fits and chase the high score.
Time Attack — Beat the clock. Clearing lines adds time, so a strong run extends itself. A deadlock costs time and clears a random band of the board instead of ending the run.

Each mode keeps its own best score.

Core Loop
8×8 Grid System — 2D array data structure with world/grid coordinate conversion
Zone-Based Drag — Tapping a third of the deck area picks up that block, so thin and single-cell pieces are never hard to grab
Line Clear — Row/column detection with particle VFX that inherit each block's colour
Deadlock Detection — Simulates every remaining block against all 64 cells
Scoring & Technical Play
Multi-Clear Scaling — Clearing multiple lines at once raises the per-cell value (100 / 150 / 200)
Combo System — Consecutive clears build a multiplier (×1.0 → ×1.1 → ×1.2 …)
Perfect Cross Clear — Clearing a row and column simultaneously grants a 1.5× bonus
Fever Time — Technical clears charge a gauge; at 100% all points double for 10 seconds (Classic only)
All Clear — Emptying the board awards a 3,000 point bonus
Content & Balance
42 Block Types — I/O/L/J/T/S/Z tetrominoes plus pentomino variants and a cross-shaped block
Tier-Weighted Spawning — Each block carries a tier (T1–T4) and spawn weight via BlockInfo
Solvable Spawning — The deck is drawn from blocks that actually fit the current board, with a tunable chance of offering a line-completing piece
Polish
Adaptive Layout — Camera and canvas adapt to any aspect ratio, from 21:9 phones to unfolded foldables and tablets
Pause Menu — Resume, restart, return to menu, and live audio control without leaving the run
Audio Settings — Separate BGM and SFX sliders with mute, persisted across sessions
Animated Menu Background — Drifting, rotating block pieces generated at runtime
Procedural SFX — RetroAudio synthesizes pickup/drop/clear sounds in code, pitch rising with combo
Auto-Generated Drop Shadows — Runtime shadow creation per block cell
Korean UI — Fully localized interface
🎮 How to Play
Pick a mode from the main menu.
Drag a block from the bottom deck onto the 8×8 grid.
Fill an entire row or column to clear it and score.
Chain clears back-to-back to build combos and charge the Fever gauge.
Clear a row and column at once for a Perfect bonus.
Use all 3 blocks to spawn a new set — Classic ends when nothing fits, Time Attack ends when the clock runs out.
📁 Architecture
Gameplay
Script	Role
BoardManager.cs	Grid data, coordinate conversion, line clearing, scoring, combo/fever, game over, board rescue
DeckManager.cs	Zone-based touch input, weighted deck spawning, refill
BlockDrag.cs	Drag motion, snap validation, shadow generation, sorting order
BlockInfo.cs	Per-block tier, spawn weight, fragment colour
GameModeManager.cs	Current mode and per-mode best scores (static, survives scene loads)
TimeAttackManager.cs	Countdown, time bonuses, deadlock penalty, warning state
UI & Presentation
Script	Role
MainMenuUI.cs	Mode selection, best scores, settings panel, external links
PauseMenu.cs	In-game pause, back-button and app-suspend handling
VolumeRow.cs	Reusable volume slider row with mute toggle
MenuBackground.cs	Runtime-generated drifting block pieces
CameraFitter.cs	Orthographic size and position for any aspect ratio
SafeAreaFitter.cs	Notch and gesture-bar avoidance (reserved for a later release)
Systems
Script	Role
SoundSettings.cs	Volume and mute state, persisted via PlayerPrefs
RetroAudio.cs	Procedural sound effect synthesis
BGMPlayer.cs	Looping background music with scene persistence
AdManager.cs	AdMob banner and interstitial pacing
Scenes
MainMenu — mode selection, settings, animated background
MainGame — shared by both modes; mode-specific systems disable themselves at startup
🚀 Roadmap
Store Release — Google Play production after closed testing
Dynamic Obstacles — Score-triggered blocker tiles with HP scaling (v1.1 update)
In-Game Best Score — Persistent record display during play
Object Pooling — Reuse particle instances instead of instantiating per clear
Localization — Separate Korean and English string tables
iOS Build — Xcode pipeline and App Store submission
📜 Credits & Licenses
Asset	Source	License
UI icons, UI pack, audio	Kenney	CC0 1.0
NanumSquareNeo	NAVER	SIL OFL 1.1

Buttons, panels, and the app icon are original work.

Privacy Policy
Open Source Licenses

"A journey of a thousand miles begins with a single block." 🚀
