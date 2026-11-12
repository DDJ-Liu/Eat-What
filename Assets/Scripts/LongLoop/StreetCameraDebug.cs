using UnityEngine;

namespace EatWhat.LongLoop
{
    [DisallowMultipleComponent]
    public class StreetCameraDebug : MonoBehaviour
    {
        [SerializeField] private bool loggingEnabled = false;
        [SerializeField] private bool logNextSample = false;
        [SerializeField] private float minimumIntervalSeconds = 1f;
        private float lastLogTime=float.NegativeInfinity;
        public StreetFollowResult LastResult { get; private set; }
        public ulong LastSequence { get; private set; }
        public float LastSize { get; private set; }
        public Vector2 LastLookInput { get; private set; }
        public Vector2 LastMoveInput { get; private set; }
        public string LastStatus { get; private set; }
        public int LoggedSamples { get; private set; }
        public void ConfigureLogging(bool on,float minimumInterval)
        { loggingEnabled=on; minimumIntervalSeconds=minimumInterval; }
        public void RequestSample() { logNextSample=true; }
        public void Capture(StreetFollowResult result,string status,float now,ulong sequence,float size,Vector2 lookInput,Vector2 moveInput)
        {
            LastResult=result; LastStatus=status; LastSequence=sequence; LastSize=size; LastLookInput=lookInput; LastMoveInput=moveInput;
            if(!loggingEnabled || !logNextSample || float.IsNaN(now) || float.IsInfinity(now)) return;
            float interval=minimumIntervalSeconds;
            if(float.IsNaN(interval) || float.IsInfinity(interval) || interval<0) interval=1f;
            if(now>=lastLogTime && now-lastLogTime<interval) return;
            logNextSample=false; lastLogTime=now; LoggedSamples++;
            Debug.Log(FormatLine());
        }
        public string FormatLine()
        {
            var result=LastResult;var r=result.Channels;var s=result.State;
            return $"StreetFollow status={LastStatus} sequence={LastSequence} target={result.Target} goal={s.Goal} "+
                $"weight={s.Lookahead.SignedWeight} stopped={s.StoppedSeconds} X={Phase(s.X)} Y={Phase(s.Y)} "+
                $"raw=({r.RawSum.x},{r.RawSum.y}) clipped=({r.ClippedSum.x},{r.ClippedSum.y}) gate={r.GateApplied} "+
                $"outside={r.BaselineOutside} size={LastSize} time={s.LastTime} valid={result.Valid}";
        }
        private static string Phase(RecenterAxisState s)=>!s.Active?"Inactive":s.Time>=s.Duration?"Complete":
            s.Time<s.Ramp?"Accelerating":s.Time<s.Ramp+s.Cruise?"Cruising":"Decelerating";
        protected virtual void OnDisable() { logNextSample=false; }
    }
}
