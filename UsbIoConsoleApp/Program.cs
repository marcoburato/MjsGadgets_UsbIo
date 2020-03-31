using System;
using System.Text;

namespace MjsGadgets
{
    class Program
    {
        private static string _serialNumber;


        public static void Main(string[] args)
        {
            int i = 0;
            if (ParseStr(args, 0) == "--serial-number")
            {
                _serialNumber = ParseStr(args, 1);

                i = 2;
            }
            
            try
            {
                switch (ParseStr(args, i))
                {
                    case "list-devices":
                        ListDevices();
                        break;
                    case "get-all-inputs":
                        GetAllInputs();
                        break;
                    case "get-all-relays":
                        GetAllRelays();
                        break;
                    case "get-input":
                        GetInput(ParseInt(args, i + 1, 1, UsbIo.NumInputs));
                        break;
                    case "get-relay":
                        GetRelay(ParseInt(args, i + 1, 1, UsbIo.NumRelays));
                        break;
                    case "set-all-relays":
                        SetAllRelays(ParseBools(args, i + 1, UsbIo.NumRelays));
                        break;
                    case "set-relay":
                        SetRelay(ParseInt(args, i + 1, 1, UsbIo.NumRelays), ParseBool(args, i + 2));
                        break;
                    default:
                        PrintUsageAndAbort();
                        break;
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine("Error: " + exc.Message);
                Environment.Exit(1);
            }
        }

        private static string ParseStr(string[] args, int index)
        {
            if (index >= args.Length)
                PrintUsageAndAbort();

            return args[index];
        }

        private static int ParseInt(string[] args, int index, int min, int max)
        {
            if (index >= args.Length)
                PrintUsageAndAbort();

            if (int.TryParse(args[index], out int value) == false)
                PrintUsageAndAbort();

            if (value < min || value > max)
                PrintUsageAndAbort();

            return value;
        }

        private static bool ParseBool(string[] args, int index)
        {
            int i = ParseInt(args, index, 0, 1);
            return (i != 0);
        }

        private static bool[] ParseBools(string[] args, int index, int count)
        {
            string s = ParseStr(args, index);
            if (s.Length != count)
                PrintUsageAndAbort();

            bool[] result = new bool[count];
            for (int i = 0; i < count; i++)
            {
                switch (s[i])
                {
                    case '0':
                        result[i] = false;
                        break;
                    case '1':
                        result[i] = true;
                        break;
                    default:
                        PrintUsageAndAbort();
                        break;
                }
            }

            return result;
        }

        private static void PrintUsageAndAbort()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  UsbIoCtrl [--serial-number <serialnumber>] list-devices");
            Console.WriteLine("  UsbIoCtrl [--serial-number <serialnumber>] get-all-inputs");
            Console.WriteLine("  UsbIoCtrl [--serial-number <serialnumber>] get-all-relays");
            Console.WriteLine("  UsbIoCtrl [--serial-number <serialnumber>] get-input <num>");
            Console.WriteLine("  UsbIoCtrl [--serial-number <serialnumber>] get-relay <num>");
            Console.WriteLine("  UsbIoCtrl [--serial-number <serialnumber>] set-all-relays <states>");
            Console.WriteLine("  UsbIoCtrl [--serial-number <serialnumber>] set-relay <num> <state>");
            Console.WriteLine("where:");
            Console.WriteLine("  <serialnumber> is the target device serial number. If not provided, the first device found will be used.");
            Console.WriteLine("  <num> is the input of relay number 1..4");
            Console.WriteLine("  <state> is 0 (OFF) or 1 (ON)");
            Console.WriteLine("  <states> sequence of <state>, one for each relay (4)");
            Environment.Exit(1);
        }

        private static void ListDevices()
        {
            var serialNumbers = UsbIo.GetDevices();

            foreach (var serialNumber in serialNumbers)
                Console.WriteLine("Device: " + serialNumber);
        }

        private static void GetAllInputs()
        {
            using (var device = Open())
            {
                device.GetCurrentStates(out int inputsMask, out _);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < UsbIo.NumInputs; i++)
                {
                    bool state = (inputsMask & (1 << i)) != 0;
                    sb.Append(state ? '1' : '0');
                }
                Console.WriteLine(sb.ToString());
            }
        }

        private static void GetAllRelays()
        {
            using (var device = Open())
            {
                device.GetCurrentStates(out _, out int relaysMask);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < UsbIo.NumRelays; i++)
                {
                    bool state = (relaysMask & (1 << i)) != 0;
                    sb.Append(state ? '1' : '0');
                }
                Console.WriteLine(sb.ToString());
            }
        }

        private static void GetInput(int num)
        {
            CheckInputNum(num);

            using (var device = Open())
            {
                bool state = device.GetInput(num);

                Console.WriteLine(state ? '1' : '0');
            }
        }

        private static void GetRelay(int num)
        {
            CheckRelayNum(num);

            using (var device = Open())
            {
                bool state = device.GetRelay(num);

                Console.WriteLine(state ? '1' : '0');
            }
        }

        private static void SetAllRelays(bool[] states)
        {
            int mask = 0;
            for (int i = 0; i < UsbIo.NumRelays; i++)
                if (states[i])
                    mask |= (1 << i);

            using (var device = Open())
            {
                device.SetRelays(mask);
            }
        }

        private static void SetRelay(int num, bool state)
        {
            CheckRelayNum(num);

            using (var device = Open())
            {
                device.SetRelay(num, state);
            }
        }

        private static void CheckInputNum(int num)
        {
            if (num < 1 || num > UsbIo.NumInputs)
            {
                Console.WriteLine("Error: invalid input number, must be 1-4");
                Environment.Exit(1);
            }
        }

        private static void CheckRelayNum(int num)
        {
            if (num < 1 || num > UsbIo.NumRelays)
            {
                Console.WriteLine("Error: invalid relay number, must be 1-4");
                Environment.Exit(1);
            }
        }
        
        private static UsbIo Open()
        {
            var serialNumbers = UsbIo.GetDevices();

            var serialNumberToOpen = _serialNumber;

            if (serialNumberToOpen == null)
            {
                if (serialNumbers.Length == 0)
                {
                    Console.WriteLine("No device found");
                    Environment.Exit(1);
                }

                serialNumberToOpen = serialNumbers[0];

                if (serialNumbers.Length > 1)
                    Console.WriteLine($"Warning: multiple devices found, picked first: {serialNumberToOpen}");
            }

            var device = new UsbIo(serialNumberToOpen);

            return device;
        }
    }
}
