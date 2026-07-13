using UnityEngine;
using System.Collections.Generic;

namespace PainQuest.Scoring
{
    [System.Serializable]
    public class ExerciseScoringConfig
    {
        [Header("Joint Configuration")]
        public PoseJoint jointA = PoseJoint.LeftShoulder;
        public PoseJoint jointVertex = PoseJoint.LeftHip;
        public PoseJoint jointB = PoseJoint.LeftKnee;

        [Header("Bilateral Tracking")]
        public bool useBilateral = false;
        public PoseJoint jointA_R = PoseJoint.RightShoulder;
        public PoseJoint jointVertex_R = PoseJoint.RightHip;
        public PoseJoint jointB_R = PoseJoint.RightKnee;

        [Header("Angle Thresholds")]
        [Range(30, 180)] public float upThreshold = 160f;
        [Range(30, 180)] public float downThreshold = 120f;

        [Header("Rep Settings")]
        [Range(5, 20)] public int targetReps = 10;
        [Range(10, 60)] public float minRange = 25f;

        [Header("Timing Overrides (0 = use RepAnalyzer default)")]
        [Tooltip("Minimum time (seconds) a full down-and-up motion must take to " +
                 "count as one rep. Raise this for slow, deliberate exercises " +
                 "(e.g. single-leg balance work) where a wobble/pause mid-rep " +
                 "can otherwise look like two quick reps. Lower it for fast " +
                 "compound movements. 0 = use RepAnalyzer's built-in default.")]
        public float minRepDurationOverride = 0f;
        [Tooltip("Minimum time (seconds) between two counted reps. 0 = use " +
                 "RepAnalyzer's built-in default.")]
        public float repCooldownOverride = 0f;

        private static Dictionary<string, ExerciseScoringConfig> _presets;

        public static Dictionary<string, ExerciseScoringConfig> Presets
        {
            get
            {
                if (_presets == null)
                {
                    _presets = new Dictionary<string, ExerciseScoringConfig>
                    {
                        ["AirSquat"] = AirSquat(),
                        ["ArmStretch"] = ArmStretch(),
                        ["BicycleCrunch"] = BicycleCrunch(),
                        ["Burpee"] = Burpee(),
                        ["CircleCrunch"] = CircleCrunch(),
                        ["CrossJumps"] = CrossJumps(),
                        ["JumpingJacks"] = JumpingJack(),
                        ["PistolSquat"] = PistolSquat(),
                        ["Plank"] = Plank(),
                        ["Pushup"] = Pushup(),
                        ["Situps"] = Situps()
                    };
                }
                return _presets;
            }
        }

        public static ExerciseScoringConfig GetConfig(string exerciseName)
        {
            string normalized = Normalize(exerciseName);
            foreach (var kvp in Presets)
            {
                if (Normalize(kvp.Key) == normalized)
                    return kvp.Value;
            }

            string validKeys = string.Join(", ", Presets.Keys);
            Debug.LogError(
                $"[ExerciseScoringConfig] No preset for '{exerciseName}' " +
                $"(normalized: '{normalized}'). Valid names: {validKeys}. " +
                $"Falling back to Burpee.");
            return Burpee();
        }

        public static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            string n = s.Replace(" ", "").Replace("_", "").Replace("-", "")
                        .ToLowerInvariant();
            if (n.Length > 1 && n.EndsWith("s"))
                n = n.Substring(0, n.Length - 1);
            return n;
        }

        // ─── EXERCISE PRESETS ──────────────────────────────────────────────
        // UNCHANGED from your working version: ArmStretch, AirSquat,
        // BicycleCrunch, CircleCrunch, CrossJumps, JumpingJacks, Plank, Pushup.
        // Only PistolSquat and Burpee are kept as your already-fixed values
        // (verified sound, not touched). Situps is the one actually adjusted
        // below -- see its comment for why.

        // Arm Stretch — UNCHANGED
        public static ExerciseScoringConfig ArmStretch() => new()
        {
            jointA = PoseJoint.LeftShoulder,
            jointVertex = PoseJoint.LeftElbow,
            jointB = PoseJoint.LeftWrist,
            useBilateral = false,
            upThreshold = 160f,
            downThreshold = 100f,
            targetReps = 8,
            minRange = 25f
        };

        // Air Squat — UNCHANGED
        public static ExerciseScoringConfig AirSquat() => new()
        {
            jointA = PoseJoint.LeftHip,
            jointVertex = PoseJoint.LeftKnee,
            jointB = PoseJoint.LeftAnkle,
            useBilateral = true,
            jointA_R = PoseJoint.RightHip,
            jointVertex_R = PoseJoint.RightKnee,
            jointB_R = PoseJoint.RightAnkle,
            upThreshold = 165f,
            downThreshold = 85f,
            targetReps = 10,
            minRange = 35f
        };

        // Bicycle Crunch — UNCHANGED
        public static ExerciseScoringConfig BicycleCrunch() => new()
        {
            jointA = PoseJoint.LeftShoulder,
            jointVertex = PoseJoint.LeftHip,
            jointB = PoseJoint.LeftKnee,
            useBilateral = true,
            jointA_R = PoseJoint.RightShoulder,
            jointVertex_R = PoseJoint.RightHip,
            jointB_R = PoseJoint.RightKnee,
            upThreshold = 150f,
            downThreshold = 100f,
            targetReps = 12,
            minRange = 25f
        };

