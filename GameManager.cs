using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager I;
    public enum State { Countdown, Play, GoalPause, Over }

    public List<CarController> cars = new List<CarController>();
    public CarController player;
    public BallController ball;

    public State state = State.Countdown;
    public bool CarsActive => state != State.Countdown;
    int blue, orange;
    float timeLeft = 300f, timer = 3f;
    bool overtime;
    string banner = "";
    Team lastScorer;
    GUIStyle big, mid;

    void Awake() { I = this; }
    void Start() { Kickoff(); }

    void Kickoff()
    {
        ball.ResetBall();
        int[] idx = { 0, 0 };
        foreach (var c in cars)
        {
            int n = idx[(int)c.team]++;
            float sz = c.team == Team.Blue ? -1f : 1f;
            float x = n == 0 ? 0f : (n % 2 == 1 ? 11f : -11f);
            float z = sz * (n == 0 ? 26f : 22f);
            c.ResetTo(new Vector3(x, 0.4f, z), Quaternion.Euler(0, c.team == Team.Blue ? 0 : 180, 0));
        }
        state = State.Countdown; timer = 3.2f;
    }

    void Update()
    {
        switch (state)
        {
            case State.Countdown:
                timer -= Time.deltaTime;
                banner = Mathf.Max(1, Mathf.CeilToInt(timer)).ToString();
                if (timer <= 0f) { state = State.Play; banner = "GO!"; timer = 0.8f; }
                break;
            case State.Play:
                if (banner == "GO!") { timer -= Time.deltaTime; if (timer <= 0f) banner = ""; }
                if (!overtime)
                {
                    timeLeft -= Time.deltaTime;
                    if (timeLeft <= 0f)
                    {
                        timeLeft = 0f;
                        if (blue == orange) { overtime = true; banner = "OVERTIME!"; timer = 1.5f; }
                        else EndMatch();
                    }
                }
                else if (banner == "OVERTIME!") { timer -= Time.deltaTime; if (timer <= 0f) banner = ""; }
                AssignRoles();
                break;
            case State.GoalPause:
                timer -= Time.deltaTime;
                if (timer <= 0f) Kickoff();
                break;
            case State.Over:
                if (Input.GetKeyDown(KeyCode.Return) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began && timer <= 0f)) Restart();
                timer -= Time.deltaTime;
                break;
        }
    }

    void AssignRoles()
    {
        for (int t = 0; t < 2; t++)
        {
            CarController best = null; float bd = float.MaxValue;
            foreach (var c in cars)
            {
                if ((int)c.team != t) continue;
                float d = (c.transform.position - ball.transform.position).sqrMagnitude;
                if (d < bd) { bd = d; best = c; }
            }
            foreach (var c in cars)
            {
                if ((int)c.team != t) continue;
                var bot = c.provider as BotBrain;
                if (bot != null) bot.isChaser = c == best;
            }
        }
    }

    public void OnGoal(Team scorer)
    {
        if (state != State.Play) return;
        if (scorer == Team.Blue) blue++; else orange++;
        lastScorer = scorer;
        banner = (scorer == Team.Blue ? "BLUE" : "ORANGE") + " GOAL!";
        if (overtime) { EndMatch(); return; }
        state = State.GoalPause; timer = 3f;
    }

    void EndMatch()
    {
        state = State.Over; timer = 1.5f;
        banner = blue > orange ? "BLUE WINS!" : "ORANGE WINS!";
    }

    void Restart()
    {
        blue = orange = 0; timeLeft = 300f; overtime = false; banner = "";
        Kickoff();
    }

    void OnGUI()
    {
        float H = Screen.height, W = Screen.width, u = H * 0.1f;
        if (big == null)
        {
            big = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            mid = new GUIStyle(big);
        }
        big.fontSize = Mathf.RoundToInt(u * 1.5f);
        mid.fontSize = Mathf.RoundToInt(u * 0.6f);

        // scoreboard
        GUI.color = new Color(0, 0, 0, 0.45f);
        GUI.DrawTexture(new Rect(W / 2f - 4.2f * u, 0.15f * u, 8.4f * u, 1.1f * u), Texture2D.whiteTexture);
        GUI.color = new Color(0.3f, 0.6f, 1f);
        GUI.Label(new Rect(W / 2f - 4.2f * u, 0.15f * u, 2.1f * u, 1.1f * u), blue.ToString(), mid);
        GUI.color = new Color(1f, 0.6f, 0.2f);
        GUI.Label(new Rect(W / 2f + 2.1f * u, 0.15f * u, 2.1f * u, 1.1f * u), orange.ToString(), mid);
        GUI.color = Color.white;
        int s = Mathf.CeilToInt(timeLeft);
        string tm = overtime ? "OT" : (s / 60) + ":" + (s % 60).ToString("00");
        GUI.Label(new Rect(W / 2f - 2.1f * u, 0.15f * u, 4.2f * u, 1.1f * u), tm, mid);

        // boost bar (bottom centre-right of stick)
        if (player != null)
        {
            float bw = 3.4f * u, bh = 0.35f * u, bx = W / 2f - bw / 2f, by = H - 0.7f * u;
            GUI.color = new Color(0, 0, 0, 0.5f); GUI.DrawTexture(new Rect(bx, by, bw, bh), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.65f, 0.1f);
            GUI.DrawTexture(new Rect(bx, by, bw * player.boost / CarController.MaxBoost, bh), Texture2D.whiteTexture);
            GUI.color = Color.white;
            mid.fontSize = Mathf.RoundToInt(u * 0.4f);
            GUI.Label(new Rect(bx, by - 0.6f * u, bw, 0.6f * u), Mathf.RoundToInt(player.boost) + "  |  " + Mathf.RoundToInt(player.Speed * 3.6f) + " km/h", mid);
        }

        if (!string.IsNullOrEmpty(banner))
        {
            GUI.color = state == State.GoalPause && lastScorer == Team.Orange ? new Color(1f, 0.6f, 0.2f)
                      : state == State.GoalPause ? new Color(0.3f, 0.6f, 1f) : Color.white;
            GUI.Label(new Rect(0, H * 0.18f, W, 2f * u), banner, big);
            GUI.color = Color.white;
            if (state == State.Over)
            {
                mid.fontSize = Mathf.RoundToInt(u * 0.5f);
                GUI.Label(new Rect(0, H * 0.18f + 2f * u, W, u), "Tap to play again", mid);
            }
        }
    }
}
