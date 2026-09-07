using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class GrandPrixMode : MonoBehaviour
{
    public static GrandPrixMode I { get; private set; }

    public enum PowerUp
    {
        None,
        Boost,
        Shield,
        Mine,
        Shock
    }

    [Header("Race")]
    public int lapsToWin = 3;
    public float roundDuration = 240f;

    [Header("Items")]
    public int itemBoxCount = 8;
    public float itemPickupRadius = 1.45f;
    public float itemRespawnTime = 7f;
    public int boostPadCount = 10;
    public float boostPadRadius = 1.7f;
    public float boostPadRespawnTime = 4.5f;
    public float boostPadTime = 1.35f;
    public float boostTime = 3.4f;
    public float boostSpeedMul = 1.38f;
    public float shieldTime = 8f;
    public float shockSlowTime = 4f;
    public float slowSpeedMul = 0.58f;
    public float mineSlowTime = 2.8f;
    public float mineRadius = 1.35f;

    class KartState
    {
        public int lap;
        public int checkpoint;
        public bool finished;
        public float finishTime;
        public PowerUp heldItem;
        public float boostUntil;
        public float shieldUntil;
        public float slowUntil;
        public float nextAiUseAt;
    }

    class ItemBox
    {
        public GameObject root;
        public float respawnAt;
        public bool active = true;
    }

    class BoostPad
    {
        public GameObject root;
        public float respawnAt;
        public bool active = true;
    }

    class Mine
    {
        public GameObject root;
        public int ownerKartIndex;
        public float armedAt;
    }

    KartController[] karts;
    readonly Dictionary<int, KartState> states = new();
    readonly List<ItemBox> itemBoxes = new();
    readonly List<BoostPad> boostPads = new();
    readonly List<Mine> mines = new();

    float roundTimer;
    bool running;

    Material itemBoxMat;
    Material boostPadMat;
    Material boostArrowMat;
    Material mineMat;

    public float RoundTimer => roundTimer;
    public int LapsToWin => Mathf.Max(1, lapsToWin);
    public bool IsRunning => running;
    public int TotalKarts => karts?.Length ?? 0;

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
    }

    public void Init(KartController[] allKarts)
    {
        CleanupItems();

        karts = allKarts;
        states.Clear();
        roundTimer = Mathf.Max(30f, roundDuration);
        running = karts != null && karts.Length > 1;

        if (!running)
        {
            Debug.LogError("GrandPrixMode needs at least two karts.");
            return;
        }

        foreach (var kart in karts)
        {
            if (kart == null) continue;

            int nearest = WaypointCircuit.I != null ? WaypointCircuit.I.Nearest(kart.transform.position) : 0;
            states[kart.kartIndex] = new KartState
            {
                checkpoint = nearest,
                nextAiUseAt = Time.time + Random.Range(1.2f, 3.5f)
            };

            kart.controlsEnabled = true;
            kart.maxSpeedMul = 1f;
            kart.steerMul = 1f;
            kart.SetVisualColor(kart.kartColor);
            kart.GetComponent<KartVisualEffects>()?.SetState(false, false);

            var ai = kart.GetComponent<KartAI>();
            if (ai != null)
            {
                ai.enabled = !kart.isPlayer;
                ai.behaviour = KartAI.Behaviour.FollowTrack;
                ai.target = null;
            }
        }

        SpawnItemBoxes();
        SpawnBoostPads();
        GrandPrixHUD.I?.Refresh();
    }

    void Update()
    {
        SpinItems();
        AnimateBoostPads();

        if (!running) return;

        float now = Time.time;
        roundTimer -= Time.deltaTime;

        UpdateKarts(now);
        UpdateLapProgress(now);
        CheckBoostPads(now);
        CheckItemPickups(now);
        CheckMineHits(now);
        UpdateAIItems(now);

        if (roundTimer <= 0f || AllFinished())
            EndRound();

        GrandPrixHUD.I?.Refresh();
    }

    void UpdateKarts(float now)
    {
        foreach (var kart in karts)
        {
            if (kart == null || !states.TryGetValue(kart.kartIndex, out var state)) continue;

            if (state.finished)
            {
                kart.controlsEnabled = false;
                kart.StopImmediately();
                continue;
            }

            kart.controlsEnabled = true;
            kart.maxSpeedMul = now < state.boostUntil ? boostSpeedMul : 1f;

            if (now < state.slowUntil)
                kart.maxSpeedMul *= slowSpeedMul;

            if (now < state.shieldUntil)
            {
                kart.SetVisualColor(new Color(0.36f, 1f, 0.58f));
                kart.GetComponent<KartVisualEffects>()?.SetState(false, true);
            }
            else if (now < state.boostUntil)
            {
                kart.SetVisualColor(new Color(1f, 0.78f, 0.20f));
                kart.GetComponent<KartVisualEffects>()?.SetState(false, false);
            }
            else if (now < state.slowUntil)
            {
                kart.SetVisualColor(new Color(0.50f, 0.42f, 1f));
                kart.GetComponent<KartVisualEffects>()?.SetState(false, false);
            }
            else
            {
                kart.SetVisualColor(kart.kartColor);
                kart.GetComponent<KartVisualEffects>()?.SetState(false, false);
            }

            if (kart.isPlayer && kart.actionInput)
                UseHeldItem(kart, now);
        }
    }

    void UpdateLapProgress(float now)
    {
        var circuit = WaypointCircuit.I;
        if (circuit == null) return;

        foreach (var kart in karts)
        {
            if (kart == null || !states.TryGetValue(kart.kartIndex, out var state)) continue;
            if (state.finished) continue;

            int nearest = circuit.Nearest(kart.transform.position);
            bool crossedStart = state.checkpoint > WaypointCircuit.Count * 0.72f &&
                nearest < WaypointCircuit.Count * 0.22f;

            if (crossedStart)
            {
                state.lap++;

                if (state.lap >= LapsToWin)
                {
                    state.finished = true;
                    state.finishTime = roundDuration - roundTimer;
                    kart.controlsEnabled = false;
                    kart.StopImmediately();
                    kart.SetVisualColor(new Color(1f, 0.92f, 0.24f));
                }
            }

            state.checkpoint = nearest;
        }
    }

    void CheckItemPickups(float now)
    {
        foreach (var box in itemBoxes)
        {
            if (!box.active && now >= box.respawnAt)
                SetBoxActive(box, true);

            if (!box.active) continue;

            foreach (var kart in karts)
            {
                if (kart == null || !states.TryGetValue(kart.kartIndex, out var state)) continue;
                if (state.finished || state.heldItem != PowerUp.None) continue;

                Vector3 delta = kart.transform.position - box.root.transform.position;
                delta.y = 0f;

                if (delta.sqrMagnitude <= itemPickupRadius * itemPickupRadius)
                {
                    state.heldItem = RandomPowerUp(kart);
                    state.nextAiUseAt = now + Random.Range(0.6f, 2.2f);
                    SetBoxActive(box, false);
                    box.respawnAt = now + itemRespawnTime;
                    GrandPrixHUD.I?.Refresh();
                    break;
                }
            }
        }
    }

    void UpdateAIItems(float now)
    {
        foreach (var kart in karts)
        {
            if (kart == null || kart.isPlayer || !states.TryGetValue(kart.kartIndex, out var state)) continue;
            if (state.finished || state.heldItem == PowerUp.None || now < state.nextAiUseAt) continue;

            UseHeldItem(kart, now);
            state.nextAiUseAt = now + Random.Range(2.5f, 5f);
        }
    }

    void UseHeldItem(KartController kart, float now)
    {
        if (kart == null || !states.TryGetValue(kart.kartIndex, out var state)) return;
        if (state.heldItem == PowerUp.None || state.finished) return;

        PowerUp item = state.heldItem;
        state.heldItem = PowerUp.None;

        switch (item)
        {
            case PowerUp.Boost:
                state.boostUntil = Mathf.Max(state.boostUntil, now + boostTime);
                break;

            case PowerUp.Shield:
                state.shieldUntil = Mathf.Max(state.shieldUntil, now + shieldTime);
                break;

            case PowerUp.Mine:
                SpawnMine(kart, now);
                break;

            case PowerUp.Shock:
                ShockOpponents(kart, now);
                break;
        }

        GrandPrixHUD.I?.Refresh();
    }

    void ShockOpponents(KartController user, float now)
    {
        foreach (var kart in karts)
        {
            if (kart == null || kart == user || !states.TryGetValue(kart.kartIndex, out var state)) continue;
            if (state.finished) continue;

            if (now < state.shieldUntil)
            {
                state.shieldUntil = 0f;
                continue;
            }

            state.slowUntil = Mathf.Max(state.slowUntil, now + shockSlowTime);
        }
    }

    void SpawnMine(KartController owner, float now)
    {
        Vector3 forward = new Vector3(Mathf.Cos(owner.heading), 0f, Mathf.Sin(owner.heading));
        Vector3 pos = owner.transform.position - forward.normalized * 1.65f + Vector3.up * 0.18f;

        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = $"Mine_{owner.kartIndex}";
        root.transform.position = pos;
        root.transform.localScale = Vector3.one * 0.72f;

        var collider = root.GetComponent<Collider>();
        if (collider != null) UnityObjectUtil.Destroy(collider);

        var renderer = root.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material = mineMat != null
                ? mineMat
                : RuntimeMaterials.Emissive(new Color(1f, 0.15f, 0.08f), new Color(1f, 0.05f, 0.02f), 2.8f);

        mines.Add(new Mine
        {
            root = root,
            ownerKartIndex = owner.kartIndex,
            armedAt = now + 0.45f
        });
    }

    void CheckMineHits(float now)
    {
        for (int i = mines.Count - 1; i >= 0; i--)
        {
            var mine = mines[i];
            if (mine.root == null)
            {
                mines.RemoveAt(i);
                continue;
            }

            mine.root.transform.Rotate(Vector3.up, 160f * Time.deltaTime, Space.Self);
            if (now < mine.armedAt) continue;

            foreach (var kart in karts)
            {
                if (kart == null || kart.kartIndex == mine.ownerKartIndex) continue;
                if (!states.TryGetValue(kart.kartIndex, out var state) || state.finished) continue;

                Vector3 delta = kart.transform.position - mine.root.transform.position;
                delta.y = 0f;

                if (delta.sqrMagnitude > mineRadius * mineRadius) continue;

                if (now < state.shieldUntil)
                    state.shieldUntil = 0f;
                else
                {
                    state.slowUntil = Mathf.Max(state.slowUntil, now + mineSlowTime);
                    kart.StopImmediately();
                }

                UnityObjectUtil.Destroy(mine.root);
                mines.RemoveAt(i);
                break;
            }
        }
    }

    void SpawnItemBoxes()
    {
        itemBoxMat = RuntimeMaterials.Emissive(new Color(0.05f, 0.55f, 1f), new Color(0.08f, 0.85f, 1f), 2.6f);
        mineMat = RuntimeMaterials.Emissive(new Color(1f, 0.15f, 0.08f), new Color(1f, 0.05f, 0.02f), 2.8f);

        int count = Mathf.Max(3, itemBoxCount);
        for (int i = 0; i < count; i++)
        {
            float t = 0.11f + (float)i / count * 0.82f;
            Vector3 center = WaypointCircuit.At(t);
            Vector3 next = WaypointCircuit.At(t + 0.01f);
            Vector3 forward = (next - center).normalized;
            Vector3 right = new Vector3(-forward.z, 0f, forward.x);
            float side = i % 2 == 0 ? -1f : 1f;
            Vector3 pos = center + right * side * (WaypointCircuit.HalfWidth * 0.42f) + Vector3.up * 0.42f;

            itemBoxes.Add(new ItemBox
            {
                root = CreateItemBox(pos, i)
            });
        }
    }

    void SpawnBoostPads()
    {
        boostPadMat = RuntimeMaterials.Emissive(new Color(1f, 0.52f, 0.08f), new Color(1f, 0.35f, 0.02f), 2.4f);
        boostArrowMat = RuntimeMaterials.Emissive(new Color(1f, 0.96f, 0.30f), new Color(1f, 0.82f, 0.12f), 2.2f);

        int count = Mathf.Max(2, boostPadCount);
        for (int i = 0; i < count; i++)
        {
            float t = 0.065f + (float)i / count * 0.86f;
            Vector3 center = WaypointCircuit.At(t);
            Vector3 next = WaypointCircuit.At(t + 0.01f);
            Vector3 forward = (next - center).normalized;
            Vector3 right = new Vector3(-forward.z, 0f, forward.x);
            float side = i % 3 == 0 ? 0f : (i % 2 == 0 ? -0.55f : 0.55f);
            Vector3 pos = center + right * side * WaypointCircuit.HalfWidth + Vector3.up * 0.09f;

            boostPads.Add(new BoostPad
            {
                root = CreateBoostPad(pos, forward, i)
            });
        }
    }

    GameObject CreateBoostPad(Vector3 pos, Vector3 forward, int index)
    {
        var root = new GameObject($"Boost Pad {index}");
        root.transform.position = pos;
        root.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

        CreateBoostPadPart(root.transform, "Base", Vector3.zero, new Vector3(2.4f, 0.06f, 1.15f), Quaternion.identity, boostPadMat);
        CreateBoostPadPart(root.transform, "Arrow Left", new Vector3(0f, 0.06f, -0.22f), new Vector3(0.32f, 0.04f, 0.78f), Quaternion.Euler(0f, 28f, 0f), boostArrowMat);
        CreateBoostPadPart(root.transform, "Arrow Right", new Vector3(0f, 0.065f, 0.22f), new Vector3(0.32f, 0.04f, 0.78f), Quaternion.Euler(0f, -28f, 0f), boostArrowMat);

        var lightGo = new GameObject("Boost Pad Light");
        lightGo.transform.SetParent(root.transform, false);
        lightGo.transform.localPosition = Vector3.up * 0.12f;

        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.5f, 0.08f);
        light.range = 4.5f;
        light.intensity = 1.35f;
        light.shadows = LightShadows.None;

        return root;
    }

    GameObject CreateBoostPadPart(Transform parent, string name, Vector3 localPosition, Vector3 scale, Quaternion localRotation, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        go.transform.localScale = scale;

        var collider = go.GetComponent<Collider>();
        if (collider != null) UnityObjectUtil.Destroy(collider);

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material = material;

        return go;
    }

    void AnimateBoostPads()
    {
        float pulse = 0.85f + Mathf.Sin(Time.time * 6f) * 0.15f;
        foreach (var pad in boostPads)
        {
            if (pad.root == null || !pad.active) continue;
            pad.root.transform.localScale = new Vector3(1f, 1f, pulse);
        }
    }

    void CheckBoostPads(float now)
    {
        foreach (var pad in boostPads)
        {
            if (!pad.active && now >= pad.respawnAt)
                SetBoostPadActive(pad, true);

            if (!pad.active || pad.root == null) continue;

            foreach (var kart in karts)
            {
                if (kart == null || !states.TryGetValue(kart.kartIndex, out var state)) continue;
                if (state.finished) continue;

                Vector3 delta = kart.transform.position - pad.root.transform.position;
                delta.y = 0f;

                if (delta.sqrMagnitude > boostPadRadius * boostPadRadius)
                    continue;

                state.boostUntil = Mathf.Max(state.boostUntil, now + boostPadTime);
                kart.speed = Mathf.Max(kart.speed, 7.5f);
                SetBoostPadActive(pad, false);
                pad.respawnAt = now + boostPadRespawnTime;
                break;
            }
        }
    }

    void SetBoostPadActive(BoostPad pad, bool active)
    {
        pad.active = active;
        if (pad.root != null)
            pad.root.SetActive(active);
    }

    GameObject CreateItemBox(Vector3 pos, int index)
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = $"Item Box {index}";
        root.transform.position = pos;
        root.transform.localScale = Vector3.one * 0.72f;

        var collider = root.GetComponent<Collider>();
        if (collider != null) UnityObjectUtil.Destroy(collider);

        var renderer = root.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material = itemBoxMat;

        var lightGo = new GameObject("Item Box Light");
        lightGo.transform.SetParent(root.transform, false);
        lightGo.transform.localPosition = Vector3.zero;

        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.08f, 0.85f, 1f);
        light.range = 4.2f;
        light.intensity = 1.5f;
        light.shadows = LightShadows.None;

        return root;
    }

    void SpinItems()
    {
        foreach (var box in itemBoxes)
        {
            if (box.root == null || !box.active) continue;
            box.root.transform.Rotate(new Vector3(18f, 70f, 32f) * Time.deltaTime, Space.Self);
        }
    }

    void SetBoxActive(ItemBox box, bool active)
    {
        box.active = active;
        if (box.root != null)
            box.root.SetActive(active);
    }

    PowerUp RandomPowerUp(KartController kart)
    {
        int place = PlaceOf(kart.kartIndex);
        float comebackBonus = Mathf.InverseLerp(1f, Mathf.Max(2, TotalKarts), place);
        float roll = Random.value;

        if (roll < 0.30f + comebackBonus * 0.18f) return PowerUp.Boost;
        if (roll < 0.52f) return PowerUp.Mine;
        if (roll < 0.75f) return PowerUp.Shield;
        return PowerUp.Shock;
    }

    bool AllFinished()
    {
        foreach (var state in states.Values)
            if (!state.finished) return false;

        return true;
    }

    void EndRound()
    {
        if (!running) return;

        running = false;
        foreach (var kart in karts)
        {
            if (kart == null) continue;
            kart.controlsEnabled = false;
            kart.StopImmediately();
        }

        GrandPrixHUD.I?.ShowResult(BuildResultMessage());
    }

    string BuildResultMessage()
    {
        List<KartController> ranking = BuildRanking();
        if (ranking.Count == 0) return "Race ended";

        var winner = ranking[0];
        var sb = new StringBuilder();
        sb.Append("Grand Prix Winner: ");
        sb.Append(winner.isPlayer ? "YOU" : $"Kart {winner.kartIndex}");

        for (int i = 0; i < ranking.Count; i++)
        {
            var kart = ranking[i];
            var state = states[kart.kartIndex];
            sb.AppendLine();
            sb.Append($"{i + 1}. {(kart.isPlayer ? "YOU" : $"Kart {kart.kartIndex}")} - Lap {Mathf.Min(state.lap, LapsToWin)}/{LapsToWin}");
        }

        return sb.ToString();
    }

    List<KartController> BuildRanking()
    {
        var ranking = new List<KartController>();
        if (karts == null) return ranking;

        ranking.AddRange(karts);
        ranking.RemoveAll(k => k == null || !states.ContainsKey(k.kartIndex));
        ranking.Sort(CompareRacePosition);
        return ranking;
    }

    int CompareRacePosition(KartController a, KartController b)
    {
        var sa = states[a.kartIndex];
        var sb = states[b.kartIndex];

        if (sa.finished != sb.finished) return sa.finished ? -1 : 1;
        if (sa.finished && sb.finished) return sa.finishTime.CompareTo(sb.finishTime);

        int aProgress = sa.lap * WaypointCircuit.Count + sa.checkpoint;
        int bProgress = sb.lap * WaypointCircuit.Count + sb.checkpoint;
        return bProgress.CompareTo(aProgress);
    }

    void CleanupItems()
    {
        foreach (var box in itemBoxes)
            if (box.root != null) UnityObjectUtil.Destroy(box.root);

        foreach (var pad in boostPads)
            if (pad.root != null) UnityObjectUtil.Destroy(pad.root);

        foreach (var mine in mines)
            if (mine.root != null) UnityObjectUtil.Destroy(mine.root);

        itemBoxes.Clear();
        boostPads.Clear();
        mines.Clear();
    }

    public int ActiveItemBoxCount()
    {
        int count = 0;
        foreach (var box in itemBoxes)
            if (box.root != null && box.active)
                count++;

        return count;
    }

    public int ActiveBoostPadCount()
    {
        int count = 0;
        foreach (var pad in boostPads)
            if (pad.root != null && pad.active)
                count++;

        return count;
    }

    public int PlayerLap(int idx) => states.TryGetValue(idx, out var state) ? Mathf.Min(state.lap + 1, LapsToWin) : 1;
    public bool IsFinished(int idx) => states.TryGetValue(idx, out var state) && state.finished;
    public PowerUp HeldItem(int idx) => states.TryGetValue(idx, out var state) ? state.heldItem : PowerUp.None;

    public int PlaceOf(int idx)
    {
        List<KartController> ranking = BuildRanking();
        for (int i = 0; i < ranking.Count; i++)
            if (ranking[i].kartIndex == idx) return i + 1;

        return Mathf.Max(1, ranking.Count);
    }
}
