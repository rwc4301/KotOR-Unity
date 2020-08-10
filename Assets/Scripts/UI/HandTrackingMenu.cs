using UnityEngine;

namespace KotORUnity.UI
{
    [RequireComponent(typeof(Canvas))]
    public class HandTrackingMenu : MonoBehaviour
    {
        public Transform trackingObject;
        public Vector3 offsetPosition, offsetRotation;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (trackingObject) {
                transform.rotation = trackingObject.rotation * Quaternion.Euler(offsetRotation);
                transform.position = trackingObject.position + transform.right * offsetPosition.x + transform.up * offsetPosition.y + transform.forward * offsetPosition.z;
            }
        }
    }
}