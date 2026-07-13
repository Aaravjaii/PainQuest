using UnityEngine;

namespace PainQuest
{
    [CreateAssetMenu(
        menuName = "PainQuest/Dialogue Sequence",
        fileName = "DialogueSeq_New")]
    public class DialogueSequence : ScriptableObject
    {
        public DialogueLine[] lines;
    }
}