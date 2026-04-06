using UnityEngine;
using UnityEngine.InputSystem;

namespace Combat
{
    public class PlayFieldRaycaster : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Collider2D playFieldArea;

        public bool TryGetMouseWorldPoint(out Vector3 worldPoint)
        {
            worldPoint = default;

            if (targetCamera is null)
                targetCamera = Camera.main;

            if (targetCamera is null)
                return false;

            Vector2 mouseScreen = Mouse.current.position.ReadValue();
            worldPoint = targetCamera.ScreenToWorldPoint(mouseScreen);
            worldPoint.z = 0f;

            if (playFieldArea is null)
                return true;

            return playFieldArea.OverlapPoint(worldPoint);
        }
    }
}