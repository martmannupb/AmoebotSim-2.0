using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AS2
{

    // Copyright ©
    // Part of the personal code library of Tobias Maurer.
    // Usage by any current or previous members the University of paderborn and projects associated with it is permitted.

    public class MouseController : MonoBehaviour
    {

        // Singleton
        public static MouseController instance;

        // Our main cam
        public Camera cam;

        public bool updateManually = false;
        public bool movementLocked = false;

        Vector3 currFramePosition;
        Vector3 lastFramePosition;
        Quaternion curCamRotation;

        public MouseController()
        {
            instance = this;
        }

        // Use this for initialization
        void Start()
        {
            if (cam == null)
            {
                cam = Camera.main;
            }
            orthographicSizeTarget = cam.orthographicSize;
        }

        // Update is called once per frame
        void Update()
        {
            if (updateManually == false)
            {
                UpdateLogic();
            }

        }

        public void UpdateManually()
        {
            UpdateLogic();
        }

        public void UpdateLogic()
        {
            currFramePosition = cam.ScreenToWorldPoint(Input.mousePosition);
            currFramePosition.z = 0;
            curCamRotation = cam.transform.rotation;

            UpdateCameraMovement();

            lastFramePosition = cam.ScreenToWorldPoint(Input.mousePosition);
            lastFramePosition.z = 0;
        }

        Vector3 diff = Vector3.zero;
        Vector3 diffSmooth = Vector3.zero;
        Vector3 diffSmoothNormalized60FPS = Vector3.zero;

        // Scrollwheel

        int useSystemNr = 0;

        // Old System
        float orthographicSizeTarget;
        float orthographicSizeCurrentVelocity = 0f;
        // Orthographic Constants
        public float minOrthographicSize = 5f;
        public float maxOrthographicSize = 30f;
        public float cameraMoveSpeedKeyboard = 1f;
        //public float cameraMovMaxSpeedMousewheelPerSec = 10f;

        // New System
        int desiredHeightOfSquare = 32;
        public int minHeightOfSquare;
        public int maxHeightOfSquare;
        public int squarePixelSize = 32;
        public int pixelDivisionConstant = 1;

        void UpdateCameraMovement()
        {

            if (movementLocked) return;

            // Keyboard WASD
            bool arrowLeft = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
            bool arrowDown = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
            bool arrowRight = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
            bool arrowUp = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
            bool anyArrowKeyPressed = arrowLeft || arrowDown || arrowRight || arrowUp;

            // Handle screen dragging
            // 0 left 1 right 2 middle mouse button
            if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
            {

                diffSmooth = diff; //one down???
                diff = lastFramePosition - currFramePosition;
                //diff = Quaternion.Euler(0f, 0f, -curCamRotation.eulerAngles.z) * diff; // also works
                diff = Quaternion.Inverse(curCamRotation) * diff;
                cam.transform.Translate(diff);
                // Calculate normalized difference (to 60fps)
                diffSmoothNormalized60FPS = (Time.deltaTime * 60f) * diff;
            }
            else
            {
                // Not holding down a drag mouse button
                if (anyArrowKeyPressed)
                {
                    // Arrow key movement
                    float leftRight = arrowLeft && arrowRight ? 0 : (arrowLeft ? -cameraMoveSpeedKeyboard : (arrowRight ? cameraMoveSpeedKeyboard : 0f));
                    float upDown = arrowUp && arrowDown ? 0 : (arrowUp ? cameraMoveSpeedKeyboard : (arrowDown ? -cameraMoveSpeedKeyboard : 0f));
                    diff = new Vector3(leftRight, upDown, 0);
                    diffSmoothNormalized60FPS = (Time.deltaTime * 60f) * diff;
                    cam.transform.Translate(diffSmoothNormalized60FPS);
                }
                else
                {
                    // Camera movement lerp
                    if (diffSmoothNormalized60FPS.magnitude > 0.01f * Time.deltaTime)
                    {
                        cam.transform.Translate(diffSmoothNormalized60FPS * Time.deltaTime * 60f);
                        diffSmoothNormalized60FPS *= Mathf.Pow(0.90f, Time.deltaTime * 60f);
                    }
                    else
                    {
                        diffSmoothNormalized60FPS = Vector3.zero;
                    }

                }
            }

            // Scrollwheel
            // If we're over a UI element, then bail out from this
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            switch (useSystemNr)
            {
                case 0:
                    // Set target orthographic size
                    orthographicSizeTarget -= orthographicSizeTarget * Input.GetAxis("Mouse ScrollWheel") * 3f;
                    orthographicSizeTarget = Mathf.Clamp(orthographicSizeTarget, minOrthographicSize, maxOrthographicSize);
                    // Update orthographic size
                    cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, orthographicSizeTarget, ref orthographicSizeCurrentVelocity, 0.05f);
                    break;
                case 1:
                    float mouseWheel = Input.GetAxis("Mouse ScrollWheel");
                    if (mouseWheel != 0)
                    {
                        desiredHeightOfSquare += (int)(mouseWheel / Mathf.Abs(mouseWheel)) * (squarePixelSize / pixelDivisionConstant);
                    }

                    desiredHeightOfSquare = Mathf.Clamp(desiredHeightOfSquare, minHeightOfSquare, maxHeightOfSquare);

                    if (desiredHeightOfSquare % (squarePixelSize / pixelDivisionConstant) != 0)
                    {
                        float min = desiredHeightOfSquare - (desiredHeightOfSquare % (squarePixelSize / pixelDivisionConstant));
                        desiredHeightOfSquare = (int)Mathf.Clamp(desiredHeightOfSquare, min, min + (squarePixelSize / pixelDivisionConstant));
                    }
                    cam.orthographicSize = (float)Screen.height / (2f * (float)desiredHeightOfSquare);
                    break;
            }
        }

        public void LockCameraMovement()
        {
            movementLocked = true;
        }

        public void UnlockCameraMovement()
        {
            movementLocked = false;
        }
    }

} // namespace AS2
