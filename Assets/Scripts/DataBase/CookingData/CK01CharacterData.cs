using System;
using System.Collections.Generic;
using UnityEngine;

// Schema v2.5 row data only. No scene, presentation or encrypted-release integration.
[Serializable]
public sealed class CK01CharacterData : ScriptableObject
{
    // 角色主键，填写chr_开头的小写ID；也是角色标记及actor:目标ID。
    public string character_id = string.Empty;
    // 名字框本地化键；留空不显示名字框，旁白也按此规则填写。
    public string name_key = string.Empty;
    // 全身立绘文件基名，无路径/扩展名且保留大小写；留空不能进入立绘出场名单，本阶段只查文件名不绑定。
    public string portrait_sprite = string.Empty;
    // 原画布宽度比例，-0.5到0.5；指定对齐显示锚点的横向点，留空语义为0，不把空改成实际0。
    public CK01OptionalFloat portrait_offset_x;
    // 原画布高度比例，0到1；0为底部，留空语义为0；先偏移再围绕显示锚点缩放。
    public CK01OptionalFloat portrait_offset_y;
    // 立绘缩放倍率，须大于0；留空语义为1，保留空值等待后续适配。
    public CK01OptionalFloat portrait_scale;
    // 街上形象Prefab文件基名，无路径/扩展名；留空表示配表不指定，回落到实例自身，无则没有形象。
    public string street_visual_prefab = string.Empty;
    // 导表派生的真实Prefab引用；不是CSV列，不替代上方原文件名。空列/缺失均为空，运行时由Unity适配层区分。
    public GameObject street_visual_prefab_asset = null;
    // 完整版表情预留文本；本版存储但不消费。
    public string expressions = string.Empty;
    // 情绪图标相对立绘的横向偏移，完整版预留，本阶段只存储。
    public CK01OptionalFloat emote_offset_x;
    // 情绪图标相对立绘的纵向偏移，完整版预留，本阶段只存储。
    public CK01OptionalFloat emote_offset_y;
    // 人工备注，不参与运行逻辑。
    public string comment = string.Empty;
}
