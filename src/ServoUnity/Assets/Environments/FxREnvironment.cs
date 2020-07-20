using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class FxREnvironment : MonoBehaviour
{
    [SerializeField] private Material SkyboxMaterial;
    [SerializeField] private AmbientMode AmbientLightSource;
    [SerializeField] [ColorUsage(true, true)] private Color AmbientLightColor;
    [SerializeField] private List<ReflectionProbe> ReflectionProbs;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        // Set up the lighting and skybox
        RenderSettings.skybox = Instantiate(SkyboxMaterial);
        RenderSettings.ambientMode = AmbientLightSource;
        RenderSettings.ambientLight = AmbientLightColor;
        // Let the environment render before we render the reflections
        yield return new WaitForEndOfFrame();
        // Snapshot the reflections
        // TODO: this could probably be baked 
        foreach (var reflectionProbe in ReflectionProbs)
        {
            reflectionProbe.RenderProbe();
        }

        // Rotate the skybox opposite the direction the environment is rotated, so it properly aligns 
        if (RenderSettings.skybox.HasProperty("_Rotation"))
        {
            var baseRotation = SkyboxMaterial.GetFloat("_Rotation");
            var adjustedRotation = baseRotation - transform.rotation.eulerAngles.y;
            RenderSettings.skybox.SetFloat("_Rotation", adjustedRotation);
        }
    }

}