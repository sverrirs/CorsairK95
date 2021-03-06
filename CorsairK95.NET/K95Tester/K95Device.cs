﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MadWizard.WinUSBNet;

namespace K95Tester
{
    /// <summary>
    /// The light intensity for the LED lights
    /// </summary>
    public enum LedBrightness
    {
        Off = 0x0000, 
        Low = 0x0001,
        Medium = 0x0002,
        High = 0x0003
    }

    public class K95Device : IDisposable
    {
        /// <summary>
        /// This is the default device GUID that is installed with the supplied custom driver. If you change this value in the driver .inf file then 
        /// this must be overwritten as well through the constructor.
        /// </summary>
        private readonly string _deviceInterfaceGuid = @"{0F42485A-C797-4B13-813B-3E3EC5691526}";

        /// <summary>
        /// The actual WinUSB connected device, if connected and working properly this will be a non-null value
        /// </summary>
        private USBDevice _device = null;

        /// <summary>
        /// The background thread that is monitoring the asynchonous input pipe on the USB device
        /// This is just a simple BackgroundWorker for now.
        /// </summary>
        private readonly BackgroundWorker _bgWorker = new BackgroundWorker();

        /// <summary>
        /// Boolean used to terminate the background thread and processing on the USB port, the device will still be connected though
        /// </summary>
        private bool _terminate = false;

        public K95Device()
        {
            _bgWorker.WorkerReportsProgress = true;
            _bgWorker.DoWork += BgWorkerOnDoWork;
            _bgWorker.RunWorkerCompleted += BgWorkerOnRunWorkerCompleted;
            _bgWorker.ProgressChanged += BgWorkerOnProgressChanged;
        }

        private static void PrintInfo(USBDevice device)
        {
            Console.WriteLine("Device: {0}", device.Descriptor.FullName);
            Console.WriteLine("Pipes: {0}", device.Pipes?.Count());
            Console.WriteLine("Control Pipe Timeout: {0}", device.ControlPipeTimeout);

            foreach (USBInterface iface in device.Interfaces)
            {
                Console.WriteLine("Interface - id:{0}, protocol:{1}, class:{2}, subclass:{3}",
                  iface.Number, iface.Protocol, iface.ClassValue, iface.SubClass);
                
                Console.WriteLine("Pipes: {0}", iface.Pipes?.Count());
                if (iface.Pipes != null)
                {
                    foreach (USBPipe pipe in iface.Pipes)
                    {
                        Console.WriteLine("Pipe - Address:{0}, isIn:{1}, isOut:{2}, maxPacketSize:{3}", pipe.Address, pipe.IsIn, pipe.IsOut, pipe.MaximumPacketSize);
                    }
                }
            }
        }

        public void Connect()
        {
            if (_terminate)
                throw new InvalidOperationException("Corsair K95 has already been terminated.");

            if (_device != null)
                throw new InvalidOperationException("Corsair K95 is already connected.");
                
            try
            {
                USBDeviceInfo[] details = USBDevice.GetDevices(_deviceInterfaceGuid);
                /*foreach (var deviceDetails in details)
                {
                    using (USBDevice device = new USBDevice(deviceDetails))
                    {
                        PrintInfo(device);
                    }
                }*/

                _device = new USBDevice(details[0]);
                _bgWorker.RunWorkerAsync(_device);

                Thread.Sleep(1000);
            }
            catch (Exception e)
            {
                Trace.WriteLine("K95: Connect Exception: " + e.ToString());
                _device = null;
                throw;
            }
        }

        /// <summary>
        /// Prints out detailed device information for all K95 devices connected.
        /// </summary>
        public void PrintDevices()
        {
            var devices = USBDevice.GetDevices(_deviceInterfaceGuid);
            if (devices.Length <= 0)
            {
                Console.WriteLine("No devices found for GUID: "+_deviceInterfaceGuid );
                return;
            }

            foreach (var deviceDetails in devices)
            {
                using (USBDevice device = new USBDevice(deviceDetails))
                {
                    PrintInfo(device);
                }
            }
        }

