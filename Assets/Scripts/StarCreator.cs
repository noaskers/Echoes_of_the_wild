using UnityEngine;

public class StarCreator : MonoBehaviour
{
    [ContextMenu("Create Stars")]
    public void CreateStars()
    {
        // Create the particle system
        GameObject starObject = new GameObject("Stars");
        ParticleSystem stars = starObject.AddComponent<ParticleSystem>();
        
        // Configure the particle system
        ConfigureStars(stars);
        
        Debug.Log("Stars created! Assign this to your DayNightCycle script.");
    }

    void ConfigureStars(ParticleSystem ps)
    {
        var main = ps.main;
        main.duration = 10f;
        main.loop = true;
        main.startLifetime = 1000f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.1f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 180f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 1000f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 150f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        
        // Create a gradient that fades in/out
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(Color.white, 0.0f), 
                new GradientColorKey(Color.white, 1.0f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0.0f, 0.0f), 
                new GradientAlphaKey(1.0f, 0.1f), 
                new GradientAlphaKey(1.0f, 0.9f), 
                new GradientAlphaKey(0.0f, 1.0f) 
            }
        );
        colorOverLifetime.color = gradient;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateStarMaterial();
    }

    Material CreateStarMaterial()
    {
        // Create a simple particle material
        Shader particleShader = Shader.Find("Particles/Standard Unlit");
        Material starMaterial = new Material(particleShader);
        starMaterial.color = Color.white;
        return starMaterial;
    }
}