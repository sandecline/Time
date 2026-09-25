#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace TimeMetroidvania.Editor
{
    /// <summary>
    /// 为项目正式像素素材统一应用不会改变切片布局的导入参数。
    /// </summary>
    internal sealed class PixelArtTexturePostprocessor : AssetPostprocessor
    {
        private const string ArtRoot = "Assets/_Project/Art/";
        private const float PixelsPerUnit = 32f;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
        }
    }
}
#endif
