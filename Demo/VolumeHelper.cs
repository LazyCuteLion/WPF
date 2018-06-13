using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace System.Volume
{
    /// <summary>
    /// 获取或设置系统 主音量 (支持 Xp 和 Win7+)
    /// </summary>
    public class VolumeHelper : INotifyPropertyChanged
    {
        #region  内 置 函 数

        private delegate void TryCatch();
        private static bool m_ThrowException = false;
        private static List<string> m_ListException;
        private static readonly object m_Locker = new object();

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName]string name = "")
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// 辅助类全局变量 : 如果操作音量的过程中 发生错误, 是否将异常抛出. (默认为 false, 用户操作音量失败 将得不到任何响应效果)
        /// </summary>
        public static bool ThrowException
        {
            get { return m_ThrowException; }
            set { m_ThrowException = value; }
        }
        private static void InvokeTryCatch(string method, TryCatch action)
        {
            try
            {
                if (action != null) action();
            }
            catch (Exception exp)
            {
                if (VolumeHelper.ThrowException) throw;
                else
                {
                    string msg = string.Format("在执行函数 VolumeHelper.{0}(*) 时发生异常: {1} (若要查看完整异常 请设置 VolumeHelper.ThrowException=true;)", method, exp.Message);
                    VolumeHelper.RecordException(msg);
                }
            }
        }
        private static void RecordException(string errMsg)
        {
            lock (m_Locker)
            {
                try
                {
                    if (m_ListException == null) m_ListException = new List<string>();
                    m_ListException.Add(errMsg);

                    int count = 0;
                    while (count < 10 && m_ListException.Count > 100) { m_ListException.RemoveAt(0); count++; }
                }
                catch { }
            }
        }

        private static bool IsWinXp
        {
            get { return Environment.OSVersion.Version.Major < 6; }
        }

        #endregion

        /// <summary>
        /// 获取或设置 系统主音量 是否静音
        /// </summary>
        public bool IsMute
        {
            get
            {
                lock (m_Locker)
                {
                    if (IsWinXp)
                        return WinXpVolumeOperate.IsMute;
                    else
                        return Win7VolumeOperate.IsMute;
                }
            }
            set
            {
                lock (m_Locker)
                {
                    if (IsWinXp)
                        WinXpVolumeOperate.IsMute = value;
                    else
                        Win7VolumeOperate.IsMute = value;

                    this.OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 获取或设置 系统主音量 的大小 (0-100)
        /// </summary>
        public int MasterVolume
        {
            get
            {
                lock (m_Locker)
                {
                    if (IsWinXp)
                        return WinXpVolumeOperate.MasterVolume;
                    else
                        return Win7VolumeOperate.MasterVolume;
                }
            }
            set
            {
                lock (m_Locker)
                {
                    if (IsMute)
                        IsMute = false;
                    if (IsWinXp)
                        WinXpVolumeOperate.MasterVolume = value;
                    else
                        Win7VolumeOperate.MasterVolume = value;
                    this.OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 获取 系统主音量 的 音频波动，返回 3个int数组, 分别是 主声道、左声道、右声道 的波动值 (0-100)
        /// </summary>
        public int[] PeakValues
        {
            get
            {
                lock (m_Locker)
                {
                    if (IsWinXp)
                        return WinXpVolumeOperate.PeakValues;
                    else
                        return Win7VolumeOperate.PeakValues;
                }
            }
        }

        private DispatcherTimer _timer;

        public void Listening(double milliseconds = 100)
        {
            if (_timer == null)
            {
                _timer = new DispatcherTimer();
                _timer.Tick += (s, e) =>
                {
                    this.OnPropertyChanged("MasterVolume");
                    this.OnPropertyChanged("PeakValues");
                    this.OnPropertyChanged("IsMute");
                };
            }
            if (milliseconds < 1)
                milliseconds = 100;
            _timer.Interval = TimeSpan.FromMilliseconds(milliseconds);
            _timer.Stop();
            _timer.Start();
        }

        public void Stop()
        {
            _timer?.Stop();
        }

        private VolumeHelper() { }

        public static readonly VolumeHelper Current = new VolumeHelper();

        private class WinXpVolumeOperate
        {
            public static bool IsMute
            {
                get
                {
                    bool isMute = GetIsMuted();
                    return isMute;
                }
                set
                {
                    bool isMute = GetIsMuted();
                    if (isMute != value) ReverseMute();
                }
            }
            public static int MasterVolume
            {
                get
                {
                    int value = GetVolume();
                    return (int)Math.Round(value * 100d / 65535);
                }
                set
                {
                    int nowValue = MasterVolume;
                    if (nowValue != value)
                    {
                        int newValue = (int)Math.Round(value * 65535 / 100d);
                        SetVolume(newValue);
                    }
                    //if (IsMute) IsMute = false;
                }
            }
            public static int[] PeakValues
            {
                get { return new int[3] { 0, 0, 0 }; }
            }

            private static void ReverseMute()
            {
                InvokeTryCatch("WinXpVolumeOperate.ReverseMute", () =>
                {
                    // 如果系统是静音状态 则 解除静音, 如果不是静音状态 则 设为静音
                    WinXpDllInterface.keybd_event(WinXpDllInterface.VK_VOLUME_MUTE, WinXpDllInterface.MapVirtualKey(WinXpDllInterface.VK_VOLUME_MUTE, 0), WinXpDllInterface.KEYEVENTF_EXTENDEDKEY, 0);
                    WinXpDllInterface.keybd_event(WinXpDllInterface.VK_VOLUME_MUTE, WinXpDllInterface.MapVirtualKey(WinXpDllInterface.VK_VOLUME_MUTE, 0), WinXpDllInterface.KEYEVENTF_EXTENDEDKEY | WinXpDllInterface.KEYEVENTF_KEYUP, 0);
                });
            }
            private static bool GetIsMuted()
            {
                bool result = false;
                InvokeTryCatch("WinXpVolumeOperate.GetIsMuted", () =>
                {

                    WinXpDllInterface.MIXERINFO mixer = InnerGetMixerInfo();
                    WinXpDllInterface.MIXERDETAILS mcd = new WinXpDllInterface.MIXERDETAILS();
                    mcd.cbStruct = Marshal.SizeOf(typeof(WinXpDllInterface.MIXERDETAILS));
                    mcd.dwControlID = (int)mixer.muteCtl;
                    mcd.cChannels = 1;
                    mcd.cbDetails = 4;
                    mcd.paDetails = Marshal.AllocHGlobal((int)mcd.cbDetails);

                    WinXpDllInterface.mixerGetControlDetails(0, ref mcd, WinXpDllInterface.MIXER_GETCONTROLDETAILSF_VALUE | WinXpDllInterface.MIXER_OBJECTF_MIXER);
                    int rtn = Marshal.ReadInt32(mcd.paDetails);
                    Marshal.FreeHGlobal(mcd.paDetails);

                    result = rtn != 0;
                });
                return result;
            }
            private static int GetVolume()
            {
                int result = 0;
                InvokeTryCatch("WinXpVolumeOperate.GetVolume", () =>
                {
                    int currVolume;
                    int mixerControl;

                    WinXpDllInterface.mixerOpen(out mixerControl, 0, 0, 0, 0);
                    uint type = WinXpDllInterface.MIXERCONTROL_CONTROLTYPE_VOLUME;

                    InnerGetMixer(mixerControl, WinXpDllInterface.MIXERLINE_COMPONENTTYPE_DST_SPEAKERS, type, out currVolume);
                    WinXpDllInterface.mixerClose(mixerControl);

                    result = currVolume;
                });
                return result;
            }
            private static void SetVolume(int volume)
            {
                InvokeTryCatch("WinXpVolumeOperate.SetVolume", () =>
                {
                    int currVolume;
                    int mixerControl;

                    WinXpDllInterface.mixerOpen(out mixerControl, 0, 0, 0, 0);
                    uint controlType = WinXpDllInterface.MIXERCONTROL_CONTROLTYPE_VOLUME;
                    WinXpDllInterface.MIXER mixer = InnerGetMixer(mixerControl, WinXpDllInterface.MIXERLINE_COMPONENTTYPE_DST_SPEAKERS, controlType, out currVolume);

                    bool setSucceed = false;
                    for (int i = 0; i < 3; i++)
                    {
                        if (volume > mixer.lMaximum) volume = mixer.lMaximum;
                        if (volume < mixer.lMinimum) volume = mixer.lMinimum;
                        InnerSetMixer(mixerControl, mixer, volume);
                        mixer = InnerGetMixer(mixerControl, WinXpDllInterface.MIXERLINE_COMPONENTTYPE_DST_SPEAKERS, controlType, out currVolume);

                        if (volume == currVolume) { setSucceed = true; break; } //如果设置失败, 则最多重试3次
                    }

                    WinXpDllInterface.mixerClose(mixerControl);
                    if (!setSucceed) throw new Exception("多次尝试设置 Xp操作系统音量 失败 (该异常可能是偶发异常, 可以忽略)!");
                });
            }
            private static bool InnerSetMixer(int i, WinXpDllInterface.MIXER mixer, int volume)
            {
                WinXpDllInterface.MIXERDETAILS xdl = new WinXpDllInterface.MIXERDETAILS();
                WinXpDllInterface.UMIXERDETAILS uxdl = new WinXpDllInterface.UMIXERDETAILS();

                xdl.item = 0;
                xdl.dwControlID = mixer.dwControlID;
                xdl.cbStruct = Marshal.SizeOf(xdl);
                xdl.cbDetails = Marshal.SizeOf(uxdl);

                xdl.cChannels = 1;
                uxdl.dwValue = volume;

                xdl.paDetails = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(WinXpDllInterface.UMIXERDETAILS)));
                Marshal.StructureToPtr(uxdl, xdl.paDetails, false);

                int details = WinXpDllInterface.mixerSetControlDetails(i, ref xdl, WinXpDllInterface.MIXER_SETCONTROLDETAILSF_VALUE);
                return WinXpDllInterface.MMSYSERR_NOERROR == details;
            }
            private static WinXpDllInterface.MIXER InnerGetMixer(int i, uint type, uint ctrlType, out int currVolume)
            {
                currVolume = -1;

                WinXpDllInterface.LINECONTROLS mlc = new WinXpDllInterface.LINECONTROLS();
                WinXpDllInterface.MIXERLINE mxl = new WinXpDllInterface.MIXERLINE();
                WinXpDllInterface.MIXERDETAILS xdl = new WinXpDllInterface.MIXERDETAILS();
                WinXpDllInterface.UMIXERDETAILS uxdl = new WinXpDllInterface.UMIXERDETAILS();

                WinXpDllInterface.MIXER mixerControl = new WinXpDllInterface.MIXER();

                mxl.cbStruct = (uint)Marshal.SizeOf(mxl);
                mxl.dwComponentType = (uint)type;
                int details = WinXpDllInterface.mixerGetLineInfoA(i, ref mxl, WinXpDllInterface.MIXER_GETLINEINFOF_COMPONENTTYPE);

                if (WinXpDllInterface.MMSYSERR_NOERROR == details)
                {
                    int mcSize = 152;
                    int control = Marshal.SizeOf(typeof(WinXpDllInterface.MIXER));
                    mlc.pamxctrl = Marshal.AllocCoTaskMem(mcSize);
                    mlc.cbStruct = (uint)Marshal.SizeOf(mlc);

                    mlc.dwLineID = mxl.dwLineID;
                    mlc.dwControl = (uint)ctrlType;
                    mlc.cControls = 1;
                    mlc.cbmxctrl = (uint)mcSize;

                    mixerControl.cbStruct = mcSize;

                    details = WinXpDllInterface.mixerGetLineControlsA(i, ref mlc, WinXpDllInterface.MIXER_GETLINECONTROLSF_ONEBYTYPE);
                    bool result = WinXpDllInterface.MMSYSERR_NOERROR == details;
                    if (result)
                    {
                        mixerControl = (WinXpDllInterface.MIXER)Marshal.PtrToStructure(mlc.pamxctrl, typeof(WinXpDllInterface.MIXER));

                        int mcDetailsSize = Marshal.SizeOf(typeof(WinXpDllInterface.MIXERDETAILS));
                        int mcDetailsUnsigned = Marshal.SizeOf(typeof(WinXpDllInterface.UMIXERDETAILS));
                        xdl.cbStruct = mcDetailsSize;
                        xdl.dwControlID = mixerControl.dwControlID;
                        xdl.paDetails = Marshal.AllocCoTaskMem(mcDetailsUnsigned);
                        xdl.cChannels = 1;
                        xdl.item = 0;
                        xdl.cbDetails = mcDetailsUnsigned;
                        details = WinXpDllInterface.mixerGetControlDetailsA(i, ref xdl, WinXpDllInterface.MIXER_GETCONTROLDETAILSF_VALUE);
                        uxdl = (WinXpDllInterface.UMIXERDETAILS)Marshal.PtrToStructure(xdl.paDetails, typeof(WinXpDllInterface.UMIXERDETAILS));
                        currVolume = uxdl.dwValue;

                        return mixerControl;
                    }
                }

                return mixerControl;
            }
            private static WinXpDllInterface.MIXERINFO InnerGetMixerInfo()
            {
                WinXpDllInterface.MIXERLINE mxl = new WinXpDllInterface.MIXERLINE();
                WinXpDllInterface.LINECONTROLS mlc = new WinXpDllInterface.LINECONTROLS();
                mxl.cbStruct = (uint)Marshal.SizeOf(typeof(WinXpDllInterface.MIXERLINE));
                mlc.cbStruct = (uint)Marshal.SizeOf(typeof(WinXpDllInterface.LINECONTROLS));

                WinXpDllInterface.mixerGetLineInfo(0, ref mxl, WinXpDllInterface.MIXER_OBJECTF_MIXER | WinXpDllInterface.MIXER_GETLINEINFOF_DESTINATION);

                mlc.dwLineID = mxl.dwLineID;
                mlc.cControls = mxl.cControls;
                mlc.cbmxctrl = (uint)Marshal.SizeOf(typeof(WinXpDllInterface.MIXERCONTROL));
                mlc.pamxctrl = Marshal.AllocHGlobal((int)(mlc.cbmxctrl * mlc.cControls));

                WinXpDllInterface.mixerGetLineControls(0, ref mlc, WinXpDllInterface.MIXER_OBJECTF_MIXER | WinXpDllInterface.MIXER_GETLINECONTROLSF_ALL);

                WinXpDllInterface.MIXERINFO rtn = new WinXpDllInterface.MIXERINFO();

                for (int i = 0; i < mlc.cControls; i++)
                {
                    WinXpDllInterface.MIXERCONTROL mxc = (WinXpDllInterface.MIXERCONTROL)Marshal.PtrToStructure((IntPtr)((int)mlc.pamxctrl + (int)mlc.cbmxctrl * i), typeof(WinXpDllInterface.MIXERCONTROL));
                    switch (mxc.dwControlType)
                    {
                        case WinXpDllInterface.MIXERCONTROL_CONTROLTYPE_VOLUME:
                            rtn.volumeCtl = mxc.dwControlID;
                            rtn.minVolume = mxc.Bounds.lMinimum;
                            rtn.maxVolume = mxc.Bounds.lMaximum;
                            break;
                        case WinXpDllInterface.MIXERCONTROL_CONTROLTYPE_MUTE:
                            rtn.muteCtl = mxc.dwControlID;
                            break;
                    }
                }

                Marshal.FreeHGlobal(mlc.pamxctrl);

                return rtn;
            }

            private class WinXpDllInterface
            {
                [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
                public static extern int mixerClose(int hmx);

                [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
                public static extern int mixerGetControlDetailsA(int hmxobj, ref MIXERDETAILS pmxcd, uint fdwDetails);

                [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
                public static extern int mixerGetLineControlsA(int hmxobj, ref LINECONTROLS pmxlc, uint fdwControls);

                [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
                public static extern int mixerGetLineInfoA(int hmxobj, ref MIXERLINE pmxl, uint fdwInfo);

                [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
                public static extern int mixerOpen(out int phmx, int uMxId, int dwCallback, int dwInstance, uint fdwOpen);

                [DllImport("winmm.dll", CharSet = CharSet.Ansi)]
                public static extern int mixerSetControlDetails(int hmxobj, ref MIXERDETAILS pmxcd, uint fdwDetails);

                [DllImport("winmm.dll", CharSet = CharSet.Auto)]
                public static extern uint mixerGetLineInfo(int hmxobj, ref MIXERLINE pmxl, uint flags);

                [DllImport("winmm.dll", CharSet = CharSet.Auto)]
                public static extern uint mixerGetLineControls(int hmxobj, ref LINECONTROLS pmxlc, uint flags);

                [DllImport("winmm.dll", CharSet = CharSet.Auto)]
                public static extern uint mixerGetControlDetails(int hmxobj, ref MIXERDETAILS pmxcd, uint flags);


                [DllImport("user32.dll")]
                public static extern void keybd_event(byte bVk, byte bScan, UInt32 dwFlags, UInt32 dwExtraInfo);
                [DllImport("user32.dll")]
                public static extern Byte MapVirtualKey(UInt32 uCode, UInt32 uMapType);

                public const byte VK_VOLUME_MUTE = 0xAD;
                public const UInt32 KEYEVENTF_EXTENDEDKEY = 0x0001;
                public const UInt32 KEYEVENTF_KEYUP = 0x0002;


                public const uint MAXPNAMELEN = 32;
                public const uint MMSYSERR_NOERROR = 0;
                public const uint MIXER_SHORT_NAME_CHARS = 16;
                public const uint MIXER_LONG_NAME_CHARS = 64;
                public const uint MIXER_GETLINEINFOF_COMPONENTTYPE = 0x3;
                public const uint MIXER_GETCONTROLDETAILSF_VALUE = 0x0;
                public const uint MIXER_GETLINECONTROLSF_ONEBYTYPE = 0x2;
                public const uint MIXER_SETCONTROLDETAILSF_VALUE = 0x0;
                public const uint MIXER_GETLINEINFOF_DESTINATION = 0x00000000;
                public const uint MIXER_GETLINECONTROLSF_ALL = 0x00000000;
                public const uint MIXER_OBJECTF_MIXER = 0x00000000;
                public const uint MIXERLINE_COMPONENTTYPE_DST_FIRST = 0x0;
                public const uint MIXERCONTROL_CT_CLASS_FADER = 0x50000000;
                public const uint MIXERCONTROL_CT_UNITS_UNSIGNED = 0x30000;
                public const uint MIXERCONTROL_CONTROLTYPE_FADER = (MIXERCONTROL_CT_CLASS_FADER | MIXERCONTROL_CT_UNITS_UNSIGNED);
                public const uint MIXERCONTROL_CONTROLTYPE_VOLUME = (MIXERCONTROL_CONTROLTYPE_FADER + 1);
                public const uint MIXERCONTROL_CT_CLASS_SWITCH = 0x20000000;
                public const uint MIXERCONTROL_CT_SC_SWITCH_BOOLEAN = 0x00000000;
                public const uint MIXERCONTROL_CT_UNITS_BOOLEAN = 0x00010000;
                public const uint MIXERCONTROL_CONTROLTYPE_BOOLEAN = MIXERCONTROL_CT_CLASS_SWITCH | MIXERCONTROL_CT_SC_SWITCH_BOOLEAN | MIXERCONTROL_CT_UNITS_BOOLEAN;
                public const uint MIXERCONTROL_CONTROLTYPE_MUTE = MIXERCONTROL_CONTROLTYPE_BOOLEAN + 2;
                public const uint MIXERLINE_COMPONENTTYPE_DST_SPEAKERS = (MIXERLINE_COMPONENTTYPE_DST_FIRST + 4);


                public struct MIXER
                {
                    public int cbStruct;
                    public int dwControlID;
                    public int dwControlType;
                    public int fdwControl;
                    public int cMultipleItems;
                    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)MIXER_SHORT_NAME_CHARS)]
                    public string szShortName;
                    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)MIXER_LONG_NAME_CHARS)]
                    public string szName;
                    public int lMinimum;
                    public int lMaximum;
                    [MarshalAs(UnmanagedType.U4, SizeConst = 10)]
                    public int reserved;
                }

                public struct MIXERINFO
                {
                    public uint volumeCtl;
                    public uint muteCtl;
                    public int minVolume;
                    public int maxVolume;
                }

                [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
                public struct MIXERLINE
                {
                    public uint cbStruct;
                    public uint dwDestination;
                    public uint dwSource;
                    public uint dwLineID;
                    public uint fdwLine;
                    public uint dwUser;
                    public uint dwComponentType;
                    public uint cChannels;
                    public uint cConnections;
                    public uint cControls;
                    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)MIXER_SHORT_NAME_CHARS)]
                    public string szShortName;
                    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)MIXER_LONG_NAME_CHARS)]
                    public string szName;

                    public uint dwType;
                    public uint dwDeviceID;
                    public ushort wMid;
                    public ushort wPid;
                    public uint vDriverVersion;
                    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)MAXPNAMELEN)]
                    public string szPname;
                }

                public struct MIXERDETAILS
                {
                    public int cbStruct;
                    public int dwControlID;
                    public int cChannels;
                    public int item;
                    public int cbDetails;
                    public IntPtr paDetails;
                }

                public struct LINECONTROLS
                {
                    public uint cbStruct;
                    public uint dwLineID;
                    public uint dwControl;
                    public uint cControls;
                    public uint cbmxctrl;
                    public IntPtr pamxctrl;
                }

                [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
                public struct MIXERCONTROL
                {
                    [StructLayout(LayoutKind.Explicit)]
                    public struct BoundsInfo
                    {
                        [FieldOffset(0)]
                        public int lMinimum;
                        [FieldOffset(4)]
                        public int lMaximum;
                        [FieldOffset(0)]
                        public uint dwMinimum;
                        [FieldOffset(4)]
                        public uint dwMaximum;
                        [FieldOffset(8), MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
                        public uint[] dwReserved;
                    }
                    [StructLayout(LayoutKind.Explicit)]
                    public struct MetricsInfo
                    {
                        [FieldOffset(0)]
                        public uint cSteps;
                        [FieldOffset(0)]
                        public uint cbCustomData;
                        [FieldOffset(4), MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
                        public uint[] dwReserved;
                    }

                    public uint cbStruct;
                    public uint dwControlID;
                    public uint dwControlType;
                    public uint fdwControl;
                    public uint cMultipleItems;
                    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)MIXER_SHORT_NAME_CHARS)]
                    public string szShortName;
                    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)MIXER_LONG_NAME_CHARS)]
                    public string szName;
                    public BoundsInfo Bounds;
                    public MetricsInfo Metrics;
                }

                public struct UMIXERDETAILS
                {
                    public int dwValue;
                }

            }
        }

        private class Win7VolumeOperate
        {
            public static bool IsMute
            {
                get
                {
                    bool isMute = GetEndPointVolumeMute();
                    return isMute;
                }
                set
                {
                    bool isMute = IsMute;
                    if (isMute != value) SetEndPointVolumeMute(value);
                }
            }
            public static int MasterVolume
            {
                get
                {
                    float value = GetEndPointVolumeMasterVolumeLevelScalar();
                    return (int)Math.Round(value * 100d);
                }
                set
                {
                    int nowValue = MasterVolume;
                    if (nowValue != value)
                    {
                        float newValue = (float)((double)value / 100d);
                        SetEndPointVolumeMasterVolumeLevelScalar(newValue);
                    }
                    //if (IsMute) IsMute = false;
                }
            }
            public static int[] PeakValues
            {
                get
                {
                    try
                    {
                        float[] channelPeakValues = GetAudioMeterInfoChannelsPeakValues();
                        return new int[3]
                        {
                            (int)Math.Round(GetAudioMeterInfoPeakValue() * 100d),
                            (int)Math.Round((channelPeakValues!=null && channelPeakValues.Length>=1 ? channelPeakValues[0]:0) * 100d),
                            (int)Math.Round((channelPeakValues!=null && channelPeakValues.Length>=2 ? channelPeakValues[1]:0) * 100d)
                        };
                    }
                    catch { return new int[3] { 0, 0, 0 }; }
                }
            }

            private static Win7ComInterface.IMMDevice m_Device;
            private static Win7ComInterface.IAudioMeterInformation m_AudioMeterInfo;
            private static Win7ComInterface.IAudioEndpointVolume m_EndPointVolume;

            private static Win7ComInterface.IMMDevice Device
            {
                get
                {
                    if (m_Device == null)
                    {
                        const int EDataFlow_eRender = 0;
                        const int ERole_eMultimedia = 1;
                        m_Device = GetDefaultAudioEndpoint(EDataFlow_eRender, ERole_eMultimedia);
                    }
                    return m_Device;
                }
            }
            private static Win7ComInterface.IAudioMeterInformation AudioMeterInfo
            {
                get
                {
                    if (m_AudioMeterInfo == null)
                        m_AudioMeterInfo = GetAudioMeterInformation();
                    return m_AudioMeterInfo;
                }
            }
            private static Win7ComInterface.IAudioEndpointVolume EndPointVolume
            {
                get
                {
                    if (m_EndPointVolume == null)
                        m_EndPointVolume = GetAudioEndPointVolume();
                    return m_EndPointVolume;
                }
            }
            private static Win7ComInterface.IMMDevice GetDefaultAudioEndpoint(int dataFlow, int role)
            {
                Win7ComInterface.IMMDevice result = null;
                InvokeTryCatch("Win7VolumeOperate.GetDefaultAudioEndpoint", () =>
                {
                    Win7ComInterface.IMMDeviceEnumerator _realEnumerator = new Win7ComInterface._MMDeviceEnumerator() as Win7ComInterface.IMMDeviceEnumerator;
                    if (_realEnumerator == null) throw new Exception("初始化 Win7以上操作系统的 音频Com MMDeviceEnumerator 组件失败!");

                    Win7ComInterface.IMMDevice device = null;
                    int errorCode = _realEnumerator.GetDefaultAudioEndpoint((int)dataFlow, (int)role, out device);
                    Marshal.ThrowExceptionForHR(errorCode);
                    result = device;
                });
                return result;
            }
            private static Win7ComInterface.IAudioMeterInformation GetAudioMeterInformation()
            {
                Win7ComInterface.IAudioMeterInformation result = null;
                InvokeTryCatch("Win7VolumeOperate.GetAudioMeterInformation", () =>
                {
                    Win7ComInterface.IMMDevice device = Device;
                    if (device == null) return;

                    const uint CLSCTX_ALL = 23u;

                    object comMeterInfo;
                    Guid IID_IAudioMeterInformation = typeof(Win7ComInterface.IAudioMeterInformation).GUID;
                    Marshal.ThrowExceptionForHR(device.Activate(ref IID_IAudioMeterInformation, CLSCTX_ALL, IntPtr.Zero, out comMeterInfo));
                    Win7ComInterface.IAudioMeterInformation meterInfo = comMeterInfo as Win7ComInterface.IAudioMeterInformation;

                    if (meterInfo == null) throw new Exception("初始化 Win7以上操作系统的 音频Com IAudioMeterInformation 组件失败!");

                    int HardwareSupp;
                    Marshal.ThrowExceptionForHR(meterInfo.QueryHardwareSupport(out HardwareSupp));
                    result = meterInfo;
                });
                return result;
            }
            private static Win7ComInterface.IAudioEndpointVolume GetAudioEndPointVolume()
            {
                Win7ComInterface.IAudioEndpointVolume result = null;
                InvokeTryCatch("Win7VolumeOperate.GetAudioEndPointVolume", () =>
                {
                    Win7ComInterface.IMMDevice device = Device;
                    if (device == null) return;

                    const uint CLSCTX_ALL = 23u;

                    object comEndPointVol;
                    Guid IID_IAudioEndpointVolume = typeof(Win7ComInterface.IAudioEndpointVolume).GUID;
                    Marshal.ThrowExceptionForHR(device.Activate(ref IID_IAudioEndpointVolume, CLSCTX_ALL, IntPtr.Zero, out comEndPointVol));
                    Win7ComInterface.IAudioEndpointVolume endPointVol = comEndPointVol as Win7ComInterface.IAudioEndpointVolume;

                    if (endPointVol == null) throw new Exception("初始化 Win7以上操作系统的 音频Com IAudioEndpointVolume 组件失败!");

                    uint HardwareSupp;
                    Marshal.ThrowExceptionForHR(endPointVol.QueryHardwareSupport(out HardwareSupp));

                    result = endPointVol;
                });
                return result;
            }
            private static int GetAudioMeterInfoCount()
            {
                int result = 0;
                InvokeTryCatch("Win7VolumeOperate.GetAudioMeterInfoCount", () =>
                {
                    Win7ComInterface.IAudioMeterInformation audioMeterInfo = AudioMeterInfo;
                    if (audioMeterInfo == null) return;

                    int temp;
                    Marshal.ThrowExceptionForHR(audioMeterInfo.GetMeteringChannelCount(out temp));
                    result = temp;
                });
                return result;
            }
            private static float GetAudioMeterInfoPeakValue()
            {
                float result = 0;
                InvokeTryCatch("Win7VolumeOperate.GetAudioMeterInfoPeakValue", () =>
                {
                    Win7ComInterface.IAudioMeterInformation audioMeterInfo = AudioMeterInfo;
                    if (audioMeterInfo == null) return;

                    float temp;
                    Marshal.ThrowExceptionForHR(audioMeterInfo.GetPeakValue(out temp));
                    result = temp;
                });
                return result;
            }
            private static float[] GetAudioMeterInfoChannelsPeakValues()
            {
                float[] result = new float[] { 0f, 0f };
                InvokeTryCatch("Win7VolumeOperate.GetAudioMeterInfoChannelsPeakValues", () =>
                {
                    Win7ComInterface.IAudioMeterInformation audioMeterInfo = AudioMeterInfo;
                    if (audioMeterInfo == null) return;

                    int count;
                    Marshal.ThrowExceptionForHR(audioMeterInfo.GetMeteringChannelCount(out count));

                    float[] peakValues = new float[count];
                    GCHandle Params = GCHandle.Alloc(peakValues, GCHandleType.Pinned);
                    Marshal.ThrowExceptionForHR(audioMeterInfo.GetChannelsPeakValues(peakValues.Length, Params.AddrOfPinnedObject()));
                    Params.Free();
                    result = peakValues;
                });
                return result;
            }
            private static bool GetEndPointVolumeMute()
            {
                bool result = false;
                InvokeTryCatch("Win7VolumeOperate.GetEndPointVolumeMute", () =>
                {
                    Win7ComInterface.IAudioEndpointVolume endPointVolume = EndPointVolume;
                    if (endPointVolume == null) return;

                    bool temp;
                    Marshal.ThrowExceptionForHR(endPointVolume.GetMute(out temp));
                    result = temp;
                });
                return result;
            }
            private static void SetEndPointVolumeMute(bool value)
            {
                InvokeTryCatch("Win7VolumeOperate.SetEndPointVolumeMute", () =>
                {
                    Win7ComInterface.IAudioEndpointVolume endPointVolume = EndPointVolume;
                    if (endPointVolume == null) return;
                    Marshal.ThrowExceptionForHR(endPointVolume.SetMute(value, Guid.Empty));
                });
            }
            private static float GetEndPointVolumeMasterVolumeLevelScalar()
            {
                float result = 0;
                InvokeTryCatch("Win7VolumeOperate.GetEndPointVolumeMasterVolumeLevelScalar", () =>
                {
                    Win7ComInterface.IAudioEndpointVolume endPointVolume = EndPointVolume;
                    if (endPointVolume == null) return;

                    float temp;
                    Marshal.ThrowExceptionForHR(endPointVolume.GetMasterVolumeLevelScalar(out temp));
                    result = temp;
                });
                return result;
            }
            private static void SetEndPointVolumeMasterVolumeLevelScalar(float value)
            {
                InvokeTryCatch("Win7VolumeOperate.SetEndPointVolumeMasterVolumeLevelScalar", () =>
                {
                    Win7ComInterface.IAudioEndpointVolume endPointVolume = EndPointVolume;
                    if (endPointVolume == null) return;
                    Marshal.ThrowExceptionForHR(endPointVolume.SetMasterVolumeLevelScalar(value, Guid.Empty));
                });
            }

            private class Win7ComInterface
            {
                [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
                internal class _MMDeviceEnumerator
                {
                }


                [Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
                internal interface IAudioEndpointVolume
                {
                    //以下很多函数 没用, 但既不能删除, 也不能修改, 甚至不能调换位置

                    [PreserveSig, Obsolete("不用", true)]
                    int RegisterControlChangeNotify(IAudioEndpointVolumeCallback pNotify);
                    [PreserveSig, Obsolete("不用", true)]
                    int UnregisterControlChangeNotify(IAudioEndpointVolumeCallback pNotify);
                    [PreserveSig, Obsolete("不用", true)]
                    int GetChannelCount(out int pnChannelCount);
                    [PreserveSig, Obsolete("不用", true)]
                    int SetMasterVolumeLevel(float fLevelDB, Guid pguidEventContext);
                    [PreserveSig]
                    int SetMasterVolumeLevelScalar(float fLevel, Guid pguidEventContext);
                    [PreserveSig, Obsolete("不用", true)]
                    int GetMasterVolumeLevel(out float pfLevelDB);
                    [PreserveSig]
                    int GetMasterVolumeLevelScalar(out float pfLevel);
                    [PreserveSig, Obsolete("不用", true)]
                    int SetChannelVolumeLevel(uint nChannel, float fLevelDB, Guid pguidEventContext);
                    [PreserveSig, Obsolete("不用", true)]
                    int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, Guid pguidEventContext);
                    [PreserveSig, Obsolete("不用", true)]
                    int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);
                    [PreserveSig, Obsolete("不用", true)]
                    int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);
                    [PreserveSig]
                    int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, Guid pguidEventContext);
                    [PreserveSig]
                    int GetMute(out bool pbMute);
                    [PreserveSig, Obsolete("不用", true)]
                    int GetVolumeStepInfo(out uint pnStep, out uint pnStepCount);
                    [PreserveSig, Obsolete("不用", true)]
                    int VolumeStepUp(Guid pguidEventContext);
                    [PreserveSig, Obsolete("不用", true)]
                    int VolumeStepDown(Guid pguidEventContext);
                    [PreserveSig]
                    int QueryHardwareSupport(out uint pdwHardwareSupportMask);
                    [PreserveSig, Obsolete("不用", true)]
                    int GetVolumeRange(out float pflVolumeMindB, out float pflVolumeMaxdB, out float pflVolumeIncrementdB);
                }

                [Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
                internal interface IAudioMeterInformation
                {
                    [PreserveSig]
                    int GetPeakValue(out float pfPeak);
                    [PreserveSig]
                    int GetMeteringChannelCount(out int pnChannelCount);
                    [PreserveSig]
                    int GetChannelsPeakValues(int u32ChannelCount, [In] IntPtr afPeakValues);
                    [PreserveSig]
                    int QueryHardwareSupport(out int pdwHardwareSupportMask);
                }

                [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
                internal interface IMMDevice
                {
                    //以下很多函数 没用, 但既不能删除, 也不能修改, 甚至不能调换位置

                    [PreserveSig]
                    int Activate(ref Guid iid, uint dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface); //CLSCTX dwClsCtx
                    [PreserveSig, Obsolete("不用", true)]
                    int OpenPropertyStore(int stgmAccess, out IPropertyStore propertyStore); //EStgmAccess stgmAccess
                    [PreserveSig, Obsolete("不用", true)]
                    int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
                    [PreserveSig, Obsolete("不用", true)]
                    int GetState(out int pdwState);
                }

                [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
                internal interface IMMDeviceEnumerator
                {
                    //以下很多函数 没用, 但既不能删除, 也不能修改, 甚至不能调换位置

                    [PreserveSig, Obsolete("不用", true)]
                    int EnumAudioEndpoints(int dataFlow, int StateMask, out IMMDeviceCollection device);
                    [PreserveSig]
                    int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppEndpoint);
                    [PreserveSig, Obsolete("不用", true)]
                    int GetDevice(string pwstrId, out IMMDevice ppDevice);
                    [PreserveSig, Obsolete("不用", true)]
                    int RegisterEndpointNotificationCallback(IntPtr pClient);
                    [PreserveSig, Obsolete("不用", true)]
                    int UnregisterEndpointNotificationCallback(IntPtr pClient);
                }

                [Guid("657804FA-D6AD-4496-8A60-352752AF4F89"), Obsolete("不用", true), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
                internal interface IAudioEndpointVolumeCallback
                {
                    //[PreserveSig]
                    //int OnNotify(IntPtr pNotifyData);
                }
                [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), Obsolete("不用", true), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
                internal interface IMMDeviceCollection
                {
                    //[PreserveSig]
                    //int GetCount(out uint pcDevices);
                    //[PreserveSig]
                    //int Item(uint nDevice, out IMMDevice Device);
                }
                [Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"), Obsolete("不用", true), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
                internal interface IPropertyStore
                {
                    //[PreserveSig]
                    //int GetCount(out int count);
                    //[PreserveSig]
                    //int GetAt(int iProp, out PropertyKey pkey);
                    //[PreserveSig]
                    //int GetValue(ref PropertyKey key, out PropVariant pv);
                    //[PreserveSig]
                    //int SetValue(ref PropertyKey key, ref PropVariant propvar);
                    //[PreserveSig]
                    //int Commit();
                }
            }
        }
    }

}
