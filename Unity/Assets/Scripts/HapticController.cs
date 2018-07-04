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

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using Leap;
public class HapticController : Singleton<HapticController>
{
    public static byte JOYSTICK_DATA_BUTTON_PRESSED = 6;
    public static byte JOYSTICK_DATA_BUTTON_RELEASED = 7;
    public static byte JOYSTICK_DATA_NONE = 10;
    public static byte JOYSTICK_DATA_COMBOL = 11;
    public static byte JOYSTICK_DATA_COMBOR = 12;

    public string serverIP = "127.0.0.1";
    public static int dllPort = 26000;
    public int unityPort = 27000;
    byte[] data;
    Socket sender;
    IPEndPoint target;
    UDPReceiver workerObject;

    public Text messageText;
    public float power;
    public float minFrequency;
    public float maxFrequency;
    public AnimationCurve powerCurve;
    public float powerCurveMaxTime;
    float lastSentPower = 0;
    float nextPower = 0;

    public static byte lastJoystickValue = JOYSTICK_DATA_NONE;

    public static double qw;
    public static double qx;
    public static double qy;
    public static double qz;

    public static bool toggle = false;

    Coroutine c;
    Thread workerThread;
    static GameObject cubeGO;

    public HandController LMHandController;

    public float maximumDistance;
    public float distance;
    private Vector3 signalModePosition;
    HandModel[] hands;
    HandModel aHand;
    Vector3 indexFingerPosition;
    FingerModel finger;

    public float NextPower
    {
        get
        {
            return nextPower;
        }
        set
        {
            nextPower = value;
        }
    }

    public byte LastCommand
    {
        get
        {
            return lastJoystickValue;
        }
        set
        {
            lastJoystickValue = value;
        }
    }

    void Awake()
    {
        sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        target = new IPEndPoint(IPAddress.Parse(serverIP), dllPort);
        Debug.Log("UDP sender ready!");

        workerObject = new UDPReceiver(unityPort, dllPort);
        workerThread = new Thread(workerObject.DoWork);

        // Start the worker thread.
        workerThread.Start();
        Debug.Log("main thread: Starting worker thread...");

        // Loop until worker thread activates.
        while (!workerThread.IsAlive) ;

        Debug.Log("UDP receiver ready!");

        doRequest(0);

        distance = 1000.0f;
        maximumDistance = Vector3.Distance(GameObject.Find("BoxA").transform.position, GameObject.Find("BoxB").transform.position);
        signalModePosition = GameObject.Find("BoxC").transform.position;

        requestChangeOnOrientationStatus(false);
        setStatusByName("BoxA", true);
        setStatusByName("BoxB", true);
        setStatusByName("BoxC", true);
        setMessageText("Please touch the boxes to get different haptic feedback according to the object's material.");
    }

    void OnDestroy()
    {
        doRequest(0);
        Debug.Log("worker thread stopping");

        workerObject.RequestStop();
        workerThread.Join();

        try
        {
            sender.Shutdown(SocketShutdown.Both);
            sender.Close();
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }

        Debug.Log("sender destroyed");
    }

    Quaternion rot;

    void Update()
    {
        hands = LMHandController.GetAllGraphicsHands();
        if (hands.Length > 0)
        {
            aHand = hands[0];
            finger = aHand.fingers[1]; //thumb is index 0
            indexFingerPosition = finger.GetTipPosition();
            distance = Vector3.Distance(signalModePosition, indexFingerPosition);
            if (distance > maximumDistance) distance = -1;
        }
        else
        {
            distance = -1;
            doRequest(0);
        }

        this.gameObject.transform.position = indexFingerPosition;

        //gameObject.GetComponent<Renderer>().material.color = Color.gray;
    }

    public static void SetX(GameObject o, float y)
    {
        Vector3 newPosition = new Vector3(o.transform.position.x, y, o.transform.position.z);
        o.transform.position = newPosition;
    }

    private void setStatusByName(String name, bool status)
    {
        GameObject go = GameObject.Find(name);

        if (go != null)
        {
            go.GetComponent<MeshRenderer>().enabled = status;
            go.GetComponent<Collider>().enabled = status;
        }
    }

    void OnGUI()
    {
        Event e = Event.current;
        if (e.type.Equals(EventType.KeyDown))
        {
            switch (e.keyCode)
            {
                case KeyCode.C:
                    UnityEngine.VR.InputTracking.Recenter();
                    break;
                case KeyCode.UpArrow:
                    doRequest(127);
                    break;
                case KeyCode.DownArrow:
                    doRequest(0);
                    break;
            }
        }
    }

    private void setMessageText(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
    }

    public void requestChangeOnOrientationStatus(bool status)
    {
        data = Encoding.ASCII.GetBytes((status ? -1.0f : -2.0f).ToString());
        sender.SendTo(data, target);
    }

    public void doRequest(float power)
    {
        try
        {
            if (power != lastSentPower)
            {
                data = Encoding.ASCII.GetBytes(((byte)power).ToString());
                sender.SendTo(data, target);
                lastSentPower = power;
            }
        }
        catch (UnityException e)
        {
            Debug.Log("EXCEPTION ******************************* " + e.Message);
        }
    }

    public class UDPReceiver
    {
        UdpClient client;
        IPEndPoint source;

        // This method will be called when the thread is started.
        public void DoWork()
        {
            bool result;

            while (!_shouldStop)
            {
                try
                {
                    if (client.Available >= 1)
                    {
                        byte[] data = client.Receive(ref source);

                        if (data.Length == 16)
                        {
                            qw = (double)((((int)data[0] << 24) + ((int)data[1] << 16) + ((int)data[2] << 8) + data[3])) * (1.0 / (1 << 30));
                            qx = (double)((((int)data[4] << 24) + ((int)data[5] << 16) + ((int)data[6] << 8) + data[7])) * (1.0 / (1 << 30));
                            qy = (double)((((int)data[8] << 24) + ((int)data[9] << 16) + ((int)data[10] << 8) + data[11])) * (1.0 / (1 << 30));
                            qz = (double)((((int)data[12] << 24) + ((int)data[13] << 16) + ((int)data[14] << 8) + data[15])) * (1.0 / (1 << 30));
                            //Debug.Log("IMU >>>>>> " + qx + " : "+ qy + " : "+ qz + " : " + qw);
                        }
                        else
                        {
                            result = Byte.TryParse(Encoding.UTF8.GetString(data), out lastJoystickValue);
                            if (result)
                            {
                                // TODO : extract this to a ComboHelper class
                                Debug.Log("Micro joystick event >>>>>>>>>>> " + lastJoystickValue);
                                
                            }

                            switch (lastJoystickValue)
                            {
                                case 7: //HapRing.JOYSTICK_DATA_BUTTON_RELEASED
                                    {
                                        toggle = false;
                                        break;
                                    }
                                case 6: //HapRing.JOYSTICK_DATA_BUTTON_PUSHED
                                    {
                                        toggle = true;
                                        break;
                                    }
                                default:
                                    break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("EXCEPTION ******************************* " + e.Message);
                }
            }
            client.Close();

            Debug.Log("worker thread: terminating gracefully.");
        }

        public void RequestStop()
        {
            _shouldStop = true;
            Debug.Log("requesting worker thread shutdown");
        }
        // Volatile is used as hint to the compiler that this data
        // member will be accessed by multiple threads.
        private volatile bool _shouldStop = false;

        public UDPReceiver(int port, int dllport)
        {
            client = new UdpClient(port);
            source = new IPEndPoint(IPAddress.Broadcast, dllPort);
        }
    }
}
