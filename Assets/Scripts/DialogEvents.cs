using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Prefer to manage the dialogue referenced in code via a pub/sub mechanism, so the Dialogue UI calls these
///   methods and then they are broadcast
/// </summary>
public class DialogEvents : MonoBehaviour
{
    public event Action DialogueStart;
    public event Action DialogueEnd;

    public void OnDialogueStart()
    {
        DialogueStart?.Invoke();
    }
    public void OnDialogueEnd()
    {
        DialogueEnd?.Invoke();
    }
}