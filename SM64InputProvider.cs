using UnityEngine;
using MelonLoader;
using System.Reflection;

namespace LibSM64
{
    public abstract class SM64InputProvider : MonoBehaviour
    {
        public enum Button
        {
            Jump,
            Kick,
            Stomp
        };

        public abstract Vector3 GetCameraLookDirection();
        public abstract Vector2 GetJoystickAxes();
        public abstract bool GetButtonHeld( Button button );
    }

    // This will be your class that reads the game's inputs and converts them to Mario inputs.
    public class SM64InputGame : SM64InputProvider
    {
        public override Vector3 GetCameraLookDirection()
        {
            return new Vector3(-Camera.main.transform.forward.z, 0, Camera.main.transform.forward.x);
        }

        public override Vector2 GetJoystickAxes()
        {
            // Check for held button or left analog stick axis in the player's input object.
            // For analog stick: return new Vector2(axis.z, -axis.x);
            // For button held: return -((buttonLeft) ? Vector2.left : (buttonRight) ? Vector2.right : Vector2.zero);
            Rewired.Player P = (Rewired.Player)typeof(RInput).GetField("P", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Singleton<RInput>.Instance);
            return new Vector2(P.GetAxis("Left Stick Y"), -P.GetAxis("Left Stick X")).normalized;
        }

        public override bool GetButtonHeld(Button button)
        {
            // Check against the game's button presses
            Rewired.Player P = (Rewired.Player)typeof(RInput).GetField("P", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Singleton<RInput>.Instance);
            bool result = false;
            switch (button)
            {
                case Button.Jump:
                    //result = inp.GetButton(JUMP);
                    result = P.GetButton("Button A");
                    break;

                case Button.Kick:
                    //result = inp.GetButton(EQUIPMENT);
                    result = P.GetButton("Button B");
                    break;

                case Button.Stomp:
                    //result = inp.GetButton(INTERACT);
                    break;
            }

            return result;
        }
    }
}