        /// <summary>
        /// Sends a raw Control Out message to the keyboard with an optional byte buffer payload.
        /// See more info at: http://www.beyondlogic.org/usbnutshell/usb6.shtml
        /// </summary>
        /// <param name="requestType">The bmRequestType of request to make (x40 or x21).
        /// The bmRequestType field will determine the direction of the request, type of request and designated recipient. 
        /// The bmRequestType is normally parsed and execution is branched to a number of handlers such as a Standard Device request handler, 
        /// a Standard Interface request handler, a Standard Endpoint request handler, a Class Device request handler etc.</param>
        /// <param name="request">The function request to execute on the board</param>
        /// <param name="value">The value to set</param>
        /// <param name="index">Optional, the control index to send this to (there are usually four). Default it zero.</param>
        /// <param name="buffer">optional, the data payload to send to the keyboard along with the message</param>
        public void Send(byte requestType, byte request, int value, int index = 0, byte[] buffer = null )
        {
            _device.ControlOut(requestType, request, value, index, buffer ?? new byte[0]);
        }

        /// <summary>
        /// Controls the backlight brightness of the entire keyboard.
        /// </summary>
        /// <param name="brightness">The brightness level requested</param>
        public void SetLedBrightness( LedBrightness brightness )
        {
            Send(0x40, 0x31, (int)brightness, 0);
        }

        public void Disconnect()
        {
            if (_device == null || _terminate)
                return;

            // Indicate that we want to terminate
            _terminate = true;

            // Abort any waiting tasks on the main input pipe
            try
            {
                _device?.Interfaces.FirstOrDefault()?.InPipe.Abort();
            }
            catch (USBException e)
            {
                // This exception happens if the USB device has been unplugged 
                // before disconnecting, we can safely ignore it
                //USBException: Failed to abort pipe. -
            }

            // TODO: Perhaps we should wait until termination has been confirmed?
        }

        public void Dispose()
        {
            Disconnect();
        }

        private void BgWorkerOnDoWork(object sender, DoWorkEventArgs e)
        {
            var device = e.Argument as USBDevice;

            // TODO: for now just get the first pipe, this will always be the subclass=93 interface that is the one that we want
            var dInterface = device?.Interfaces.FirstOrDefault();

            if (dInterface?.InPipe == null)
                return;

            // Only 5 bytes are ever transferred from the device
            var inbuffer = new byte[dInterface.InPipe.MaximumPacketSize];
            var prevbuffer = new byte[dInterface.InPipe.MaximumPacketSize];

            while (true)
            {
                // If we have been instructed to terminate the USB connection end the background thread 
                // and move into cleanup.
                if (_terminate)
                    return;

                int bytesRead = 0;
                try
                {
                    bytesRead = dInterface.InPipe.Read(inbuffer, 0, inbuffer.Length);
                }
                catch (USBException uex)
                {
                    // This exception is thrown when we terminate the Pipe when disposing of the connection
                    // if the error string contains this value then silently ignore the error and move to terminate the app
                    if (!string.IsNullOrEmpty(uex.Message) && !uex.Message.Contains("Failed to read from pipe."))
                        throw;
                }

                if (bytesRead > 0)
                {
                    Console.WriteLine("");
                    for (int i = 0; i < inbuffer.Length; i++)
                    {
                        var inbyte = inbuffer[i];
                        if( inbyte == 0 )
                            continue;

                        Console.WriteLine("{0}: {1:X}", i, inbyte);
                    }

                    inbuffer.CopyTo(prevbuffer, 0);
                }
                /*else
                {
                    Console.WriteLine("Nothing read from pipe");
                }*/
            }
        }

        private void BgWorkerOnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Nothing for now
        }

        private void BgWorkerOnRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Cleanup the USB device connection and free all resources
            if (_device != null)
            {
                _device.Dispose();
                _device = null;
            }
        }
    }
}
