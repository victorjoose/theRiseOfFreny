using System;
using System.Reflection;
using UnityEngine;

// The Escudeiro and the Ciclope have no prefabs: their art came without working Animator
// controllers, so they are made at runtime from the Golem prefab (its physics, layers, coin and
// sounds setup), with the Golem script swapped for theirs and FrameAnimator drawing the sheets.
// The templates sit under an inactive holder, so they never run themselves; Instantiate copies
// them like any prefab.
public static class EnemyTemplates {
    private static Transform holder;

    public static GameObject Make<T>(GameObject golemPrefab, string name, Vector2 colliderSize, Vector2 colliderOffset) where T : Enemy {
        if (golemPrefab == null) {
            return null;
        }
        if (holder == null) {
            GameObject h = new GameObject("Enemy Templates");
            h.SetActive(false);
            holder = h.transform;
        }
        GameObject template = UnityEngine.Object.Instantiate(golemPrefab, holder);
        template.name = name;
        Enemy source = template.GetComponent<Enemy>();
        T target = template.AddComponent<T>();
        CopySerialized(source, target);
        UnityEngine.Object.DestroyImmediate(source);
        foreach (Animator animator in template.GetComponentsInChildren<Animator>(true)) {
            UnityEngine.Object.DestroyImmediate(animator);
        }
        foreach (SpriteSkin skin in template.GetComponentsInChildren<SpriteSkin>(true)) {
            UnityEngine.Object.DestroyImmediate(skin);
        }
        if (template.GetComponent<Collider2D>() is BoxCollider2D box) {
            box.size = colliderSize;
            box.offset = colliderOffset;
        }
        return template;
    }

    // every field the inspector would show (public, or [SerializeField]) of the Enemy base
    private static void CopySerialized(Enemy from, Enemy to) {
        for (Type type = typeof(Enemy); type != null && type != typeof(MonoBehaviour); type = type.BaseType) {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
                bool serialized = (field.IsPublic && !field.IsNotSerialized) || field.IsDefined(typeof(SerializeField), false);
                if (serialized && !field.IsInitOnly) {
                    field.SetValue(to, field.GetValue(from));
                }
            }
        }
    }
}
