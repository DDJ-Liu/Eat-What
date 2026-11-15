using System;
using System.Collections.Generic;
using UnityEngine;

// Schema v2.5 row data only. No scene, presentation or encrypted-release integration.
[Serializable]
public sealed class CK01SceneData : ScriptableObject
{
    // 场景主键，填写scn_开头小写ID，供scene_switch/set_scene_state引用。
    public string scene_id = string.Empty;
    // 场景显示名本地化键。
    public string name_key = string.Empty;
    // street街道或interior室内，填写对应小写枚举。
    public string scene_type = string.Empty;
    // 是否按小房间规则取景，填写true或false。
    public bool is_small_room;
    // 实际场景文件基名，无路径/扩展名；本卡只存字段，不加载场景。
    public string scene_asset = string.Empty;
    // 无存档记录时使用的状态ID，须在同场景SceneStates中存在。
    public string default_state = string.Empty;
    // 人工备注，不参与运行逻辑。
    public string comment = string.Empty;
}
