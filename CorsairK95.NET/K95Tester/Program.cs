using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace K95Tester
{
    class Program
    {
        static ManualResetEvent _terminateSignal = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            K95Device usb = new K95Device();
            usb.Connect();

            Console.WriteLine("Waiting on main thread");

            _terminateSignal.WaitOne(Timeout.Infinite);

            Console.WriteLine("Resuming main thread and exiting");
        }
    }
}
