using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Tilemap3D : MonoBehaviour
{
    [Header("Grid")]
    public int width = 10;
    public int height = 4;
    public int depth = 10;
    public Vector3 cellSize = Vector3.one;

    [Header("Prefabs")]
    public TilePalette palette; // optional palette asset
    public GameObject singlePrefab; // fallback when palette not used

    [Header("Runtime")]
    public Transform tileParent; // where spawned tiles will be parented

    [Header("Placement")]
    [Tooltip("If true, placed instances will use the prefab's own rotation. Otherwise `placeRotation` is used.")]
    public bool usePrefabRotation = false;
    [Tooltip("Euler rotation applied to placed tiles when `Use Prefab Rotation` is false.")]
    public Vector3 placeRotation = Vector3.zero;

    // internal storage of placed tile GameObjects (editor + runtime)
    private readonly Dictionary<Vector3Int, GameObject> tiles = new Dictionary<Vector3Int, GameObject>();

    void OnValidate()
    {
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);
        depth = Mathf.Max(1, depth);
        cellSize = new Vector3(Mathf.Max(0.01f, cellSize.x), Mathf.Max(0.01f, cellSize.y), Mathf.Max(0.01f, cellSize.z));
    }

    public Vector3 GetWorldPosition(Vector3Int cell)
    {
        return transform.position + new Vector3(cell.x * cellSize.x, cell.y * cellSize.y, cell.z * cellSize.z);
    }

    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        Vector3 local = worldPos - transform.position;
        int x = Mathf.RoundToInt(local.x / cellSize.x);
        int y = Mathf.RoundToInt(local.y / cellSize.y);
        int z = Mathf.RoundToInt(local.z / cellSize.z);
        return new Vector3Int(x, y, z);
    }

    public bool IsInside(Vector3Int c)
    {
        return c.x >= 0 && c.x < width && c.y >= 0 && c.y < height && c.z >= 0 && c.z < depth;
    }

    public bool HasTile(Vector3Int c) => tiles.ContainsKey(c);

    public GameObject PlaceTile(Vector3Int cell, GameObject prefab)
    {
        if (!IsInside(cell) || prefab == null) return null;

        // remove existing
        RemoveTile(cell);

        Transform parent = tileParent != null ? tileParent : transform;
        // rotation: respect `usePrefabRotation` or apply configured Euler rotation
        Quaternion rot = usePrefabRotation ? prefab.transform.rotation : Quaternion.Euler(placeRotation);
        var go = Instantiate(prefab, GetWorldPosition(cell), rot, parent);
        go.name = prefab.name + string.Format("_({0},{1},{2})", cell.x, cell.y, cell.z);

        tiles[cell] = go;
        return go;
    }

    public void RemoveTile(Vector3Int cell)
    {
        if (tiles.TryGetValue(cell, out var go))
        {
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
            tiles.Remove(cell);
        }
    }

    public void Clear()
    {
        var keys = new List<Vector3Int>(tiles.Keys);
        foreach (var k in keys) RemoveTile(k);
    }

    public GameObject GetTile(Vector3Int cell)
    {
        tiles.TryGetValue(cell, out var go);
        return go;
    }

    // helper used by editor (safe to call at edit time)
    public void FillLayer(int layerY, GameObject prefab)
    {
        if (prefab == null) return;
        layerY = Mathf.Clamp(layerY, 0, height - 1);
        for (int x = 0; x < width; x++)
            for (int z = 0; z < depth; z++)
                PlaceTile(new Vector3Int(x, layerY, z), prefab);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.6f, 1f, 0.15f);
        Vector3 size = new Vector3(width * cellSize.x, height * cellSize.y, depth * cellSize.z);
        Gizmos.DrawCube(transform.position + size * 0.5f - new Vector3(0, 0, 0), size);

        Gizmos.color = new Color(0f, 0.6f, 1f, 0.35f);
        // draw grid lines on bottom layer
        for (int x = 0; x <= width; x++)
        {
            Vector3 a = transform.position + new Vector3(x * cellSize.x, 0, 0);
            Vector3 b = a + new Vector3(0, 0, depth * cellSize.z);
            Gizmos.DrawLine(a, b);
        }
        for (int z = 0; z <= depth; z++)
        {
            Vector3 a = transform.position + new Vector3(0, 0, z * cellSize.z);
            Vector3 b = a + new Vector3(width * cellSize.x, 0, 0);
            Gizmos.DrawLine(a, b);
        }
    }
}
