using System;
using EatWhat.LongLoop.Configuration;
using UnityEngine;

namespace EatWhat.LongLoop
{
    public enum StreetFollowPointStatus { Inactive, Ready, MissingReferences, MissingConfig, InvalidInput, ExternalChange }

    [DisallowMultipleComponent]
    public class StreetFollowPoint : MonoBehaviour
    {
        [SerializeField] private Transform foot;
        [SerializeField] private Transform ownedFollowPoint;
        private Transform capturedNode, capturedParent;
        private Vector3 originalLocal, lastLocal;
        private bool captured, written, lost, subscribed;
        public Transform Foot => foot;
        public Transform Node => ownedFollowPoint;
        public StreetFollowPointStatus Status { get; private set; }

        public void Configure(Transform independentFoot, Transform ownedChild)
        { Suspend(); foot=independentFoot; ownedFollowPoint=ownedChild; lost=false; }
        protected virtual void OnEnable()
        {
            lost=false;
            if (!subscribed) { StreetConfigs.Changed+=OnConfigChanged; subscribed=true; }
        }
        protected virtual void OnDisable() { Unsubscribe(); Suspend(); Status=StreetFollowPointStatus.Inactive; }
        protected virtual void OnDestroy() { Unsubscribe(); Suspend(); }
        private void Unsubscribe()
        { if(subscribed)StreetConfigs.Changed-=OnConfigChanged; subscribed=false; }
        private void OnConfigChanged()
        {
            if(StreetConfigs.Current==null || StreetConfigs.Current.DepthScale==null)
            { Suspend(); Status=StreetFollowPointStatus.MissingConfig; }
        }

        public bool TryUpdate()
        {
            if(!Application.isPlaying || !isActiveAndEnabled) return false;
            if(foot==null || ownedFollowPoint==null || ownedFollowPoint==foot || !ownedFollowPoint.IsChildOf(foot))
            { Status=StreetFollowPointStatus.MissingReferences; return false; }
            if(StreetConfigs.Current==null || StreetConfigs.Current.DepthScale==null)
            { Suspend(); Status=StreetFollowPointStatus.MissingConfig; return false; }
            if(captured && !ReferenceEquals(capturedNode,ownedFollowPoint)) Suspend();
            if(lost || (captured && (capturedNode.parent!=capturedParent ||
                (written && !capturedNode.localPosition.Equals(lastLocal)))))
            { lost=true; Status=StreetFollowPointStatus.ExternalChange; return false; }
            Vector3 value;
            try { value=GroundSpace.FollowPoint(foot.position); }
            catch(ArgumentException) { Status=StreetFollowPointStatus.InvalidInput; return false; }
            catch(InvalidOperationException) { Status=StreetFollowPointStatus.InvalidInput; return false; }
            if(!captured)
            {
                capturedNode=ownedFollowPoint; capturedParent=capturedNode.parent;
                originalLocal=capturedNode.localPosition; captured=true;
            }
            capturedNode.position=value;
            lastLocal=capturedNode.localPosition; written=true;
            Status=StreetFollowPointStatus.Ready;
            return true;
        }
        public void Suspend()
        {
            if(captured && written && !lost && capturedNode!=null && capturedNode.parent==capturedParent &&
                capturedNode.localPosition.Equals(lastLocal)) capturedNode.localPosition=originalLocal;
            captured=false; written=false; capturedNode=null; capturedParent=null;
        }
    }
}
