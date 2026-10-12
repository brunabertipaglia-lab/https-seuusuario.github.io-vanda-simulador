using UnityEditor;

namespace CapiTinkering.EditorTools
{
    /// <summary>
    /// Importa automaticamente as imagens de Assets/CapiTinkering/Art como Sprite (2D and UI),
    /// para que o retrato do Capi e o logo possam ser usados em componentes Image sem ajuste manual.
    /// </summary>
    public sealed class CapiArtImporter : AssetPostprocessor
    {
        private const string ArtFolder = "Assets/CapiTinkering/Art/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtFolder)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
        }
    }
}
