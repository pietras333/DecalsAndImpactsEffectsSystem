using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject that defines surface properties and their associated impact effects.
/// Used by the impact system to determine how different types of impacts interact with surfaces.
/// </summary>
[CreateAssetMenu(menuName = "Impact System/Surface", fileName = "Surface")]
public class Surface : ScriptableObject
{
    /// <summary>
    /// Represents the mapping between an impact type and its associated surface effect.
    /// </summary>
    [Serializable]
    public class SurfaceImpactTypeEffect
    {
        [Tooltip("The type of impact that triggers this effect")]
        public ImpactType ImpactType;

        [Tooltip("The surface effect to play when this impact type occurs")]
        public SurfaceEffect SurfaceEffect;

        /// <summary>
        /// Creates a new surface impact type effect mapping.
        /// </summary>
        /// <param name="impactType">The type of impact</param>
        /// <param name="effect">The effect to play</param>
        public SurfaceImpactTypeEffect(ImpactType impactType, SurfaceEffect effect)
        {
            ImpactType = impactType;
            SurfaceEffect = effect;
        }
    }

    /// <summary>
    /// List of impact type to surface effect mappings for this surface.
    /// </summary>
    [SerializeField]
    [Tooltip("Define how different impact types affect this surface")]
    private List<SurfaceImpactTypeEffect> impactTypeEffects = new List<SurfaceImpactTypeEffect>();

    /// <summary>
    /// Gets the list of impact type effects.
    /// </summary>
    public IReadOnlyList<SurfaceImpactTypeEffect> ImpactTypeEffects => impactTypeEffects;

    /// <summary>
    /// Adds a new impact type effect mapping.
    /// </summary>
    /// <param name="impactType">The type of impact</param>
    /// <param name="effect">The effect to play</param>
    public void AddImpactEffect(ImpactType impactType, SurfaceEffect effect)
    {
        impactTypeEffects.Add(new SurfaceImpactTypeEffect(impactType, effect));
    }

    /// <summary>
    /// Gets the surface effect for a specific impact type.
    /// </summary>
    /// <param name="impactType">The type of impact to look up</param>
    /// <returns>The surface effect for the impact type, or null if not found</returns>
    public SurfaceEffect GetEffectForImpactType(ImpactType impactType)
    {
        foreach (var effect in impactTypeEffects)
        {
            if (effect.ImpactType == impactType)
            {
                return effect.SurfaceEffect;
            }
        }
        return null;
    }
}
