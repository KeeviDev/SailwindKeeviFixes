using System.Collections;
using UnityEngine;

namespace KeeviFixes.Runtime
{
    /// <summary>
    /// Sits on a boat with tied lines and attaches their springs again one frame after the game switches the boat on,
    /// once the boat's physics body is back.
    /// </summary>
    internal sealed class MooredBoat : MonoBehaviour
    {
        private BoatMooringRopes boatLines;

        public static void AttachTo(BoatMooringRopes boatLines)
        {
            if (boatLines.GetComponent<MooredBoat>() == null)
                boatLines.gameObject.AddComponent<MooredBoat>().boatLines = boatLines;
        }

        private void OnEnable()
        {
            StartCoroutine(AttachSpringsNextFrame());
        }

        private IEnumerator AttachSpringsNextFrame()
        {
            yield return null;
            MooringRetie.AttachSprings(boatLines);
        }
    }
}
