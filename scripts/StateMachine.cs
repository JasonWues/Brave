using Godot;
using Brave.scripts.Interface;

[GlobalClass]
public partial class StateMachine : Node
{
    private int _currentState = -1;
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
    }
}
