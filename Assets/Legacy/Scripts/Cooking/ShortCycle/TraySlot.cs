using System.Collections.Generic;
using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    public sealed class TraySlot : ShortCycleGridPieceBase, IMouseHitDepth
    {
        public string ItemId { get; private set; }
        public string InstanceId { get; private set; }
        private bool occupied;
        private BoxCollider2D authoredCollider;
        private bool authoredColliderEnabled;
        private PolygonCollider2D pickupCollider;
        private Sprite pickupSprite;
        private Vector3 pickupOrigin, pickupAxisX, pickupAxisY;
        private bool geometryCaptured, pickupFlipX, pickupFlipY;
        private readonly List<Vector2> shape = new List<Vector2>();

        public object MouseHitScope
        {
            get
            {
                if (!occupied || !isActiveAndEnabled || !InteractionAllowed || pickupCollider == null ||
                    !pickupCollider.enabled || OwnerSession == null || OwnerSession.CurrentPhase != ShortCyclePhase.Phase1 ||
                    OwnerSession.TrayInteraction == null) return null;
                var tray = OwnerSession.TrayInteraction;
                return tray.ResolveDragAppearance(ItemId, InstanceId) == this ? tray : null;
            }
        }
        public string MouseHitKey { get { return InstanceId; } }

        public bool TryGetMouseHitDepth(Vector2 point, out float depth)
        {
            depth = 0f;
            if (MouseHitScope == null || !pickupCollider.OverlapPoint(point)) return false;
            var local = (Vector2)transform.InverseTransformPoint(point);
            var bestSquared = -1f;
            for (var index=0;index<pickupCollider.pathCount;index++)
            {
                pickupCollider.GetPath(index,shape);
                if (!ContainsPoint(shape,local)) continue;
                var nearestSquared = float.PositiveInfinity;
                for (int i=0,j=shape.Count-1;i<shape.Count;j=i++)
                {
                    var start=(Vector2)transform.TransformPoint(shape[j]);
                    var end=(Vector2)transform.TransformPoint(shape[i]);
                    var dx=end.x-start.x;var dy=end.y-start.y;
                    var lengthSquared=dx*dx+dy*dy;
                    var fraction=lengthSquared>0f ? Mathf.Clamp01(((point.x-start.x)*dx+(point.y-start.y)*dy)/lengthSquared) : 0f;
                    var x=point.x-start.x-fraction*dx;var y=point.y-start.y-fraction*dy;
                    nearestSquared=Mathf.Min(nearestSquared,x*x+y*y);
                }
                bestSquared=Mathf.Max(bestSquared,nearestSquared);
            }
            if (bestSquared<0f || float.IsInfinity(bestSquared)) return false;
            depth=(float)System.Math.Sqrt(bestSquared);
            return true;
        }
        private static bool ContainsPoint(List<Vector2> points,Vector2 point)
        {
            var inside=false;
            for (int i=0,j=points.Count-1;i<points.Count;j=i++)
                if ((points[i].y>point.y)!=(points[j].y>point.y) &&
                    point.x<(points[j].x-points[i].x)*(point.y-points[i].y)/(points[j].y-points[i].y)+points[i].x)
                    inside=!inside;
            return inside;
        }

        protected override void OnDisplayStateChanged() { RefreshPickupGeometry(); }
        private void LateUpdate() { RefreshPickupGeometry(); }

        private void RefreshPickupGeometry()
        {
            if (authoredCollider == null)
            {
                authoredCollider = GetComponent<BoxCollider2D>();
                if (authoredCollider == null) return;
                authoredColliderEnabled = authoredCollider.enabled;
            }
            if (!occupied || !ContentVisible || !InteractionAllowed || !gameObject.activeInHierarchy)
            {
                authoredCollider.enabled = false;
                if (pickupCollider != null) pickupCollider.enabled = false;
                return;
            }
            var icon = PrimaryIconRenderer;
            var frame = GetComponent<SpriteRenderer>();
            if (!Visible(icon))
            {
                if (pickupCollider != null) pickupCollider.enabled = false;
                authoredCollider.enabled = authoredColliderEnabled && Visible(frame);
                return;
            }
            Vector3 iconOrigin, iconAxisX, iconAxisY;
            GetBasis(icon,out iconOrigin,out iconAxisX,out iconAxisY);
            if (!geometryCaptured || pickupSprite != icon.sprite || pickupOrigin != iconOrigin ||
                pickupAxisX != iconAxisX || pickupAxisY != iconAxisY || pickupFlipX != icon.flipX || pickupFlipY != icon.flipY)
            {
                var paths = new List<Vector2[]>();
                // Only the food contour participates. A plate/frame must not steal
                // a neighbouring item's visible pixels through its transparent area.
                AppendShapes(icon,paths);
                if (paths.Count > 0)
                {
                    if (pickupCollider == null)
                    {
                        pickupCollider = gameObject.AddComponent<PolygonCollider2D>();
                        pickupCollider.isTrigger = true;
                    }
                    pickupCollider.pathCount = paths.Count;
                    for (var i=0;i<paths.Count;i++) pickupCollider.SetPath(i,paths[i]);
                }
                else if (pickupCollider != null) pickupCollider.pathCount = 0;
                pickupSprite=icon.sprite;pickupOrigin=iconOrigin;pickupAxisX=iconAxisX;pickupAxisY=iconAxisY;
                pickupFlipX=icon.flipX;pickupFlipY=icon.flipY;geometryCaptured=true;
            }
            var hasShape = pickupCollider != null && pickupCollider.pathCount > 0;
            if (pickupCollider != null) pickupCollider.enabled = authoredColliderEnabled && hasShape;
            // Preserve the authored box dimensions for DragContainer's grip/limits.
            // Sprites without imported shapes retain that box, never a guessed full-canvas rectangle.
            authoredCollider.enabled = authoredColliderEnabled && !hasShape;
        }
        private static bool Visible(SpriteRenderer renderer)
        { return renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy && renderer.sprite != null; }
        private void GetBasis(SpriteRenderer renderer,out Vector3 origin,out Vector3 x,out Vector3 y)
        {
            origin=x=y=Vector3.zero;
            if (renderer == null) return;
            origin=transform.InverseTransformPoint(renderer.transform.position);
            x=transform.InverseTransformPoint(renderer.transform.TransformPoint(Vector3.right))-origin;
            y=transform.InverseTransformPoint(renderer.transform.TransformPoint(Vector3.up))-origin;
        }
        private void AppendShapes(SpriteRenderer renderer,List<Vector2[]> paths)
        {
            for(var i=0;i<renderer.sprite.GetPhysicsShapeCount();i++)
            {
                renderer.sprite.GetPhysicsShape(i,shape);
                if(shape.Count<3)continue;
                var points=new Vector2[shape.Count];
                for(var j=0;j<shape.Count;j++)
                {
                    var p=shape[j];if(renderer.flipX)p.x=-p.x;if(renderer.flipY)p.y=-p.y;
                    points[j]=transform.InverseTransformPoint(renderer.transform.TransformPoint(p));
                }
                paths.Add(points);
            }
        }

        public void ConfigureItem(CK01ItemData item, int quantity)
        {
            ConfigureItem(item, quantity, item == null ? null : item.name, null);
        }

        public void ConfigureItem(CK01ItemData item, int quantity, string itemId, string instanceId)
        {
            occupied = item != null && quantity > 0 && !string.IsNullOrEmpty(itemId);
            ItemId = itemId;
            InstanceId = instanceId;
            Configure(new ShortCycleGridPieceDisplay
            {
                icon_sprite = item == null ? null : item.icon_sprite,
                name_key = item == null ? null : item.name_key,
                fallback_name = item == null ? string.Empty : item.name,
                badge_text = quantity > 1 ? "x" + quantity : string.Empty,
                is_placeholder = false
            });
        }
    }
}
