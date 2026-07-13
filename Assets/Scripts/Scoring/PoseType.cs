// Assets/Scripts/Scoring/PoseTypes.cs
using System;

namespace PainQuest.Scoring
{
    [Serializable]
    public struct PoseLandmark
    {
        public float x, y, z, v;
    }

    public enum PoseJoint
    {
        Nose = 0,
        LeftShoulder = 11,
        RightShoulder = 12,
        LeftElbow = 13,
        RightElbow = 14,
        LeftWrist = 15,
        RightWrist = 16,
        LeftHip = 23,
        RightHip = 24,
        LeftKnee = 25,
        RightKnee = 26,
        LeftAnkle = 27,
        RightAnkle = 28
    }

    public class PoseResult
    {
        public PoseLandmark[] landmarks;
        public bool valid;
    }
}