using System.Collections;
using UnityEngine;

public abstract class TransitionBehaviour : MonoBehaviour
{
    protected TransitionController controller;
    protected Coroutine activeAnimationCoroutine;

    protected virtual void Awake()
    {
        controller = GetComponent<TransitionController>();
        if (controller == null)
        {
            Debug.LogError($"TransitionBehaviour on {gameObject.name} requires TransitionController component");
            return;
        }
        controller.RegisterBehaviour(this);
    }

    public Coroutine PlayTransition(string transitionName)
    {
        StopActiveAnimation();
        Debug.Log($"{gameObject.name} Plays Transition: {transitionName}");
        activeAnimationCoroutine = StartCoroutine(TransitionCoroutine(transitionName));
        return activeAnimationCoroutine;
    }

    public void StopActiveAnimation()
    {
        if (activeAnimationCoroutine != null)
        {
            StopCoroutine(activeAnimationCoroutine);
            activeAnimationCoroutine = null;
        }
    }

    protected abstract IEnumerator TransitionCoroutine(string transitionName);
}
