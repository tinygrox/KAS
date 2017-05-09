﻿// Kerbal Attachment System API
// Mod idea: KospY (http://forum.kerbalspaceprogram.com/index.php?/profile/33868-kospy/)
// API design and implemenation: igor.zavoychinskiy@gmail.com
// License: Public Domain

using KASAPIv1;
using System;
using System.Linq;
using UnityEngine;

namespace KASImpl {

class LinkUtilsImpl : ILinkUtils {
  /// <inheritdoc/>
  public ILinkTarget FindLinkTargetFromSource(ILinkSource source) {
    // Docked parts must be connected via attach nodes. Use them to doublecheck if the link is OK.
    if (source.cfgLinkMode == LinkMode.DockVessels) {
      if (source != null && source.attachNode != null && source.attachNode.attachedPart != null) {
        return source.attachNode.attachedPart.FindModulesImplementing<ILinkTarget>()
            .FirstOrDefault(s => s.attachNode != null && s.attachNode.attachedPart == source.part);
      }
      return null;
    }
    // Non-docked parts are bound via part IDs. Usen them to resolve source<=>target relation.
    if (source.linkTargetPartId > 0) {
      var targetPart = FlightGlobals.FindPartByID(source.linkTargetPartId);
      return targetPart.FindModulesImplementing<ILinkTarget>().FirstOrDefault(
          t => t.linkState == LinkState.Linked && t.linkSourcePartId == source.part.flightID);
    }
    return null;
  }

  /// <inheritdoc/>
  public ILinkSource FindLinkSourceFromTarget(ILinkTarget target) {
    // Docked parts must be connected via attach nodes. Use them to doublecheck if the link is OK.
    if (target != null && target.attachNode != null && target.attachNode.attachedPart != null) {
      return target.attachNode.attachedPart.FindModulesImplementing<ILinkSource>()
          .FirstOrDefault(s => s.attachNode != null && s.attachNode.attachedPart == target.part);
    }
    // Non-docked parts are bound via part IDs. Usen them to resolve source<=>target relation.
    if (target.linkSourcePartId > 0) {
      var sourcePart = FlightGlobals.FindPartByID(target.linkSourcePartId);
      return sourcePart.FindModulesImplementing<ILinkSource>().FirstOrDefault(
          s => s.linkState == LinkState.Linked && s.linkTargetPartId == target.part.flightID);
    }
    return null;
  }

  /// <inheritdoc/>
  public DockedVesselInfo CoupleParts(AttachNode sourceNode, AttachNode targetNode) {
    var srcPart = sourceNode.owner;
    var srcVessel = srcPart.vessel;
    var trgPart = targetNode.owner;
    var trgVessel = trgPart.vessel;

    var vesselInfo = new DockedVesselInfo();
    vesselInfo.name = srcVessel.vesselName;
    vesselInfo.vesselType = srcVessel.vesselType;
    vesselInfo.rootPartUId = srcVessel.rootPart.flightID;

    GameEvents.onActiveJointNeedUpdate.Fire(srcVessel);
    GameEvents.onActiveJointNeedUpdate.Fire(trgVessel);
    sourceNode.attachedPart = trgPart;
    sourceNode.attachedPartId = trgPart.flightID;
    targetNode.attachedPart = srcPart;
    targetNode.attachedPartId = srcPart.flightID;
    srcPart.attachMode = AttachModes.STACK;  // All KAS links are expected to be STACK.
    srcPart.Couple(trgPart);
    // Depending on how active vessel has updated do either force active or make active. Note, that
    // active vessel can be EVA kerbal, in which case nothing needs to be adjusted.    
    // FYI: This logic was taken from ModuleDockingNode.DockToVessel.
    if (srcVessel == FlightGlobals.ActiveVessel) {
      FlightGlobals.ForceSetActiveVessel(sourceNode.owner.vessel);  // Use actual vessel.
      FlightInputHandler.SetNeutralControls();
    } else if (sourceNode.owner.vessel == FlightGlobals.ActiveVessel) {
      sourceNode.owner.vessel.MakeActive();
      FlightInputHandler.SetNeutralControls();
    }
    GameEvents.onVesselWasModified.Fire(sourceNode.owner.vessel);

    return vesselInfo;
  }

  /// <inheritdoc/>
  public Vessel DecoupleParts(Part part1, Part part2) {
    Vessel inactiveVessel;
    if (part1.parent == part2) {
      part1.decouple();
      inactiveVessel = part2.vessel;
    } else if (part2.parent == part1) {
      part2.decouple();
      inactiveVessel = part1.vessel;
    } else {
      Debug.LogWarningFormat("Cannot decouple since parts belong to different vessels: {0} != {1}",
                             part1.vessel, part2.vessel);
      return null;
    }
    part1.vessel.CycleAllAutoStrut();
    part2.vessel.CycleAllAutoStrut();
    return inactiveVessel;
  }
}

}  // namespace