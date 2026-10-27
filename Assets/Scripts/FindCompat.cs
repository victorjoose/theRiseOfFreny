using UnityEngine;

// Unity 6.6 deprecates FindObjectsOfType and the FindObjectsByType overloads that take a sort
// mode, while the CI compile check still runs on the 2021 API, which lacks the new overloads.
// Every "find all of these in the scene" goes through here, so neither side warns or breaks.
public static class FindCompat {

    public static T[] All<T>(bool includeInactive = false) where T : Object {
        FindObjectsInactive inactive = includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;
#if UNITY_6000_6_OR_NEWER
        return Object.FindObjectsByType<T>(inactive);
#else
        return Object.FindObjectsByType<T>(inactive, FindObjectsSortMode.None);
#endif
    }
}
