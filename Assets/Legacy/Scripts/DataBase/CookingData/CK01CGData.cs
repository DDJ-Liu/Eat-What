using System;
using System.Collections.Generic;
using UnityEngine;

// Schema v2.5 row data only. No scene, presentation or encrypted-release integration.
[Serializable]
public sealed class CK01CGData : ScriptableObject
{
    // CG主键，填写cg_开头小写ID，供cg_show引用。
    public string cg_id = string.Empty;
    // CG图片文件基名，无路径/扩展名；只查约定目录中的精确文件名，本阶段不绑定。
    public string cg_sprite = string.Empty;
    // black_fade黑幕或cross_fade交叉淡化；留空回落演出CG配置。
    public string transition_mode = string.Empty;
    // 进入过渡时长，单位秒且非负；留空回落配置，显式0代表0时长。
    public CK01OptionalFloat fade_in;
    // 退出过渡时长，单位秒且非负；留空回落配置。
    public CK01OptionalFloat fade_out;
    // 图片自身目标缩放倍率；留空回落配置，不在本卡执行推镜。
    public CK01OptionalFloat ken_burns_zoom;
    // CG总横向漂移量，单位px@2560×1440，向右为正，允许负值；独立留空回落。
    public CK01OptionalFloat ken_burns_pan_x;
    // CG总纵向漂移量，单位px@2560×1440，向上为正，允许负值；独立留空回落。
    public CK01OptionalFloat ken_burns_pan_y;
    // 从完全显示开始到完整目标的时长，单位秒且非负；留空用配置。
    public CK01OptionalFloat ken_burns_duration;
    // linear/quadratic/smooth_step/ease_in_out进度曲线，放大与漂移共用；留空用配置。
    public string ken_burns_curve = string.Empty;
    // CG期间保留对话框；空用配置，与显式false不同。
    public CK01OptionalBool keep_box;
    // CG期间保留立绘；空用配置，与显式false不同。
    public CK01OptionalBool keep_portraits;
    // CG期间保留名字框；空用配置，与显式false不同。
    public CK01OptionalBool keep_name;
    // 人工备注，不参与运行逻辑。
    public string comment = string.Empty;
}
