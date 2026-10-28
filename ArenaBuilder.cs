using UnityEngine;

public static class ArenaBuilder
{
    public const float W = 52f, L = 80f, H = 18f;
    public const float GoalW = 14f, GoalH = 6f, GoalD = 8f;
    const float Ramp = 4f, Corner = 9f;

    static Material baseMat;
    static PhysicMaterial arenaPhys;
    static Transform root;

    static Material Mat(Color c)
    {
        if (baseMat == null)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseMat = p.GetComponent<Renderer>().sharedMaterial;
            Object.Destroy(p);
        }
        var m = new Material(baseMat) { color = c };
        m.SetFloat("_Glossiness", 0.15f);
        return m;
    }

    static GameObject Block(string name, Vector3 pos, Vector3 size, Quaternion rot, Color col, bool visible = true, bool solid = true)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = name; g.transform.SetParent(root, false);
        g.transform.SetPositionAndRotation(pos, rot);
        g.transform.localScale = size;
        g.layer = GameBootstrap.ArenaLayer;
        var r = g.GetComponent<Renderer>(); r.material = Mat(col);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.enabled = visible;
        var c = g.GetComponent<Collider>();
        if (!solid) Object.Destroy(c); else c.sharedMaterial = arenaPhys;
        return g;
    }

    public static void Build()
    {
        root = new GameObject("Arena").transform;
        arenaPhys = new PhysicMaterial("Arena") { bounciness = 0f, dynamicFriction = 0.3f, staticFriction = 0.3f,
            bounceCombine = PhysicMaterialCombine.Maximum, frictionCombine = PhysicMaterialCombine.Minimum };

        Color floor = new Color(0.10f, 0.20f, 0.35f), wall = new Color(0.20f, 0.30f, 0.45f), line = new Color(0.9f, 0.95f, 1f);
        Color blue = new Color(0.1f, 0.4f, 1f), orange = new Color(1f, 0.5f, 0.1f);
        Quaternion id = Quaternion.identity;
        float hw = W / 2f, hl = L / 2f;

        // floor (extends under both goals) + invisible ceiling
        Block("Floor", new Vector3(0, -0.5f, 0), new Vector3(W, 1, L + 2 * GoalD), id, floor);
        Block("Ceiling", new Vector3(0, H + 0.5f, 0), new Vector3(W + 4, 1, L + 4), id, wall, false);

        // side walls + ramps
        foreach (float sx in new[] { -1f, 1f })
        {
            Block("SideWall", new Vector3(sx * (hw + 0.5f), H / 2f, 0), new Vector3(1, H, L), id, wall);
            Slab(new Vector3(sx * hw, 0, 0), new Vector3(-sx, 0, 0), Ramp, L, floor * 1.4f, true);
        }

        // end walls (with goal opening) + ramps
        float sideW = (W - GoalW) / 2f;
        foreach (float sz in new[] { -1f, 1f })
        {
            foreach (float sx in new[] { -1f, 1f })
            {
                Block("EndWall", new Vector3(sx * (GoalW / 2f + sideW / 2f), H / 2f, sz * (hl + 0.5f)), new Vector3(sideW, H, 1), id, wall);
                Slab(new Vector3(sx * (GoalW / 2f + sideW / 2f), 0, sz * hl), new Vector3(0, 0, -sz), Ramp, sideW, floor * 1.4f, false);
            }
            Block("GoalTop", new Vector3(0, (GoalH + H) / 2f, sz * (hl + 0.5f)), new Vector3(GoalW, H - GoalH, 1), id, wall);

            // goal pocket
            Color gc = sz < 0 ? blue : orange;
            float gz = sz * (hl + GoalD / 2f);
            Block("GoalBack", new Vector3(0, GoalH / 2f, sz * (hl + GoalD + 0.5f)), new Vector3(GoalW + 2, GoalH + 1, 1), id, gc * 0.8f);
            Block("GoalRoof", new Vector3(0, GoalH + 0.5f, gz), new Vector3(GoalW + 2, 1, GoalD), id, gc * 0.6f);
            foreach (float sx in new[] { -1f, 1f })
                Block("GoalSide", new Vector3(sx * (GoalW / 2f + 0.5f), GoalH / 2f, gz), new Vector3(1, GoalH, GoalD), id, gc * 0.7f);

            // glowing goal line
            Block("GoalLine", new Vector3(0, 0.02f, sz * hl), new Vector3(GoalW, 0.04f, 0.4f), id, gc, true, false);

            var trig = new GameObject("GoalTrigger" + (sz < 0 ? "Blue" : "Orange"));
            trig.transform.SetParent(root, false);
            trig.transform.position = new Vector3(0, GoalH / 2f, sz * (hl + GoalD / 2f + 1.0f));
            var bc = trig.AddComponent<BoxCollider>();
            bc.isTrigger = true; bc.size = new Vector3(GoalW, GoalH, GoalD - 2f);
            // ball in the -Z goal (Blue's) scores for Orange and vice versa
            trig.AddComponent<GoalTrigger>().scoringTeam = sz < 0 ? Team.Orange : Team.Blue;
        }

        // corner chamfers (vertical 45 degree walls)
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
            {
                Vector3 mid = new Vector3(sx * (hw - Corner / 2f), H / 2f, sz * (hl - Corner / 2f));
                Vector3 outward = new Vector3(sx, 0, sz).normalized;
                Block("Corner", mid + outward * 0.5f, new Vector3(Corner * 1.4142f, H, 1),
                      Quaternion.Euler(0, 45f * sx * sz, 0), wall);
            }

        // markings
        Block("CenterLine", new Vector3(0, 0.02f, 0), new Vector3(W, 0.04f, 0.3f), id, line, true, false);
        var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.transform.SetParent(root, false); Object.Destroy(ring.GetComponent<Collider>());
        ring.transform.position = new Vector3(0, 0.01f, 0); ring.transform.localScale = new Vector3(16, 0.01f, 16);
        ring.GetComponent<Renderer>().material = Mat(line);
        var ring2 = Object.Instantiate(ring, root);
        ring2.transform.position = new Vector3(0, 0.02f, 0); ring2.transform.localScale = new Vector3(15.4f, 0.01f, 15.4f);
        ring2.GetComponent<Renderer>().material = Mat(floor);

        // boost pads
        Vector3[] small = {
            new Vector3(0,0,-28), new Vector3(0,0,28), new Vector3(-15,0,-14), new Vector3(15,0,-14),
            new Vector3(-15,0,14), new Vector3(15,0,14), new Vector3(-20,0,0), new Vector3(20,0,0),
            new Vector3(0,0,-12), new Vector3(0,0,12) };
        foreach (var p in small) Pad(p, false);
        Vector3[] big = { new Vector3(-21,0,-33), new Vector3(21,0,-33), new Vector3(-21,0,33), new Vector3(21,0,33),
                          new Vector3(-23,0,0), new Vector3(23,0,0) };
        foreach (var p in big) Pad(p, true);
    }

    /// 45-degree ramp slab along a wall. 'inward' points from the wall into the arena.
    static void Slab(Vector3 wallPoint, Vector3 inward, float r, float length, Color col, bool alongZ)
    {
        Vector3 normal = (inward + Vector3.up).normalized;
        Vector3 pos = wallPoint + inward * (r / 2f) + Vector3.up * (r / 2f) - normal * 0.5f;
        float hyp = r * 1.4142f;
        Quaternion rot; Vector3 size;
        if (alongZ) { rot = Quaternion.LookRotation(Vector3.forward, normal); size = new Vector3(hyp, 1f, length); }
        else { rot = Quaternion.LookRotation(Vector3.Cross(Vector3.right, normal), normal); size = new Vector3(length, 1f, hyp); }
        Block("Ramp", pos, size, rot, col);
    }

    static void Pad(Vector3 p, bool big)
    {
        var go = new GameObject(big ? "BigPad" : "Pad");
        go.transform.SetParent(root, false);
        go.transform.position = p + Vector3.up * 0.05f;
        var sc = go.AddComponent<SphereCollider>(); sc.isTrigger = true; sc.radius = big ? 2.6f : 1.8f;
        sc.center = Vector3.up * 0.8f;
        var pad = go.AddComponent<BoostPad>();
        pad.amount = big ? 100f : 12f; pad.respawn = big ? 10f : 4f;

        var v = GameObject.CreatePrimitive(big ? PrimitiveType.Sphere : PrimitiveType.Cylinder);
        Object.Destroy(v.GetComponent<Collider>());
        v.transform.SetParent(go.transform, false);
        v.transform.localPosition = Vector3.up * (big ? 0.9f : 0.1f);
        v.transform.localScale = big ? new Vector3(1.6f, 1.6f, 1.6f) : new Vector3(1.8f, 0.05f, 1.8f);
        v.GetComponent<Renderer>().material = Mat(big ? new Color(1f, 0.8f, 0.1f) : new Color(1f, 0.9f, 0.3f));
        pad.visual = v;
    }
}
