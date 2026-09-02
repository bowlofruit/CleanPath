using System;
using UnityEngine;

namespace CleanPath.Config
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "CleanPath/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        public BallConfig Ball = BallConfig.Default();
        public ShotConfig Shot = ShotConfig.Default();
        public InfectionConfig Infection = InfectionConfig.Default();
    }

    [Serializable]
    public class BallConfig
    {
        /// <summary>Fallback radius when no scene ball is wired. Scene ball scale is used in play mode.</summary>
        public float playerRadius = 0.42f;
        /// <summary>Scales shot/charge ball radius: radius = k × ∛shotMass.</summary>
        public float radiusPerMassCbrt = 0.42f;
        public float minCriticalMassFrac = 0.05f;
        /// <summary>Starting mass budget for shots. Does not affect player ball size.</summary>
        public float startMass = 12f;

        public static BallConfig Default() => new BallConfig();
    }

    [Serializable]
    public class ShotConfig
    {
        public float chargeRatePerSec = 2.0f;
        /// <summary>Hold must last at least this long before a shot fires (prevents accidental taps).</summary>
        public float minHoldDuration = 0.2f;
        public float projectileSpeed = 9.0f;
        public float projectileAcceleration = 18.0f;

        public static ShotConfig Default() => new ShotConfig();
    }

    [Serializable]
    public class InfectionConfig
    {
        public float energyPerMass = 10.0f;
        public float blastMultiplier = 1.4f;
        /// <summary>Energy cost to infect each obstacle along the chain (prevents free spread when bushes touch).</summary>
        public float linkCost = 3.0f;
        public float costPerMeter = 12.0f;
        public float maxLinkGap = 1.2f;
        public float chainDelayPerHop = 0.14f;

        public static InfectionConfig Default() => new InfectionConfig();
    }
}
