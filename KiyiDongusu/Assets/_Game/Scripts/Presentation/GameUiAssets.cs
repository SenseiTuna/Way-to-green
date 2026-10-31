using UnityEngine;

namespace KiyiDongusu
{
    // Small UI-only rounded surface; illustrations remain the approved A assets.
    public sealed class GameUiAssets
    {
        public readonly Font Regular, Bold;
        public readonly Sprite Surface;
        readonly Texture2D texture;
        public GameUiAssets()
        {
            Regular = Resources.Load<Font>("Typography/Lato-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Bold = Resources.Load<Font>("Typography/Lato-Bold") ?? Regular;
            const int size = 64, radius = 14;
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "UI_RoundedSurface", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(radius - x - .5f, x + .5f - (size - radius), 0);
                    float dy = Mathf.Max(radius - y - .5f, y + .5f - (size - radius), 0);
                    float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + .5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
            texture.SetPixels32(pixels); texture.Apply(false, true);
            Surface = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect, Vector4.one * radius);
            Surface.name = "UI_RoundedSurface";
        }
        public void Dispose() { Object.Destroy(Surface); Object.Destroy(texture); }
    }
}
