using System.Collections;
using UnityEngine;

namespace PainQuest
{
    [RequireComponent(typeof(Animator))]
    public class GuideAnimatorController : MonoBehaviour
    {
        [Header("Animator Controller Assets")]
        public RuntimeAnimatorController defaultAC;
        public RuntimeAnimatorController jointPainAC;
        public RuntimeAnimatorController muscleBackAC;
        public RuntimeAnimatorController coreStomachAC;

        static readonly int H_IsTalking = Animator.StringToHash("IsTalking");
        static readonly int H_IsWalking = Animator.StringToHash("IsWalking");
        static readonly int H_DoQuickInfo = Animator.StringToHash("DoQuickInfo");
        static readonly int H_DoIdle = Animator.StringToHash("DoIdle");

        Animator _anim;
        Coroutine _exerciseRoutine;

        void Awake()
        {
            _anim = GetComponent<Animator>();
        }

        void Start()
        {
            StartCoroutine(SwapToDefault());
        }

        public void SetTalking(bool talking)
        {
            EnsureAC(defaultAC);
            _anim.SetBool(H_IsTalking, talking);
            _anim.SetBool(H_IsWalking, false);
        }

        public void SetWalking(bool walking)
        {
            EnsureAC(defaultAC);
            _anim.SetBool(H_IsWalking, walking);
            _anim.SetBool(H_IsTalking, false);
        }

        public void SetIdleDefault()
        {
            EnsureAC(defaultAC);
            _anim.SetBool(H_IsTalking, false);
            _anim.SetBool(H_IsWalking, false);
            _anim.SetTrigger(H_DoIdle);
        }

        public void PlayQuickInfo()
        {
            EnsureAC(defaultAC);
            _anim.SetTrigger(H_DoQuickInfo);
        }

        public Coroutine PlayExercise(ExerciseData data, System.Action exitCallback = null)
        {
            if (_exerciseRoutine != null)
                StopCoroutine(_exerciseRoutine);

            _exerciseRoutine = StartCoroutine(ExerciseRoutine(data, exitCallback));
            return _exerciseRoutine;
        }

        public void StopExercise(ExerciseData data)
        {
            if (_exerciseRoutine != null)
            {
                StopCoroutine(_exerciseRoutine);
                _exerciseRoutine = null;
            }

            if (!string.IsNullOrEmpty(data?.exitTrigger))
            {
                Debug.Log($"EXIT TRIGGER -> {data.exitTrigger}");
                _anim.SetTrigger(Animator.StringToHash(data.exitTrigger));
            }
            else
            {
                _anim.SetTrigger(H_DoIdle);
            }
        }

        public Coroutine PlayExitAndWait(ExerciseData data)
        {
            if (_exerciseRoutine != null)
                StopCoroutine(_exerciseRoutine);

            _exerciseRoutine = StartCoroutine(ExitRoutine(data));
            return _exerciseRoutine;
        }

        public void SetRestIdle()
        {
            _anim.SetTrigger(H_DoIdle);
        }

        public IEnumerator SwapToDefault()
        {
            yield return StartCoroutine(SwapACRoutine(defaultAC));
        }

        public IEnumerator SwapToQuest(PainQuestType quest)
        {
            RuntimeAnimatorController ac = quest switch
            {
                PainQuestType.JointPain => jointPainAC,
                PainQuestType.MuscleBack => muscleBackAC,
                PainQuestType.CoreStomach => coreStomachAC,
                _ => defaultAC
            };

            yield return StartCoroutine(SwapACRoutine(ac));
        }

        IEnumerator ExerciseRoutine(ExerciseData data, System.Action exitCallback)
        {
            Debug.Log($"========== EXERCISE START ==========");
            Debug.Log($"Exercise: {data.exerciseName}");
            Debug.Log($"Start Trigger: {data.startTrigger}");
            Debug.Log($"Loop Trigger: {data.loopTrigger}");
            Debug.Log($"Loop Retrigger: {data.loopRetrigger}");
            Debug.Log($"Exit Trigger: {data.exitTrigger}");

            // START
            if (!string.IsNullOrEmpty(data.startTrigger))
            {
                Debug.Log($"FIRING START -> {data.startTrigger}");

                _anim.ResetTrigger(H_DoIdle);
                _anim.SetTrigger(Animator.StringToHash(data.startTrigger));

                if (data.startClipDuration > 0f)
                    yield return new WaitForSeconds(data.startClipDuration);
            }

            // LOOP
            if (!string.IsNullOrEmpty(data.loopTrigger))
            {
                Debug.Log($"FIRING LOOP -> {data.loopTrigger}");

                _anim.ResetTrigger(H_DoIdle);
                _anim.SetTrigger(Animator.StringToHash(data.loopTrigger));

                if (data.loopRetriggerInterval > 0f &&
                    !string.IsNullOrEmpty(data.loopRetrigger))
                {
                    float timer = 0f;

                    while (true)
                    {
                        timer += Time.deltaTime;

                        if (timer >= data.loopRetriggerInterval)
                        {
                            timer = 0f;

                            Debug.Log($"RETRIGGER LOOP -> {data.loopRetrigger}");

                            _anim.ResetTrigger(H_DoIdle);
                            _anim.SetTrigger(
                                Animator.StringToHash(data.loopRetrigger)
                            );
                        }

                        yield return null;
                    }
                }
            }

            yield return new WaitForSeconds(99999f);
        }

        IEnumerator ExitRoutine(ExerciseData data)
        {
            if (!string.IsNullOrEmpty(data.exitTrigger))
            {
                Debug.Log($"FIRING EXIT -> {data.exitTrigger}");

                _anim.SetTrigger(Animator.StringToHash(data.exitTrigger));

                if (data.exitClipDuration > 0f)
                    yield return new WaitForSeconds(data.exitClipDuration);
            }
            else
            {
                _anim.SetTrigger(H_DoIdle);
                yield return new WaitForEndOfFrame();
            }
        }

        IEnumerator SwapACRoutine(RuntimeAnimatorController ac)
        {
            if (ac != null && _anim.runtimeAnimatorController != ac)
            {
                Debug.Log($"SWAPPING AC -> {ac.name}");
                _anim.runtimeAnimatorController = ac;
            }

            yield return new WaitForEndOfFrame();
        }

        void EnsureAC(RuntimeAnimatorController ac)
        {
            if (ac != null && _anim.runtimeAnimatorController != ac)
                StartCoroutine(SwapACRoutine(ac));
        }
    }
}