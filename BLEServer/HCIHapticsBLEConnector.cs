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

using Windows.Storage.Streams;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Devices.Enumeration.Pnp;

namespace HCIHaptics
{
    public enum BUTTON_DATA
    {
        BUTTONA_PRESSED,
        BUTTONA_RELEASED,
        BUTTONB_PRESSED,
        BUTTONB_RELEASED,
        NONE
    }

    public delegate void DeviceConnectionUpdatedHandler(bool isConnected);
    
    public class HCIHapticsBLEConnector
    {
        public const float ORIENTATION_STATUS_ENABLED   = -1;
        public const float ORIENTATION_STATUS_DISABLED  = -2;
        public const float LED_STATUS_ENABLED           = -3;
        public const float LED_STATUS_DISABLED          = -4;

        public static HCIHapticsBLEConnector getInstance()
        {
            return instance;
        }

        public bool IsServiceInitialized { get; set; }

        public static BUTTON_DATA getButtonAData
        {
            get
            {
                if (getInstance().buttonAData.Count == 0) return BUTTON_DATA.NONE;

                BUTTON_DATA retval;

                lock (getInstance().buttonAData)
                {
                    retval = getInstance().buttonAData[0];
                    getInstance().buttonAData.RemoveAt(0);
                }

                return retval;
            }
        }

        public static BUTTON_DATA getButtonBData
        {
            get
            {
                if (getInstance().buttonBData.Count == 0) return BUTTON_DATA.NONE;

                BUTTON_DATA retval;

                lock (getInstance().buttonBData)
                {
                    retval = getInstance().buttonBData[0];
                    getInstance().buttonBData.RemoveAt(0);
                }

                return retval;
            }
        }

        public static Tuple<Byte, Byte> getJoystickAxesData
        {
            get
            {
                if (getInstance().joystickAxesData.Count == 0) return null;

                Tuple<Byte, Byte> retval;

                lock (getInstance().joystickAxesData)
                {
                    retval = getInstance().joystickAxesData[0];
                    getInstance().joystickAxesData.RemoveAt(0);
                }

                return retval;
            }
        }

        public static byte[] IMUOrientationDataValue
        {
            get
            {
                lock (getInstance().IMUOrientationData)
                {
                    int count = getInstance().IMUOrientationData.Count;

                    if (count == 0 )
                    {
                        return null;
                    }
 
                    byte[] retval = new byte[count];

                    for (int i = 0; i < count; i++) retval[i] = getInstance().IMUOrientationData[i]; //getInstance().IMUData.CopyTo(retval);
                    getInstance().IMUOrientationData.Clear();

                    return retval;
                }
            }
        }

        public static byte[] IMUAccelerationDataValue
        {
            get
            {
                lock (getInstance().IMUAccelerationData)
                {
                    int count = getInstance().IMUAccelerationData.Count;

                    if (count == 0)
                    {
                        return null;
                    }

                    byte[] retval = new byte[count];

                    for (int i = 0; i < count; i++) retval[i] = getInstance().IMUAccelerationData[i]; //getInstance().IMUData.CopyTo(retval);
                    getInstance().IMUAccelerationData.Clear();

                    return retval;
                }
            }
        }

        public static byte FlexiDataValue
        {
            get
            {
                if (getInstance().flexiData.Count == 0) return 0;

                byte retval;

                lock (getInstance().flexiData)
                {
                    retval = getInstance().flexiData[0];
                    getInstance().flexiData.RemoveAt(0);
                }

                return retval;
            }
        }

        /// //////////////////////////////////////////////////////////////////////////////////////////////////////
        /// plublic access to the HapRing data
        /// //////////////////////////////////////////////////////////////////////////////////////////////////////

        public static void sendRTValueToHaptic(byte value) { getInstance().setValueForHapticRTV(value); }
        public static void sendEffectValueToHaptic(byte value) { getInstance().setValueForHapticEffect(value); }
        public static void sendLEDStatus(bool status) { getInstance().setLEDStatus(status); }
        public static void sendIMUOrientationStatus(bool status) { getInstance().setIMUOrientationStatus(status); }
        public static void sendIMUAccelerationStatus(bool status) { getInstance().setIMUAccelerationStatus(status); }

