using System;

namespace ZBase.UnityScreenNavigator.Foundation.Animation
{
    internal struct AnimationPlayer : IUpdatable
    {
        public IAnimation Animation { get; }

        public bool IsPlaying { get; private set; }

        public float Time { get; private set; }

        public readonly bool IsFinished => Time >= Animation.Duration;

        public AnimationPlayer(IAnimation animation)
        {
            Animation = animation;
            IsPlaying = false;

            var time = Math.Max(0, Math.Min(animation.Duration, 0f));
            animation.SetTime(Time = time);
        }

        public void Update(float deltaTime)
        {
            if (!IsPlaying)
            {
                return;
            }

            SetTime(Time + deltaTime);
        }

        public void Play()
        {
            IsPlaying = true;
        }

        public void Stop()
        {
            IsPlaying = false;
        }

        public void Reset()
        {
            SetTime(0.0f);
        }

        public void SetTime(float time)
        {
            time = Math.Max(0, Math.Min(Animation.Duration, time));
            Animation.SetTime(time);

            if (IsPlaying && time >= Animation.Duration)
            {
                Stop();
            }

            Time = time;
        }
    }
}