using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MouseInteractionLayer : MonoBehaviour
{
    public int layerID;
    public bool manualStackLayer = false;

    public List<LayerMask> alloweInteractionLayers;

    [Header("点击空白取消")]
    public bool clickNullEqualsCancel = false;
    public UnityEvent onClickNullCancel;
    private void Start()
    {
        if (MouseManager.Instance == null)
        {
            Debug.Log("No MouseManager");
            return;
        }

        if (manualStackLayer)
        {
            Debug.Log("ManualPushLayer, OnEnable not working");
            return;
        }

        if (!MouseManager.Instance.mouseInteractiveLayers.Contains(this))
        {
            MouseManager.Instance.mouseInteractiveLayers.Push(this);
        }
        else
        {
            Debug.Log($"Layer {name} is already in Manger Stack");
        }
    }

    private void OnEnable()
    {
        if(MouseManager.Instance == null)
        {
            Debug.Log("No MouseManager");
            return;
        }

        if (manualStackLayer)
        {
            Debug.Log("ManualPushLayer, OnEnable not working");
            return;
        }

        if (!MouseManager.Instance.mouseInteractiveLayers.Contains(this))
        {
            MouseManager.Instance.mouseInteractiveLayers.Push(this);
        }
        else
        {
            Debug.Log($"Layer {name} is already in Manger Stack");
        }
    }

    private void OnDisable()
    {
        if (MouseManager.Instance == null)
        {
            Debug.Log("No MouseManager");
            return;
        }

        if (manualStackLayer)
        {
            Debug.Log("ManualPopLayer, OnDisable not working");
            return;
        }

        if (MouseManager.Instance.mouseInteractiveLayers.Contains(this))
        {
            Tools.TryRemoveFromStack<MouseInteractionLayer>(MouseManager.Instance.mouseInteractiveLayers, this);
        }
        else
        {
            Debug.Log($"Layer {name} not in Manager Stack");
        }
    }

    public void OnPushLayer()
    {
        Debug.Log($"PushedLayer {name}");
        if (MouseManager.Instance == null)
        {
            Debug.Log("No MouseManager");
            return;
        }

        if (!MouseManager.Instance.mouseInteractiveLayers.Contains(this))
        {
            MouseManager.Instance.mouseInteractiveLayers.Push(this);
        }
        else
        {
            Debug.Log($"Layer {name} is already in Manger Stack");
        }
    }

    public void OnRemoveLayer()
    {
        if (MouseManager.Instance == null)
        {
            Debug.Log("No MouseManager");
            return;
        }

        if (MouseManager.Instance.mouseInteractiveLayers.Contains(this))
        {
            Tools.TryRemoveFromStack<MouseInteractionLayer>(MouseManager.Instance.mouseInteractiveLayers, this);
        }
        else
        {
            Debug.Log("Layer not in Manager Stack");
        }
    }
}