        /// //////////////////////////////////////////////////////////////////////////////////////////////////////

        private HCIHapticsBLEConnector()
        {
            buttonAData = new List<BUTTON_DATA>();
            buttonBData = new List<BUTTON_DATA>();
            joystickAxesData = new List<Tuple<Byte,Byte>>();
            IMUOrientationData = new List<byte>();
            IMUAccelerationData = new List<byte>();
            flexiData = new List<byte>();
        }

        ~HCIHapticsBLEConnector()
        {
            clear();
        }

        public static void clear()
        {
            getInstance().setLEDStatus(false);
            getInstance().setIMUOrientationStatus(false);
            getInstance().setValueForHapticRTV((byte)0);
            getInstance().buttonAData.Clear();
            getInstance().buttonBData.Clear();
            getInstance().joystickAxesData.Clear();

            if (getInstance().service != null)
            {
                getInstance().service.Dispose();
                getInstance().service = null;
            }

            if (getInstance().characteristicJoystickAxes != null) getInstance().characteristicJoystickAxes = null;
            if (getInstance().characteristicButtonA != null) getInstance().characteristicButtonA = null;
            if (getInstance().characteristicButtonB != null) getInstance().characteristicButtonB = null;
            if (getInstance().characteristicHapticRTV != null) getInstance().characteristicHapticRTV = null;
            if (getInstance().characteristicHapticEffect != null) getInstance().characteristicHapticEffect = null;
            if (getInstance().characteristicLED != null) getInstance().characteristicLED = null;
            if (getInstance().characteristicIMUOrientationStatus != null) getInstance().characteristicIMUOrientationStatus = null;
            if (getInstance().characteristicIMUAccelerationStatus != null) getInstance().characteristicIMUAccelerationStatus = null;
            if (getInstance().characteristicIMUOrientation != null) getInstance().characteristicIMUOrientation = null;
            if (getInstance().characteristicIMUAcceleration != null) getInstance().characteristicIMUAcceleration = null;
            if (getInstance().characteristicFlexi != null) getInstance().characteristicFlexi = null;
            
            //getInstance().IsServiceInitialized = false;

            if (watcher != null)
            {
                watcher.Stop();
                watcher = null;
            }
        }
        
        private async void setValueForHapticRTV(byte value)
        {
            if (IsServiceInitialized && characteristicHapticRTV != null)
            {
                writerHapticRTV.WriteByte(value);
                await characteristicHapticRTV.WriteValueAsync(writerHapticRTV.DetachBuffer(), GattWriteOption.WriteWithoutResponse);
            }
            else
            {
                throw new Exception("The Service is not initialized, please initialize it before writing a Characteristic Value.");
            }
        }

        private async void setValueForHapticEffect(byte value)
        {
            if (IsServiceInitialized && characteristicHapticEffect != null)
            {

                writerHapticEffect.WriteByte(value);
                await characteristicHapticEffect.WriteValueAsync(writerHapticEffect.DetachBuffer(), GattWriteOption.WriteWithoutResponse);
            }
            else
            {
                throw new Exception("The Service is not initialized, please initialize it before writing a Characteristic Value.");
            }
        }

        private async void setLEDStatus(bool status)
        {
            if (IsServiceInitialized && characteristicLED != null)
            {
                writerLED.WriteByte(status ? (byte)1 : (byte)0);
                await characteristicLED.WriteValueAsync(writerLED.DetachBuffer(), GattWriteOption.WriteWithoutResponse);
            }
            else
            {
                throw new Exception("The Service is not initialized, please initialize it before writing a Characteristic Value.");
            }
        }

