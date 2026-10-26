using System.Collections;
using UnityEngine;

public class RainSystem : MonoBehaviour
{
    [SerializeField] private ParticleSystem rain;
    [SerializeField] private Vector2 rainDuration = new Vector2(20f, 60f);
    [SerializeField] private Vector2 clearDuration = new Vector2(30f, 90f);
    [SerializeField] private bool startRaining;

    private void Start()
    {
        if (startRaining) rain.Play();
        else rain.Stop();

        StartCoroutine(WeatherLoop());
    }

    private IEnumerator WeatherLoop()
    {
        bool raining = startRaining;

        while (true)
        {
            Vector2 range = raining ? rainDuration : clearDuration;
            yield return new WaitForSeconds(Random.Range(range.x, range.y));

            raining = !raining;

            if (raining) rain.Play();
            else rain.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}