using UnityEngine;

namespace Enemies
{
    public class PathFollower : MonoBehaviour
    {
        [SerializeField] private EnemyPath path;
        [SerializeField] private float speed = 2f;
        [SerializeField] private float startingDistance = 0f;
        [SerializeField] private bool faceMovementDirection = true;

        public EnemyPath Path => path;
        public float Speed => speed;
        public float DistanceTravelled { get; private set; }
        public bool ReachedEnd => path != null && DistanceTravelled >= path.TotalLength;

        private void Start()
        {
            DistanceTravelled = Mathf.Max(0f, startingDistance);
            SnapToPath();
        }

        private void FixedUpdate()
        {
            if (path is null)
                return;

            DistanceTravelled += speed * Time.fixedDeltaTime;
            DistanceTravelled = Mathf.Min(DistanceTravelled, path.TotalLength);

            SnapToPath();
        }

        public void SetPath(EnemyPath newPath, float startingDistanceOnPath = 0f)
        {
            path = newPath;
            DistanceTravelled = Mathf.Max(0f, startingDistanceOnPath);
            SnapToPath();
        }

        public void SetSpeed(float newSpeed)
        {
            speed = Mathf.Max(0f, newSpeed);
        }

        private void SnapToPath()
        {
            if (path is null)
                return;

            Vector2 position = path.GetPositionAtDistance(DistanceTravelled);
            transform.position = new Vector3(position.x, position.y, transform.position.z);

            if (faceMovementDirection)
            {
                Vector2 tangent = path.GetTangentAtDistance(DistanceTravelled);

                if (tangent.sqrMagnitude > 0.0001f)
                {
                    float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Euler(0f, 0f, angle);
                }
            }
        }
    }
}
