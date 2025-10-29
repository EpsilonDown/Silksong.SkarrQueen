using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using System.Collections;
using System.Linq;
using UnityEngine;
using static SceneLoad;

namespace SkarrQueen.Behaviours;

internal class TrapLoader : MonoBehaviour
{
    internal static GameObject Trap;
    private IEnumerator Start()
    {
        yield return AssetManager.LoadBundleAssets();

        var sceneObj = AssetManager.Get<GameObject>("Boss Scene");
        if (!sceneObj)
        {
            Debug.Log("Failed to get Trap!");
            yield break;
        }
        Trap = sceneObj.transform.Find("Barb Traps").Find("Trapper Barb Trap").gameObject;
        Trap.AddComponent<Trap>();
        Debug.Log("Traploader: Trap loaded in!");
        Trap.SetActive(false);
    }
    internal static GameObject GetTrap()
    {
        return Trap;
    }
    private void OnDestroy()
    {
        AssetManager.UnloadManualBundles();
    }
}
internal class BladeEditor : MonoBehaviour
{
    private PlayMakerFSM _control = null;
    private void Awake()
    {
        _control = gameObject.LocateMyFSM("Control");
        FsmState Break = _control.FsmStates.First(state => state.Name == "Break");
        var breakactions = Break.Actions.ToList();
        breakactions.Insert(0, new InvokeMethod(BecomeTrap));
        Break.Actions = breakactions.ToArray();
    }
    private void BecomeTrap()
    {
        GameObject trap = Instantiate(TrapLoader.GetTrap());
        trap.transform.position = gameObject.transform.position;
        trap.SetActive(true);
    }
    
}
internal class Trap : MonoBehaviour
{
    private PlayMakerFSM _trapcontrol;
    private void Awake()
    {
        _trapcontrol = gameObject.LocateMyFSM("Control");
        var recollect = _trapcontrol.FsmStates.First(state => state.Name == "Reparent");
        var init = _trapcontrol.FsmStates.First(state => state.Name == "Init");
        var dormant = _trapcontrol.FsmStates.First(state => state.Name == "Dormant");
        var throwtrap = _trapcontrol.FsmStates.First(state => state.Name == "Throw Out");
        var antic = _trapcontrol.FsmStates.First(state => state.Name == "Antic");



        var throwactions = throwtrap.Actions.ToList();
        throwactions.RemoveAt(10);
        throwactions.RemoveAt(8);
        throwactions.RemoveAt(1);
        throwtrap.actions = throwactions.ToArray();

        recollect.Actions = new FsmStateAction[] { new InvokeMethod(Destroyitself) }; ;
        _trapcontrol.SetState("Init");

        StartCoroutine(Throwauto());
    }
    private IEnumerator Throwauto()
    {
        yield return new WaitForSeconds(0.1f);
        _trapcontrol.SendEvent("THROW");
    }
    private void Destroyitself()
    {
        Destroy(gameObject);
    }
}
internal class PermanentTrap : MonoBehaviour
{
    private PlayMakerFSM _trapcontrol;
    private void Awake()
    {
        _trapcontrol = gameObject.LocateMyFSM("Control");
        var trapbreak = _trapcontrol.FsmStates.First(state => state.Name == "Break");
        var trapbreak2 = _trapcontrol.FsmStates.First(state => state.Name == "Small Break");
        trapbreak.Actions = new FsmStateAction[] { };
        trapbreak2.Transitions = new FsmTransition[] { };
        trapbreak2.Actions = new FsmStateAction[] { };
        trapbreak.Transitions = new FsmTransition[] { };
    }
}