﻿// Kerbal Attachment System
// Mod idea: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// Module author: igor.zavoychinskiy@gmail.com
// License: Public Domain

using KSPDev.ConfigUtils;
using KSPDev.DebugUtils;
using KSPDev.GUIUtils;
using KSPDev.LogUtils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KAS {

/// <summary>
/// Dialog for adjusting parts.
/// </summary>
[KSPAddon(KSPAddon.Startup.FlightAndEditor, false /*once*/)]
[PersistentFieldsDatabase("KAS/settings/KASConfig")]
sealed class ControllerPartEditorTool : MonoBehaviour,
    // KSPDev interfaces
    IHasGUI {

  #region Configuration settings
  /// <summary>Keyboard key to trigger the GUI.</summary>
  /// <include file="SpecialDocTags.xml" path="Tags/ConfigSetting/*"/>
  [PersistentField("Debug/partAlignToolKey")]
  public string openGUIKey = "";
  #endregion

  #region Local fields
  const string DialogTitle = "KAS part adjustment tool";
  #endregion

  /// <summary>Keyboard event that opens/closes the remote GUI.</summary>
  static Event openGUIEvent;

  PartDebugAdjustmentDialog dlg;

  #region IHasGUI implementation
  /// <inheritdoc/>
  public void OnGUI() {
    if (openGUIEvent != null && Event.current.Equals(openGUIEvent)) {
      Event.current.Use();
      if (dlg == null) {
        dlg = DebugGui.MakePartDebugDialog(DialogTitle);
      } else {
        DebugGui.DestroyPartDebugDialog(dlg);
        dlg = null;
      }
    }
  }
  #endregion

  #region MonoBehavour methods
  void Awake() {
    ConfigAccessor.ReadFieldsInType(GetType(), instance: this);
    if (!string.IsNullOrEmpty(openGUIKey)) {
      openGUIEvent = Event.KeyboardEvent(openGUIKey);
    }
  }
  #endregion
}

}  // namespace
