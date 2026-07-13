using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace PainQuest
{
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("UI")]
        public TextMeshProUGUI subtitleLabel;
        public Image subtitleBackground;

        [Header("Audio")]
        public AudioSource voiceSource;

        [Header("Defaults")]
        public float defaultLineDuration = 3f;

        public System.Action OnSequenceComplete;
        public System.Action<string> OnLineStarted;

        bool _isPlaying = false;
        Coroutine _playRoutine;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        void Start()
        {
            HideSubtitle();
        }

        public bool IsPlaying => _isPlaying;

        public Coroutine PlaySequence(DialogueSequence sequence)
        {
            if (_playRoutine != null)
                StopCoroutine(_playRoutine);

            _playRoutine = StartCoroutine(PlaySequenceRoutine(sequence));
            return _playRoutine;
        }

        public Coroutine ShowLine(string text, float duration = 0f)
        {
            if (_playRoutine != null)
                StopCoroutine(_playRoutine);

            _playRoutine = StartCoroutine(
                ShowSingleLine(text,
                duration > 0f ? duration : defaultLineDuration));

            return _playRoutine;
        }

        public void ShowSubtitlePersistent(string text)
        {
            if (subtitleLabel)
            {
                subtitleLabel.text = text;
                subtitleLabel.enabled = true;
            }

            if (subtitleBackground)
                subtitleBackground.gameObject.SetActive(true);
        }

        public void HideSubtitle()
        {
            if (subtitleLabel)
                subtitleLabel.enabled = false;

            if (subtitleBackground)
                subtitleBackground.gameObject.SetActive(false);
        }

        public void StopAll()
        {
            if (_playRoutine != null)
                StopCoroutine(_playRoutine);

            _isPlaying = false;
            HideSubtitle();

            if (voiceSource)
                voiceSource.Stop();
        }

        IEnumerator PlaySequenceRoutine(DialogueSequence sequence)
        {
            if (sequence == null || sequence.lines == null)
                yield break;

            _isPlaying = true;

            foreach (var line in sequence.lines)
            {
                if (string.IsNullOrEmpty(line.text))
                    continue;

                ShowSubtitlePersistent(line.text);
                OnLineStarted?.Invoke(line.text);

                if (voiceSource && line.audioClip)
                {
                    voiceSource.Stop();
                    voiceSource.clip = line.audioClip;
                    voiceSource.Play();
                }

                float dur =
                    line.displayDuration > 0f
                    ? line.displayDuration
                    : (line.audioClip != null
                        ? line.audioClip.length + 0.3f
                        : defaultLineDuration);

                yield return new WaitForSeconds(dur);

                HideSubtitle();
                yield return new WaitForSeconds(0.2f);
            }

            _isPlaying = false;

            if (voiceSource)
                voiceSource.Stop();

            OnSequenceComplete?.Invoke();
        }

        IEnumerator ShowSingleLine(string text, float duration)
        {
            _isPlaying = true;

            ShowSubtitlePersistent(text);

            yield return new WaitForSeconds(duration);

            HideSubtitle();

            _isPlaying = false;
        }
    }
}