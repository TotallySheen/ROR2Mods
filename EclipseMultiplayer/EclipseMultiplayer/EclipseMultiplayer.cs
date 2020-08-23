using System;
using BepInEx;
using RoR2;

namespace EclipseMultiplayer
{
    [BepInPlugin("com.sheen.EclipseMultiplayer", "Eclipse Multiplayer", "1.0.0")]
    public class EclipseMultiplayer : BaseUnityPlugin
    {
        public void Awake()
        {
            // Changing this value makes the range of default difficulties include all
            // 8 levels of Eclipse, instead of just the default 3
            DifficultyCatalog.standardDifficultyCount = 11;
        }
    }
}
