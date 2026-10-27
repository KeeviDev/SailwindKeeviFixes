using System;
using System.Reflection;
using HarmonyLib;
using KeeviFixes.Configuration;
using UnityEngine;

namespace KeeviFixes.Patches
{
    /// <summary>
    /// Sets the smoke of food, pipes, pots and kettles to match their heat as soon as they are created.
    /// </summary>
    [HarmonyPatch]
    internal static class ItemSpawnSmokePatches
    {
        private static readonly Action<CookableFood> UpdateFoodSmoke =
            InstanceAction<CookableFood>("UpdateParticles");

        private static readonly Action<CookableFoodSoup> UpdateSoupSmoke =
            InstanceAction<CookableFoodSoup>("UpdateSoupParticles");

        private static readonly Action<ShipItemPipe> UpdatePipeSmoke =
            InstanceAction<ShipItemPipe>("UpdateParticles");

        private static readonly AccessTools.FieldRef<ShipItemPipe, ParticleSystem> PipeSmoke =
            InstanceField<ShipItemPipe, ParticleSystem>("smokeParticles");

        /// <summary>
        /// Skips these patches, with a warning, when the game lacks the members they rely on.
        /// </summary>
        private static bool Prepare()
        {
            bool supported = UpdateFoodSmoke != null
                && UpdateSoupSmoke != null
                && UpdatePipeSmoke != null
                && PipeSmoke != null
                && AccessTools.DeclaredMethod(typeof(CookableFood), "Awake") != null
                && AccessTools.DeclaredMethod(typeof(CookableFoodSoup), "Awake") != null
                && AccessTools.DeclaredMethod(typeof(CookableFoodKettle), "Awake") != null
                && AccessTools.DeclaredMethod(typeof(ShipItem), "Awake") != null
                && HasField(typeof(CookableFood), "particles", typeof(ParticleSystem))
                && HasField(typeof(CookableFood), "currentHeat", typeof(float))
                && HasField(typeof(CookableFoodKettle), "boilParticles", typeof(ParticleSystem));
            if (!supported)
            {
                Plugin.Log.LogWarning(
                    "Could not find the smoke members of food and pipes - the game may have updated. " +
                    "Food, pipes, pots and kettles may still give off a puff of smoke when they appear.");
            }

            return supported;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CookableFood), "Awake")]
        private static void AfterFoodAwake(CookableFood __instance, ParticleSystem ___particles)
        {
            if (___particles != null && ModConfig.FixSmokeOnItemSpawn.Value)
                UpdateFoodSmoke(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CookableFoodSoup), "Awake")]
        private static void AfterSoupAwake(CookableFoodSoup __instance, ParticleSystem ___particles)
        {
            if (___particles != null && ModConfig.FixSmokeOnItemSpawn.Value)
                UpdateSoupSmoke(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CookableFoodKettle), "Awake")]
        private static void AfterKettleAwake(ParticleSystem ___boilParticles, float ___currentHeat)
        {
            if (___boilParticles == null || !ModConfig.FixSmokeOnItemSpawn.Value)
                return;

            ParticleSystem.EmissionModule emission = ___boilParticles.emission;
            emission.enabled = ___currentHeat >= 1f;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ShipItem), "Awake")]
        private static void AfterItemAwake(ShipItem __instance)
        {
            if (__instance is ShipItemPipe pipe && PipeSmoke(pipe) != null && ModConfig.FixSmokeOnItemSpawn.Value)
                UpdatePipeSmoke(pipe);
        }

        /// <summary>
        /// Returns a delegate calling the parameterless void instance method <paramref name="name"/> declared on
        /// <typeparamref name="T"/>, or null when there is no such method.
        /// </summary>
        private static Action<T> InstanceAction<T>(string name)
        {
            MethodInfo method = AccessTools.DeclaredMethod(typeof(T), name, Type.EmptyTypes);
            return method != null && method.ReturnType == typeof(void)
                ? AccessTools.MethodDelegate<Action<T>>(method)
                : null;
        }

        /// <summary>
        /// Returns a reference to the instance field <paramref name="name"/> of type <typeparamref name="TField"/> on
        /// <typeparamref name="T"/>, or null when there is no such field.
        /// </summary>
        private static AccessTools.FieldRef<T, TField> InstanceField<T, TField>(string name)
        {
            return HasField(typeof(T), name, typeof(TField)) ? AccessTools.FieldRefAccess<T, TField>(name) : null;
        }

        private static bool HasField(Type owner, string name, Type type)
        {
            FieldInfo field = AccessTools.Field(owner, name);
            return field != null && field.FieldType == type;
        }
    }
}
