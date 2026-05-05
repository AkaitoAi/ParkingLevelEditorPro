using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[System.Serializable]
public class Variant
{
    public GameObject prefab;
    public float weight = 1f;

    public float minScale = 1f;
    public float maxScale = 1f;

    public bool allowRotation = true;
}

[System.Serializable]
public class PropEntry
{
    public string name;
    public GameObject basePrefab;
    public List<Variant> variants = new List<Variant>();

    public bool useVariants = true;

    public bool randomRotation = true;
    public bool randomScale = false;
    public float scaleVariation = 0.1f;
}

public class ParkingLevelEditor : EditorWindow
{
    private int gridWidth = 30;
    private int gridHeight = 30;
    private float cellSize = 2f;

    [SerializeField]
    private List<PropEntry> propEntries = new List<PropEntry>();

    private int selectedIndex = -1;

    private bool drawFullGrid = true;
    private bool dragPaint = true;

    private Transform parentRoot;

    private Vector2 paletteScroll;
    private int columns = 4;

    private Dictionary<Vector2Int, GameObject> placedObjects = new Dictionary<Vector2Int, GameObject>();

    private bool toolEnabled = true;

    // ✅ NEW: prevents repeat actions on same cell
    private Vector2Int lastActionGridPos = new Vector2Int(int.MinValue, int.MinValue);

