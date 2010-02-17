/*
TouchFramework connects touch tracking from a tracking engine to WPF controls 
allow scaling, rotation, movement and other multi-touch behaviours.

Copyright 2009 - Mindstorm Limited (reg. 05071596)

Author - Simon Lerpiniere

This file is part of TouchFramework.

TouchFramework is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

TouchFramework is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser Public License for more details.

You should have received a copy of the GNU Lesser Public License
along with TouchFramework.  If not, see <http://www.gnu.org/licenses/>.

If you have any questions regarding this library, or would like to purchase 
a commercial licence, please contact Mindstorm via www.mindstorm.com.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

using System.Windows;
using System.Windows.Shapes;
using System.Drawing;
using System.Timers;
using System.IO;
using System.Diagnostics;

namespace TouchFramework
{
    /// <summary>
    /// Wraps any FrameworkElement object with a controlling interface which stores touch information and 
    /// processes actions based on the touches present.
    /// </summary>
    public class MTSmoothContainer : MTContainer, IDisposable
    {
        object sync = new object();
        Timer timer;

        LinearFilter2d TranslateFilter = new LinearFilter2d();
        LinearFilter2d CenterFilter = new LinearFilter2d();
        LinearFilter RotateFilter = new LinearFilter();
        LinearFilter ScaleFilter = new LinearFilter();
        LinearFilter2d DampingFilter = new LinearFilter2d();
        LinearFilter AngularDampingFilter = new LinearFilter();

        bool centerInit = false;

        delegate void InvokeDelegate();

        /// <summary>
        /// Constructor for MTElementC                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        ontainer.
        /// </summary>
        /// <param name="createFrom">The FrameworkElement this container is going to store touches for and manipulate.</param>
        public MTSmoothContainer(FrameworkElement createFrom, Panel cont, ElementProperties props)
            : base(createFrom, cont, props)
        {
            this.timer = new Timer();
            this.timer.Interval = 3;
            this.timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            this.timer.Start();

            this.ScaleFilter.Reset(1.0f, 1.0f);

            this.Delay = 100;
            this.DampingDelay = 1200;
        }

        /// <summary>
        /// Delay in Milliseconds which handles the length of time the filters run for.
        /// A lower delay values makes movement faster, a higher value slows everything down.
        /// The default of 100 is usually fine here.
        /// </summary>
        public int Delay
        {
            set
            {
                this.TranslateFilter.Delay = value;
                this.RotateFilter.Delay = value;
                this.ScaleFilter.Delay = value;
                this.CenterFilter.Delay = value;
                this.timer.Enabled = (value > 0);
            }
        }

        /// <summary>
        /// Delay in Milliseconds which handles the length of time the damping filters run for.
        /// Damping filters are used for the inertia functionality.  A lower value results in the element
        /// moving a shorter distance when flicked, a higher value increases the distance.
        /// </summary>
        public int DampingDelay
        {
            set
            {
                this.DampingFilter.Delay = value;
                this.AngularDampingFilter.Delay = value;
            }
        }

        /// <summary>
        /// Performs a rendertransform applying the scale to the working object.
        /// </summary>
        /// <param name="scaleFactor">Value to multiply the object's width and height by</param>
        /// <param name="centerPoint">Point in screen space for the center of the scale operation</param>
        public override void Scale(float scaleFactor, PointF centerPoint)
        {
            if (!Supports(TouchAction.Resize)) return;
            SetCenterTarget(centerPoint);
            if (scaleFactor != 1.0f && scaleFactor != 0.0f)
            {
                this.ScaleFilter.Target *= scaleFactor;
            }
        }

        /// <summary>
        /// Performs a rendertransform moving the object from it's current position (after all previous tranforms).
        /// </summary>
        /// <param name="offsetX">Number of pixels to move the object on the x axis.</param>
        /// <param name="offsetY">Number of pixels to move the object on the y axis.</param>
        public override void Move(float offsetX, float offsetY)
        {
            if (!Supports(TouchAction.Move)) return;
            PointF target = TranslateFilter.Target;
            target.X += offsetX;
            target.Y += offsetY;
            TranslateFilter.Target = target;
        }
        
        /// <summary>
        /// Performs a rendertransform rotating the object around the center point provided from it's current rotation (after all previous transforms).
        /// </summary>
        /// <param name="angle">The angle to rotate by.</param>
        /// <param name="centerPoint">Point in screen space for the center of the scale operation.</param>
        public override void Rotate(float angle, PointF centerPoint)
        {
            if (!Supports(TouchAction.Rotate)) return;
            SetCenterTarget(centerPoint);
            if (angle < 170 && angle > -170)
            {
                this.RotateFilter.Target += angle;
            }
        }

        /// <summary>
        /// Combines a scale, rotate and move operation for simplicity
        /// </summary>
        /// <param name="angle">The angle to rotate by.</param>
        /// <param name="scaleFactor">Value to multiply the object's width and height by.</param>
        /// <param name="offsetX">Number of pixels to move the object on the x axis.</param>
        /// <param name="offsetY">Number of pixels to move the object on the y axis.</param>
        /// <param name="centerPoint">Point in screen space for the center of the scale operation.</param>
        public override void ScaleRotateMove(float angle, float scaleFactor, float offsetX, float offsetY, PointF centerPoint)
        {
            PointF target = TranslateFilter.Target;
            target.X += offsetX;
            target.Y += offsetY;
            TranslateFilter.Target = target;
            SetCenterTarget(centerPoint);
        
            if (scaleFactor != 1.0f && scaleFactor != 0.0f)
            {
                this.ScaleFilter.Target *= scaleFactor;
            }

            if (angle < 170 && angle > -170)
            {
                this.RotateFilter.Target += angle;
            }
        }
        
        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            smoothActions();
        }

        void smoothActions()
        {
            lock (sync)
            {
                if (Supports(TouchAction.Move)) this.TranslateFilter.Step();
                if (Supports(TouchAction.Rotate)) this.RotateFilter.Step();
                if (Supports(TouchAction.Resize)) this.ScaleFilter.Step();
                if (Supports(TouchAction.Move) || Supports(TouchAction.Resize)) this.CenterFilter.Step();

                // If we've just been touched, stop all damping
                // NOTE: You can disable this to test spinning move.
                if (this.ObjectTouches.JustTouched) { this.DampingFilter.Stop(); }
                if (this.ObjectTouches.TwoOrMoreTouch) { this.AngularDampingFilter.Stop(); }

                // If we support flicking then dampen the movement
                if (Supports(TouchAction.Flick))
                {
                    if (!this.DampingFilter.IsFiltering && this.ObjectTouches.Lifted)
                    {
                        PointF point = this.TranslateFilter.LastVelocityFromSet;
                        this.DampingFilter.Reset(point, new PointF(0, 0));
                    }
                    this.DampingFilter.StepIfFiltering();
                }
                if (Supports(TouchAction.Spin))
                {
                    if (!this.AngularDampingFilter.IsFiltering && this.ObjectTouches.OneOrMoreLifted)
                    {
                        float angle = this.RotateFilter.LastVelocityFromSet;
                        this.AngularDampingFilter.Reset(angle, 0);
                    }
                    this.AngularDampingFilter.StepIfFiltering();
                }

                WorkingObject.Dispatcher.BeginInvoke((InvokeDelegate)delegate() { this.updatePosition(); });
            }
        }

        void stopDamping()
        {
            this.DampingFilter.Stop();
            this.AngularDampingFilter.Stop();
        }

        void updatePosition()
        {
            PointF centerPoint = this.CenterFilter.Position;

            double dCenX = centerPoint.X - StartX;
            double dCenY = centerPoint.Y - StartY;
            
            float scaleFactor = 1.0f;
            scaleFactor /= ScaleFilter.PreviousPosition;
            scaleFactor *= ScaleFilter.Position;

            ScaleTransform st = new ScaleTransform(scaleFactor, scaleFactor, dCenX, dCenY);
            TranslateTransform tt = new TranslateTransform(this.TranslateFilter.Velocity.X, this.TranslateFilter.Velocity.Y);
            RotateTransform rt = new RotateTransform(RotateFilter.Velocity, dCenX, dCenY);

            double dPosX = 0, dPosY = 0;
            float dRotPos = 0f;
            
            if (this.DampingFilter.IsFiltering)
            {
                dPosX = this.DampingFilter.Position.X;
                dPosY = this.DampingFilter.Position.Y;

                dCenX = (centerPoint.X + this.DampingFilter.CumulativePosition.X) - StartX;
                dCenY = (centerPoint.Y + this.DampingFilter.CumulativePosition.Y) - StartY;

                // If we are supposed to check the bounds with the container, check using the intersect
                if (this.Supports(TouchAction.BoundsCheck))
                {
                    if (dPosX > 0 && checkIntersects(IntersectEdge.Right)) this.DampingFilter.Stop();
                    if (dPosX < 0 && checkIntersects(IntersectEdge.Left)) this.DampingFilter.Stop();
                    if (dPosY < 0 && checkIntersects(IntersectEdge.Top)) this.DampingFilter.Stop();
                    if (dPosY > 0 && checkIntersects(IntersectEdge.Bottom)) this.DampingFilter.Stop();
                }
            }

            if (this.AngularDampingFilter.IsFiltering)
            {
                dRotPos = this.AngularDampingFilter.Position;
            }

            TranslateTransform dt = new TranslateTransform(dPosX, dPosY);
            RotateTransform drt = new RotateTransform(dRotPos, dCenX, dCenY);           
            addTransforms(st, tt, dt, rt, drt);

            this.ApplyTransforms();
        }

        void SetCenterTarget(PointF target)
        {
            CheckInitCentre();
            CenterFilter.Target = target;
        }

        void CheckInitCentre()
        {
            if (centerInit) return;
            PointF cen = this.GetElementCenter();
            this.CenterFilter.Reset(cen, cen);
            centerInit = true;
        }

        public void Stop()
        {
            timer.Stop();
        }

        #region IDisposable Members

        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.Stop();
                    timer.Dispose();
                }
                disposed = true;
            }
        }

        #endregion

        ~MTSmoothContainer()
        {
            Dispose(false);
        }
    }
}

