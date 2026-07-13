// ═══════════════════════════════════════════════════════════════════════════════
// PainQuestEnums.cs
// All shared enums and data structs used across the whole project.
// ═══════════════════════════════════════════════════════════════════════════════
namespace PainQuest
{
    public enum PainQuestType
    {
        JointPain,
        MuscleBack,
        CoreStomach
    }

    public enum GameState
    {
        MainMenu,           // UI visible, player frozen
        Intro,              // ARIA greeting playing
        ExerciseSession,    // Active exercise running
        Rest,               // Rest between exercises
        QuestVictory,       // One quest complete
        AllQuestsComplete   // All selected quests done
    }

    public enum GuideState
    {
        Idle,
        Talking,
        Walking,
        Exercising
    }
}
