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

namespace TouchFramework
{
    /// <summary>
    /// Bitwise enum to allow simple storage of multi-options in one var
    /// and easy comparison of supported actions.
    /// </summary>
    public enum TouchAction
    {
        None = 1,
        Move = 2,
        Resize = 4,
        Rotate = 8,
        Flick = 16,
        Spin = 32,
        SelectToFront = 64,
        Tap = 128,
        Slide = 256,
        ScrollX = 512,
        ScrollY = 1024,
        Drag = 2048
    }

    /// <summary>
    /// Handles bitwise support for TouchAction enums to allow containers to 
    /// easily specify the multiple behaviours they support.
    /// </summary>
    public class SupportedActions
    {
        TouchAction actions = TouchAction.None;

        public void AddSupport(TouchAction action)
        {
            actions |= action;
        }

        public bool CheckSupported(TouchAction action)
        {
            return (actions & action) == action;
        }

        public void AddSupportForAll()
        {
            var vals = Enum.GetValues(typeof(TouchAction));
            foreach (TouchAction val in vals)
            {
                actions |= val;
            }
        }
    }
}
