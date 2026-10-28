using UnityEngine;

/// <summary>Put this on one GameObject in an empty scene. Builds the whole match at runtime.</summary>
public class GameBootstrap : MonoBehaviour
{
    public const int ArenaLayer = 8, CarLayer = 9, BallLayer = 10;
    public static int GroundMask => 1 << ArenaLayer;

    static Material baseMat;

    static Material Mat(Color c)
    {
        if (baseMat == null)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseMat = p.GetComponent<Renderer>().sharedMaterial;
            Destroy(p);
        }
        var m = new Material(baseMat) { color = c };
        m.SetFloat("_Glossiness", 0.35f);
        return m;
    }

    static GameObject Part(PrimitiveType t, Transform parent, Vector3 pos, Vector3 scale, Color col, Vector3? euler = null)
    {
        var g = GameObject.CreatePrimitive(t);
        Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos; g.transform.localScale = scale;
        if (euler.HasValue) g.transform.localRotation = Quaternion.Euler(euler.Value);
        g.GetComponent<Renderer>().material = Mat(col);
        g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return g;
    }

    void Awake()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Time.fixedDeltaTime = 1f / 60f;
        Physics.gravity = new Vector3(0f, -18f, 0f);
        Physics.defaultSolverIterations = 10;
        Physics.defaultSolverVelocityIterations = 4;
        Physics.bounceThreshold = 1f;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.66f, 0.78f);
        var lg = new GameObject("Sun").AddComponent<Light>();
        lg.type = LightType.Directional; lg.intensity = 0.9f; lg.shadows = LightShadows.None;
        lg.transform.rotation = Quaternion.Euler(55f, -30f, 0f);

        ArenaBuilder.Build();

        var ball = BuildBall();
        var mgr = new GameObject("GameManager").AddComponent<GameManager>();
        mgr.ball = ball;

        float hl = ArenaBuilder.L / 2f;
        Vector3 blueGoal = new Vector3(0, 0, -hl), orangeGoal = new Vector3(0, 0, hl);
        var controls = new GameObject("Controls").AddComponent<TouchControls>();

        // 2 v 2: you + 1 bot against 2 bots
        mgr.player = BuildCar(Team.Blue, controls, mgr);
        BuildBot(Team.Blue, ball, mgr, orangeGoal, blueGoal);
        BuildBot(Team.Orange, ball, mgr, blueGoal, orangeGoal);
        BuildBot(Team.Orange, ball, mgr, blueGoal, orangeGoal);

        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.06f, 0.12f);
        cam.nearClipPlane = 0.3f; cam.farClipPlane = 250f;
        camGo.AddComponent<AudioListener>();
        var rig = camGo.AddComponent<CameraRig>();
        rig.car = mgr.player.transform; rig.ball = ball.transform;
        camGo.transform.position = new Vector3(0, 6, -35);
    }

    BallController BuildBall()
    {
        var go = new GameObject("Ball") { layer = BallLayer };
        go.transform.position = new Vector3(0, 1.3f, 0);
        var sc = go.AddComponent<SphereCollider>();
        sc.radius = 1.1f;
        sc.sharedMaterial = new PhysicMaterial("Ball") { bounciness = 0.62f, dynamicFriction = 0.35f, staticFriction = 0.35f,
            bounceCombine = PhysicMaterialCombine.Maximum, frictionCombine = PhysicMaterialCombine.Average };
        go.AddComponent<Rigidbody>();
        var vis = Part(PrimitiveType.Sphere, go.transform, Vector3.zero, Vector3.one * 2.2f, new Color(0.95f, 0.95f, 0.98f));
        Color patch = new Color(0.15f, 0.15f, 0.2f);
        foreach (var d in new[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back })
            Part(PrimitiveType.Sphere, vis.transform, d * 0.47f, Vector3.one * 0.32f, patch);
        return go.AddComponent<BallController>();
    }

    CarController BuildCar(Team team, InputProvider provider, GameManager mgr)
    {
        Color body = team == Team.Blue ? new Color(0.12f, 0.42f, 1f) : new Color(1f, 0.5f, 0.08f);
        var go = new GameObject(team + "Car") { layer = CarLayer };
        var bc = go.AddComponent<BoxCollider>();
        bc.center = new Vector3(0, 0.05f, 0); bc.size = new Vector3(1.7f, 0.7f, 3.2f);
        bc.sharedMaterial = new PhysicMaterial("Car") { bounciness = 0f, dynamicFriction = 0.05f, staticFriction = 0.05f,
            frictionCombine = PhysicMaterialCombine.Minimum, bounceCombine = PhysicMaterialCombine.Average };
        go.AddComponent<Rigidbody>();
        var car = go.AddComponent<CarController>();
        car.team = team; car.provider = provider;

        Part(PrimitiveType.Cube, go.transform, new Vector3(0, 0.05f, 0), new Vector3(1.7f, 0.45f, 3.2f), body);
        Part(PrimitiveType.Cube, go.transform, new Vector3(0, 0.42f, -0.15f), new Vector3(1.3f, 0.32f, 1.5f), body * 0.7f);
        Part(PrimitiveType.Cube, go.transform, new Vector3(0, 0.42f, 0.55f), new Vector3(1.2f, 0.22f, 0.15f), new Color(0.6f, 0.85f, 1f));
        Part(PrimitiveType.Cube, go.transform, new Vector3(0, 0.38f, -1.5f), new Vector3(1.6f, 0.08f, 0.45f), body * 0.5f);
        foreach (var w in new[] { new Vector2(0.9f, 1.1f), new Vector2(-0.9f, 1.1f), new Vector2(0.9f, -1.1f), new Vector2(-0.9f, -1.1f) })
            Part(PrimitiveType.Cylinder, go.transform, new Vector3(w.x, 0f, w.y), new Vector3(0.6f, 0.13f, 0.6f), new Color(0.08f, 0.08f, 0.1f), new Vector3(0, 0, 90));
        car.flame = Part(PrimitiveType.Cube, go.transform, new Vector3(0, 0.05f, -2.2f), new Vector3(0.5f, 0.4f, 1.2f), new Color(1f, 0.55f, 0.1f));
        car.flame.SetActive(false);

        mgr.cars.Add(car);
        return car;
    }

    void BuildBot(Team team, BallController ball, GameManager mgr, Vector3 attack, Vector3 own)
    {
        var brain = new GameObject(team + "Bot").AddComponent<BotBrain>();
        var car = BuildCar(team, brain, mgr);
        brain.car = car; brain.ball = ball.transform;
        brain.attackGoal = attack; brain.ownGoal = own;
        brain.skill = 0.8f;
        brain.transform.SetParent(car.transform, false);
    }
}
