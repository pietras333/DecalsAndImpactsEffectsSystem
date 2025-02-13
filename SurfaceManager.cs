using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages surface interactions and effects in the game world.
/// Handles impact effects for different surface types.
/// </summary>
public class SurfaceManager : MonoBehaviour
{
    private static SurfaceManager _instance;

    [SerializeField]
    private List<SurfaceType> surfaces = new List<SurfaceType>();

    [SerializeField]
    private int defaultPoolSize = 10;

    [SerializeField]
    private Surface defaultSurface;

    private Dictionary<Texture, SurfaceType> surfaceCache;

    public static SurfaceManager Instance
    {
        get => _instance;
        private set => _instance = value;
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError(
                $"Multiple SurfaceManager instances detected! Destroying: {gameObject.name}"
            );
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeSurfaceCache();
    }

    private void InitializeSurfaceCache()
    {
        surfaceCache = new Dictionary<Texture, SurfaceType>();
        foreach (var surface in surfaces)
        {
            if (surface.Albedo != null)
            {
                surfaceCache[surface.Albedo] = surface;
            }
        }
    }

    public void HandleImpact(
        GameObject hitObject,
        Vector3 hitPoint,
        Vector3 hitNormal,
        ImpactType impact,
        int triangleIndex
    )
    {
        if (hitObject == null)
        {
            Debug.LogWarning("HandleImpact called with null hitObject");
            return;
        }

        if (hitObject.TryGetComponent<Terrain>(out var terrain))
        {
            HandleTerrainImpact(terrain, hitPoint, hitNormal, impact);
        }
        else if (hitObject.TryGetComponent<Renderer>(out var renderer))
        {
            HandleRendererImpact(renderer, hitPoint, hitNormal, impact, triangleIndex);
        }
    }

    private void HandleTerrainImpact(
        Terrain terrain,
        Vector3 hitPoint,
        Vector3 hitNormal,
        ImpactType impact
    )
    {
        var activeTextures = GetActiveTexturesFromTerrain(terrain, hitPoint);
        foreach (var activeTexture in activeTextures)
        {
            ProcessImpactEffect(
                activeTexture.Texture,
                hitPoint,
                hitNormal,
                impact,
                activeTexture.Alpha
            );
        }
    }

    private void HandleRendererImpact(
        Renderer renderer,
        Vector3 hitPoint,
        Vector3 hitNormal,
        ImpactType impact,
        int triangleIndex
    )
    {
        var activeTexture = GetActiveTextureFromRenderer(renderer, triangleIndex);
        ProcessImpactEffect(activeTexture, hitPoint, hitNormal, impact, 1f);
    }

    private List<TextureAlpha> GetActiveTexturesFromTerrain(Terrain terrain, Vector3 hitPoint)
    {
        Vector3 terrainPosition = hitPoint - terrain.transform.position;
        Vector3 splatMapPosition = new Vector3(
            terrainPosition.x / terrain.terrainData.size.x,
            0,
            terrainPosition.z / terrain.terrainData.size.z
        );

        int x = Mathf.FloorToInt(splatMapPosition.x * terrain.terrainData.alphamapWidth);
        int z = Mathf.FloorToInt(splatMapPosition.z * terrain.terrainData.alphamapHeight);

        float[,,] alphaMap = terrain.terrainData.GetAlphamaps(x, z, 1, 1);
        var activeTextures = new List<TextureAlpha>();

        for (int i = 0; i < alphaMap.Length; i++)
        {
            if (alphaMap[0, 0, i] > 0)
            {
                activeTextures.Add(
                    new TextureAlpha(
                        terrain.terrainData.terrainLayers[i].diffuseTexture,
                        alphaMap[0, 0, i]
                    )
                );
            }
        }

        return activeTextures;
    }

    private Texture GetActiveTextureFromRenderer(Renderer renderer, int triangleIndex)
    {
        if (!renderer.TryGetComponent<MeshFilter>(out var meshFilter))
        {
            Debug.LogError($"{renderer.name} has no MeshFilter! Using default impact effect.");
            return null;
        }

        var mesh = meshFilter.mesh;
        if (mesh.subMeshCount <= 1)
        {
            return renderer.sharedMaterial.mainTexture;
        }

        var hitTriangleIndices = new[]
        {
            mesh.triangles[triangleIndex * 3],
            mesh.triangles[triangleIndex * 3 + 1],
            mesh.triangles[triangleIndex * 3 + 2]
        };

        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            var submeshTriangles = mesh.GetTriangles(i);
            if (IsTriangleInSubmesh(submeshTriangles, hitTriangleIndices))
            {
                return renderer.sharedMaterials[i].mainTexture;
            }
        }

