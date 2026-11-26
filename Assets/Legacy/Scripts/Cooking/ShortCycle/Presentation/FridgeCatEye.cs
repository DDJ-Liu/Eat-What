using UnityEngine;

namespace EatWhat.Cooking.ShortCycle
{
    /// <summary>One independently wired fridge-cat eye driven by shared Transition presets.</summary>
    [DisallowMultipleComponent]
    public sealed class FridgeCatEye : MonoBehaviour
    {
        [Header("Eye layers")]
        [SerializeField] private SpriteRenderer eyeBase = null;
        [SerializeField] private SpriteRenderer pupil = null;
        [SerializeField] private SpriteRenderer highlight = null;
        [SerializeField] private SpriteRenderer glow = null;

        [Header("TK-MOT-07 blink presets")]
        [SerializeField] private TransitionController blinkTransition = null;
        [SerializeField] private string blinkClosedPreset = "BlinkClosed";
        [SerializeField] private string blinkOpenPreset = "BlinkOpen";

        private int blinkRequestGeneration;

        public SpriteRenderer EyeBase { get { return eyeBase; } }
        public SpriteRenderer Pupil { get { return pupil; } }
        public SpriteRenderer Highlight { get { return highlight; } }
        public SpriteRenderer Glow { get { return glow; } }
        public bool CanBlink
        {
            get
            {
                return eyeBase != null && pupil != null && highlight != null && glow != null &&
                    blinkTransition != null &&
                    !string.IsNullOrWhiteSpace(blinkClosedPreset) &&
                    !string.IsNullOrWhiteSpace(blinkOpenPreset);
            }
        }

        /// <summary>
        /// Restarts this eye's close/open preset sequence. TransitionController cancels the prior
        /// preset coroutine first, so repeated requests cannot leave parallel blink coroutines.
        /// </summary>
        public bool Blink()
        {
            if (!CanBlink)
            {
                return false;
            }

            var requestGeneration = ++blinkRequestGeneration;
            return blinkTransition.GoTo(blinkClosedPreset, () =>
            {
                if (requestGeneration != blinkRequestGeneration)
                {
                    return;
                }

                blinkTransition.GoTo(blinkOpenPreset);
            });
        }

        /// <summary>Reserved for a future pupil-look implementation; layer references stay explicit.</summary>
        public void LookAt(Vector2 direction)
        {
            // CAT-T1 establishes the public contract only. Runtime gaze tuning belongs to a later task.
        }

        private void OnDisable()
        {
            blinkRequestGeneration++;
            if (blinkTransition != null)
            {
                blinkTransition.CancelAll();
            }
        }
    }
}
