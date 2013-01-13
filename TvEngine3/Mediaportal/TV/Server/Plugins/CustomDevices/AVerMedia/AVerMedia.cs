﻿#region Copyright (C) 2005-2011 Team MediaPortal

// Copyright (C) 2005-2011 Team MediaPortal
// http://www.team-mediaportal.com
// 
// MediaPortal is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MediaPortal is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MediaPortal. If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using DirectShowLib;
using Mediaportal.TV.Server.TVLibrary.Interfaces;
using Mediaportal.TV.Server.TVLibrary.Interfaces.Interfaces;
using Mediaportal.TV.Server.TVLibrary.Interfaces.Interfaces.Device;
using Mediaportal.TV.Server.TVLibrary.Interfaces.Logging;
using MicrosoftCustomDevicePlugin = Mediaportal.TV.Server.Plugins.CustomDevices.Microsoft.Microsoft;

namespace Mediaportal.TV.Server.Plugins.CustomDevices.AVerMedia
{
  /// <summary>
  /// A class for handling conditional access with the AVerMedia SatGate (A706C).
  /// </summary>
  public class AVerMedia : MicrosoftCustomDevicePlugin, IConditionalAccessProvider, ICiMenuActions
  {
    #region enums

    private enum AVerMediaCiState
    {
      Empty = 0,
      CamPresent,
      CamInitialising,
      CamReady
    }

    private enum AVerMediaMmiMessageType : ushort
    {
      Menu = 2,
      List = 3,
      Enquiry = 5,
      Unknown = 9
    }

    #endregion

    #region callback definitions

    /// <summary>
    /// Called by the CI API COM object when the common interface slot state changes.
    /// </summary>
    /// <param name="context">Internal context information maintained by the COM object.</param>
    /// <param name="stateData">Information related to the state change.</param>
    /// <returns>an HRESULT to indicate whether the state change was successfully handled</returns>
    [UnmanagedFunctionPointerAttribute(CallingConvention.StdCall)]
    private delegate Int32 OnAVerMediaCiStateChange(IntPtr context, IntPtr stateData);

    /// <summary>
    /// Called by the CI COM object when an MMI message is available.
    /// </summary>
    /// <param name="context">Internal context information maintained by the COM object.</param>
    /// <param name="messageData">The MMI message data.</param>
    /// <returns>an HRESULT to indicate whether the message was successfully handled</returns>
    [UnmanagedFunctionPointerAttribute(CallingConvention.StdCall)]
    private delegate Int32 OnAVerMediaMmiMessage(IntPtr context, IntPtr messageData);

    #endregion

    #region COM imports

