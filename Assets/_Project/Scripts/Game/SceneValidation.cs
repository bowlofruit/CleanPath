using UnityEngine;

namespace CleanPath.Game
{
    public static class SceneValidation
    {
        public static bool Require(Object obj, string label)
        {
            if (obj != null) return true;
            Debug.LogError($"CleanPath: required reference missing — {label}.");
            return false;
        }
    }
}
