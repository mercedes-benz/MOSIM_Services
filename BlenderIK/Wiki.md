# Blender IK Service

## Overall Data
| Property             |  Values |
| --- | ------ |
| Name                 | BlenderIKService |
| Nature               | Service to calculate a natural posture for a given sceleton and set of constraints. |
| Programming Language | Python (Blender) |
| Development Tools    | Blender |
| Additional Libraries | - |
| Databases            | - |
| License              | - |

## Functionalities

The Service holds a representation of a sceleton. The consumer provides the current posture and a set of constraints. The service then applies the poster to the internal representation and tries to modify this posture, so that the constrains are satisfied. 

The consumer can now focus on key-parts of the posture (eg. position and rotation of hands and feet) and delegate the calculation of a natural pose of all remaining joints to this service. 

![Wiki_05](uploads/f2439e77db1fe5b87f114165c05cc884/Wiki_05.PNG)

## Interface

At the moment the service supports two functions to consume the service. **ComputeIK** only exists for backwards-compatibility and will be removed in the final release. For new projects, always use the new interface-function **CalculateIKPosture**.

```thrift
//IKService definition
service MInverseKinematicsService extends MMIServiceBase
{
        //Legacy support
    scene.MAvatarPostureValues ComputeIK(1: scene.MAvatarPostureValues postureValues, 2: list<MIKProperty> properties),

        //New method
    MIKServiceResult CalculateIKPosture(1: scene.MAvatarPostureValues postureValues, 2: list<mmu.MConstraint> constraints, 3: map<string,string> properties),
}

struct MIKServiceResult
{
    1: required scene.MAvatarPostureValues Posture;
    2: required bool Success;
    3: required list<double> Error;
}

```

### Examples

The service expects a complete MAvatarPosture as the first Argument. Typically this is the posture from the last produced frame. The second argument is a list of MConstraint. There are 4 effectors these constraints may target: RightWrist, LeftWrist, RightAnkle and LeftAnkle; all from MJointType. In the current incarnation of the service, only these 4 Joints can be addressed.

As an example, the consumer can use the MHandpose from the GraspPointService to build the constraint. It must be noted, that the ikservice has no notion of time. It is up to the consumer to ensure, that the resulting posture is close enough to the last posture, to prevent the Avatar from *jumping* from one pose to the next.

As the first part of the result, the service calculates a posture, which satisfies the constrains and looks as natural as possible. This is not garanteed to succeed. Therefore the return-value contains a success-flag to indicate, weather all constraints are meet. The third part of the result contains a list with the L1-norm of the positioning-error for every constraint (NaN if the constraint was satisfied). This enables the consumer to decide, weather it should keep trying, or change to another strategy.

![Wiki_02](uploads/2dabe0747cc6d76c2fc4a246077d1eb0/Wiki_02.PNG)

![Wiki_03](uploads/da76a4f02712944ac14200190063d5b4/Wiki_03.PNG)

![Wiki_01](uploads/cbf14badc35ca7f30ec01e2957f6b882/Wiki_01.PNG)

### Known Issues

- The center of the sceleton will never leave ist place. This ensures, that a faulty constraint cannot *move* the avatar across the scene. But it can also lead to an never-ending task, if the consumer does not analize the success-flag and react accordingly. Especially the stable MMU, which still consume the legacy-function **ComputeIK** could be easily traped with this problem.

- If the service does not succeed, there is no channel to communicate why it failed. The success-flag indicates only, weather all constrains could be satisfied. The service misses the means to indicate that an error occured, e.g. because the avatar-ID is unknown to the service. A proposal for the next iteration of the interface already addresses this issue.

![Wiki_06](uploads/f25be2a92a1d46ce370029f35253fd98/Wiki_06.png)

## Technical Information

The service is implemented as an python-module. This module can be integrated in a Blender-distribution via the integrated pip-command. Modul expects the Apache-Thrift-Module to be installed in the same way. The service must be started by invocing Blender with the right *.blend.file (see below) and the script to start the service. A batch-file is provided to call the correct startup-command.

``` bat
Blender\blender.exe resources\IKService_dennis.blend --background --python main.py -- run %*
```

The sceleton must be prepeared in a Blender-File prior to usage. It is sufficient to generate the armatures from the T-pose. For a natural pose, Blender needs *pole-targets* to ensure the correct bending of joints. Also all constraints about maximum and minimum-rotations of specific joints are encoded in that file.

The service uses *ik-targets* to apply the constraints on the armature. These targets are named by convention as JointType+'IK' (eg: RightWristIK to manipulate the RightWrist-Joint). The targets are only active, if an associated constraint is given. Translation and rotation for a Joint must be provided in separate constraints. The service will always begin with positional constraints.

## Licensing

Please note, that the final licensing of this service is still subject to discussion! The following assessment is my personal interpretation without judical knowledge.

As the service is de-facto an addon to Blender, the license of Blender applies to the service. This means, no restrictions can be placed on sharing the code. See also: [Blender.org](https://www.blender.org/about/license/).

If the server is distributed with Blender as one unit, it is a modified version of Blender. Therefore *GPL-v2 or later* applies. Since this bundle would also contain Thrift, which is licensed under Apache-V2, the *or-later*-clause is triggert and the whole distribution falls under the GPL-V3, as V2 is incompatible with Apache-V2.

Therefore the license of this software cannot be in violation of GPL-V2. This leaves MIT, 3-clause-BSD, lgpl and gpl-V3 as an option.