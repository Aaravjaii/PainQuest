using UnityEngine;

namespace PainQuest
{
    // ═══════════════════════════════════════════════════════════════════════════
    // ExerciseData  —  ScriptableObject
    //
    // Create one asset per exercise via:
    //   Right-click Project → Create → PainQuest → Exercise Data
    //
    // Assign clips, durations, rest time, dialogue all in the Inspector.
    // ═══════════════════════════════════════════════════════════════════════════
    [CreateAssetMenu(menuName = "PainQuest/Exercise Data", fileName = "ExerciseData_New")]
    public class ExerciseData : ScriptableObject
    {
        [Header("Identity")]
        public string exerciseName = "Exercise Name";
        public PainQuestType questType;

        [Header("Animation Clips")]
        [Tooltip("One-shot clip played before the loop starts (e.g. Pistol Start). Leave empty if not needed.")]
        public AnimationClip startClip;

        [Tooltip("Looping clip played during the exercise duration.")]
        public AnimationClip loopClip;

        [Tooltip("One-shot clip played when the exercise ends (e.g. Pistol To Idle). Leave empty if not needed.")]
        public AnimationClip exitClip;

        [Header("Animator Trigger Names")]
        [Tooltip("Trigger name to fire the start clip. Must match parameter in Animator exactly.")]
        public string startTrigger = "";

        [Tooltip("Trigger name to fire the loop clip. Must match parameter in Animator exactly.")]
        public string loopTrigger = "";

        [Tooltip("Trigger name to re-fire the loop each cycle (for retriggered loops). Leave empty if the loop trigger is enough.")]
        public string loopRetrigger = "";

        [Tooltip("Trigger name to fire the exit clip. Must match parameter in Animator exactly.")]
        public string exitTrigger = "";

        [Header("Timing")]
        [Tooltip("Duration in seconds for the start one-shot clip before loop begins.")]
        [Range(0f, 3f)]
        public float startClipDuration = 0.8f;

        [Tooltip("How long the looping exercise runs in seconds.")]
        [Range(5f, 120f)]
        public float exerciseDuration = 30f;

        [Tooltip("How often to re-fire the loop trigger (seconds). Set to 0 if clip loops without retriggering).")]
        [Range(0f, 5f)]
        public float loopRetriggerInterval = 0f;

        [Tooltip("Duration in seconds to wait for the exit clip to finish before returning to idle.")]
        [Range(0f, 3f)]
        public float exitClipDuration = 1.0f;

        [Tooltip("Rest period in seconds after this exercise. 0 = no rest (e.g. last exercise in quest).")]
        [Range(0f, 120f)]
        public float restDuration = 15f;

        [Header("Callout Dialogue")]
        [Tooltip("What ARIA says before this exercise starts. Use \\n for new lines.")]
        [TextArea(2, 4)]
        public string calloutText = "Exercise callout text here.";

        [Tooltip("Optional voice clip for the callout.")]
        public AudioClip calloutAudio;

        // ── Scoring ──────────────────────────────────────────────────────────
        [Header("Scoring")]
        public UnityEngine.Video.VideoClip referenceVideo;  // drag Burpee.mp4 here

        [Header("Scoring Config")]
        public PainQuest.Scoring.ExerciseScoringConfig scoringConfig
            = new PainQuest.Scoring.ExerciseScoringConfig();
        // Configure upThreshold/downThreshold per exercise in Inspector

        // ★ NEW — Post-Exercise / Rest Period Dialogue ──────────────────────────
        [Header("Rest Period Dialogue (plays during the rest countdown)")]
        [Tooltip("Line-by-line dialogue ARIA speaks during the rest period after " +
                 "this exercise finishes, shown together with the REST countdown. " +
                 "Leave empty/null to skip — rest period will just show the countdown.")]
        public DialogueSequence postExerciseDialogue;
    }
}