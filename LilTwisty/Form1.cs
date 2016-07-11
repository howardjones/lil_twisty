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

using Microsoft.Win32;

namespace LilTwisty
{

    public partial class Form1 : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        enum KeyModifier
        {
            None = 0,
            Alt = 1,
            Control = 2,
            Shift = 4,
            WinKey = 8
        }

        public List<KnownDisplay> known_displays = new List<KnownDisplay>();
        public Rectangle desktop_area = new Rectangle(0, 0, 0, 0);
        public float display_scalefactor = 1.0F;
        public int selected_display = -1;

        public Form1()
        {
            InitializeComponent();

            RegisterHotKey(this.Handle, 0, (int)KeyModifier.Shift + (int)KeyModifier.WinKey, Keys.F9.GetHashCode());       // Register Win + Shift + F9 as global hotkey. 
            RegisterHotKey(this.Handle, 1, (int)KeyModifier.Shift + (int)KeyModifier.WinKey, Keys.F10.GetHashCode());       // Register Win + Shift + F10 as global hotkey. 

        }

        public void repopulate_display_list()
        {
            DISPLAY_DEVICE d = new DISPLAY_DEVICE();
            DEVMODE dm = new DEVMODE();
            d.cb = Marshal.SizeOf(d);

            known_displays.Clear();
            int[] bounding_parts = new int[] { 0, 0, 0, 0 };

            for (uint i = 0; i < 100; i++)
            {
                bool result = NativeMethods.EnumDisplayDevices(null, i, ref d, 0);
                if (!result)
                {
                    break;
                }

                string label = "";

                int res = NativeMethods.EnumDisplaySettings(
                    d.DeviceName, 
                    NativeMethods.ENUM_CURRENT_SETTINGS, 
                    ref dm);

                if (res != 0)
                {
                    POINTL pos = dm.dmPosition;

                    Rectangle display_bounding = new Rectangle(pos.x, pos.y, dm.dmPelsWidth, dm.dmPelsHeight);
                    
                    Console.WriteLine("DISPLAY {0}: {1}", i, display_bounding);

                    label = string.Format("[{0}] {3} <{4}> {1}", i, d.DeviceString, d.DeviceName, display_bounding, dm.dmDisplayOrientation);
                    // listBox1.Items.Add(label);

                    KnownDisplay kd = new KnownDisplay();
                    kd.display_id = i;
                    kd.label = label;
                    kd.bounding = display_bounding;
                    kd.scaled = display_bounding;

                    known_displays.Add(kd);

                    bounding_parts[0] = Math.Min(display_bounding.Left, bounding_parts[0]);
                    bounding_parts[1] = Math.Min(display_bounding.Top, bounding_parts[1]);
                    bounding_parts[2] = Math.Max(display_bounding.Left + display_bounding.Width, bounding_parts[2]);
                    bounding_parts[3] = Math.Max(display_bounding.Top + display_bounding.Height, bounding_parts[3]);
                }
            }
                        
            desktop_area = new Rectangle(bounding_parts[0], bounding_parts[1], bounding_parts[2] - bounding_parts[0], bounding_parts[3] - bounding_parts[1]);
            Console.WriteLine("DESKTOP: {0}", desktop_area);
            float scale_h = (float)desktop_area.Width / (float)panel1.Width;
            float scale_v = (float)desktop_area.Height / (float)panel1.Height;
            
            display_scalefactor = Math.Max(scale_h, scale_v);
            Console.WriteLine("{0} {1} => {2}", scale_h, scale_v, display_scalefactor);

            // if the desktop extends either side of the origin, this will pull it all into view.
            int origin_x = (int)((float)-desktop_area.Left / display_scalefactor);
            int origin_y = (int)((float)-desktop_area.Top / display_scalefactor);

            float scaled_height_diff = ((float)panel1.Height - (float)desktop_area.Height / display_scalefactor) / 2.0F;
            float scaled_width_diff = ((float)panel1.Width - (float)desktop_area.Width/ display_scalefactor) / 2.0F;

            Console.WriteLine("{0} {1}", scaled_width_diff, scaled_height_diff);

            origin_x += (int)scaled_width_diff;
            origin_y += (int)scaled_height_diff;

            for (int x=0; x < known_displays.Count; x++)
            {
                KnownDisplay kd = known_displays[x];
                
                Rectangle new_r = new Rectangle(
                        origin_x + (int)((float)kd.bounding.Left / display_scalefactor),
                        origin_y + (int)((float)kd.bounding.Top / display_scalefactor),
                        (int)((float)kd.bounding.Width / display_scalefactor),
                        (int)((float)kd.bounding.Height / display_scalefactor)
                    );

                Console.WriteLine("SCALED: {0}", new_r);
                kd.scaled = new_r;
                known_displays[x] = kd;                               
            }
        }

        private void saveSettings()
        {            
            
        }

        private void loadSettings()
        {
         
        }

