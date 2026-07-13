using UnityEngine;

namespace PainQuest
{
    // ═══════════════════════════════════════════════════════════════════════════
    // QuestData  —  ScriptableObject
    //
    // Create one asset per quest via:
    //   Right-click Project → Create → PainQuest → Quest Data
    //
    // Drag ExerciseData assets into the exercises list in order.
    // ═══════════════════════════════════════════════════════════════════════════
    [CreateAssetMenu(menuName = "PainQuest/Quest Data", fileName = "QuestData_New")]
    public class QuestData : ScriptableObject
    {
        [Header("Quest Identity")]
        public string questName = "Quest Name";
        public PainQuestType questType;

        [Header("Quest Card UI")]
        [Tooltip("Image shown on the quest selection card.")]
        public Sprite questCardImage;

        [TextArea(2, 3)]
        public string questDescription = "Quest description here.";

        [Header("Animator Controller")]
        [Tooltip("The RuntimeAnimatorController asset for this quest's exercises.")]
        public RuntimeAnimatorController animatorController;

        [Header("Exercises (in order)")]
        [Tooltip("Drag ExerciseData assets here in the order they should play.")]
        public ExerciseData[] exercises;

        [Header("Victory Dialogue")]
        [TextArea(2, 4)]
        public string victoryText = "Quest complete! Great work!";
        public AudioClip victoryAudio;
    }
}
