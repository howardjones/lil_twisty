using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;


namespace LilTwisty
{

    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            uint display_id = 9;                       
            
            RotateDisplay.go(display_id);

            listBox1.Items.Clear();
            update_listbox();
        }

        private void update_listbox()
        {
            DISPLAY_DEVICE d = new DISPLAY_DEVICE();
            DEVMODE dm = new DEVMODE();
            d.cb = Marshal.SizeOf(d);

            List<Rectangle> displays = new List<Rectangle>();
            List<Rectangle> scaled_displays = new List<Rectangle>();
            Rectangle bounding = new Rectangle(0, 0, 0, 0);

            for (uint i = 0; i < 100; i++)
            {
                bool result = NativeMethods.EnumDisplayDevices(null, i, ref d, 0);
                if (!result)
                {
                    break;
                }

                string label = "";

                int res = NativeMethods.EnumDisplaySettings(
                d.DeviceName, NativeMethods.ENUM_CURRENT_SETTINGS, ref dm);

                if (res != 0)
                {
                    POINTL pos = dm.dmPosition;

                    Rectangle display_bounding = new Rectangle(pos.x, pos.y, dm.dmPelsWidth, dm.dmPelsHeight);
                    displays.Add(display_bounding);

                    label = string.Format("[{0}] {3} <{4}> {1} {2}", i, d.DeviceString, d.DeviceName, display_bounding, dm.dmDisplayOrientation);
                    listBox1.Items.Add(label);

                }
            }

            int[] bounding_parts = new int[] { 0, 0, 0, 0 };

            foreach (Rectangle r in displays)
            {
                bounding_parts[0] = Math.Min(r.Left, bounding_parts[0]);
                bounding_parts[1] = Math.Min(r.Top, bounding_parts[1]);
                bounding_parts[2] = Math.Max(r.Left + r.Width, bounding_parts[2]);
                bounding_parts[3] = Math.Max(r.Top + r.Height, bounding_parts[3]);
            }

            bounding = new Rectangle(bounding_parts[0], bounding_parts[1], bounding_parts[2] - bounding_parts[0], bounding_parts[3] - bounding_parts[1]);
            float scale_h = (float)bounding.Width / (float)panel1.Width;
            float scale_v = (float)bounding.Height / (float)panel1.Height;
            float scalefactor = Math.Max(scale_h, scale_v);

            Console.WriteLine(bounding);
            Console.WriteLine("{0} {1} => {2}", scale_h, scale_v, scalefactor);

            foreach (Rectangle r in displays)
            {
                Rectangle new_r = new Rectangle(
                        panel1.Left + (int)((float)r.Left / scalefactor),
                        panel1.Top + (int)((float)r.Top / scalefactor),
                        (int)((float)r.Width / scalefactor),
                        (int)((float)r.Height / scalefactor)
                    );
                Console.WriteLine(new_r);
                scaled_displays.Add(new_r);
            }
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            update_listbox();
                        


        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void notifyIcon1_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }
    }

    class RotateDisplay
    {
        public static void go(uint deviceID)
        {
            // uint deviceID = 1; // zero origin (i.e. 1 means DISPLAY2)

            DISPLAY_DEVICE d = new DISPLAY_DEVICE();
            DEVMODE dm = new DEVMODE();
            d.cb = Marshal.SizeOf(d);

            Console.WriteLine("Finding displays");
            NativeMethods.EnumDisplayDevices(null, deviceID, ref d, 0);

            Console.WriteLine(d);
            Console.WriteLine(d.DeviceString);

            int res = NativeMethods.EnumDisplaySettings(
                d.DeviceName, NativeMethods.ENUM_CURRENT_SETTINGS, ref dm);

            Console.WriteLine(res);

            if (0 != res)
            {
                int temp = dm.dmPelsHeight;
                dm.dmPelsHeight = dm.dmPelsWidth;
                dm.dmPelsWidth = temp;

                Console.WriteLine(dm.dmPelsHeight);
                Console.WriteLine(dm.dmPelsWidth);
                Console.WriteLine(dm.dmDisplayOrientation);

                switch (dm.dmDisplayOrientation)
                {
                    case NativeMethods.DMDO_DEFAULT:
                        dm.dmDisplayOrientation = NativeMethods.DMDO_90;
                        break;
                    case NativeMethods.DMDO_90:
                        dm.dmDisplayOrientation = NativeMethods.DMDO_DEFAULT;
                        break;
                    case NativeMethods.DMDO_180:
                        dm.dmDisplayOrientation = NativeMethods.DMDO_270;
                        break;
                    case NativeMethods.DMDO_270:
                        dm.dmDisplayOrientation = NativeMethods.DMDO_180;
                        break;
                    default:
                        break;
                }

                DISP_CHANGE iRet = NativeMethods.ChangeDisplaySettingsEx(
                    d.DeviceName, ref dm, IntPtr.Zero,
                    DisplaySettingsFlags.CDS_UPDATEREGISTRY, IntPtr.Zero);
                // if (iRet != DISP_CHANGE.Successful) handle error
            }
        }
    }

    internal class NativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern DISP_CHANGE ChangeDisplaySettingsEx(
            string lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd,
            DisplaySettingsFlags dwflags, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern bool EnumDisplayDevices(
            string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice,
            uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        internal static extern int EnumDisplaySettings(
            string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

        public const int DMDO_DEFAULT = 0;
        public const int DMDO_90 = 1;
        public const int DMDO_180 = 2;
        public const int DMDO_270 = 3;

        public const int ENUM_CURRENT_SETTINGS = -1;
    }

    [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Ansi)]
    internal struct DEVMODE
    {
        public const int CCHDEVICENAME = 32;
        public const int CCHFORMNAME = 32;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        [System.Runtime.InteropServices.FieldOffset(0)]
        public string dmDeviceName;
        [System.Runtime.InteropServices.FieldOffset(32)]
        public Int16 dmSpecVersion;
        [System.Runtime.InteropServices.FieldOffset(34)]
        public Int16 dmDriverVersion;
        [System.Runtime.InteropServices.FieldOffset(36)]
        public Int16 dmSize;
        [System.Runtime.InteropServices.FieldOffset(38)]
        public Int16 dmDriverExtra;
        [System.Runtime.InteropServices.FieldOffset(40)]
        public DM dmFields;

        [System.Runtime.InteropServices.FieldOffset(44)]
        Int16 dmOrientation;
        [System.Runtime.InteropServices.FieldOffset(46)]
        Int16 dmPaperSize;
        [System.Runtime.InteropServices.FieldOffset(48)]
        Int16 dmPaperLength;
        [System.Runtime.InteropServices.FieldOffset(50)]
        Int16 dmPaperWidth;
        [System.Runtime.InteropServices.FieldOffset(52)]
        Int16 dmScale;
        [System.Runtime.InteropServices.FieldOffset(54)]
        Int16 dmCopies;
        [System.Runtime.InteropServices.FieldOffset(56)]
        Int16 dmDefaultSource;
        [System.Runtime.InteropServices.FieldOffset(58)]
        Int16 dmPrintQuality;

        [System.Runtime.InteropServices.FieldOffset(44)]
        public POINTL dmPosition;
        [System.Runtime.InteropServices.FieldOffset(52)]
        public Int32 dmDisplayOrientation;
        [System.Runtime.InteropServices.FieldOffset(56)]
        public Int32 dmDisplayFixedOutput;

        [System.Runtime.InteropServices.FieldOffset(60)]
        public short dmColor;
        [System.Runtime.InteropServices.FieldOffset(62)]
        public short dmDuplex;
        [System.Runtime.InteropServices.FieldOffset(64)]
        public short dmYResolution;
        [System.Runtime.InteropServices.FieldOffset(66)]
        public short dmTTOption;
        [System.Runtime.InteropServices.FieldOffset(68)]
        public short dmCollate;
        [System.Runtime.InteropServices.FieldOffset(72)]
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
        public string dmFormName;
        [System.Runtime.InteropServices.FieldOffset(102)]
        public Int16 dmLogPixels;
        [System.Runtime.InteropServices.FieldOffset(104)]
        public Int32 dmBitsPerPel;
        [System.Runtime.InteropServices.FieldOffset(108)]
        public Int32 dmPelsWidth;
        [System.Runtime.InteropServices.FieldOffset(112)]
        public Int32 dmPelsHeight;
        [System.Runtime.InteropServices.FieldOffset(116)]
        public Int32 dmDisplayFlags;
        [System.Runtime.InteropServices.FieldOffset(116)]
        public Int32 dmNup;
        [System.Runtime.InteropServices.FieldOffset(120)]
        public Int32 dmDisplayFrequency;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal struct DISPLAY_DEVICE
    {
        [MarshalAs(UnmanagedType.U4)]
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        [MarshalAs(UnmanagedType.U4)]
        public DisplayDeviceStateFlags StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINTL
    {
        public int x;
        public int y;
    }

    enum DISP_CHANGE : int
    {
        Successful = 0,
        Restart = 1,
        Failed = -1,
        BadMode = -2,
        NotUpdated = -3,
        BadFlags = -4,
        BadParam = -5,
        BadDualView = -6
    }

    [Flags()]
    enum DisplayDeviceStateFlags : int
    {
        AttachedToDesktop = 0x1,
        MultiDriver = 0x2,

        PrimaryDevice = 0x4,

        MirroringDriver = 0x8,

        VGACompatible = 0x16,

        Removable = 0x20,

        ModesPruned = 0x8000000,
        Remote = 0x4000000,
        Disconnect = 0x2000000
    }

    [Flags()]
    enum DisplaySettingsFlags : int
    {
        CDS_UPDATEREGISTRY = 1,
        CDS_TEST = 2,
        CDS_FULLSCREEN = 4,
        CDS_GLOBAL = 8,
        CDS_SET_PRIMARY = 0x10,
        CDS_RESET = 0x40000000,
        CDS_NORESET = 0x10000000
    }

    [Flags()]
    enum DM : int
    {
        Orientation = 0x1,
        PaperSize = 0x2,
        PaperLength = 0x4,
        PaperWidth = 0x8,
        Scale = 0x10,
        Position = 0x20,
        NUP = 0x40,
        DisplayOrientation = 0x80,
        Copies = 0x100,
        DefaultSource = 0x200,
        PrintQuality = 0x400,
        Color = 0x800,
        Duplex = 0x1000,
        YResolution = 0x2000,
        TTOption = 0x4000,
        Collate = 0x8000,
        FormName = 0x10000,
        LogPixels = 0x20000,
        BitsPerPixel = 0x40000,
        PelsWidth = 0x80000,
        PelsHeight = 0x100000,
        DisplayFlags = 0x200000,
        DisplayFrequency = 0x400000,
        ICMMethod = 0x800000,
        ICMIntent = 0x1000000,
        MediaType = 0x2000000,
        DitherType = 0x4000000,
        PanningWidth = 0x8000000,
        PanningHeight = 0x10000000,
        DisplayFixedOutput = 0x20000000
    }

}
