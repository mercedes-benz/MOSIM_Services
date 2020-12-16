import bpy
from mathutils import * 
import math

import json



# relevant to set path properly for imports to function
#import sys
#print(bpy.path.abspath("//"))
#sys.path.append(bpy.path.abspath("//"))

from MMIStandard.avatar.ttypes import MAvatarPosture, MJoint, MJointType, MChannel
from MMIStandard.math.ttypes import MQuaternion, MVector3

#from BlenderMMI.IntermediateSkeletonApplication import IntermediateSkeletonApplication

#--> MQuaternion(q.x, q.z, q.y, -q.w)
#--> MVector3(v.x, v.z, v.y)

def MVector2Vector(v):
    #return Vector((v.X, v.Z, v.Y))
    return Vector((-v.X, v.Y, v.Z))

def MQuaternion2Quaternion(q):
    #return Quaternion((-q.W, q.X, q.Z, q.Y))
    return Quaternion((-q.W, -q.X, q.Y, q.Z))

def ID2MChannel(id):
    if(id == 0):
        return MChannel.XOffset
    if(id == 1):
        return MChannel.YOffset
    if(id == 2):
        return MChannel.ZOffset
    if(id == 3):
        return MChannel.XRotation
    if(id == 4):
        return MChannel.YRotation
    if(id == 5):
        return MChannel.ZRotation
    if(id == 6):
        return MChannel.WRotation
    
def JSON2MAvatarPosture(filepath):
    data = {}
    with open(filepath, "r") as f:
        data = json.load(f)
    name = data["AvatarID"]
    jointlist = []
    for j in data["Joints"]:
        jname = j["ID"]
        jtype = j["Type"]
        jpos = MVector3(j["Position"]["X"], j["Position"]["Y"], j["Position"]["Z"])
        jrot = MQuaternion(j["Rotation"]["X"], j["Rotation"]["Y"], j["Rotation"]["Z"], j["Rotation"]["W"])
        channellist = [ID2MChannel(x) for x in j["Channels"]]
        
        mj = MJoint(jname, jtype, jpos, jrot, channellist, j["Parent"])
        jointlist.append(mj)
        
    return MAvatarPosture(name, jointlist)
"""
def JSON2MAvatarPosture(filepath):
    data = {}
    with open(filepath, "r") as f:
        data = json.load(f)
    name = data["1"]["str"]
    jointlist = []
    joint_data = data["2"]["lst"]
    for i in range(2, len(data["2"]["lst"])):
        
        bonename = joint_data[i]["1"]["str"]
        bonetype = joint_data[i]["2"]["i32"]
        #bonetype = MJointType._VALUES_TO_NAMES[bonetype]
        
        vd = joint_data[i]["3"]["rec"]
        vector = MVector3(vd["1"]["dbl"], vd["2"]["dbl"], vd["3"]["dbl"])


        qd = data["2"]["lst"][i]["4"]["rec"]
        quat = MQuaternion(qd["1"]["dbl"], qd["2"]["dbl"], qd["3"]["dbl"], qd["4"]["dbl"])
        
        channel_data = joint_data[i]["5"]["lst"]
        channels = [x for x in channel_data[2:]]
        
        parent = None if not '6' in joint_data[i] else joint_data[i]['6']["str"]

        jointlist.append(MJoint(bonename, bonetype, vector, quat, channels, parent))
    return MAvatarPosture(name, jointlist)
"""
def GetJointByName(list, name):
    for j in list:
        if j.ID == name:
            return j
    return null

def FindProjection(x, y, z):
    max = 0
    if (abs(y.y) > abs(y.x) and abs(y.y) > abs(y.z)):
        max = 1
    elif (abs(y.z) > abs(y.x) and abs(y.z) > abs(y.y)):
        max = 2
    
    v1 = Vector((x.y, x.z)).normalized()
    v2 = Vector((z.y, z.z)).normalized()
    if max == 1:
        v1 = Vector((x.x, x.z)).normalized()
        v2 = Vector((z.x, z.z)).normalized()
    elif max == 2:
        v1 = Vector((x.x, x.y)).normalized()
        v2 = Vector((z.x, z.y)).normalized()
    
    return (v1, v2)

