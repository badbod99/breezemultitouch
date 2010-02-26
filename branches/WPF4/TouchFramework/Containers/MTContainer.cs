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

using TouchFramework.ControlHandlers;

namespace TouchFramework
{
    /// <summary>
    /// Wraps any FrameworkElement object with a controlling interface which stores touch information and 
    /// processes actions based on the touches present.
    /// </summary>
    public abstract class MTContainer : IDisposable
    {
        const int DEFAULT_MINX = 0;
        const int DEFAULT_MINY = 0;
        const int DEFAULT_MAXX = 2000;
        const int DEFAULT_MAXY = 2000;

        TransformGroup transforms = new TransformGroup();
        bool clearTransformsNext = false;

        public readonly FrameworkElement WorkingObject;
        public readonly ElementProperties ElementDef = null;
        public readonly Panel TopContainer = null;
                
        public TouchDictionary ObjectTouches = new TouchDictionary();
        
        public int StartX = 0;
        public int StartY = 0;
        public int MaxX = DEFAULT_MAXX;
        public int MaxY = DEFAULT_MAXY;
        public int MinX = DEFAULT_MINX;
        public int MinY = DEFAULT_MINY;
        public double RealWidth = 0;
        public double RealHeight = 0;

        delegate void InvokeDelegate();

        float fullScale = 1f;
        Matrix oldTranform;

        ElementHandler handler = null;

        PointF relativePos = PointF.Empty;
        PointF oldRelativePos = PointF.Empty;

        private float cumulativeMoveX = 0f;
        private float cumulativeMoveY = 0f;

         /// <summary>
        /// Constructor for MTElementContainer.
        /// </summary>
        /// <param name="createFrom">The FrameworkElement this container is going to store touches for and manipulate.</param>
        public MTContainer(FrameworkElement createFrom, Panel topCont, ElementProperties props)
        {
            if (createFrom == null) throw new ArgumentNullException("createFrom must not be null");
            WorkingObject = createFrom;
            handler = ElementHandler.GetHandler(this.WorkingObject, this.TopContainer);
            ElementDef = props;
            this.TopContainer = topCont;
        }

        /// <summary>
        /// Alias for the GetHashCode function for simplicity.
        /// </summary>
        public int Id
        {
            get
            {
                return this.GetHashCode();
            }
        }

        /// <summary>
        /// Allows you to reset the container after making manual changes to the elements positions.
        /// </summary>
        public virtual void Reset()
        {
        }

        /// <summary>
        /// Returns the hascode of the working object so it matches the hashcode from the hit test.
        /// </summary>
        /// <returns>int identifier from the FrameworkElement.GetHashCode()</returns>
        public override int GetHashCode()
        {
            return WorkingObject.GetHashCode();
        }

        /// <summary>
        /// Takes the touch information present in touch collections and performs actions required (e.g. scale/rotate/move)
        /// NB: Currently performs scale/rotate/move/tap on all objects.  Needs to be object properties to perform only desired actions.
        /// </summary>
        public void ActOnTouches()
        {
            bool hasChanged = checkForChanges();
            if (!hasChanged) return;

            updateRelativePositions();
            cumulativeMoveX += ObjectTouches.MoveX;
            cumulativeMoveY += ObjectTouches.MoveY;

            if (ObjectTouches.JustTouched) DoTouchDown();
            if (ObjectTouches.Lifted) DoTouchUp();
            if (ObjectTouches.Tapped) DoTap();
            if (ObjectTouches.MoveX != 0 || ObjectTouches.MoveY != 0) DoDrag(); 

            if (oldRelativePos != relativePos)
            {
                DoSlide();
                DoScroll();
            }
            
            float angle = ObjectTouches.GetAngleChanged();
            float scale = ObjectTouches.GetDistanceChangeRatio();

            scale = limitScale(scale);
            updateFullScale(scale);

            float moveX = ObjectTouches.MoveX;
            float moveY = ObjectTouches.MoveY;

            var points = getCorners();
            checkPointEdge(points, ref moveX, ref moveY);
            
            this.ScaleRotateMove(angle, scale, moveX, moveY, ObjectTouches.ActionCenter);
        }

