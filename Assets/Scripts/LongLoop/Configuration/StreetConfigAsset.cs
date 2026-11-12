using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace EatWhat.LongLoop.Configuration
{
    public abstract class StreetConfigAsset : ScriptableObject
    {
        public event Action Changed;

        private string lastSerializedState;

        protected bool SetValue<T>(ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            lastSerializedState = SerializedState();
            Changed?.Invoke();
            return true;
        }

        protected virtual void OnEnable()
        {
            lastSerializedState = SerializedState();
        }

        protected virtual void OnValidate()
        {
            var state = SerializedState();
            if (lastSerializedState == null)
            {
                lastSerializedState = state;
                return;
            }
            if (string.Equals(lastSerializedState, state, StringComparison.Ordinal)) return;
            lastSerializedState = state;
            Changed?.Invoke();
        }

        internal void CaptureSerializedState()
        {
            lastSerializedState = SerializedState();
        }

        protected abstract string SerializedState();

        protected static string StateKey(params object[] values)
        {
            var parts = new string[values.Length];
            for (var i = 0; i < values.Length; i++)
                parts[i] = Convert.ToString(values[i], CultureInfo.InvariantCulture);
            return string.Join("|", parts);
        }
    }
}