        private async void setIMUOrientationStatus(bool status)
        {
            if (IsServiceInitialized && characteristicIMUOrientationStatus != null)
            {
                writerIMUOrientationStatus.WriteByte(status? (byte)1 : (byte)0);
                await characteristicIMUOrientationStatus.WriteValueAsync(writerIMUOrientationStatus.DetachBuffer(), GattWriteOption.WriteWithoutResponse);
            }
            else
            {
                throw new Exception("The Service is not initialized, please initialize it before writing a Characteristic Value.");
            }
        }

        private async void setIMUAccelerationStatus(bool status)
        {
            if (IsServiceInitialized && characteristicIMUAccelerationStatus != null)
            {
                writerIMUAccelerationStatus.WriteByte(status ? (byte)1 : (byte)0);
                await characteristicIMUAccelerationStatus.WriteValueAsync(writerIMUAccelerationStatus.DetachBuffer(), GattWriteOption.WriteWithoutResponse);
            }
            else
            {
                throw new Exception("The Service is not initialized, please initialize it before writing a Characteristic Value.");
            }
        }

        /*
        GattCommunicationStatus status = 
        await heartRateControlPointCharacteristic.WriteValueAsync
        (
            writer.DetachBuffer(),GattWriteOption.WriteWithoutResponse
        );

        if(status != GattCommunicationStatus.Success)
        {
            throw new Exception("Your device is unreachable, most likely the device is out of range, " +
                "or is running low on battery, please make sure your device is working and try again.");
        }
        //*/

        public static void setup(string deviceName)
        {
            var task = getInstance().setupInternal(deviceName);
            Task.WaitAll(task);
        }

