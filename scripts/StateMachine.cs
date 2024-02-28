using Godot;
using Brave.scripts.Interface;

[GlobalClass]
public partial class StateMachine : Node
{
    private int _currentState = -1;

    public float StateTime;
    public int CurrentState
    {
        get => _currentState;
        set
        { 
            _state.TransitionState(_currentState,value);  
            _currentState = value;
        }
    }

    private IStateHandler _state;

    public override async void _Ready()
    {
        if (Owner is not IStateHandler)
        {
            //TODO throw error 
        }
        _state = Owner as IStateHandler;
        await ToSignal(Owner, Node.SignalName.Ready);
        CurrentState = 1;
    }

    public override void _PhysicsProcess(double delta)
    {
        while (true)
        {
            var next = _state.GetNextState(CurrentState);
            if (CurrentState == next)
            {
                break;
            }
            else
            {
                CurrentState = next;
            }
            
        }
        _state.TickPhysics(CurrentState,delta);
        StateTime += (float)delta;
    }
}
