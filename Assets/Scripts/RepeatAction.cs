using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class RepeatAction
{
    InputAction action;
    float? downTime = null;
    float? repeatTime = null;

    public float RepeatDelay = 0.35f;
    public float RepeatInterval = 0.05f;

    public RepeatAction(InputAction inputAction, float repeatDelay, float repeatInterval)
    {
        action = inputAction;
        RepeatDelay = repeatDelay;
        RepeatInterval = repeatInterval;
    }

    public bool IsTriggered()
    {
        var time = Time.realtimeSinceStartup;

        if (action.WasPerformedThisFrame())
        {
            downTime = time;

            // Trigger the first
            return true;
        }
        else if (action.IsPressed())
        {
            if (!downTime.HasValue)
            {
                downTime = time;

                // This should't happen if Update is called every frame.
                return true;
            }
            else
            {
                if (!repeatTime.HasValue && time > downTime.Value + RepeatDelay)
                {
                    repeatTime = time;

                    // Trigger the first repeat upon the initial delay
                    return true;
                }

                if (repeatTime.HasValue && time > repeatTime.Value + RepeatInterval)
                {
                    repeatTime = time;

                    // Trigger a seubsquent repeat
                    return true;
                }
            }
        }
        else
        {
            downTime = null;
            repeatTime = null;
        }

        return false;
    }
}
