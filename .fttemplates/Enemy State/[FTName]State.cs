using System.Diagnostics;

public class [FTName]State : BaseState<EnemyState>
{
    private EnemyFSM enemyFSM;

    public [FTName]State(EnemyFSM fsm) : base(EnemyState.[FTName])
    {
        enemyFSM = fsm;
    }

    public override void EnterState()
    {

    }

    public override void UpdateState()
    {

    }

    public override void ExitState()
    {

    }
}
