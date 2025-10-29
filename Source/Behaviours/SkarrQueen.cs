using System.Collections;
using System.Linq;
using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace SkarrQueen.Behaviours;

[RequireComponent(typeof(tk2dSpriteAnimator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayMakerFSM))]
internal class SkarrQueenKarmelita : MonoBehaviour {

    private tk2dSpriteAnimator _anim = null!;
    private Rigidbody2D _body = null!;
    private PlayMakerFSM _control = null!;
    private Transform _heroTransform = null!;
    private FsmInt MaxCharge = new FsmInt("Max Charge");
    private FsmInt ChargeCount = new FsmInt("Charged");
    private FsmFloat ChargeSpeed = new FsmFloat("Charge Speed");
    private FsmInt MaxBounce = new FsmInt("Max Bounce");
    private FsmInt BounceCount = new FsmInt("Bounced");
    private FsmFloat BouncePower = new FsmFloat("Bounce Power");
    private FsmInt MaxCombo = new FsmInt("Max Combo");
    private FsmInt ComboCount = new FsmInt("Combod");
    private FsmEvent Charge = new FsmEvent("CHARGE");
    private FsmEvent Bounce = new FsmEvent("BOUNCE");
    private FsmEvent Combo = new FsmEvent("NEXT");
    private void Awake() {
        Debug.Log("Queen is LOADED!!!!");
        SetupBoss();
    }

    /// <summary>
    /// Set up the modded boss.
    /// </summary>
    private void SetupBoss() {

        GetComponents();
        IncreaseHealth();
        StartCoroutine(InstaP3());
        ComboAttacks();
        ModifyPhase2();
        PlaceUnbreakableTraps();
    }
    private void GetComponents() {
        _anim = GetComponent<tk2dSpriteAnimator>();
        _body = GetComponent<Rigidbody2D>();
        gameObject.GetComponent<DamageHero>().damageDealt = 2;
        _control = gameObject.LocateMyFSM("Control");
        gameObject.transform.Find("BallSlash").gameObject.GetComponent<DamageHero>().damageDealt = 2;

    }

    private void IncreaseHealth() {
        var health = GetComponent<HealthManager>();
        #if DEBUG
        health.hp = 100;
        #endif
        health.hp = 2000;
        
    }
    private IEnumerator InstaP3()
    {
        yield return null;
        yield return null;
        _control.Fsm.GetFsmBool("Double Throws").Value = true;
        _control.Fsm.GetFsmFloat("Idle Min").Value = 0.25f;
        _control.Fsm.GetFsmFloat("Idle Max").Value = 0.25f;
        _control.Fsm.GetFsmInt("P2 HP").Value = 1600;
        _control.Fsm.GetFsmInt("P3 HP").Value = 800;
        FsmState Choice = _control.FsmStates.First(state => state.Name == "Attack Choice");
        if (Choice.Actions[12] is SendRandomEventV4 sendrandom)
        {
            sendrandom.activeBool = false;
        }
    }

