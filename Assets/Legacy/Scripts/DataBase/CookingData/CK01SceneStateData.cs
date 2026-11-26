using System;
using System.Collections.Generic;
using UnityEngine;

// Schema v2.5 row data only. No scene, presentation or encrypted-release integration.
[Serializable]
public sealed class CK01SceneStateData : ScriptableObject
{
    // 所属场景ID，引用Scenes；与state_id共同构成主键。
    public string scene_id = string.Empty;
    // 场景内唯一的状态ID，不带统一前缀；可在不同场景使用同名状态。
    public string state_id = string.Empty;
    // 说明性剧情阶段，程序不读取。
    public string story_stage = string.Empty;
    // 状态说明，填写供策划阅读的文字。
    public string description = string.Empty;
    // 说明性切换来源，程序不读取。
    public string switch_source = string.Empty;
    // 人工备注，不参与运行逻辑。
    public string comment = string.Empty;
}
