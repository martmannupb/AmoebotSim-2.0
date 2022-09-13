using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Copyright ©
// Part of the personal code library of Tobias Maurer.
// Usage by any current or previous members the University of paderborn and projects associated with it is permitted.

public class MouseController : MonoBehaviour {

    public Camera cam;

    public bool updateManually = false;

    Vector3 currFramePosition;
    Vector3 lastFramePosition;

    Vector3 dragStartPosition;

	// Use this for initialization
	void Start () {
        if(cam == null)
        {
            cam = Camera.main;
        }
        orthographicSizeTarget = cam.orthographicSize;
    }

    // Update is called once per frame
    void Update() {
        if(updateManually == false)
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

        //UpdateCursor();
        UpdateDragging();
        UpdateCameraMovement();

        lastFramePosition = cam.ScreenToWorldPoint(Input.mousePosition);
        lastFramePosition.z = 0;
    }

    void UpdateDragging() {
        // If we're over a UI element, then bail out from this
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) {
            return;
        }

        // Start Drag
        if (Input.GetMouseButtonDown(0)) {
            dragStartPosition = currFramePosition;
        }

        int start_x = Mathf.FloorToInt(dragStartPosition.x);
        int end_x = Mathf.FloorToInt(currFramePosition.x);
        int start_y = Mathf.FloorToInt(dragStartPosition.y);
        int end_y = Mathf.FloorToInt(currFramePosition.y);

        if (end_x < start_x) {
            int tmp = end_x;
            end_x = start_x;
            start_x = tmp;
        }
        if (end_y < start_y) {
            int tmp = end_y;
            end_y = start_y;
            start_y = tmp;
        }
        
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
    public float cameraMovSpeedKeyboard = 10f;
    //public float cameraMovMaxSpeedMousewheelPerSec = 10f;

    // New System
    int desiredHeightOfSquare = 32;
    public int minHeightOfSquare;
    public int maxHeightOfSquare;
    public int squarePixelSize = 32;
    public int pixelDivisionConstant = 1;

    void UpdateCameraMovement() {
        // Keyboard WASD
        //bool a = Input.GetKey(KeyCode.A);
        //bool s = Input.GetKey(KeyCode.S);
        //bool d = Input.GetKey(KeyCode.D);
        //bool w = Input.GetKey(KeyCode.W);
        //int xAxis = Convert.ToInt32(d) - Convert.ToInt32(a);
        //int yAxis = Convert.ToInt32(w) - Convert.ToInt32(s);
        //Camera = new Vector3(transform.position.x + xAxis * cameraMovSpeedKeyboard * Time.deltaTime, transform.position.y + yAxis * cameraMovSpeedKeyboard * Time.deltaTime, transform.position.z);
        
        // Handle screen dragging
        // 0 left 1 right 2 middle mouse button
        if (Input.GetMouseButton(2)) {

            diffSmooth = diff; //one down???
            diff = lastFramePosition - currFramePosition;
            cam.transform.Translate(diff);
            // Calculate normalized difference (to 60fps)
            diffSmoothNormalized60FPS = (Time.deltaTime * 60f) * diff;
        }
        else {
            // Check Speed Bounds
            /*if (Mathf.Pow(cameraMovMaxSpeedMousewheelPerSec / 60f, 2) < diffSmoothNormalized60FPS.sqrMagnitude) {
                // Brake
                diffSmoothNormalized60FPS = ((cameraMovMaxSpeedMousewheelPerSec / 60f) / diffSmoothNormalized60FPS.magnitude) * diffSmoothNormalized60FPS;
            }*/

            if (diffSmoothNormalized60FPS.magnitude > 0.01f * Time.deltaTime) {
                cam.transform.Translate(diffSmoothNormalized60FPS * Time.deltaTime * 60f);
                diffSmoothNormalized60FPS *= Mathf.Pow(0.99f, Time.deltaTime * 60f);
            }
            else {
                diffSmoothNormalized60FPS = Vector3.zero;
            }
        }
        if (Input.GetMouseButton(2)) {
            
        }

        // Scrollwheel
        // If we're over a UI element, then bail out from this
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        switch (useSystemNr) {
            case 0:
                // Set target orthographic size
                orthographicSizeTarget -= orthographicSizeTarget * Input.GetAxis("Mouse ScrollWheel") * 3f;
                orthographicSizeTarget = Mathf.Clamp(orthographicSizeTarget, minOrthographicSize, maxOrthographicSize);
                // Update orthographic size
                cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, orthographicSizeTarget, ref orthographicSizeCurrentVelocity, 0.05f);
                break;
            case 1:
                float mouseWheel = Input.GetAxis("Mouse ScrollWheel");
                if (mouseWheel != 0) {
                    desiredHeightOfSquare += (int)(mouseWheel / Mathf.Abs(mouseWheel)) * (squarePixelSize / pixelDivisionConstant);
                }

                desiredHeightOfSquare = Mathf.Clamp(desiredHeightOfSquare, minHeightOfSquare, maxHeightOfSquare);

                if (desiredHeightOfSquare % (squarePixelSize / pixelDivisionConstant) != 0) {
                    float min = desiredHeightOfSquare - (desiredHeightOfSquare % (squarePixelSize / pixelDivisionConstant));
                    desiredHeightOfSquare = (int)Mathf.Clamp(desiredHeightOfSquare, min, min + (squarePixelSize / pixelDivisionConstant));
                }
                cam.orthographicSize = (float)Screen.height / (2f * (float)desiredHeightOfSquare);
                break;
        }
    }
}