    private void ComboAttacks()
    {
        #region states
        FsmState Jumpback = _control.FsmStates.First(state => state.Name == "Jump Back");
        FsmState Charging = _control.FsmStates.First(state => state.Name == "Dash Grind");
        FsmState Idle = _control.FsmStates.First(state => state.Name == "Start Idle");

        FsmState Spinlaunch = _control.FsmStates.First(state => state.Name == "Jump Launch");
        FsmState Spearslam = _control.FsmStates.First(state => state.Name == "Spear Slam");
        FsmState SlamHit = _control.FsmStates.First(state => state.Name == "Soft Land");

        FsmState ChargeStart = _control.FsmStates.First(state => state.Name == "Jump Back Dir");
        FsmState ThrowStart = _control.FsmStates.First(state => state.Name == "Throw Antic");
        FsmState SlashStart = _control.FsmStates.First(state => state.Name == "Slash Antic");
        FsmState SpinStart = _control.FsmStates.First(state => state.Name == "Cyclone Antic");
        FsmState AirThrowStart = _control.FsmStates.First(state => state.Name == "Launch Antic");
        FsmState SlamStart = _control.FsmStates.First(state => state.Name == "Jump Antic");

        FsmState ThrowEnd = _control.FsmStates.First(state => state.Name == "Double Throw?");
        FsmState SlashEnd = _control.FsmStates.First(state => state.Name == "Slash End");
        FsmState SpinSemiEnd = _control.FsmStates.First(state => state.Name == "Cyclone 4");
        FsmState SpinEnd = _control.AddState("Cyclone End");
        FsmState AirThrowEnd = _control.FsmStates.First(state => state.Name == "Throw Land");
        FsmState ChargeEnd = _control.FsmStates.First(state => state.Name == "Dash Grind Spin 3");
        FsmState SlamEnd = _control.FsmStates.First(state => state.Name == "Spin Attack Land");

        var MultiCharge = _control.AddState("Charge Again");
        var MultiBounce = _control.AddState("Bounce Again");
        var ReBounce = _control.AddState("ReBounce");
        var ThrowCombo = _control.AddState("Combo Throw");
        var SlashCombo = _control.AddState("Combo Slash");
        var SpinCombo = _control.AddState("Combo Spin");
        var AirThrowCombo = _control.AddState("Combo Air");

        #endregion
        MaxBounce.Value = 2;
        MaxCharge.Value = 2;
        MaxCombo.Value = 2;

        #region slightly faster slashes
        var slashactions = SlashStart.Actions.ToList();
        slashactions.Insert(0, new Wait
        {
            time = 0.5f,
            finishEvent = FsmEvent.Finished,
        });
        SlashStart.Actions = slashactions.ToArray();
        var cycloneactions = SpinStart.Actions.ToList();
        cycloneactions.Insert(0, new Wait
        {
            time = 0.4f,
            finishEvent = FsmEvent.Finished,
        });
        SpinStart.Actions = cycloneactions.ToArray();
        #endregion

        //Idle: Reset Combo, Dash, Bounce Count
        #region Idle setup

        var idleactions = Idle.Actions.ToList();
        idleactions.Insert(0, new SetIntValue
        {
            intVariable = ChargeCount,
            intValue = 0,
        });
        idleactions.Insert(0, new SetFloatValue
        {
            floatVariable = ChargeSpeed,
            floatValue = -40f,
        });
        idleactions.Insert(0, new SetIntValue
        {
            intVariable = BounceCount,
            intValue = 0,
        });
        idleactions.Insert(0, new SetFloatValue
        {
            floatVariable = BouncePower,
            floatValue = 65f,
        });
        idleactions.Insert(0, new SetIntValue
        {
            intVariable = ComboCount,
            intValue = 0,
        });
        Idle.Actions = idleactions.ToArray();

        #endregion

        //Multi-charge or charge combo
        #region MultiCharge
        //soeed increasing with more charge
        if (Charging.Actions[4] is AccelerateToXByScale setAccel)
        {
            setAccel.accelerationFactor = 10f;
            setAccel.targetSpeed = ChargeSpeed;
        }
        if (Jumpback.Actions[0] is SetVelocityByScale vel)
        {
            vel.speed = 100f;
        }
        var chargeactions = MultiCharge.Actions.ToList();
        chargeactions.Add(new IntAdd
        {
            intVariable = ChargeCount,
            add = 1,
        });
        chargeactions.Add(new FloatAdd
        {
            floatVariable = ChargeSpeed,
            add = -10f,
        });
        chargeactions.Add(new IntCompare
        {
            integer1 = ChargeCount,
            integer2 = MaxCharge,
            equal = FsmEvent.Finished,
            lessThan = Charge,
            greaterThan = Combo,
        });
        MultiCharge.Actions = chargeactions.ToArray();

        var MCTransition = new FsmTransition
        {
            ToFsmState = ChargeStart,
            ToState = "Jump Back Dir",
            FsmEvent = Charge,
        };
        var FinishedTransition = new FsmTransition
        {
            ToFsmState = Idle,
            ToState = "Start Idle",
            FsmEvent = FsmEvent.Finished,
        };
        var MCComboTransition = new FsmTransition
        {
            ToFsmState = AirThrowStart,
            FsmEvent = Combo,
        };
        MultiCharge.Transitions = [ MCTransition, FinishedTransition, MCComboTransition ];


        var chargeendactions = ChargeEnd.Actions.ToList();
        chargeactions.Add(new Wait
        {
            time = 0.1f,
            finishEvent = FsmEvent.Finished,
        });
        ChargeEnd.Actions = chargeendactions.ToArray();
        var ChargeTransition = ChargeEnd.Transitions.First(tran => tran.EventName == "FINISHED");
        ChargeTransition.toState = "Charge Again";
        ChargeTransition.toFsmState = MultiCharge;
        #endregion

        //Multi-bounce or bounce combo
        #region MultiBounce
        var rebounceactions = ReBounce.Actions.ToList();
        foreach (var action in Spinlaunch.Actions)
        {
            if (!(action is Tk2dPlayAnimationWithEvents))
            { rebounceactions.Add(action); }
        }
        foreach (var transition in Spinlaunch.Transitions)
        {
            ReBounce.Transitions = [transition];
        }
        ReBounce.Actions = rebounceactions.ToArray();

        var Bounceactions = MultiBounce.Actions.ToList();
        Bounceactions.Add(new IntAdd
        {
            intVariable = BounceCount,
            add = 1,
        });
        Bounceactions.Add(new InvokeMethod(SmallerSlam));
        Bounceactions.Add(new IntCompare
        {
            integer1 = BounceCount,
            integer2 = MaxBounce,
            equal = FsmEvent.Finished,
            lessThan = Bounce,
            greaterThan = Combo,
        });
        MultiBounce.Actions = Bounceactions.ToArray();

        if (Spinlaunch.Actions[1] is SetVelocityByScale bouncevel)
        {
            bouncevel.ySpeed = BouncePower;
        }

        var MultiBounceTransition = new FsmTransition
        {
            ToFsmState = ReBounce,
            ToState = "ReBounce",
            FsmEvent = Bounce,
        };
        var SlamFinishedTransition = new FsmTransition
        {
            ToFsmState = SlamEnd,
            ToState = "Spin Attack Land",
            FsmEvent = FsmEvent.Finished,
        };
        var SlamComboTransition = new FsmTransition
        {
            ToFsmState = SpinStart,
            ToState = "Cyclone Antic",
            FsmEvent = Combo,
        };

        MultiBounce.Transitions = [MultiBounceTransition, SlamFinishedTransition, SlamComboTransition];

        var SlamTransition = Spearslam.Transitions.First(tran => tran.EventName == "FINISHED");
        SlamTransition.toState = "Bounce Again";
        SlamTransition.toFsmState = MultiBounce;
        var HitTransition = SlamHit.Transitions.First(tran => tran.EventName == "FINISHED");
        HitTransition.toState = "Bounce Again";
        HitTransition.toFsmState = MultiBounce;

        #endregion

        


        var spinendactions = SpinEnd.Actions.ToList();
        foreach (var action in SlashEnd.Actions)
        { spinendactions.Add(action); }
        var SpinFinishedTransition = new FsmTransition
        {
            ToFsmState = SpinCombo,
            ToState = "Combo Spin",
            FsmEvent = FsmEvent.Finished
        };
        SpinEnd.Transitions = [SpinFinishedTransition];
        SpinEnd.Actions = spinendactions.ToArray();

        var SpinSemiEndTransition = SpinSemiEnd.Transitions.First(tran => tran.EventName == "FINISHED");
        SpinSemiEndTransition.toState = "Cyclone End";
        SpinSemiEndTransition.toFsmState = SpinEnd;


        //Charge->Airthrow->Slash->Slam->Spin->Throw->Charge Cycle
        #region combo attacks
        foreach (var endstate in new[] {ThrowEnd, SlashEnd, SpinEnd, AirThrowEnd})
        {
            var EndTransition = endstate.Transitions.First(transition => transition.EventName == "FINISHED");
            switch (endstate.name)
            {
                case "Double Throw?":
                    EndTransition.toState = "Combo Throw";
                    EndTransition.toFsmState = ThrowCombo;
                    break;
                case "Slash End":
                    EndTransition.toState = "Combo Slash";
                    EndTransition.toFsmState = SlashCombo;
                    break;
                case "Cyclone End":
                    EndTransition.toState = "Combo Spin";
                    EndTransition.toFsmState = SpinCombo;
                    break;
                case "Throw Land":
                    EndTransition.toState = "Combo Air";
                    EndTransition.toFsmState = AirThrowCombo;
                    break;
            }
        }
        foreach (var combostate in new[] { ThrowCombo, SlashCombo, SpinCombo, AirThrowCombo })
        {
            var ComboTransition = new FsmTransition
            {
                ToFsmState = ThrowStart,
                FsmEvent = Combo,
            };
            var ComboFinishTransition = new FsmTransition
            {
                ToFsmState = Idle,
                FsmEvent = FsmEvent.Finished,
            };
            var comboactions = combostate.Actions.ToList();
            comboactions.Add(new IntAdd
            {
                intVariable = ComboCount,
                add = 1,
            });
            comboactions.Add(new SetIntValue
            {
                intVariable = ChargeCount,
                intValue = 10,
            });
            comboactions.Add(new SetIntValue
            {
                intVariable = BounceCount,
                intValue = 10,
            });
            comboactions.Add(new IntCompare
            {
                integer1 = ComboCount,
                integer2 = MaxCombo,
                equal = FsmEvent.Finished,
                lessThan = Combo,
                greaterThan = FsmEvent.Finished,
            });
            combostate.Actions = comboactions.ToArray();
            switch (combostate.name)
            {
                case "Combo Throw":
                    ComboTransition.toState = "Jump Back Dir";
                    ComboTransition.toFsmState = ChargeStart;
                    break;
                case "Combo Slash":
                    ComboTransition.toState = "Jump Antic";
                    ComboTransition.toFsmState = SlamStart;
                    break;
                case "Combo Spin":
                    ComboTransition.toState = "Throw Antic";
                    ComboTransition.toFsmState = ThrowStart;
                    break;
                case "Combo Air":
                    ComboTransition.toState = "Slash Antic";
                    ComboTransition.toFsmState = SlashStart;
                    break;
            }
            combostate.Transitions = [ComboTransition, ComboFinishTransition];
        }
        #endregion

    }
    //Make every other slam smaller for balance issue
    private void SmallerSlam()
    {
        if (BounceCount.Value % 2 != 0) {BouncePower.Value = 35f;}
        else { BouncePower.Value = 65f; }
    }

