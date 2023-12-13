# Unity_RenderPipeline_Coexistance
Usefull scripts to author a project having both URP and HDRP for unity 2022LTS and 2023LTS.

# Installation
Copy past the CoexistanceScripts folder the files in your project.

# Usage
Use name endings on scene name to specify intent:
- "_COMMON" suffix means that this scene contains part of a scene that works with both pipeline (everything non rendering, or rendering that can adapt).
- "_URP" suffix means that this scene contains the lighting information and specific rendering component that is only for URP.
- "_HDRP" suffix means that this scene contains the lighting information and specific rendering component that is only for HDRP.

Without ending or a different one, your scene will not be affected by the given scripts here.

For each scene, you can use the LightmapData component in order to stored your baked lightmaps per render pipeline. We expect only 1 per scene so only first found can have an impact.

In URP, ReflectionProbes that was previously used by HDRP have their custom Cubemap nullified. Use the ReflectionProbeData component to store the one used in URP. It should be on the same GameObject than the ReflectionProbe it affects.

# What it changes
1. By changing the pipeline type, it reopen your scenes (the ones for the previous pipeline are removed and the ones for the new pipeline are added)
2. By loading a scene with one of the prefix above, it automaticaly load the paired other scene (for _URP/_HDRP, it adds the _COMMON, and vice versa)
3. After scene is loaded, some patch are processed:
    - Replace the baked lightmap with the one stored in LightmapData according to your current pipeline if any.
    - Replace URP's ReflectionProbe custom cubemap with the one stored in ReflectionProbeData if any.
    - If pipeline in use is URP, ensure the _URP is set active. We do this because Sky is pipeline specific and the legacy environment system used in URP works with ActiveScene. (Not the case in HDRP)
