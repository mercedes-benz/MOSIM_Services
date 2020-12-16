# -*- coding: utf-8 -*-


from typing import List, Dict
from operator import attrgetter
import logging
import math

# Blender-Imports
from mathutils import Vector, Quaternion
import bpy

# private-imports
from BlenderMMI.IntermediateSkeletonApplication import IntermediateSkeletonApplication
from server import convert, check

# MOSIM-declarations
from MMIStandard.services import MInverseKinematicsService
from MMIStandard.services.ttypes import MIKProperty, MIKServiceResult, MIKOperationType
from MMIStandard.avatar.ttypes import MAvatarPostureValues, MEndeffectorType, MJointType
from MMIStandard.core.ttypes import MBoolResponse
from MMIStandard.constraints.ttypes import MConstraint, MJointConstraint


logger = logging.getLogger(__name__)

class IKService(MInverseKinematicsService.Iface):
    """ 
    Adapter-Object to connect the Thrift-Server with the logic. Contains the 
    method for the <MMIServiceBase> and <MInverseKinematicsService>.
    """

    def __init__(self, posture):
        logger.info("Initializing %s", self.__class__)
        self.avatars          = dict()  # Only for reference, not in use!
        self.skeletons        = dict()  # Only for reference, not in use!
        self.app              = None    # IntermediateSkeletonApplication, contains the avatar and the skeleton
        
        # While this Class kann memorize multiple avatars and skeletons, it 
        # doesn't provide a way to switch between them.
        
        avatar                = posture  # avatar = Posture ?, replaces self.avatars?
        avatar.AvatarID       = "lalala"
        
        # self.counter = 0 # not in use!
        self.SetAvatar(avatar)
        
        self._IKcounter = 0
        bpy.context.scene.unit_settings.system_rotation = 'RADIANS'
        
    def SetAvatar(self, avatar) -> bool: # Types!
        if avatar.AvatarID is not None and avatar.Joints is not None:
            logger.debug('Set Avatar: %s', avatar.AvatarID)
            avatar_id                 = avatar.AvatarID            
            self.avatars[avatar_id]   = avatar
            
            # create the app. It does the scaling according to the posture automatically. 
            self.app = IntermediateSkeletonApplication(avatar_id, avatar) # app wird bei jedem set überschrieben?           
        else:
            logger.warning("Tried to set an empty avatar")
        return True

    def CalculateIKPosture(self, postureValues: MAvatarPostureValues, constraints: List[MConstraint], properties: Dict[str, str]) -> MIKServiceResult:
    # def CalculateIKPosture(self, *args) -> MIKServiceResult:
        """
        Implementation for <MInverseKinematicsService>: The method applies the 
        postureValues to the Blender-Representation of the Avatar. Then it 
        
        Arguments:
         - postureValues    MAvatarPostureValues
         - constraints      List of MConstraints
         - properties       MIKproperty
         
        Returns:
         - MIKServiceResult
            . Posture   MAvatarPostureValues
            . Success   bool
            . Error     list[double]
        """
        logger.debug("Call to CalculatIKPosture [%i]", self._IKcounter)
        print(f"postureValues: {postureValues}")
        print(f"Constraints: {constraints}")
        print(f"properties: {properties}")
        
        # preparations
        ## check avatar id
        
        avatar = self.app # Should be a dict-lookup
        
        
        ## ToDo: Check if this is necessary
        avatar.disableAllConstraints()
        bpy.context.view_layer.update() # Maybe only usefull with life-preview
        for b in avatar.object.pose.bones: # implementation details!
            b.rotation_mode="QUATERNION"
            b.rotation_quaternion = Quaternion((1,0,0,0))
            b.location = Vector((0,0,0))
        
        
        # apply posture
        bpy.context.view_layer.update() # Maybe only usefull with life-preview
        avatar.ApplyMAvatarPostureValues(postureValues.PostureData)
        bpy.context.view_layer.update() # Maybe only usefull with life-preview
        
        # End ToDo: Check if this is necessary
        
        # sort constraint befor application            
        constraints = sorted(constraints, key=_constraintweight)
        
        # initialization
        success = True
        error = [float('nan')] * len(constraints)

        # check whether both hands are constrained:
        LeftWrist = False
        RightWrist = False
        for constraint in constraints:
            if not constraint.JointConstraint is None:
                if constraint.JointConstraint.JointType == MJointType.LeftWrist:
                    LeftWrist = True
                elif constraint.JointConstraint.JointType == MJointType.RightWrist:
                    RightWrist = True
        if not (LeftWrist and RightWrist) and LeftWrist:
            logger.debug("Only RightWrist set")
            avatar.FixAtCurrentPosititionRotation("RightWrist")
        elif not (LeftWrist and RightWrist) and RightWrist:
            avatar.FixAtCurrentPosititionRotation("LeftWrist")
            logger.debug("Only LeftWrist set")
        elif (LeftWrist and RightWrist):
            logger.debug("Both Wrist set")
        else:
            logger.debug("No Wrist set")
        
        for constraint in constraints:
            if constraint.JointConstraint is None: 
                continue
                
            success *= _applyJointConstraint(avatar, constraint)
            
        logger.debug("Checking results.")
        for idx, constraint in enumerate(constraints):
            if constraint.JointConstraint is None: 
                continue
                
            suc, L1err = _checkJointConstraint(avatar, constraint)
            
        bpy.context.view_layer.update()
        # read posture values from blender rig
        newAvatarPval             = MAvatarPostureValues()
        newAvatarPval.AvatarID    = postureValues.AvatarID
        newAvatarPval.PostureData = avatar.ReadMAvatarPostureValues()        
        
        logger.debug("CalculateIKPosture %i done. Success: %s", self._IKcounter, success)
        self._IKcounter += 1
        result =  MIKServiceResult(newAvatarPval, MBoolResponse(success), error)
        #print(result)
        return result
        
    def ComputeIK(self, avatarPval: MAvatarPostureValues, MIKprops: List[MIKProperty]) -> MAvatarPostureValues:
        """
        Implementation for <MInverseKinematicsService>: The method computes a 
        novel posture based on the given MIKProperties. In particular, the 
        posture values of the resulting posture are returned.
        
        Arguments:
         - avatarPval  MAvatarPostureValues
         - MIKprops    MIKproperty
        
        Returns:
        - newAvatarPval MAvatarPostureValues
        """
        logger.debug("Call to ComputeIK [%i]", self._IKcounter)
        
        # Set the avatar's initial position to the one indicated by avatarPval
        self.app.disableAllConstraints()
        bpy.context.view_layer.update() # Maybe only usefull with life-preview
        for b in self.app.object.pose.bones: # implementation details!
            b.rotation_mode="QUATERNION"
            b.rotation_quaternion = Quaternion((1,0,0,0))
            b.location = Vector((0,0,0))
            
        # makes sure, that position is set before rotation
        MIKprops.sort(key=attrgetter('OperationType'))
            
        bpy.context.view_layer.update() # Maybe only usefull with life-preview
        self.app.ApplyMAvatarPostureValues(avatarPval.PostureData)
        bpy.context.view_layer.update() # Maybe only usefull with life-preview
        
        # check whether both hands are constrained:
        LeftWrist = False
        RightWrist = False
        for MIKelement in MIKprops:
            joint_id = MIKelement.Target # "LeftWrist" # 
            if not joint_id is None:
                if joint_id == MEndeffectorType.LeftHand:
                    LeftWrist = True
                elif joint_id == MEndeffectorType.RightHand:
                    RightWrist = True
        if not (LeftWrist and RightWrist) and LeftWrist:
            logger.debug("Only RightWrist set")
            self.app.FixAtCurrentPosititionRotation("RightWrist")
        elif not (LeftWrist and RightWrist) and RightWrist:
            self.app.FixAtCurrentPosititionRotation("LeftWrist")
            logger.debug("Only LeftWrist set")
        elif (LeftWrist and RightWrist):
            logger.debug("Both Wrist set")
        else:
            logger.debug("No Wrist set")

        # For each MIKProps, add the corresponding constraint
        for MIKelement in MIKprops:
            weight   = MIKelement.Weight
            values   = MIKelement.Values
            joint_id = MEndeffectorType._VALUES_TO_NAMES[MIKelement.Target] # "LeftWrist" # 
            OpType   = MIKOperationType._VALUES_TO_NAMES[MIKelement.OperationType]
        
            # Add IK constraints to skeleton bones. If blender automatically changes the posture, obtain
            if OpType == 'SetPosition':
                t   = Vector()
                t.x = -values[0]
                t.y = values[1]
                t.z = values[2]
                logger.debug("Asking for position %f, %f, %f", t.x, t.y, t.z)
                self.app.AddPositionConstraint(joint_id, t)
                
            elif OpType == 'SetRotation':
                q   = Quaternion()
                q.w = -values[3]
                q.x = -values[0]
                q.y = values[1]
                q.z = values[2]
                logger.debug("Asking for Quaternion %f, %f, %f, %f", q.w, q.x, q.y, q.z)
                self.app.AddRotationConstraint(joint_id, q)
                
        
        # This step is needed if Blender doesn't compute the new position as constraints are added
        self.app.solveIK() # pass
        bpy.context.view_layer.update()
                
        # read posture values from blender rig
        newAvatarPval             = MAvatarPostureValues()
        newAvatarPval.AvatarID    = avatarPval.AvatarID
        newAvatarPval.PostureData = self.app.ReadMAvatarPostureValues()
        
        # Reset the constraints on the avatar posture
        # debugMsg += self.app.CheckIKConstraintStatus()
        bpy.context.view_layer.update()
        logger.debug("ComputeIK %i done.", self._IKcounter)
        self._IKcounter += 1
        # time.sleep(1)
        return newAvatarPval
        
