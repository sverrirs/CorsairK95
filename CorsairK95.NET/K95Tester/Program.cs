using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace K95Tester
{
    internal class Program
    {
        private static ManualResetEvent _terminateSignal = new ManualResetEvent(false);

        private static void Main(string[] args)
        {
            Console.WriteLine("Waiting on main thread");

            Experiment();

            //Test();

            //StartDiscovery();

            _terminateSignal.WaitOne(Timeout.Infinite);

            Console.WriteLine("Resuming main thread and exiting");
        }

        /// <summary>
        /// Runs a simple connection experiment and sending led control messages
        /// </summary>
        public static void Experiment()
        {
            K95Device usb = new K95Device();
            try
            {
                usb.Connect();

                LedBrightness[] ledbrightness = {LedBrightness.Off,
                                                 LedBrightness.Low, LedBrightness.Medium, LedBrightness.High,
                                                 LedBrightness.Medium, LedBrightness.Low };

                // Now cycle through the brightness intensities for the keyboard
                for ( int i = 0; i < 10000; i++)
                {
                    usb.SetLedBrightness(ledbrightness[i % ledbrightness.Length]);

                    // Short wait to let the hardware get ready again and the user to notice the change
                    Thread.Sleep(150);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: "+ex.Message);
                Console.WriteLine(ex.ToString());

                usb.Disconnect();
            }
        }

        public static void Test()
        {
            /*int i = 0;
                int[] ledbrightness = new[] {0x0000, 0x0001, 0x0002, 0x0003, 0x0002, 0x0001};
                while (i++ < 10000)
                {
                    _device.ControlOut(0x40, 0x31, ledbrightness[i % ledbrightness.Length], 0);
                    Thread.Sleep(150);
                }*/

            //_device.ControlOut(0x21, 0x09, 0x0200, 0, new byte[] { 0x01 | 0x02 | 0x04 });
            //_device.ControlOut(0x21, 0x09, 0x0200, 0, new byte[] { 0x01 | 0x02 });

            /*try { _device.ControlOut(0x21, 0x09, 0x02, 0, new byte[] { 0x01 }); }
            catch {}*/
            // _device.ControlOut(0x21, 0x09, 0x200, 0, new byte[] { 0x00 });

            /* _device.ControlOut(0x40, 0x31, 0x0003, 0, new byte[]
             {
                 0x12, 0x00, 0x5f,
                 0x00, 0x02,
                 0x0a, 0x01
             });*/


            /*_device.ControlOut(0x40, 0x02, 0x0040, 0);
            _device.ControlOut(0x40, 0x02, 0x0040, 0);
            _device.ControlOut(0x40, 0x02, 0x0030, 0);

            _device.ControlOut(0x40, 0x02, 0x0001, 0);*/


            /*_device.ControlOut(0x21, 0x09, 0x0300, 0x03, new byte[]
             { 0x7F,   0x01,   0x3C,   0x00,   0x00,   0x00,   0x00,   0x00,
0x07,   0x77,   0x00,   0x00,   0x07,   0x70,   0x00,   0x77,
0x00,   0x00,   0x00,   0x00,   0x00,   0x77,   0x00,   0x00,
0x70,   0x00,   0x70,   0x77,   0x00,   0x00,   0x00,   0x00,
0x00,   0x77,   0x00,   0x00,   0x70,   0x00,   0x00,   0x77,
0x00,   0x00,   0x70,   0x00,   0x00,   0x77,   0x00,   0x00,
0x00,   0x00,   0x00,   0x77,   0x00,   0x00,   0x00,   0x00,
0x00,   0x77,   0x00,   0x00,   0x00,   0x07,   0x00,   0x77 }
             );*/

            /*_device.ControlOut(
                0x40,
                0x16,
                0x0000, 
                1, 
                new byte[]
                {
                    0x12, 0xd0, 0x00, 0xd1, 0x00, 0xd2, 0x00, 0xd3, 0x00, 0xd4, 0x00, 0xd5, 0x00, 0xd6, 0x00, 0xd7, 0x00, 0xd8, 0x00, 0xd9, 0x00, 0xda, 0x00, 0xdb, 0x00, 0xdc, 0x00, 0xdd, 0x00, 0xde, 0x00, 0xdf, 0x00, 0xe8, 0x00, 0xe9, 0x00
                });*/


            // Set keys to software mode
            // 0x07, 0x04, 0x02

            // Set keys into hardware mode
            // 0x07, 0x04, 0x01  

            //_device.ControlOut(0x21, 0x09, 0x0300, 0x03, new byte []{ 0x07, 0x04, 0x01 });
        }

        /// <summary>
        /// Brute-force sends bytes to the keyboard in an attempt to figure out what control sequences are supported
        /// NOTE: This has the potential to actually brick your entire keyboard! 
        /// </summary>
        public static void StartDiscovery()
        {
            using (var output = new StreamWriter("output-0x21.txt"))
            {
                output.WriteLine("Start"); output.Flush();

                K95Device usb = new K95Device();
                usb.Connect();

                for (int request = 0; request < 256; request++)
                {
                    for (int value = 0; value < 3840; value++)
                    {
                        //for (int key = 0; key < 256; key++)
                        {
                            /*if (usb == null)
                        {
                            usb = new K95Device();
                            usb.Connect();
                        }*/

                            try
                            {
                                //usb.Send(0x21, Convert.ToByte(request), value, 0, new[] { (byte)key });
                                //Console.WriteLine("0x{0:X}, 0x{1:X}, 0x{2:X}", request, value, key);
                                // request == 20
                                usb.Send(0x21, Convert.ToByte(request), value, 0);
                                output.WriteLine("0x21, 0x{0:X}, 0x{1:X}", request, value); output.Flush();
                                Thread.Sleep(250);
                            }
                            catch (Exception)
                            {
                                /*try { usb?.Disconnect(); } catch (Exception) {}
                            usb = null;*/
                            }
                        }
                    }
                }

                output.WriteLine("Finished"); output.Flush();
            }
        }
    }
}