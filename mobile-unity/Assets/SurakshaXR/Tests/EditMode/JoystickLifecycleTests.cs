using System.Reflection;
using NUnit.Framework;
using SurakshaXR.Presentation;
using UnityEngine;
using UnityEngine.UIElements;
using PointerType = UnityEngine.UIElements.PointerType;

namespace SurakshaXR.Tests
{
    public sealed class JoystickLifecycleTests
    {
        private MovementJoystick joystick;
        private Vector2 movement;
        private static readonly Vector2 Right = new Vector2(142, 88);
        [SetUp] public void Setup() { movement = Vector2.zero; joystick = new MovementJoystick(value => movement = value); }
        [TearDown] public void Cleanup() => joystick.Cancel();
        // Exercise the same state transitions used by UI Toolkit event callbacks.
        // A runtime panel/native touch device is covered by physical acceptance.
        private bool Begin(int id, string type = "touch", int button = 0)
            => (bool)Invoke("BeginDrag", id, type, Right, button);
        private bool Move(int id, int buttons = 1) => (bool)Invoke("MoveDrag", id, Right, buttons);
        private bool End(int id) => (bool)Invoke("EndDrag", id);
        private object Invoke(string method, params object[] values)
            => typeof(MovementJoystick).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(joystick, values);

        [Test] public void OtherFingerCannotStealOrReleaseActiveMovement()
        {
            Assert.That(Begin(1), Is.True); Assert.That(movement.x, Is.EqualTo(1).Within(.0001f));
            Assert.That(Begin(2), Is.False);
            Assert.That(End(2), Is.False, "Unrelated capture-out/cancel must not reset the active finger.");
            Assert.That(movement.x, Is.EqualTo(1).Within(.0001f));
            Assert.That(End(1), Is.True);
            Assert.That(movement, Is.EqualTo(Vector2.zero)); Assert.That(joystick.Value, Is.EqualTo(Vector2.zero));
        }
        [Test] public void PauseOrRedrawCancellationRejectsLateMovesUntilFreshPress()
        {
            Begin(1); joystick.Cancel();
            Assert.That(Move(1), Is.False); Assert.That(movement, Is.EqualTo(Vector2.zero));
            Assert.That(Begin(2), Is.True); Assert.That(movement.x, Is.GreaterThan(.9f));
            End(2); Assert.That(movement, Is.EqualTo(Vector2.zero));
        }
        [Test] public void MissingMouseUpIsRecoveredFromReleasedButtonMove()
        {
            Begin(PointerId.mousePointerId, PointerType.mouse);
            Move(PointerId.mousePointerId, 0);
            Assert.That(movement, Is.EqualTo(Vector2.zero));
            Assert.That(Move(PointerId.mousePointerId), Is.False);
        }
        [Test] public void RightMouseButtonDoesNotLatchJoystick()
        {
            Assert.That(Begin(PointerId.mousePointerId, PointerType.mouse, 1), Is.False);
            Assert.That(movement, Is.EqualTo(Vector2.zero));
            Assert.That(Begin(PointerId.mousePointerId, PointerType.mouse), Is.True);
        }
        [Test] public void DisabledJoystickStopsCurrentDragAndRejectsNewPress()
        {
            Begin(1); joystick.SetEnabled(false); Move(1);
            Assert.That(movement, Is.EqualTo(Vector2.zero));
            Assert.That(Begin(2), Is.False);
            joystick.SetEnabled(true); Assert.That(Begin(2), Is.True);
        }
        [Test] public void EditorCaptureDoesNotTreatMissingNativeTouchAsRelease()
        {
            Assert.That(Application.isPlaying, Is.False);
            Begin(1); Invoke("CheckNativeRelease");
            Assert.That(movement.x, Is.GreaterThan(.9f));
            joystick.Cancel(); Assert.That(movement, Is.EqualTo(Vector2.zero));
        }
    }
}
