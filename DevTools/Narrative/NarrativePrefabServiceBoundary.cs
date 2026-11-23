// External Runtime directory/resolution delivery only; no service, FSM, composition or UI behavior.
using System;
using EatWhat.Narrative.Core;
namespace EatWhat.Narrative.Unity
{
    public sealed class NarrativeRuntimeEntry : UnityEngine.MonoBehaviour
    {
        public INarrativeDataDirectory Directory;
        public Func<string,object> PortraitSprites;
        public object GetCharacterPortraitSprite(string id){return PortraitSprites==null?null:PortraitSprites(id);}
        public Func<NarrativeDialogue,NarrativeDialogueContinuationPolicy,DialoguePresentationResolution> Resolve;
        public bool TryGetDirectory(out INarrativeDataDirectory data){data=Directory;return data!=null;}
        public DialoguePresentationResolution ResolveForService(NarrativeDialogue dialogue,NarrativeDialogueContinuationPolicy policy){return Resolve(dialogue,policy);}
    }
}
