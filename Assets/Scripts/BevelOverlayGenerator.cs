using UnityEngine;

public static class BevelOverlayGenerator
{
    private static Sprite cachedSprite;

    public static Sprite GetBevelSprite()
    {
        if (cachedSprite != null) return cachedSprite;

        int size = 128;
        float strength = 0.55f;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            float ny = (float)y / (size - 1);
            for (int x = 0; x < size; x++)
            {
                float nx = (float)x / (size - 1);
                float bevel = ny - nx;

                Color c;
                if (bevel > 0)
                    c = new Color(1f, 1f, 1f, bevel * strength);
                else
                    c = new Color(0f, 0f, 0f, -bevel * strength);

                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();

        cachedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return cachedSprite;
    }
}