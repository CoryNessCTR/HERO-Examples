/**
 * Example demonstrating the position closed-loop servo in a continuous rotation.
 * Tested with Logitech F310 USB Gamepad inserted into HERO.
 * 
 * Use the mini-USB cable to deploy/debug.
 *
 * Be sure to select the correct feedback sensor using SetFeedbackDevice() below,
 * And change change the _sensorRange to match the sensor you're using.
 *
 * After deploying/debugging this to your HERO, first use the righ X-stick 
 * to throttle the Talon manually.  This will confirm your hardware setup.
 * Be sure to confirm that when the Talon is driving forward (green) the 
 * position sensor is moving in a positive direction.  If this is not the cause
 * flip the boolena input to the SetSensorDirection() call below.
 *
 * Once you've ensured your feedback device is in-phase with the motor,
 * use the left X-stick to select your rotational position and the button
 * to set it as your target.  Your mechanical output should always take 
 * the shortest distance to get to the target, even if your sensor value
 * wraps around to a new rotation.
 *
 * Tweak the PID gains accordingly.
 */

using System;
using Microsoft.SPOT;
using CTRE;
using System.Threading;
using System.Text;
using CTRE.MotorControllers;

namespace HERO_Continuous_Position_Servo_Example
{
    public class Program
    {
        static RobotApplication _robotApp = new RobotApplication();

        public static void Main()
        {
            _robotApp.init();

            while(true)
            {
                _robotApp.run();

                Thread.Sleep(10);
            }
        }
    }

    public class RobotApplication
    {
        /** scalor to max throttle in manual mode. */
        const float kJoystickScaler = 0.3f;

        /** hold bottom left shoulder button to enable motors */
        const uint kEnableButton = 7;

        /** make a talon with deviceId 0 */
        TalonSrx _talon = new TalonSrx(0);

        /** Use a USB gamepad plugged into the HERO */
        CTRE.Controller.GameController _gamepad = new CTRE.Controller.GameController(CTRE.UsbHostDevice.GetInstance(0), 0);

        /** hold the current button values from gamepad*/
        bool[] _btns = new bool[10];

        /** hold the last button values from gamepad, this makes detecting on-press events trivial */
        bool[] _btnsLast = new bool[10];

        /** some objects used for printing to the console */
        StringBuilder _sb = new StringBuilder();
        int _timeToPrint = 0;

        float _targetPosition = 0;

        /* We'll need this for calcualting distances
         * It is the maximum sensor value in a single rotation
         * The CTRE Mag Encoder is in units of Rotations,
         *   so for this example it has a range of 1          */
        float _sensorRange = 1;


        public void init()
        {
            /* first choose the sensor */
            _talon.SetFeedbackDevice(TalonSrx.FeedbackDevice.CtreMagEncoder_Absolute);
            _talon.SetSensorDirection(false);

            /* set closed loop gains in slot0 */
            _talon.SetP(0, 0.5f); /* tweak this first, a little bit of overshoot is okay */
            _talon.SetI(0, 0f);
            _talon.SetD(0, 0f);
            _talon.SetF(0, 0f); /* For position servo kF is rarely used. Leave zero */

            /* use slot0 for closed-looping */
            _talon.SelectProfileSlot(0);

            /*set target to the current position */
            ResetTargetPosition();
        }

        public void run()
        {
            Loop10Ms();
            
            //if (_gamepad.GetConnectionStatus() == CTRE.UsbDeviceConnection.Connected) // check if gamepad is plugged in OR....
            if (_gamepad.GetButton(kEnableButton)) // check if bottom left shoulder buttom is held down.
            {
                /* then enable motor outputs*/
                CTRE.Watchdog.Feed();
            }

        }

        void ResetTargetPosition()
        {
            _targetPosition = _talon.GetPosition();
            _talon.SetPosition(_targetPosition); //Sets current and desired positions to be equal so we don't move unexpectedly at startup

            Thread.Sleep(100);  //wait to make sure SetPosition takes effect
        }

