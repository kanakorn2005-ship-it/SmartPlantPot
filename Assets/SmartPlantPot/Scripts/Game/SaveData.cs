using System;
using System.Collections.Generic;

namespace SmartPlantPot
{
    /// <summary>Everything the game remembers between sessions (JSON in Application.persistentDataPath).</summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int points;
        public string lastDay = "";
        public int dayOffset;
        public List<InventoryItem> inventory = new List<InventoryItem>();
        public List<PotSave> pots = new List<PotSave>();
        public List<SpeciesConfig> species = new List<SpeciesConfig>();
        public GameSettings settings = new GameSettings();
        public List<string> ownedSkins = new List<string>();
        public string equippedSkin = "";
    }

    [Serializable]
    public class InventoryItem
    {
        public string id;
        public int count;
    }

    [Serializable]
    public class PotSave
    {
        public string potId;
        public string speciesId;
        public float growth;
        public float quality = 60f;
        public int streak;
        public int mutation;
        public string lastDailySellDay = "";
        public string lastFertilizeDay = "";
        public string plantedOn = "";
        public double daySum;
        public double daySeconds;
        public float fertilizerMinutesLeft;

        // servo
        public bool servoAutoWater;
        public float servoWaterSeconds = 3f;
        public float servoFertilizerSeconds = 2f;

        // mock sensor
        public bool simManual;
        public float simMoisture = -1f;
        public float simTemperature = -1f;
        public float simLight = -1f;
    }

    /// <summary>Settings changed from the in-game Mock / Config panel.</summary>
    [Serializable]
    public class GameSettings
    {
        public bool useRealSensors;
        public string readingsUrl = "http://192.168.1.50/api/pots";
        public string commandUrl = "";
        public bool useRealWeather = true;
        public double latitude = 13.7563;
        public double longitude = 100.5018;
        public int weatherOverride = -1;
        public float manualHour = -1f;
        public int eventOverride = -1;
        public float growthSpeed = 1f;
        public float remindAfterSeconds = 120f;
    }
}
