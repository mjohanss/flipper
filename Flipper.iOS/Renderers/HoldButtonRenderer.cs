using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;
using Xamarin.Forms.Platform.iOS;
using Xamarin.Forms;
using Flipper.Controls;
using Flipper.iOS.Renderers;

[assembly: ExportRenderer(typeof(HoldButton), typeof(HoldButtonRenderer))]

namespace Flipper.iOS.Renderers
{
    public class HoldButtonRenderer : VisualElementRenderer<HoldButton> // ViewRenderer<HoldButton, UIView>
    {
        protected override void OnElementChanged(ElementChangedEventArgs<HoldButton> e)
        {
            base.OnElementChanged(e);
            UserInteractionEnabled = true;
        }

        private float _touchProgress;
        private NSTimer _animationTimer;
        private float _timerResolution = 0.01f;
        private State _state = State.Inactive;
        private float _progressLineThickness = 15f;

        private enum State
        {
            Inactive,
            InProgress,
            Completed,
            RollingBack
        }

        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            base.TouchesBegan(touches, evt);

            _state = State.InProgress;

            if(_touchProgress > 0 && this.Element.PressResumed != null && this.Element.PressResumed.CanExecute(null))
            {
                this.Element.PressResumed.Execute(null);
            }

            if (_animationTimer == null)
            {
                _animationTimer = NSTimer.CreateRepeatingScheduledTimer(_timerResolution, TimerUpdate);
            }
        }

        private void TimerUpdate(NSTimer timer)
        {

            if (_state == State.InProgress)
            {
                _touchProgress += _timerResolution / this.Element.Delay;

                if (_touchProgress > 1)
                {
                    _touchProgress = 1;

                    if (_state == State.InProgress)
                    {
                        _state = State.Completed;
                        if (this.Element.PressCompleted != null && this.Element.PressCompleted.CanExecute(null))
                        {
                            this.Element.PressCompleted.Execute(null);
                        }
                    }
                }
            }
            else if (_state == State.RollingBack) 
            {
                _touchProgress -= _timerResolution / this.Element.Delay;

                if (_touchProgress < 0)
                {
                    _touchProgress = 0;

                    if (_animationTimer != null)
                    {
                        _animationTimer.Invalidate();
                        _animationTimer = null;
                    }
                }
            }

            this.Element.TransitionValue = _touchProgress;
            SetNeedsDisplay();
        }

        public override void TouchesEnded(NSSet touches, UIEvent evt)
        {
            base.TouchesEnded(touches, evt);
            Rollback();
        }

        public override void TouchesCancelled(NSSet touches, UIEvent evt)
        {
            base.TouchesCancelled(touches, evt);
            Rollback();
        }

        private void Rollback()
        {
            _state = State.RollingBack;

            if (_touchProgress > 0 && this.Element.PressCanceled != null && this.Element.PressCanceled.CanExecute(null))
            {
                this.Element.PressCanceled.Execute(null);
            }
        }

        public override void Draw(CoreGraphics.CGRect rect)
        {
            var centerX = rect.X + (rect.Width / 2);
            var centerY = rect.Y + (rect.Height / 2);
            var radius = (float)Math.Min(rect.Width / 2, rect.Height /2);
            var startAngle = 0;
            var endAngle = (float)(Math.PI * 2);

            var context = UIGraphics.GetCurrentContext();
            context.SetFillColor(this.Element.Color.ToCGColor());
            context.AddArc(centerX, centerY, radius, startAngle, endAngle, true);
            context.DrawPath(CoreGraphics.CGPathDrawingMode.Fill);

            if(_touchProgress != 0)
            {
                var progress =  (float)(Math.PI * 2) * _touchProgress;

                context.SetStrokeColor(this.Element.ProgressColor.ToCGColor());
                context.SetLineWidth(_progressLineThickness);
                context.AddArc(centerX, centerY, radius - _progressLineThickness / 2f, startAngle - (float)(Math.PI / 2), progress - (float)(Math.PI / 2), false);
                context.DrawPath(CoreGraphics.CGPathDrawingMode.Stroke);
            }
        }
    }
}