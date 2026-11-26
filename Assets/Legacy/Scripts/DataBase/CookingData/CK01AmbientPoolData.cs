using System;
using System.Collections.Generic;
using UnityEngine;

// Schema v2.5 row data only. No scene, presentation or encrypted-release integration.
[Serializable]
public sealed class CK01AmbientPoolData : ScriptableObject
{
    // 气泡池主键，填写amb_开头的小写ID。
    public string pool_id = string.Empty;
    // 说明性用途范围，供人工理解该池给谁用。
    public string scope = string.Empty;
    // 用分号;分隔的Dialogues ID，至少一条，每条应为ambient；本阶段不执行抽取逻辑。
    public List<string> dialogue_ids = new List<string>();
    // 人工备注，不参与运行逻辑。
    public string comment = string.Empty;
}
