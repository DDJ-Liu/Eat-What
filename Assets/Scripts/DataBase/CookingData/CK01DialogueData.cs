using System;
using System.Collections.Generic;
using UnityEngine;

// Schema v2.5 row data only. No scene, presentation or encrypted-release integration.
[Serializable]
public sealed class CK01DialogueData : ScriptableObject
{
    // 对话主键，填写dlg_开头的小写ID。
    public string dialogue_id = string.Empty;
    // ambient路人气泡、normal普通、story剧情，填写对应小写枚举。
    public string tier = string.Empty;
    // 出场角色ID，引用Characters；从01连续填，空表示本位置无立绘。 当前四组为本版列，人数增加时按同模式扩列，不设置永久四人上限。
    public string cast_01 = string.Empty;
    // 站位模板中的站位ID；空按出场人数选择默认站位，显式位置优先。 当前四组为本版列，人数增加时按同模式扩列，不设置永久四人上限。
    public string cast_01_station = string.Empty;
    // 显示锚点相对屏幕底边中点的横向px偏移，2560×1440基准，向右为正；填写后优先于站位。 当前四组为本版列，人数增加时按同模式扩列，不设置永久四人上限。
    public CK01OptionalFloat cast_01_pos_x;
    // 显示锚点相对屏幕底边中点的纵向px偏移，2560×1440基准，向上为正；保留空与显式0的区别。 当前四组为本版列，人数增加时按同模式扩列，不设置永久四人上限。
    public CK01OptionalFloat cast_01_pos_y;
    // 本段额外缩放，与角色表缩放相乘；留空由后续适配处理。 当前四组为本版列，人数增加时按同模式扩列，不设置永久四人上限。
    public CK01OptionalFloat cast_01_scale;
    // 立绘前后层号，数字大在前，允许有符号整数；留空与0分别保存。 当前四组为本版列，人数增加时按同模式扩列，不设置永久四人上限。
    public CK01OptionalInt cast_01_order;
    // 出场角色ID，引用Characters；从01连续填，空表示本位置无立绘。 当前四组为本版列，人数增加时按同模式扩列，不设置永久四人上限。
    public string cast_02 = string.Empty;
    // 站位模板中的站位ID；空按出场人数选择默认站位，显式位置优先。 当前四组为本版列，人数增加时按同模式扩列，不设置永久四人上限。
    public string cast_02_station = string.Empty;
    // 显示锚点相对屏幕底边中点的横向px偏移，2560×1440基准，向右为正；填写后优先于站位。 当前四组为本版列，人数增加时按同模式扩列，不设置永久四人上限。
    public CK01OptionalFloat cast_02_pos_x;
    // 显示锚点相对屏幕底边中点的纵向px偏移，2560×1440基准，向上为正；保留空与显式0的区别。 当前四组为本版列，人数增加时按同模式扩列，不设置永久四人上限。
    public CK01OptionalFloat cast_02_pos_y;
    // 本段额外缩放，与角色表缩放相乘；留空由后续适配处理。 当前四组为本版列，人数增加时按同模式扩列，不设置永久四人上限。
    public CK01OptionalFloat cast_02_scale;
    // 立绘前后层号，数字大在前，允许有符号整数；留空与0分别保存。 当前四组为本版列，人数增加时按同模式扩列，不设置永久四人上限。
    public CK01OptionalInt cast_02_order;
    // 出场角色ID，引用Characters；从01连续填，空表示本位置无立绘。 当前四组为本版列，人数增加时按同模式扩列，不设置永久四人上限。
    public string cast_03 = string.Empty;
    // 站位模板中的站位ID；空按出场人数选择默认站位，显式位置优先。 当前四组为本版列，人数增加时按同模式扩列，不设置永久四人上限。
    public string cast_03_station = string.Empty;
    // 显示锚点相对屏幕底边中点的横向px偏移，2560×1440基准，向右为正；填写后优先于站位。 当前四组为本版列，人数增加时按同模式扩列，不设置永久四人上限。
    public CK01OptionalFloat cast_03_pos_x;
    // 显示锚点相对屏幕底边中点的纵向px偏移，2560×1440基准，向上为正；保留空与显式0的区别。 当前四组为本版列，人数增加时按同模式扩列，不设置永久四人上限。
    public CK01OptionalFloat cast_03_pos_y;
    // 本段额外缩放，与角色表缩放相乘；留空由后续适配处理。 当前四组为本版列，人数增加时按同模式扩列，不设置永久四人上限。
    public CK01OptionalFloat cast_03_scale;
    // 立绘前后层号，数字大在前，允许有符号整数；留空与0分别保存。 当前四组为本版列，人数增加时按同模式扩列，不设置永久四人上限。
    public CK01OptionalInt cast_03_order;
    // 出场角色ID，引用Characters；从01连续填，空表示本位置无立绘。 当前四组为本版列，人数增加时按同模式扩列，不设置永久四人上限。
    public string cast_04 = string.Empty;
    // 站位模板中的站位ID；空按出场人数选择默认站位，显式位置优先。 当前四组为本版列，人数增加时按同模式扩列，不设置永久四人上限。
    public string cast_04_station = string.Empty;
    // 显示锚点相对屏幕底边中点的横向px偏移，2560×1440基准，向右为正；填写后优先于站位。 当前四组为本版列，人数增加时按同模式扩列，不设置永久四人上限。
    public CK01OptionalFloat cast_04_pos_x;
    // 显示锚点相对屏幕底边中点的纵向px偏移，2560×1440基准，向上为正；保留空与显式0的区别。 当前四组为本版列，人数增加时按同模式扩列，不设置永久四人上限。
    public CK01OptionalFloat cast_04_pos_y;
    // 本段额外缩放，与角色表缩放相乘；留空由后续适配处理。 当前四组为本版列，人数增加时按同模式扩列，不设置永久四人上限。
    public CK01OptionalFloat cast_04_scale;
    // 立绘前后层号，数字大在前，允许有符号整数；留空与0分别保存。 当前四组为本版列，人数增加时按同模式扩列，不设置永久四人上限。
    public CK01OptionalInt cast_04_order;
    // tier组命名模板；留空选择default，本阶段只保存名字。
    public string tier_profile = string.Empty;
    // entrance组命名模板；留空选择default，本阶段只保存名字。
    public string entrance_profile = string.Empty;
    // exit组命名模板；留空选择default，本阶段只保存名字。
    public string exit_profile = string.Empty;
    // layout组命名模板；留空选择default，本阶段只保存名字。
    public string layout_profile = string.Empty;
    // speaker组命名模板；留空选择default，本阶段只保存名字。
    public string speaker_profile = string.Empty;
    // expression组命名模板；留空选择default，本阶段只保存名字。
    public string expression_profile = string.Empty;
    // text组命名模板；留空选择default，本阶段只保存名字。
    public string text_profile = string.Empty;
    // reaction组命名模板；留空选择default，本阶段只保存名字。
    public string reaction_profile = string.Empty;
    // cg组命名模板；留空选择default，本阶段只保存名字。
    public string cg_profile = string.Empty;
    // 本段覆盖集key=value;key=value，key取演出参数注册表；只对本段生效，本卡不应用覆盖。
    public string overrides = string.Empty;
    // 台词链第一句，引用DialogueLines.line_id；顺序/归属由链定义。
    public string first_line = string.Empty;
    // 接续对话ID，引用Dialogues；有切场景命令时等切换完成后开始，气泡不使用。
    public string next_dialogue = string.Empty;
    // 结束命令cmd(arg);cmd(arg)，原样保存，后续调度消费。
    public string on_end = string.Empty;
    // 人工备注，不参与运行逻辑。
    public string comment = string.Empty;
}
