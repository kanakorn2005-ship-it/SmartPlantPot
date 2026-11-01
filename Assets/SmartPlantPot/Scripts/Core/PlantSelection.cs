using System;
using UnityEngine;

namespace SmartPlantPot
{
    /// <summary>Which plant the user tapped. The info card, slots and mascot listen to this.</summary>
    public static class PlantSelection
    {
        public static PlantUnit Current { get; private set; }
        public static event Action<PlantUnit> Changed;

        public static void Select(PlantUnit plant)
        {
            if (Current == plant) return;
            Current = plant;
            if (Changed != null) Changed(plant);
        }

        /// <summary>Tap a plant to open its card, tap it again to close.</summary>
        public static void Toggle(PlantUnit plant)
        {
            Select(Current == plant ? null : plant);
        }

        public static void Clear()
        {
            Select(null);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Current = null;
            Changed = null;
        }
    }
}
