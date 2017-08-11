using System;
using System.Threading;
using Microsoft.SPOT;
using System.Text;
using CTRE.MotorControllers;

namespace HERO_Mecanum_Drive_Example
{
    public class Program
    {
        /* create a talon */
        static TalonSrx leftFrnt = new TalonSrx(4);
        static TalonSrx leftRear = new TalonSrx(3);
        static TalonSrx rghtFrnt = new TalonSrx(2);
        static TalonSrx rghtRear = new TalonSrx(1);

        static CTRE.Controller.GameController _gamepad = null;

        static CTRE.Drive.Mecanum drive = new CTRE.Drive.Mecanum(leftFrnt, leftRear, rghtFrnt, rghtRear);

        public static void Main()
        {
            /* loop forever */
            while (true)
            {
                /* keep feeding watchdog to enable motors */
                CTRE.Watchdog.Feed();

                Drive();

                Thread.Sleep(20);
            }
        }
        /**
         * If value is within 10% of center, clear it.
         * @param [out] floating point value to deadband.
         */
        static void Deadband(ref float value)
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
        /**
         * Nomalize the vector sum of mecanum math.  Some prefer to 
         * scale from the max possible value to '1'.  Others
         * prefer to simply cut off if the sum exceeds '1'.
         */
        static void Normalize(ref float toNormalize)
        {
            if (toNormalize > 1)
            {
                toNormalize = 1;
            }
            else if (toNormalize < -1)
            {
                toNormalize = -1;
            }
            else
            {
                /* nothing to do */
            }
        }
        static void Drive()
        {
            if (null == _gamepad)
                _gamepad = new CTRE.Controller.GameController(CTRE.UsbHostDevice.GetInstance(0), 0);

            float x = _gamepad.GetAxis(0);      // Positive is strafe-right, negative is strafe-left
            float y = -1 * _gamepad.GetAxis(1); // Positive is forward, negative is reverse
            float turn = _gamepad.GetAxis(2);  // Positive is turn-right, negative is turn-left

            Deadband(ref x);
            Deadband(ref y);
            Deadband(ref turn);

            drive.Set(CTRE.Drive.Styles.Basic.PercentOutput, y, x, turn);
        }
    }
}