using UnityEngine;
using UnityEngine.InputSystem;

namespace Combat
{
    public class PlayFieldRaycaster : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private LayerMask playFieldMask;

        public bool TryGetMouseWorldPoint(out Vector3 worldPoint)
        {
            worldPoint = default;

            if (Mouse.current == null)
                return false;

            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

            Camera cam = worldCamera is not null ? worldCamera : Camera.main;
            if (cam is null)
                return false;

            Ray ray = cam.ScreenPointToRay(mouseScreenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, playFieldMask))
            {
                worldPoint = hit.point;
                return true;
            }

            return false;
        }
    }
}