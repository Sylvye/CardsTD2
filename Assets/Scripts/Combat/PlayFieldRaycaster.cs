using UnityEngine;
using UnityEngine.InputSystem;

namespace Combat
{
    public class PlayFieldRaycaster : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Collider2D playFieldArea;

        public bool TryGetMouseWorldPoint(out Vector3 worldPoint, bool requirePlayField = true)
        {
            worldPoint = default;

            if (!TryGetTargetCamera(out Camera camera))
                return false;

            Vector2 mouseScreen = Mouse.current.position.ReadValue();
            worldPoint = camera.ScreenToWorldPoint(mouseScreen);
            worldPoint.z = 0f;

            if (!requirePlayField || playFieldArea is null)
                return true;

            return playFieldArea.OverlapPoint(worldPoint);
        }

        public bool TryScreenToWorldPoint(Vector2 screenPoint, out Vector3 worldPoint)
        {
            worldPoint = default;

            if (!TryGetTargetCamera(out Camera camera))
                return false;

            worldPoint = camera.ScreenToWorldPoint(screenPoint);
            worldPoint.z = 0f;
            return true;
        }

        private bool TryGetTargetCamera(out Camera camera)
        {
            if (targetCamera is null)
                targetCamera = Camera.main;

            camera = targetCamera;
            return camera is not null;
        }
    }
}
