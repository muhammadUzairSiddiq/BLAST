using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelCatalog", menuName = "BLAST/Level Catalog")]
public class LevelCatalog : ScriptableObject
{
    [SerializeField] private List<GameObject> levelRoots = new List<GameObject>();

    public int Count => levelRoots.Count;

    public Level GetLevel(int index)
    {
        if (index < 0 || index >= levelRoots.Count)
            return null;

        GameObject root = levelRoots[index];
        return root != null ? root.GetComponent<Level>() : null;
    }
}
