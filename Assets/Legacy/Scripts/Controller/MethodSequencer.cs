using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MethodSequencer : MonoBehaviour
{
    [Header("设置")]
    public float sequanceSpacingTime = 0.5f;

    [Header("方法序列")]
    public List<UnityEvent> methods = new List<UnityEvent>();

    [Header("事件")]
    public UnityEvent onSequenceStart;
    public UnityEvent onSequenceEnd;

    private Coroutine runningSequence;
    public bool isPlaying { get; private set; }

    /// <summary>
    /// 激活序列，按顺序间隔执行所有方法
    /// </summary>
    public void Activate()
    {
        if (isPlaying) return;
        runningSequence = StartCoroutine(RunSequence());
    }

    /// <summary>
    /// 中断当前正在执行的序列
    /// </summary>
    public void Stop()
    {
        if (runningSequence != null)
        {
            StopCoroutine(runningSequence);
            runningSequence = null;
        }
        isPlaying = false;
    }

    private IEnumerator RunSequence()
    {
        isPlaying = true;
        onSequenceStart?.Invoke();

        for (int i = 0; i < methods.Count; i++)
        {
            methods[i]?.Invoke();

            if (i < methods.Count - 1)
                yield return new WaitForSeconds(sequanceSpacingTime);
        }

        runningSequence = null;
        isPlaying = false;
        onSequenceEnd?.Invoke();
    }
}
