using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;

public enum ConditionMode
{
    AND,    // 所有条件为 true 才允许
    OR      // 任一条件为 true 即允许
}

[System.Serializable]
public class ConditionReceiver
{
    public bool allowToUse = false;
    public ConditionMode conditionMode = ConditionMode.AND;
    public UnityEvent<ConditionReceiver> checkAllowEvent;
    private List<bool> _conditionResults = new List<bool>();

    /// <summary>
    /// 条件方法调用此方法报告结果，内部收集后按 conditionMode 组合
    /// </summary>
    public void ReportCondition(bool result)
    {
        _conditionResults.Add(result);
    }

    public bool Evaluate()
    {
        if (_conditionResults.Count == 0) return true;
        if (conditionMode == ConditionMode.AND)
            return _conditionResults.All(r => r);
        else
            return _conditionResults.Any(r => r);
    }

    /// <summary>
    /// 完整条件检查流程：Clear → Invoke → Evaluate → 写 allowToUse。
    /// 返回 true 表示有绑定的条件事件。
    /// </summary>
    public bool RunCheck()
    {
        if (checkAllowEvent == null || checkAllowEvent.GetPersistentEventCount() == 0)
            return false;
        _conditionResults.Clear();
        checkAllowEvent.Invoke(this);
        allowToUse = Evaluate();
        return true;
    }

    public void alwaysFalseCondition(ConditionReceiver receiver)
    {
        receiver.ReportCondition(false);
    }

    public void alwaysTrueCondition(ConditionReceiver receiver)
    {
        receiver.ReportCondition(true);
    }
}
