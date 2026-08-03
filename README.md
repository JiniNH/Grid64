# 🧩 Block Puzzle 2D (Unity Prototype)

![Unity](https://img.shields.io/badge/Unity-100000?style=for-the-badge&logo=unity&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![Status](https://img.shields.io/badge/Status-Prototype_v1.0-brightgreen?style=for-the-badge)

A classic 2D drag-and-drop block puzzle game built with Unity and C#. This repository contains the foundational core loop of the game, elevated with dynamic combo multipliers, satisfying visual/audio feedback, and a complete game-over simulation logic.

## 🛠️ Tech Stack
* **Engine:** Unity 6.3 LTS (6000.3.18f1)
* **Scripting:** C#
* **UI:** TextMeshPro (UGUI)
* **Data:** Unity PlayerPrefs (Local Save)

## ✨ Core Features Completed

* **Dynamic Grid System:** Mathematical 2D array mapping (`gridData`) for an 8x8 board.
* **Precision Drag & Drop:** Smooth mouse interactions with automatic grid snapping.
* **Smart Placement Validation:** Algorithm to prevent out-of-bounds positioning and block overlaps (Index out of range protection).
* **Multi-Line Clear & Combo System:** 
  * Automatic detection and destruction of fully occupied rows/columns.
  * Scaled scoring for simultaneous clears (1 Line: 100pts, 2 Lines: 300pts, 3 Lines: 600pts, 4+ Lines: 1000pts).
  * Continuous clears trigger Combo multipliers (+50pts per combo) with animated UI popups (Fade-out effect).
* **Infinite Deck System:** Procedural spawn of 30 unique block prefabs (ranging from 1x1 to massive 3x3 and special shapes) that automatically refills when the deck is depleted.
* **VFX & Procedural Audio:** 
  * Integrated BlockExplosion Particle System for satisfying line clears.
  * `RetroAudio.cs`: Procedurally generated 8-bit sound effects (Sine/Square waves) without external assets.
* **Best Score Persistence:** Local high-score tracking using `PlayerPrefs` that persists across application sessions.
* **Deadlock Detection (Game Over):** Advanced simulation algorithm that checks all 64 grid positions against remaining blocks. Accurately triggers a game-over state (with animated UI popup) when 0 valid placements are available.

## 🎮 How to Play
1. Pick up a block from the bottom deck using your mouse or touch input.
2. Drag and drop the block onto the 8x8 grid.
3. Fill up an entire row or column to clear the line and score points!
4. **[Pro Tip]** Clear lines in consecutive turns to build Combos and earn massive bonus points.
5. Use all 3 blocks in the deck to spawn a new random set.
6. Watch your space—the game ends when there is no valid space left to place any of the remaining blocks. Can you beat your Best Score?

## 🚀 Next Steps (Roadmap)
* [ ] **Main Menu / Title Scene:** Design a welcoming entry screen with a 'Start Game' button and logo.
* [ ] **Juicy Animations:** Add smooth drop-shadow animations for blocks during drag, and subtle screen shakes for multi-line clears.
* [ ] **BGM Integration:** Add an engaging background music track to complete the retro vibe.
* [ ] **Theme Customization:** Allow players to choose different block color palettes (e.g., Night mode, Retro mode).

> “A journey of a thousand miles begins with a single block.” 🚀
