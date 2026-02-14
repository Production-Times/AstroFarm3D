using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Tilemap3D/Tile Palette", fileName = "TilePalette")]
public class TilePalette : ScriptableObject
{
    [Tooltip("Prefabs available to paint into the 3D tilemap")]
    public List<GameObject> prefabs = new List<GameObject>();
}
