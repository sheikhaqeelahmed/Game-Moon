using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PlanetScrappers.EditorTools
{
    /// <summary>
    /// Renders review screenshots of Rust Valley from fixed viewpoints into Builds/Snapshots.
    /// Useful for progress reports and for checking the layout without playing.
    /// Batch mode (needs a GPU, so do not pass -nographics):
    ///   Unity.exe -batchmode -quit -projectPath . -executeMethod PlanetScrappers.EditorTools.SceneSnapshots.Capture
    /// </summary>
    public static class SceneSnapshots
    {
        const string ScenePath = "Assets/Scenes/RustValley.unity";

        [MenuItem("Planet Scrappers/Capture Review Snapshots", priority = 40)]
        public static void Capture()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            string outDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds", "Snapshots");
            Directory.CreateDirectory(outDir);

            var player = GameObject.Find("Player (Rookie Scrapper)");
            var camObject = GameObject.Find("Player Camera");
            var playerCam = camObject != null ? camObject.GetComponent<Camera>() : null;

            if (playerCam != null)
                Render(playerCam, Path.Combine(outDir, "01_spawn_third_person.png"));

            var go = new GameObject("SnapshotCamera");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.farClipPlane = 3000f;
            cam.clearFlags = CameraClearFlags.Skybox;

            if (player != null)
            {
                Vector3 p = player.transform.position;
                cam.fieldOfView = 40f;
                Shot(cam, p + new Vector3(1.4f, 1.5f, 4.2f), p + new Vector3(0.3f, 1.2f, 0f), Path.Combine(outDir, "00_rookie_and_bot_closeup.png"));
                cam.fieldOfView = 60f;
            }

            Shot(cam, new Vector3(0f, 150f, -150f), new Vector3(0f, 0f, 0f), Path.Combine(outDir, "02_overview_from_south.png"));
            Shot(cam, new Vector3(0f, 220f, 0f), Quaternion.Euler(90f, 0f, 0f), Path.Combine(outDir, "03_top_down.png"));
            Shot(cam, new Vector3(-30f, 6f, -10f), new Vector3(-60f, 1f, 30f), Path.Combine(outDir, "04_vehicle_graveyard.png"));
            Shot(cam, new Vector3(30f, 6f, -5f), new Vector3(60f, 1f, 28f), Path.Combine(outDir, "05_container_yard.png"));
            Shot(cam, new Vector3(0f, 2f, -20f), new Vector3(0f, 2f, 40f), Path.Combine(outDir, "06_machine_row_gate.png"));
            Shot(cam, new Vector3(0f, 4f, -25f), new Vector3(0f, 3f, -47f), Path.Combine(outDir, "07_base_cluster.png"));

            // Character and vehicle close-ups for the art pass.
            cam.fieldOfView = 35f;
            if (player != null)
            {
                Vector3 p = player.transform.position;
                Shot(cam, p + new Vector3(0.6f, 1.4f, 3.6f), p + new Vector3(0f, 1.15f, 0f), Path.Combine(outDir, "08_rookie_front.png"));
                Shot(cam, p + new Vector3(3.4f, 1.6f, 0.6f), p + new Vector3(0f, 1.15f, 0f), Path.Combine(outDir, "09_rookie_side.png"));
            }
            foreach (var (n, file) in new[] { ("Captain Bolt", "10_bolt.png"), ("Engineer Nova", "11_nova.png"), ("Starter Salvage Truck", "12_truck.png") })
            {
                var t = GameObject.Find(n);
                if (t == null) continue;
                bool truck = file == "12_truck.png";
                Vector3 c = t.transform.position + Vector3.up * (truck ? 1.3f : 1.1f);
                Vector3 eye = c + t.transform.forward * (truck ? 7.5f : 3.4f) + t.transform.right * (truck ? 4.5f : 0.9f) + Vector3.up * (truck ? 1.8f : 0.3f);
                Shot(cam, eye, c, Path.Combine(outDir, file));
            }
            cam.fieldOfView = 60f;

            Object.DestroyImmediate(go);
            EditorSceneManager.CloseScene(scene, false);
            Debug.Log($"[Snapshots] Saved to {outDir}");
        }

        static void Shot(Camera cam, Vector3 pos, Vector3 lookAt, string path) =>
            Shot(cam, pos, Quaternion.LookRotation(lookAt - pos, Vector3.up), path);

        static void Shot(Camera cam, Vector3 pos, Quaternion rot, string path)
        {
            cam.transform.SetPositionAndRotation(pos, rot);
            Render(cam, path);
        }

        static void Render(Camera cam, string path, int width = 1600, int height = 900)
        {
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
    }
}
