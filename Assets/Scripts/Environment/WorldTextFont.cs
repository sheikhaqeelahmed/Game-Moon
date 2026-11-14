using UnityEngine;

namespace PlanetScrappers.Environment
{
    /// <summary>
    /// Feeds the live font atlas into a sign's depth-tested material. The built-in font's
    /// texture is generated at runtime, so it cannot be saved in the material asset.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(TextMesh), typeof(MeshRenderer))]
    public class WorldTextFont : MonoBehaviour
    {
        TextMesh textMesh;
        MeshRenderer meshRenderer;

        void OnEnable()
        {
            textMesh = GetComponent<TextMesh>();
            meshRenderer = GetComponent<MeshRenderer>();
            Font.textureRebuilt += OnFontRebuilt;
            Apply();
        }

        void OnDisable()
        {
            Font.textureRebuilt -= OnFontRebuilt;
        }

        void OnFontRebuilt(Font font)
        {
            if (textMesh != null && font == textMesh.font) Apply();
        }

        void Apply()
        {
            if (textMesh == null || textMesh.font == null || meshRenderer.sharedMaterial == null) return;
            var atlas = textMesh.font.material != null ? textMesh.font.material.mainTexture : null;
            if (atlas != null && meshRenderer.sharedMaterial.mainTexture != atlas)
                meshRenderer.sharedMaterial.mainTexture = atlas;
        }
    }
}
