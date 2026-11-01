using System;
using UnityEngine;

namespace SmartPlantPot
{
    /// <summary>
    /// Base class for anything that feeds sensor readings into <see cref="PlantUnit"/>s
    /// (the simulator now, the real ESP32/cloud backend later).
    /// Only one should be enabled at a time.
    /// </summary>
    public abstract class PlantDataSource : MonoBehaviour
    {
        public static PlantDataSource Active { get; private set; }

        /// <summary>Raised when the connection state / label changes.</summary>
        public event Action ConnectionChanged;

        /// <summary>True when readings are arriving.</summary>
        public abstract bool IsConnected { get; }

        /// <summary>Short label for the header pill, e.g. "Simulated" or "Live".</summary>
        public abstract string ConnectionLabel { get; }

        /// <summary>Remote care: ask the pot to water / record a prune.</summary>
        public abstract void SendCareCommand(PlantUnit plant, CareAction action);

        protected virtual void OnEnable()
        {
            Active = this;
        }

        protected virtual void OnDisable()
        {
            if (Active == this) Active = null;
        }

        protected void RaiseConnectionChanged()
        {
            if (ConnectionChanged != null) ConnectionChanged();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Active = null;
        }
    }
}
