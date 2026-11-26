using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SmartPlantPot
{
    /// <summary>
    /// Keeps all pots framed on any screen shape (landscape PC or portrait phone) and eases a little
    /// toward the selected pot. In portrait the pots are lifted so the bottom sheet doesn't hide them.
    ///
    /// Free look (to see the 3D team's shop): drag on the 3D view to orbit, mouse wheel / pinch to
    /// zoom, right- or middle-drag (two fingers) to move. <see cref="ResetView"/> (the HUD button or R)
    /// glides back to the plants; selecting a plant does too.
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

        [Header("Free look")]
        public bool allowFreeLook = true;
        [Tooltip("Degrees per screen pixel dragged.")]
        public float orbitSpeed = 0.22f;
        public float minDistance = 0.35f;
        public float maxFreeDistance = 3.2f;
        [Range(-30f, 0f)] public float minPitch = -12f;
        [Range(20f, 89f)] public float maxPitch = 80f;
        [Tooltip("The camera stays inside this box in free look (the shop's walls). Size 0 = no limit.")]
        public Bounds roomBounds;
        public float freeLookFov = 55f;

        public static CameraRig Instance { get; private set; }

        /// <summary>True while the user is looking around the shop.</summary>
        public bool IsFreeLook
        {
            get { return freeLook; }
        }

        /// <summary>True during (and the frame after) a camera drag, so the drag doesn't count as a tap.</summary>
        public static bool BlocksClick
        {
            get { return Instance != null && Instance.dragging; }
        }

        float baseFov = -1f;

        bool freeLook;
        Vector3 pivot;
        float yaw, pitch, distance;
        float targetYaw, targetPitch, targetDistance;
        Vector3 targetPivot;

        bool pressed;
        bool pressOnUI;
        bool dragging;
        int releaseFrame = -1;
        Vector2 pressPos, lastPos;
        float lastPinch = -1f;
        PlantUnit lastSelection;
        static readonly List<RaycastResult> hits = new List<RaycastResult>();

        Vector3 pos;
        Vector3 look;
        bool first = true;

        void Awake()
        {
            Instance = this;
            if (cam == null) cam = GetComponent<Camera>();
            viewDirection = viewDirection.normalized;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Back to the plants (HUD button, R key, or selecting a plant).</summary>
        public void ResetView()
        {
            freeLook = false;
            lastPinch = -1f;
        }

        void Update()
        {
            if (!allowFreeLook || cam == null) return;
            if (dragging && !pressed && Time.frameCount > releaseFrame) dragging = false;

            var kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame && !TypingInField()) ResetView();

            if (ModalPanel.AnyOpen)
            {
                pressed = false;
                return;
            }

            var mouse = Mouse.current;
            var touch = Touchscreen.current;
            int touches = TouchCount(touch);

            // ---- two fingers: pinch to zoom, move to pan ----
            if (touches >= 2)
            {
                Vector2 a = touch.touches[0].position.ReadValue();
                Vector2 b = touch.touches[1].position.ReadValue();
                float d = Vector2.Distance(a, b);
                Vector2 mid = (a + b) * 0.5f;
                if (lastPinch > 0f)
                {
                    EnterFreeLook();
                    targetDistance = Mathf.Clamp(targetDistance * lastPinch / Mathf.Max(1f, d), minDistance, maxFreeDistance);
                    Pan(mid - lastPos);
                }
                lastPinch = d;
                lastPos = mid;
                dragging = true;
                pressed = true;
                releaseFrame = Time.frameCount;
                return;
            }
            lastPinch = -1f;

            // ---- one pointer: drag = orbit (or pan with the right / middle mouse button) ----
            var p = Pointer.current;
            if (p != null)
            {
                Vector2 pos = p.position.ReadValue();
                bool panButton = mouse != null && (mouse.rightButton.isPressed || mouse.middleButton.isPressed);
                bool down = p.press.isPressed || panButton;
                bool downThisFrame = p.press.wasPressedThisFrame ||
                                     (mouse != null && (mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame));
                if (downThisFrame)
                {
                    pressed = true;
                    pressOnUI = OverUI(pos);
                    pressPos = lastPos = pos;
                }
                if (pressed && down && !pressOnUI)
                {
                    if (!dragging && (pos - pressPos).sqrMagnitude > 14f * 14f)
                    {
                        dragging = true;
                        EnterFreeLook();
                    }
                    if (dragging)
                    {
                        Vector2 delta = pos - lastPos;
                        if (panButton) Pan(delta);
                        else
                        {
                            targetYaw += delta.x * orbitSpeed;
                            targetPitch = Mathf.Clamp(targetPitch - delta.y * orbitSpeed, minPitch, maxPitch);
                        }
                    }
                }
                lastPos = pos;
                if (pressed && !down)
                {
                    pressed = false;
                    releaseFrame = Time.frameCount;   // keep blocking the tap this frame
                }
            }

            // ---- mouse wheel = zoom ----
            if (mouse != null)
            {
                float wheel = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(wheel) > 0.01f && !OverUI(mouse.position.ReadValue()))
                {
                    EnterFreeLook();
                    float notches = wheel / 120f;
                    if (Mathf.Abs(notches) < 0.2f) notches = Mathf.Sign(wheel);   // some mice report 1 per notch
                    targetDistance = Mathf.Clamp(targetDistance * Mathf.Pow(0.88f, notches), minDistance, maxFreeDistance);
                }
            }
        }

        static int TouchCount(Touchscreen touch)
        {
            if (touch == null) return 0;
            int n = 0;
            foreach (var t in touch.touches)
                if (t.press.isPressed) n++;
            return n;
        }

        static bool TypingInField()
        {
            var es = EventSystem.current;
            return es != null && es.currentSelectedGameObject != null &&
                   es.currentSelectedGameObject.GetComponent<InputField>() != null;
        }

        static bool OverUI(Vector2 screen)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            var ped = new PointerEventData(es);
            ped.position = screen;
            hits.Clear();
            es.RaycastAll(ped, hits);
            foreach (var h in hits)
                if (h.module is GraphicRaycaster) return true;
            return false;
        }

        void EnterFreeLook()
        {
            if (freeLook) return;
            freeLook = true;
            if (PlantSelection.Current != null) PlantSelection.Clear();
            lastSelection = null;

            // start from exactly where the camera is now
            Vector3 camPos = cam.transform.position;
            Vector3 fwd = cam.transform.forward;
            float d = (pos - look).magnitude;
            if (d < 0.05f) d = 1.2f;
            pivot = targetPivot = camPos + fwd * d;
            distance = targetDistance = Mathf.Clamp(d, minDistance, maxFreeDistance);
            Vector3 back = -fwd;
            yaw = targetYaw = Mathf.Atan2(back.x, back.z) * Mathf.Rad2Deg;
            pitch = targetPitch = Mathf.Clamp(Mathf.Asin(Mathf.Clamp(back.y, -1f, 1f)) * Mathf.Rad2Deg, minPitch, maxPitch);
        }

        void Pan(Vector2 delta)
        {
            Vector3 right = cam.transform.right;
            Vector3 up = cam.transform.up;
            float k = targetDistance * 0.0016f;
            targetPivot -= (right * delta.x + up * delta.y) * k;
            targetPivot = ClampToRoom(targetPivot, 0.05f);
        }

        Vector3 ClampToRoom(Vector3 p, float margin)
        {
            if (roomBounds.size.sqrMagnitude < 0.01f) return p;
            Vector3 min = roomBounds.min + Vector3.one * margin;
            Vector3 max = roomBounds.max - Vector3.one * margin;
            return new Vector3(Mathf.Clamp(p.x, min.x, max.x), Mathf.Clamp(p.y, min.y, max.y), Mathf.Clamp(p.z, min.z, max.z));
        }

        void FreeLookUpdate()
        {
            float k = 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime);
            yaw = Mathf.LerpAngle(yaw, targetYaw, k);
            pitch = Mathf.Lerp(pitch, targetPitch, k);
            distance = Mathf.Lerp(distance, targetDistance, k);
            pivot = Vector3.Lerp(pivot, targetPivot, k);

            Vector3 dir = Quaternion.Euler(-pitch, yaw, 0f) * Vector3.forward;   // pivot -> camera (pitch > 0 = above)
            Vector3 camPos = ClampToRoom(pivot + dir * distance, 0.12f);
            pos = camPos;
            look = pivot;
            cam.transform.position = camPos;
            cam.transform.rotation = Quaternion.LookRotation(pivot - camPos, Vector3.up);
            float fk = 1f - Mathf.Exp(-4f * Time.unscaledDeltaTime);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, Mathf.Max(freeLookFov, baseFov), fk);
        }

        void LateUpdate()
        {
            if (cam == null || PlantGrowth.All.Count == 0) return;

            // picking a plant brings the camera back to the plants
            var current = PlantSelection.Current;
            if (freeLook && current != null && current != lastSelection) freeLook = false;
            lastSelection = current;

            if (freeLook)
            {
                if (baseFov < 0f) baseFov = cam.fieldOfView;
                FreeLookUpdate();
                return;
            }

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
