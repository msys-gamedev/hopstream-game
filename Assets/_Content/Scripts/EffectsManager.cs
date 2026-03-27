using System.Collections.Generic;
using UnityEngine;

public class EffectsManager : MonoBehaviour
{
    public GameObject startVfx;
    public GameObject endVfx;
    public List<ParticleSystem> particles = new List<ParticleSystem>();

    private void Awake()
    {
        FillList();
        DisableParticle();
    }

    public void FillList()
    {
        particles.Clear();

        if (startVfx != null)
            particles.AddRange(startVfx.GetComponentsInChildren<ParticleSystem>());

        if (endVfx != null)
            particles.AddRange(endVfx.GetComponentsInChildren<ParticleSystem>());
    }

    public void EnableParticle()
    {
        foreach (var t in particles)
        {
            t.Play();
        }
    }

    public void DisableParticle()
    {
        foreach (var t in particles)
        {
            t.Stop();
        }
    }
}