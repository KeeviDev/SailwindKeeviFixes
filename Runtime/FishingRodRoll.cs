using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using KeeviFixes.Configuration;
using UnityEngine;

namespace KeeviFixes.Runtime
{
    /// <summary>
    /// Keeps the model of a fishing rod that isn't held in place while a fish pulls, with its bend turned toward the
    /// fish.
    /// </summary>
    internal static class FishingRodRoll
    {
        /// <summary>Rate, per second, at which the model eases toward its target pose.</summary>
        private const float EaseSpeed = 4f;

        /// <summary>
        /// Distance, in metres, the bobber and the rod's tip must lie off the rod's axis for the roll to follow them.
        /// </summary>
        private const float MinOffAxis = 0.02f;

        /// <summary>Distance, in metres, within which the model counts as back at its resting place.</summary>
        private const float RestTolerance = 0.001f;

        private static readonly AccessTools.FieldRef<ShipItemFishingRod, Animator> Model =
            Find<ShipItemFishingRod, Animator>("anim");

        private static readonly AccessTools.FieldRef<ShipItemFishingRod, Transform> LineAttachment =
            Find<ShipItemFishingRod, Transform>("ropeAtt");

        private static readonly ConditionalWeakTable<FishingRodFish, RodPose> Poses =
            new ConditionalWeakTable<FishingRodFish, RodPose>();

        private static bool warnedNoModel;

        /// <summary>Whether the game has the fishing rod fields this class reads.</summary>
        public static bool IsSupported
        {
            get { return Model != null && LineAttachment != null; }
        }

        /// <summary>
        /// Records the rod's resting pose and model parts for <see cref="AfterUpdate"/>. Logs a warning, once per
        /// session, when the model lacks those parts.
        /// </summary>
        public static void OnAwake(FishingRodFish fish, Transform rotator, ShipItemFishingRod rod)
        {
            Animator model = Model(rod);
            SkinnedMeshRenderer skin = model != null ? model.GetComponentInChildren<SkinnedMeshRenderer>(true) : null;
            Transform butt = skin != null ? skin.rootBone : null;
            Transform joint = butt != null ? ChildBone(skin, butt) : null;
            Transform tip = LineAttachment(rod);
            if (joint == null || tip == null)
            {
                if (!warnedNoModel)
                {
                    warnedNoModel = true;
                    Plugin.Log.LogWarning(
                        "Could not find the fishing rod's bones - the game may have updated. " +
                        "Fishing rods that aren't held may still turn out of place when a fish bites.");
                }

                return;
            }

            Poses.Remove(fish);
            Poses.Add(fish, new RodPose(rotator.localPosition, model.transform, butt, joint, tip));
        }

        /// <summary>
        /// Replaces the game's turn of the model of a rod that isn't held while it fights a fish: rolls the model about
        /// the rod's axis so the rod bends toward the bobber, with the rod's butt kept in place. Sets the roll at once on
        /// the first frame of such a fight and eases it afterwards. Otherwise eases the model back to its resting
        /// place. Must run right after the game's update of the rod.
        /// </summary>
        /// <param name="rotationBefore">The model's rotation before the game's update this frame.</param>
        public static void AfterUpdate(
            FishingRodFish fish,
            Transform rotator,
            ShipItemFishingRod rod,
            Transform bobber,
            Quaternion restRotation,
            Quaternion rotationBefore)
        {
            RodPose pose;
            if (!Poses.TryGetValue(fish, out pose))
                return;

            bool fighting = !fish.fishDead && fish.currentFish != null;
            if (!fighting || rod.held != null || !FixGuard.IsOn(ModConfig.FixFishingRodsTurning))
            {
                pose.Rolling = false;
                EaseBack(rotator, pose);
                return;
            }

            Vector3 shake = rotator.localPosition - (pose.Moved ? pose.LastPosition : pose.RestPosition);
            Quaternion rolled = RolledRotation(rotator, bobber.position, restRotation, pose);
            rotator.localRotation = pose.Rolling
                ? Quaternion.Lerp(rotationBefore, rolled, Time.deltaTime * EaseSpeed)
                : rolled;
            pose.Rolling = true;
            Vector3 position = ButtKeptPosition(rotator, restRotation, pose);
            rotator.localPosition = position + shake;
            pose.LastPosition = position;
            pose.Moved = true;
        }

