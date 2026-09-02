using UnityEngine;

namespace CleanPath.Game
{
    public class BillboardFaceCamera : MonoBehaviour
    {
        [SerializeField] bool _lockY = true;

        public void Tick(Camera cam)
        {
            Vector3 dir = cam.transform.position - transform.position;
            if (_lockY) dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);
        }
    }
}
