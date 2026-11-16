using System.Collections.Generic;
using UnityEngine;

namespace PlanetScrappers.Interaction
{
    /// <summary>Something the player can use with E when standing close: a station, a pickup.</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        [SerializeField] float radius = 2.8f;

        public float Radius => radius;
        public abstract string Prompt { get; }
        public virtual bool CanInteract => isActiveAndEnabled;
        public abstract void Interact();

        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);
    }
}
