using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
    public string SceneName;

    IEnumerator Start()
    {
        var op = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
        op.allowSceneActivation = false;
        while (op.progress < 0.9f)
        {
            yield return new WaitForEndOfFrame();
        }
        op.allowSceneActivation = true;
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(SceneName));
    }
}
