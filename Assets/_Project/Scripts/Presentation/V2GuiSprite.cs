using UnityEngine;

namespace TimeMetroidvania.Presentation
{
    /// <summary>从多精灵图集裁取纹理区域，在运行时 IMGUI 中绘制。</summary>
    public static class V2GuiSprite
    {
        public static void Draw(Sprite sprite, Rect target)
        {
            if (sprite == null) return;
            GUI.DrawTextureWithTexCoords(target, sprite.texture, Uv(sprite), true);
        }

        public static void DrawFilled(Sprite sprite, Rect target, float amount)
        {
            if (sprite == null) return;
            amount = Mathf.Clamp01(amount);
            if (amount <= 0f) return;
            Rect uv = Uv(sprite);
            uv.width *= amount;
            target.width *= amount;
            GUI.DrawTextureWithTexCoords(target, sprite.texture, uv, true);
        }

        private static Rect Uv(Sprite sprite)
        {
            Rect pixels = sprite.textureRect;
            Texture texture = sprite.texture;
            return new Rect(pixels.x / texture.width, pixels.y / texture.height,
                pixels.width / texture.width, pixels.height / texture.height);
        }
    }
}
