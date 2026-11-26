using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>One visual fridge-cat tier with the fixed five-column shelf contract.</summary>
    [DisallowMultipleComponent]
    public sealed class FridgeCatTier : MonoBehaviour
    {
        public const int ShelfColumnCount = 5;

        [SerializeField] private Transform[] shelfAnchors = new Transform[ShelfColumnCount];

        public Transform GetShelfAnchor(int column)
        {
            if (column < 0 || column >= ShelfColumnCount ||
                shelfAnchors == null || shelfAnchors.Length != ShelfColumnCount)
            {
                return null;
            }

            return shelfAnchors[column];
        }

        public bool HasCompleteShelfContract
        {
            get
            {
                if (shelfAnchors == null || shelfAnchors.Length != ShelfColumnCount)
                {
                    return false;
                }

                for (var index = 0; index < shelfAnchors.Length; index++)
                {
                    if (shelfAnchors[index] == null)
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