        protected bool checkPointEdge(System.Windows.Point[] points)
        {
            bool hit = false;
            foreach (var p in points)
            {
                hit = (hit || p.X < 0);
                hit = (hit || p.Y < 0);
                hit = (hit || p.X > TopContainer.ActualWidth);
                hit = (hit || p.Y > TopContainer.ActualHeight);
            }
            return hit;
        }

        protected void checkPointEdge(System.Windows.Point[] points, ref float moveX, ref float moveY)
        {
            foreach (var p in points)
            {
                if (p.X < 0 && moveX < 0) moveX = 0;
                if (p.Y < 0 && moveY < 0) moveY = 0;
                if (p.X > TopContainer.ActualWidth && moveX > 0) moveX = 0;
                if (p.Y > TopContainer.ActualHeight && moveY > 0) moveY = 0;
            }
        }

        protected System.Windows.Point[] getCorners()
        {
            var t = WorkingObject.TransformToVisual(this.TopContainer);
            System.Windows.Point topLeft = t.Transform(new System.Windows.Point(0,0));
            System.Windows.Point topRight = t.Transform(new System.Windows.Point(WorkingObject.ActualWidth,0));
            System.Windows.Point bottomLeft = t.Transform(new System.Windows.Point(0,WorkingObject.ActualHeight));
            System.Windows.Point bottomRight = t.Transform(new System.Windows.Point(WorkingObject.ActualWidth,WorkingObject.ActualHeight));

            return new System.Windows.Point[] { topLeft, topRight, bottomLeft, bottomRight };
        }

        /// <summary>
        /// Updates the position of the movement center relative to the working object
        /// </summary>
        void updateRelativePositions()
        {
            oldRelativePos = relativePos;
            relativePos = getRelativePos(ObjectTouches.MoveCenter);
            if (oldRelativePos == PointF.Empty || ObjectTouches.JustTouched) oldRelativePos = relativePos;
        }

        /// <summary>
        /// Gets the relative position of screenpoint to the working object.
        /// </summary>
        /// <param name="screenPoint">Point in screen space.</param>
        /// <returns>Point in relative object space.</returns>
        protected PointF getRelativePos(PointF screenPoint)
        {
            GeneralTransform gt = TopContainer.TransformToVisual(this.WorkingObject);
            System.Windows.Point curPoint = gt.Transform(new System.Windows.Point(screenPoint.X, screenPoint.Y));
            return new PointF((float)curPoint.X, (float)curPoint.Y);
        }

        /// <summary>
        /// Performs a slide on the handler for this object
        /// </summary>
        public void DoSlide()
        {
            if (!Supports(TouchAction.Slide)) return;
            if (handler == null) return;

            float slideX, slideY;
            slideX = (relativePos.X - oldRelativePos.X);
            slideY = (relativePos.Y - oldRelativePos.Y);
                        
            handler.Slide(slideX, slideY);
        }

        /// <summary>
        /// Tells the handler to perform a touchdown.
        /// </summary>
        public void DoDrag()
        {
            bool pass = (Math.Abs(cumulativeMoveX) > ElementDef.DragThresholdPixels);
            if (!(this.Supports(TouchAction.Drag) && pass)) return;

            handler.Drag(ObjectTouches.MoveCenter, relativePos);
        }

        /// <summary>
        /// Tells the handler to perform a touchdown.
        /// </summary>
        public void DoTouchDown()
        {
            handler.TouchDown(relativePos);
        }

        /// <summary>
        /// Tells the handler to perform a touchup.
        /// </summary>
        public void DoTouchUp()
        {
            handler.TouchUp(oldRelativePos);
            cumulativeMoveX = 0f;
            cumulativeMoveY = 0f;
        }

