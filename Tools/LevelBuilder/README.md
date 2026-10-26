# Level builder (campaign stages without opening Unity)

Python 3 + Pillow. Run from the repository root.

- `render.py <scene.unity> <out.png>`: draws a scene's tilemaps with markers for the start,
  enemies, exits, savepoints, coins and chests (env `PX` = pixels per tile, `BRIGHT` = brightness).
- `build_stage.py <layout module> <base stage> <new stage>`: builds
  `Assets/Scenes/MaxLevels/<new stage>.unity` from a layout (see `layout_1_6.py`), keeping the
  base stage's cameras, lights, player, savepoint, exit and tilemap objects. The ground is painted
  with `Ground Rule Tile` exactly as Unity would (`ruletile.py` reproduces its rules), the
  background repeats a patch of the base stage's bricks, and enemies, coins, chests and torches
  are prefab instances. A new stage also needs a line in `CampaignLevels.cs` and an entry in
  the Build Settings.
- `openings.py <scene.unity>`: lists the openings at the edges of a stage (where exits go).

Layout rules of thumb (Freny's jump is about 2.6 tiles high and short): steps and ledges at most
2 tiles up, gaps at most 2 tiles wide, and an entity at (x, y) stands on row y (floor is row y-1).

Example:

    python3 Tools/LevelBuilder/build_stage.py layout_1_6 1-2 1-6
    python3 Tools/LevelBuilder/render.py Assets/Scenes/MaxLevels/1-6.unity /tmp/1-6.png