        private void twistButton_Click(object sender, EventArgs e)
        {
            if (selected_display >= 0)
            {
                RotateDisplay.go((uint)selected_display);
                repopulate_display_list();
            }            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            loadSettings();
            repopulate_display_list();            
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

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            Console.WriteLine("Repaint");
            
            foreach (KnownDisplay kd in known_displays)
            {                
                Console.WriteLine("Drawing: {0} {1}", kd.scaled, kd.label);

                Rectangle draw = kd.scaled;
                Point centre_point = new Point(draw.Width / 2 + draw.Left, draw.Height / 2 + draw.Top);
                String label = String.Format("{0}", kd.display_id);

                if (kd.display_id == selected_display)
                {
                    e.Graphics.FillRectangle(Brushes.White, draw);
                    e.Graphics.DrawString(label, SystemFonts.CaptionFont, Brushes.Black, centre_point);
                } else
                {
                    e.Graphics.FillRectangle(Brushes.LightGray, draw);
                    e.Graphics.DrawString(label, SystemFonts.SmallCaptionFont, Brushes.Black, centre_point);
                }
                e.Graphics.DrawRectangle(Pens.Black, draw);                
            }
            
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            repopulate_display_list();
        }

        private void panel1_MouseClick(object sender, MouseEventArgs e)
        {
            selected_display = -1;
            foreach (KnownDisplay kd in known_displays)
            {
                if (kd.scaled.Contains(e.Location)) {
                    selected_display = (int)kd.display_id;
                    Console.WriteLine("Selected new display: {0}", selected_display);
                }
            }

            panel1.Invalidate();
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == 0x0312)
            {
                /* Note that the three lines below are not needed if you only want to register one hotkey.
                 * The below lines are useful in case you want to register multiple keys, which you can use a switch with the id as argument, or if you want to know which key/modifier was pressed for some particular reason. */

                Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);                  // The key of the hotkey that was pressed.
                KeyModifier modifier = (KeyModifier)((int)m.LParam & 0xFFFF);       // The modifier of the hotkey that was pressed.
                int id = m.WParam.ToInt32();                                        // The id of the hotkey that was pressed.
                
                Console.WriteLine("Hotkey {0} was pressed", id);
                // do something

                if (selected_display >= 0)
                {
                    if (id == 1)
                    {
                        RotateDisplay.set_rotation((uint)selected_display, NativeMethods.DMDO_90);
                    }
                    if (id == 0)
                    {
                        RotateDisplay.set_rotation((uint)selected_display, NativeMethods.DMDO_DEFAULT);
                    }

                    repopulate_display_list();
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            saveSettings();
            UnregisterHotKey(this.Handle, 0);       // Unregister hotkey with id 0 before closing the form. 
            UnregisterHotKey(this.Handle, 1);       // Unregister hotkey with id 1 before closing the form. 
        }
    }

    public struct KnownDisplay
    {
        public Rectangle bounding;
        public Rectangle scaled;
        public uint display_id;
        public String label;
    };

    class RotateDisplay
    {
        public static void set_rotation(uint deviceID, int newOrientation)
        {
            // uint deviceID = 1; // zero origin (i.e. 1 means DISPLAY2)

            DISPLAY_DEVICE d = new DISPLAY_DEVICE();
            DEVMODE dm = new DEVMODE();
            d.cb = Marshal.SizeOf(d);

            NativeMethods.EnumDisplayDevices(null, deviceID, ref d, 0);

            int res = NativeMethods.EnumDisplaySettings(
                d.DeviceName, NativeMethods.ENUM_CURRENT_SETTINGS, ref dm);

            if (0 != res)
            {
                int temp = dm.dmPelsHeight;
                dm.dmPelsHeight = dm.dmPelsWidth;
                dm.dmPelsWidth = temp;

                dm.dmDisplayOrientation = newOrientation;

                DISP_CHANGE iRet = NativeMethods.ChangeDisplaySettingsEx(
                    d.DeviceName, ref dm, IntPtr.Zero,
                    DisplaySettingsFlags.CDS_UPDATEREGISTRY, IntPtr.Zero);             
            }
        }

        public static void go(uint deviceID)
        {
            // uint deviceID = 1; // zero origin (i.e. 1 means DISPLAY2)

            DISPLAY_DEVICE d = new DISPLAY_DEVICE();
            DEVMODE dm = new DEVMODE();
            d.cb = Marshal.SizeOf(d);
            
            NativeMethods.EnumDisplayDevices(null, deviceID, ref d, 0);
            
            int res = NativeMethods.EnumDisplaySettings(
                d.DeviceName, NativeMethods.ENUM_CURRENT_SETTINGS, ref dm);
            
            if (0 != res)
            {
                int temp = dm.dmPelsHeight;
                dm.dmPelsHeight = dm.dmPelsWidth;
                dm.dmPelsWidth = temp;

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
