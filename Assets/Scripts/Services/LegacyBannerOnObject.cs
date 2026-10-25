using UnityEngine;
using UnityEngine.Advertisements;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

/// <summary>
/// Title screen Unity Ads banner that supports safe retries, optional hide when another GameObject is active,
/// and optional Unity Scheduled Notifications integration.
/// </summary>
public class LegacyBannerOnObject : MonoBehaviour, IUnityAdsInitializationListener
{
#if UNITY_ANDROID
    [SerializeField] private string gameId = "5980443";
    [SerializeField] private string adUnitId = "Banner_Android";
#elif UNITY_IOS
    [SerializeField] private string gameId = "5980442";
    [SerializeField] private string adUnitId = "Banner_iOS";
#else
    private string gameId = "UnsupportedPlatform";
    private string adUnitId = "UnsupportedPlatform";
#endif

    [Header("Banner Settings")]
    [SerializeField] private bool testMode = true;
    [SerializeField] private BannerPosition bannerPosition = BannerPosition.BOTTOM_CENTER;

    [Header("Hide When Active Object")]
    [SerializeField] private bool enableHideWhenActive = false;
    [SerializeField] private GameObject hideWhenActive;

    [Header("Notifications")]
    [SerializeField] private bool enableNotifications = true;
    [SerializeField] private string notificationTitle = "TimeCat awaits!";
    [SerializeField] private string notificationText = "Come back and continue your adventure!";
    [SerializeField] private int hoursUntilNotification = 6;

    private bool initialized;
    private bool bannerVisible;
    private bool retryScheduled;
    private bool notificationsSet;

    // --------------------------------------------------------------------

    private void Start()
    {
        Debug.Log("🟡 Initializing Unity Ads (Title screen only)...");
        if (gameId == "UnsupportedPlatform")
        {
            Debug.LogWarning("[LegacyBannerOnObject] Unsupported platform for Unity Ads.");
            return;
        }

        Advertisement.Initialize(gameId, testMode, this);
    }

    private void Update()
    {
        if (enableHideWhenActive && hideWhenActive)
        {
            bool shouldHide = hideWhenActive.activeInHierarchy;
            if (shouldHide && bannerVisible)
            {
                HideBanner();
            }
            else if (!shouldHide && !bannerVisible && initialized)
            {
                ShowBanner();
            }
        }
    }

    public void OnInitializationComplete()
    {
        Debug.Log("✅ Unity Ads initialized.");
        initialized = true;

        if (enableNotifications && !notificationsSet)
            ScheduleNotification();

        LoadAndShowBanner();
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogError($"❌ Unity Ads init failed: {error} - {message}");
    }

    // --------------------------------------------------------------------

    private void LoadAndShowBanner()
    {
        if (!initialized) return;

        Debug.Log($"📡 Loading banner at {bannerPosition}...");
        Advertisement.Banner.SetPosition(bannerPosition);

        var loadOpts = new BannerLoadOptions
        {
            loadCallback = OnBannerLoaded,
            errorCallback = OnBannerError
        };

        Advertisement.Banner.Load(adUnitId, loadOpts);
    }

    private void OnBannerLoaded()
    {
        Debug.Log("📢 Banner loaded.");
        ShowBanner();
    }

    private void ShowBanner()
    {
        if (!initialized) return;

        var showOpts = new BannerOptions
        {
            showCallback = () =>
            {
                bannerVisible = true;
                Debug.Log("✅ Banner shown.");
            },
            hideCallback = () =>
            {
                bannerVisible = false;
                Debug.Log("🚫 Banner hidden.");
            },
            clickCallback = () => Debug.Log("👆 Banner clicked.")
        };

        Advertisement.Banner.Show(adUnitId, showOpts);
    }

    private void OnBannerError(string message)
    {
        Debug.LogError($"❌ Banner load error: {message}");

        Advertisement.Banner.Hide(true);
        bannerVisible = false;

        if (!retryScheduled)
        {
            retryScheduled = true;

            float delay = message.Contains("No fill") ? 30f : 6f;
            Debug.Log($"⏳ Will retry banner in {delay} seconds...");

            Invoke(nameof(RetryBanner), delay);
        }
    }

    private void RetryBanner()
    {
        retryScheduled = false;
        if (!this || !initialized) return;

        Debug.Log("🔁 Retrying banner load...");
        LoadAndShowBanner();
    }

    // --------------------------------------------------------------------

    private void OnDisable()
    {
        HideBanner();
    }

    private void OnDestroy()
    {
        HideBanner();
    }

    private void HideBanner()
    {
        if (!bannerVisible) return;

        Debug.Log("🚫 Hiding banner (scene change or disable).");
        Advertisement.Banner.Hide(true);
        bannerVisible = false;
    }

    // --------------------------------------------------------------------
    // 📱 Notifications
    private void ScheduleNotification()
    {
#if UNITY_ANDROID
        var channel = new AndroidNotificationChannel()
        {
            Id = "timecat_channel",
            Name = "TimeCat Notifications",
            Importance = Importance.Default,
            Description = "Notifications reminding players to return to TimeCat."
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);

        var notification = new AndroidNotification
        {
            Title = notificationTitle,
            Text = notificationText,
            SmallIcon = "icon_0",
            FireTime = System.DateTime.Now.AddHours(hoursUntilNotification)
        };

        AndroidNotificationCenter.SendNotification(notification, "timecat_channel");
        notificationsSet = true;

        Debug.Log($"📅 Scheduled notification in {hoursUntilNotification} hours.");
#endif
    }
}
