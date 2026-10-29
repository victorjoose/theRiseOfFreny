using System;
using System.Collections.Generic;
using UnityEngine;

// The mouse pointer is hidden while Freny is being played (the mouse buttons are the sword and
// the shot, and a pointer in the middle of the fight only confused), and comes back on every
// screen that is clicked: menus, pause, the end of a run, the shop. Only hidden, never locked,
// so the clicks still reach the game; in the web build this is the page's cursor over the canvas.
public static class GameCursor {

    private struct Request {
        public UnityEngine.Object owner;
        public Func<bool> wants;
    }

    private static readonly List<Request> requests = new List<Request>();
    private static bool hidden;

    // a gameplay screen that needs the pointer while wants() is true (dropped with its owner)
    public static void ShowWhile(UnityEngine.Object owner, Func<bool> wants) {
        requests.Add(new Request { owner = owner, wants = wants });
    }

    // called every frame by GameMenus: playing is a gameplay scene, not paused, window focused
    public static void Apply(bool playing) {
        bool hide = playing && !Wanted();
        if (hide != hidden || Cursor.visible == hide) {
            hidden = hide;
            Cursor.visible = !hide;
        }
    }

    private static bool Wanted() {
        bool wanted = false;
        for (int i = requests.Count - 1; i >= 0; i--) {
            if (requests[i].owner == null) {
                requests.RemoveAt(i);
            } else if (!wanted && requests[i].wants()) {
                wanted = true;
            }
        }
        return wanted;
    }
}
