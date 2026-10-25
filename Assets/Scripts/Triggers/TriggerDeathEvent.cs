using UnityEngine;

public class TriggerDeathEvent : MonoBehaviour
{
    public enum DeathType
    {
        StalagmiteAbove,
        FallOnStalagmite
        // Add more if needed
    }

    public DeathType deathType = DeathType.StalagmiteAbove;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameObject.Find("TimeManager").SetActive(false);
            GameObject[] deathObjects = GameObject.FindGameObjectsWithTag("Death");

            if (deathObjects.Length == 0)
            {
                Debug.LogWarning("No objects found with tag 'Death'.");
                return;
            }

            GameObject selectedDeathObject = null;

            // Prefer an active one
            foreach (var obj in deathObjects)
            {
                if (obj.activeInHierarchy)
                {
                    selectedDeathObject = obj;
                    break;
                }
            }

            // If no active one was found, just take the first
            if (selectedDeathObject == null)
                selectedDeathObject = deathObjects[0];

            YouDied youDied = selectedDeathObject.GetComponent<YouDied>();

            if (youDied != null)
            {
                switch (deathType)
                {
                    case DeathType.StalagmiteAbove:
                        youDied.PlayStalagmiteAboveDeath();
                        break;
                    case DeathType.FallOnStalagmite:
                        youDied.PlayFallOnStalagmiteDeath();
                        break;
                }
            }
            else
            {
                Debug.LogWarning("YouDied component not found on selected 'Death' object.");
            }
        }
    }
}
