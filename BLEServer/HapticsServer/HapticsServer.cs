
/**
 * Wearable Vibrotactile Fingercap for Tabletop/3D Interaction
 *
 *  Copyright 2017 by HCI Group - Universität Hamburg (https://www.inf.uni-hamburg.de/de/inst/ab/mci.html)
 *
 *  Licensed under "The MIT License (MIT) – military use of this product is forbidden – V 0.2".
 *  Some rights reserved. See LICENSE.
 */

/*
 *   Author: Oscar Javier Ariza Nunez <ariza@informatik.uni-hamburg.de>
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;

using HCIHaptics;

namespace HapticsServerNS
{
    public class ServicesContainer
    {
        public static int       dllPort      = 26000;
        public static int       unityPort    = 27000;

        private byte[]      data    = new byte[6];
        private Socket      sender  = null;
        private IPEndPoint  target;

        private const byte StartingIntensity = 20; 

        private byte HapticRTValue          = StartingIntensity;
        private byte HapticEffectValue      = 51;
        private bool irLED                  = false;
        private bool imuOrientationStatus   = false;
        private bool imuAccelerationStatus  = false;
   
        public ServicesContainer(string serverIP)
        {
            sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            target = new IPEndPoint(IPAddress.Parse(serverIP), unityPort);
            Console.WriteLine("UDP sender ready!");
            
            (new Thread(() => this.UDPServer())).Start();
            Console.WriteLine("UDP receiver ready!");
        }

        public void KeyboardSender()
        {
            while (true)
            {
                if (Console.ReadKey(true).Key == ConsoleKey.L)
                {
                    irLED = !irLED;
                    HCIHapticsBLEConnector.sendLEDStatus(irLED);
                    Console.WriteLine("LED >>> " + irLED);
                }
                if (Console.ReadKey(true).Key == ConsoleKey.O)
                {
                    imuOrientationStatus = !imuOrientationStatus;
                    HCIHapticsBLEConnector.sendIMUOrientationStatus(imuOrientationStatus);
                    Console.WriteLine("IMU Orientation >>> " + imuOrientationStatus);
                }
                if (Console.ReadKey(true).Key == ConsoleKey.A)
                {
                    imuAccelerationStatus = !imuAccelerationStatus;
                    HCIHapticsBLEConnector.sendIMUAccelerationStatus(imuAccelerationStatus);
                    Console.WriteLine("IMU Acceleration >>> " + imuAccelerationStatus);
                }
                else if (Console.ReadKey(true).Key == ConsoleKey.N)
                {
                    HapticRTValue--;
                    if (HapticRTValue < StartingIntensity) HapticRTValue = 127;
                    HCIHapticsBLEConnector.sendRTValueToHaptic(HapticRTValue);
                    Console.WriteLine("Haptic RTV >>> " + HapticRTValue); 
                }
                else if (Console.ReadKey(true).Key == ConsoleKey.M)
                {
                    HapticRTValue++;
                    if (HapticRTValue > 127) HapticRTValue = StartingIntensity;
                    HCIHapticsBLEConnector.sendRTValueToHaptic(HapticRTValue);
                    Console.WriteLine("Haptic RTV>>> " + HapticRTValue);
                }
                else if (Console.ReadKey(true).Key == ConsoleKey.V)
                {
                    HapticEffectValue--;
                    if (HapticEffectValue > 123) HapticEffectValue = 123;
                    HCIHapticsBLEConnector.sendEffectValueToHaptic(HapticEffectValue);
                    HCIHapticsBLEConnector.sendEffectValueToHaptic(255); // mandatory command
                    Console.WriteLine("Haptic Effect >>> " + HapticEffectValue);
                }
                else if (Console.ReadKey(true).Key == ConsoleKey.B)
                {
                    HapticEffectValue++;
                    if (HapticEffectValue == 124) HapticEffectValue = 0;
                    HCIHapticsBLEConnector.sendEffectValueToHaptic(HapticEffectValue);
                    HCIHapticsBLEConnector.sendEffectValueToHaptic(255); // mandatory command
                    Console.WriteLine("Haptic Effect >>> " + HapticEffectValue);
                }
                else if (Console.ReadKey(true).Key == ConsoleKey.C)
                {
                    HCIHapticsBLEConnector.sendRTValueToHaptic(0); 
                    Console.WriteLine("Haptic >>> 0");
                }
                else if (Console.ReadKey(true).Key == ConsoleKey.X)
                {
                    HCIHapticsBLEConnector.sendEffectValueToHaptic(10);
                    HCIHapticsBLEConnector.sendEffectValueToHaptic(71);
                    HCIHapticsBLEConnector.sendEffectValueToHaptic(105);
                    HCIHapticsBLEConnector.sendEffectValueToHaptic(52);
                    HCIHapticsBLEConnector.sendEffectValueToHaptic(118);
                    HCIHapticsBLEConnector.sendEffectValueToHaptic(255); // mandatory command
                    Console.WriteLine("Haptic Effect array");

                    Thread.Sleep(300 * 6);
                    HCIHapticsBLEConnector.sendRTValueToHaptic(0);
                }
            }
        }

        public void ButtonListener()
        {
            while (true)
            {
                if (sender == null) continue;

                BUTTON_DATA jData = HCIHapticsBLEConnector.getButtonAData;
                if (jData != BUTTON_DATA.NONE)
                {
                    data = Encoding.ASCII.GetBytes(""+(int)jData);
                    sender.SendTo(data, target);
                    Console.WriteLine("Button A >>> " + jData);
                }

                jData = HCIHapticsBLEConnector.getButtonBData;
                if (jData != BUTTON_DATA.NONE)
                {
                    data = Encoding.ASCII.GetBytes("" + (int)jData);
                    sender.SendTo(data, target);
                    Console.WriteLine("Button B >>> " + jData);
                }

                Thread.Sleep(1);
            }
        }

        byte[] axesData = new byte[2];

        public void JoystickAxesListener()
        {
            while (true)
            {
                if (sender == null) continue;

                Tuple<Byte, Byte> jData = HCIHapticsBLEConnector.getJoystickAxesData;
                if (jData != null)
                {
                    axesData[0] = jData.Item1;
                    axesData[1] = jData.Item2;
                    sender.SendTo(axesData, target);
                    Console.WriteLine("JXZ >>> " + (axesData[0] - 0) + " " + (axesData[1] - 0));
                }

                Thread.Sleep(1);
            }
        }

        public void FlexiForceListener()
        {
            while (true)
            {
                if (sender == null) continue;

                byte fData = HCIHapticsBLEConnector.FlexiDataValue;
                if(fData != 0)
                {
                    //data = Encoding.ASCII.GetBytes("" + (int)fData);
                    //sender.SendTo(data, target);
                    Console.WriteLine("Pressure >>> " + fData);
                }

                Thread.Sleep(1);
            }
        }

        public void IMUOrientationListener()
        {
            while (true)
            {
                if(sender == null) continue;

                byte[] imuData = HCIHapticsBLEConnector.IMUOrientationDataValue;
                if (imuData != null)
                {
                    if (imuData.Length == 16)
                    {
                        //*
                        double qw = (double)((((int)imuData[0] << 24) + ((int)imuData[1] << 16) + ((int)imuData[2] << 8) + imuData[3])) * (1.0 / (1 << 30));
                        double qx = (double)((((int)imuData[4] << 24) + ((int)imuData[5] << 16) + ((int)imuData[6] << 8) + imuData[7])) * (1.0 / (1 << 30));
                        double qy = (double)((((int)imuData[8] << 24) + ((int)imuData[9] << 16) + ((int)imuData[10] << 8) + imuData[11])) * (1.0 / (1 << 30));
                        double qz = (double)((((int)imuData[12] << 24) + ((int)imuData[13] << 16) + ((int)imuData[14] << 8) + imuData[15])) * (1.0 / (1 << 30));
                        Console.Write("IMU Q >>>>>> ");
                        Console.Write(qx + " : ");
                        Console.Write(qy + " : ");
                        Console.Write(qz + " : ");
                        Console.Write(qw);
                        Console.WriteLine();
                        //*/

                        sender.SendTo(imuData, target);
                    }
                    else
                    {
                        Console.WriteLine("Invalid IMU Q frame! ");
                    }
                }
                
                Thread.Sleep(1);
            }
        }

        public void IMUAccelerationListener()
        {
            while (true)
            {
                if (sender == null) continue;

                byte[] imuData = HCIHapticsBLEConnector.IMUAccelerationDataValue;
                if (imuData != null)
                {
                    if (imuData.Length == 12)
                    {
                        //*
                        double ax = (double)((((int)imuData[0] << 24) + ((int)imuData[1] << 16) + ((int)imuData[2] << 8) + imuData[3])) * (1.0 / (1 << 30));
                        double ay = (double)((((int)imuData[4] << 24) + ((int)imuData[5] << 16) + ((int)imuData[6] << 8) + imuData[7])) * (1.0 / (1 << 30));
                        double az = (double)((((int)imuData[8] << 24) + ((int)imuData[9] << 16) + ((int)imuData[10] << 8) + imuData[11])) * (1.0 / (1 << 30));
                        Console.Write("IMU A >>>>>> ");
                        Console.Write(ax + " : ");
                        Console.Write(ay + " : ");
                        Console.Write(az);
                        Console.WriteLine();
                        //*/

                        sender.SendTo(imuData, target);
                    }
                    else
                    {
                        Console.WriteLine("Invalid IMU A frame! ");
                    }
                }

                Thread.Sleep(1);
            }
        }

        private float receivedCommand;

        public void UDPServer()
        {
            byte receivedValue;
            byte[] data;
            UdpClient client = new UdpClient(dllPort);
            IPEndPoint source = new IPEndPoint(IPAddress.Broadcast, unityPort);

            while (true)
            {
                try
                {
                    if(client.Available>=1)
                    {
                        data = client.Receive(ref source);
                        
                        if(Byte.TryParse(Encoding.UTF8.GetString(data), out receivedValue))
                        {
                            HCIHapticsBLEConnector.sendRTValueToHaptic(receivedValue); // TODO: Add support for effects
                            ///Console.WriteLine("Rx >>>>>>>>>>> " + receivedValue);
                        }
                        else if (float.TryParse(Encoding.UTF8.GetString(data), out receivedCommand))
                        {
                            if(receivedCommand == HCIHapticsBLEConnector.ORIENTATION_STATUS_ENABLED)
                                HCIHapticsBLEConnector.sendIMUOrientationStatus(true);
                            else if(receivedCommand == HCIHapticsBLEConnector.ORIENTATION_STATUS_DISABLED)
                                HCIHapticsBLEConnector.sendIMUOrientationStatus(false);
                            else if (receivedCommand == HCIHapticsBLEConnector.LED_STATUS_ENABLED)
                                HCIHapticsBLEConnector.sendLEDStatus(true);
                            else if (receivedCommand == HCIHapticsBLEConnector.LED_STATUS_DISABLED)
                                HCIHapticsBLEConnector.sendLEDStatus(false);

                            Console.WriteLine("Status request >>>>>>>>>>> " + receivedCommand);
                        }
                    }                    
                    Thread.Sleep(1);
                }
                catch (Exception err)
                {
                    Console.WriteLine("EXCEPTION    >>>>>>>>>>>    " + err.ToString());
                }
            }
        }
    };

    class HapticsServer
    {
        static void Main(string[] args)
        {
            try
            {
                HCIHapticsBLEConnector.setup("HCI_FingerCap");
                
                Console.WriteLine("Device Connected and Service Initialized!");

                ServicesContainer container = new ServicesContainer(args[0]);
                (new Thread(() => container.KeyboardSender())).Start();
                (new Thread(() => container.ButtonListener())).Start();
                (new Thread(() => container.JoystickAxesListener())).Start();
                (new Thread(() => container.IMUOrientationListener())).Start();
                (new Thread(() => container.IMUAccelerationListener())).Start();
                (new Thread(() => container.FlexiForceListener())).Start();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
