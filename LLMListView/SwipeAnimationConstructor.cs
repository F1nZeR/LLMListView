﻿#region License
//   Copyright 2015 Brook Shi
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License. 
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace LLM
{
    public class SwipeAnimationConstructor
    {
        private SwipeAnimatorConfig _config = new SwipeAnimatorConfig();

        public SwipeAnimatorConfig Config
        {
            get { return _config; }
            set { _config = value; }
        }

        public static SwipeAnimationConstructor Create(SwipeAnimatorConfig config)
        {
            SwipeAnimationConstructor constructor = new SwipeAnimationConstructor();
            constructor.Config = config;
            return constructor;
        }

        public void DisplaySwipeAnimation(Action triggerCallback, Action restoreCallback)
        {
            var swipeAnimator = GetSwipeAnimator(_config.SwipeMode);

            if (swipeAnimator == null)
                return;

            if(swipeAnimator.ShouldTriggerAction(Config))
            {
                swipeAnimator.ActionTrigger(Config, triggerCallback);
            }
            else
            {
                swipeAnimator.Restore(Config, restoreCallback);
            }
        }

        public ISwipeAnimator GetSwipeAnimator(SwipeMode mode)
        {
            switch (mode)
            {
                case SwipeMode.Collapse:
                    return CollapseSwipeAnimator.Instance;
                case SwipeMode.Fix:
                    return FixedSwipeAnimator.Instance;
                case SwipeMode.Expand:
                    return ExpandSwipeAnimator.Instance;
                case SwipeMode.None:
                    return null;
                default:
                    throw new NotSupportedException("not supported swipe mode");
            }
        }
    }

    public interface ISwipeAnimator
    {
        void Restore(SwipeAnimatorConfig config, Action restoreCallback);
        void ActionTrigger(SwipeAnimatorConfig config, Action triggerCallback);
        bool ShouldTriggerAction(SwipeAnimatorConfig config);
    }

    public abstract class BaseSwipeAnimator : ISwipeAnimator
    {
        public abstract void ActionTrigger(SwipeAnimatorConfig config, Action triggerCallback);

        public virtual bool ShouldTriggerAction(SwipeAnimatorConfig config)
        {
            return config.ActionRateForSwipeLength <= config.CurrentSwipeRate;
        }

        public void Restore(SwipeAnimatorConfig config, Action restoreCallback)
        {
            Storyboard animStory = new Storyboard();
            animStory.Children.Add(Utils.CreateDoubleAnimation(config.MainTransform, "X", config.EasingFunc, 0, config.Duration));
            animStory.Children.Add(Utils.CreateDoubleAnimation(config.SwipeClipTransform, "ScaleX", config.EasingFunc, 0, config.Duration));

            animStory.Completed += (sender, e) =>
            {
                config.SwipeClipRectangle.Rect = new Rect(0, 0, 0, 0);
                config.SwipeClipTransform.ScaleX = 1;

                if (restoreCallback != null)
                    restoreCallback();
            };

            animStory.Begin();
        }
    }

    public class CollapseSwipeAnimator : BaseSwipeAnimator
    {
        public readonly static ISwipeAnimator Instance = new CollapseSwipeAnimator();

        public override void ActionTrigger(SwipeAnimatorConfig config, Action triggerCallback)
        {
            Restore(config, triggerCallback);
        }
    }

    public class FixedSwipeAnimator : BaseSwipeAnimator
    {
        public readonly static ISwipeAnimator Instance = new FixedSwipeAnimator();

        public override void ActionTrigger(SwipeAnimatorConfig config, Action triggerCallback)
        {
            var targetWidth = config.TriggerActionTargetWidth;
            var clipScaleX = targetWidth / config.CurrentSwipeWidth;

            Storyboard animStory = new Storyboard();
            animStory.Children.Add(Utils.CreateDoubleAnimation(config.MainTransform, "X", config.EasingFunc, targetWidth, config.Duration));
            animStory.Children.Add(Utils.CreateDoubleAnimation(config.SwipeClipTransform, "ScaleX", config.EasingFunc, clipScaleX, config.Duration));

            animStory.Completed += (sender, e) =>
            {
                config.SwipeClipTransform.ScaleX = 1;
                config.SwipeClipRectangle.Rect = new Rect(0, 0, targetWidth, config.SwipeClipRectangle.Rect.Height);

                if (triggerCallback != null)
                    triggerCallback();
            };

            animStory.Begin();
        }
    }

    public class ExpandSwipeAnimator : BaseSwipeAnimator
    {
        public readonly static ISwipeAnimator Instance = new ExpandSwipeAnimator();

        public override void ActionTrigger(SwipeAnimatorConfig config, Action triggerCallback)
        {
            var targetX = config.Direction == SwipeDirection.Left ? -config.ItemActualWidth : config.ItemActualWidth;
            var clipScaleX = config.ItemActualWidth / config.CurrentSwipeWidth;

            Storyboard animStory = new Storyboard();
            animStory.Children.Add(Utils.CreateDoubleAnimation(config.MainTransform, "X", config.EasingFunc, targetX, config.Duration));
            animStory.Children.Add(Utils.CreateDoubleAnimation(config.SwipeClipTransform, "ScaleX", config.EasingFunc, clipScaleX, config.Duration));

            animStory.Completed += (sender, e) =>
            {
                config.SwipeClipRectangle.Rect = new Rect(0, 0, 0, 0);
                config.SwipeClipTransform.ScaleX = 1;

                if (triggerCallback != null)
                    triggerCallback();
            };

            animStory.Begin();
        }
    }

    public class SwipeAnimatorConfig
    {
        public EasingFunctionBase LeftEasingFunc { get; set; }

        public EasingFunctionBase RightEasingFunc { get; set; }

        public TranslateTransform MainTransform { get; set; }

        public ScaleTransform SwipeClipTransform { get; set; }

        public RectangleGeometry SwipeClipRectangle { get; set; }

        public int Duration { get; set; }

        public SwipeMode LeftSwipeMode { get; set; }

        public SwipeMode RightSwipeMode { get; set; }

        public SwipeDirection Direction { get; set; }

        public double LeftActionRateForSwipeLength { get; set; }

        public double RightActionRateForSwipeLength { get; set; }

        public double LeftSwipeLengthRate { get; set; }

        public double RightSwipeLengthRate { get; set; }

        public double ItemActualWidth { get; set; }

        public double CurrentSwipeWidth { get; set; }


        public double LeftRateForActualWidth { get { return LeftSwipeLengthRate * LeftActionRateForSwipeLength; } }

        public double RightRateForActualWidth { get { return RightSwipeLengthRate * RightActionRateForSwipeLength; } }

        public bool CanSwipeLeft { get { return Direction == SwipeDirection.Left && LeftSwipeMode != SwipeMode.None; } }

        public bool CanSwipeRight { get { return Direction == SwipeDirection.Right && RightSwipeMode != SwipeMode.None; } }

        public EasingFunctionBase EasingFunc { get { return Direction == SwipeDirection.Left ? LeftEasingFunc : RightEasingFunc; } }

        public double SwipeLengthRate { get { return Direction == SwipeDirection.Left ? LeftSwipeLengthRate : RightSwipeLengthRate; } }

        public double ActionRateForSwipeLength { get { return Direction == SwipeDirection.Left ? LeftActionRateForSwipeLength : RightActionRateForSwipeLength; } }

        public SwipeMode SwipeMode { get { return Direction == SwipeDirection.Left ? LeftSwipeMode : RightSwipeMode; } }

        public double CurrentSwipeRate { get { return CurrentSwipeWidth / ItemActualWidth / SwipeLengthRate; } }

        public double TriggerActionTargetWidth { get { return ItemActualWidth * SwipeLengthRate; } }
    }
}