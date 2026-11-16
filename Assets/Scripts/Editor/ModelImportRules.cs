using UnityEditor;

namespace PlanetScrappers.EditorTools
{
    /// <summary>
    /// Import rules for the Blender-generated models in Assets/Art/Models:
    /// no embedded materials (the builder assigns its own by the "__Key" name suffix),
    /// no animation, cameras or lights, and the FBX axis conversion baked into the meshes
    /// so every node comes in with an identity rotation.
    /// </summary>
    public class ModelImportRules : AssetPostprocessor
    {
        public override uint GetVersion() => 3;

        /// <summary>Logs every node of a model (batch-mode check of axis conversion).</summary>
        public static void DumpModels()
        {
            foreach (var name in new[] { "Rookie", "SH01Truck" })
            {
                var go = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>($"Assets/Art/Models/{name}.fbx");
                foreach (var t in go.GetComponentsInChildren<UnityEngine.Transform>())
                {
                    var mf = t.GetComponent<UnityEngine.MeshFilter>();
                    string b = mf != null ? $" bounds c={mf.sharedMesh.bounds.center} s={mf.sharedMesh.bounds.size}" : "";
                    UnityEngine.Debug.Log($"[Dump] {name}/{t.name} p={t.localPosition} r={t.localEulerAngles} s={t.localScale}{b}");
                }
            }
        }

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/Art/Models/")) return;
            var importer = (ModelImporter)assetImporter;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.bakeAxisConversion = true;
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Low;
        }
    }
}