def _applyJointConstraint(avatar, constraint: MJointConstraint) -> bool:
    
    joint_id = MJointType._VALUES_TO_NAMES.get(constraint.JointConstraint.JointType, "Undefined")
    joint_id = MJointType._VALUES_TO_NAMES.get(constraint.JointConstraint.JointType, "Undefined")
    if joint_id == "Undefined":
        raise ValueError("Can't apply JointConstraint to undefined joint")
    
    geo = constraint.JointConstraint.GeometryConstraint
    if geo is None:
        logger.debug("IK-Service can only apply MGeometryConstraints.")
        raise ValueError('No GeometryConstraint!')
    
    if geo.ParentToConstraint is not None:
        logger.debug(geo.ParentToConstraint)
        pos = geo.ParentToConstraint.Position
        rot = geo.ParentToConstraint.Rotation
        avatar.AddPositionConstraint(joint_id, Vector([-pos.X, pos.Y, pos.Z]))
        avatar.AddRotationConstraint(joint_id, Quaternion([-rot.W, -rot.X, rot.Y, rot.Z]))
    else:
        translation = geo.TranslationConstraint
        if translation is not None:
            mCenter = convert.interval3Center(translation.Limits)
            bCenter = convert.vector_m2b(mCenter)
            avatar.AddPositionConstraint(joint_id, bCenter)
            
        rotation = geo.RotationConstraint
        if rotation is not None:
            mCenter = convert.interval3Center(rotation.Limits)
            bCenter = convert.rotation_m2b(mCenter)
            avatar.AddRotationConstraint(joint_id, bCenter)
        
    return True
    
