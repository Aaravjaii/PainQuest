using UnityEngine;

namespace PainQuest
{
    [System.Serializable]
    public class DialogueLine
    {
        [TextArea(2, 4)]
        public string text;

        public AudioClip audioClip;

        [Range(0f, 15f)]
        public float displayDuration = 0f;
    }
}