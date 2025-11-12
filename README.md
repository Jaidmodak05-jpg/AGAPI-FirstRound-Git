Memory Matching Game
 Overview
  This is a 2D Memory Matching Game built in Unity where players flip pairs of cards to find matching images.
  It includes sound effects, scoring, combo bonuses, timer-based challenges, a main menu with difficulty options, and a game-over screen.

 Game Rules
  The game starts with all cards face down.
  Players click on two cards to flip them.
  If both cards match, they vanish from the board.
  If they don’t match, both cards flip back.
  The player continues until:
  All cards are matched (You Win!), or
  The timer runs out (Game Over!).

Scoring:
  +100 points for each correct match.
  –25 points penalty for each wrong match.
  Combo Bonus: Each consecutive match increases score by +10 extra per combo level.

Features Implemented
  Dynamic grid generation based on difficulty
  Matching logic with combo bonuses
  Real-time timer countdown
  Sound effects for match, miss, win, and game-over
  Game over panel showing final and best scores
  Restart button
Main menu with:
  Easy (2x2 grid, 60 sec)
  Normal (4x4 grid, 45 sec)
  Hard (6x6 grid, 30 sec)

Custom mode (user sets rows, columns, and time)
 Fade-out animation when cards vanish
 Responsive UI with TextMeshPro elements

 Scripts Overview
 GameControllerUI.cs
    Manages game flow: score, combo, timer, and win/lose conditions.
    Tracks all cards and handles matching logic.
    Displays Game Over Panel with score and “Play Again” functionality.

Plays appropriate sound via AudioManager.cs.

 AudioManager.cs
  Handles background music and sound effects.
  Plays Match, Miss, Win, and Game Over audio clips.

GridGeneratorUI.cs
  Dynamically spawns the card grid based on selected difficulty.
  Maintains proper spacing and card alignment.
  Each card prefab connects to CardUI.cs for interaction.

CardUI.cs
  Controls the flip animation and interaction for each card.
  Handles vanish animation after a successful match.
  Prevents flipping more than two cards simultaneously.

GameSettings.cs
  Stores chosen difficulty and grid size (rows, columns, timer).
  Allows Main Menu → Game Scene data transfer.

MainMenuUI.cs
  Handles all button navigation (Play, Quit, Custom Difficulty).
  Applies selected difficulty to GameSettings before loading the game scene.

Developer Notes
  Developed using Unity 2022+.
  Works with both PC and mobile resolutions.
  All scripts are modular and can be extended for themes or multiplayer.

Credits
  Developed by Jaid Modak
  For the First Round Test Assignment – Memory Matching Game
