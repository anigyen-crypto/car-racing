using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-click scene builder. Menu: Tools > Racing Kit > Build Demo Track.
/// Generates ground, road mesh, walls, checkpoints, start/finish line,
/// player car, AI cars, race manager, chase camera and HUD.
/// Press Play afterwards and you're racing.
/// </summary>
public static class TrackBuilder
{
    const string TrackRootName = "RacingKit_Track";
    const float RoadWidth = 14f;

    [MenuItem("Tools/Racing Kit/Build Demo Track (ground, road, cars, HUD)")]
    public static void Build()
    {
        if (!EditorUtility.DisplayDialog(
                "Build Demo Track",
                "This creates a complete demo race in the current scene.\nAn existing 'RacingKit_Track' object will be replaced.",
                "Build", "Cancel"))
            return;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Build Demo Track");

        var oldRoot = GameObject.Find(TrackRootName);
        if (oldRoot != null) Object.DestroyImmediate(oldRoot);

        var root = new GameObject(TrackRootName);
        Undo.RegisterCreatedObjectUndo(root, "Build Demo Track");

        Vector3[] wps = MakeWaypoints();

        BuildGround(root.transform);
        BuildRoad(root.transform, wps, RoadWidth);
        BuildWalls(root.transform, wps, RoadWidth);
        List<Checkpoint> checkpoints = BuildCheckpoints(root.transform, wps, RoadWidth);

        // ---------- Race Manager ----------
        var raceGo = new GameObject("RaceManager");
        raceGo.transform.SetParent(root.transform);
        var race = raceGo.AddComponent<RaceManager>();
        race.totalLaps = 3;
        race.checkpointCount = checkpoints.Count;
        race.checkpoints = ToTransforms(checkpoints, root.transform);

        // ---------- Shared waypoint transforms (for AI + customization) ----------
        var wpRoot = new GameObject("Waypoints");
        wpRoot.transform.SetParent(root.transform);
        var wpTransforms = new Transform[wps.Length];
        for (int i = 0; i < wps.Length; i++)
        {
            var t = new GameObject("wp_" + i);
            t.transform.SetParent(wpRoot.transform, false);
            t.transform.position = wps[i];
            wpTransforms[i] = t.transform;
        }

        // ---------- Cars ----------
        Vector3 travelDir = (wps[1] - wps[0]).normalized;
        Vector3 rightVec = Vector3.Cross(Vector3.up, travelDir);

        GameObject playerCar = CreateCar("PlayerCar", new Color(0.85f, 0.12f, 0.12f), true, null);
        playerCar.transform.SetParent(root.transform);
        playerCar.transform.position = wps[0] - travelDir * 5f + rightVec * -3f + Vector3.up * 0.8f;
        playerCar.transform.rotation = Quaternion.LookRotation(travelDir);

        GameObject ai1 = CreateCar("AICar_Blue", new Color(0.15f, 0.35f, 0.9f), false, wpTransforms);
        ai1.transform.SetParent(root.transform);
        ai1.transform.position = wps[0] - travelDir * 5f + rightVec * 3f + Vector3.up * 0.8f;
        ai1.transform.rotation = Quaternion.LookRotation(travelDir);

        GameObject ai2 = CreateCar("AICar_Yellow", new Color(0.95f, 0.8f, 0.1f), false, wpTransforms);
        ai2.transform.SetParent(root.transform);
        ai2.transform.position = wps[0] - travelDir * 11f + rightVec * -3f + Vector3.up * 0.8f;
        ai2.transform.rotation = Quaternion.LookRotation(travelDir);

        race.playerCar = playerCar.transform;

        // ---------- Camera ----------
        Camera cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
        }
        cam.fieldOfView = 60f;
        cam.transform.position = new Vector3(0f, 50f, -50f);
        var follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.target = playerCar.transform;
        follow.SnapBehind();

        // ---------- Directional light (use existing one when present) ----------
        Light[] lights = Object.FindObjectsOfType<Light>();
        Light sun = null;
        foreach (Light l in lights)
            if (l.type == LightType.Directional) { sun = l; break; }
        if (sun == null)
        {
            var lightGo = new GameObject("Sun");
            sun = lightGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            sun.intensity = 1f;
        }

        // ---------- HUD ----------
        var hudCanvasGo = new GameObject("RaceHUD");
        hudCanvasGo.transform.SetParent(root.transform);
        var canvas = hudCanvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvasGo.AddComponent<CanvasScaler>();
        hudCanvasGo.AddComponent<GraphicRaycaster>();

