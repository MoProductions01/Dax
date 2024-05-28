using UnityEngine;
    
    /// <summary>
    /// This class takes care of the majority of the issues involved with multiple
    /// screen resolutions rather than all of the UI anchoring.
    /// </summary>
    [RequireComponent(typeof(Camera))][ExecuteAlways]
    public class CameraResize : MonoBehaviour
    {
        [SerializeField]
        private Camera GameCamera;        
        private Vector2 TargetAspectRatio = new(1,1);   // Default resolution for the game
        private Vector2 RectCenter = new(0.5f, 0.5f);   // Center of the default screen rectangle        
        private Vector2 LastResolution;                 // Last resolution of the screen

        /// <summary>
        /// Grab a reference to the camera once this script is validated
        /// </summary>
        private void OnValidate()
        {
            GameCamera ??= GetComponent<Camera>();
        }

        /// <summary>
        /// Check if the screen resesolution has changed during the engine's
        /// LateUpdate calls
        /// </summary>
        public void LateUpdate()
        {        
            // Current screen resolution
            Vector2 currentScreenResolution = new Vector2(Screen.width, Screen.height);
    
            // Don't run all the calculations if the screen resolution has not changed
            if (LastResolution != currentScreenResolution)
            {
                CalculateCameraRect(currentScreenResolution);
            }

            LastResolution = currentScreenResolution;
        }

        /// <summary>
        /// Calculates the current rectangle for the camera based on it's resolution/aspect ratio.
        /// Only called if the resolution has changed.
        /// </summary>
        /// <param name="currentScreenResolution"></param>
        private void CalculateCameraRect(Vector2 currentScreenResolution)
        {
            var normalizedAspectRatio = TargetAspectRatio / currentScreenResolution;
            var size = normalizedAspectRatio / Mathf.Max(normalizedAspectRatio.x, normalizedAspectRatio.y);
            GameCamera.rect = new Rect(default, size) { center = RectCenter };
        }
    }

