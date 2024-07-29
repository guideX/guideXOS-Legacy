using System;
using System.Windows.Forms;
using static System.ConsoleKey;
using guideXOS.Misc;
namespace guideXOS.Kernel.Drivers {
    /// <summary>
    /// Human Interface Device
    /// Human Interface Devices (HID) is a device class definition to replace PS/2-style connectors with a generic USB driver to support HID devices such as keyboards, mice, game controllers, and so on. Prior to HID, devices could only utilize strictly defined protocols for mice and keyboards. Hardware innovation required either overloading data in an existing protocol or creating nonstandard hardware with its own specialized driver. HID provides support for boot mode devices while adding support for innovation through extensible, standardized, and easily programmable interfaces.
    /// HID devices include alphanumeric displays, bar code readers, speakers, headsets, auxiliary displays, sensors, and many others.Hardware vendors also use HID for their proprietary devices.
    /// HID began with USB but was designed to be bus-agnostic.It was designed for low latency, low bandwidth devices but with flexibility to specify the rate in the underlying transport.The USB-IF ratified the specification for HID over USB in 1996. Support for HID over other transports soon followed. Details on currently supported transports can be found in HID Transports Supported in Windows.Third-party, vendor-specific transports are also allowed via custom transport drivers.
    /// </summary>
    public static unsafe class HID {
        #region "public static variables"
        /// <summary>
        /// USB Request
        /// </summary>
        static USBRequest* _usbRequest;
        /// <summary>
        /// Console Keys
        /// </summary>
        public static ConsoleKey[] ConsoleKeys;
        /// <summary>
        /// Mouse
        /// </summary>
        public static USBDevice Mouse;
        /// <summary>
        /// Keyboard
        /// </summary>
        public static USBDevice Keyboard;
        #endregion
        /// <summary>
        /// Initialize
        /// </summary>
        public static void Initialize() {
            Mouse = null;
            Keyboard = null;
            _usbRequest = (USBRequest*)Allocator.Allocate((ulong)sizeof(USBRequest));
        }

        /// <summary>
        /// Get HID Packet
        /// </summary>
        /// <param name="device"></param>
        /// <param name="devicedesc"></param>
        /// <returns></returns>
        public static bool GetHIDPacket(USBDevice device, uint devicedesc) {
            (*_usbRequest).Clean();
            _usbRequest->Request = 1;
            _usbRequest->RequestType = 0xA1;
            _usbRequest->Index = 0;
            _usbRequest->Length = 3;
            _usbRequest->Value = 0x0100;
            bool res = USB.SendAndReceive(device, _usbRequest, (void*)devicedesc, device.Parent);
            return res;
        }
        /// <summary>
        /// Get Keyboard
        /// </summary>
        /// <param name="device"></param>
        /// <param name="ScanCode"></param>
        /// <param name="Key"></param>
        public static void GetKeyboard(USBDevice device, out byte ScanCode, out ConsoleKey Key) {
            Key = None;
            ScanCode = 0;
            byte* desc = stackalloc byte[10];
            bool res = GetHIDPacket(device, (uint)desc);
            if (res) {
                if (desc[2] != 0) {
                    ScanCode = desc[2];
                    if (ScanCode < ConsoleKeys.Length) Key = ConsoleKeys[ScanCode];
                }
            }
        }
        /// <summary>
        /// Get Mouse
        /// </summary>
        /// <param name="device"></param>
        /// <param name="AxisX"></param>
        /// <param name="AxisY"></param>
        /// <param name="buttons"></param>
        public static void GetMouse(USBDevice device, out sbyte AxisX, out sbyte AxisY, out MouseButtons buttons) {
            AxisX = 0;
            AxisY = 0;
            buttons = MouseButtons.None;
            byte* desc = stackalloc byte[10];
            bool res = GetHIDPacket(device, (uint)desc);
            if (res) {
                AxisX = (sbyte)desc[1];
                AxisY = (sbyte)desc[2];
                if (desc[0] & 0x01) buttons |= MouseButtons.Left;
                if (desc[0] & 0x02) buttons |= MouseButtons.Right;
                if (desc[0] & 0x04) buttons |= MouseButtons.Middle;
            }
        }
        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="device"></param>
        public static void Initialize(USBDevice device) {
            if (device.Protocol == 1) {
                USB.NumDevice++;
                //Console.WriteLine($"[HID] USB Keyboard at port:{device.Address}");
                InitializeKeyboard(device);
            } else if (device.Protocol == 2) {
                USB.NumDevice++;
                //Console.WriteLine($"[HID] USB Mouse at port:{device.Address}");
                InitializeMouse(device);
            }
        }
        /// <summary>
        /// Initialize Mouse
        /// </summary>
        /// <param name="device"></param>
        static void InitializeMouse(USBDevice device) {
            Mouse = device;
        }
        /// <summary>
        /// Initialize Keyboard
        /// </summary>
        /// <param name="device"></param>
        static void InitializeKeyboard(USBDevice device) {
            Keyboard = device;
            ConsoleKeys = new ConsoleKey[] {
                None,
                None,
                None,
                None,
                A,
                B,
                C,
                D,
                E,
                F,
                G,
                H,
                I,
                J,
                K,
                L,
                M,
                N,
                O,
                P,
                Q,
                R,
                S,
                T,
                U,
                V,
                W,
                X,
                Y,
                Z,
                D1,
                D2,
                D3,
                D4,
                D5,
                D6,
                D7,
                D8,
                D9,
                D0,
                Enter,
                Escape,
                Backspace,
                Tab,
                Space,
            };
        }
    }
}