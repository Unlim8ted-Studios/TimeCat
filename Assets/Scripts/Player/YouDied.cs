using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class YouDied : MonoBehaviour
{
    public GameObject deathBackgroundObject;       // Assign the GameObject with the background SpriteRenderer
    public GameObject deathAnimationObject;        // Assign the GameObject with the animation SpriteRenderer
    public float frameDelay = 0.1f;
    public float delayBeforeReload = 1f;

    public Sprite[] stalagmiteAboveFrames;
    public Sprite[] fallOnStalagmiteFrames;

    private SpriteRenderer deathAnimationRenderer;
    private bool hasTriggered = false;

    void Start()
    {
        if (deathBackgroundObject != null)
            deathBackgroundObject.SetActive(false);
        if (deathAnimationObject != null)
        {
            deathAnimationObject.SetActive(false);
            deathAnimationRenderer = deathAnimationObject.GetComponent<SpriteRenderer>();
        }
    }

    public void PlayStalagmiteAboveDeath()
    {
        StartCoroutine(PlayDeathAnimation(stalagmiteAboveFrames));
    }

    public void PlayFallOnStalagmiteDeath()
    {
        StartCoroutine(PlayDeathAnimation(fallOnStalagmiteFrames));
    }

    private IEnumerator PlayDeathAnimation(Sprite[] animationFrames)
    {
        if (deathBackgroundObject != null)
            deathBackgroundObject.SetActive(true);

        if (deathAnimationObject != null && deathAnimationRenderer != null)
        {
            deathAnimationObject.SetActive(true);

            foreach (var frame in animationFrames)
            {
                deathAnimationRenderer.sprite = frame;
                yield return new WaitForSeconds(frameDelay);
            }
        }

        yield return new WaitForSeconds(delayBeforeReload);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