        var hud = hudCanvasGo.AddComponent<HUD>();
        hud.speedText = MakeText(hudCanvasGo.transform, "Speed", new Vector2(0f, 0f), new Vector2(25f, 30f), new Vector2(400f, 60f), 34, TextAnchor.LowerLeft);
        hud.lapText = MakeText(hudCanvasGo.transform, "Lap", new Vector2(0f, 1f), new Vector2(25f, -25f), new Vector2(400f, 45f), 28, TextAnchor.UpperLeft);
        hud.timeText = MakeText(hudCanvasGo.transform, "Time", new Vector2(0f, 1f), new Vector2(25f, -75f), new Vector2(400f, 45f), 28, TextAnchor.UpperLeft);
        hud.bestText = MakeText(hudCanvasGo.transform, "Best", new Vector2(0f, 1f), new Vector2(25f, -125f), new Vector2(400f, 45f), 24, TextAnchor.UpperLeft);
        hud.messageText = MakeText(hudCanvasGo.transform, "Message", new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(900f, 200f), 56, TextAnchor.MiddleCenter);
        hud.car = playerCar.GetComponent<CarController>();
        hud.race = race;

        race.hud = hud;

        Selection.activeGameObject = root;
        Debug.Log("[Racing Kit] Demo track built. Press Play to race! (WASD to drive, Space = handbrake, R = reset car)");
    }

    // ---------------------------------------------------------------
    //  Track geometry
    // ---------------------------------------------------------------

    /// <summary>
    /// A wobbly closed loop. Edit these formulas or replace with your own
    /// list of points to design custom tracks.
    /// </summary>
    static Vector3[] MakeWaypoints()
    {
        int n = 20;
        var wps = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.PI * 2f / n;
            float rx = 150f + 25f * Mathf.Sin(a * 2f);
            float rz = 100f + 25f * Mathf.Cos(a * 3f);
            wps[i] = new Vector3(Mathf.Sin(a) * rx, 0f, Mathf.Cos(a) * rz);
        }
        return wps;
    }

    static void BuildGround(Transform root)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(root, false);
        ground.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        ground.transform.localScale = new Vector3(50f, 1f, 50f); // 500 x 500 world units
        ground.GetComponent<Renderer>().sharedMaterial = MakeMat(new Color(0.16f, 0.33f, 0.16f));
        ground.isStatic = true;

        // Outer boundary walls so cars can't drive off the world
        float edge = 252f;
        BuildBoundaryWall(root.transform, new Vector3(edge, 1.25f, 0f), new Vector3(594f, 1f, 6f));
        BuildBoundaryWall(root.transform, new Vector3(-edge, 1.25f, 0f), new Vector3(594f, 1f, 6f));
        BuildBoundaryWall(root.transform, new Vector3(0f, 1.25f, edge), new Vector3(6f, 1f, 594f));
        BuildBoundaryWall(root.transform, new Vector3(0f, 1.25f, -edge), new Vector3(6f, 1f, 594f));
    }

    static void BuildBoundaryWall(Transform parent, Vector3 pos, Vector3 scale)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(box.GetComponent<MeshRenderer>());
        Object.DestroyImmediate(box.GetComponent<MeshFilter>());
        box.name = "BoundaryWall";
        box.transform.SetParent(parent, false);
        box.transform.position = pos;
        box.transform.localScale = scale;
        box.isStatic = true;
    }

    static void BuildRoad(Transform root, Vector3[] wps, float width)
    {
        int n = wps.Length;
        var verts = new Vector3[n * 2];
        var uvs = new Vector2[n * 2];
        var tris = new int[n * 6];

        for (int i = 0; i < n; i++)
        {
            Vector3 prev = wps[(i - 1 + n) % n];
            Vector3 next = wps[(i + 1) % n];
            Vector3 dir = (next - prev).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, dir);

            verts[i * 2] = wps[i] - side * (width * 0.5f) + Vector3.up * 0.02f;
            verts[i * 2 + 1] = wps[i] + side * (width * 0.5f) + Vector3.up * 0.02f;
            uvs[i * 2] = new Vector2(0f, i);
            uvs[i * 2 + 1] = new Vector2(1f, i);
        }

        for (int i = 0; i < n; i++)
        {
            int a = i * 2;
            int b = i * 2 + 1;
            int c = ((i + 1) % n) * 2;
            int d = ((i + 1) % n) * 2 + 1;
            int t = i * 6;
            tris[t] = a; tris[t + 1] = c; tris[t + 2] = b;
            tris[t + 3] = b; tris[t + 4] = c; tris[t + 5] = d;
        }

        var mesh = new Mesh();
        mesh.name = "RoadMesh";
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var road = new GameObject("Road");
        road.transform.SetParent(root, false);
        road.AddComponent<MeshFilter>().sharedMesh = mesh;
        road.AddComponent<MeshRenderer>().sharedMaterial = MakeMat(new Color(0.28f, 0.28f, 0.3f));
        var col = road.AddComponent<MeshCollider>();
        col.sharedMesh = mesh;
        road.isStatic = true;
    }

    static void BuildWalls(Transform root, Vector3[] wps, float width)
    {
        var wallParent = new GameObject("Walls");
        wallParent.transform.SetParent(root);
        int n = wps.Length;

        for (int i = 0; i < n; i++)
        {
            Vector3 a = wps[i];
            Vector3 b = wps[(i + 1) % n];
            Vector3 mid = (a + b) * 0.5f;
            Vector3 dir = (b - a).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            float segLen = Vector3.Distance(a, b) + 1.5f;

            BuildWallBox(wallParent.transform, mid - side * (width * 0.5f + 0.3f), dir, segLen);
            BuildWallBox(wallParent.transform, mid + side * (width * 0.5f + 0.3f), dir, segLen);
        }
    }

    static void BuildWallBox(Transform parent, Vector3 pos, Vector3 dir, float length)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(box.GetComponent<MeshRenderer>());
        Object.DestroyImmediate(box.GetComponent<MeshFilter>());
        box.name = "TrackWall";
        box.transform.SetParent(parent, false);
        box.transform.position = pos + Vector3.up * 1.25f;
        box.transform.rotation = Quaternion.LookRotation(dir);
        box.transform.localScale = new Vector3(0.5f, 2.5f, length);
        box.isStatic = true;
    }

    static List<Checkpoint> BuildCheckpoints(Transform root, Vector3[] wps, float width)
    {
        var list = new List<Checkpoint>();
        int n = wps.Length;

        for (int i = 0; i < n; i += 4) // 5 checkpoints around the loop
        {
            Vector3 here = wps[i];
            Vector3 dir = (wps[(i + 1) % n] - wps[(i - 1 + n) % n]).normalized;

            var go = new GameObject("Checkpoint_" + list.Count);
            go.transform.SetParent(root);
            go.transform.position = here;
            go.transform.rotation = Quaternion.LookRotation(dir);

            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(width + 6f, 4f, 3f);
            col.center = new Vector3(0f, 2f, 0f);

            var cp = go.AddComponent<Checkpoint>();
            cp.index = list.Count;
            list.Add(cp);

            if (i == 0) BuildStartLineVisual(root, here, dir, width);
        }
        return list;
    }

    static void BuildStartLineVisual(Transform root, Vector3 pos, Vector3 travelDir, float width)
    {
        Vector3 side = Vector3.Cross(Vector3.up, travelDir);

        var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.name = "StartFinishLine";
        line.transform.SetParent(root, false);
        line.transform.position = pos + Vector3.up * 0.03f;
        line.transform.rotation = Quaternion.LookRotation(side);
        line.transform.localScale = new Vector3(width + 2f, 0.1f, 2f);
        line.GetComponent<Renderer>().sharedMaterial = MakeMat(new Color(0.1f, 0.75f, 0.2f));
        Object.DestroyImmediate(line.GetComponent<BoxCollider>());
    }

    static Transform[] ToTransforms(List<Checkpoint> checkpoints, Transform parent)
    {
        var arr = new Transform[checkpoints.Count];
        for (int i = 0; i < checkpoints.Count; i++)
        {
            arr[i] = checkpoints[i].transform;
            arr[i].SetParent(parent, true);
        }
        return arr;
    }

    // ---------------------------------------------------------------
    //  Cars
    // ---------------------------------------------------------------

    /// <summary>
    /// Builds a complete drivable car: rigidbody, body + cabin, 4 WheelColliders
    /// with matching wheel meshes, and a driver script (player or AI).
    /// </summary>
    static GameObject CreateCar(string name, Color color, bool player, Transform[] aiWaypoints)
    {
        var car = new GameObject(name);

        var rb = car.AddComponent<Rigidbody>();
        rb.mass = 1200f;
        rb.drag = 0.05f;
        rb.angularDrag = 0.1f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Body
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(car.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        body.transform.localScale = new Vector3(1.8f, 0.6f, 4.2f);
        body.GetComponent<Renderer>().sharedMaterial = MakeMat(color);

        // Cabin
        var cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.name = "Cabin";
        cabin.transform.SetParent(car.transform, false);
        cabin.transform.localPosition = new Vector3(0f, 1.05f, -0.3f);
        cabin.transform.localScale = new Vector3(1.6f, 0.55f, 2f);
        cabin.GetComponent<Renderer>().sharedMaterial = MakeMat(Color.Lerp(color, Color.black, 0.45f));

        // Wheel colliders + visuals
        WheelCollider fl = CreateWheelCollider(car, "WheelFL", new Vector3(-0.85f, 0.4f, 1.45f));
        WheelCollider fr = CreateWheelCollider(car, "WheelFR", new Vector3(0.85f, 0.4f, 1.45f));
        WheelCollider rl = CreateWheelCollider(car, "WheelRL", new Vector3(-0.85f, 0.4f, -1.45f));
        WheelCollider rr = CreateWheelCollider(car, "WheelRR", new Vector3(0.85f, 0.4f, -1.45f));

        Transform mfl = CreateWheelMesh(car, "WheelFL_Mesh", new Vector3(-0.85f, 0.4f, 1.45f));
        Transform mfr = CreateWheelMesh(car, "WheelFR_Mesh", new Vector3(0.85f, 0.4f, 1.45f));
        Transform mrl = CreateWheelMesh(car, "WheelRL_Mesh", new Vector3(-0.85f, 0.4f, -1.45f));
        Transform mrr = CreateWheelMesh(car, "WheelRR_Mesh", new Vector3(0.85f, 0.4f, -1.45f));

        var controller = car.AddComponent<CarController>();
        controller.wheelFL = fl;
        controller.wheelFR = fr;
        controller.wheelRL = rl;
        controller.wheelRR = rr;
        controller.meshFL = mfl;
        controller.meshFR = mfr;
        controller.meshRL = mrl;
        controller.meshRR = mrr;

        if (player)
        {
            car.AddComponent<PlayerInput>();
        }
        else
        {
            var ai = car.AddComponent<AICarController>();
            ai.waypoints = aiWaypoints;
        }

        return car;
    }

    static WheelCollider CreateWheelCollider(GameObject car, string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(car.transform, false);
        go.transform.localPosition = pos;

        var wc = go.AddComponent<WheelCollider>();
        wc.radius = 0.38f;
        wc.suspensionDistance = 0.3f;

        var spring = wc.suspensionSpring;
        spring.spring = 42000f;
        spring.damper = 3800f;
        spring.targetPosition = 0.5f;
        wc.suspensionSpring = spring;

        var fwd = wc.forwardFriction;
        fwd.stiffness = 1.4f;
        wc.forwardFriction = fwd;

        var side = wc.sidewaysFriction;
        side.stiffness = 1.2f;
        wc.sidewaysFriction = side;

        return wc;
    }

    static Transform CreateWheelMesh(GameObject car, string name, Vector3 pos)
    {
        var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cyl.name = name;
        cyl.transform.SetParent(car.transform, false);
        cyl.transform.localPosition = pos;
        cyl.transform.localRotation = Quaternion.Euler(0f, 0f, 90f); // axis along X
        cyl.transform.localScale = new Vector3(0.76f, 0.26f, 0.76f); // diameter 0.76 = 2 x collider radius
        cyl.GetComponent<Renderer>().sharedMaterial = MakeMat(new Color(0.08f, 0.08f, 0.08f));
        Object.DestroyImmediate(cyl.GetComponent<CapsuleCollider>());
        return cyl.transform;
    }

    // ---------------------------------------------------------------
    //  UI helpers
    // ---------------------------------------------------------------

    static Text MakeText(Transform parent, string name, Vector2 anchor, Vector2 anchoredPos,
                         Vector2 size, int fontSize, TextAnchor alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var t = go.AddComponent<Text>();
        t.font = GetFont();
        t.fontSize = fontSize;
        t.alignment = alignment;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;

        var rt = t.rectTransform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = Color.black;
        shadow.effectDistance = new Vector2(2f, -2f);

        return t;
    }

    static Font GetFont()
    {
        try { var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); if (f != null) return f; } catch { }
        try { var f = Resources.GetBuiltinResource<Font>("Arial.ttf"); if (f != null) return f; } catch { }
        return null;
    }

    static Material MakeMat(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        return mat;
    }
}
