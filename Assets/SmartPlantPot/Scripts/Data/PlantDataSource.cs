using System;
using System.Collections.Generic;
using UnityEngine;

namespace SmartPlantPot
{
    /// <summary>
    /// Base class for anything that feeds sensor readings into <see cref="PlantUnit"/>s
    /// (the simulator now, the real ESP32/cloud backend later).
    /// Several can be enabled together (real sensors for some pots, the simulator for the rest);
    /// <see cref="Active"/> is the enabled one with the highest <see cref="Priority"/> - it drives
    /// the header status pill and receives care / servo commands.
    /// </summary>
    public abstract class PlantDataSource : MonoBehaviour
    {
        public static PlantDataSource Active { get; private set; }

        static readonly List<PlantDataSource> enabledSources = new List<PlantDataSource>();

        /// <summary>Higher wins when several sources are enabled (real hardware beats the simulator).</summary>
        protected virtual int Priority
        {
            get { return 0; }
        }

        /// <summary>Raised when the connection state / label changes.</summary>
        public event Action ConnectionChanged;

        /// <summary>True when readings are arriving.</summary>
        public abstract bool IsConnected { get; }

        /// <summary>Short label for the header pill, e.g. "Simulated" or "Live".</summary>
        public abstract string ConnectionLabel { get; }

        /// <summary>Remote care: ask the pot to water / record a prune.</summary>
        public abstract void SendCareCommand(PlantUnit plant, CareAction action);

        /// <summary>Move the pot's servo valve (0 = closed, 90 = water, 180 = fertilizer) for some seconds (0 = hold).</summary>
        public virtual void SendServo(PlantUnit plant, float angle, float seconds)
        {
        }

        protected virtual void OnEnable()
        {
            enabledSources.Remove(this);
            enabledSources.Add(this);
            PickActive();
        }

        protected virtual void OnDisable()
        {
            enabledSources.Remove(this);
            PickActive();
        }

        static void PickActive()
        {
            PlantDataSource best = null;
            foreach (var s in enabledSources)
                if (s != null && (best == null || s.Priority >= best.Priority)) best = s;
            if (Active == best) return;
            Active = best;
            if (best != null) best.RaiseConnectionChanged();
        }

        protected void RaiseConnectionChanged()
        {
            if (ConnectionChanged != null) ConnectionChanged();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Active = null;
            enabledSources.Clear();
        }
    }
}
