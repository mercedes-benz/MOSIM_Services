import bpy
from mathutils import * 
import math


src = "lalala"
trg = "asdf363"


def CopyEditBones(src, trg):
    bpy.context.view_layer.objects.active = src
    bpy.context.view_layer.update()
    bpy.ops.object.mode_set(mode="EDIT", toggle = False)
    
    bpy.context.view_layer.update()
    
    
    edit_bones = []
    
    for eb in src.data.edit_bones:
        bc = {
            "name":eb.name,
            "parent":"" if eb.parent is None else eb.parent.name,
            "roll":eb.roll,
            "tail":eb.tail + Vector(),
            "head":eb.head + Vector(),
            "tail_radius":eb.tail_radius,
            "use_connect":eb.use_connect,
            "use_cyclic_offset":eb.use_cyclic_offset,
            "use_deform":eb.use_deform,
            "use_endroll_as_inroll":eb.use_endroll_as_inroll,
            "use_envelope_multiply":eb.use_envelope_multiply,
            "use_inherit_rotation":eb.use_inherit_rotation,
            "use_inherit_scale":eb.use_inherit_scale,
            "use_local_location":eb.use_local_location,
            "use_relative_parent":eb.use_relative_parent,
        }
        edit_bones.append(bc)
    bpy.ops.object.mode_set(mode="OBJECT", toggle=False)
    bpy.context.view_layer.objects.active = trg
    bpy.ops.object.mode_set(mode="EDIT", toggle = False)
    bpy.context.view_layer.update()    
    
    for eb in edit_bones:
        if not eb["name"] in trg.data.edit_bones:
            nb = trg.data.edit_bones.new(eb["name"])
            if eb["parent"] != "":
                nb.parent = trg.data.edit_bones(eb["parent"])
            nb.roll = eb["roll"]
            nb.tail = eb["tail"]
            nb.head = eb["head"]
            nb.tail_radius = eb["tail_radius"]
            nb.use_connect = eb["use_connect"]
            nb.use_cyclic_offset = eb["use_cyclic_offset"]
            nb.use_deform = eb["use_deform"]
            nb.use_endroll_as_inroll = eb["use_endroll_as_inroll"]
            nb.use_envelope_multiply = eb["use_envelope_multiply"]
            nb.use_inherit_rotation = eb["use_inherit_rotation"]
            nb.use_inherit_scale = eb["use_inherit_scale"]
            nb.use_local_location = eb["use_local_location"]
            nb.use_relative_parent = eb["use_relative_parent"]
    bpy.ops.object.mode_set(mode="OBJECT", toggle=False)

def CopyConstraints(src, trg):
    for b in src.pose.bones:
        for c in b.constraints:
            print("constraint ", c, b.name)
            tc = trg.pose.bones[b.name].constraints.new( c.type )
            
            for prop in dir(c):
                try:
                    cProp = getattr(c, prop)
                    if type(cProp) in [type(str()), type(float()), type(int()), type(bool())]:
                        print("  ", prop, ": ", cProp)
                        setattr(tc, prop, cProp)
                    elif type(cProp) == type(src):
                        print("    object: ")
                        if cProp == src:
                            print(" self reference replaced")
                            setattr(tc, prop, trg)
                        else:
                            setattr(tc, prop, cProp)
                    else:
                        print(" ignored: ", prop, ": ", cProp)
                except:
                    print("except ", prop)
                    pass

def CopyBoneSettings(src, trg):
    for b in src.pose.bones:
        tb = trg.pose.bones[b.name]
        print("bone: ", b)
        for prop in dir(b):
            try:
                cProp = getattr(b, prop)
                if prop == "name" or prop.startswith("__"):
                    continue
                if type(cProp) in [type(str()), type(float()), type(int()), type(bool())]:
                    print("  ", prop, ": ", cProp)
                    setattr(tb, prop, cProp)
                elif type(cProp) == type(b.lock_rotation):
                    setattr(tb, prop, cProp)
                    for i in range(len(cProp)):
                        print(" array group: ", prop, cProp[i])
                #else:
                    #print(" ignored: ", prop, ": ", cProp)
            except:
                print("except: ", prop)
    
#CopyEditBones(bpy.data.objects[src], bpy.data.objects[trg])
#CopyConstraints(bpy.data.objects[src], bpy.data.objects[trg])
#CopyBoneSettings(bpy.data.objects[src], bpy.data.objects[trg])