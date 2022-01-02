using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using Component = UnityEngine.Component;

public class Player_CamLook : MonoBehaviour
{

    [Header("Mouse Sensitivity")]
    public float mouseSens = 100f;

    [Header("Player Reference")] public Transform playerBody;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    
    private void Update()
    {

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSens * Time.deltaTime;

            playerBody.Rotate(Vector3.up * mouseX);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CAM_ChangeCursorState();
        }
    }

    [Description("Changes the lock state of the cursor depending on which state it was last.")]
    private static void CAM_ChangeCursorState()
    {
        switch (Cursor.lockState) // Change the lock mode to the opposite of what it currently is.
        {
            case CursorLockMode.Locked:
                Cursor.lockState = CursorLockMode.None;
                break;
            
            case CursorLockMode.None:
                Cursor.lockState = CursorLockMode.Locked;
                break;  
            
            case CursorLockMode.Confined:
                Cursor.lockState = CursorLockMode.None;
                break;
            
            default:
                Debug.LogError("This error shouldn't show up but fuck it.");
                break;
        }
    }
}
