using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ParallaxPrefabTrigger : MonoBehaviour
{
    [SerializeField] private GameObject firstParallaxPrefab;
    [SerializeField] private GameObject secondParallaxPrefab;
    [SerializeField] private GameObject currentParallaxInstance;

    private readonly Dictionary<Transform, HashSet<Collider>> playerContacts =
        new Dictionary<Transform, HashSet<Collider>>();

    private int currentPrefabIndex;
    private bool hasValidPrefabs;

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void Awake()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (!triggerCollider.isTrigger)
        {
            Debug.LogError("[ParallaxPrefabTrigger] The attached Collider must have Is Trigger enabled.", this);
        }

        hasValidPrefabs = ValidatePrefabs();
        if (hasValidPrefabs)
        {
            FindCurrentParallaxInstance();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasValidPrefabs || !TryGetPlayerRoot(other, out Transform playerRoot))
        {
            return;
        }

        if (!playerContacts.TryGetValue(playerRoot, out HashSet<Collider> contacts))
        {
            contacts = new HashSet<Collider>();
            playerContacts.Add(playerRoot, contacts);
        }

        if (contacts.Add(other) && contacts.Count == 1)
        {
            SwitchParallaxPrefab();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!TryGetPlayerRoot(other, out Transform playerRoot)
            || !playerContacts.TryGetValue(playerRoot, out HashSet<Collider> contacts))
        {
            return;
        }

        contacts.Remove(other);
        if (contacts.Count == 0)
        {
            playerContacts.Remove(playerRoot);
        }
    }

    private bool ValidatePrefabs()
    {
        if (firstParallaxPrefab == null || secondParallaxPrefab == null)
        {
            Debug.LogError("[ParallaxPrefabTrigger] Assign both Parallax prefab fields in the Inspector.", this);
            return false;
        }

        if (firstParallaxPrefab == secondParallaxPrefab)
        {
            Debug.LogError("[ParallaxPrefabTrigger] The two Parallax prefab fields must reference different prefabs.", this);
            return false;
        }

        return true;
    }

    private void FindCurrentParallaxInstance()
    {
        if (currentParallaxInstance != null)
        {
            currentPrefabIndex = currentParallaxInstance.name == secondParallaxPrefab.name ? 1 : 0;
            return;
        }

        currentParallaxInstance = GameObject.Find(firstParallaxPrefab.name);
        if (currentParallaxInstance != null)
        {
            currentPrefabIndex = 0;
            return;
        }

        currentParallaxInstance = GameObject.Find(secondParallaxPrefab.name);
        if (currentParallaxInstance != null)
        {
            currentPrefabIndex = 1;
        }
    }

    private void SwitchParallaxPrefab()
    {
        int nextPrefabIndex = currentPrefabIndex == 0 ? 1 : 0;
        GameObject nextPrefab = nextPrefabIndex == 0 ? firstParallaxPrefab : secondParallaxPrefab;

        if (currentParallaxInstance == null)
        {
            currentParallaxInstance = Instantiate(nextPrefab);
        }
        else
        {
            Transform currentTransform = currentParallaxInstance.transform;
            GameObject replacement = Instantiate(
                nextPrefab,
                currentTransform.position,
                currentTransform.rotation,
                currentTransform.parent);
            replacement.transform.localScale = currentTransform.localScale;

            Destroy(currentParallaxInstance);
            currentParallaxInstance = replacement;
        }

        currentPrefabIndex = nextPrefabIndex;
    }

    private static bool TryGetPlayerRoot(Collider other, out Transform playerRoot)
    {
        playerRoot = null;
        if (other == null)
        {
            return false;
        }

        PlayerController playerController = other.GetComponentInParent<PlayerController>();
        if (playerController != null)
        {
            playerRoot = playerController.transform;
            return true;
        }

        Transform root = other.transform.root;
        if (other.CompareTag("Player") || root.CompareTag("Player"))
        {
            playerRoot = root;
            return true;
        }

        return false;
    }
}