        /// <summary>Eases a moved turning part's position back to its resting place.</summary>
        private static void EaseBack(Transform rotator, RodPose pose)
        {
            if (!pose.Moved)
                return;

            Vector3 position = Vector3.Lerp(rotator.localPosition, pose.RestPosition, Time.deltaTime * EaseSpeed);
            if ((position - pose.RestPosition).sqrMagnitude <= RestTolerance * RestTolerance)
            {
                position = pose.RestPosition;
                pose.Moved = false;
            }

            rotator.localPosition = position;
            pose.LastPosition = position;
        }

        /// <summary>
        /// Returns the resting rotation of the turning part rolled about the rod's axis so the rod bends toward the
        /// bobber, and stores that roll in <paramref name="pose"/>.
        /// </summary>
        private static Quaternion RolledRotation(
            Transform rotator,
            Vector3 bobber,
            Quaternion restRotation,
            RodPose pose)
        {
            Transform model = pose.Model;
            Vector3 butt = model.InverseTransformPoint(pose.Butt.position);
            Vector3 axis = model.InverseTransformPoint(pose.Joint.position) - butt;
            Vector3 bend = Vector3.ProjectOnPlane(model.InverseTransformPoint(pose.Tip.position) - butt, axis);

            Matrix4x4 restModel = Matrix4x4.TRS(pose.RestPosition, restRotation, rotator.localScale)
                * rotator.worldToLocalMatrix * model.localToWorldMatrix;
            Vector3 restAxis = restModel.MultiplyVector(axis).normalized;
            Vector3 restButt = restModel.MultiplyPoint3x4(butt);
            Vector3 restBend = Vector3.ProjectOnPlane(restModel.MultiplyVector(bend), restAxis);
            Vector3 toBobber = Vector3.ProjectOnPlane(rotator.parent.InverseTransformPoint(bobber) - restButt, restAxis);
            float minOffAxis = MinOffAxis * MinOffAxis;
            if (restBend.sqrMagnitude >= minOffAxis && toBobber.sqrMagnitude >= minOffAxis)
                pose.Roll = Vector3.SignedAngle(restBend, toBobber, restAxis);

            return Quaternion.AngleAxis(pose.Roll, restAxis) * restRotation;
        }

        /// <summary>
        /// Returns the position of the turning part, at its current rotation, that keeps the rod's butt at its resting
        /// place.
        /// </summary>
        private static Vector3 ButtKeptPosition(Transform rotator, Quaternion restRotation, RodPose pose)
        {
            Vector3 butt = Vector3.Scale(rotator.localScale, rotator.InverseTransformPoint(pose.Butt.position));
            return pose.RestPosition + restRotation * butt - rotator.localRotation * butt;
        }

        /// <summary>Returns the first bone of the skin whose parent is the butt, or null when there is none.</summary>
        private static Transform ChildBone(SkinnedMeshRenderer skin, Transform butt)
        {
            foreach (Transform bone in skin.bones)
            {
                if (bone != null && bone.parent == butt)
                    return bone;
            }

            return null;
        }

        private static AccessTools.FieldRef<TObject, TField> Find<TObject, TField>(string name)
        {
            FieldInfo field = AccessTools.Field(typeof(TObject), name);
            return field != null && field.FieldType == typeof(TField)
                ? AccessTools.FieldRefAccess<TObject, TField>(field)
                : null;
        }

        /// <summary>A rod's model parts, where its turning part rests, and how it is rolled now.</summary>
        private sealed class RodPose
        {
            public readonly Vector3 RestPosition;

            public readonly Transform Model;

            public readonly Transform Butt;

            /// <summary>The bone the rod bends at.</summary>
            public readonly Transform Joint;

            public readonly Transform Tip;

            public float Roll;

            /// <summary>Whether this class rolled the model on the previous frame.</summary>
            public bool Rolling;

            /// <summary>Whether this class moved the turning part off its resting place.</summary>
            public bool Moved;

            /// <summary>The turning part's last position set by this class, excluding the game's shaking.</summary>
            public Vector3 LastPosition;

            public RodPose(Vector3 restPosition, Transform model, Transform butt, Transform joint, Transform tip)
            {
                RestPosition = restPosition;
                Model = model;
                Butt = butt;
                Joint = joint;
                Tip = tip;
            }
        }
    }
}
