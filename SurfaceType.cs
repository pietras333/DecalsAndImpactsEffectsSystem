using UnityEngine;

/// <summary>
/// Represents a surface type with associated texture and surface properties.
/// Used for decal and impact effect systems.
/// </summary>
[System.Serializable]
public class SurfaceType
{
    /// <summary>
    /// The albedo (diffuse) texture for the surface material.
    /// </summary>
    [SerializeField]
    [Tooltip("The main color/texture map for the surface")]
    private Texture albedo;

    /// <summary>
    /// The physical surface properties and characteristics.
    /// </summary>
    [SerializeField]
    [Tooltip("Physical properties of the surface")]
    private Surface surface;

    /// <summary>
    /// Gets or sets the albedo texture.
    /// </summary>
    public Texture Albedo
    {
        get => albedo;
        set => albedo = value;
    }

    /// <summary>
    /// Gets or sets the surface properties.
    /// </summary>
    public Surface Surface
    {
        get => surface;
        set => surface = value;
    }
}
