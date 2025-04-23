using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class GameManager : MonoBehaviour
{
    #region Singleton
    public static GameManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
        }

        DontDestroyOnLoad(this);
    }
    #endregion

    public GameObject playerMoveObj;
    public GameObject metalBarObj;
    public GameObject portalObj;

    private void OnEnable()
    {
        PortalTeleporter.WentToScaledWorld += ScaleUp;
        PortalTeleporter.LeftScaledWorld += ScaleDown;
        PortalTeleporter.MakeBlockadeDisappear += Disappear;
    }

    private void OnDisable()
    {
        PortalTeleporter.WentToScaledWorld -= ScaleUp;
        PortalTeleporter.LeftScaledWorld -= ScaleDown;
        PortalTeleporter.MakeBlockadeDisappear -= Disappear;
    }

    private void ScaleUp()
    {
        playerMoveObj.GetComponent<DynamicMoveProvider>().moveSpeed = 10f;
    }

    private void ScaleDown()
    {
        playerMoveObj.GetComponent<DynamicMoveProvider>().moveSpeed = 2.5f;
    }

    private void Disappear()
    {
        metalBarObj.SetActive(false);
        portalObj.SetActive(false);
    }
}
