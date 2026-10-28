using System.Collections.Generic;
using UnityEngine;

// A climbable column (made by StageTerrain from the ladder tiles). LadderClimber on Freny looks the
// ladders up by area, so no trigger messages are needed.
public class Ladder : MonoBehaviour {

    public static readonly List<Ladder> All = new List<Ladder>();

    public Bounds Area;

    private void OnEnable() {
        All.Add(this);
    }

    private void OnDisable() {
        All.Remove(this);
    }

    public static Ladder At(Vector2 point) {
        foreach (Ladder ladder in All) {
            if (ladder.Area.Contains(new Vector3(point.x, point.y, ladder.Area.center.z))) {
                return ladder;
            }
        }
        return null;
    }
}
