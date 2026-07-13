// Assets/Scripts/Scoring/RepAnalyzer.cs
using System.Collections.Generic;
using UnityEngine;

namespace PainQuest.Scoring
{
    public class RepAnalyzer
    {
        public int RepCount { get; private set; }
        public float CurrentAngle { get; private set; }
        public float FormScore { get; private set; }
        public float RepQuality { get; private set; }
        public float ExerciseMatchConfidence { get; private set; } = 1f;

        public enum RepStatus
        {
            NameMismatch,
            AtTop,
            TrackingDown,
            TrackingUp,
            RepCounted,
            RejectedRange,     // returned to top but didn't move far enough - likely noise or a very partial rep
            RejectedTooFast    // returned to top too quickly to be a real rep (noise spike) or too soon after the last counted rep
        }

        public RepStatus Status { get; private set; } = RepStatus.AtTop;
        public float LastAttemptedRangeDeg { get; private set; }
        public float MinAcceptedRangeDeg => _minRange * _rangeLeniency;

        // upThreshold is no longer readonly -- see auto-adjustment below.
        private float _upThreshold;
        private readonly float _downThreshold;
        private readonly float _minRange;
        private readonly float _smoothing;
        private float _tolerance;
        private readonly float _rangeLeniency;

        private const float DefaultRepCooldown = 0.2f;
        private const float DefaultMinRepDuration = 0.32f;
        private readonly float _repCooldown;
        private readonly float _minRepDuration;

        // Safety net: if someone starts a down-motion and never returns to
        // the top (they stop, walk away, camera loses them mid-rep, OR --
        // as discovered -- the configured upThreshold is simply higher than
        // this video ever reaches), don't let the attempt hang forever.
        private const float MaxMotionDuration = 8f;

        // ═════════════════════════════════════════════════════════════════
        // AUTO-ADJUSTMENT (NEW)
        // Previously, when a motion got abandoned via MaxMotionDuration, it
        // was silently discarded with no logging and no learning -- so if
        // upThreshold was simply wrong for this specific video (too high to
        // ever be reached), EVERY attempt failed the exact same way for the
        // whole session. That's what "stuck at 0 reps" and "only 2 of many
        // reps counted" both were: not noise, a threshold that was never
        // reachable.
        //
        // Now: an abandoned attempt records the highest angle it actually
        // reached. If that happens even once (configurable via
        // AbandonmentsBeforeAutoAdjust), upThreshold is lowered to just
        // below that real observed peak, with a floor so it can never drop
        // so low the exercise becomes trivial (downThreshold + half of
        // minRange, minimum). This only ever activates when the configured
        // number is actually wrong for the footage -- exercises whose
        // thresholds are already correct never hit MaxMotionDuration in the
        // first place, so this is inert for them.
        // ═════════════════════════════════════════════════════════════════
        private float _observedPeakDuringAbandoned = -999f;
        private int _consecutiveAbandonments = 0;
        private const int AbandonmentsBeforeAutoAdjust = 1;
        private const float AutoAdjustMargin = 5f;

        private const float MaxPlausibleDeltaPerSample = 130f;

        private float _smoothAngle = -1f;
        private float _prevSmoothAngle = -1f;
        private float _lastRawAngle = -1f;
        private bool _nameMatches = true;

        private enum Phase { AtTop, InMotion }
        private Phase _phase = Phase.AtTop;

        private float _minAngleThisRep = 999f;
        private float _maxAngleThisRep = -999f;
        private float _lastRepTime = -999f;
        private float _motionStartTime = -999f;

        private readonly List<float> _repFormScores = new List<float>();

        public RepAnalyzer(float upThreshold = 160f, float downThreshold = 120f,
                            float minRange = 25f, float smoothing = 0.08f,
                            float rangeLeniency = 0.6f,
                            float minRepDurationOverride = 0f,
                            float repCooldownOverride = 0f)
        {
            _upThreshold = upThreshold;
            _downThreshold = downThreshold;
            _minRange = minRange;
            _smoothing = Mathf.Clamp01(smoothing);
            _rangeLeniency = Mathf.Clamp01(rangeLeniency);
            _minRepDuration = minRepDurationOverride > 0f ? minRepDurationOverride : DefaultMinRepDuration;
            _repCooldown = repCooldownOverride > 0f ? repCooldownOverride : DefaultRepCooldown;

            RecomputeTolerance();
        }

        private void RecomputeTolerance()
        {
            float range = Mathf.Abs(_upThreshold - _downThreshold);
            _tolerance = Mathf.Clamp(range * 0.12f, 3f, 20f);
        }

        public void SetNameMatch(bool matches)
        {
            _nameMatches = matches;
            ExerciseMatchConfidence = matches ? 1f : 0f;
            if (!matches) Status = RepStatus.NameMismatch;
        }

