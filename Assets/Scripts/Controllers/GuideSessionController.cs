using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PainQuest
{
    public class GuideSessionController : MonoBehaviour
    {
        [Header("References")]
        public Transform playerTransform;
        public GuideAnimatorController guideAnim;
        public DialogueManager dialogueManager;

        [Header("Proximity")]
        public float triggerDistance = 5f;
        public float eyeContactSpeed = 120f;
        public float eyeContactThreshold = 5f;

        [Header("Greeting")]
        public DialogueSequence greetingSequence;

        [Header("Available Quests")]
        public QuestData[] availableQuests;

        [Header("Between Quests")]
        public DialogueSequence betweenQuestsSequence;

        [Header("Scoring")]
        public PainQuest.Scoring.ExerciseScoringManager scoringManager;

        // ── Session state ────────────────────────────────────────────────────
        List<QuestData> _selectedQuests = new List<QuestData>();
        bool _sessionActive = false;
        bool _playerInRange = false;
        int _currentQuestIdx = 0;

        // ★ NEW — gate that blocks proximity auto-trigger until quest selection
        bool _selectionConfirmed = false;

        // ── Animation cache ──────────────────────────────────────────────────
        Animator _animator;
        bool _animatorReady = false;

        // ─────────────────────────────────────────────────────────────────────
        void Start()
        {
            if (playerTransform == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p) playerTransform = p.transform;
            }

            if (guideAnim == null) guideAnim = GetComponent<GuideAnimatorController>();
            if (dialogueManager == null) dialogueManager = FindFirstObjectByType<DialogueManager>();

            // Cache animator reference
            if (guideAnim != null)
            {
                _animator = guideAnim.GetComponent<Animator>();
                _animatorReady = _animator != null;
            }
            else
            {
                _animator = GetComponent<Animator>();
                _animatorReady = _animator != null;
            }

            if (guideAnim != null)
                guideAnim.SetIdleDefault();
            else if (_animatorReady)
                _animator.SetTrigger("DoIdle");
        }

        void Update()
        {
            if (!_selectionConfirmed || _sessionActive || playerTransform == null) return;

            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= triggerDistance && !_playerInRange)
            {
                _playerInRange = true;
                StartCoroutine(SessionRoutine());
            }
            else if (dist > triggerDistance + 1f)
            {
                _playerInRange = false;
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // PUBLIC API
        // ═════════════════════════════════════════════════════════════════════
        public void SetSelectedQuests(List<QuestData> quests)
        {
            _selectedQuests = new List<QuestData>(quests);
            _selectionConfirmed = true;
        }

        public void StartSessionDirectly()
        {
            if (_sessionActive) return;
            if (!_selectionConfirmed)
            {
                Debug.LogWarning("[GuideSessionController] StartSessionDirectly called " +
                                  "before SetSelectedQuests. Call SetSelectedQuests first.");
                return;
            }

            _sessionActive = true;
            _playerInRange = true;
            StartCoroutine(SessionRoutine());
        }

        // ═════════════════════════════════════════════════════════════════════
        // MAIN SESSION ROUTINE
        // ═════════════════════════════════════════════════════════════════════
        IEnumerator SessionRoutine()
        {
            _sessionActive = true;

            yield return StartCoroutine(MakeEyeContact());
            yield return StartCoroutine(SwapAndTalk(greetingSequence));

            for (int qi = 0; qi < _selectedQuests.Count; qi++)
            {
                _currentQuestIdx = qi;
                QuestData quest = _selectedQuests[qi];

                string introText = $"▶  {quest.questName.ToUpper()}\nGet ready!";
                yield return StartCoroutine(SwapAndSayLine(introText, 2.5f));

                if (guideAnim != null)
                    guideAnim.SetIdleDefault();
                else
                    SetAnimatorTrigger("DoIdle");

                if (guideAnim != null)
                    yield return StartCoroutine(guideAnim.SwapToQuest(quest.questType));
                yield return new WaitForSeconds(0.3f);

                for (int ei = 0; ei < quest.exercises.Length; ei++)
                {
                    ExerciseData exercise = quest.exercises[ei];
                    bool isLast = (ei == quest.exercises.Length - 1);
                    yield return StartCoroutine(RunExercise(exercise, isLast));
                }

                if (guideAnim != null)
                    yield return StartCoroutine(guideAnim.SwapToDefault());

                SetAnimatorTalking(true);
                yield return dialogueManager.ShowLine(quest.victoryText, 3.5f);
                if (quest.victoryAudio)
                    PlayVoice(quest.victoryAudio);
                yield return new WaitForSeconds(3.5f);
                SetAnimatorTalking(false);

                if (guideAnim != null)
                    guideAnim.SetIdleDefault();
                else
                    SetAnimatorTrigger("DoIdle");

                if (qi < _selectedQuests.Count - 1 && betweenQuestsSequence != null)
                {
                    yield return StartCoroutine(SwapAndTalk(betweenQuestsSequence));
                    yield return new WaitForSeconds(1f);
                }
            }

            if (guideAnim != null)
                yield return StartCoroutine(guideAnim.SwapToDefault());

            SetAnimatorTalking(true);
            yield return dialogueManager.ShowLine(
                "ALL PAIN MONSTERS DEFEATED!\nYou are a true PainQuest Champion, Hero!\nUntil next time!", 5f);
            yield return new WaitForSeconds(5f);
            SetAnimatorTalking(false);

            if (guideAnim != null)
                guideAnim.SetIdleDefault();
            else
                SetAnimatorTrigger("DoIdle");

            yield return new WaitForSeconds(10f);
            _sessionActive = false;
            _playerInRange = false;
        }

        // ═════════════════════════════════════════════════════════════════════
        // RUN ONE EXERCISE
        // ═════════════════════════════════════════════════════════════════════
        IEnumerator RunExercise(ExerciseData data, bool isLastInQuest)
        {
            if (scoringManager != null)
                scoringManager.PrepareExercise(data);

            // ── Callout (before exercise) ───────────────────────────────────
            dialogueManager.ShowSubtitlePersistent(data.calloutText);
            if (data.calloutAudio) PlayVoice(data.calloutAudio);
            yield return new WaitForSeconds(data.calloutAudio != null
                ? data.calloutAudio.length + 0.3f : 2.5f);
            dialogueManager.HideSubtitle();
            yield return new WaitForSeconds(0.2f);

            if (scoringManager != null)
                scoringManager.StartVideoAndScoring();

            // ── Start clip ────────────────────────────────────────────────────
            if (!string.IsNullOrEmpty(data.startTrigger))
            {
                SetAnimatorTrigger(data.startTrigger);
                if (data.startClipDuration > 0f)
                    yield return new WaitForSeconds(data.startClipDuration);
            }

            // ── Loop clip + countdown ───────────────────────────────────────
            if (!string.IsNullOrEmpty(data.loopTrigger))
                SetAnimatorTrigger(data.loopTrigger);

            float elapsed = 0f;
            float retriggerT = 0f;
            while (elapsed < data.exerciseDuration)
            {
                elapsed += Time.deltaTime;
                retriggerT += Time.deltaTime;

                if (data.loopRetriggerInterval > 0f
                    && !string.IsNullOrEmpty(data.loopRetrigger)
                    && retriggerT >= data.loopRetriggerInterval)
                {
                    retriggerT = 0f;
                    SetAnimatorTrigger(data.loopRetrigger);
                }

                int remaining = Mathf.CeilToInt(data.exerciseDuration - elapsed);
                dialogueManager.ShowSubtitlePersistent($"{data.exerciseName}\n{remaining}s");
                yield return null;
            }

            dialogueManager.HideSubtitle();

            // ── Exit clip ─────────────────────────────────────────────────────
            if (!string.IsNullOrEmpty(data.exitTrigger))
            {
                SetAnimatorTrigger(data.exitTrigger);
                if (data.exitClipDuration > 0f)
                    yield return new WaitForSeconds(data.exitClipDuration);
            }
            else
            {
                SetAnimatorTrigger("DoIdle");
                yield return new WaitForEndOfFrame();
            }

            // ── Stop scoring ─────────────────────────────────────────────────
            float score = 0f;
            if (scoringManager != null)
            {
                score = scoringManager.StopScoring();
                Debug.Log($"[GuideSession] {data.exerciseName} final score: {score:F1}%");
                yield return new WaitForSeconds(3f);
                scoringManager.HideScoringUI();
            }

            // ── Rest ──────────────────────────────────────────────────────────
            // ── Post Exercise Dialogue / Rest ────────────────────────────────
            if (data.postExerciseDialogue != null ||
                (!isLastInQuest && data.restDuration > 0f))
            {
                yield return StartCoroutine(RestWithDialogue(data));
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // REST PERIOD WITH OPTIONAL DIALOGUE
        // ═════════════════════════════════════════════════════════════════════
        IEnumerator RestWithDialogue(ExerciseData data)
        {
            SetAnimatorTrigger("DoIdle");

            DialogueSequence seq = data.postExerciseDialogue;
            bool hasDialogue = seq != null &&
                               seq.lines != null &&
                               seq.lines.Length > 0;

            // ── Last Exercise Case ─────────────────────────────────────────────
            // If there is no rest period, still play the dialogue.
            if (data.restDuration <= 0f)
            {
                if (hasDialogue)
                {
                    SetAnimatorTalking(true);

                    yield return dialogueManager.PlaySequence(seq);

                    SetAnimatorTalking(false);
                }

                yield break;
            }

            // ── Normal Rest Period ─────────────────────────────────────────────
            int lineIndex = 0;
            float lineTimeLeft = 0f;
            string currentLine = "";
            bool talkingStarted = false;

            if (hasDialogue)
            {
                SetAnimatorTalking(true);
                talkingStarted = true;
                StartNextLine(seq, ref lineIndex, ref lineTimeLeft, ref currentLine);
            }

            float restElapsed = 0f;

            while (restElapsed < data.restDuration)
            {
                float dt = Time.deltaTime;
                restElapsed += dt;

                int restRemaining = Mathf.CeilToInt(data.restDuration - restElapsed);

                if (hasDialogue && lineIndex <= seq.lines.Length)
                {
                    lineTimeLeft -= dt;

                    if (lineTimeLeft <= 0f && lineIndex < seq.lines.Length)
                    {
                        StartNextLine(seq, ref lineIndex, ref lineTimeLeft, ref currentLine);
                    }
                    else if (lineTimeLeft <= 0f && lineIndex >= seq.lines.Length)
                    {
                        if (talkingStarted)
                        {
                            SetAnimatorTalking(false);
                            talkingStarted = false;
                        }

                        currentLine = "";
                    }
                }

                string combined = string.IsNullOrEmpty(currentLine)
                    ? $"REST  —  {restRemaining}s"
                    : $"{currentLine}\n\nREST  —  {restRemaining}s";

                dialogueManager.ShowSubtitlePersistent(combined);

                yield return null;
            }

            if (talkingStarted)
                SetAnimatorTalking(false);

            dialogueManager.HideSubtitle();
        }

        // ═════════════════════════════════════════════════════════════════════
        // ANIMATION HELPERS - FIXED
        // ═════════════════════════════════════════════════════════════════════

        void SetAnimatorTrigger(string triggerName)
        {
            if (!_animatorReady || string.IsNullOrEmpty(triggerName))
                return;

            try
            {
                int hash = Animator.StringToHash(triggerName);

                // Check if the parameter exists before setting it
                foreach (AnimatorControllerParameter param in _animator.parameters)
                {
                    if (param.nameHash == hash && param.type == AnimatorControllerParameterType.Trigger)
                    {
                        _animator.SetTrigger(hash);
                        return;
                    }
                }

                // If trigger doesn't exist, log warning once
                Debug.LogWarning($"[GuideSessionController] Trigger '{triggerName}' not found in Animator. Available triggers: " +
                                 string.Join(", ", GetAvailableTriggers()));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GuideSessionController] Error setting trigger '{triggerName}': {e.Message}");
            }
        }

        void SetAnimatorTalking(bool talking)
        {
            if (!_animatorReady) return;

            try
            {
                int hash = Animator.StringToHash("IsTalking");

                // Check if parameter exists
                foreach (AnimatorControllerParameter param in _animator.parameters)
                {
                    if (param.nameHash == hash && param.type == AnimatorControllerParameterType.Bool)
                    {
                        _animator.SetBool(hash, talking);
                        return;
                    }
                }

                // Fallback to SetTrigger if no bool parameter
                if (talking)
                    SetAnimatorTrigger("Talk");
                else
                    SetAnimatorTrigger("StopTalk");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GuideSessionController] Error setting talking state: {e.Message}");
            }
        }

        string[] GetAvailableTriggers()
        {
            if (!_animatorReady) return new string[0];

            List<string> triggers = new List<string>();
            foreach (AnimatorControllerParameter param in _animator.parameters)
            {
                if (param.type == AnimatorControllerParameterType.Trigger)
                    triggers.Add(param.name);
            }
            return triggers.ToArray();
        }

        void StartNextLine(DialogueSequence seq, ref int lineIndex,
                           ref float lineTimeLeft, ref string currentLine)
        {
            var line = seq.lines[lineIndex];
            currentLine = line.text;

            if (dialogueManager.voiceSource && line.audioClip)
            {
                dialogueManager.voiceSource.Stop();
                dialogueManager.voiceSource.clip = line.audioClip;
                dialogueManager.voiceSource.Play();
            }

            lineTimeLeft = line.displayDuration > 0f
                ? line.displayDuration
                : (line.audioClip != null ? line.audioClip.length + 0.3f
                                           : dialogueManager.defaultLineDuration);

            lineIndex++;
        }

        // ═════════════════════════════════════════════════════════════════════
        // HELPERS
        // ═════════════════════════════════════════════════════════════════════

        IEnumerator MakeEyeContact()
        {
            if (guideAnim != null)
                yield return StartCoroutine(guideAnim.SwapToDefault());

            while (true)
            {
                Vector3 dir = playerTransform.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.001f) break;

                Quaternion target = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, target, eyeContactSpeed * Time.deltaTime);

                float angle = Quaternion.Angle(transform.rotation, target);
                if (angle <= eyeContactThreshold) break;

                yield return null;
            }

            yield return new WaitForSeconds(0.4f);
        }

        IEnumerator SwapAndTalk(DialogueSequence sequence)
        {
            if (sequence == null) yield break;
            if (guideAnim != null)
                yield return StartCoroutine(guideAnim.SwapToDefault());
            SetAnimatorTalking(true);
            yield return dialogueManager.PlaySequence(sequence);
            SetAnimatorTalking(false);
            yield return new WaitForSeconds(0.3f);
        }

        IEnumerator SwapAndSayLine(string text, float duration)
        {
            if (guideAnim != null)
                yield return StartCoroutine(guideAnim.SwapToDefault());
            SetAnimatorTalking(true);
            yield return dialogueManager.ShowLine(text, duration);
            SetAnimatorTalking(false);
            yield return new WaitForSeconds(0.2f);
        }

        void PlayVoice(AudioClip clip)
        {
            if (dialogueManager?.voiceSource && clip)
            {
                dialogueManager.voiceSource.Stop();
                dialogueManager.voiceSource.clip = clip;
                dialogueManager.voiceSource.Play();
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 0.4f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, triggerDistance);
        }
    }
}