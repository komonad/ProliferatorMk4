using System;
using CommonAPI;
using HarmonyLib;
using UnityEngine;

namespace ProliferatorMk4
{
    [HarmonyPatch(typeof(Resources), nameof(Resources.Load), typeof(string), typeof(Type))]
    [HarmonyBefore(CommonAPIPlugin.GUID)]
    internal static class TextureResourcesPatch
    {
        private const string TexpackPrefix = "Assets/texpack/";

        private static bool Prefix(ref string path, Type systemTypeInstance, ref UnityEngine.Object __result)
        {
            if (!path.StartsWith(TexpackPrefix, StringComparison.Ordinal))
            {
                return true;
            }

            string name = path.Substring(TexpackPrefix.Length);
            if (systemTypeInstance == typeof(Texture2D))
            {
                __result = TextureHelper.GetTexture(name);
                return __result == null;
            }

            if (systemTypeInstance == typeof(Sprite))
            {
                __result = TextureHelper.GetSprite(name);
                return __result == null;
            }

            return true;
        }
    }
}