        /// <summary>
        /// Performs a scroll on the handler for this object
        /// </summary>
        public void DoScroll()
        {
            if (!(Supports(TouchAction.ScrollX) || Supports(TouchAction.ScrollY))) return;

            float scrollX = 0f, scrollY = 0f;

            if (Supports(TouchAction.ScrollX)) scrollX = (relativePos.X - oldRelativePos.X);
            if (Supports(TouchAction.ScrollY)) scrollY = (relativePos.Y - oldRelativePos.Y);

            handler.Scroll(scrollX, scrollY);
        }

        /// <summary>
        /// Calls the OnTap delegate as provided by the client.  Runs when an object has a finger added when last frame it had none.
        /// </summary>
        public void DoTap()
        {
            if (!Supports(TouchAction.Tap)) return;

            GeneralTransform gt = TopContainer.TransformToVisual(this.WorkingObject);
            System.Windows.Point curPoint = gt.Transform(new System.Windows.Point(ObjectTouches.MoveCenter.X, ObjectTouches.MoveCenter.Y));

            PointF tapPoint = new PointF((float)curPoint.X, (float)curPoint.Y);
            handler.Tap(tapPoint);
        }

        /// <summary>
        /// Performs a rendertransform applying the scale to the working object.
        /// </summary>
        /// <param name="scaleFactor">Value to multiply the object's width and height by</param>
        /// <param name="centerPoint">Point in screen space for the center of the scale operation</param>
        public abstract void Scale(float scaleFactor, PointF centerPoint);

        /// <summary>
        /// Performs a rendertransform moving the object from it's current position (after all previous tranforms).
        /// </summary>
        /// <param name="offsetX">Number of pixels to move the object on the x axis.</param>
        /// <param name="offsetY">Number of pixels to move the object on the y axis.</param>
        public abstract void Move(float offsetX, float offsetY);
        
        /// <summary>
        /// Performs a rendertransform rotating the object around the center point provided from it's current rotation (after all previous transforms).
        /// </summary>
        /// <param name="angle">The angle to rotate by.</param>
        /// <param name="centerPoint">Point in screen space for the center of the operation.</param>
        public abstract void Rotate(float angle, PointF centerPoint);

        /// <summary>
        /// Performs a rendertransform rotating the object around the default center point as calculated by the container (usually the center of the element).
        /// </summary>
        /// <param name="angle">The angle to rotate by.</param>
        public virtual void Rotate(float angle)
        {
            PointF cen = this.GetElementCenter();
            this.Rotate(angle, cen);
        }

        /// <summary>
        /// Combines a scale, rotate and move operation for simplicity
        /// </summary>
        /// <param name="angle">The angle to rotate by.</param>
        /// <param name="scaleFactor">Value to multiply the object's width and height by.</param>
        /// <param name="offsetX">Number of pixels to move the object on the x axis.</param>
        /// <param name="offsetY">Number of pixels to move the object on the y axis.</param>
        /// <param name="centerPoint">Point in screen space for the center of the scale operation.</param>
        public abstract void ScaleRotateMove(float angle, float scaleFactor, float offsetX, float offsetY, PointF centerPoint);

        /// <summary>
        /// Applies all transforms which have been added.  You need to call this if you are adding transforms ad-hoc, e.g. using the Scale, Rotate or Move independant functions.
        /// NOTE: After calling this function, a flag is set to clear the transforms before any further transforms are added.  
        /// This is to work around a problem with the rendertransform logic in WPF that stacks up rendertransforms rather than building them into one matrix transform.
        /// </summary>
        public void ApplyTransforms()
        {
            WorkingObject.RenderTransform = transforms;

            // Store the current transform ready for use in the next frame
            oldTranform = WorkingObject.RenderTransform.Value;

            // Reset our transform group (each time we will fill it with new tranforms)
            clearTransformsNext = true;
        }

