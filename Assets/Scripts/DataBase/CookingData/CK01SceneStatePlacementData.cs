using System;
using System.Collections.Generic;
using UnityEngine;

// Schema v2.5 row data only. No scene, presentation or encrypted-release integration.
[Serializable]
public sealed class CK01SceneStatePlacementData : ScriptableObject
{
    // 所属场景ID，引用Scenes；共同主键之一。
    public string scene_id = string.Empty;
    // 同场景的状态ID，引用SceneStates；共同主键之一。
    public string state_id = string.Empty;
    // actor角色、object物件、exit出口；共同主键之一。
    public string kind = string.Empty;
    // 角色条目填角色标记ID（可带#2等实例后缀），物件/出口填对象ID；共同主键之一。
    public string target = string.Empty;
    // 仅角色使用；无预摆实例时必填锚点ID，有预摆实例时空表示保留其位置。离线不猜场景实例。
    public string anchor_id = string.Empty;
    // 仅角色使用，left或right；留空保持取值链原语义。
    public string facing = string.Empty;
    // 角色是否在场、物件是否启用、出口true为解锁，填写true或false。
    public bool active;
    // 仅角色使用的对话ID；空按Schema约定B回落实例自身。
    public string dialogue_id = string.Empty;
    // 仅角色使用的气泡池ID；空按Schema约定B回落实例自身。
    public string ambient_pool = string.Empty;
    // 仅角色使用，none/talk/story；空按Schema约定B推导，不改为空间固定默认。
    public string marker = string.Empty;
    // 人工备注，不参与运行逻辑。
    public string comment = string.Empty;
}
