using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CookTableState
{
    DefaultState,
    TableEditState,
    CookingState
}

public class CookTableManager : MonoBehaviour
{
    public CookTableState currentState = CookTableState.DefaultState;
    public GameObject VisualParent;

    [Header("Table Edit")]
    //TableEditLayer Auto Push/Remove under EditParent
    public MouseInteractionLayer TableEditLayer;
    public GameObject EditParent;
    public GridPlacement_Grid tableGrid;

    void Start()
    {
        /*VisualParent.SetActive(false);*/
        //EditParent.gameObject.SetActive(false);
        //tableGrid.gameObject.SetActive(false);
        EnterState(currentState);
    }

    void Update()
    {
        UpdateCurrentState();
    }

    public void ChangeState(CookTableState newState)
    {
        ExitState(currentState);
        currentState = newState;
        EnterState(currentState);
    }

    public void onTableEditDone_CookingLoop()
    {
        //EditParent.gameObject.SetActive(false);
        tableGrid.gameObject.SetActive(false);
        
    }

    public void onEnterCooking()
    {
        ChangeState(CookTableState.CookingState);
    }

    void EnterState(CookTableState state)
    {
        switch (state)
        {
            case CookTableState.TableEditState:
                //TableEditLayer Auto Push/Remove under EditParent
                //EditParent.gameObject.SetActive(true);
                tableGrid.gameObject.SetActive(true);
                //TODO: Table Generation by RoomID;

                break;
            case CookTableState.CookingState:
                /*TableParent.SetActive(true);*/
                //TODO: Table Generation by RoomID;

                break;
            default:
                break;
        }
    }

    public void onLoadTableSet(int roomID)
    {
        Debug.Log($"Load #{roomID} Table tools");
    }

    void UpdateCurrentState()
    {
        switch (currentState)
        {
            case CookTableState.TableEditState:
                break;
            case CookTableState.CookingState:
                break;
            default:

                break;
        }
    }

    void ExitState(CookTableState state)
    {
        switch (state)
        {
            case CookTableState.TableEditState:
                //EditParent.gameObject.SetActive(false);
                tableGrid.gameObject.SetActive(false);
                break;
            case CookTableState.CookingState:
                break;
            default:

                break;
        }
    }
}