def CreateArmature(posture):
    armature = bpy.data.armatures.new(posture.AvatarID)
    o = bpy.data.objects.new(posture.AvatarID, armature)
    bpy.context.scene.collection.objects.link(o)
    
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.mode_set(mode="EDIT", toggle=False)
    
    edit_bones = armature.edit_bones
    ###
    # create edit bones
    ###
    for j in posture.Joints:
        b = edit_bones.new(j.ID)
        print("new bone: ", b.name)
        if not j.Parent is None:
            parent_bone = edit_bones[j.Parent]
            
            print(parent_bone)
            edit_bones[j.ID].parent = edit_bones[j.Parent]    

        #b.use_relative_parent = True
        b.use_local_location = True
        b.use_inherit_rotation = True
        b.use_deform = True
        b.use_inherit_scale
        b.head = Vector((0,0,0))
        b.tail = Vector((0,0.01,0))

    ###
    # position head
    ###
    for j in posture.Joints:
        b = edit_bones[j.ID]
        rotation = MQuaternion2Quaternion(j.Rotation)
        position = MVector2Vector(j.Position)
        
        if not j.Parent is None:
            parent = GetJointByName(posture.Joints, j.Parent)
            parRot = parent.Rotation
            rotation = parRot @ rotation
            position = parRot @ position + parent.Position
        
        j.Position = position
        j.Rotation = rotation
        
        m = rotation.to_matrix().to_4x4()
        m.translation = position
        
        b.matrix = m
        #bpy.context.view_layer.update()
        #b.tail = position
    for j in posture.Joints:
        b = edit_bones[j.ID]
        if not j.Parent is None:
            bP = edit_bones[j.Parent]
            distance = 0.1
            if len(bP.children) == 1:
                distance = abs((b.head - bP.head).length)
            if distance <= 0:
                distance = 0.1
            bP.tail = bP.head + (bP.tail - bP.head).normalized() * distance
        """    
        #parent_rotation = Quaternion() if j.Parent is None else Quaternion(b.parent["globalRot"])
        
        rotation = parent_rotation @  MQuaternion2Quaternion(j.Rotation)
        b["globalRot"] = rotation
        
        parent_pos = Vector() if j.Parent is None else b.parent.head
        
        
        #parent_matrix = Matrix() if j.Parent is None else b.parent.matrix.inverted()
        head_pos = parent_pos + parent_rotation @ MVector2Vector(j.Position)
        m = rotation.to_matrix().to_4x4()
        m.translation = head_pos
        b.matrix = m
        
        #b.head = parent_pos + parent_rotation @ MVector2Vector(j.Position)
        """
    """
    ###
    # position tail
    ### 
    for j in posture.Joints:
        b = edit_bones[j.ID]
        if len(b.children) == 0:
            b.tail = b.head + Quaternion(b.parent["globalRot"]) @ Vector((0,0.01,0))
        else:
            b.tail = b.children[0].head
            
        print(b.name, b.matrix, b.matrix.to_quaternion(), Quaternion(b["globalRot"]), j.Rotation)

        #b.head = MVector2Vector(j.Position)
        
        #b.tail = b.head + (MQuaternion2Quaternion(j.Rotation) @ Vector((0,0,0.1)))

    ###
    # compute matrix 
    ###
    for j in posture.Joints:
        b = edit_bones[j.ID]
        
        m = Quaternion(b["globalRot"]).to_matrix().to_4x4()
        m.translation = b.head
        #b.matrix = m
        bpy.context.view_layer.update()
    """
            
    """
    for j in posture.Joints:
        b = edit_bones[j.ID]
        xaxis = b.x_axis
        yaxis = b.y_axis
        zaxis = b.z_axis
        
        
        
        xtarget = Vector((1,0))
        ztarget = Vector((0,1))
        if(b.parent):
            m = b.parent.matrix
            m.translation = Vector((0,0,0))
            #xaxis = m.inverted() @ xaxis
            #zaxis = m.inverted() @ zaxis
            #yaxis = m.inverted() @ yaxis
            
            #rot = (MQuaternion2Quaternion(j.Rotation).to_matrix() @ Matrix().to_3x3())
            rot = Quaternion(b["globalRot"]).to_matrix() @ Matrix().to_3x3()
            print(b.name, xaxis, zaxis, rot.col[0], rot.col[2])
            xtarget, ztarget = FindProjection(rot.col[0], rot.col[1], rot.col[2])
        

        (xaxis, zaxis) = FindProjection(xaxis, yaxis, zaxis)
        anglez = zaxis.angle_signed(ztarget)
        anglex = xaxis.angle_signed(xtarget)
        print(b.name, " x: ", xaxis, " ", xtarget, " angle: ", anglex )
        print(b.name, " z: ", zaxis, " ", ztarget, " angle: ", anglez )
        print("")
        b.roll = -anglez
        bpy.context.view_layer.update()
            
        """
    bpy.ops.object.mode_set(mode="OBJECT", toggle=False)    
    return o


def ApplyMAvatarPostureValues(o, posture, values):
    i = 0
    for j in posture.Joints:
        pb = o.pose.bones[j.ID]
        
        t = Vector()
        q = Quaternion()
        
        for c in j.Channels:
            if c == MChannel.XOffset:
                t.x = -values[i]
            elif c == MChannel.YOffset:
                t.y = values[i]
            elif c == MChannel.ZOffset:
                t.z = values[i]
                
            elif c == MChannel.WRotation:
                q.w = -values[i]
            elif c == MChannel.XRotation:
                q.x = -values[i]
            elif c == MChannel.YRotation:
                q.y = values[i]
            elif c == MChannel.ZRotation:
                q.z = values[i]
            i += 1
        pb.rotation_quaternion = q
        pb.location = t
        
        
def ReadMAvatarPostureValues(o, posture):
    data = []
    
    for j in posture.Joints:
        pb = o.pose.bones[j.ID]
        t = pb.location
        q = pb.rotation_quaternion

        for c in j.Channels:
            if c == MChannel.XOffset:
                data.append(-t.x)
            elif c == MChannel.YOffset:
                data.append(t.y)
            elif c == MChannel.ZOffset:
                data.append(t.z)
                
            elif c == MChannel.WRotation:
                data.append(-q.w)
            elif c == MChannel.XRotation:
                data.append(-q.x)
            elif c == MChannel.YRotation:
                data.append(q.y)
            elif c == MChannel.ZRotation:
                data.append(q.z)
    return data


#path = bpy.path.abspath("//new_intermediate.mos")
#posture = JSON2MAvatarPosture2(path)

# uncomment this line to create armature
#o = CreateArmature(posture)