        public bool Feed(float rawAngle)
        {
            if (!_nameMatches)
            {
                Status = RepStatus.NameMismatch;
                return false;
            }
            if (rawAngle < 0f || rawAngle > 180f) return false;

            if (_lastRawAngle >= 0f)
            {
                float rawDelta = Mathf.Abs(rawAngle - _lastRawAngle);
                if (rawDelta > MaxPlausibleDeltaPerSample)
                    return false; // true garbage spike, not a real sample
            }
            _lastRawAngle = rawAngle;

            _prevSmoothAngle = _smoothAngle;
            if (_smoothAngle < 0f)
                _smoothAngle = rawAngle;
            else
                _smoothAngle = Mathf.Lerp(_smoothAngle, rawAngle, 1f - _smoothing);

            CurrentAngle = _smoothAngle;

            bool repCompleted = false;

            switch (_phase)
            {
                case Phase.AtTop:
                    Status = RepStatus.AtTop;
                    if (_smoothAngle < _upThreshold - _tolerance)
                    {
                        _phase = Phase.InMotion;
                        _motionStartTime = Time.unscaledTime;
                        _minAngleThisRep = _smoothAngle;
                        _maxAngleThisRep = _smoothAngle;
                    }
                    break;

                case Phase.InMotion:
                    _minAngleThisRep = Mathf.Min(_minAngleThisRep, _smoothAngle);
                    _maxAngleThisRep = Mathf.Max(_maxAngleThisRep, _smoothAngle);

                    float elapsed = Time.unscaledTime - _motionStartTime;

                    if (elapsed > MaxMotionDuration)
                    {
                        Debug.LogWarning($"[RepAnalyzer] Motion abandoned after {MaxMotionDuration:F0}s without returning near upThreshold ({_upThreshold:F0} +/- {_tolerance:F0}). Highest angle reached this attempt: {_maxAngleThisRep:F1}.");

                        _observedPeakDuringAbandoned = Mathf.Max(_observedPeakDuringAbandoned, _maxAngleThisRep);
                        _consecutiveAbandonments++;

                        if (_consecutiveAbandonments >= AbandonmentsBeforeAutoAdjust
                            && _observedPeakDuringAbandoned < _upThreshold - AutoAdjustMargin)
                        {
                            float floor = _downThreshold + _minRange * 0.5f;
                            float newUp = Mathf.Max(_observedPeakDuringAbandoned - AutoAdjustMargin, floor);

                            Debug.LogWarning($"[RepAnalyzer] Auto-adjusting upThreshold: {_upThreshold:F0} -> {newUp:F0} (this video never reaches the configured top after {_consecutiveAbandonments} attempt(s) -- using the highest angle actually observed instead).");

                            _upThreshold = newUp;
                            RecomputeTolerance();
                            _consecutiveAbandonments = 0;
                            _observedPeakDuringAbandoned = -999f;
                        }

                        _phase = Phase.AtTop;
                        Status = RepStatus.AtTop;
                        _minAngleThisRep = 999f;
                        _maxAngleThisRep = -999f;
                        break;
                    }

                    // Cosmetic direction for UI purposes only - doesn't affect counting.
                    Status = (_smoothAngle >= _prevSmoothAngle) ? RepStatus.TrackingUp : RepStatus.TrackingDown;

                    if (_smoothAngle > _upThreshold - _tolerance)
                    {
                        // Back near the top - evaluate whether this was a real rep.
                        float range = _maxAngleThisRep - _minAngleThisRep;
                        LastAttemptedRangeDeg = range;

                        bool cooldownOk = Time.unscaledTime - _lastRepTime >= _repCooldown;
                        bool durationOk = elapsed >= _minRepDuration;
                        bool rangeOk = range >= _minRange * _rangeLeniency;

                        if (rangeOk && cooldownOk && durationOk)
                        {
                            RepCount++;
                            repCompleted = true;
                            _lastRepTime = Time.unscaledTime;

                            float score = CalculateRepScore(range);
                            _repFormScores.Add(score);
                            FormScore = AverageFormScore();
                            RepQuality = score;
                            Status = RepStatus.RepCounted;

                            // A genuinely completed rep is proof the current
                            // thresholds work -- clear any pending abandonment
                            // tally so a lucky earlier fluke doesn't linger.
                            _consecutiveAbandonments = 0;
                            _observedPeakDuringAbandoned = -999f;
                        }
                        else if (!cooldownOk || !durationOk)
                        {
                            Status = RepStatus.RejectedTooFast;
                        }
                        else
                        {
                            Status = RepStatus.RejectedRange;
                        }

                        _minAngleThisRep = 999f;
                        _maxAngleThisRep = -999f;
                        _phase = Phase.AtTop;
                    }
                    break;
            }

            return repCompleted;
        }

        private float CalculateRepScore(float range)
        {
            float rangeScore = Mathf.Clamp01(range / (_minRange * 1.2f));
            float extensionScore = Mathf.Clamp01(_maxAngleThisRep / Mathf.Max(_upThreshold, 1f));
            float rawScore = (rangeScore * 0.7f + extensionScore * 0.3f);
            return Mathf.Clamp01(rawScore) * 100f;
        }

        private float AverageFormScore()
        {
            if (_repFormScores.Count == 0) return 0f;
            float sum = 0f;
            foreach (var s in _repFormScores) sum += s;
            return sum / _repFormScores.Count;
        }

        public void Reset()
        {
            RepCount = 0;
            FormScore = 0f;
            RepQuality = 0f;
            _smoothAngle = -1f;
            _prevSmoothAngle = -1f;
            _lastRawAngle = -1f;
            _phase = Phase.AtTop;
            _minAngleThisRep = 999f;
            _maxAngleThisRep = -999f;
            _lastRepTime = -999f;
            _motionStartTime = -999f;
            _consecutiveAbandonments = 0;
            _observedPeakDuringAbandoned = -999f;
            LastAttemptedRangeDeg = 0f;
            Status = RepStatus.AtTop;
            _repFormScores.Clear();
            ExerciseMatchConfidence = _nameMatches ? 1f : 0f;
        }
    }
}