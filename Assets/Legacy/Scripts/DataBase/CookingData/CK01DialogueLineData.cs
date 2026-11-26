using System;
using System.Collections.Generic;
using UnityEngine;

// Schema v2.5 row data only. No scene, presentation or encrypted-release integration.
[Serializable]
public sealed class CK01DialogueLineData : ScriptableObject
{
    // 台词主键，填写ln_开头的小写ID。
    public string line_id = string.Empty;
    // 所属对话标签，引用Dialogues；链与标签不符由后续内容校验告警。
    public string dialogue_id = string.Empty;
    // 下一句台词ID；留空表示最后一句，按链读取，不按CSV行序推进。
    public string next_line = string.Empty;
    // 普通/剧情对话必填角色ID（包含旁白），气泡类可空；空时使用挂载气泡池NPC，需场景时后续检查。
    public string speaker = string.Empty;
    // 仅本句名字框覆盖本地化键；留空保留角色名字规则。
    public string name_override_key = string.Empty;
    // 台词正文的本地化键；在Localization中配置实际文字。
    public string text_key = string.Empty;
    // 本句开始出字时执行的命令文本，原样保存，不在导入时执行。
    public string commands = string.Empty;
    // 完整版表情预留，本版不读。
    public string expression = string.Empty;
    // 完整版情绪预留，本版不读。
    public string emotion = string.Empty;
    // 完整版强调预留，本版不读。
    public string emphasis = string.Empty;
    // 完整版场景情绪预留，本版不读。
    public string scene_emote = string.Empty;
    // 人工备注，不参与运行逻辑。
    public string comment = string.Empty;
}