def _checkJointConstraint(avatar, constraint: MJointConstraint) -> (bool, float):
    
    success = 1
    error = 0.
    joint_id = MJointType._VALUES_TO_NAMES.get(
        constraint.JointConstraint.JointType, "Undefined")
    
    geo = constraint.JointConstraint.GeometryConstraint
    if geo is None: 
        raise ValueError('No GeometryConstraint!')
    
    translation = geo.TranslationConstraint
    if translation is not None:
        
        pos = avatar.getJointPosition(joint_id)
        L1err = check.translationconstraint(
            convert.bPosition_to_MVector(pos), 
            translation.Limits, 
            translation.Type
        )
        error = L1err
        success *= True if L1err==0. else False
        
    rotation = geo.RotationConstraint
    if rotation is not None:
        bRot = avatar.getJointRotation(joint_id)
        rot = convert.euler_b2m(bRot.to_euler('XZY'))
        L1err = check.rotationconstraint(rot, rotation.Limits)
        success *= True if L1err==0. else False
        
    return bool(success), error
    
def _is_applicable(c: MConstraint) -> bool:
    return c.JointConstraint is not None and c.JointConstraint.GeometryConstraint is not None
    
def _constraintweight(constraint: MConstraint) -> int:
    if constraint.JointConstraint.GeometryConstraint.TranslationConstraint is not None:
        return 1
    else:
        return 2