    //Phase 2
    private void ModifyPhase2() {
        FsmState Phase2 = _control.FsmStates.First(state => state.Name == "P2 Roar");
        if (Phase2.Actions[5] is SetFloatValue idlemin)
        {
            idlemin.floatValue = 0.1f;
        }
        if (Phase2.Actions[6] is SetFloatValue idlemax)
        {
            idlemax.floatValue = 0.1f;
        }
        var P2actions = Phase2.Actions.ToList();
        P2actions.Add(new IntAdd
        {
            intVariable = MaxCharge,
            add = 2,
        });
        P2actions.Add(new IntAdd
        {
            intVariable = MaxBounce,
            add = 1,
        });
        P2actions.Add(new IntAdd
        {
            intVariable = MaxCombo,
            add = 2,
        });
        P2actions.Add(new InvokeMethod(ModifyBlades));
        Phase2.Actions = P2actions.ToArray();

        ModifyPhase3();

    }


    private void ModifyBlades()
    {
        FsmState ThrowL = _control.FsmStates.First(state => state.Name == "Throw L");
        FsmState ThrowR = _control.FsmStates.First(state => state.Name == "Throw R");
        FsmState AirThrow = _control.FsmStates.First(state => state.Name == "Air Sickles");
        foreach (var throwing in new [] {ThrowL, ThrowR, AirThrow} )
        {
            var throwactions = throwing.Actions.ToList();
            throwactions.Insert(5, new InvokeMethod(AddTrapToBlade));
            throwactions.Insert(3, new InvokeMethod(AddTrapToBlade));
            throwing.Actions = throwactions.ToArray();
        }
    }
    private void AddTrapToBlade()
    {
        var go = _control.Fsm.GetFsmGameObject("Sickle").Value;
        _control.Fsm.GetFsmGameObject("Sickle").Value.GetComponent<DamageHero>().damageDealt = 2;
        if (!go.GetComponent<BladeEditor>())
        { go.AddComponent<BladeEditor>(); }
    }

