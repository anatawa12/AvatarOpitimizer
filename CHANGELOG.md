# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog].

[Keep a Changelog]: https://keepachangelog.com/en/1.1.0/

## [Unreleased]
### Added

### Changed

### Deprecated

### Removed

### Fixed
- VRM: Fix BlendShape mapping [`#1375`](https://github.com/anatawa12/AvatarOptimizer/pull/1375)

### Security

## [1.8.3] - 2024-12-24
### Fixed
- Prefab overrides on the scene are reverted on first load of the scene at first launch [`#1372`](https://github.com/anatawa12/AvatarOptimizer/pull/1372)
- Animating transform with C# named properties are broken by merge bone [`#1373`](https://github.com/anatawa12/AvatarOptimizer/pull/1373)
  - Animator window won't create such animation but some script generates and it works surprisingly
- Errors with blendShapes with exactly same name in a mesh [`#1374`](https://github.com/anatawa12/AvatarOptimizer/pull/1374)
  - Such mesh can be generated with Autodesk Maya or 3ds Max
  - Unity API denies generating such mesh with C# so AAO will rename such blendShapes to unique name to support.
  - Unity Animator does animate first blendshale only so second shape would generally removed by remove unused blendShapes.

## [1.8.2] - 2024-12-11
### Added
- `-` button for prefab safe set [`#1368`](https://github.com/anatawa12/AvatarOptimizer/pull/1368)

### Fixed
- Errors with models with UV at very edge [`#1363`](https://github.com/anatawa12/AvatarOptimizer/pull/1363)
- Errors if exactly same AnimatorController is specified for multiple playable layers [`#1366`](https://github.com/anatawa12/AvatarOptimizer/pull/1366)
- Errors if objects removed by some component is listed on exclusions of Trace and Optimize [`#1367`](https://github.com/anatawa12/AvatarOptimizer/pull/1367)
- OverflowException when creating prefab [`#1369`](https://github.com/anatawa12/AvatarOptimizer/pull/1369)

## [1.8.1] - 2024-12-01
### Fixed
- Optimize Texture may break liltoon outline mask [`#1357`](https://github.com/anatawa12/AvatarOptimizer/pull/1357)

## [1.8.0] - 2024-11-30
### Added
- AnyState to Entry/Exit optimization in Optimize Animator [`#1157`](https://github.com/anatawa12/AvatarOptimizer/pull/1157)
  - If AAO found animator layer only with AnyState, AAO tries to convert them to Entry / Exit pattern.
    - Currently due to implementation there are some patterns that can be convert but but not converted.
    - We may relax some restriction in the future.
  - Because we have to check for each condition if we use AnyState but we can check for only one (in best case) with entry/exit, this generally reduces cost for checking an parameter in a state.
  - Combined with Entry / Exit to 1D BlendTree optimization, which is implemented in previous release, your AnyState layer may be optimized to 1D BlendTree.
- Optimize Texture in Trace nad Optimize [`#1181`](https://github.com/anatawa12/AvatarOptimizer/pull/1181) [`#1184`](https://github.com/anatawa12/AvatarOptimizer/pull/1184) [`#1193`](https://github.com/anatawa12/AvatarOptimizer/pull/1193) [`#1215`](https://github.com/anatawa12/AvatarOptimizer/pull/1215) [`#1225`](https://github.com/anatawa12/AvatarOptimizer/pull/1225) [`#1235`](https://github.com/anatawa12/AvatarOptimizer/pull/1235) [`#1268`](https://github.com/anatawa12/AvatarOptimizer/pull/1268) [`#1278`](https://github.com/anatawa12/AvatarOptimizer/pull/1278) [`#1313`](https://github.com/anatawa12/AvatarOptimizer/pull/1313) [`#1328`](https://github.com/anatawa12/AvatarOptimizer/pull/1328) [`#1338`](https://github.com/anatawa12/AvatarOptimizer/pull/1338) [`#1334`](https://github.com/anatawa12/AvatarOptimizer/pull/1334) [`#1346`](https://github.com/anatawa12/AvatarOptimizer/pull/1346)
  - Avatar Optimizer will pack texture and tries to reduce the VRAM usage.
  - Currently liltoon is only supported.
- `Copy Enablement Animation` to Merge Skinned Mesh [`#1173`](https://github.com/anatawa12/AvatarOptimizer/pull/1173)
  - This feature copies activeness / enablement animation from merge target renderers to the merged renderer.
  - This feature is not enabled by default. You have to enable it in the inspector.
  - This feature supports copying activeness animation of `activeSelf` of the GameObjects or ancestors of the GameObjects.
    However, this feature does not work if multiple GameObjects (or both GameObject and Renderer itself) are animated.
  - In addition, this feature will be animate the `enabled` of the merged renderer, so you must not animate the `enabled` of the merged renderer.
    - If animations are unsupported, AAO will show an error message and abort the build.
- Support Read/Write disabled Meshes with Av3Emulator Enabled [`#1185`](https://github.com/anatawa12/AvatarOptimizer/pull/1185)
  - Previously, AAO cannot process meshes with Read/Write disabled if AAO is triggered by Av3Emulator.
  - Since this release, AAO can process meshes with Read/Write disabled if AAO is triggered by Av3Emulator.
  - In addition, AAO now supports non-Float32 vertex buffers.
    - We still use Float32 internally so Int32 data might lose precision a little.
    - However, AFAIK there is no real-world problem with this so we implemented this way.
    - If you found such a case, please report it.
  - This change make AAO incompatible with Unity without Graphics.
    - If you're building your avatar with batchmode with -nographics, please remove -nographics.
- Asset Description for Avatar Modify Support bundled in an avatar, Shinano [`#1189`](https://github.com/anatawa12/AvatarOptimizer/pull/1189)
- API to get in advance whether a polygon will be removed [`#1177`](https://github.com/anatawa12/AvatarOptimizer/pull/1177)
- Rename BlendShape component to rename BlendShapes [`#1245`](https://github.com/anatawa12/AvatarOptimizer/pull/1245) [`#1296`](https://github.com/anatawa12/AvatarOptimizer/pull/1296)
  - This can be used to avoid blendShape name conflicts in Merge Skinned Mesh
- Invert option for Remove Mesh in Box [`#1257`](https://github.com/anatawa12/AvatarOptimizer/pull/1257)
  - You now can remove polygons outside of the box instead of inside the box.
  - Along with this new feature, we renamed `Remove Mesh in Box` to `Remove Mesh By Box` to make it more clear.
    - This doesn't change the class name of the component since it's already a part of the public API.
- Remove Mesh By UV Tile, a new way to remove polygons [`#1263`](https://github.com/anatawa12/AvatarOptimizer/pull/1263)
  - You now easily remove some polygons of models configured for UV Tile Discard.
  - This component removes polygons like UV Tile Discard with Vertex Discard Mode.
- Automatically remove unnecessary material properties based on shader [`#1041`](https://github.com/anatawa12/AvatarOptimizer/pull/1041)
  - This feature is added to `Remove Unused Objects` in `Trace and Optimize`.
  - When you changed shader for an material, properties for previously used shaders might be remain
  - This may increase your avatar size by unexpectedly including unused textures
- Right-click menu option to create a new GameObject with a specified component [`#1290`](https://github.com/anatawa12/AvatarOptimizer/pull/1290)
- Automatically Merge Blendshape [`#1300`](https://github.com/anatawa12/AvatarOptimizer/pull/1300) [`#1324`](https://github.com/anatawa12/AvatarOptimizer/pull/1324)
  - This is new automatic optimization in Trace and Optimize
  - This is a part of "Optimize BlendShape" optimization.
  - AAO 1.8.0 introduced BlendShape support for Merge Skinned Mesh, but new default mode "Rename to avoid conflicts" would increase number of BlendShape.
  - This feature is added to relax this problem by automatically merging multiple BlendShapes of one Mesh.
  - With this feature, you can use rename mode without performance loss.
- Fix mode for PhysBone Limits in Merge PhysBone [`#665`](https://github.com/anatawa12/AvatarOptimizer/pull/665) [`#1336`](https://github.com/anatawa12/AvatarOptimizer/pull/1336) [`#1345`](https://github.com/anatawa12/AvatarOptimizer/pull/1345)
  - In addition to existing `Copy` and `Override`, we added `Fix` mode.
  - This mode will try to correct roll axis by rotating bone.
  - This feature allows you to configure the mode for PhysBone Limits in Merge PhysBone.
  - This is useful if all configuration is same but roll axis is different.
- Automatically merging meshes which have BlendShapes [`#1308`](https://github.com/anatawa12/AvatarOptimizer/pull/1308)
  - In previous version of Avatar Optimizer, meshes which have BlendShapes are not automatically merged.
  - This was because BlendShape manipulation load is proportional to the number of vertices in Unity 2019.
  - However, in Unity 2020 and later, BlendShape manipulation load is mostly proportional to the number of moving vertices.
  - This means that increasing the number of vertices in a mesh which has BlendShapes does not increase the load of BlendShape manipulation much.
  - Therefore, we decided to automatically merge such meshes.
- Improved OSC Gimmick Support [`#1306`](https://github.com/anatawa12/AvatarOptimizer/pull/1306) [`#1351`](https://github.com/anatawa12/AvatarOptimizer/pull/1351)
  - We added two information for OSC Gimmick in Asset Description.
  - By defining parameters read / written by OSC Gimmick, your OSC Gimmick no longer breaks.
- Automatically Merge Material Slot [`#1334`](https://github.com/anatawa12/AvatarOptimizer/pull/1334)
  - If you have multile material slots with same material, it will be merged automatically.

### Changed
- Skip Enablement Mismatched Renderers is now disabled by default [`#1169`](https://github.com/anatawa12/AvatarOptimizer/pull/1169)
  - You still can enable it in the Inspector.
  - This change does not affect the behavior of previously added components.
- Use UInt16 index buffer if possible even when total vertex count is more than 2^16 [`#1178`](https://github.com/anatawa12/AvatarOptimizer/pull/1178) [`#1255`](https://github.com/anatawa12/AvatarOptimizer/pull/1255)
  - With baseVertex in index buffer, we can use UInt16 index buffer even if total vertex count is more than 2^16.
  - Of course, if one submeh references wide range of vertices, we cannot use UInt16 index buffer so we still use UInt32 index buffer in such a case.
- Reimplement Preview system with NDMF Preview System [`#1131`](https://github.com/anatawa12/AvatarOptimizer/pull/1131) [`#1195`](https://github.com/anatawa12/AvatarOptimizer/pull/1195) [`#1218`](https://github.com/anatawa12/AvatarOptimizer/pull/1218) [`#1270`](https://github.com/anatawa12/AvatarOptimizer/pull/1270)
  - This will prevent issues relates to Animation Mode bug.
  - This allows you to preview Remove Mesh components without selecting Mesh OR while in Animation Mode.
- Improved Prefab Safe Set, which are used in MergePhysBone, MergeSkinnedMesh, FreezeBlendShape and more components [`#1212`](https://github.com/anatawa12/AvatarOptimizer/pull/1212) [`#1219`](https://github.com/anatawa12/AvatarOptimizer/pull/1219) [`#1221`](https://github.com/anatawa12/AvatarOptimizer/pull/1221) [`#1236`](https://github.com/anatawa12/AvatarOptimizer/pull/1236) [`#1287`](https://github.com/anatawa12/AvatarOptimizer/pull/1287) [`#1294`](https://github.com/anatawa12/AvatarOptimizer/pull/1294) [`#1348`](https://github.com/anatawa12/AvatarOptimizer/pull/1348)
  - This should improve compatibility with replacing base prefab, which is added in Unity 2022.
- Allow multiple component for Remove Mesh components with API [`#1216`](https://github.com/anatawa12/AvatarOptimizer/pull/1216) [`#1218`](https://github.com/anatawa12/AvatarOptimizer/pull/1218)
  - This allows non-destructive tools to add Remove Mesh components even if Remove Mesh component are added before.
- Animator Parser Debug Window now supports ObjectReference animation support [`#1222`](https://github.com/anatawa12/AvatarOptimizer/pull/1222)
- Reimplemented Animator Parser node system [`#1227`](https://github.com/anatawa12/AvatarOptimizer/pull/1227)
- Renamed debug options internally [`#1228`](https://github.com/anatawa12/AvatarOptimizer/pull/1228)
  - This will lose previously configured debug options.
  - However, debug options are not considered as Public API as stated in documents so this is not backward incompatible changes in semver 2.0.0 section 8.
- Performance Improvements [`#1234`](https://github.com/anatawa12/AvatarOptimizer/pull/1234) [`#1243`](https://github.com/anatawa12/AvatarOptimizer/pull/1243) [`#1240`](https://github.com/anatawa12/AvatarOptimizer/pull/1240) [`#1288`](https://github.com/anatawa12/AvatarOptimizer/pull/1288) [`#1304`](https://github.com/anatawa12/AvatarOptimizer/pull/1304) [`#1307`](https://github.com/anatawa12/AvatarOptimizer/pull/1307) [`#1314`](https://github.com/anatawa12/AvatarOptimizer/pull/1314) [`#1325`](https://github.com/anatawa12/AvatarOptimizer/pull/1325) [`#1327`](https://github.com/anatawa12/AvatarOptimizer/pull/1327) [`#1326`](https://github.com/anatawa12/AvatarOptimizer/pull/1326) [`#1341`](https://github.com/anatawa12/AvatarOptimizer/pull/1341)
- Transform gizmo are now hidden while you're editing box of Remove Mesh in Box [`#1259`](https://github.com/anatawa12/AvatarOptimizer/pull/1259)
  - This prevents mistakenly moving the Skinned Mesh Renderer while editing the box.
- Make MergePhysBone implement `INetworkID` [`#1260`](https://github.com/anatawa12/AvatarOptimizer/pull/1260)
  - This allow you to configure networkid for merged PhysBone component
- Changed locale code for simplified chinese from `zh-cn` to `zh-hans` [`#1264`](https://github.com/anatawa12/AvatarOptimizer/pull/1264)
  - This would improve compatibility with other NDMF tools.
  - Many NDMF tools uses `zh-hans` so previously you may see both 中文 (中国) and 中文 (简体).
  - I think zh-hans is more accurate expression so I changed so.
- Improve RemoveMeshByMask compability with Tex Trans Tool [`#1269`](https://github.com/anatawa12/AvatarOptimizer/pull/1269)
- Support for Shaders that depends on vertex index [`#1275`](https://github.com/anatawa12/AvatarOptimizer/pull/1275) [`#1281`](https://github.com/anatawa12/AvatarOptimizer/pull/1281) [`#1285`](https://github.com/anatawa12/AvatarOptimizer/pull/1285)
  - Avatar Optimizer will not automatically merge meshes that are using vertex index
  - since merging them may change vertex order, which changes vertex index
- BlendShape support for Merge Skinned Mesh [`#1286`](https://github.com/anatawa12/AvatarOptimizer/pull/1286) [`#1299`](https://github.com/anatawa12/AvatarOptimizer/pull/1299)
  - You now can successfully merge Meshes with BlendShape with Merge Skinned Mesh.
  - Actually, previous version does not have proper consideration for BlendShape.
  - This version introduces options to select BlendShape behavior in Merge Skinned Mesh.
- Renamed "Automatically Freeze BlendShape" to "Optimize BlendShape" [`#1300`](https://github.com/anatawa12/AvatarOptimizer/pull/1300)

### Removed
- Unity 2019 Support [`#1146`](https://github.com/anatawa12/AvatarOptimizer/pull/1146)
  - For 2019 users, please use 1.7.x.

### Fixed
- Avatar Optimizer does not support `Additive Reference Pose` [`#1208`](https://github.com/anatawa12/AvatarOptimizer/pull/1208)
- Typo in menu for creating Asset Description [`#1213`](https://github.com/anatawa12/AvatarOptimizer/pull/1213)
- maxSquish cannot be configured for mergePB`#1231`
- Avatar Descriptor can be removed by Avatar Optimizer in extreamely rare case [`#1242`](https://github.com/anatawa12/AvatarOptimizer/pull/1242)
- Material property animation with weight 0 layer might be broken with AutoMergeSkinnedMesh [`#1248`](https://github.com/anatawa12/AvatarOptimizer/pull/1248) [`#1253`](https://github.com/anatawa12/AvatarOptimizer/pull/1253)
- Remove Mesh in Box does not work for meshes without Bones [`#1256`](https://github.com/anatawa12/AvatarOptimizer/pull/1256)
- NullReferenceException in `GetBlendShape` if Mesh is not specified for SkinnedMeshRenderer [`#1267`](https://github.com/anatawa12/AvatarOptimizer/pull/1267)
- Animation for target renderer of Merge Skinned Mesh might be overridden by animation for source renderer [`#1276`](https://github.com/anatawa12/AvatarOptimizer/pull/1276)
- BlendShape with same name is impclitly merged in Merge Skinned Mesh [`#1286`](https://github.com/anatawa12/AvatarOptimizer/pull/1286)
  - Now you can rename BlendShape to avoid conflicts.
- NRE if specified expression parameters is None [`#1303`](https://github.com/anatawa12/AvatarOptimizer/pull/1303)
  - This error only happens if you don't use Modular Avatar since Modular Avatar will assign parameters asset.
- Show version name on NDMF Console [`#1309`](https://github.com/anatawa12/AvatarOptimizer/pull/1309)
- Fix non-VRChat project support [`#1310`](https://github.com/anatawa12/AvatarOptimizer/pull/1310)
- 'shader' doesn't have a float or range property 'prop' error [`#1312`](https://github.com/anatawa12/AvatarOptimizer/pull/1312)
- Error if all components are on inactive GameObject [`#1318`](https://github.com/anatawa12/AvatarOptimizer/pull/1318)
- Animation bindings for BoxCollider generated by VRCStation will be removed [`#1331`](https://github.com/anatawa12/AvatarOptimizer/pull/1331)
  - This might break the GogoLoco or other flying avatar that supports Quest / Android.
- NRE if some playabke layer is missing from AvatarDesciptor [`#1350`](https://github.com/anatawa12/AvatarOptimizer/pull/1350)

## [1.7.13] - 2024-10-01
### Fixed
- Null Reference Exception with newly created VRCAnimatorPlayAudio [`#1199`](https://github.com/anatawa12/AvatarOptimizer/pull/1199)
- Particle System that uses local scale will be broken [`#1197`](https://github.com/anatawa12/AvatarOptimizer/pull/1197)
- Avatars with Visame Skinned Mesh disabled will not able to upload [`#1202`](https://github.com/anatawa12/AvatarOptimizer/pull/1202)
- Default value for RemoveMeshInBox is not correct in Play mode [`#1217`](https://github.com/anatawa12/AvatarOptimizer/pull/1217)
  - This fix will make `Initialize` method set default value for `boxes`.

## [1.7.12] - 2024-08-27
### Changed
- Rewritten Check for Update system [`#1151`](https://github.com/anatawa12/AvatarOptimizer/pull/1151)

### Fixed
- VRCConstraints with Target might be removed unexpectedly [`#1150`](https://github.com/anatawa12/AvatarOptimizer/pull/1150)
- FinalIK Gimmicks with IKExecutionOrder is broken [`#1153`](https://github.com/anatawa12/AvatarOptimizer/pull/1153)
- Broken validation for MergePhysBone merging PhysBones with specified target [`#1160`](https://github.com/anatawa12/AvatarOptimizer/pull/1160)

## [1.7.11] - 2024-08-08
### Added
- VRCSDK 3.7.0 support [`#1140`](https://github.com/anatawa12/AvatarOptimizer/pull/1140)
  - This includes VRCConstraints support

### Fixed
- Some Humanoid Bones might be removed [`#1137`](https://github.com/anatawa12/AvatarOptimizer/pull/1137)
  - Repeated `AddPathDependency` is broken.
- Render is broken if all weighted bone is none and some other non-weight bone is not none [`#1138`](https://github.com/anatawa12/AvatarOptimizer/pull/1138)

## [1.7.10] - 2024-08-02
### Added
- Experimental VRCConstraints support [`#1129`](https://github.com/anatawa12/AvatarOptimizer/pull/1129) [`#1130`](https://github.com/anatawa12/AvatarOptimizer/pull/1130)
  - This only works for VRCSDK ~~`3.6.2-constraints.3`~~ `3.6.2-constraints.4` and not works with other versions including future versions.

### Fixed
- AutoMergeSkinnedMesh is broken if all merging meshes has no SubMeshes [`#1127`](https://github.com/anatawa12/AvatarOptimizer/pull/1127)

## [1.7.9] - 2024-07-25
### Fixed
- Index out of bounds error with remove mesh by mask with negative UV [`#1123`](https://github.com/anatawa12/AvatarOptimizer/pull/1123)

## [1.7.8] - 2024-07-22
### Fixed
- Index out of bounds error with remove mesh by mask [`#1119`](https://github.com/anatawa12/AvatarOptimizer/pull/1119)
- NRE with Generic Avatar [`#1122`](https://github.com/anatawa12/AvatarOptimizer/pull/1122)

## [1.7.7] - 2024-07-08
### Added
- Add Traditional Chinese [`#1102`](https://github.com/anatawa12/AvatarOptimizer/pull/1102)

### Fixed
- `VRCAnimatorPlayAudio` support is broken [`#1114`](https://github.com/anatawa12/AvatarOptimizer/pull/1114)

## [1.7.6] - 2024-06-17
### Fixed
- BlendTree with NormalizedBlendValues Broken with MergeBlendTree [`#1096`](https://github.com/anatawa12/AvatarOptimizer/pull/1096)
- Remove Zero Sized Polygon may remove small polygons [`#1098`](https://github.com/anatawa12/AvatarOptimizer/pull/1098)

## [1.7.5] - 2024-06-10
### Added
- Warnings for bad API Usages [`#1091`](https://github.com/anatawa12/AvatarOptimizer/pull/1091)

### Changed
- Ignore floating point precision error in Merge PhysBone [`#1086`](https://github.com/anatawa12/AvatarOptimizer/pull/1086)
- Animation Warning of Merge Skinned Mesh will not generated if source Renderer is not animated [`#1087`](https://github.com/anatawa12/AvatarOptimizer/pull/1087)
- Expression Parameters are now considered as a part of Avatar Dynamics Parameter destination [`#1089`](https://github.com/anatawa12/AvatarOptimizer/pull/1089)
- Relax condition for scaled evenly check [`#1092`](https://github.com/anatawa12/AvatarOptimizer/pull/1092)
  - Trace and Optimize will merge more bones than before.

### Removed
- Write to Asset on Play menu item which is no-op [`#1085`](https://github.com/anatawa12/AvatarOptimizer/pull/1085)
- Particle Syatem with Mesh Renderer shape will be broken [`#1093`](https://github.com/anatawa12/AvatarOptimizer/pull/1093)

### Fixed
- Merge BlendTree Layer will break some BlendTrees that have overriden by other layers [`#1084`](https://github.com/anatawa12/AvatarOptimizer/pull/1084)

## [1.7.4] - 2024-05-17
### Fixed
- Some rare material swap animation can cause exception [`#1067`](https://github.com/anatawa12/AvatarOptimizer/pull/1067)
- Invalid AABB error message from UnityEngine if there are no source for Merge Skinned Mesh [`#1068`](https://github.com/anatawa12/AvatarOptimizer/pull/1068)

## [1.7.3] - 2024-05-10
### Added
- Declare VRCSDK 3.6.x compatibility [`#1060`](https://github.com/anatawa12/AvatarOptimizer/pull/1060)

### Fixed
- Mesh preview may cause empty mesh on enter play mode if reload scene is disabled [`#1064`](https://github.com/anatawa12/AvatarOptimizer/pull/1064)
- MMD Compatibility can be broken with Merge BlendTree Layers [`#1065`](https://github.com/anatawa12/AvatarOptimizer/pull/1065)

## [1.7.2] - 2024-05-09
### Fixed
- Entry/Exit to BlendTree broken with None motion in default state `1057`
- An error with MergePhysBone [`#1061`](https://github.com/anatawa12/AvatarOptimizer/pull/1061)
- Animators depends on the WD=off behavior can be broken [`#1062`](https://github.com/anatawa12/AvatarOptimizer/pull/1062)

## [1.7.1] - 2024-05-07
### Added
- Implement mask texture editor [`#1044`](https://github.com/anatawa12/AvatarOptimizer/pull/1044)
- Add Simplified Chinese translation [`#1055`](https://github.com/anatawa12/AvatarOptimizer/pull/1045)

### Changed
- Improved behavior with Read/Write Off [`#1045`](https://github.com/anatawa12/AvatarOptimizer/pull/1045)
  - Because of Unity limitation, AAO cannot process meshes with R/W off on `Start` so it will be error.
  - However, on `Awake`, we can read them so AAO should process them.
  - Since this version, AAO will process meshes with R/W off on `Awake`.
  - This reduces the number of errors on the apply on play.
  - If you're using Av3Emulator, you still see the error.
  - In addition, in such case, we'll show `Auto Fix` button on the error message.
  - If you press the button, AAO will fix the error by changing the mesh to read/write enabled.
- `Advanced Options` section has benn renamed to `Debug Options` [`#1052`](https://github.com/anatawa12/AvatarOptimizer/pull/1052)
  - This express the purpose of the section more clearly.
- Added `Advanced Optimizations` and moved `Remove Zero sized Polygons` to it [`#1052`](https://github.com/anatawa12/AvatarOptimizer/pull/1052)
  - The `Remove Zero sized Polygons` can break some shaders or animations so it's not enabled by default.
  - To make it more clear, we moved it to `Advanced Optimizations`.

### Fixed
- Material Slot animations for multi-material multi-pass rendering are broken [`#1042`](https://github.com/anatawa12/AvatarOptimizer/pull/1042)
  - Previously we only preserves animations for the number of submeshes instead of material slots.
- Relax Bounds condition for Automatic Merge Skinned Mesh [`#1043`](https://github.com/anatawa12/AvatarOptimizer/pull/1043)
  - Previously, AAO doesn't merge Skinned Meshes if bounds are different accurately.
  - Since this version, AAO will merge meshes if bounds are the same with precision to the last 6 digits of decimal point.
- Entry/Exit to BlendTree broken with None state `1048`
- Particle System with bone-rigged Skinned Mesh Renderer will be broken [`#1054`](https://github.com/anatawa12/AvatarOptimizer/pull/1054)

## [1.7.0] - 2024-04-30
### Added
- Animator Optimizer [`#854`](https://github.com/anatawa12/AvatarOptimizer/pull/854)
  - Most features of Animator Optimizer is not available in Unity 2019.
  - Animator Optimizer optimizes your Animator Controller without behaviour Changes
  - Current Optimizer includes the following optimization
    - Remove meaningless properties [`#854`](https://github.com/anatawa12/AvatarOptimizer/pull/854)
    - Converts Entry / Exit to 1D BlendTree [`#854`](https://github.com/anatawa12/AvatarOptimizer/pull/854) [`#867`](https://github.com/anatawa12/AvatarOptimizer/pull/867) [`#927`](https://github.com/anatawa12/AvatarOptimizer/pull/927) [`#970`](https://github.com/anatawa12/AvatarOptimizer/pull/970) [`#982`](https://github.com/anatawa12/AvatarOptimizer/pull/982) [`#991`](https://github.com/anatawa12/AvatarOptimizer/pull/991)
    - Merges multiple BlendTrees to single Direct BlendTree [`#870`](https://github.com/anatawa12/AvatarOptimizer/pull/870) [`#978`](https://github.com/anatawa12/AvatarOptimizer/pull/978) [`#980`](https://github.com/anatawa12/AvatarOptimizer/pull/980) [`#984`](https://github.com/anatawa12/AvatarOptimizer/pull/984)
    - Removes meaningless Animator Layers [`#870`](https://github.com/anatawa12/AvatarOptimizer/pull/870)
- Asset Description [`#847`](https://github.com/anatawa12/AvatarOptimizer/pull/847)
  - Asset Description is the file to provide information of your assets for Avatar Optimizer.
  - Please see documentation for more details.
- Warning for material animation in Merge Skinned Mesh [`#769`](https://github.com/anatawa12/AvatarOptimizer/pull/769)
  - Merge Skinned Mesh does not support animating material properties differently. (In other words, it can be broken.)
  - Since this version, AAO will warn for such a case.
  - If you animated all materials from same animations, your animation will not be warned.
- API for declaring dependency relationship to the name of the component [`#943`](https://github.com/anatawa12/AvatarOptimizer/pull/943)
  - You can use this API to not change the name of the GameObject.
- Configuring `Clamp BlendShapes (Deprecated)` [`#957`](https://github.com/anatawa12/AvatarOptimizer/pull/957)
  - Since VRCSDK 3.5.1, VRCSDK sets `Clamp BlendShapes (Deprecated)` to true on assembly reload.
  - This is not a good setting for AAO in EditMode since AAO does not support clamping BlendShapes.
  - That's why AAO now configures `Clamp BlendShapes (Deprecated)` to false in edit mode and true in play mode.
  - PlayMode is usually used for testing the avatar behavior so it's better to have the same setting as VRChat client.
  - If you want not to change this setting, please disable `Tools/Avatar Optimizer/Configure Clamp BlendShape Weight`.
- Automatic Merge Skinned Mesh [`#952`](https://github.com/anatawa12/AvatarOptimizer/pull/952) [`#972`](https://github.com/anatawa12/AvatarOptimizer/pull/972) [`#1010`](https://github.com/anatawa12/AvatarOptimizer/pull/1010) [`#1020`](https://github.com/anatawa12/AvatarOptimizer/pull/1020) [`#1021`](https://github.com/anatawa12/AvatarOptimizer/pull/1021)
  - Trace and Optimize now automatically merges Skinned Meshes if possible.
  - Trace and Optimize will merge your mesh if the material properties or enablement of the mesh is animated similarly and has no BlendShapes.
- Components API for Scripting Usage [`#976`](https://github.com/anatawa12/AvatarOptimizer/pull/976)
  - You can now add AAO components from your script.
- Animations animating missing GameObject is removed [`#994`](https://github.com/anatawa12/AvatarOptimizer/pull/994)
  - Since this update, animations that targeting GameObjects with post-AAO paths is no longer working.
  - Please create animations that targeting GameObjects with pre-AAO paths.
- Remove Mesh by Mask [`#998`](https://github.com/anatawa12/AvatarOptimizer/pull/998)
  - With this component, you can remove polygons with mask texture.
  - You can use use mask for [MeshDeleterWithTexture] or alpha mask to remove polygons.
- Remove Empty SubMesh in Trace and Optimize [`#1007`](https://github.com/anatawa12/AvatarOptimizer/pull/1007)
  - This removes empty SubMeshes including becomes empty by optimization.

[MeshDeleterWithTexture]: https://github.com/gatosyocora/MeshDeleterWithTexture

### Changed
- MergePhysBone now corrects curve settings [`#775`](https://github.com/anatawa12/AvatarOptimizer/pull/775)
- MergePhysBone now warns if chain length are not same [`#775`](https://github.com/anatawa12/AvatarOptimizer/pull/775)
- MergePhysBone with only one source is now error [`#775`](https://github.com/anatawa12/AvatarOptimizer/pull/775)
  - It was not working well and not a error by a bug.
- Animator Parser is completely rewritten [`#850`](https://github.com/anatawa12/AvatarOptimizer/pull/850) [`#968`](https://github.com/anatawa12/AvatarOptimizer/pull/968) [`#1011`](https://github.com/anatawa12/AvatarOptimizer/pull/1011)
  - New Animator Parser allow us to track animating properties animated by components removed by AAO.
- PhysBone that swings no bones are now removed [`#864`](https://github.com/anatawa12/AvatarOptimizer/pull/864)
  - I found such a PhysBone on Lime so I added this feature.
- Switched Localization system to NDMF from CL4EE [`#873`](https://github.com/anatawa12/AvatarOptimizer/pull/873)
  - Since this release, Avatar Optimizer is no longer depends on CL4EE.
  - Because VCC doesn't remove unused packages, CL4EE may still be installed on your project.
  - If you want to remove CL4EE, please remove it manually.
- Suppressed animated BlendShape warning of FreezeBlendShape if it's animated to a few constants [`#881`](https://github.com/anatawa12/AvatarOptimizer/pull/881)
  - Modern models have tons of BlendShapes to change their face shape but emotion animation of some of them animates such a BlendShapes to constant (default value).
  - That's unnecessary (incorrect I think) and force users to remove or change the clip when user wants to face shape.
  - I see AAO users use `FreezeBlendShapes` for overriding such a BlendShapes on twitter.
  - I think using this way is reasonable enough so I suppressed the warning if AAO detected such a usage.
- Changed minimum VRCSDK to 3.3.0 [`#882`](https://github.com/anatawa12/AvatarOptimizer/pull/882)
  - VRCSDK 3.3.0 is required for stable NDMF-VRCSDK compatibility.
- Endpoint Position settings for newly created MergePhysBone is now Copy instead of Clear [`#945`](https://github.com/anatawa12/AvatarOptimizer/pull/945)
  - The Clear settings will increase the number of PhysBone Transforms so it's not better as a default settings.
- Improved activeness animation warning in Merge Skinned Mesh [`#948`](https://github.com/anatawa12/AvatarOptimizer/pull/948)
  - Reduced false-positive warnings
    - Previously, AAO warns if activeness warning is applied to different GameObjects.
    - However, this can be false-positive if animation is applied to different GameObjects with same timing. 
    - Since this version, AAO will not warn if the activeness is animated in same animation clip with same curve.
  - Combined warning per Merge Skinned Mesh component.
    - Previously, AAO warns for each source Renderers.
    - Since this version, AAO creates one warning for each Merge Skinned Mesh component.
- Add error for Cloth component in Merge Skinned Mesh component [`#949`](https://github.com/anatawa12/AvatarOptimizer/pull/949)
  - The Cloth component is not supported by Merge Skinned Mesh component.
  - In previous versions, AAO will keep the source Skinned Mesh Renderer if it's with Cloth component by bug.
  - Since this version, AAO will make an error if the source Skinned Mesh Renderer is with Cloth component.
- Remove Unused Objects now removes PhysBones and Contact Receivers with parameters defined but not used by Animator Controllers [`#959`](https://github.com/anatawa12/AvatarOptimizer/pull/959)
  - Previously, AAO did not remove PhysBones and Contact Receivers if they are defined in Animator Controllers whether they are used or not.
  - I thought such a PhysBones on the base body are rare but my friend told me there is Manuka has such a PhysBone so I added this feature.
- Dropping GameObject to PrefabSafeSet adds the All components on the GameObject to the PrefabSafeSet [`#960`](https://github.com/anatawa12/AvatarOptimizer/pull/960)
  - You can add all PhysBones on the GameObject by dropping the GameObject to the MergePhysBone component.
- MergeSkinnedMesh now warns if Root Bone or Anchor Override are not set [`#963`](https://github.com/anatawa12/AvatarOptimizer/pull/963)
- It will be error if read/write mesh is off in play mode again [`#1018`](https://github.com/anatawa12/AvatarOptimizer/pull/1018)
  - I found that we may not possible to read mesh with r/w mesh off mode in play mode with the Av3Emulator.
- Disabled AutoFreezeBlendShape for Skinned Mesh Renderers with Cloth component [`#1029`](https://github.com/anatawa12/AvatarOptimizer/pull/1029)
  - According to the report, making some polygons zero-size by AutoFreezeBlendShape will make initializing avatar extremely heavy.
  - After a small discussion, we decided to not automatically optimize Skinned Mesh Renderers with Cloth component.

### Removed
- Compatibility with VRCQuestTools v1.x [`#847`](https://github.com/anatawa12/AvatarOptimizer/pull/847)
  - Please use VRCQuestTools v2.x, which has compatibility with AAO.

### Fixed
- Inspector of ComponentTypePair (GCDebug) is broken [`#846`](https://github.com/anatawa12/AvatarOptimizer/pull/846)
- Bones swung by unused PhysBones (which will be removed by AAO) are not merged [`#850`](https://github.com/anatawa12/AvatarOptimizer/pull/850)
  - Note that To fix this problem, AnimatorParser is almost completely rewritten.
  - It's not expected to have behavior change, but if you found some, please report it.
- Re-fix Nested Constraint can be broken with Trace and Optimize [`#880`](https://github.com/anatawa12/AvatarOptimizer/pull/880)
- Fix non-VRChat project support [`#884`](https://github.com/anatawa12/AvatarOptimizer/pull/884)
- Fix VRM support [`#892`](https://github.com/anatawa12/AvatarOptimizer/pull/892)
- ArgumentNullException in Edit-mode Remove Mesh Preview [`#942`](https://github.com/anatawa12/AvatarOptimizer/pull/942)
- Bad behavior if EditMode preview is enabled when entering play mode [`#956`](https://github.com/anatawa12/AvatarOptimizer/pull/956)
- PlayableLayerControl or AnimatorLayerControl on non-root animator are ignored [`#964`](https://github.com/anatawa12/AvatarOptimizer/pull/964)
- Box Editor of Remove Mesh in Box can be broke with scale of Skinned Mesh Renderer [`#1019`](https://github.com/anatawa12/AvatarOptimizer/pull/1019)

## [1.6.13] - 2024-04-13
### Fixed
- Animator Controller on the VRCStation will be broken [`#1002`](https://github.com/anatawa12/AvatarOptimizer/pull/1002)
- Remove Component can be fails with RequireComponent attribute [`#1003`](https://github.com/anatawa12/AvatarOptimizer/pull/1003)

## [1.6.12] - 2024-04-09
### Removed
- Activeness Optimization for Constraint component [`#996`](https://github.com/anatawa12/AvatarOptimizer/pull/996)
  - The constraint component is too complex to optimize correctly and reliably

## [1.6.11] - 2024-04-07
### Fixed
- Bounds become broken if Update When Offscreen is enabled [`#990`](https://github.com/anatawa12/AvatarOptimizer/pull/990)

## [1.6.10] - 2024-04-05
### Fixed
- Missing Reference Exception with Trace and Optimize [`#986`](https://github.com/anatawa12/AvatarOptimizer/pull/986)

## [1.6.9] - 2024-03-27
### Added
- Support for VRCSDK 3.5.2 [`#926`](https://github.com/anatawa12/AvatarOptimizer/pull/926) [`#935`](https://github.com/anatawa12/AvatarOptimizer/pull/935)

### Fixed
- ContextHolder become unknown component in NDMF 1.4.0 [`#946`](https://github.com/anatawa12/AvatarOptimizer/pull/946)

## [1.6.8] - 2024-03-12
### Fixed
- If some component refers external component, internal error [`#921`](https://github.com/anatawa12/AvatarOptimizer/pull/921)

## [1.6.7] - 2024-02-28
### Fixed
- Compilation Error due to VRCImposterSettings with VRCSDK 3.2.x [`#905`](https://github.com/anatawa12/AvatarOptimizer/pull/905)
- Skinned Mesh Renderers with None mesh will become Mesh with no polygons [`#906`](https://github.com/anatawa12/AvatarOptimizer/pull/906)
  - This may affects bounds of Performance Rank in VRChat
- Exclusions not working with Automatically Remove Zero Sized Polygons [`#907`](https://github.com/anatawa12/AvatarOptimizer/pull/907)
- Fix non-VRChat project support [`#884`](https://github.com/anatawa12/AvatarOptimizer/pull/884) (backport in `#909`)
- Merge Toonlit with uv tiling is broken [`#911`](https://github.com/anatawa12/AvatarOptimizer/pull/911)

## [1.6.6] - 2024-01-31
### Fixed
- Some features are not working well if `Trace and Optimize` is not attached [`#876`](https://github.com/anatawa12/AvatarOptimizer/pull/876)

## [1.6.5] - 2024-01-29
### Changed
- AvatarOptimizer now uses ErrorReporting API of NDMF instead of our own API [`#805`](https://github.com/anatawa12/AvatarOptimizer/pull/805)
- Project is slightly renamed to AAO: Avatar Optimizer [`#830`](https://github.com/anatawa12/AvatarOptimizer/pull/830)
  - The term `AAO` and `Avatar Optimizer` are not changed, but display name on the VCC is changed to `AAO: Avatar Optimizer`

### Fixed
- Fix support for UniVRM components [`#802`](https://github.com/anatawa12/AvatarOptimizer/pull/802)
- VRM: Fix MergeSkinnedMesh breaking BlendShapeClip / VRM10Expression [`#810`](https://github.com/anatawa12/AvatarOptimizer/pull/810)
- Unknown component warning were shown multiple time for one type [`#818`](https://github.com/anatawa12/AvatarOptimizer/pull/818)
  - In addition, location of the unknown components are shown on the error report.
- Empty Armature trick broken [`#819`](https://github.com/anatawa12/AvatarOptimizer/pull/819)
- Added workaround for `Array index (n) is out of bounds (size=m)` error
- Humanoid Bones may be removed by Trace and Optimize [`#831`](https://github.com/anatawa12/AvatarOptimizer/pull/831)
- Add `license`, `documentationUrl`, and `changelogUrl` to package.json [`#851`](https://github.com/anatawa12/AvatarOptimizer/pull/851)
- Nested Constraint can be broken with Trace and Optimize [`#857`](https://github.com/anatawa12/AvatarOptimizer/pull/857)

## [1.6.4] - 2023-12-10
### Fixed
- Error with generic avatar in 2022 [`#794`](https://github.com/anatawa12/AvatarOptimizer/pull/794)
- Assertion Error in some rare case [`#795`](https://github.com/anatawa12/AvatarOptimizer/pull/795)

## [1.6.3] - 2023-12-09
### Added
- Support for VRCSDK 3.5.x [`#787`](https://github.com/anatawa12/AvatarOptimizer/pull/787)
  - Actually, previous version of AAO works well with VRCSDK 3.5.x / Unity 2022 with tiny bugs.
  - I've fixed some bugs in Unity 2022 in this release.
  - Since this version, package.json declares Avatar Optimizer is compatible with VRCSDK 3.5.x.
  - I was planned to release this changes while VRCSDK 3.5.0 is in beta.
  - However, VRCSDK 3.5.0 beta was only 3 hours so I could not.

### Fixed
- Fix NullReferenceException on Unity 2022 when extra Animator components are present [`#778`](https://github.com/anatawq12/AvatarOptimizer/pull/778)
- Fix Errors with Generic Avatars [`#779`](https://github.com/anatawa12/AvatarOptimizer/pull/779)
- Editing Prefabs with AAO Components in Unity 2022 will cause Error [`#782`](https://github.com/anatawa12/AvatarOptimizer/pull/782)
- Error if there are reference to Prefab Asset PhysBone Collider [`#783`](https://github.com/anatawa12/AvatarOptimizer/pull/783)
- Remove Mesh in Box editor broken if inspector is narrow [`#784`](https://github.com/anatawa12/AvatarOptimizer/pull/784)
- Errors for partially incorrectly configured avatars [`#786`](https://github.com/anatawa12/AvatarOptimizer/pull/786)
  - Since this release, instead of internal errors, warnings are shown

## [1.6.2] - 2023-11-30
### Fixed
- Path remapping for merge bone will not work well in some (relatively rare) case [`#764`](https://github.com/anatawa12/AvatarOptimizer/pull/764)
- Error due to PhysBone collider with root bone outside avatar [`#766`](https://github.com/anatawa12/AvatarOptimizer/pull/766)

## [1.6.1] - 2023-11-29
### Fixed
- Error if there are None colliders for PhysBone [`#758`](https://github.com/anatawa12/AvatarOptimizer/pull/758)
- BlendShapes can broken in extreamly rare cases [`#760`](https://github.com/anatawa12/AvatarOptimizer/pull/760)
  - It seems this is due to Unity bug.

## [1.6.0] - 2023-11-27
### Added
- Public API for registering component information [`#632`](https://github.com/anatawa12/AvatarOptimizer/pull/632) [`#668`](https://github.com/anatawa12/AvatarOptimizer/pull/668)
- Disabling PhysBone animation based on mesh renderer enabled animation [`#640`](https://github.com/anatawa12/AvatarOptimizer/pull/640)
  - If you toggles your clothes with simple toggle, PhysBones on the your avatar will also be toggled automatically!
- Small performance improve [`#641`](https://github.com/anatawa12/AvatarOptimizer/pull/641)
- Ability to prevent changing enablement of component [`#668`](https://github.com/anatawa12/AvatarOptimizer/pull/668)
- Remove Zero Sized Polygons [`#659`](https://github.com/anatawa12/AvatarOptimizer/pull/659)
- Add support for UniVRM components [`#653`](https://github.com/anatawa12/AvatarOptimizer/pull/653)
- Support for Mesh Topologies other than Triangles [`#692`](https://github.com/anatawa12/AvatarOptimizer/pull/692)
- Skip enablement mismatched Renderers in Merge Skinned Mesh [`#670`](https://github.com/anatawa12/AvatarOptimizer/pull/670)
  - This is now enabled by default for newly added Merge Skinned Mesh.
- Error for MergeBone on the Avatar Root [`#716`](https://github.com/anatawa12/AvatarOptimizer/pull/716)
- Warning for conflicts with animation
  - Warning for freezing animated BlendShapes [`#719`](https://github.com/anatawa12/AvatarOptimizer/pull/719)
  - Warning for merging renderers that activeness/enablement animated differently [`#675`](https://github.com/anatawa12/AvatarOptimizer/pull/675)
- PhysBone Optimization [`#733`](https://github.com/anatawa12/AvatarOptimizer/pull/733)
  - Unnessesary isAnimated is now unconfigured
  - Floor Colliders with same configuration will be merged to one floor collider
- Minimum Support for FinalIK [`#735`](https://github.com/anatawa12/AvatarOptimizer/pull/735)

### Changed
- All logs passed to ErrorReport is now shown on the console log [`#643`](https://github.com/anatawa12/AvatarOptimizer/pull/643)
- Improved Behaviour with multi-material multi pass rendering [`#662`](https://github.com/anatawa12/AvatarOptimizer/pull/662)
  - Previously, multi-material multi pass rendering are flattened.
  - Since 1.6, flattened if component doesn't support that.
- Remove Unused Objects removes meaningless Animators and Renderers [`#709`](https://github.com/anatawa12/AvatarOptimizer/pull/709)
  - Renderers without Mesh and Animators without AnimatorController is meaningless

### Removed
- Legacy GC [`#633`](https://github.com/anatawa12/AvatarOptimizer/pull/633)
- Preventing removing `IEditorOnly` in callback order -1024 [`#658`](https://github.com/anatawa12/AvatarOptimizer/pull/658)
  - This is no longer needed since 1.5.0 but I forgot to remove so I removed in 1.6

### Fixed
- Improve support of newer Unity versions [`#608`](https://github.com/anatawa12/AvatarOptimizer/pull/608)
- Improve support of projects without VRCSDK [`#609`](https://github.com/anatawa12/AvatarOptimizer/pull/609) [`#625`](https://github.com/anatawa12/AvatarOptimizer/pull/625) [`#627`](https://github.com/anatawa12/AvatarOptimizer/pull/627)
- Prefab blinks when we see editor of PrefabSafeSet of prefab asset [`#645`](https://github.com/anatawa12/AvatarOptimizer/pull/645) [`#664`](https://github.com/anatawa12/AvatarOptimizer/pull/664)
- complex shader with SkinnedMeshRenderer without Bones Brokebn [`#694`](https://github.com/anatawa12/AvatarOptimizer/pull/694)
- bounds can be changed in apply on play if updateWhenOffscreen is true [`#697`](https://github.com/anatawa12/AvatarOptimizer/pull/697)
- Compatibility with transform moving plugins [`#715`](https://github.com/anatawa12/AvatarOptimizer/pull/715)
  - Remove Mesh in Box was not working well with [FloorAdjuster]
- Some missing components warnings [`#736`](https://github.com/anatawa12/AvatarOptimizer/pull/736)
  - warning for `ONSPAudioSource`, `VRCImpostorSettings`, and `RectTransform` are fixed
- Remove Unused Object may break ParticleSystem [`#738`](https://github.com/anatawa12/AvatarOptimizer/pull/738)
  - Trigger Colliders can be disapper if you specify Transform instead of Collider instance.
  - Initially diabled particle system module will be ignored
- MergeBone breaks `ignoreTransforms` of PhysBone [`#745`](https://github.com/anatawa12/AvatarOptimizer/pull/745)

[FloorAdjuster]: https://github.com/Narazaka/FloorAdjuster
## [1.5.11] - 2023-11-18
### Fixed
- Dynamic Bone support not working [`#727`](https://github.com/anatawa12/AvatarOptimizer/pull/727)

## [1.5.10] - 2023-11-04
### Fixed
- RigidBody Joint can be broken [`#683`](https://github.com/anatawa12/AvatarOptimizer/pull/683)

## [1.5.9] - 2023-10-29
### Fixed
- Animation clip length can be changed [`#647`](https://github.com/anatawa12/AvatarOptimizer/pull/647)

## [1.5.8] - 2023-10-20
### Fixed
- warning about VRCTestMarker when Build & Test [`#628`](https://github.com/anatawa12/AvatarOptimizer/pull/628)

## [1.5.7] - 2023-10-19
### Added
- Add compatibility for VRCQuestTools [`#619`](https://github.com/anatawa12/AvatarOptimizer/pull/619)

### Fixed
- AutoFreezeBlendShape will freeze BlendShapes with editor value instead of animated constant [`#622`](https://github.com/anatawa12/AvatarOptimizer/pull/622)

## [1.5.6] - 2023-10-17
### Changed
- Make no-op as possible if no AAO component attached for your avatar [`#603`](https://github.com/anatawa12/AvatarOptimizer/pull/603)
- Error Report window is refreshed after exiting play mode [`#606`](https://github.com/anatawa12/AvatarOptimizer/pull/606)

### Removed
- Error for Read/Write Mesh off Mesh [`#615`](https://github.com/anatawa12/AvatarOptimizer/pull/615)
  - Since AAO creates Mesh every time, no more error is required!

### Fixed
- Multi-frame BlendShape can be broken [`#601`](https://github.com/anatawa12/AvatarOptimizer/pull/601)
- Update notice may show incorrect version [`#602`](https://github.com/anatawa12/AvatarOptimizer/pull/602)
- `Preview` button is not disabled even if mesh is none [`#605`](https://github.com/anatawa12/AvatarOptimizer/pull/605)
- BindPose Optimization may break mesh with scale 0 bone [`#612`](https://github.com/anatawa12/AvatarOptimizer/pull/612)

## [1.5.5] - 2023-10-15
### Fixed
- Constraints and Animations can be broken with Automatic MergeBone [`#594`](https://github.com/anatawa12/AvatarOptimizer/pull/594)
- NRE with SMR with None with preview system [`#596`](https://github.com/anatawa12/AvatarOptimizer/pull/596)
- Some Multi-Frame BlendShape broken [`#597`](https://github.com/anatawa12/AvatarOptimizer/pull/597)
- BlendShape can be broken with MergeBone Optimization [`#599`](https://github.com/anatawa12/AvatarOptimizer/pull/599)

## [1.5.4] - 2023-10-14
### Added
- Add compatibility for Satania's KiseteneEx [`#584`](https://github.com/anatawa12/AvatarOptimizer/pull/584)

### Changed
- Normal check is skipped for empty mesh [`#588`](https://github.com/anatawa12/AvatarOptimizer/pull/588)
- Meshes without Normal are shown on the normal existance mismatch warning [`#588`](https://github.com/anatawa12/AvatarOptimizer/pull/588)

### Fixed
- Error with MeshRenderer without MeshFilter [`#581`](https://github.com/anatawa12/AvatarOptimizer/pull/581)
- Preview not working with VRMConverter [`#582`](https://github.com/anatawa12/AvatarOptimizer/pull/582)
- AvatarMask about HumanoidBone broken [`#586`](https://github.com/anatawa12/AvatarOptimizer/pull/586)
- Unused Humanoid Bones can be removed [`#587`](https://github.com/anatawa12/AvatarOptimizer/pull/587)

## [1.5.3] - 2023-10-11
### Changed
- Ignore the warning instead of migration from 0.3.x or older [`#570`](https://github.com/anatawa12/AvatarOptimizer/pull/570)

### Fixed
- AnimatorController with Synced can be broken [`#564`](https://github.com/anatawa12/AvatarOptimizer/pull/564)
- AnimatorOverrideController may not be proceed correctly [`#567`](https://github.com/anatawa12/AvatarOptimizer/pull/567)
- Unclear behaviour if we merged meshes with and without normals [`#569`](https://github.com/anatawa12/AvatarOptimizer/pull/569)

## [1.5.2] - 2023-10-10
### Added
- Feature for debugging GC Objects [`#543`](https://github.com/anatawa12/AvatarOptimizer/pull/543)
- More MMD BlendShapes are registered [`#552`](https://github.com/anatawa12/AvatarOptimizer/pull/552)
  - New English Translation BlendShapes are compatible with AAO!
- Check for update [`#554`](https://github.com/anatawa12/AvatarOptimizer/pull/554)

### Changed
- You now cannot key any of AvatarOptimizer Components [`#551`](https://github.com/anatawa12/AvatarOptimizer/pull/551)
  - Previously you can key AvatarOptimizer Coponent but it was meaningless.

### Fixed
- EditMode Preview of RemoveMeshInBox is not correct [`#550`](https://github.com/anatawa12/AvatarOptimizer/pull/550)
- Avatar Standard Colliders can be removed [`#553`](https://github.com/anatawa12/AvatarOptimizer/pull/553)
- Freeze BlendShape may break Visame with MergeSkinnedMesh [`#561`](https://github.com/anatawa12/AvatarOptimizer/pull/561)

## [1.5.1] - 2023-10-08
### Fixed
- MergePhysBone component may be shown as unknown components [`#541`](https://github.com/anatawa12/AvatarOptimizer/pull/541)
- MergeBone may break Fur [`#542`](https://github.com/anatawa12/AvatarOptimizer/pull/542)

## [1.5.0] - 2023-10-07
### Added
- Support for [NDMF](https://ndmf.nadena.dev) integration [`#375`](https://github.com/anatawa12/AvatarOptimizer/pull/375)
- Pre-building validation for MergeBone [`#417`](https://github.com/anatawa12/AvatarOptimizer/pull/417)
  - There are some (rare) cases that are not supported by MergeBone. This adds warning for such case.
- Validation error for self recursive MergeSkinnedMesh [`#418`](https://github.com/anatawa12/AvatarOptimizer/pull/418)
- Advanced Settings Section for Trace and Optimize [`#419`](https://github.com/anatawa12/AvatarOptimizer/pull/419)
  - Moved `Use Advanced Animator Parser` to there
  - Added `Exclusions` for exclude some GameObjects from optimization
  - In this section, there are for debugging GC Objects [`#464`](https://github.com/anatawa12/AvatarOptimizer/pull/464)
- Avoid Name Conflict in MergeBone [`#467`](https://github.com/anatawa12/AvatarOptimizer/pull/467)
- Full EditMode Preview of RemoveMesh Components [`#500`](https://github.com/anatawa12/AvatarOptimizer/pull/500)
- Significant Performance Improvements with small code changes [`#523`](https://github.com/anatawa12/AvatarOptimizer/pull/523)

### Changed
- Improved 'Remove Unused Objects' [`#401`](https://github.com/anatawa12/AvatarOptimizer/pull/401)
  - Remove Unused Objects now removes unnecessary Components & Bones!
  - With new algorithm, you can preserve end bones (`#430`)
  - You may use `Use Legacy GC` to use legacy algotythm for Remove Unused Objects in `Advanced Settings` (`#419`)
- Performance: Share MeshInfo2 between SkinnedMesh processing and MergeBone [`#421`](https://github.com/anatawa12/AvatarOptimizer/pull/421)
- Declare compatible with VRCSDK 3.4.x [`#513`](https://github.com/anatawa12/AvatarOptimizer/pull/513)
- Change Japanese Translation of "BlendShape" [`#535`](https://github.com/anatawa12/AvatarOptimizer/pull/535)

### Deprecated
- UnusedBonesByReferenceTool component is now obsolete [`#430`](https://github.com/anatawa12/AvatarOptimizer/pull/430)
  - Newly introduced algorithm of`Remove Unused Objects` does same thing!
  - You can migrate to `Remove Unused Objects` only with one click!

### Removed
- internal ApplyOnPlay framework [`#504`](https://github.com/anatawa12/AvatarOptimizer/pull/504)

### Fixed
- Crash with Unity 2022 [`#423`](https://github.com/anatawa12/AvatarOptimizer/pull/423)
  - [Due to bug in Unity Editor 2022.3 or later][unity-bug], Avatar Optimizer was not compatible with Unity 2022.
- Error if all vertices of some BlendShape is removed by RemoveMeshByBlendShape or RemoveMeshInBox [`#440`](https://github.com/anatawa12/AvatarOptimizer/pull/440)
- RemoveMeshByBlendShape on the SkinnedMeshRenderer with MergeSkinnedMesh not working [`#451`](https://github.com/anatawa12/AvatarOptimizer/pull/451)
- MergeBone may make some bone inactive to active if bone being merged is inactive [`#454`](https://github.com/anatawa12/AvatarOptimizer/pull/454)
- Avoid problematic material slot in MergeSkinnedMesh [`#508`](https://github.com/anatawa12/AvatarOptimizer/pull/508)
  - This avoids [Unity's bug in 2019][unity-bug-material]. In Unity 2022, this is no longer needed.
- Editor of EditSkinnedMesh components may not work well if the object is inactive [`#518`](https://github.com/anatawa12/AvatarOptimizer/pull/518)

[unity-bug]: https://issuetracker.unity3d.com/issues/crash-on-gettargetassemblybyscriptpath-when-a-po-file-in-the-packages-directory-is-not-under-an-assembly-definition
[unity-bug-material]: https://issuetracker.unity3d.com/issues/material-is-applied-to-two-slots-when-applying-material-to-a-single-slot-while-recording-animation


## [1.4.3] - 2023-09-05
### Fixed
- Mesh broken with BlendShape Frame with weight 0 [`#408`](https://github.com/anatawa12/AvatarOptimizer/pull/408)

## [1.4.2] - 2023-09-04
### Fixed
- Components/GameObjects can falsely detected as always disabled / inactive. [`#403`](https://github.com/anatawa12/AvatarOptimizer/pull/403)

## [1.4.1] - 2023-09-02
### Fixed
- RootBone become None with Merge SkinedMesh [`#399`](https://github.com/anatawa12/AvatarOptimizer/pull/399)

## [1.4.0] - 2023-09-02
### Added
- Support for Multi Frame BlendShapes [`#333`](https://github.com/anatawa12/AvatarOptimizer/pull/333)
- Add link to help page [`#382`](https://github.com/anatawa12/AvatarOptimizer/pull/382)
- Advanced Animator Parser [`#343`](https://github.com/anatawa12/AvatarOptimizer/pull/343)
  - This is new AnimatorController parser to collect animated properties
  - This parser understands AnimatorLayers, so with this parser,
    AAO can freeze BlendShapes which are always finally animated to a constant value.
  - This also understands Additive Layer and BlendTree, so extremely rare problem in previous Animator Parser
    with Additive Layer or BlendTree will be fixed with this parser.
- Multi Pass Rendering of Last SubMesh support [`#384`](https://github.com/anatawa12/AvatarOptimizer/pull/384)
- Remove Mesh By BlendShape Editor now can set BlendShape weights to 0/100

### Changed
- Auto FreezeBlendShape now freezes meaningless BlendShape [`#334`](https://github.com/anatawa12/AvatarOptimizer/pull/334)
  - If you removed some vertices with RemoveMeshInBox or RemoveMeshWithBlendShape, some BlendShape may transform no vertices
  - Auto FreeseBlendShae now freezez such a BlendShapes
- Auto FreezeBlendShape now freezes vertices even if already FreezeBlendShape is configured. [`#334`](https://github.com/anatawa12/AvatarOptimizer/pull/334)
- Meshes generated by AAO now have name [`#371`](https://github.com/anatawa12/AvatarOptimizer/pull/371)
  - This will improve compatibility with UniVRM.
- VPM Package now doesn't include Test code [`#372`](https://github.com/anatawa12/AvatarOptimizer/pull/372) [`#373`](https://github.com/anatawa12/AvatarOptimizer/pull/373)
- Better error infomation for MeshInfo2 error [`#381`](https://github.com/anatawa12/AvatarOptimizer/pull/381)
- Declare compatible with VRCSDK 3.3.x [`#395`](https://github.com/anatawa12/AvatarOptimizer/pull/395)
- Understandable Error if there are Missing Script Component [`#398`](https://github.com/anatawa12/AvatarOptimizer/pull/398)
  - Why VRCSDK doesn't have such a error system?

### Fixed
- MergeBone not working well with non-restpose bones [`#379`](https://github.com/anatawa12/AvatarOptimizer/pull/379)
- Unclear Error with Mesh with Read/Write off [`#386`](https://github.com/anatawa12/AvatarOptimizer/pull/386)
- Clear Endpoint Position may not work well with ignore transforms [`#390`](https://github.com/anatawa12/AvatarOptimizer/pull/390)
- Clear Endpoint Position doesn't support Undo [`#390`](https://github.com/anatawa12/AvatarOptimizer/pull/390)

## [1.3.4] - 2023-08-22
### Changed
- Internal implementation of Trace and Optimize [`#361`](https://github.com/anatawa12/AvatarOptimizer/pull/361)
- Documentation Improvements [`#366`](https://github.com/anatawa12/AvatarOptimizer/pull/366) [`#365`](https://github.com/anatawa12/AvatarOptimizer/pull/365)

## [1.3.3] - 2023-08-21
### Added
- BlendShape Weight mismatch warning is now build-time warning instad of validate time warning [`#359`](https://github.com/anatawa12/AvatarOptimizer/pull/359)
  - Thanks to FreeseBlendShape by TraceAndOptimize, most pre-build this warning is false positive. So this warning is moved to build-time only.

### Fixed
- ClearEndpointPosition is not applied for non-first PhysBones on the GameObject [`#357`](https://github.com/anatawa12/AvatarOptimizer/pull/357)
- Incompatbile with Reload Scene disabaled [`#358`](https://github.com/anatawa12/AvatarOptimizer/pull/358)

## [1.3.2] - 2023-08-20
### Fixed
- Children of IsActive animated object is not considered [`#342`](https://github.com/anatawa12/AvatarOptimizer/pull/342)
- Multi Passs Rendering not supported error doesn't have location info [`#347`](https://github.com/anatawa12/AvatarOptimizer/pull/347)

## [1.3.1] - 2023-08-19
### Fixed
- Unity Editor may freezes when there are circular dependency [`#329`](https://github.com/anatawa12/AvatarOptimizer/pull/329)
- Network ID is not assigned for newly created PBs [`#331`](https://github.com/anatawa12/AvatarOptimizer/pull/331)
- Internally assigned animator controller is not skipped for default choosen playable layer in Trace and Optimize [`#332`](https://github.com/anatawa12/AvatarOptimizer/pull/332)
- VRCSDK assigned default animators are not considered in Trace and Optimize [`#332`](https://github.com/anatawa12/AvatarOptimizer/pull/332)
  - This bug doesn't create bad behavior for now but will does in the feature.
- Humanoid Animation are not considered in Trace and Optimize [`#332`](https://github.com/anatawa12/AvatarOptimizer/pull/332)
  - This bug doesn't create bad behavior for now but will does in the feature.
- Material Slot with null material is created if there are more SubMesh than Material Slots [`#337`](https://github.com/anatawa12/AvatarOptimizer/pull/337)
- AAO silently ignored multi pass rendering [`#337`](https://github.com/anatawa12/AvatarOptimizer/pull/337)
  - For now, multi pass rendering of last SubMesh is not (yet) supported so now cause error but will be supported.
- There is no warning about BlendShape weight difference [`#336`](https://github.com/anatawa12/AvatarOptimizer/pull/336)

## [1.3.0] - 2023-08-12
### Added
- Remove always disabled objects [`#278`](https://github.com/anatawa12/AvatarOptimizer/pull/278)
- The new Remove Mesh By BlendShape component removes mesh data based on BlendShapes. [`#275`](https://github.com/anatawa12/AvatarOptimizer/pull/275)
- Option to process Make Children before modular avatar [`#296`](https://github.com/anatawa12/AvatarOptimizer/pull/296)

### Changed
- Use UnityEditor api to compress texture [`#276`](https://github.com/anatawa12/AvatarOptimizer/pull/276)
  - This also adds some supported texture formats.
- Every component have `AAO` prefix in their name now [`#290`](https://github.com/anatawa12/AvatarOptimizer/pull/290)
  - The official shorthand for this tools is `AAO`!
- `Automatic Configuration` component has been renamed to `Trace And Optimize` [`#295`](https://github.com/anatawa12/AvatarOptimizer/pull/295)

### Fixed
- UnusedBonesByReferenceTool error with SMR without mesh [`#280`](https://github.com/anatawa12/AvatarOptimizer/pull/280)
- MergeSkinnedMesh doesn't work well with eyelids [`#284`](https://github.com/anatawa12/AvatarOptimizer/pull/284)
- Animating Behaviour.m_Enabled not working [`#287`](https://github.com/anatawa12/AvatarOptimizer/pull/287)
- Error Report Window may not refreshed after build error [`#299`](https://github.com/anatawa12/AvatarOptimizer/pull/299)
- Apply On Play may not working well [`#305`](https://github.com/anatawa12/AvatarOptimizer/pull/305)
- Some components unexpectedly can be added multiple times [`#306`](https://github.com/anatawa12/AvatarOptimizer/pull/306)

## [1.2.0] - 2023-07-26
### Added
- Automatic bounds computation in MergeSkinnedMesh [`#264`](https://github.com/anatawa12/AvatarOptimizer/pull/264)
- Automatic Configuration System [`#265`](https://github.com/anatawa12/AvatarOptimizer/pull/265)
  - Currently FreezeBlendShape can be automatically configured.
- Support for material swapping animation in MergeSkinnedMesh [`#274`](https://github.com/anatawa12/AvatarOptimizer/pull/274)

### Changed
- Support newly activated avatars in play mode for apply on play [`#263`](https://github.com/anatawa12/AvatarOptimizer/pull/263)

### Fixed
- Breaks mesh without tangent [`#271`](https://github.com/anatawa12/AvatarOptimizer/pull/271)

## [1.1.1] - 2023-07-14
### Changed
- Avatar GameObject marked as EditorOnly no longer be removed [`#261`](https://github.com/anatawa12/AvatarOptimizer/pull/261)
  - Previously, if avatar GameObject is marked as EditorOnly, whole avatar is removed and this confuses users.

### Fixed
- Name of failed ApplyOnPlayCallback is not included in error message [`#260`](https://github.com/anatawa12/AvatarOptimizer/pull/260)
- Entering play mode can be extremely slow if you have many avatar on the scene [`#262`](https://github.com/anatawa12/AvatarOptimizer/pull/262)

## [1.1.0] - 2023-07-13
### Added
- Now we can choose texture format for Merge Toon Lit Material [`#251`](https://github.com/anatawa12/AvatarOptimizer/pull/251)
  - This includes one tiny **BREAKING CHANGES**.
  - Previously MergeToonLit uses ARGB32 as texture format but for now, it use ASTC 6x6 or DXT5 by default based on platform.

### Changed
- Move Components into `Avatar Optimizer` folder [`#247`](https://github.com/anatawa12/AvatarOptimizer/pull/247)
  - Previously they are `Optimizer` folder
- Completely rewrite apply on play system [`#249`](https://github.com/anatawa12/AvatarOptimizer/pull/249)
  - This will remove EditorOnly on play.
  - This enable UnusedBonesByReferencesTool component on play.
  - This replaces way to apply [modular-avatar by bdunderscore] on play.
    - modular avatar will be applied before AvatarOptimizer main logic and after removing EditorOnly.
  - This will remove enable/disable checkbox on components, which had no meaning.
  - The framework for this changes will be published as separated framework when ready.
- Use binary form of asset file in avatar optimizer output [`#252`](https://github.com/anatawa12/AvatarOptimizer/pull/252)

[modular-avatar]: https://modular-avatar.nadena.dev/

### Fixed
- Manual bake not working with avatars with invalid file name chars [`#253`](https://github.com/anatawa12/AvatarOptimizer/pull/253)
- Merge Toon Lit duplicates vertex too many [`#256`](https://github.com/anatawa12/AvatarOptimizer/pull/256)
  - This could causes huge increase in avatar size. this is now fixed.

## [1.0.0] - 2023-06-27
**If you're using v0.3.x or older, Please upgrade to v0.4.x before upgrading v1.x.x!**

**もし v0.3.x 以前を使用しているのであれば, v1.x.xに更新する前に v0.4.x に更新してください!**

### Removed
- Save format migration system [`#199`](https://github.com/anatawa12/AvatarOptimizer/pull/199)
  - We no longer see save data in format of v0.3.x or older.
  - Please migrate to v0.4.x format before installing v1.0.0.

## [0.4.12] - 2023-06-22
### Added
- MergePhysBone without ClearEndpointPosition [`#239`](https://github.com/anatawa12/AvatarOptimizer/pull/239)
  - Instead of ClearEndpointPosition, you can use original value, or override Endpoint Position.

## [0.4.11] - 2023-06-19
### Changed
- Show error with user friendly message if BlendShape for eyelids are removed / frozen. [`#253`](https://github.com/anatawa12/AvatarOptimizer/pull/253)

### Fixed
- eyelids BlendShape settings are mapped even if it's disabled [`#235`](https://github.com/anatawa12/AvatarOptimizer/pull/235)
  - This fixes error if internally eyelids BlendShape are frozen.

## [0.4.10] - 2023-06-17
### Fixed
- PrefabSafesSet's prefab modifications on latest layer are invisible on inspector [`#229`](https://github.com/anatawa12/AvatarOptimizer/pull/229)

## [0.4.9] - 2023-06-16
### Fixed
- NullReferenceException if window is in background [`#226`](https://github.com/anatawa12/AvatarOptimizer/pull/226)

## [0.4.8] - 2023-06-16
## [0.4.7] - 2023-06-13
### Fixed
- Dropping PhysBone to MergePhysBone is not working [`#221`](https://github.com/anatawa12/AvatarOptimizer/pull/221)
- First box will be size of zero [`#223`](https://github.com/anatawa12/AvatarOptimizer/pull/223)

## [0.4.6] - 2023-06-10
### Changed
- Improve ErrorReport window on build error [`#216`](https://github.com/anatawa12/AvatarOptimizer/pull/216)

## [0.4.5] - 2023-06-06
### Fixed
- Error in MergeSkinnedMeshProcessor with RecordMoveProperty [`#214`](https://github.com/anatawa12/AvatarOptimizer/pull/214)

## [0.4.4] - 2023-06-04
### Changed
- Make `Remove Empty Renderer Object` enabled by default [`#208`](https://github.com/anatawa12/AvatarOptimizer/pull/208)

## [0.4.3] - 2023-06-02
### Added
- Adding multiple values to PrefabSafeSet [`#200`](https://github.com/anatawa12/AvatarOptimizer/pull/200)
  - See [this video](https://github.com/anatawa12/AvatarOptimizer/issues/128#issuecomment-1568540903) for more details.
- Overriden PrefabSafeSet properties are now highlighted as blue and bold [`#200`](https://github.com/anatawa12/AvatarOptimizer/pull/200)

### Fixed
- Error when we removed some modification in PrefabSafeSet [`#201`](https://github.com/anatawa12/AvatarOptimizer/pull/201)
  - There are several situations for this problem:
    - When we removed value in original component
    - When we removed new value in overrides
    - When we reverted added twice in overrides
    - When we reverted deletion in overrides
    - When we reverted fake deletion in overrides
- Error when we reverted whole PrefabSafeSet with modifications [`#201`](https://github.com/anatawa12/AvatarOptimizer/pull/201)

## [0.4.2] - 2023-05-30
### Fixed
- MergeSkinnedMesh depends on other EditSkinnedMesh components does not working [`#195`](https://github.com/anatawa12/AvatarOptimizer/pull/195)
- Error with removed modified property in PrefabSafeSet Editor [`#196`](https://github.com/anatawa12/AvatarOptimizer/pull/196)
- Apply on Play may not work [`#198`](https://github.com/anatawa12/AvatarOptimizer/pull/198)

## [0.4.1] - 2023-05-23
### Changed
- Reimplemented Animation Mapping System Completely [`#168`](https://github.com/anatawa12/AvatarOptimizer/pull/168)
  - This should fixes problem with objects/components at same place.
- Improve Animation Mapping System [`#172`](https://github.com/anatawa12/AvatarOptimizer/pull/172)
  - This should reduce build time
- Disable animating `m_Enabled` of source SkinnedMeshRenderer [`#190`](https://github.com/anatawa12/AvatarOptimizer/pull/190)
  - Animating `m_Enabled` of source SkinnedMeshRenderer now doesn't affects merged SkinnedMeshRenderer
  - If you actually want to enable/disable merged SkinnedMeshRenderer,
    animate `m_Enabled` of merged SkinnedMeshRenderer instead.

### Fixed
- Error is not cleared on build [`#170`](https://github.com/anatawa12/AvatarOptimizer/pull/170)
- Merge PhysBone is not working [`#177`](https://github.com/anatawa12/AvatarOptimizer/pull/177)
  - Previously, values are not copied correctly
- The help box for description of components without description were shown [`#178`](https://github.com/anatawa12/AvatarOptimizer/pull/178)
- Name of Is Animated and Parameter field are not correct [`#183`](https://github.com/anatawa12/AvatarOptimizer/pull/183)
- We cannot set override setting of Colliders to Copy [`#183`](https://github.com/anatawa12/AvatarOptimizer/pull/183)
- Error with MergeToonLit [`#185`](https://github.com/anatawa12/AvatarOptimizer/pull/185)
- Poor word choice in Japanese Translation [`#174`](https://github.com/anatawa12/AvatarOptimizer/pull/174)
- Localization is not applied for some fields [`#186`](https://github.com/anatawa12/AvatarOptimizer/pull/186)

## [0.4.0] - 2023-05-20
### Added
- Japanese Translation: 日本語化 [`#152`](https://github.com/anatawa12/AvatarOptimizer/pull/152)

### Changed
- Save format for MergePhysBone [`#166`](https://github.com/anatawa12/AvatarOptimizer/pull/166)
  - Previously used backed PhysBone component for override values are removed.
  - There are no changes in behaviour. Just migrate your assets.

### Removed
- Delete GameObject feature [`#153`](https://github.com/anatawa12/AvatarOptimizer/pull/153)
  - Use `EditorOnly` tag instead

### Fixed
- Error with Animator at non-root GameObject [`#164`](https://github.com/anatawa12/AvatarOptimizer/pull/164)
- Error with copied MergePhysBone component [`#165`](https://github.com/anatawa12/AvatarOptimizer/pull/165)

## [0.3.5] - 2023-05-15
### Changed
- Internal Errors not relates to any Object are now reported [`#160`](https://github.com/anatawa12/AvatarOptimizer/pull/160)

### Fixed
- Error if there are multiple GameObjects with same path [`#159`](https://github.com/anatawa12/AvatarOptimizer/pull/159)

## [0.3.4] - 2023-05-15
### Fixed
- Reference to Component will become None [`#156`](https://github.com/anatawa12/AvatarOptimizer/pull/156)
- BlendShapes for Eyelids can be broken with FreezeBlendShape [`#154`](https://github.com/anatawa12/AvatarOptimizer/pull/154)

## [0.3.3] - 2023-05-14
## [0.3.2] - 2023-05-14
### Added
- Error Reporting System [`#124`](https://github.com/anatawa12/AvatarOptimizer/pull/124)
  - This adds window shows errors on build
  - This is based on Modular Avatar's Error Reporting Window. thanks `@bdunderscore`
- Website for AvatarOptimizer [`#139`](https://github.com/anatawa12/AvatarOptimizer/pull/139)
  - Available at <https://vpm.anatawa12.com/avatar-optimizer/>
- Manual Bake Avatar [`#147`](https://github.com/anatawa12/AvatarOptimizer/pull/147)
  - Left click the avatar and click `[AvatarOptimizer] Manual Bake Avatar`

### Changed
- Improved & reimplemented Animation (re)generation system [`#137`](https://github.com/anatawa12/AvatarOptimizer/pull/137)
  - This is completely internal changes. Should not break your project
  - In previous implementation, animations for GameObjects moved by MergeBone, MergePhysBone or else doesn't work well
  - This reimplementation should fix this problem

### Fixed
- Migration fails with scenes/prefabs in read-only packages [`#136`](https://github.com/anatawa12/AvatarOptimizer/pull/136)
  - Now, migration process doesn't see any scenes/prefabs in read-only packages.

## [0.3.1] - 2023-05-05
### Fixed
- Can't remove SkinnedMeshRenderer error [`#133`](https://github.com/anatawa12/AvatarOptimizer/pull/133)
  - This error should do nothing bad but it confuses everyone
- Bad behaviour with VRCFury on build [`#134`](https://github.com/anatawa12/AvatarOptimizer/pull/134)

## [0.3.0] - 2023-05-04
### Added
- Make Children of Me [`#100`](https://github.com/anatawa12/AvatarOptimizer/pull/100)
  - As a alternative of feature removal in same pull request
- UnusedBonesByReferencesTool [`#112`](https://github.com/anatawa12/AvatarOptimizer/pull/112)
  - This is port of [UnusedBonesByReferencesTool by Narazaka][UnusedBonesByReferencesTool]
- Support for VRCSDK 3.2.0! [`#117`](https://github.com/anatawa12/AvatarOptimizer/pull/117)
  - This includes support for PhysBone Versions and PhysBone 1.1

[UnusedBonesByReferencesTool]: https://narazaka.booth.pm/items/3831781

### Changed
- Use IEditorOnly instead of mokeypatching VRCSDK [`#102`](https://github.com/anatawa12/AvatarOptimizer/pull/102)
- Move the toggle for Override and the setting of the value after Override closer together. [`#105`](https://github.com/anatawa12/AvatarOptimizer/pull/105)
  - With this changes, the merged PhysBone is now hidden.
  - The merged PhysBone will be shown in Play mode.
- Now we can Copy (instead of Override) `Pull`, `Gravity`, `Immobile` properties even if `Integration Type` is overriden. [`#105`](https://github.com/anatawa12/AvatarOptimizer/pull/105)
  - During migration, if `Integration Type` (previously called `Force`) is configured to be Override, `Pull`, `Gravity`, `Immobile` will be configured to be Override.
  - This is **BREAKING** changes.
- Now we can Copy / Override `Immobile Type` and `Immobile` (strength) separately. [`#105`](https://github.com/anatawa12/AvatarOptimizer/pull/105)
  - Previously, if you override `Immobile Type`, you also required to override `Immobile` but no longer required.
  - This is **BREAKING** changes in the semantics of `immobile` property.
- Upgrade CL4EE to 1.0.0 [`#121`](https://github.com/anatawa12/AvatarOptimizer/pull/121)

### Removed
- **BREAKING** Removed Prefab Safe List [`#95`](https://github.com/anatawa12/AvatarOptimizer/pull/95)
- **BREAKING** Removed RootTransform feature from MergePhysBone [`#100`](https://github.com/anatawa12/AvatarOptimizer/pull/100)
  - See [this issue comment][about-root-transform] for more datails.
- Removed support for VRCSDK 3.1.x. [`#117`](https://github.com/anatawa12/AvatarOptimizer/pull/117)
  - Dropping VRCSDK support is a **BREAKING** changes.
  - However, we may drop old VRCSDK support in the minor releases of AvatarOptimizer in the feature.
  - In the other hand, we promise we'll never drop old VRCSDK support in the patch releases.
  - Notice: in the 0.x.y release, y is a minor releases in this project.

[about-root-transform]: https://github.com/anatawa12/AvatarOptimizer/issues/62#issuecomment-1512586282

### Fixed
- ShouldIgnoreComponentPatch cases compilation error [`#108`](https://github.com/anatawa12/AvatarOptimizer/pull/108)
- Merge PhysBone is working even if parents are differ [`#129`](https://github.com/anatawa12/AvatarOptimizer/pull/129)

## [0.2.8] - 2023-04-19
### Fixed
- NullReferenceException with prefabs in editor for PrefabSafeSet [`#92`](https://github.com/anatawa12/AvatarOptimizer/pull/92)

## [0.2.7] - 2023-04-01
### Added
- Support for VRCSDK 3.1.12 and 3.1.13

## [0.2.6] - 2023-03-31
### Added
- Internationalization support [`#77`](https://github.com/anatawa12/AvatarOptimizer/pull/77)
  - This adds way to translate editor elements.
  - However, no other language translation than English is not added yet.
  - Please feel free to make PullRequest if you can maintain the translation.

### Fixed
- Remove Empty Renderer Object is not shown on the inspector [`#76`](https://github.com/anatawa12/AvatarOptimizer/pull/76)
- normal vector and tangent vector might not be unit length [`#81`](https://github.com/anatawa12/AvatarOptimizer/pull/81)
  - This can be problem with FreezeBlendShape.
- Assertion does not work well [`#85`](https://github.com/anatawa12/AvatarOptimizer/pull/85)
  - This can make invalid mesh
- Mesh is broken if more than 65536 vertices are exists [`#87`](https://github.com/anatawa12/AvatarOptimizer/pull/87)
  - Because we didn't check for vertices count and index format, vertex index can be overflow before.
- Generated assets are invisible for a while [`#88`](https://github.com/anatawa12/AvatarOptimizer/pull/88)

## [0.2.5] - 2023-03-24
### Added
- Show SaveVersion internal property on editor. [`#71`](https://github.com/anatawa12/AvatarOptimizer/pull/71)
  - This makes it easier to make it easier to see prefab overrides

### Changed
- reduce unnecessary modification in PrefabSafeSet/List [`#64`](https://github.com/anatawa12/AvatarOptimizer/pull/64)
  - Previously PrefabSafeSet/List will always generates array size change modification.
  - Now, array size change will be generated when added/removed elements from the collection.
- use ExecuteAlways instead of ExecuteInEditMode [`#72`](https://github.com/anatawa12/AvatarOptimizer/pull/72)

### Fixed
- save version is not saved again [`#69`](https://github.com/anatawa12/AvatarOptimizer/pull/69)
- None is added/removed on the prefab modifications [`#73`](https://github.com/anatawa12/AvatarOptimizer/pull/73)

## [0.2.4] - 2023-03-22
### Changed
- make Accessing v1 error [`#61`](https://github.com/anatawa12/AvatarOptimizer/pull/61)
  - This reduces future mistakes like #59

### Fixed
- RemoveMeshInBox refers old v1 configuration [`#60`](https://github.com/anatawa12/AvatarOptimizer/pull/60)

## [0.2.3] - 2023-03-20
### Fixed
- instantiating material occurs [`#58`](https://github.com/anatawa12/AvatarOptimizer/pull/58)

## [0.2.2] - 2023-03-20
### Added
- Make Children [`#53`](https://github.com/anatawa12/AvatarOptimizer/pull/53)

### Changed
- Do not use cache on applying components now [`#56`](https://github.com/anatawa12/AvatarOptimizer/pull/56)

### Fixed
- NullReferenceException if some component is removed [`#54`](https://github.com/anatawa12/AvatarOptimizer/pull/54)
- save version is not saved. this may break future migration [`#55`](https://github.com/anatawa12/AvatarOptimizer/pull/55)

## [0.2.1] - 2023-03-20
### Fixed
- Migration failed if some renderer is None [`#49`](https://github.com/anatawa12/AvatarOptimizer/pull/49)

## [0.2.0] - 2023-03-19
### Added
- Support for Prerelease in publish system [`#19`](https://github.com/anatawa12/AvatarOptimizer/pull/19)
- Changelogs (including ones for traditional releases) [`#19`](https://github.com/anatawa12/AvatarOptimizer/pull/19)
- Auto Test [`#23`](https://github.com/anatawa12/AvatarOptimizer/pull/23)
- Prefab support [`#11`](https://github.com/anatawa12/AvatarOptimizer/pull/11)

### Changed
- **BREAKING** Save format for many components [`#11`](https://github.com/anatawa12/AvatarOptimizer/pull/11)
  - Even if you added more elements than before on prefab, added elements on prefab instance will be kept.
  - In previous implementation (unity default array prefab overrides implementation), can be broken easily.
- **BREAKING** All materials are merged by default [`#11`](https://github.com/anatawa12/AvatarOptimizer/pull/11)
  - Due to save format migration, every materials will be marked as merged.
  - If you have some materials not to be merged, please re-reconfigure that.

### Fixed
- Fixed IndexOutOfError if there are more bones than bindposes [`#30`](https://github.com/anatawa12/AvatarOptimizer/pull/30)
- SkinnedMeshRenderers without bones will break mesh [`#35`](https://github.com/anatawa12/AvatarOptimizer/pull/35)
- We may forget checking components on disable objects [`#46`](https://github.com/anatawa12/AvatarOptimizer/pull/46)

## [0.1.4]
### Added
- Support for feature Migration [`be0147b`](https://github.com/anatawa12/AvatarOptimizer/commit/be0147bb783cb9ecb5f7193c360f9d1483853e33)

### Changed
- Box editor of RemoveMeshInBox [`15fc931`](https://github.com/anatawa12/AvatarOptimizer/commit/15fc931fed401ba62bba7c2ad51a11bf69e3d044)
- Installer unitypackage name [`b32167f`](https://github.com/anatawa12/AvatarOptimizer/commit/b32167fbacb6f2e6539c3d9c9d02dbde7ac3147e)
- Warn if MergeSkinnedMesh is with SkinnedMeshRenderer with Mesh [`1016aa6`](https://github.com/anatawa12/AvatarOptimizer/commit/1016aa6cd3a70e34bff093f84eca0386c867ce33)

### Fixed
- MergeToonLit is always marked as dirty [`82ba212`](https://github.com/anatawa12/AvatarOptimizer/commit/82ba2126f5c5f53fc0d804fe2387731af27d9471)
- RemoveMeshInBox does not handle bone correctly [`b2fea4f`](https://github.com/anatawa12/AvatarOptimizer/commit/b2fea4fa6c8c6fd99eabb1773a187a427ac66216)

## [0.1.3]
### Fixed
- Merge ToonLit error [`d0f1ef2`](https://github.com/anatawa12/AvatarOptimizer/commit/d0f1ef213919a0a20b280ab19c36371cfe3509d4)
- NRE if some bone is null [`88efc2a`](https://github.com/anatawa12/AvatarOptimizer/commit/88efc2aed4da135e5ac9850c40720807734fd3f3)

## [0.1.2]
### Fixed
- FreezeBlendShape behaviour [`0cebf27`](https://github.com/anatawa12/AvatarOptimizer/commit/0cebf27e6aeb39ead464745c649a350ae8bb7726)

## [0.1.1]
### Added
- (internal) MeshInfo2 system for speed up [`36716dd`](https://github.com/anatawa12/AvatarOptimizer/commit/36716ddb27bf987d0a993b40f4a03f6fc1f79988)
- RemoveMeshInBox component [`96f3526`](https://github.com/anatawa12/AvatarOptimizer/commit/96f35265c0b4cdebc38ed634e58cd1a5490ec5a5)

### Changed
- Make VPAI installer unitypackage install 0.1.x [`6615991`](https://github.com/anatawa12/AvatarOptimizer/commit/66159914b7f609851dcb18971ad34187f9b7c1d9)
- Editor of FreezeBlendShape [`cdd2031`](https://github.com/anatawa12/AvatarOptimizer/commit/cdd20317c449641dc1607780bdba8bab598b8b99)

### Fixed
- Several bugs

## [0.1.0] - 2023-01-16
### Changed
- Move components from `Anatawa12/` to `Optimizer/` [`949d267`](https://github.com/anatawa12/AvatarOptimizer/commit/949d267dcf164ff0762f2314457e7a54ed3c6432)

### Fixed
- Several bugs

## [0.0.2] - 2023-01-15
### Added
- Build Badges onto README [`57614ac`](https://github.com/anatawa12/AvatarOptimizer/commit/57614ac8dfd5b375966fb06dafd8dd99b10ce792)
- How to build onto README [`2ad57cc`](https://github.com/anatawa12/AvatarOptimizer/commit/2ad57cc74799f9b26a1028af1f31ad7199fd81f4)
- Merge ToonLit Material [`a460e3f`](https://github.com/anatawa12/AvatarOptimizer/commit/a460e3f0c9c70e302d0419fcd474240ed1d5624b)

### Fixed
- FreezeBlendShape remains [`95c0d43`](https://github.com/anatawa12/AvatarOptimizer/commit/95c0d4352c8641a7e2cfd1a9f3c8d2c5b9424d42)

## [0.0.1] - 2023-01-13
### Added
- Merge Skinned Mesh
- Merge PhysBone
- Freeze BlendShape
- Merge Bone
- Clear Endpoint Position

[Unreleased]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.8.3...HEAD
[1.8.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.8.2...v1.8.3
[1.8.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.8.1...v1.8.2
[1.8.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.8.0...v1.8.1
[1.8.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.7.13...v1.8.0
[1.7.13]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.7.12...v1.7.13
[1.7.12]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.7.11...v1.7.12
[1.7.11]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.7.10...v1.7.11
[1.7.10]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.7.9...v1.7.10
[1.7.9]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.7.8...v1.7.9
[1.7.8]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.7.7...v1.7.8
[1.7.7]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.7.6...v1.7.7
[1.7.6]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.7.5...v1.7.6
[1.7.5]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.7.4...v1.7.5
[1.7.4]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.7.3...v1.7.4
[1.7.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.7.2...v1.7.3
[1.7.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.7.1...v1.7.2
[1.7.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.7.0...v1.7.1
[1.7.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.13...v1.7.0
[1.6.13]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.12...v1.6.13
[1.6.12]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.11...v1.6.12
[1.6.11]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.10...v1.6.11
[1.6.10]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.9...v1.6.10
[1.6.9]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.8...v1.6.9
[1.6.8]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.7...v1.6.8
[1.6.7]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.6...v1.6.7
[1.6.6]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.5...v1.6.6
[1.6.5]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.4...v1.6.5
[1.6.4]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.3...v1.6.4
[1.6.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.2...v1.6.3
[1.6.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.1...v1.6.2
[1.6.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.6.0...v1.6.1
[1.6.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.11...v1.6.0
[1.5.11]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.10...v1.5.11
[1.5.10]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.9...v1.5.10
[1.5.9]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.8...v1.5.9
[1.5.8]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.7...v1.5.8
[1.5.7]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.6...v1.5.7
[1.5.6]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.5...v1.5.6
[1.5.5]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.4...v1.5.5
[1.5.4]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.3...v1.5.4
[1.5.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.2...v1.5.3
[1.5.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.1...v1.5.2
[1.5.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.5.0...v1.5.1
[1.5.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.4.3...v1.5.0
[1.4.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.4.2...v1.4.3
[1.4.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.4.1...v1.4.2
[1.4.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.4.0...v1.4.1
[1.4.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.3.4...v1.4.0
[1.3.4]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.3.3...v1.3.4
[1.3.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.3.2...v1.3.3
[1.3.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.3.1...v1.3.2
[1.3.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.3.0...v1.3.1
[1.3.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.2.0...v1.3.0
[1.2.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.1.1...v1.2.0
[1.1.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.1.0...v1.1.1
[1.1.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.12...v1.0.0
[0.4.12]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.11...v0.4.12
[0.4.11]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.10...v0.4.11
[0.4.10]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.9...v0.4.10
[0.4.9]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.8...v0.4.9
[0.4.8]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.7...v0.4.8
[0.4.7]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.6...v0.4.7
[0.4.6]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.5...v0.4.6
[0.4.5]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.4...v0.4.5
[0.4.4]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.3...v0.4.4
[0.4.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.2...v0.4.3
[0.4.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.1...v0.4.2
[0.4.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.4.0...v0.4.1
[0.4.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.3.5...v0.4.0
[0.3.5]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.3.4...v0.3.5
[0.3.4]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.3.3...v0.3.4
[0.3.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.3.2...v0.3.3
[0.3.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.3.1...v0.3.2
[0.3.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.3.0...v0.3.1
[0.3.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.8...v0.3.0
[0.2.8]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.7...v0.2.8
[0.2.7]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.6...v0.2.7
[0.2.6]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.5...v0.2.6
[0.2.5]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.4...v0.2.5
[0.2.4]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.3...v0.2.4
[0.2.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.2...v0.2.3
[0.2.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.1...v0.2.2
[0.2.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.2.0...v0.2.1
[0.2.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.1.4...v0.2.0
[0.1.4]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.1.3...v0.1.4
[0.1.3]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.1.2...v0.1.3
[0.1.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.1.1...v0.1.2
[0.1.1]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.0.2...v0.1.0
[0.0.2]: https://github.com/anatawa12/AvatarOptimizer/compare/v0.0.1...v0.0.2
[0.0.1]: https://github.com/anatawa12/AvatarOptimizer/releases/tag/v0.0.1
