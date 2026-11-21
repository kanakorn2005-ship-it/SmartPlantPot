#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SmartPlantPot
{
    /// <summary>
    /// Editor-only debugging aid: when the file Logs/clickprobe.on contains the word "on", every
    /// pointer press and release logs what the EventSystem hits under the pointer (handy when a
    /// tap seems to do nothing). Does nothing otherwise and is never part of a build.
    /// </summary>
    public class ClickProbe : MonoBehaviour
    {
        static readonly List<RaycastResult> hits = new List<RaycastResult>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            const string flag = "Logs/clickprobe.on";
            if (!File.Exists(flag) || File.ReadAllText(flag).Trim() != "on") return;
            var go = new GameObject("ClickProbe");
            go.hideFlags = HideFlags.HideInHierarchy;
            go.AddComponent<ClickProbe>();
        }

        void Update()
        {
            var p = Pointer.current;
            var es = EventSystem.current;
            if (p == null || es == null) return;
            bool down = p.press.wasPressedThisFrame;
            bool up = p.press.wasReleasedThisFrame;
            if (!down && !up) return;
            var pos = p.position.ReadValue();
            var ped = new PointerEventData(es);
            ped.position = pos;
            hits.Clear();
            es.RaycastAll(ped, hits);
            string s = "[ClickProbe] " + (down ? "down" : "") + (up ? "up" : "") + " " + p.GetType().Name + " at " + pos +
                       " overUI=" + es.IsPointerOverGameObject() + " hits=" + hits.Count + ":";
            for (int i = 0; i < hits.Count && i < 5; i++)
                s += " | " + Path(hits[i].gameObject.transform);
            Debug.Log(s);
        }

        static string Path(Transform t)
        {
            string s = t.name;
            int n = 0;
            while (t.parent != null && n++ < 3)
            {
                t = t.parent;
                s = t.name + "/" + s;
            }
            return s;
        }
    }
}
#endif