    private void ModifyPhase3()
    {
        FsmState Phase3 = _control.FsmStates.First(state => state.Name == "P3 Roar");
        if (Phase3.Actions[5] is SetFloatValue idlemin)
        {
            idlemin.floatValue = 0f;
        }
        if (Phase3.Actions[6] is SetFloatValue idlemax)
        {
            idlemax.floatValue = 0f;
        }
        var P3actions = Phase3.Actions.ToList();
        P3actions.Add(new IntAdd
        {
            intVariable = MaxCharge,
            add = 2,
        });
        P3actions.Add(new IntAdd
        {
            intVariable = MaxBounce,
            add = 1,
        });
        P3actions.Add(new IntAdd
        {
            intVariable = MaxCombo,
            add = 5,
        });
        P3actions.Add(new InvokeMethod(StartPhase3));
        Phase3.Actions = P3actions.ToArray();
    }

    private void StartPhase3()
    {
        FsmState JumpSpin = _control.FsmStates.First(state => state.Name == "Spin Antic");
        FsmState AirSpin = _control.FsmStates.First(state => state.Name == "Launch Spin");
        FsmState DoubleThrow = _control.FsmStates.First(state => state.Name == "Rethrow 2");
        FsmState ThrowCombo = _control.FsmStates.First(state => state.Name == "Combo Throw");
        var EndTransition = DoubleThrow.Transitions.First(transition => transition.EventName == "FINISHED");
        EndTransition.toState = "Combo Throw";
        EndTransition.toFsmState = ThrowCombo;

        var spinactions = JumpSpin.Actions.ToList();
        spinactions.Insert(0, new InvokeMethod(PlaceTrapCondition));
        JumpSpin.Actions = spinactions.ToArray();
        var spin2actions = AirSpin.Actions.ToList();
        spin2actions.Insert(0, new InvokeMethod(PlaceTrap));
        AirSpin.Actions = spin2actions.ToArray();
    }
    private void PlaceTrap()
    {
        GameObject trap = Instantiate(TrapLoader.GetTrap());
        trap.transform.position = gameObject.transform.position;
        trap.SetActive(true);
    }
    private void PlaceTrapCondition()
    {
        if (BounceCount.Value > 3)
        {
            GameObject trap = Instantiate(TrapLoader.GetTrap());
            trap.transform.position = gameObject.transform.position;
            trap.SetActive(true);
        }
    }
    private void PlaceUnbreakableTraps()
    {
        for (int i = 0; i < 10; i++) {
            GameObject trap = Instantiate(TrapLoader.GetTrap());
            trap.transform.position = new Vector3(135f + 3*i, 36f, 0f);
            trap.AddComponent<PermanentTrap>();
            trap.SetActive(true);

        }
    }

    private void OnDestroy() {
    }
}