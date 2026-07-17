# 🧩 Block Puzzle 2D (Unity Prototype)

![Unity](https://img.shields.io/badge/Unity-100000?style=for-the-badge&logo=unity&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![Status](https://img.shields.io/badge/Status-Prototype_v1.0-brightgreen?style=for-the-badge)

A classic 2D drag-and-drop block puzzle game built with Unity and C#. 
This repository contains the foundational core loop (v1.0) of the game, featuring a robust grid system, intuitive physics, dynamic line-clearing mechanics, and a complete game-over logic.


## 🛠️ Tech Stack
* **Engine:** Unity 6.3 LTS (6000.3.18f1)
* **Scripting:** C# 
* **UI:** TextMeshPro (UGUI)


## ✨ Core Features Completed (v1.0.0)
* **Dynamic Grid System**: Mathematical 2D array mapping (`gridData`) for an 8x8 board.
* **Precision Drag & Drop**: Smooth mouse interactions with automatic grid snapping.
* **Smart Placement Validation**: Algorithm to prevent out-of-bounds positioning and block overlaps (Index out of range protection).
* **Line Clear & Scoring System**: Automatic detection and destruction of fully occupied rows/columns, updating the Score UI in real-time (800 points per line clear).
* **Infinite Deck System**: Random generation of blocks (1x1, 2x1, L-shape) that automatically refills when the deck is depleted.
* **Deadlock Detection (Game Over)**: Advanced simulation algorithm that checks all 64 grid positions against remaining blocks. Accurately triggers a game-over state when 0 valid placements are available.


## 🎮 How to Play
1. Pick up a block from the bottom deck using your mouse.
2. Drag and drop the block onto the 8x8 grid.
3. Fill up an entire row or column to clear the line and score points!
4. Use all 3 blocks in the deck to spawn a new set.
5. Watch your space—the game ends when there is no valid space left to place any of the remaining blocks.


## 🚀 Next Steps (Roadmap)
- [ ] **Visual Polishing**: Establish a core visual theme and replace primitive shapes with high-quality sprites.
- [ ] **VFX & Animation**: Add particle effects for Line Clears (Explosions!) and smooth drop-shadow animations for blocks.
- [ ] **UI Upgrades**: Design a Game Over popup panel with a Restart button, and polish the Score Text font/layout.
- [ ] **Audio**: Add engaging sound effects (snap, clear) and background music (BGM).


---
*“A journey of a thousand miles begins with a single block.”* 🚀
