using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HidLibrary;

namespace MjsGadgets
{
    public class UsbIo : IDisposable
    {
        private const int Vid = 0x0B40;
        private const int Pid = 0x012D;

        private HidDevice _dev;

        public const int NumInputs = 4;
        public const int NumRelays = 4;


        public UsbIo(string serialNumber)
        {
            _dev = GetDeviceBySerialNumber(serialNumber) ?? throw new IOException($"Device with serial number {serialNumber} not found");
        }

        public void GetCurrentStates(out int inputsMask, out int relaysMask)
        {
            byte[] data = new byte[64];
            data[0] = 0x00;
            data[1] = 0x9C;
            data[2] = 0x03;
            data[3] = 0x00;
            data[4] = 0xF0;
            if (_dev.Write(data) == false)
                throw new IOException("HID Write failed");

            var response = _dev.Read();
            if (response.Status != HidDeviceData.ReadStatus.Success)
                throw new IOException("HID Read failed");

            byte mask = response.Data[3];
            inputsMask = (byte)((mask >> 4) ^ 0x0F);
            relaysMask = (byte)(mask & 0x0F);
        }

        public bool GetInput(int num)
        {
            if (num < 1 || num > NumInputs)
                throw new ArgumentOutOfRangeException(nameof(num), $"Input number must be between 1 and {NumInputs}");

            GetCurrentStates(out int inputsMask, out _);
            return (inputsMask & (1 << (num - 1))) != 0;
        }

        public bool GetRelay(int num)
        {
            if (num < 1 || num > NumRelays)
                throw new ArgumentOutOfRangeException(nameof(num), $"Relay number must be between 1 and {NumRelays}");

            GetCurrentStates(out _, out int relaysMask);
            return (relaysMask & (1 << (num - 1))) != 0;
        }

        public void SetRelay(int num, bool state)
        {
            if (num < 1 || num > NumRelays)
                throw new ArgumentOutOfRangeException(nameof(num), $"Relay number must be between 1 and {NumRelays}");

            byte[] data = new byte[64];
            data[0] = 0x00;
            data[1] = 0x9F;
            data[2] = 0x03;
            data[3] = (byte)(num - 1);
            data[4] = (byte)(state ? 1 : 0);
            if (_dev.Write(data) == false)
                throw new IOException("HID Write failed");
            _dev.Read();
        }

        public void SetRelays(int mask)
        {
            if (mask > 0x0F)
                throw new ArgumentOutOfRangeException(nameof(mask), $"Only {NumRelays} bits expected");

            byte[] data = new byte[64];
            data[0] = 0x00;
            data[1] = 0x9D;
            data[2] = 0x03;
            data[3] = (byte)mask;
            data[4] = 0;
            if (_dev.Write(data) == false)
                throw new IOException("Write failed");
            _dev.Read();
        }

        public static string[] GetDevices()
        {
            var list = new List<string>();

            var devices = HidDevices.Enumerate(Vid, Pid);
            foreach (var device in devices)
                if (device.ReadSerialNumber(out byte[] data))
                    list.Add(DecodeSerialNumberData(data));

            return list.ToArray();
        }

        static private HidDevice GetDeviceBySerialNumber(string serialNumber)
        {
            var devices = HidDevices.Enumerate(Vid, Pid);
            foreach (var device in devices)
                if (device.ReadSerialNumber(out byte[] data) && DecodeSerialNumberData(data) == serialNumber)
                    return device;
            return null;
        }

        static private string DecodeSerialNumberData(byte[] data)
        {
            var s = Encoding.Unicode.GetString(data);
            var terminatorIndex = s.IndexOf('\0');
            if (terminatorIndex >= 0)
                return s.Substring(0, terminatorIndex);
            else
                return s;
        }

        #region IDisposable Support
        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    _dev.Dispose();
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