        // Burpee — kept exactly as your already-fixed version (not touched).
        public static ExerciseScoringConfig Burpee() => new()
        {
            jointA = PoseJoint.LeftShoulder,
            jointVertex = PoseJoint.LeftHip,
            jointB = PoseJoint.LeftKnee,
            useBilateral = true,
            jointA_R = PoseJoint.RightShoulder,
            jointVertex_R = PoseJoint.RightHip,
            jointB_R = PoseJoint.RightKnee,
            upThreshold = 140f,
            downThreshold = 55f,
            targetReps = 8,
            minRange = 50f,
            minRepDurationOverride = 0.4f,
            repCooldownOverride = 0.5f
        };

        // Circle Crunch — UNCHANGED
        public static ExerciseScoringConfig CircleCrunch() => new()
        {
            jointA = PoseJoint.LeftShoulder,
            jointVertex = PoseJoint.LeftHip,
            jointB = PoseJoint.LeftKnee,
            useBilateral = true,
            jointA_R = PoseJoint.RightShoulder,
            jointVertex_R = PoseJoint.RightHip,
            jointB_R = PoseJoint.RightKnee,
            upThreshold = 150f,
            downThreshold = 100f,
            targetReps = 10,
            minRange = 20f
        };

        // Cross Jumps — UNCHANGED
        public static ExerciseScoringConfig CrossJumps() => new()
        {
            jointA = PoseJoint.LeftHip,
            jointVertex = PoseJoint.LeftKnee,
            jointB = PoseJoint.LeftAnkle,
            useBilateral = true,
            jointA_R = PoseJoint.RightHip,
            jointVertex_R = PoseJoint.RightKnee,
            jointB_R = PoseJoint.RightAnkle,
            upThreshold = 160f,
            downThreshold = 100f,
            targetReps = 12,
            minRange = 30f
        };

        // Jumping Jacks — UNCHANGED
        public static ExerciseScoringConfig JumpingJack() => new()
        {
            jointA = PoseJoint.LeftElbow,
            jointVertex = PoseJoint.LeftShoulder,
            jointB = PoseJoint.LeftHip,
            useBilateral = true,
            jointA_R = PoseJoint.RightElbow,
            jointVertex_R = PoseJoint.RightShoulder,
            jointB_R = PoseJoint.RightHip,
            upThreshold = 160f,
            downThreshold = 40f,
            targetReps = 10,
            minRange = 80f
        };

        // Pistol Squat — kept exactly as your already-fixed version (not
        // touched). minRepDurationOverride 1.2s + repCooldownOverride 1.5s
        // is what stops slow micro-wobbles from being counted as extra reps;
        // targetReps lowered to 5 to match a realistic slow set.
        public static ExerciseScoringConfig PistolSquat() => new()
        {
            jointA = PoseJoint.LeftHip,
            jointVertex = PoseJoint.LeftKnee,
            jointB = PoseJoint.LeftAnkle,
            useBilateral = false,
            upThreshold = 165f,
            downThreshold = 75f,
            targetReps = 5,
            minRange = 40f,
            minRepDurationOverride = 1.2f,
            repCooldownOverride = 1.5f
        };

        // Plank — UNCHANGED
        public static ExerciseScoringConfig Plank() => new()
        {
            jointA = PoseJoint.LeftShoulder,
            jointVertex = PoseJoint.LeftElbow,
            jointB = PoseJoint.LeftWrist,
            useBilateral = true,
            jointA_R = PoseJoint.RightShoulder,
            jointVertex_R = PoseJoint.RightElbow,
            jointB_R = PoseJoint.RightWrist,
            upThreshold = 170f,
            downThreshold = 140f,
            targetReps = 5,
            minRange = 20f
        };

        // Pushup — UNCHANGED
        public static ExerciseScoringConfig Pushup() => new()
        {
            jointA = PoseJoint.LeftShoulder,
            jointVertex = PoseJoint.LeftElbow,
            jointB = PoseJoint.LeftWrist,
            useBilateral = true,
            jointA_R = PoseJoint.RightShoulder,
            jointVertex_R = PoseJoint.RightElbow,
            jointB_R = PoseJoint.RightWrist,
            upThreshold = 155f,
            downThreshold = 85f,
            targetReps = 10,
            minRange = 30f
        };

        
        public static ExerciseScoringConfig Situps() => new()
        {
            jointA = PoseJoint.LeftShoulder,
            jointVertex = PoseJoint.LeftHip,
            jointB = PoseJoint.LeftKnee,
            useBilateral = true,
            jointA_R = PoseJoint.RightShoulder,
            jointVertex_R = PoseJoint.RightHip,
            jointB_R = PoseJoint.RightKnee,
            upThreshold = 85f,      // was 95 — lowered further, real peak looked lower in your test
            downThreshold = 45f,    // was 50 — lowered slightly to keep a healthy gap/tolerance
            targetReps = 10,
            minRange = 10f,         // was 12 — minor loosening, upThreshold is the real fix here
            minRepDurationOverride = 0.35f,
            repCooldownOverride = 0.4f
        };
    }
}