        public async Task setupInternal(string deviceName)
        {
            try
            {
                DeviceInformation deviceInfo = null;

                var devices = await DeviceInformation.FindAllAsync
                (
                    GattDeviceService.GetDeviceSelectorFromUuid(new Guid("0000180A-0000-1000-8000-00805F9B34FB")), // Device Information Service
                    new string[] { "System.Devices.ContainerId" }
                );

                if (devices.Count > 0)
                {
                    foreach (var device in devices)
                    {
                        if (device.Name.Equals(deviceName))
                        {
                            deviceInfo = device;
                            break;
                        }
                    }
                }
                else
                {
                    throw new Exception("Could not find any devices. Please make sure your device is paired and powered on!");
                }
                
                // Check if the device is initially connected, and display the appropriate message to the user
                var deviceObject = await PnpObject.CreateFromIdAsync
                (
                    PnpObjectType.DeviceContainer,
                    deviceInfo.Properties["System.Devices.ContainerId"].ToString(),
                    new string[] { "System.Devices.Connected" }
                );

                bool isConnected;
                Boolean.TryParse(deviceObject.Properties["System.Devices.Connected"].ToString(), out isConnected);

                if(isConnected)
                {
                    await InitializeServiceAsync(deviceInfo);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Retrieving device properties failed with message: " + e.Message);
            }
        }

        public async Task InitializeServiceAsync(DeviceInformation device)
        {
            try
            {
                deviceContainerId = "{" + device.Properties["System.Devices.ContainerId"] + "}";

                service = await GattDeviceService.FromIdAsync(device.Id);
                if (service != null)
                {
                    await ConfigureServiceForNotificationsAsync();
                    IsServiceInitialized = true;
                }
                else
                {
                    throw new Exception("Access to the device is denied, because the application was not granted access, " +
                        "or the device is currently in use by another application.");
                }
            }
            catch (Exception e)
            {
                throw new Exception("ERROR: Accessing your device failed." + Environment.NewLine + e.Message);
            }
        }

        private async Task ConfigureServiceForNotificationsAsync()
        {
            try
            {
                /*
                for (ushort i = 0; i < 0xffff; ++i)
                {
                    var characteristics = service.GetCharacteristics(GattCharacteristic.ConvertShortIdToUuid(i));
                    
                    if (characteristics.Count > 0)
                    {
                        var characteristic1 = characteristics[0];
                        var value = await characteristic1.ReadValueAsync();
                    }
                }
                */

                if
                (
                    service.GetCharacteristics(new Guid("0000FFFA-0000-1000-8000-00805F9B34FB")).Count == 0 ||
                    service.GetCharacteristics(new Guid("0000FFFB-0000-1000-8000-00805F9B34FB")).Count == 0 ||
                    service.GetCharacteristics(new Guid("0000FFFC-0000-1000-8000-00805F9B34FB")).Count == 0 ||
                    service.GetCharacteristics(new Guid("0000FFFD-0000-1000-8000-00805F9B34FB")).Count == 0 ||
                    service.GetCharacteristics(new Guid("0000FFFE-0000-1000-8000-00805F9B34FB")).Count == 0 ||
                    service.GetCharacteristics(new Guid("0000FFF1-0000-1000-8000-00805F9B34FB")).Count == 0 ||
                    service.GetCharacteristics(new Guid("0000FFF2-0000-1000-8000-00805F9B34FB")).Count == 0 ||
                    service.GetCharacteristics(new Guid("0000FFF3-0000-1000-8000-00805F9B34FB")).Count == 0 ||
                    service.GetCharacteristics(new Guid("0000FFF4-0000-1000-8000-00805F9B34FB")).Count == 0 ||
                    service.GetCharacteristics(new Guid("0000FFF5-0000-1000-8000-00805F9B34FB")).Count == 0 ||
                    service.GetCharacteristics(new Guid("0000FFA1-0000-1000-8000-00805F9B34FB")).Count == 0 ||
                    service.GetCharacteristics(new Guid("0000FFA2-0000-1000-8000-00805F9B34FB")).Count == 0
                    service.GetCharacteristics(new Guid("0000FFF6-0000-1000-8000-00805F9B34FB")).Count == 0
                )
                {
                    throw new Exception("A required characteristic was not found on your device.");
                }

                characteristicHapticRTV             = service.GetCharacteristics(new Guid("0000FFFA-0000-1000-8000-00805F9B34FB"))[0];
                characteristicHapticEffect          = service.GetCharacteristics(new Guid("0000FFFB-0000-1000-8000-00805F9B34FB"))[0];
                characteristicLED                   = service.GetCharacteristics(new Guid("0000FFFC-0000-1000-8000-00805F9B34FB"))[0];
                characteristicIMUOrientationStatus  = service.GetCharacteristics(new Guid("0000FFFD-0000-1000-8000-00805F9B34FB"))[0];
                characteristicIMUAccelerationStatus = service.GetCharacteristics(new Guid("0000FFFE-0000-1000-8000-00805F9B34FB"))[0];
                characteristicJoystickAxes          = service.GetCharacteristics(new Guid("0000FFF1-0000-1000-8000-00805F9B34FB"))[0];
                characteristicButtonA               = service.GetCharacteristics(new Guid("0000FFF4-0000-1000-8000-00805F9B34FB"))[0];
                characteristicButtonB               = service.GetCharacteristics(new Guid("0000FFF5-0000-1000-8000-00805F9B34FB"))[0];
                characteristicIMUOrientation        = service.GetCharacteristics(new Guid("0000FFA1-0000-1000-8000-00805F9B34FB"))[0];
                characteristicIMUAcceleration       = service.GetCharacteristics(new Guid("0000FFA2-0000-1000-8000-00805F9B34FB"))[0];
                characteristicFlexi                 = service.GetCharacteristics(new Guid("0000FFF6-0000-1000-8000-00805F9B34FB"))[0];

                characteristicJoystickAxes.ValueChanged     += characteristicJoystickAxes_ValueChanged;
                characteristicButtonA.ValueChanged += characteristicButtonA_ValueChanged;
                characteristicButtonB.ValueChanged += characteristicButtonB_ValueChanged;

                characteristicIMUOrientation.ValueChanged   += characteristicIMUOrientation_ValueChanged;
                characteristicIMUAcceleration.ValueChanged  += characteristicIMUAcceleration_ValueChanged;
                characteristicFlexi.ValueChanged            += characteristicFlexi_ValueChanged;

                var currentDescriptor = await characteristicJoystickAxes.ReadClientCharacteristicConfigurationDescriptorAsync();
                if ((currentDescriptor.Status != GattCommunicationStatus.Success) || (currentDescriptor.ClientCharacteristicConfigurationDescriptor != GattClientCharacteristicConfigurationDescriptorValue.Notify))
                {
                    GattCommunicationStatus status = await characteristicJoystickAxes.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                    if (status == GattCommunicationStatus.Unreachable) { StartDeviceConnectionWatcher(); }
                }

                currentDescriptor = await characteristicButtonA.ReadClientCharacteristicConfigurationDescriptorAsync();
                if ((currentDescriptor.Status != GattCommunicationStatus.Success) || (currentDescriptor.ClientCharacteristicConfigurationDescriptor !=  GattClientCharacteristicConfigurationDescriptorValue.Notify))
                {
                    GattCommunicationStatus status = await characteristicButtonA.WriteClientCharacteristicConfigurationDescriptorAsync( GattClientCharacteristicConfigurationDescriptorValue.Notify);
                    if (status == GattCommunicationStatus.Unreachable) { StartDeviceConnectionWatcher(); }
                }

                currentDescriptor = await characteristicButtonB.ReadClientCharacteristicConfigurationDescriptorAsync();
                if ((currentDescriptor.Status != GattCommunicationStatus.Success) || (currentDescriptor.ClientCharacteristicConfigurationDescriptor != GattClientCharacteristicConfigurationDescriptorValue.Notify))
                {
                    GattCommunicationStatus status = await characteristicButtonB.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                    if (status == GattCommunicationStatus.Unreachable) { StartDeviceConnectionWatcher(); }
                }

                currentDescriptor = await characteristicIMUOrientation.ReadClientCharacteristicConfigurationDescriptorAsync();
                if ((currentDescriptor.Status != GattCommunicationStatus.Success) || (currentDescriptor.ClientCharacteristicConfigurationDescriptor != GattClientCharacteristicConfigurationDescriptorValue.Notify))
                {
                    GattCommunicationStatus status = await characteristicIMUOrientation.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                    if (status == GattCommunicationStatus.Unreachable) { StartDeviceConnectionWatcher(); }
                }

                currentDescriptor = await characteristicIMUAcceleration.ReadClientCharacteristicConfigurationDescriptorAsync();
                if ((currentDescriptor.Status != GattCommunicationStatus.Success) || (currentDescriptor.ClientCharacteristicConfigurationDescriptor != GattClientCharacteristicConfigurationDescriptorValue.Notify))
                {
                    GattCommunicationStatus status = await characteristicIMUAcceleration.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                    if (status == GattCommunicationStatus.Unreachable) { StartDeviceConnectionWatcher(); }
                }

                currentDescriptor = await characteristicFlexi.ReadClientCharacteristicConfigurationDescriptorAsync();
                if ((currentDescriptor.Status != GattCommunicationStatus.Success) || (currentDescriptor.ClientCharacteristicConfigurationDescriptor != GattClientCharacteristicConfigurationDescriptorValue.Notify))
                {
                    GattCommunicationStatus status = await characteristicFlexi.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                    if (status == GattCommunicationStatus.Unreachable) { StartDeviceConnectionWatcher(); }
                }
            }
            catch (Exception e)
            {
                throw new Exception("ERROR: Accessing your device failed." + Environment.NewLine + e.Message);
            }
        }
        
        private void characteristicJoystickAxes_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var data = new byte[args.CharacteristicValue.Length];
            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(data);

            Tuple<Byte, Byte> axes = new Tuple<Byte, Byte>(data[0], data[1]);

            lock (joystickAxesData) { joystickAxesData.Add(axes); }
        }

