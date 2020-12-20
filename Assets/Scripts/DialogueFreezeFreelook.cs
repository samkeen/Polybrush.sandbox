using System;
using Cinemachine;
using UnityEngine;


/// <summary>
/// This overloads the Inpup Axis provider for the Cinemachine freelook camera.
/// 
/// https://forum.unity.com/threads/cinemachine-freelook-third-person-disable-enable-following-mouse-movement.960491/
/// https://docs.unity3d.com/Packages/com.unity.cinemachine@2.6/api/Cinemachine.AxisState.IInputAxisProvider.html
/// https://unity.com/unity/features/editor/art-and-design/cinemachine
/// </summary>
public class DialogueFreezeFreelook : MonoBehaviour, AxisState.IInputAxisProvider
{
    public string HorizontalInput = "Mouse X";
    public string VerticalInput = "Mouse Y";
    
    private bool isInDialog;

    private void Start()
    {
        FindObjectOfType<DialogEvents>().DialogueStart += OnDialogueStart;
        FindObjectOfType<DialogEvents>().DialogueEnd += OnDialogueEnd;
    }

    public float GetAxisValue(int axis)
    {
        // No input if in Dialogue
        if (isInDialog)
            return 0;
        // else return input as normal
        switch (axis)
        {
            case 0: return Input.GetAxis(HorizontalInput);
            case 1: return Input.GetAxis(VerticalInput);
            default: return 0;
        }
    }

    public void OnDialogueStart()
    {
        isInDialog = true;
    }
    
    public void OnDialogueEnd()
    {
        isInDialog = false;
    }
    
    private void OnDisable()
    {
        var dialogueEvents = FindObjectOfType<DialogEvents>();
        if (dialogueEvents != null)
        {
            dialogueEvents.DialogueStart -= OnDialogueStart;
            dialogueEvents.DialogueEnd -= OnDialogueEnd;
        }
    }
}