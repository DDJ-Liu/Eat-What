using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Container : MonoBehaviour
{
    public ContainerTag ContainerTag;
    public enum state { Idle, Highlight,}

    public state ContainerState = state.Idle;

    public GameObject WaitingVisual;
    public CookingDropZone dropZone;
    public GameObject DropZoneHighLight;
    public GameObject FrontContainerObject;
    private TransitionController transitionController;
    private Collider2D dropZoneCollider;

    private void Awake()
    {
        transitionController = GetComponent<TransitionController>();
        if (transitionController == null)
        {
            Debug.LogWarning($"Container on {gameObject.name} has no TransitionController, transitions will not work");
        }

        // 缓存 dropZone 的 Collider2D 引用
        if (dropZone != null)
        {
            dropZoneCollider = dropZone.GetComponent<Collider2D>();
            if (dropZoneCollider == null)
            {
                Debug.LogError($"Container on {gameObject.name}: dropZone has no Collider2D component");
            }
        }

        // 默认 DropZone 与 Highlight 关闭
        if (DropZoneHighLight != null) DropZoneHighLight.SetActive(false);
        closeDropZone();
        //TransitionTo(state.Idle, false);
    }

    private void Start()
    {
    }

    /// <summary>
    /// Container 的统一操作接口。
    /// 根据当前状态和目标状态自动决定执行路径。
    /// </summary>
    /// <param name="targetState">目标状态（Idle / Highlight）</param>
    /// <param name="dropZoneShouldBeOpen">目标 DropZone 状态（true=打开, false=关闭）</param>
    public void TransitionTo(state targetState, bool dropZoneShouldBeOpen)
    {
        bool needStateTransition = (ContainerState != targetState);

        if (needStateTransition)
        {
            if (targetState == state.Highlight)
            {
                if (dropZoneShouldBeOpen)
                {
                    startHighlightWithCallback(() => openDropZone());
                }
                else
                {
                    startHighlight();
                }
            }
            else // targetState == state.Idle
            {
                startIdle();
            }
        }
        else
        {
            // 状态无需切换，仅调整 DropZone
            if (ContainerState == state.Highlight)
            {
                if (dropZoneShouldBeOpen)
                    openDropZone();
                else
                    closeDropZone();
            }
            // 如果当前是 Idle，DropZone 必然关闭，不需要操作

        }
    }

    #region 私有实现方法

    private void startHighlightWithCallback(Action callback)
    {
        Debug.Log($"Container {gameObject.name} set Highlight + callback");
        ContainerState = state.Highlight;

        if (transitionController != null)
        {
            transitionController.PlayTransition("Highlight", () => startHighlightPart2(callback));
        }
        else
        {
            onHighlightEndThenCallback(callback);
        }
    }

    private void startHighlightPart2(Action callback)
    {
        Debug.Log($"Container {gameObject.name} set Highlight2 + callback");

        if (transitionController != null)
        {
            transitionController.PlayTransition("Highlight2", () => onHighlightEndThenCallback(callback));
        }
        else
        {
            onHighlightEndThenCallback(callback);
        }
    }

    private void onHighlightEndThenCallback(Action callback)
    {
        WaitingVisual.SetActive(false);
        /*if(FrontContainerObject != null)
        {
            HoverTrigger hover = FrontContainerObject.GetComponent<HoverTrigger>();
            if(hover != null)
            {
                hover.enabled = true;
            }
        }*/
        callback?.Invoke();
    }

    private void startHighlight()
    {
        Debug.Log($"Container {gameObject.name} set Highlight");
        ContainerState = state.Highlight;

        if (transitionController != null)
        {
            transitionController.PlayTransition("Highlight", startHighlightPart2NoCallback);
        }
        else
        {
            setHighlightEnd();
        }
    }

    private void startHighlightPart2NoCallback()
    {
        Debug.Log($"Container {gameObject.name} set Highlight2");

        if (transitionController != null)
        {
            transitionController.PlayTransition("Highlight2", setHighlightEnd);
        }
        else
        {
            setHighlightEnd();
        }
    }

    private void setHighlightEnd()
    {
        /*if (FrontContainerObject != null)
        {
            HoverTrigger hover = FrontContainerObject.GetComponent<HoverTrigger>();
            if (hover != null)
            {
                hover.enabled = true;
            }
        }*/
        WaitingVisual.SetActive(false);
    }

    private void startIdle()
    {
        Debug.Log($"Container {gameObject.name} set Idle");
        ContainerState = state.Idle;
        /*if (FrontContainerObject != null)
        {
            FrontContainerObject.SetActive(false);
            HoverTrigger hover = FrontContainerObject.GetComponent<HoverTrigger>();
            if (hover != null)
            {
                hover.enabled = false;
            }
        }*/
        closeDropZone();

        if (transitionController != null)
        {
            transitionController.PlayTransition("Idle", startIdlePart2);
        }
        else
        {
            setIdleEnd();
        }
    }

    private void startIdlePart2()
    {
        Debug.Log($"Container {gameObject.name} set Idle2");

        if (transitionController != null)
        {
            transitionController.PlayTransition("Idle2", setIdleEnd);
        }
        else
        {
            setIdleEnd();
        }
    }

    private void setIdleEnd()
    {
        /*FrontContainerObject.GetComponent<SpriteRenderer>().color = new Color(FrontContainerObject.GetComponent<SpriteRenderer>().color.r, FrontContainerObject.GetComponent<SpriteRenderer>().color.g, FrontContainerObject.GetComponent<SpriteRenderer>().color.b, 0);
        FrontContainerObject.SetActive(true);*/
        WaitingVisual.SetActive(true);
        //FrontContainerObject.GetComponent<SpriteRenderer>().color = new Color(FrontContainerObject.GetComponent<SpriteRenderer>().color.r, FrontContainerObject.GetComponent<SpriteRenderer>().color.g, FrontContainerObject.GetComponent<SpriteRenderer>().color.b, 0);

    }

    private void openDropZone()
    {
        if (dropZoneCollider != null) dropZoneCollider.enabled = true;
        if (DropZoneHighLight != null) DropZoneHighLight.SetActive(true);
    }

    private void closeDropZone()
    {
        if (dropZoneCollider != null) dropZoneCollider.enabled = false;
        if (DropZoneHighLight != null) DropZoneHighLight.SetActive(false);
    }

    #endregion

    #region 废弃方法（保留供过渡期使用）

    [Obsolete("Use TransitionTo(state.Highlight, dropZoneShouldBeOpen: true) instead")]
    public void setHighlight_Start()
    {
        TransitionTo(state.Highlight, dropZoneShouldBeOpen: false);
    }

    [Obsolete("Use TransitionTo instead")]
    public void setHighlightAndOpenDropZone()
    {
        TransitionTo(state.Highlight, dropZoneShouldBeOpen: true);
    }

    [Obsolete("Use TransitionTo(state.Idle, false) instead")]
    public void setIdle_Start()
    {
        TransitionTo(state.Idle, dropZoneShouldBeOpen: false);
    }

    #endregion
}