        private void characteristicButtonA_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var data = new byte[args.CharacteristicValue.Length];
            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(data);

            BUTTON_DATA value = (data[0] == 1) ? BUTTON_DATA.BUTTONA_PRESSED : BUTTON_DATA.BUTTONA_RELEASED;

            lock (buttonAData) { buttonAData.Add(value); }
        }

        private void characteristicButtonB_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var data = new byte[args.CharacteristicValue.Length];
            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(data);

            BUTTON_DATA value = (data[0] == 1) ? BUTTON_DATA.BUTTONB_PRESSED : BUTTON_DATA.BUTTONB_RELEASED;

            lock (buttonBData) { buttonBData.Add(value); }
        }

        private void characteristicIMUOrientation_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var data = new byte[args.CharacteristicValue.Length];
            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(data);

            for (int i = 0; i < 16; i++) {
                lock (getInstance().IMUOrientationData)
                {
                    getInstance().IMUOrientationData.Add(data[i]);
                }
            }
        }

        private void characteristicIMUAcceleration_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var data = new byte[args.CharacteristicValue.Length];
            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(data);

            for (int i = 0; i < 12; i++)
            {
                lock (getInstance().IMUAccelerationData)
                {
                    getInstance().IMUAccelerationData.Add(data[i]);
                }
            }
        }
 
        private void characteristicFlexi_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var data = new byte[args.CharacteristicValue.Length];
            DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(data);

            byte flexiValue = data[0];

            lock (flexiData) { flexiData.Add(flexiValue); }
        }

        private void StartDeviceConnectionWatcher()
        {
            watcher = PnpObject.CreateWatcher(PnpObjectType.DeviceContainer, new string[] { "System.Devices.Connected" }, String.Empty);
            watcher.Updated += DeviceConnection_Updated;
            watcher.Start();
        }

        private async void DeviceConnection_Updated(PnpObjectWatcher sender, PnpObjectUpdate args)
        {
            var connectedProperty = args.Properties["System.Devices.Connected"];
            bool isConnected = false;
            Boolean.TryParse(connectedProperty.ToString(), out isConnected);

            if((deviceContainerId == args.Id) && isConnected)
            {
                var status = await characteristicButtonA.WriteClientCharacteristicConfigurationDescriptorAsync
                (
                     GattClientCharacteristicConfigurationDescriptorValue.Notify
                );

                if(status == GattCommunicationStatus.Success
                )
                {
                    IsServiceInitialized = true;

                    // Once the Client Characteristic Configuration Descriptor is set, the watcher is no longer required
                    watcher.Stop();
                    watcher = null;
                }

                // Notifying subscribers of connection state updates
                if (DeviceConnectionUpdated != null)
                {
                    DeviceConnectionUpdated(isConnected);
                }
            }
        }

        private static HCIHapticsBLEConnector instance = new HCIHapticsBLEConnector();
        private GattDeviceService service;

        private List<BUTTON_DATA> buttonAData;
        private List<BUTTON_DATA> buttonBData;
        private List<Tuple<byte,byte>> joystickAxesData;
        private List<byte> IMUOrientationData;
        private List<byte> IMUAccelerationData;
        private List<byte> flexiData;

        private static PnpObjectWatcher watcher;
        private String deviceContainerId;

        private GattCharacteristic characteristicJoystickAxes;
        private GattCharacteristic characteristicButtonA;
        private GattCharacteristic characteristicButtonB;
        private GattCharacteristic characteristicHapticRTV = null;
        private GattCharacteristic characteristicHapticEffect = null;
        private GattCharacteristic characteristicLED = null;
        private GattCharacteristic characteristicIMUOrientationStatus = null;
        private GattCharacteristic characteristicIMUAccelerationStatus = null;
        private GattCharacteristic characteristicIMUOrientation;
        private GattCharacteristic characteristicIMUAcceleration;
        private GattCharacteristic characteristicFlexi;

        private DataWriter writerHapticRTV             = new DataWriter();
        private DataWriter writerHapticEffect          = new DataWriter();
        private DataWriter writerLED                   = new DataWriter();
        private DataWriter writerIMUOrientationStatus  = new DataWriter();
        private DataWriter writerIMUAccelerationStatus = new DataWriter();

        public event DeviceConnectionUpdatedHandler DeviceConnectionUpdated;
    }
}