        float limitScale(float scale)
        {
            RealWidth = WorkingObject.ActualWidth * (fullScale * scale);
            RealHeight = WorkingObject.ActualHeight * (fullScale * scale);

            if (RealWidth >= this.MaxX && scale > 1f) scale = 1f;
            if (RealHeight >= this.MaxY && scale > 1f) scale = 1f;
            if (RealWidth <= this.MinX && scale < 1f) scale = 1f;
            if (RealHeight <= this.MinY && scale < 1f) scale = 1f;

            return scale;
        }

        void updateFullScale(float scale)
        {
            fullScale *= scale;
        }

        bool checkForChanges()
        {
            ObjectTouches.CalculateChanges();
            return ObjectTouches.Changed;
        }

        protected void addTransform(Transform toAdd)
        {
            addTransforms(new Transform[] { toAdd });
        }

        protected void addTransforms(params Transform[] toAdd)
        {
            // NOTE: Adding the existing transform each time causes a stackoverflow and degrades performance!
            // Leaving this here for reference!
            //      transforms.Children.Add(WorkingObject.RenderTransform);

            if (clearTransformsNext) resetTransforms();

            MatrixTransform oldMt = new MatrixTransform(oldTranform);
            transforms.Children.Add(oldMt);

            for (int i = 0; i < toAdd.Length; i++)
            {
                if (toAdd[i] != null)
                {
                    transforms.Children.Add(toAdd[i]);
                }
            }

            WorkingObject.RenderTransform = transforms;

            // Store the current tranform ready for use in the next frame
            oldTranform = WorkingObject.RenderTransform.Value;
        }

        protected enum IntersectEdge
        {
            Top,
            Bottom,
            Left,
            Right
        }

        //protected bool checkIntersects(IntersectEdge edge)
        //{
        //    Rect objectBounds = getBounds(WorkingObject, TopContainer);
        //    bool intersect = false;

        //    switch (edge)
        //    {
        //        case IntersectEdge.Top:
        //            intersect = (objectBounds.Top <= 0);
        //            break;
        //        case IntersectEdge.Bottom:
        //            intersect = (objectBounds.Bottom >= TopContainer.ActualHeight);
        //            break;
        //        case IntersectEdge.Right:
        //            intersect = (objectBounds.Right >= TopContainer.ActualWidth);
        //            break;
        //        case IntersectEdge.Left:
        //            intersect = (objectBounds.Left <= 0);
        //            break;
        //    }

        //    return intersect;
        //}

        public PointF GetElementCenter()
        {
            Rect r = this.GetElementBounds();
            double x = ((r.Right - r.Left) / 2) + r.X;
            double y = ((r.Bottom - r.Top) / 2) + r.Y;
            return new PointF((float)x, (float)y);
        }

        public Rect GetElementBounds()
        {
            return getBounds(WorkingObject, TopContainer);
        }

        Rect getBounds(FrameworkElement of, FrameworkElement from)
        {
            GeneralTransform transform = of.TransformToVisual(from);
            Rect r = transform.TransformBounds(new Rect(0, 0, of.ActualWidth, of.ActualHeight));
            return r;
        }

        void resetTransforms()
        {
            transforms = new TransformGroup();
        }

        /// <summary>
        /// Tells you whether or not this container supports a specific feature or multiple features if supplied with Or'd enum
        /// </summary>
        /// <param name="action">Or'd enum of features to check for</param>
        /// <returns>Bool whether or not this container supports all featured passed</returns>
        public bool Supports(TouchAction action)
        {
            return this.ElementDef.ElementSupport.CheckSupported(action);
        }

        #region IDisposable Members

        protected bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            if (WorkingObject is IDisposable) ((IDisposable)WorkingObject).Dispose();
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    Cleanup();
                }
                disposed = true;
            }
        }

        protected abstract void Cleanup();

        #endregion

        ~MTContainer()
        {
            Dispose(false);
        }

        public virtual void Tick()
        {
        }
    }
}