        return renderer.sharedMaterial.mainTexture;
    }

    private bool IsTriangleInSubmesh(int[] submeshTriangles, int[] hitTriangleIndices)
    {
        for (int j = 0; j < submeshTriangles.Length; j += 3)
        {
            if (
                submeshTriangles[j] == hitTriangleIndices[0]
                && submeshTriangles[j + 1] == hitTriangleIndices[1]
                && submeshTriangles[j + 2] == hitTriangleIndices[2]
            )
            {
                return true;
            }
        }
        return false;
    }

    private void ProcessImpactEffect(
        Texture texture,
        Vector3 hitPoint,
        Vector3 hitNormal,
        ImpactType impact,
        float alpha
    )
    {
        if (surfaceCache.TryGetValue(texture, out var surfaceType))
        {
            PlayImpactEffects(surfaceType.Surface, hitPoint, hitNormal, impact, alpha);
        }
        else
        {
            PlayImpactEffects(defaultSurface, hitPoint, hitNormal, impact, alpha);
        }
    }

    private void PlayImpactEffects(
        Surface surface,
        Vector3 hitPoint,
        Vector3 hitNormal,
        ImpactType impact,
        float alpha
    )
    {
        foreach (var effect in surface.ImpactTypeEffects)
        {
            if (effect.ImpactType == impact)
            {
                PlayEffects(hitPoint, hitNormal, effect.SurfaceEffect, alpha);
                break;
            }
        }
    }

    private void PlayEffects(
        Vector3 hitPoint,
        Vector3 hitNormal,
        SurfaceEffect surfaceEffect,
        float soundOffset
    )
    {
        SpawnVisualEffects(hitPoint, hitNormal, surfaceEffect);
        PlayAudioEffects(hitPoint, surfaceEffect, soundOffset);
    }

    private void SpawnVisualEffects(
        Vector3 hitPoint,
        Vector3 hitNormal,
        SurfaceEffect surfaceEffect
    )
    {
        foreach (var spawnObjectEffect in surfaceEffect.SpawnObjectEffects)
        {
            if (!(spawnObjectEffect.Probability > Random.value))
                continue;

            var pool = ObjectPool.CreateInstance(
                spawnObjectEffect.Prefab.GetComponent<PoolableObject>(),
                defaultPoolSize
            );

            var rotation = Quaternion.FromToRotation(Vector3.up, hitNormal);
            var instance = pool.GetObject(hitPoint + hitNormal * 0.001f, rotation);

            if (spawnObjectEffect.RandomizeRotation)
            {
                ApplyRandomRotation(
                    instance.transform,
                    spawnObjectEffect.RandomizedRotationMultiplier
                );
            }
        }
    }

    private void PlayAudioEffects(Vector3 hitPoint, SurfaceEffect surfaceEffect, float soundOffset)
    {
        foreach (var audioEffect in surfaceEffect.PlayAudioEffects)
        {
            var clip = audioEffect.AudioClips[Random.Range(0, audioEffect.AudioClips.Count)];
            var pool = ObjectPool.CreateInstance(
                audioEffect.AudioSourcePrefab.GetComponent<PoolableObject>(),
                defaultPoolSize
            );

            var audioSource = pool.GetObject().GetComponent<AudioSource>();
            audioSource.transform.position = hitPoint;

            var volume =
                soundOffset * Random.Range(audioEffect.VolumeRange.x, audioEffect.VolumeRange.y);
            audioSource.PlayOneShot(clip, volume);

            StartCoroutine(DisableAudioSource(audioSource, clip.length));
        }
    }

    private void ApplyRandomRotation(Transform transform, Vector3 multiplier)
    {
        var offset = new Vector3(
            Random.Range(0, 180 * multiplier.x),
            Random.Range(0, 180 * multiplier.y),
            Random.Range(0, 180 * multiplier.z)
        );

        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + offset);
    }

    private IEnumerator DisableAudioSource(AudioSource audioSource, float delay)
    {
        yield return new WaitForSeconds(delay);
        audioSource.gameObject.SetActive(false);
    }

    private readonly struct TextureAlpha
    {
        public float Alpha { get; }
        public Texture Texture { get; }

        public TextureAlpha(Texture texture, float alpha)
        {
            Texture = texture;
            Alpha = alpha;
        }
    }
}