        void Loop10Ms()
        {
            /* get all the buttons */
            FillBtns(ref _btns);


            /* scale the x-axis to go from 0 to sensorRange, left to right */
            float leftAxisX = (( (_sensorRange/2) * _gamepad.GetAxis(0)) + (_sensorRange / 2));

            float rightAxisX = kJoystickScaler * _gamepad.GetAxis(2);
            Deadband(ref rightAxisX);

            if(rightAxisX != 0)
            {
                _talon.SetControlMode(ControlMode.kPercentVbus);
                _talon.Set(rightAxisX);
            }
            else if (_talon.GetControlMode() == ControlMode.kPercentVbus)
            {
                _targetPosition = _talon.GetPosition();

                /* user has let go of the stick, lets closed-loop whereever we happen to be */
                EnableClosedLoop();
            }

            /* When you press the 'A' button on a Logitech Gamepad 
                and the enable button is pressed                    */
            if (_btns[2] && !_btnsLast[2] && _gamepad.GetButton(kEnableButton))
            {
                _targetPosition = servo(leftAxisX, _talon.GetPosition(), _sensorRange);
                EnableClosedLoop();
            }
        }

        void EnableClosedLoop()
        {
            /* Make sure we're in closed-loop mode and update the target position */
            _talon.SetVoltageRampRate(0); /* V per sec */
            _talon.SetControlMode(ControlMode.kPosition);
            _talon.Set(_targetPosition);
        }
		/**
		 * @param targetAngPosition target position to servo to (fractional).   Typically this value should be within [0,_sensorRange].
		 * @param currentPosition  The return of Talon's GetPosition().
		 * @param sensorRange  The value representing one rotation of the sensor.  Typically '1' if unit-scaling requirements are met.
		 */
        float servo(float targetAngPosition, float currentPosition, float sensorRange)
        {
            float targetPosition;
            
            /*Calculate where in the rotation you are */
            float currentAngPos = currentPosition % sensorRange;

            /*Calculate the distance needed to travel forward to the target */
            float upDistance= targetAngPosition - currentAngPos;

            /*If the target is a lesser sensor value than your current position,
             * the forward distance will come out negative so you have
             * to increment by a rotation.  */
            if (targetAngPosition < currentAngPos)
                upDistance += sensorRange;

            /*calculate the distance back to the target*/
            float downDistance = upDistance - sensorRange;

            /*use whichever distance is less, then add it to your
             * current position to get your new target */
            if (System.Math.Abs(upDistance) < System.Math.Abs(downDistance))
                targetPosition = currentPosition + upDistance;
            else
                targetPosition = currentPosition + downDistance;

            /*report some values for debuggin when you calculate a new target*/
            report(targetAngPosition, currentPosition, sensorRange, currentAngPos, upDistance, downDistance, targetPosition);

            return targetPosition;
        }

        /**
         * If value is within 10% of center, clear it.
         */
        void Deadband(ref float value)
        {
            if (value < -0.10)
            {
                /* outside of deadband */
            }
            else if (value > +0.10)
            {
                /* outside of deadband */
            }
            else
            {
                /* within 10% so zero it */
                value = 0;
            }
        }

        /** throw all the gamepad buttons into an array */
        void FillBtns(ref bool[] btns)
        {
            for (uint i = 1; i < btns.Length; ++i)
                btns[i] = _gamepad.GetButton(i);
        }

        void report(float targetAngPosition, float currentPosition, float sensorRange, float currentAngPos, float upDistance, float downDistance, float targetPosition)
        {
            _sb.Clear();
            _sb.Append("TargetAngPos=");
            _sb.Append(targetAngPosition);
            _sb.Append(" AbsCurrPos=");
            _sb.Append(currentPosition);
            _sb.Append(" CurrAngPos=");
            _sb.Append(currentAngPos);
            _sb.Append(" UpDist=");
            _sb.Append(upDistance);
            _sb.Append(" DownDist=");
            _sb.Append(downDistance);
            _sb.Append(" AbsTargPos=");
            _sb.Append(targetPosition);
            Debug.Print(_sb.ToString());
        }
    }
}
