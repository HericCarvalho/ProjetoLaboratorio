using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class VortexSceneSetup
{
    [MenuItem("Vortex/Montar Cena Neuroevolucao")]
    public static void Build()
    {
        int obstacleLayer = EnsureLayer("Obstacle");
        int shipLayer = EnsureLayer("Ship");
        Physics.IgnoreLayerCollision(shipLayer, shipLayer, true);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(0f, 28f, -5f);
            Camera.main.transform.LookAt(new Vector3(0f, 0f, 0f));
        }

        Shader standard = Shader.Find("Standard");

        Material matChao = CreateMat("Assets/Materials/MatChao.mat", standard, new Color(0.18f, 0.2f, 0.24f));
        Material matObs = CreateMat("Assets/Materials/MatObstaculo.mat", standard, new Color(0.75f, 0.25f, 0.25f));
        Material matNave = CreateMat("Assets/Materials/MatNave.mat", standard, new Color(0.2f, 0.7f, 0.95f));
        Material matGoal = CreateMat("Assets/Materials/MatGoal.mat", standard, new Color(0.2f, 0.85f, 0.35f));

        GameObject chao = GameObject.CreatePrimitive(PrimitiveType.Plane);
        chao.name = "Chao";
        chao.transform.localScale = new Vector3(4f, 1f, 3f);
        chao.GetComponent<Renderer>().sharedMaterial = matChao;

        GameObject paredes = new GameObject("Paredes");
        CreateWall(paredes.transform, "ParedeNorte", new Vector3(0f, 1f, 14.5f), new Vector3(40f, 2f, 1f), obstacleLayer, matObs);
        CreateWall(paredes.transform, "ParedeSul", new Vector3(0f, 1f, -14.5f), new Vector3(40f, 2f, 1f), obstacleLayer, matObs);
        CreateWall(paredes.transform, "ParedeLeste", new Vector3(19.5f, 1f, 0f), new Vector3(1f, 2f, 30f), obstacleLayer, matObs);
        CreateWall(paredes.transform, "ParedeOeste", new Vector3(-19.5f, 1f, 0f), new Vector3(1f, 2f, 30f), obstacleLayer, matObs);

        GameObject spawn = new GameObject("SpawnPoint");
        spawn.transform.position = new Vector3(-12f, 0.5f, 0f);
        spawn.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        GameObject goal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        goal.name = "Goal";
        goal.transform.position = new Vector3(12f, 0.5f, 0f);
        goal.transform.localScale = new Vector3(1.6f, 0.5f, 1.6f);
        goal.GetComponent<Collider>().isTrigger = true;
        goal.GetComponent<Renderer>().sharedMaterial = matGoal;

        GameObject shipObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shipObj.name = "Ship";
        shipObj.layer = shipLayer;
        shipObj.transform.localScale = new Vector3(0.7f, 0.4f, 1.1f);
        shipObj.GetComponent<Renderer>().sharedMaterial = matNave;
        Rigidbody rb = shipObj.AddComponent<Rigidbody>();
        rb.useGravity = false;
        EvolvingShip shipComp = shipObj.AddComponent<EvolvingShip>();
        SerializedObject shipSo = new SerializedObject(shipComp);
        shipSo.FindProperty("obstacleMask").intValue = 1 << obstacleLayer;
        shipSo.ApplyModifiedPropertiesWithoutUndo();

        EvolvingShip shipPrefab = PrefabUtility.SaveAsPrefabAsset(shipObj, "Assets/Prefabs/Ship.prefab").GetComponent<EvolvingShip>();
        Object.DestroyImmediate(shipObj);

        GameObject c1 = new GameObject("Cenario1");
        CreateWall(c1.transform, "Obs1", new Vector3(-3f, 1f, 1.5f), new Vector3(2f, 2f, 5f), obstacleLayer, matObs);
        CreateWall(c1.transform, "Obs2", new Vector3(4f, 1f, -2f), new Vector3(2f, 2f, 5f), obstacleLayer, matObs);

        GameObject c2 = new GameObject("Cenario2");
        CreateWall(c2.transform, "Obs1", new Vector3(-2f, 1f, -1f), new Vector3(2f, 2f, 6f), obstacleLayer, matObs);
        CreateWall(c2.transform, "Obs2", new Vector3(5f, 1f, 3.5f), new Vector3(2.5f, 2f, 5f), obstacleLayer, matObs);
        c2.SetActive(false);

        GameObject c3 = new GameObject("Cenario3");
        CreateWall(c3.transform, "Obs1", new Vector3(-4f, 1f, -3f), new Vector3(2f, 2f, 6f), obstacleLayer, matObs);
        CreateWall(c3.transform, "Obs2", new Vector3(3f, 1f, 2.5f), new Vector3(2f, 2f, 7f), obstacleLayer, matObs);
        c3.SetActive(false);

        GameObject pathObj = new GameObject("Pathfinder");
        LineRenderer lr = pathObj.AddComponent<LineRenderer>();
        lr.startWidth = 0.15f;
        lr.endWidth = 0.15f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.yellow;
        lr.endColor = Color.yellow;
        AStarPathfinder pathfinder = pathObj.AddComponent<AStarPathfinder>();
        SerializedObject pathSo = new SerializedObject(pathfinder);
        pathSo.FindProperty("obstacleMask").intValue = 1 << obstacleLayer;
        pathSo.FindProperty("lineRenderer").objectReferenceValue = lr;
        pathSo.ApplyModifiedPropertiesWithoutUndo();

        GameObject mgrObj = new GameObject("EvolutionManager");
        EvolutionManager mgr = mgrObj.AddComponent<EvolutionManager>();
        SerializedObject mgrSo = new SerializedObject(mgr);
        mgrSo.FindProperty("shipPrefab").objectReferenceValue = shipPrefab;
        mgrSo.FindProperty("spawnPoint").objectReferenceValue = spawn.transform;
        mgrSo.FindProperty("goal").objectReferenceValue = goal.transform;
        mgrSo.FindProperty("pathfinder").objectReferenceValue = pathfinder;

        SerializedProperty scenProp = mgrSo.FindProperty("scenarios");
        scenProp.arraySize = 3;
        scenProp.GetArrayElementAtIndex(0).objectReferenceValue = c1;
        scenProp.GetArrayElementAtIndex(1).objectReferenceValue = c2;
        scenProp.GetArrayElementAtIndex(2).objectReferenceValue = c3;

        SerializedProperty goalProp = mgrSo.FindProperty("scenarioGoalPositions");
        goalProp.arraySize = 3;
        goalProp.GetArrayElementAtIndex(0).vector3Value = new Vector3(12f, 0.5f, 0f);
        goalProp.GetArrayElementAtIndex(1).vector3Value = new Vector3(12f, 0.5f, 5f);
        goalProp.GetArrayElementAtIndex(2).vector3Value = new Vector3(12f, 0.5f, -5f);

        mgrSo.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Vortex.unity");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Cena Vortex montada com sucesso.");
    }

    static Material CreateMat(string path, Shader shader, Color color)
    {
        Material mat = new Material(shader) { color = color };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void CreateWall(Transform parent, string name, Vector3 pos, Vector3 scale, int layer, Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.layer = layer;
        obj.transform.SetParent(parent);
        obj.transform.position = pos;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static int EnsureLayer(string layerName)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty sp = layers.GetArrayElementAtIndex(i);
            if (sp.stringValue == layerName)
                return i;
        }
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty sp = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(sp.stringValue))
            {
                sp.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }
        return 0;
    }
}
