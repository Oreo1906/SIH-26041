using System;
using UnityEngine;
using UnityEngine.UIElements;
using PointerType = UnityEngine.UIElements.PointerType;

namespace SurakshaXR.Presentation
{
    // One captured finger moves; another finger can independently control the camera.
    public sealed class MovementJoystick : VisualElement
    {
        private readonly VisualElement thumb;
        private readonly Action<Vector2> changed;
        private readonly IVisualElementScheduledItem releaseWatchdog;
        private int pointer = -1;
        private string activePointerType;
        public Vector2 Value { get; private set; }
        public MovementJoystick(Action<Vector2> changed)
        {
            this.changed = changed; name = "movement-joystick";
            style.width = style.height = 176; style.flexShrink = 0;
            Round(this, 88); style.backgroundColor = new Color(.04f, .09f, .12f, .75f);
            style.borderTopWidth = style.borderBottomWidth = style.borderLeftWidth = style.borderRightWidth = 2;
            style.borderTopColor = style.borderBottomColor = style.borderLeftColor = style.borderRightColor = new Color(.5f, .78f, .75f, .6f);
            thumb = new VisualElement { pickingMode = PickingMode.Ignore };
            thumb.style.position = Position.Absolute; thumb.style.width = thumb.style.height = 64;
            thumb.style.backgroundColor = new Color(.32f, .85f, .72f, .95f); Round(thumb, 32); Add(thumb); PositionThumb(Vector2.zero);
            RegisterCallback<PointerDownEvent>(e => {
                if (BeginDrag(e.pointerId, e.pointerType, e.localPosition, e.button)) e.StopPropagation();
            });
            RegisterCallback<PointerMoveEvent>(e => { if (MoveDrag(e.pointerId, e.localPosition, e.pressedButtons)) e.StopPropagation(); });
            RegisterCallback<PointerUpEvent>(e => { if (EndDrag(e.pointerId)) e.StopPropagation(); });
            RegisterCallback<PointerCancelEvent>(e => { if (EndDrag(e.pointerId)) e.StopPropagation(); });
            RegisterCallback<PointerCaptureOutEvent>(e => EndDrag(e.pointerId));
            RegisterCallback<GeometryChangedEvent>(e => {
                // Rotation/reflow invalidates the old finger-to-stick coordinates.
                if (e.oldRect != e.newRect) Cancel();
            });
            releaseWatchdog = schedule.Execute(CheckNativeRelease).Every(100); releaseWatchdog.Pause();
            RegisterCallback<AttachToPanelEvent>(_ => releaseWatchdog.Resume());
            RegisterCallback<DetachFromPanelEvent>(_ => { releaseWatchdog.Pause(); Cancel(); });
        }
        public static Vector2 Normalize(Vector2 displacement, float radius = 54)
        {
            var value = Vector2.ClampMagnitude(displacement / Mathf.Max(1, radius), 1);
            const float deadZone = .12f;
            return value.magnitude <= deadZone ? Vector2.zero : value.normalized * ((value.magnitude - deadZone) / (1 - deadZone));
        }
        private void SetFromPoint(Vector2 point)
        {
            var center = Center;
            var radius = Mathf.Max(1, Mathf.Min(54, Mathf.Min(center.x, center.y) - 34));
            var offset = Vector2.ClampMagnitude(point - center, radius);
            PositionThumb(offset); var input = Normalize(offset, radius); Value = new Vector2(input.x, -input.y); changed?.Invoke(Value);
        }
        private Vector2 Center => new Vector2(float.IsNaN(layout.width) || layout.width <= 0 ? 88 : layout.width / 2,
            float.IsNaN(layout.height) || layout.height <= 0 ? 88 : layout.height / 2);
        private void PositionThumb(Vector2 offset) { var center = Center; thumb.style.left = center.x - 32 + offset.x; thumb.style.top = center.y - 32 + offset.y; }
        private bool BeginDrag(int id, string type, Vector2 point, int button)
        {
            if (pointer != -1 || !enabledInHierarchy || type == PointerType.mouse && button != 0) return false;
            pointer = id; activePointerType = type;
            if (panel != null) this.CapturePointer(pointer);
            SetFromPoint(point); return true;
        }
        private bool MoveDrag(int id, Vector2 point, int pressedButtons)
        {
            if (pointer != id) return false;
            if (!enabledInHierarchy || activePointerType == PointerType.mouse && (pressedButtons & 1) == 0
                || panel != null && !this.HasPointerCapture(pointer)) Cancel();
            else SetFromPoint(point);
            return true;
        }
        private bool EndDrag(int id) { if (pointer != id) return false; Cancel(); return true; }
        private void CheckNativeRelease()
        {
            // Catch OS interruptions that omit an Up/Cancel event. Never infer touch
            // finger IDs from UI pointer IDs; only zero live touches proves all ended.
            if (!Application.isPlaying || pointer == -1) return;
            if (!Application.isFocused || !enabledInHierarchy || !this.HasPointerCapture(pointer)
                || activePointerType == PointerType.mouse && !Input.GetMouseButton(0)
                || activePointerType == PointerType.touch && Input.touchCount == 0) Cancel();
        }
        public void Cancel()
        {
            var previous = pointer; pointer = -1; activePointerType = null;
            Value = Vector2.zero; PositionThumb(Vector2.zero); changed?.Invoke(Vector2.zero);
            if (previous != -1 && panel != null && this.HasPointerCapture(previous)) this.ReleasePointer(previous);
        }
        private static void Round(VisualElement view, float radius) { view.style.borderTopLeftRadius = view.style.borderTopRightRadius = view.style.borderBottomLeftRadius = view.style.borderBottomRightRadius = radius; }
    }
}
