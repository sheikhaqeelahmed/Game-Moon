using System;
using System.Collections.Generic;
using PlanetScrappers.Core;
using UnityEngine;

namespace PlanetScrappers.Equipment
{
    /// <summary>
    /// Which tools the player owns and which is fitted to the salvage arm. Builds a simple
    /// placeholder model for the fitted tool on the arm's tool mount.
    /// </summary>
    public class PlayerEquipment : MonoBehaviour
    {
        public static PlayerEquipment Instance { get; private set; }

        [SerializeField] Transform toolMount;
        [SerializeField] Material rustMaterial, metalMaterial, darkMaterial, glowMaterial;

        readonly HashSet<string> owned = new HashSet<string>();
        GameObject toolModel;

        public ToolDefinition Equipped { get; private set; }
        public event Action Changed;
        public IReadOnlyCollection<string> OwnedIds => owned;

        void Awake() => Instance = this;

        public bool Owns(ToolDefinition tool) => tool != null && owned.Contains(tool.id);

        public void Grant(ToolDefinition tool, bool equip)
        {
            if (tool == null) return;
            owned.Add(tool.id);
            if (equip) Equip(tool);
            else Changed?.Invoke();
        }

        public void Equip(ToolDefinition tool)
        {
            if (tool == null || !Owns(tool)) return;
            Equipped = tool;
            BuildModel(tool);
            Changed?.Invoke();
        }

        /// <summary>The owned tool with the given level (1 = wrench), for the 1/2/3 hotkeys.</summary>
        public ToolDefinition OwnedToolAtLevel(int level)
        {
            foreach (var t in GameDatabase.Instance.tools)
                if (t != null && t.level == level && Owns(t)) return t;
            return null;
        }

        public void ClearAll()
        {
            owned.Clear();
            Equipped = null;
            if (toolModel != null) Destroy(toolModel);
            Changed?.Invoke();
        }

        void BuildModel(ToolDefinition tool)
        {
            if (toolModel != null) Destroy(toolModel);
            if (toolMount == null) return;
            toolModel = new GameObject("Tool: " + tool.displayName);
            toolModel.transform.SetParent(toolMount, false);
            toolModel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // point forward from the hand

            switch (tool.id)
            {
                case "rusty_wrench":
                    Part(PrimitiveType.Cube, new Vector3(0f, 0f, 0.28f), new Vector3(0.06f, 0.04f, 0.56f), rustMaterial);
                    Part(PrimitiveType.Cube, new Vector3(-0.05f, 0f, 0.6f), new Vector3(0.04f, 0.04f, 0.14f), rustMaterial);
                    Part(PrimitiveType.Cube, new Vector3(0.05f, 0f, 0.6f), new Vector3(0.04f, 0.04f, 0.14f), rustMaterial);
                    break;
                case "scrap_hammer":
                    Part(PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.3f), new Vector3(0.05f, 0.3f, 0.05f), darkMaterial, new Vector3(90f, 0f, 0f));
                    Part(PrimitiveType.Cube, new Vector3(0f, 0f, 0.62f), new Vector3(0.3f, 0.14f, 0.16f), metalMaterial);
                    Part(PrimitiveType.Cube, new Vector3(0.12f, 0f, 0.62f), new Vector3(0.08f, 0.16f, 0.18f), rustMaterial);
                    break;
                case "electric_dismantler":
                    Part(PrimitiveType.Cube, new Vector3(0f, 0f, 0.22f), new Vector3(0.14f, 0.14f, 0.36f), darkMaterial);
                    Part(PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.5f), new Vector3(0.05f, 0.12f, 0.05f), metalMaterial, new Vector3(90f, 0f, 0f));
                    Part(PrimitiveType.Sphere, new Vector3(0f, 0f, 0.64f), Vector3.one * 0.08f, glowMaterial);
                    break;
                case "plasma_cutter":
                    Part(PrimitiveType.Cube, new Vector3(0f, 0f, 0.25f), new Vector3(0.16f, 0.16f, 0.42f), darkMaterial);
                    Part(PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.56f), new Vector3(0.07f, 0.12f, 0.07f), metalMaterial, new Vector3(90f, 0f, 0f));
                    Part(PrimitiveType.Sphere, new Vector3(0f, 0f, 0.72f), Vector3.one * 0.12f, glowMaterial);
                    break;
                case "industrial_crusher":
                    Part(PrimitiveType.Cube, new Vector3(0f, 0f, 0.3f), new Vector3(0.2f, 0.2f, 0.5f), darkMaterial);
                    Part(PrimitiveType.Cube, new Vector3(-0.12f, 0f, 0.68f), new Vector3(0.1f, 0.34f, 0.34f), metalMaterial);
                    Part(PrimitiveType.Cube, new Vector3(0.12f, 0f, 0.68f), new Vector3(0.1f, 0.34f, 0.34f), metalMaterial);
                    Part(PrimitiveType.Cube, new Vector3(0f, 0.12f, 0.45f), new Vector3(0.26f, 0.08f, 0.2f), rustMaterial);
                    break;
                default:
                    Part(PrimitiveType.Cube, new Vector3(0f, 0f, 0.3f), new Vector3(0.12f, 0.12f, 0.5f), metalMaterial);
                    break;
            }
        }

        void Part(PrimitiveType type, Vector3 pos, Vector3 scale, Material mat, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(toolModel.transform, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }
}
