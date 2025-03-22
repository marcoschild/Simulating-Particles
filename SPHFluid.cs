using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class SPHFluid : MonoBehaviour
{
    public int particleCount = 500;  // Number of particles
    public float smoothingRadius = 1.5f;
    public float viscosity = 0.1f;
    public float pressureMultiplier = 200f;
    public float restDensity = 1f;
    public float gravity = -9.81f;

    public GameObject particlePrefab; // Assign this in the Unity Inspector

    private List<FluidParticle> particles = new List<FluidParticle>();
    private Vector2 squarePosition;
    private float squareSize = 2.0f; // Adjust this to fit the size of your square

    void Start()
    {
        StartCoroutine(FindSquareAndInitialize());
    }

    IEnumerator FindSquareAndInitialize()
    {
        Debug.Log("Searching for Square object...");

        GameObject square = GameObject.Find("Square");

        // If Square is not found, try finding by tag
        if (square == null)
        {
            Debug.LogWarning("Square not found by name! Trying to find by tag 'SquareTag'...");
            square = GameObject.FindWithTag("SquareTag");
        }

        // If still not found, log all objects in the scene using the new FindObjectsByType() method
        if (square == null)
        {
            Debug.LogError("Square object not found! Make sure it is named 'Square' or tagged 'SquareTag'. Available objects in the scene:");
            foreach (GameObject obj in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                Debug.Log(obj.name);
            }
            yield break;
        }

        squarePosition = square.transform.position;
        float spawnOffset = squareSize / 2f;

        Debug.Log("Square found at: " + squarePosition);

        yield return null; // Ensure all objects are loaded before spawning particles

        Debug.Log("Starting Fluid Simulation...");

        for (int i = 0; i < particleCount; i++)
        {
            Vector2 pos = squarePosition + new Vector2(Random.Range(-spawnOffset, spawnOffset), Random.Range(-spawnOffset, spawnOffset));

            FluidParticle particle = new FluidParticle(pos);

            // Instantiate the prefab
            GameObject obj = Instantiate(particlePrefab, pos, Quaternion.identity);
            obj.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
            obj.GetComponent<SpriteRenderer>().sortingOrder = 5;

            particle.gameObject = obj;
            particles.Add(particle);

            Debug.Log($"Particle {i} spawned at {pos}");
        }

        Debug.Log($"Total Particles Created: {particles.Count}");
    }

    void Update()
    {
        foreach (var p in particles)
        {
            p.ApplyGravity(gravity);
        }

        ComputeDensities();
        ComputeForces();
        Integrate();
        RenderParticles();
    }

    void ComputeDensities()
    {
        foreach (var p in particles)
        {
            p.density = 0;
            foreach (var neighbor in particles)
            {
                float r = Vector2.Distance(p.position, neighbor.position);
                if (r < smoothingRadius)
                {
                    float weight = Mathf.Pow(smoothingRadius - r, 2);
                    p.density += weight;
                }
            }
        }
    }

    void ComputeForces()
    {
        foreach (var p in particles)
        {
            p.pressure = pressureMultiplier * (p.density - restDensity);
            Vector2 force = Vector2.zero;

            foreach (var neighbor in particles)
            {
                float r = Vector2.Distance(p.position, neighbor.position);
                if (r < smoothingRadius && r > 0)
                {
                    Vector2 dir = (neighbor.position - p.position).normalized;
                    float weight = (smoothingRadius - r);
                    force += dir * weight * (p.pressure + neighbor.pressure) / 2f;
                }
            }

            p.force = force + Vector2.up * gravity;
        }
    }

    void Integrate()
    {
        float minX = squarePosition.x - squareSize / 2f;
        float maxX = squarePosition.x + squareSize / 2f;
        float minY = squarePosition.y - squareSize / 2f;
        float maxY = squarePosition.y + squareSize / 2f;

        foreach (var p in particles)
        {
            p.velocity += p.force * (Time.deltaTime * 10f);
            p.position += p.velocity * (Time.deltaTime * 10f);

            // Keep particles inside square bounds
            p.position.x = Mathf.Clamp(p.position.x, minX, maxX);
            p.position.y = Mathf.Clamp(p.position.y, minY, maxY);
        }
    }

    void RenderParticles()
    {
        foreach (var p in particles)
        {
            if (p.gameObject != null)
            {
                p.gameObject.transform.position = p.position;
                Debug.Log($"Particle at {p.position}");
            }
        }
    }
}

public class FluidParticle
{
    public Vector2 position;
    public Vector2 velocity = Vector2.zero;
    public Vector2 force = Vector2.zero;
    public float density = 0f;
    public float pressure = 0f;
    public GameObject gameObject; // Stores the GameObject for rendering

    public FluidParticle(Vector2 pos)
    {
        position = pos;
    }

    public void ApplyGravity(float gravity)
    {
        force += new Vector2(0, gravity);
    }
}
