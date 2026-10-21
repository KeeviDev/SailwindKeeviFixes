using System.Collections;
using UnityEngine;

namespace KeeviFixes.Runtime
{
    /// <summary>
    /// Attaches the springs of a boat's tied lines again one frame after the game switches the boat on.
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
