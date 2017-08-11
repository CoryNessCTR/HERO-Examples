using System;
using System.Threading;
using Microsoft.SPOT;
using System.Text;
using CTRE.MotorControllers;

namespace Hero_Arcade_Drive_Example
{
    public class Program
    {
        /* create a talon */
        static TalonSrx rightSlave = new TalonSrx(4);
        static TalonSrx right = new TalonSrx(3);
        static TalonSrx leftSlave = new TalonSrx(2);
        static TalonSrx left = new TalonSrx(1);

        static StringBuilder stringBuilder = new StringBuilder();

        static CTRE.Mechanical.Gearbox leftG = new CTRE.Mechanical.Gearbox(left, leftSlave);
        static CTRE.Mechanical.Gearbox rightG = new CTRE.Mechanical.Gearbox(right, rightSlave);

        static CTRE.Drive.Tank drive = new CTRE.Drive.Tank(leftG, rightG, false, true);

        static CTRE.Controller.GameController _gamepad = null;

        public static void Main()
        {
            /* loop forever */
            while (true)
            {
                /* drive robot using gamepad */
                Drive();
                /* print whatever is in our string builder */
                Debug.Print(stringBuilder.ToString());
                stringBuilder.Clear();
                /* feed watchdog to keep Talon's enabled */
                CTRE.Watchdog.Feed();
                /* run this task every 20ms */
                Thread.Sleep(20);
            }
        }
        /**
         * If value is within 10% of center, clear it.
         * @param value [out] floating point value to deadband.
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
        static void Drive()
        {
            if (null == _gamepad)
                _gamepad = new CTRE.Controller.GameController(CTRE.UsbHostDevice.GetInstance(0), 0);

            float x = _gamepad.GetAxis(0);
            float y = -1 * _gamepad.GetAxis(1);
            float twist = _gamepad.GetAxis(2);

            Deadband(ref x);
            Deadband(ref y);
            Deadband(ref twist);

            drive.Set(CTRE.Drive.Styles.Basic.PercentOutput, y, twist);
        }
    }
}