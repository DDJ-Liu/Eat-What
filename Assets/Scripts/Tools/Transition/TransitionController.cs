using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransitionController : MonoBehaviour
{
    private List<TransitionBehaviour> registeredBehaviours = new List<TransitionBehaviour>();
    private List<Coroutine> activeCoroutines = new List<Coroutine>();

    public void RegisterBehaviour(TransitionBehaviour behaviour)
    {
        if (!registeredBehaviours.Contains(behaviour))
        {
            registeredBehaviours.Add(behaviour);
        }
    }

    public void PlayTransition(string transitionName, System.Action onComplete = null)
    {
        Debug.Log($"{gameObject.name} Plays Transition: {transitionName}");
        StopAllTransitions();

        foreach (var behaviour in registeredBehaviours)
        {
            Coroutine coroutine = behaviour.PlayTransition(transitionName);
            if (coroutine != null)
            {
                activeCoroutines.Add(coroutine);
            }
        }

        if (onComplete != null)
        {
            StartCoroutine(WaitForAllAndCallback(onComplete));
        }
    }

    public void StopAllTransitions()
    {
        foreach (var behaviour in registeredBehaviours)
        {
            behaviour.StopActiveAnimation();
        }
        activeCoroutines.Clear();
    }

    private IEnumerator WaitForAllAndCallback(System.Action callback)
    {
        foreach (var coroutine in activeCoroutines)
        {
            if (coroutine != null)
            {
                yield return coroutine;
            }
        }

        activeCoroutines.Clear();

        callback?.Invoke();
    }
}
