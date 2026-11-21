using UnityEngine;

namespace SmartPlantPot
{
    /// <summary>
    /// Keeps all pots framed on any screen shape (landscape PC or portrait phone) and eases a little
    /// toward the selected pot. In portrait the pots are lifted so the bottom sheet doesn't hide them.
    /// </summary>
    [AddComponentMenu("Smart Plant Pot/Camera Rig")]
    public class CameraRig : MonoBehaviour
    {
        public Camera cam;
        [Tooltip("Direction from the pots toward the camera (normalised in Awake).")]
        public Vector3 viewDirection = new Vector3(1f, 0.55f, 0f);
        public float padding = 0.12f;
        [Range(0f, 0.6f)] public float focusAmount = 0.3f;
        public float smooth = 3f;
        [Tooltip("Extra height above the pots for grown plants (metres).")]
        public float plantHeadroom = 0.35f;
        [Range(0f, 1f)] public float portraitLift = 0.55f;
        [Tooltip("Landscape: how far the selected pot slides left to leave room for its card on the right.")]
        [Range(0f, 0.8f)] public float cardRoom = 0.22f;
        [Tooltip("The camera never backs further than this from the pots (it stays inside the shop); "
                 + "on narrow screens the field of view widens instead.")]
        public float maxDistance = 1.5f;
        [Range(30f, 90f)] public float maxFieldOfView = 72f;

        float baseFov = -1f;

        Vector3 pos;
        Vector3 look;
        bool first = true;

        void Awake()
        {
            if (cam == null) cam = GetComponent<Camera>();
            viewDirection = viewDirection.normalized;
        }

        void LateUpdate()
        {
            if (cam == null || PlantGrowth.All.Count == 0) return;

            Bounds all = new Bounds();
            bool any = false;
            foreach (var p in PlantGrowth.All)
            {
                var b = new Bounds(p.transform.position + Vector3.up * plantHeadroom * 0.5f, new Vector3(0.25f, plantHeadroom, 0.25f));
                Bounds rb;
                if (ScreenSpace.WorldBounds(p.transform, out rb)) b.Encapsulate(rb);
                if (!any)
                {
                    all = b;
                    any = true;
                }
                else all.Encapsulate(b);
            }

            Vector3 dir = viewDirection;
            Vector3 right = Vector3.Cross(Vector3.up, -dir).normalized;
            Vector3 up = Vector3.Cross(-dir, right).normalized;
            Vector3 e = all.extents;
            float halfW = Mathf.Abs(e.x * right.x) + Mathf.Abs(e.y * right.y) + Mathf.Abs(e.z * right.z) + padding;
            float halfH = Mathf.Abs(e.x * up.x) + Mathf.Abs(e.y * up.y) + Mathf.Abs(e.z * up.z) + padding;

            if (baseFov < 0f) baseFov = cam.fieldOfView;
            float vfov = baseFov * Mathf.Deg2Rad * 0.5f;
            float hfov = Mathf.Atan(Mathf.Tan(vfov) * cam.aspect);
            float dist = Mathf.Max(halfW / Mathf.Tan(hfov), halfH / Mathf.Tan(vfov));
            float fov = baseFov;
            if (dist > maxDistance)
            {
                // narrow (portrait) screen: widen the lens instead of backing through the walls
                dist = maxDistance;
                float needH = Mathf.Atan(halfW / dist);
                float needV = Mathf.Max(Mathf.Atan(Mathf.Tan(needH) / Mathf.Max(0.1f, cam.aspect)), Mathf.Atan(halfH / dist));
                fov = Mathf.Min(maxFieldOfView, needV * 2f * Mathf.Rad2Deg);
                vfov = fov * Mathf.Deg2Rad * 0.5f;
                hfov = Mathf.Atan(Mathf.Tan(vfov) * cam.aspect);
            }
            cam.fieldOfView = first ? fov : Mathf.Lerp(cam.fieldOfView, fov, 1f - Mathf.Exp(-smooth * Time.unscaledDeltaTime));

            Vector3 target = all.center;
            var sel = PlantSelection.Current;
            if (sel != null && focusAmount > 0f)
            {
                Bounds sb;
                Vector3 sc = ScreenSpace.WorldBounds(sel.transform, out sb) ? sb.center : sel.transform.position;
                target = Vector3.Lerp(target, sc, focusAmount);
                dist *= 1f - focusAmount * 0.45f;
            }

            bool portrait = cam.aspect < 1f;
            if (portrait) target -= up * halfH * portraitLift * (sel != null ? 1.4f : 0.6f);
            else if (sel != null && cardRoom > 0f) target += right * dist * Mathf.Tan(hfov) * cardRoom;

            Vector3 wantPos = target + dir * dist;
            float k = first ? 1f : 1f - Mathf.Exp(-smooth * Time.unscaledDeltaTime);
            pos = Vector3.Lerp(pos, wantPos, k);
            look = Vector3.Lerp(look, target, k);
            first = false;
            cam.transform.position = pos;
            cam.transform.rotation = Quaternion.LookRotation(look - pos, Vector3.up);
        }
    }
}
