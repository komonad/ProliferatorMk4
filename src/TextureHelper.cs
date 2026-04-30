using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace ProliferatorMk4
{
    internal static class TextureHelper
    {
        private static readonly Assembly Assembly = typeof(TextureHelper).Assembly;
        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        internal static Texture2D GetTexture(string name)
        {
            if (Cache.TryGetValue(name, out Texture2D cached))
            {
                return cached;
            }

            using (Stream stream = Assembly.GetManifestResourceStream($"ProliferatorMk4.assets.sprite.{name}.png"))
            {
                if (stream == null)
                {
                    return null;
                }

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    Texture2D texture = new Texture2D(2, 2);
                    if (!texture.LoadImage(memoryStream.ToArray()))
                    {
                        return null;
                    }

                    texture.name = name;
                    Cache[name] = texture;
                    return texture;
                }
            }
        }

        internal static Sprite GetSprite(string name)
        {
            if (SpriteCache.TryGetValue(name, out Sprite cached))
            {
                return cached;
            }

            Texture2D texture = GetTexture(name);
            if (texture == null)
            {
                return null;
            }

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            SpriteCache[name] = sprite;
            return sprite;
        }
    }
}