    [ComImport, System.Security.SuppressUnmanagedCodeSecurity,
      Guid("7e57c354-7b47-4ec4-8a88-d50fb19cc688"),
      InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface CiInterface
    {
      /// <summary>
      /// Set the CI slot state change callback.
      /// </summary>
      /// <remarks>
      /// The callback delegate pointer must be a pointer to a pointer to a pointer to the delegate.
      /// </remarks>
      /// <param name="callBackPtr">An indirect reference to an OnAVerMediaCiStateChange delegate.</param>
      /// <returns>an HRESULT indicating whether the callback was registered successfully</returns>
      [PreserveSig]
      int SetStateChangeCallBack(IntPtr callBackPtr);

      /// <summary>
      /// Open the interface for a specific device.
      /// </summary>
      /// <remarks>
      /// Only one device can be active at any given time.
      /// </remarks>
      /// <param name="devicePath">A section of the device path.</param>
      /// <param name="filter1">Not required.</param>
      /// <param name="filter2">Not required.</param>
      /// <returns>an HRESULT indicating whether the interface was successfully opened</returns>
      [PreserveSig]
      int OpenInterface([MarshalAs(UnmanagedType.LPStr)] string devicePath, IBaseFilter filter1, IBaseFilter filter2);

      /// <summary>
      /// Close the interface.
      /// </summary>
      /// <returns>an HRESULT indicating whether the interface was successfully closed</returns>
      [PreserveSig]
      int CloseInterface();

      /// <summary>
      /// Send PMT or CA PMT to the CAM.
      /// </summary>
      /// <remarks>
      /// This function sends the PMT command twice so although you can send CA PMT, it is not
      /// necessarily going to work. In practise I was able to decrypt two channels simultaneously
      /// using this function.
      /// </remarks>
      /// <param name="pmtBuffer">A pointer to a buffer containing the PMT or CA PMT.</param>
      /// <param name="isNotCaPmt">Set to any non-zero value if the PMT buffer does not contain CA PMT.</param>
      /// <returns>an HRESULT indicating whether the PMT was successfully passed to the CAM</returns>
      [PreserveSig]
      int SendPmt(IntPtr pmtBuffer, int isNotCaPmt);
    }

    [ComImport, System.Security.SuppressUnmanagedCodeSecurity,
      Guid("c5bd61cc-f79a-4a5e-99d0-2e763ee372b6"),
      InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface MmiInterface
    {
      /// <summary>
      /// Set the MMI message handler delegate.
      /// </summary>
      /// <remarks>
      /// The callback delegate pointer must be a pointer to a pointer to a pointer to the delegate.
      /// </remarks>
      /// <param name="callBackPtr">An indirect reference to an OnAVerMediaMmiMessage delegate.</param>
      /// <returns>an HRESULT indicating whether the callback was registered successfully</returns>
      [PreserveSig]
      int SetMessageCallback(IntPtr callBackPtr);

      /// <summary>
      /// Open an MMI session with the CAM and request access to the CAM menu.
      /// </summary>
      /// <param name="deviceIndex">The COM object does not support multiple devices. Must be set to zero.</param>
      /// <returns>an HRESULT indicating whether the request was sent successfully</returns>
      [PreserveSig]
      int OpenMenu(byte deviceIndex);

      /// <summary>
      /// Close the CAM menu.
      /// </summary>
      /// <param name="deviceIndex">The COM object does not support multiple devices. Must be set to zero.</param>
      /// <returns>an HRESULT indicating whether the session was successfully closed</returns>
      [PreserveSig]
      int CloseMenu(byte deviceIndex);

      /// <summary>
      /// Send a response from the user to the CAM.
      /// </summary>
      /// <param name="deviceIndex">The COM object does not support multiple devices. Must be set to zero.</param>
      /// <param name="answer">The user's response.</param>
      /// <param name="answerLength">The length of the answer.</param>
      /// <returns>an HRESULT indicating whether the answer was successfully passed to the CAM</returns>
      [PreserveSig]
      int SendMenuAnswer(byte deviceIndex, [MarshalAs(UnmanagedType.LPStr)] string answer, byte answerLength);

      /// <summary>
      /// Select an entry in the CAM menu.
      /// </summary>
      /// <param name="deviceIndex">The COM object does not support multiple devices. Must be set to zero.</param>
      /// <param name="entry">The index (0..n) of the menu entry selected by the user.</param>
      /// <returns>an HRESULT indicating whether the entry was selected successfully</returns>
      [PreserveSig]
      int SelectMenuEntry(byte deviceIndex, byte entry);
    }

    [ComImport, System.Security.SuppressUnmanagedCodeSecurity,
      Guid("94ba71a1-3325-4eac-9df8-685921bbfa2a"),
      InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface PmtInterface
    {
      /// <summary>
      /// Send PMT to the CAM. The interface converts the PMT to CA PMT using the specified list action.
      /// </summary>
      /// <param name="pmtBuffer">A pointer to a buffer containing the PMT.</param>
      /// <param name="listAction">A CA PMT list management action.</param>
      /// <returns>an HRESULT indicating whether the PMT was successfully passed to the CAM</returns>
      [PreserveSig]
      int SendPmt1(IntPtr pmtBuffer, short listAction);

      /// <summary>
      /// Send PMT to the CAM. The interface converts the PMT to CA PMT using the "only" list action.
      /// </summary>
      /// <remarks>
      /// It is not desirable to use this function because it can't be used to decrypt multiple channels
      /// simultaneously. Note that the PMT buffer structure seems to be different to the other two PMT
      /// functions.
      /// </remarks>
      /// <param name="pmtBuffer">A pointer to a buffer containing the PMT.</param>
      /// <returns>an HRESULT indicating whether the PMT was successfully passed to the CAM</returns>
      [PreserveSig]
      int SendPmt2(IntPtr pmtBuffer);
    }

    #endregion

    #region structs

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct AVerMediaPmt
    {
      private byte Zero1;
      private Int16 Zero2;
      public Int16 Length;
      public IntPtr PmtPtr;   // Note: if you send normal PMT, do not include the CRC.
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private struct MmiMenuString
    {
      #pragma warning disable 0649
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
      public String Text;
      #pragma warning restore 0649
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct MmiData
    {
      public AVerMediaMmiMessageType MessageType;
      public UInt16 Unknown;
      public UInt16 IsBlindAnswer;
      public byte StringCount;
      public byte Count;      // either the number of entries in a menu/list, or the number of characters for an enquiry
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
      public MmiMenuString[] Strings;
    }

    #endregion

    #region constants

    private static readonly Guid _ciApiClsid = new Guid(0x68ace120, 0xe5b1, 0x48bd, 0xb8, 0x9f, 0x88, 0xdd, 0x96, 0xc2, 0x83, 0x8b);

    // From version 1.1.0.29 of the COM object.
    private static readonly string[] ValidDevices = new string[]
    {
      "ven_1131&dev_7160&subsys_1d551461",
      "ven_1131&dev_7160&subsys_26551461",  // A707 AVerTV Satellite Trinity
      "ven_1131&dev_7231&subsys_110f1461",  // H968 [pictures suggest this doesn't have a CI slot connection]
      "ven_1131&dev_7133&subsys_f01d1461",  // M135 [pictures suggest this doesn't have a CI slot connection]
      "ven_1131&dev_7133&subsys_20551461",  // A706 AVerTV Satellite Hybrid + FM
      "ven_1131&dev_7133&subsys_21551461",  // A706 AVerTV Satellite PCI
      "ven_1131&dev_7160&subsys_2E551461",  // A707 Pure DVBT CI/CA
      "vid_07ca&pid_0888",                  // R888 AVer3D Satellite TV???
      "vid_07ca&pid_c873",                  // C873 AVer MediaCenter USB Remote Controller
      "vid_07ca&pid_0889",                  // R889 AVer3D Satellite TV
      "ven_1131&dev_7160&subsys_71711461"   // A717 AVer3D Quadro HD
    };

    private const int MaxPmtLength = 1024;

    #endregion

    #region variables

    private bool _isAVerMedia = false;

    private object _ciApi = null;
    private CiInterface _ciInterface = null;
    private MmiInterface _mmiInterface = null;
    private PmtInterface _pmtInterface = null;
    private bool _isCamReady = false;
    private AVerMediaCiState _ciState = AVerMediaCiState.Empty;

    private string _tunerDevicePath = string.Empty;

    private ICiMenuCallbacks _ciMenuCallbacks = null;

    // The interface requires the callback delegate pointers to be passed
    // with two levels of indirection. This is possibly because they have
    // classes/interfaces to encapsulate the callbacks. Annoying but necessary.
    private OnAVerMediaCiStateChange _ciStateChangeDelegate = null;
    private IntPtr _ciStateChangeCallbackPtr = IntPtr.Zero;
    private IntPtr _ciStateChangeIndirectionPtr = IntPtr.Zero;
    private IntPtr _ciStateChangeInterfacePtr = IntPtr.Zero;

    private OnAVerMediaMmiMessage _mmiMessageDelegate = null;
    private IntPtr _mmiMessageCallbackPtr = IntPtr.Zero;
    private IntPtr _mmiMessageIndirectionPtr = IntPtr.Zero;
    private IntPtr _mmiMessageInterfacePtr = IntPtr.Zero;

    #endregion

    #region callback handlers

    /// <summary>
    /// Called by the CI API COM object when the common interface slot state changes.
    /// </summary>
    /// <param name="context">Internal context information maintained by the COM object.</param>
    /// <param name="stateData">Information related to the state change.</param>
    /// <returns>an HRESULT to indicate whether the state change was successfully handled</returns>
    private Int32 OnCiStateChange(IntPtr context, IntPtr stateData)
    {
      if (stateData == IntPtr.Zero)
      {
        this.LogDebug("AVerMedia: CI state change callback, no state data provided");
      }
      else
      {
        AVerMediaCiState newState = (AVerMediaCiState)Marshal.ReadByte(stateData, 0);
        this.LogDebug("AVerMedia: CI state change callback, old state = {0}, new state = {1}", _ciState, newState);
        _ciState = newState;
        _isCamReady = (_ciState == AVerMediaCiState.CamReady);

        // The other data is state dependent.
        if (_ciState == AVerMediaCiState.CamInitialising)
        {
          this.LogDebug("  menu title = {0}", Marshal.PtrToStringAnsi(IntPtr.Add(stateData, 2)));
        }
        else if (_ciState == AVerMediaCiState.CamReady)
        {
          int casCount = Marshal.ReadInt16(stateData, 2);
          this.LogDebug("  # CAS IDs = {0}", casCount);
          for (int i = 0; i < casCount; i++)
          {
            this.LogDebug("  {0,-2}        = 0x{1:x4}", i + 1, Marshal.ReadInt16(stateData, 4 + (i * 2)));
          }
        }
      }
      return 0;
    }

    /// <summary>
    /// Called by the CI COM object when an MMI message is available.
    /// </summary>
    /// <param name="context">Internal context information maintained by the COM object.</param>
    /// <param name="messageData">The MMI message data.</param>
    /// <returns>an HRESULT to indicate whether the message was successfully handled</returns>
    private Int32 OnMmiMessage(IntPtr context, IntPtr messageData)
    {
      if (messageData == IntPtr.Zero)
      {
        this.LogDebug("AVerMedia: MMI message callback, no message data provided");
        return 0;
      }
      if ((Int16)AVerMediaMmiMessageType.Unknown == Marshal.ReadInt16(messageData, 0))
      {
        return 0;
      }

      this.LogDebug("AVerMedia: MMI message callback");

      if (_ciMenuCallbacks == null)
      {
        this.LogDebug("AVerMedia: menu callbacks are not set");
      }

      MmiData data = (MmiData)Marshal.PtrToStructure(messageData, typeof(MmiData));
      this.LogDebug("  message type = {0}", data.MessageType);
      this.LogDebug("  unknown      = {0}", data.Unknown);
      this.LogDebug("  is blind     = {0}", data.IsBlindAnswer);
      this.LogDebug("  string count = {0}", data.StringCount);
      this.LogDebug("  count        = {0}", data.Count);

      if (data.StringCount >= 3)
      {
        this.LogDebug("  title        = {0}", data.Strings[0].Text);
        this.LogDebug("  sub-title    = {0}", data.Strings[1].Text);
        this.LogDebug("  footer       = {0}", data.Strings[2].Text);
        this.LogDebug("  # entries    = {0}", data.Count);
        try
        {
          if (_ciMenuCallbacks != null)
          {
            _ciMenuCallbacks.OnCiMenu(data.Strings[0].Text, data.Strings[1].Text, data.Strings[2].Text, data.Count);
          }
          if (data.Count > 0)
          {
            for (int i = 0; i < data.Count; i++)
            {
              this.LogDebug("  entry {0,-2}     = {1}", i + 1, data.Strings[i + 3].Text);
              if (_ciMenuCallbacks != null)
              {
                _ciMenuCallbacks.OnCiMenuChoice(i, data.Strings[i + 3].Text);
              }
            }
          }
        }
        catch (Exception ex)
        {
          this.LogDebug("AVerMedia: menu callback exception\r\n{0}", ex.ToString());
        }
      }
      else if (data.MessageType == AVerMediaMmiMessageType.Enquiry)
      {
        this.LogDebug("  text         = {0}", data.Strings[0].Text);
        this.LogDebug("  length       = {0}", data.Count);
        this.LogDebug("  blind        = {0}", data.IsBlindAnswer != 0);
        if (_ciMenuCallbacks != null)
        {
          try
          {
            _ciMenuCallbacks.OnCiRequest(data.IsBlindAnswer != 0, data.Count, data.Strings[0].Text);
          }
          catch (Exception ex)
          {
            this.LogDebug("AVerMedia: request callback exception\r\n{0}", ex.ToString());
          }
        }
      }
      else
      {
        this.LogDebug("AVerMedia: message type not supported");
      }

      return 0;
    }

    #endregion

    #region ICustomDevice members

    /// <summary>
    /// The loading priority for this device type.
    /// </summary>
    public override byte Priority
    {
      get
      {
        return 50;
      }
    }

    /// <summary>
    /// A human-readable name for the device. This could be a manufacturer or reseller name, or even a model
    /// name/number.
    /// </summary>
    public override String Name
    {
      get
      {
        return "AVerMedia";
      }
    }

    /// <summary>
    /// Attempt to initialise the device-specific interfaces supported by the class. If initialisation fails,
    /// the ICustomDevice instance should be disposed immediately.
    /// </summary>
    /// <param name="tunerFilter">The tuner filter in the BDA graph.</param>
    /// <param name="tunerType">The tuner type (eg. DVB-S, DVB-T... etc.).</param>
    /// <param name="tunerDevicePath">The device path of the DsDevice associated with the tuner filter.</param>
    /// <returns><c>true</c> if the interfaces are successfully initialised, otherwise <c>false</c></returns>
    public override bool Initialise(IBaseFilter tunerFilter, CardType tunerType, String tunerDevicePath)
    {
      this.LogDebug("AVerMedia: initialising device");

      if (String.IsNullOrEmpty(tunerDevicePath))
      {
        this.LogDebug("AVerMedia: tuner device path is not set");
        return false;
      }
      if (_isAVerMedia)
      {
        this.LogDebug("AVerMedia: device is already initialised");
        return true;
      }

      foreach (String validDevicePath in ValidDevices)
      {
        if (tunerDevicePath.ToLowerInvariant().Contains(validDevicePath))
        {
          base.Initialise(tunerFilter, tunerType, tunerDevicePath);

          this.LogDebug("AVerMedia: supported device detected");
          _isAVerMedia = true;
          _tunerDevicePath = tunerDevicePath;
          return true;
        }
      }

      this.LogDebug("AVerMedia: device not supported");
      return false;
    }

    #endregion

    #region IConditionalAccessProvider members

    /// <summary>
    /// Open the conditional access interface. For the interface to be opened successfully it is expected
    /// that any necessary hardware (such as a CI slot) is connected.
    /// </summary>
    /// <returns><c>true</c> if the interface is successfully opened, otherwise <c>false</c></returns>
    public bool OpenInterface()
    {
      this.LogDebug("AVerMedia: open conditional access interface");

      if (!_isAVerMedia)
      {
        this.LogDebug("AVerMedia: device not initialised or interface not supported");
        return false;
      }
      if (_ciApi != null)
      {
        this.LogDebug("AVerMedia: interface is already open");
        return false;
      }

      Type t = Type.GetTypeFromCLSID(_ciApiClsid);
      _ciApi = null;
      try
      {
        _ciApi = Activator.CreateInstance(t);
        if (_ciApi == null)
        {
          throw new NullReferenceException();
        }
      }
      catch
      {
        this.LogDebug("AVerMedia: failed to create CI API instance");
        return false;
      }

      _ciInterface = _ciApi as CiInterface;
      if (_ciInterface == null)
      {
        this.LogDebug("AVerMedia: failed to obtain CI control interface");
        return false;
      }
      _mmiInterface = _ciApi as MmiInterface;
      if (_mmiInterface == null)
      {
        this.LogDebug("AVerMedia: failed to obtain MMI interface");
        return false;
      }
      _pmtInterface = _ciApi as PmtInterface;
      if (_pmtInterface == null)
      {
        this.LogDebug("AVerMedia: failed to obtain PMT interface");
      }

      _ciState = AVerMediaCiState.Empty;
      _isCamReady = false;

      // Unfortunately this pointer manipulation really is necessary.
      _ciStateChangeDelegate = OnCiStateChange;
      _ciStateChangeCallbackPtr = Marshal.GetFunctionPointerForDelegate(_ciStateChangeDelegate);
      _ciStateChangeIndirectionPtr = Marshal.AllocCoTaskMem(8);
      Marshal.WriteIntPtr(_ciStateChangeIndirectionPtr, 0, _ciStateChangeCallbackPtr);
      _ciStateChangeInterfacePtr = Marshal.AllocCoTaskMem(8);
      Marshal.WriteIntPtr(_ciStateChangeInterfacePtr, 0, _ciStateChangeIndirectionPtr);

      _mmiMessageDelegate = OnMmiMessage;
      _mmiMessageCallbackPtr = Marshal.GetFunctionPointerForDelegate(_mmiMessageDelegate);
      _mmiMessageIndirectionPtr = Marshal.AllocCoTaskMem(8);
      Marshal.WriteIntPtr(_mmiMessageIndirectionPtr, 0, _mmiMessageCallbackPtr);
      _mmiMessageInterfacePtr = Marshal.AllocCoTaskMem(8);
      Marshal.WriteIntPtr(_mmiMessageInterfacePtr, 0, _mmiMessageIndirectionPtr);

      // Trim the device path to make it like "pci#ven_1131&dev_7160&subsys_26551461&rev_03#4&2165452d&0&0008#".
      // This is important. OpenInterface() won't succeed unless you do this.
      Match m = Regex.Match(_tunerDevicePath, @"\?\\([^{]+)\{");
      string devicePathSection = _tunerDevicePath;
      if (m.Success)
      {
        devicePathSection = m.Groups[1].Value;
      }

      int hr = _ciInterface.SetStateChangeCallBack(_ciStateChangeInterfacePtr);
      hr |= _mmiInterface.SetMessageCallback(_mmiMessageInterfacePtr);
      hr |= _ciInterface.OpenInterface(devicePathSection, null, null);
      if (hr == 0)
      {
        this.LogDebug("AVerMedia: result = success");
        return true;
      }

      this.LogDebug("AVerMedia: result = failure, hr = 0x{0:x} ({1})", hr, HResult.GetDXErrorString(hr));
      return false;
    }

    /// <summary>
    /// Close the conditional access interface.
    /// </summary>
    /// <returns><c>true</c> if the interface is successfully closed, otherwise <c>false</c></returns>
    public bool CloseInterface()
    {
      this.LogDebug("AVerMedia: close conditional access interface");

      if (_ciInterface != null)
      {
        _ciInterface.SetStateChangeCallBack(IntPtr.Zero);
        _ciInterface.CloseInterface();
        _ciInterface = null;
      }
      if (_mmiInterface != null)
      {
        _mmiInterface.SetMessageCallback(IntPtr.Zero);
        _mmiInterface = null;
      }
      _pmtInterface = null;
      if (_ciApi != null)
      {
        DsUtils.ReleaseComObject(_ciApi);
        _ciApi = null;
        _mmiInterface = null;
      }

      _ciStateChangeDelegate = null;
      _ciStateChangeCallbackPtr = IntPtr.Zero;
      if (_ciStateChangeIndirectionPtr != IntPtr.Zero)
      {
        Marshal.FreeCoTaskMem(_ciStateChangeIndirectionPtr);
        _ciStateChangeIndirectionPtr = IntPtr.Zero;
      }
      if (_ciStateChangeInterfacePtr != IntPtr.Zero)
      {
        Marshal.FreeCoTaskMem(_ciStateChangeInterfacePtr);
        _ciStateChangeInterfacePtr = IntPtr.Zero;
      }

      _mmiMessageDelegate = null;
      _mmiMessageCallbackPtr = IntPtr.Zero;
      if (_mmiMessageIndirectionPtr != IntPtr.Zero)
      {
        Marshal.FreeCoTaskMem(_mmiMessageIndirectionPtr);
        _mmiMessageIndirectionPtr = IntPtr.Zero;
      }
      if (_mmiMessageInterfacePtr != IntPtr.Zero)
      {
        Marshal.FreeCoTaskMem(_mmiMessageInterfacePtr);
        _mmiMessageInterfacePtr = IntPtr.Zero;
      }

      _ciState = AVerMediaCiState.Empty;

      this.LogDebug("AVerMedia: result = true");
      return true;
    }

    /// <summary>
    /// Reset the conditional access interface.
    /// </summary>
    /// <param name="rebuildGraph">This parameter will be set to <c>true</c> if the BDA graph must be rebuilt
    ///   for the interface to be completely and successfully reset.</param>
    /// <returns><c>true</c> if the interface is successfully reopened, otherwise <c>false</c></returns>
    public bool ResetInterface(out bool rebuildGraph)
    {
      rebuildGraph = false;
      return CloseInterface() && OpenInterface();
    }

    /// <summary>
    /// Determine whether the conditional access interface is ready to receive commands.
    /// </summary>
    /// <returns><c>true</c> if the interface is ready, otherwise <c>false</c></returns>
    public bool IsInterfaceReady()
    {
      this.LogDebug("AVerMedia: is conditional access interface ready");
      if (!_isAVerMedia || _ciApi == null)
      {
        this.LogDebug("AVerMedia: device not initialised or interface not supported");
        return false;
      }

      // The CI state is updated via callback.
      this.LogDebug("AVerMedia: result = {0}", _isCamReady);
      return _isCamReady;
    }

    /// <summary>
    /// Send a command to to the conditional access interface.
    /// </summary>
    /// <param name="channel">The channel information associated with the service which the command relates to.</param>
    /// <param name="listAction">It is assumed that the interface may be able to decrypt one or more services
    ///   simultaneously. This parameter gives the interface an indication of the number of services that it
    ///   will be expected to manage.</param>
    /// <param name="command">The type of command.</param>
    /// <param name="pmt">The programme map table for the service.</param>
    /// <param name="cat">The conditional access table for the service.</param>
    /// <returns><c>true</c> if the command is successfully sent, otherwise <c>false</c></returns>
    public bool SendCommand(IChannel channel, CaPmtListManagementAction listAction, CaPmtCommand command, Pmt pmt, Cat cat)
    {
      this.LogDebug("AVerMedia: send conditional access command, list action = {0}, command = {1}", listAction, command);

      if (!_isAVerMedia || _ciApi == null)
      {
        this.LogDebug("AVerMedia: device not initialised or interface not supported");
        return false;
      }
      if (pmt == null)
      {
        this.LogDebug("AVerMedia: PMT not supplied");
        return true;
      }

      //byte[] caPmt = DvbMmiHandler.CreateCaPmtRequest(pmt.GetCaPmt(listAction, command));
      ReadOnlyCollection<byte> rawPmt = pmt.GetRawPmt();
      //if (caPmt.Length > MaxPmtLength)
      if (rawPmt.Count > MaxPmtLength)
      {
        this.LogDebug("AVerMedia: buffer capacity too small");
        return false;
      }

      /*AVerMediaPmt averPmt = new AVerMediaPmt();
      averPmt.Length = (short)caPmt.Length;
      averPmt.PmtPtr = Marshal.AllocCoTaskMem(caPmt.Length);
      Marshal.Copy(caPmt, 0, averPmt.PmtPtr, caPmt.Length);
      IntPtr structPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(averPmt));
      Marshal.StructureToPtr(averPmt, structPtr, true);*/

      AVerMediaPmt averPmt = new AVerMediaPmt();
      averPmt.Length = (short)rawPmt.Count;
      averPmt.PmtPtr = Marshal.AllocCoTaskMem(rawPmt.Count);
      for (int i = 0; i < rawPmt.Count; i++)
      {
        Marshal.WriteByte(averPmt.PmtPtr, i, rawPmt[i]);
      }
      IntPtr structPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(averPmt));
      Marshal.StructureToPtr(averPmt, structPtr, true);

      //DVB_MMI.DumpBinary(structPtr, 0, Marshal.SizeOf(averPmt));
      //DVB_MMI.DumpBinary(averPmt.PmtPtr, 0, caPmt.Length);

      try
      {
        //int hr = _ciInterface.SendPmt(structPtr, 0);
        int hr = _pmtInterface.SendPmt1(structPtr, (short)listAction);
        if (hr == 0)
        {
          this.LogDebug("AVerMedia: result = success");
          return true;
        }
        this.LogDebug("AVerMedia: result = failure, hr = 0x{0:x} ({1})", hr, HResult.GetDXErrorString(hr));
        return false;
      }
      finally
      {
        Marshal.FreeCoTaskMem(averPmt.PmtPtr);
        Marshal.FreeCoTaskMem(structPtr);
      }
    }

    #endregion

    #region ICiMenuActions members

    /// <summary>
    /// Set the CAM callback handler functions.
    /// </summary>
    /// <param name="ciMenuHandler">A set of callback handler functions.</param>
    /// <returns><c>true</c> if the handlers are set, otherwise <c>false</c></returns>
    public bool SetCiMenuHandler(ICiMenuCallbacks ciMenuHandler)
    {
      if (ciMenuHandler != null)
      {
        _ciMenuCallbacks = ciMenuHandler;
        return true;
      }
      return false;
    }

    /// <summary>
    /// Send a request from the user to the CAM to open the menu.
    /// </summary>
    /// <returns><c>true</c> if the request is successfully passed to and processed by the CAM, otherwise <c>false</c></returns>
    public bool EnterCIMenu()
    {
      this.LogDebug("AVerMedia: enter menu");

      if (!_isAVerMedia || _mmiInterface == null)
      {
        this.LogDebug("AVerMedia: device not initialised or interface not supported");
        return false;
      }
      if (!_isCamReady)
      {
        this.LogDebug("AVerMedia: the CAM is not ready");
        return false;
      }

      int hr = _mmiInterface.OpenMenu(0);
      if (hr == 0)
      {
        this.LogDebug("AVerMedia: result = success");
        return true;
      }

      this.LogDebug("AVerMedia: result = failure, hr = 0x{0:x} ({1})", hr, HResult.GetDXErrorString(hr));
      return false;
    }

    /// <summary>
    /// Send a request from the user to the CAM to close the menu.
    /// </summary>
    /// <returns><c>true</c> if the request is successfully passed to and processed by the CAM, otherwise <c>false</c></returns>
    public bool CloseCIMenu()
    {
      this.LogDebug("AVerMedia: close menu");

      if (!_isAVerMedia || _mmiInterface == null)
      {
        this.LogDebug("AVerMedia: device not initialised or interface not supported");
        return false;
      }
      if (!_isCamReady)
      {
        this.LogDebug("AVerMedia: the CAM is not ready");
        return false;
      }

      int hr = _mmiInterface.CloseMenu(0);
      if (hr == 0)
      {
        this.LogDebug("AVerMedia: result = success");
        return true;
      }

      this.LogDebug("AVerMedia: result = failure, hr = 0x{0:x} ({1})", hr, HResult.GetDXErrorString(hr));
      return false;
    }

    /// <summary>
    /// Send a menu entry selection from the user to the CAM.
    /// </summary>
    /// <param name="choice">The index of the selection as an unsigned byte value.</param>
    /// <returns><c>true</c> if the selection is successfully passed to and processed by the CAM, otherwise <c>false</c></returns>
    public bool SelectMenu(byte choice)
    {
      this.LogDebug("AVerMedia: select menu entry, choice = {0}", choice);

      if (!_isAVerMedia || _mmiInterface == null)
      {
        this.LogDebug("AVerMedia: device not initialised or interface not supported");
        return false;
      }
      if (!_isCamReady)
      {
        this.LogDebug("AVerMedia: the CAM is not ready");
        return false;
      }

      int hr = _mmiInterface.SelectMenuEntry(0, choice);
      if (hr == 0)
      {
        this.LogDebug("AVerMedia: result = success");
        return true;
      }

      this.LogDebug("AVerMedia: result = failure, hr = 0x{0:x} ({1})", hr, HResult.GetDXErrorString(hr));
      return false;
    }

    /// <summary>
    /// Send a response from the user to the CAM.
    /// </summary>
    /// <param name="cancel"><c>True</c> to cancel the request.</param>
    /// <param name="answer">The user's response.</param>
    /// <returns><c>true</c> if the response is successfully passed to and processed by the CAM, otherwise <c>false</c></returns>
    public bool SendMenuAnswer(bool cancel, string answer)
    {
      if (answer == null)
      {
        answer = String.Empty;
      }
      this.LogDebug("AVerMedia: send menu answer, answer = {0}, cancel = {1}", answer, cancel);

      if (!_isAVerMedia || _mmiInterface == null)
      {
        this.LogDebug("AVerMedia: device not initialised or interface not supported");
        return false;
      }
      if (!_isCamReady)
      {
        this.LogDebug("AVerMedia: the CAM is not ready");
        return false;
      }

      if (cancel)
      {
        return SelectMenu(0); // 0 means "go back to the previous menu level"
      }
      if (answer.Length > 255)
      {
        this.LogDebug("AVerMedia: answer too long, length = {0}", answer.Length);
        return false;
      }

      int hr = _mmiInterface.SendMenuAnswer(0, answer, (byte)answer.Length);
      if (hr == 0)
      {
        this.LogDebug("AVerMedia: result = success");
        return true;
      }

      this.LogDebug("AVerMedia: result = failure, hr = 0x{0:x} ({1})", hr, HResult.GetDXErrorString(hr));
      return false;
    }

    #endregion

    #region IDisposable member

    /// <summary>
    /// Close interfaces, free memory and release COM object references.
    /// </summary>
    public override void Dispose()
    {
      base.Dispose();
      CloseInterface();
      _isAVerMedia = false;
    }

    #endregion
  }
}