    [MenuItem("Tools/Parking Level Editor PRO")]
    public static void Open()
    {
        GetWindow<ParkingLevelEditor>("Parking Editor PRO");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;

        RebuildDictionary();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnDestroy()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        GUILayout.Label("Grid Settings", EditorStyles.boldLabel);
        gridWidth = EditorGUILayout.IntField("Width", gridWidth);
        gridHeight = EditorGUILayout.IntField("Height", gridHeight);
        cellSize = EditorGUILayout.FloatField("Cell Size", cellSize);

        drawFullGrid = EditorGUILayout.Toggle("Draw Grid", drawFullGrid);
        dragPaint = EditorGUILayout.Toggle("Drag Paint", dragPaint);

        GUILayout.Space(10);

        // Toggle button
        Color prevColor = GUI.backgroundColor;
        GUI.backgroundColor = toolEnabled ? Color.green : Color.red;

        if (GUILayout.Button(toolEnabled ? "Tool: ON" : "Tool: OFF", GUILayout.Height(30)))
        {
            toolEnabled = !toolEnabled;
            SceneView.RepaintAll();
        }

        GUI.backgroundColor = prevColor;

        GUILayout.Space(10);

        SerializedObject so = new SerializedObject(this);
        so.Update();

        GUILayout.Label("Prop Entries", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(so.FindProperty("propEntries"), true);

        so.ApplyModifiedProperties();

        GUILayout.Space(10);

        GUILayout.Label("Palette Settings", EditorStyles.boldLabel);
        columns = EditorGUILayout.IntSlider("Columns", columns, 1, 8);

        GUILayout.Space(5);

        GUILayout.Label("Prop Palette", EditorStyles.boldLabel);

        paletteScroll = EditorGUILayout.BeginScrollView(paletteScroll, GUILayout.Height(150));

        int rowCount = Mathf.CeilToInt((float)propEntries.Count / columns);

        for (int row = 0; row < rowCount; row++)
        {
            EditorGUILayout.BeginHorizontal();

            for (int col = 0; col < columns; col++)
            {
                int index = row * columns + col;
                if (index >= propEntries.Count) break;

                GUI.backgroundColor = (index == selectedIndex) ? Color.green : Color.white;

                if (GUILayout.Button(propEntries[index].name, GUILayout.Height(40)))
                {
                    selectedIndex = index;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        GUI.backgroundColor = Color.white;

        GUILayout.Space(10);

        parentRoot = (Transform)EditorGUILayout.ObjectField("Parent Root", parentRoot, typeof(Transform), true);

        GUILayout.Space(10);

        if (GUILayout.Button("Rebuild Data"))
            RebuildDictionary();

        if (GUILayout.Button("Clear All"))
            ClearAll();

        if (GUILayout.Button("Remove PropData From Parent Root"))
            RemovePropDataFromChildren();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!toolEnabled) return;

        Handles.BeginGUI();
        GUI.color = Color.green;
        GUILayout.Label("TOOL ACTIVE", EditorStyles.boldLabel);
        Handles.EndGUI();

        Event e = Event.current;

        DrawGrid();

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        Vector3 pos;

        if (Physics.Raycast(ray, out RaycastHit hit))
            pos = hit.point;
        else
        {
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (!groundPlane.Raycast(ray, out float enter)) return;
            pos = ray.GetPoint(enter);
        }

        int x = Mathf.FloorToInt(pos.x / cellSize);
        int y = Mathf.FloorToInt(pos.z / cellSize);

        Vector2Int gridPos = new Vector2Int(x, y);
        Vector3 worldPos = GridToWorld(gridPos);

        Handles.color = Color.green;
        Handles.DrawWireCube(worldPos, Vector3.one * cellSize);

        if (selectedIndex < 0 || selectedIndex >= propEntries.Count) return;

        // RESET on mouse up
        if (e.type == EventType.MouseUp)
        {
            lastActionGridPos = new Vector2Int(int.MinValue, int.MinValue);
        }

        // LEFT CLICK = PLACE
        if (dragPaint && e.type == EventType.MouseDrag && e.button == 0)
        {
            if (lastActionGridPos != gridPos)
            {
                Place(gridPos);
                lastActionGridPos = gridPos;
            }
            e.Use();
        }
        else if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Place(gridPos);
            lastActionGridPos = gridPos;
            e.Use();
        }

        // RIGHT CLICK = REMOVE (WITH DRAG)
        if (dragPaint && e.type == EventType.MouseDrag && e.button == 1)
        {
            if (lastActionGridPos != gridPos)
            {
                Remove(gridPos);
                lastActionGridPos = gridPos;
            }
            e.Use();
        }
        else if (e.type == EventType.MouseDown && e.button == 1)
        {
            Remove(gridPos);
            lastActionGridPos = gridPos;
            e.Use();
        }
    }

    private void DrawGrid()
    {
        if (!drawFullGrid) return;

        Handles.color = new Color(1, 1, 1, 0.1f);

        for (int x = 0; x <= gridWidth; x++)
            Handles.DrawLine(new Vector3(x * cellSize, 0, 0), new Vector3(x * cellSize, 0, gridHeight * cellSize));

        for (int y = 0; y <= gridHeight; y++)
            Handles.DrawLine(new Vector3(0, 0, y * cellSize), new Vector3(gridWidth * cellSize, 0, y * cellSize));
    }

    private GameObject GetPrefab(PropEntry entry, out Variant chosenVariant)
    {
        chosenVariant = null;

        if (!entry.useVariants || entry.variants.Count == 0)
            return entry.basePrefab;

        float total = 0f;
        foreach (var v in entry.variants)
            total += Mathf.Max(0, v.weight);

        float rand = Random.Range(0, total);

        float sum = 0f;
        foreach (var v in entry.variants)
        {
            sum += Mathf.Max(0, v.weight);
            if (rand <= sum)
            {
                chosenVariant = v;
                return v.prefab;
            }
        }

        return entry.basePrefab;
    }

    private void Place(Vector2Int gridPos)
    {
        var entry = propEntries[selectedIndex];

        Variant chosen;
        GameObject prefab = GetPrefab(entry, out chosen);
        if (prefab == null) return;

        PropData data = prefab.GetComponent<PropData>();
        Vector2Int size = data != null ? data.footprint : Vector2Int.one;

        if (!CanPlace(gridPos, size)) return;

        Vector3 worldPos = GridToWorld(gridPos) + (data != null ? data.pivotOffset : Vector3.zero);

        GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(obj, "Place");

        obj.transform.position = worldPos;

        bool allowRotation = entry.randomRotation;
        if (chosen != null) allowRotation &= chosen.allowRotation;

        if (allowRotation)
            obj.transform.rotation = Quaternion.Euler(0, Random.Range(0, 4) * 90f, 0);

        if (entry.randomScale)
        {
            float scale = 1f + Random.Range(-entry.scaleVariation, entry.scaleVariation);

            if (chosen != null)
                scale = Random.Range(chosen.minScale, chosen.maxScale);

            obj.transform.localScale *= scale;
        }

        if (parentRoot != null)
            obj.transform.SetParent(parentRoot);

        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                placedObjects[new Vector2Int(gridPos.x + x, gridPos.y + y)] = obj;
    }

    private bool CanPlace(Vector2Int pos, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                if (placedObjects.ContainsKey(new Vector2Int(pos.x + x, pos.y + y)))
                    return false;

        return true;
    }

    private void Remove(Vector2Int gridPos)
    {
        if (!placedObjects.ContainsKey(gridPos)) return;

        GameObject obj = placedObjects[gridPos];

        List<Vector2Int> toRemove = new List<Vector2Int>();

        foreach (var kvp in placedObjects)
            if (kvp.Value == obj)
                toRemove.Add(kvp.Key);

        foreach (var key in toRemove)
            placedObjects.Remove(key);

        Undo.DestroyObjectImmediate(obj);
    }

    private void ClearAll()
    {
        foreach (var kvp in placedObjects)
            if (kvp.Value != null)
                Undo.DestroyObjectImmediate(kvp.Value);

        placedObjects.Clear();
    }

    private void RemovePropDataFromChildren()
    {
        if (parentRoot == null) return;

        int count = 0;

        foreach (Transform child in parentRoot)
        {
            PropData data = child.GetComponent<PropData>();
            if (data != null)
            {
                Undo.DestroyObjectImmediate(data);
                count++;
            }
        }

        Debug.Log($"Removed PropData from {count} objects.");
    }

    private Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * cellSize + cellSize / 2f, 0, gridPos.y * cellSize + cellSize / 2f);
    }

    private void RebuildDictionary()
    {
        placedObjects.Clear();

        if (parentRoot == null) return;

        foreach (Transform child in parentRoot)
        {
            Vector3 pos = child.position;

            int x = Mathf.FloorToInt(pos.x / cellSize);
            int y = Mathf.FloorToInt(pos.z / cellSize);

            Vector2Int gridPos = new Vector2Int(x, y);

            PropData data = child.GetComponent<PropData>();
            Vector2Int size = data != null ? data.footprint : Vector2Int.one;

            for (int i = 0; i < size.x; i++)
                for (int j = 0; j < size.y; j++)
                    placedObjects[new Vector2Int(gridPos.x + i, gridPos.y + j)] = child.gameObject;
        }
    }
}