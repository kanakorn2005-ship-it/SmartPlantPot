using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SmartPlantPot
{
    /// <summary>
    /// Polls the backend (or an ESP32 directly) for readings. Disable the simulator and enable this
    /// when the hardware is ready. Accepted JSON (see README):
    ///   {"pots":[{"id":"pot-01","moisture":35,"temperature":29,"light":520}, ...]}
    ///   [{"id":"pot-01", ...}, ...]
    ///   {"id":"pot-01","moisture":35,"temperature":29,"light":520}   (single pot)
    /// Care commands are POSTed as {"id":"pot-01","action":"water"}.
    /// </summary>
    [AddComponentMenu("Smart Plant Pot/HTTP Plant Data Source")]
    public class HttpPlantDataSource : PlantDataSource
    {
        [Tooltip("GET endpoint that returns the latest readings as JSON.")]
        public string readingsUrl = "http://192.168.1.50/api/pots";

        [Tooltip("Optional POST endpoint for care commands. Leave empty to skip.")]
        public string commandUrl = "";

        [Tooltip("Seconds between polls.")]
        public float pollInterval = 2f;

        public int timeoutSeconds = 5;

        [Tooltip("If the endpoint returns one pot without an id (e.g. ESP32 /data), apply it to this plant.")]
        public PlantUnit singlePotTarget;

        bool connected;
        string lastError = "";

        static readonly HashSet<PlantUnit> realPots = new HashSet<PlantUnit>();
        static bool sourceEnabled;

        protected override int Priority
        {
            get { return 10; }
        }

        /// <summary>True for pots the real sensors report (the simulator leaves these alone).</summary>
        public static bool IsReal(PlantUnit plant)
        {
            return sourceEnabled && plant != null && realPots.Contains(plant);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetHttpStatics()
        {
            realPots.Clear();
            sourceEnabled = false;
        }

        public override bool IsConnected { get { return connected; } }
        public override string ConnectionLabel { get { return connected ? L.Online : L.Offline; } }
        public string LastError { get { return lastError; } }

#pragma warning disable 0649 // fields are filled by JsonUtility
        [Serializable]
        class PotJson
        {
            public string id;
            public float moisture;
            public float temperature;
            public float light;
            public float humidity = -1f;     // optional (DHT air humidity)
        }

        [Serializable]
        class PotListJson
        {
            public PotJson[] pots;
        }

#pragma warning restore 0649

        [Serializable]
        class CommandJson
        {
            public string id;
            public string action;
        }

        [Serializable]
        class ServoJson
        {
            public string id;
            public string action;
            public float angle;
            public float seconds;
        }

        // Restart polling every time the source is switched on (Mock panel toggles it).
        protected override void OnEnable()
        {
            base.OnEnable();
            sourceEnabled = true;
            StartCoroutine(PollLoop());
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            sourceEnabled = false;
            realPots.Clear();
            EnvironmentState.SensorLive = false;
            StopAllCoroutines();
            if (connected)
            {
                connected = false;
                RaiseConnectionChanged();
            }
        }

        IEnumerator PollLoop()
        {
            while (true)
            {
                yield return Poll();
                yield return new WaitForSecondsRealtime(Mathf.Max(0.25f, pollInterval));
            }
        }

        /// <summary>Poll once right now (Mock panel "test connection").</summary>
        public void PollNow()
        {
            if (isActiveAndEnabled) StartCoroutine(Poll());
        }

        IEnumerator Poll()
        {
            using (var req = UnityWebRequest.Get(readingsUrl))
            {
                req.timeout = timeoutSeconds;
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    SetConnected(false, req.error);
                    yield break;
                }

                try
                {
                    Apply(req.downloadHandler.text);
                    SetConnected(true, "");
                }
                catch (Exception e)
                {
                    SetConnected(false, "Bad JSON: " + e.Message);
                }
            }
        }

        void Apply(string json)
        {
            json = json.Trim();
            if (json.StartsWith("[")) json = "{\"pots\":" + json + "}";

            roomTemps.Clear();
            roomLux.Clear();
            roomHumidity.Clear();

            var list = JsonUtility.FromJson<PotListJson>(json);
            if (list != null && list.pots != null && list.pots.Length > 0)
            {
                foreach (var p in list.pots)
                    ApplyTo(Find(p.id), p);
            }
            else
            {
                var one = JsonUtility.FromJson<PotJson>(json);
                if (one == null) return;
                var target = Find(one.id);
                if (target == null) target = singlePotTarget;
                if (target == null && PlantUnit.All.Count > 0) target = PlantUnit.All[0];
                ApplyTo(target, one);
            }

            // The pots share one room: publish what the sensors measure so the scene lighting,
            // the header and the simulated pots follow the real room instead of the weather API.
            if (roomTemps.Count > 0) EnvironmentState.SensorTemperature = Average(roomTemps);
            if (roomLux.Count > 0) EnvironmentState.SensorLux = Average(roomLux);
            if (roomHumidity.Count > 0) EnvironmentState.SensorHumidity = Average(roomHumidity);
            EnvironmentState.SensorLive = roomTemps.Count > 0 || roomLux.Count > 0;
        }

        readonly List<float> roomTemps = new List<float>();
        readonly List<float> roomLux = new List<float>();
        readonly List<float> roomHumidity = new List<float>();

        void ApplyTo(PlantUnit plant, PotJson p)
        {
            if (plant == null || p == null) return;
            realPots.Add(plant);
            plant.ApplyReading(new SensorReading(p.moisture, p.temperature, p.light));
            // 0 °C with no humidity is what the sketch sends before the DHT11 has a first reading
            bool tempOk = p.temperature > -15f && p.temperature < 70f && !(p.temperature == 0f && p.humidity <= 0f);
            if (tempOk) roomTemps.Add(p.temperature);
            if (p.light >= 0f) roomLux.Add(p.light);
            if (p.humidity > 0f) roomHumidity.Add(p.humidity);
        }

        static float Average(List<float> values)
        {
            float sum = 0f;
            foreach (var v in values) sum += v;
            return sum / values.Count;
        }

        static PlantUnit Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var p in PlantUnit.All)
                if (string.Equals(p.potId, id, StringComparison.OrdinalIgnoreCase)) return p;
            return null;
        }

        void SetConnected(bool value, string error)
        {
            lastError = error;
            if (!value) EnvironmentState.SensorLive = false;   // fall back to the weather while offline
            if (!value && !string.IsNullOrEmpty(error)) Debug.LogWarning("[SmartPlantPot] " + error);
            if (connected == value) return;
            connected = value;
            RaiseConnectionChanged();
        }

        public override void SendCareCommand(PlantUnit plant, CareAction action)
        {
            if (plant == null || string.IsNullOrEmpty(commandUrl)) return;
            string a = action == CareAction.Water ? "water" : action == CareAction.Fertilize ? "fertilize" : "prune";
            StartCoroutine(PostCommand(plant.potId, a));
        }

        public override void SendServo(PlantUnit plant, float angle, float seconds)
        {
            if (plant == null || string.IsNullOrEmpty(commandUrl)) return;
            var body = JsonUtility.ToJson(new ServoJson { id = plant.potId, action = "servo", angle = angle, seconds = seconds });
            StartCoroutine(PostJson(body));
        }

        IEnumerator PostCommand(string id, string action)
        {
            return PostJson(JsonUtility.ToJson(new CommandJson { id = id, action = action }));
        }

        IEnumerator PostJson(string body)
        {
            using (var req = new UnityWebRequest(commandUrl, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = timeoutSeconds;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                    Debug.LogWarning("[SmartPlantPot] Care command failed: " + req.error);
            }
        }
    }
}
