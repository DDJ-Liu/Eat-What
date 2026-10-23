using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutDice : InteractionController
{
    [SerializeField] private ScrollBar_Controller slice1;
    [SerializeField] private ScrollBar_Controller slice2;

    [Header("阈值与推进")]
    [SerializeField] private float triggerThreshold = 0.3f;
    [SerializeField] private float doneThreshold = 0.95f;
    [SerializeField] private float autoDriveDuration = 0.6f;

    private bool _slice1Triggered, _slice1Done;
    private bool _slice2Triggered, _slice2Done;

    protected override void Start()
    {
        //Debug
        //Initialize_Spawn();
        //Debug
        base.Start();

        slice1.gameObject.SetActive(true);
        slice2.gameObject.SetActive(false);

        slice1.onValueChanged.AddListener(OnSlice1ValueChanged);
        slice2.onValueChanged.AddListener(OnSlice2ValueChanged);

        ToolObject.GetComponent<Tool_Interaction>().OpenGameObjectAndCloseOthers("Horizontal");

        //Vertical Horizontal cut
        //myTool.transform.Find("VisualParent").eulerAngles = new Vector3(0f, 0f, 90f);
    }

    private void OnSlice1ValueChanged(float v)
    {
        if (!_slice1Triggered && v >= triggerThreshold)
        {
            _slice1Triggered = true;
            TriggerAction();
            StartCoroutine(AutoDriveToOne(slice1));
        }
        if (!_slice1Done && v >= doneThreshold)
        {
            _slice1Done = true;
            ActionCompleted();
        }
    }

    private void OnSlice2ValueChanged(float v)
    {
        if (!_slice2Triggered && v >= triggerThreshold)
        {
            _slice2Triggered = true;
            TriggerAction();
            StartCoroutine(AutoDriveToOne(slice2));
        }
        if (!_slice2Done && v >= doneThreshold)
        {
            _slice2Done = true;
            ActionCompleted();
        }
    }

    private IEnumerator AutoDriveToOne(ScrollBar_Controller slice)
    {
        float rate = (1f - triggerThreshold) / Mathf.Max(0.0001f, autoDriveDuration);

        while (slice.Value < 1f)
        {
            bool userDragging = slice.handleDraggable != null && slice.handleDraggable.dragging;
            if (!userDragging)
            {
                float next = Mathf.Min(slice.Value + rate * Time.deltaTime, 1f);
                slice.SetValue(next, true);
            }
            yield return null;
        }
    }

    public void onSlice1Done()
    {
        slice1.gameObject.SetActive(false);
        slice2.gameObject.SetActive(true);
        ToolObject.GetComponent<Tool_Interaction>().OpenGameObjectAndCloseOthers("Vertical");
        //Vertical Horizontal cut
        //myTool.transform.Find("VisualParent").eulerAngles = new Vector3(0f, 0f, 0f);
    }

    public void onSlice2Done()
    {
        slice1.gameObject.SetActive(false);
        slice2.gameObject.SetActive(false);
        onInteractionFinished();
    }

    

    public override void ActionCompleted()
    {
        Debug.Log("ActionCompleted()");
        base.ActionCompleted();
        switch(stage)
        {
            case 1:
                onSlice1Done();
                break;
            case 2:
                onSlice2Done();
                break;
        }
    }
}
